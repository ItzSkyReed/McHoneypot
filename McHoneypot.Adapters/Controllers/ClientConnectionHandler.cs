using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using McHoneypot.Adapters.MinecraftProtocol;
using McHoneypot.Adapters.MinecraftProtocol.Io;
using McHoneypot.Adapters.MinecraftProtocol.Packets;
using McHoneypot.Adapters.MinecraftProtocol.Packets.Clientbound;
using McHoneypot.Adapters.MinecraftProtocol.Packets.Serverbound;
using McHoneypot.Application.Services;
using McHoneypot.Core.Models;
using McHoneypot.Core.Models.Configuration;
using Microsoft.Extensions.Logging;

namespace McHoneypot.Adapters.Controllers;

public partial class ClientConnectionHandler(
    ServerConfig config,
    StatusPayloadProvider statusPayloadProvider,
    Socket socket,
    ILogger<ClientConnectionHandler> logger)
{
    private ConnectionState _currentState = ConnectionState.Handshaking;
    private int _clientProtocolVersion = config.FixedProtocolVersion;

    public async Task HandleConnectionAsync(CancellationToken ct = default)
    {
        var pipe = new Pipe(new PipeOptions(
            pauseWriterThreshold: 64 * 1024,
            resumeWriterThreshold: 32 * 1024,
            minimumSegmentSize: 512,
            useSynchronizationContext: false));

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        var writing = FillPipeAsync(socket, pipe.Writer, cts.Token);
        var reading = ReadPipeAsync(pipe.Reader, cts.Token);

        await Task.WhenAny(reading, writing);

        await cts.CancelAsync();

        try
        {
            await Task.WhenAll(reading, writing);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            LogUnexpectedConnectionReset(logger, ex);
        }
    }

    private static async Task FillPipeAsync(Socket socket, PipeWriter writer, CancellationToken ct)
    {
        const int minimumBufferSize = 512;
        Exception? error = null;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var memory = writer.GetMemory(minimumBufferSize);

                var bytesRead = await socket.ReceiveAsync(memory, SocketFlags.None, ct);

                if (bytesRead == 0)
                    break;

                writer.Advance(bytesRead);

                var result = await writer.FlushAsync(ct);

                if (result.IsCompleted || result.IsCanceled)
                    break;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            error = ex;
        }
        finally
        {
            await writer.CompleteAsync(error);
        }
    }

    private async Task ReadPipeAsync(PipeReader reader, CancellationToken ct)
    {
        Exception? error = null;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var result = await reader.ReadAsync(ct);
                var buffer = result.Buffer;

                if (result.IsCanceled)
                    break;

                while (MinecraftPacketParser.TryParse(ref buffer, config.MaxClientPacketLength, out var packetId, out var payload,
                           out var consumedTo))
                {
                    if (PacketRegistry.TryGetDecoder(_currentState, packetId, out var decoder))
                    {
                        var payloadReader = new SequenceReader<byte>(payload);
                        var packet = decoder.Decode(ref payloadReader);

                        await ProcessPacketAsync(packet, ct);
                    }
                    else
                    {
                        error = new InvalidDataException($"Unknown or invalid packet ID 0x{packetId:X2} in state {_currentState}");
                        return;
                    }

                    buffer = buffer.Slice(consumedTo);
                }

                reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                    break;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            error = ex;
        }
        catch
        {

        }
        finally
        {
            await reader.CompleteAsync(error);
        }
    }

    private async Task ProcessPacketAsync(IServerboundPacket packet, CancellationToken cancellationToken)
    {
        switch (packet)
        {
            case HandshakePacket handshake:
                _currentState = (ConnectionState)handshake.Intent;

                _clientProtocolVersion = config.ProtocolBehavior switch
                {
                    ProtocolMode.Chameleon => handshake.ProtocolVersion,
                    ProtocolMode.Fixed => config.FixedProtocolVersion,
                    _ => _clientProtocolVersion
                };
                break;

            case StatusRequestPacket:
                var validJson = statusPayloadProvider.GetPayload(_clientProtocolVersion);

                var responsePacket = new StatusResponsePacket(validJson);
                await SendPacketAsync(responsePacket, cancellationToken);
                break;

            case PingRequestPacket ping:
                var pongPacket = new PongResponsePacket(ping.Payload);
                await SendPacketAsync(pongPacket, cancellationToken);
                break;
        }
    }

    private async Task SendPacketAsync(IClientboundPacket packet, CancellationToken ct)
    {
        var payloadSize = packet switch
        {
            StatusResponsePacket s => PacketWriter.GetVarIntSize(s.PacketId) + PacketWriter.GetMinecraftStringSize(s.JsonResponse),
            PongResponsePacket p => PacketWriter.GetVarIntSize(p.PacketId) + 8,
            _ => throw new InvalidOperationException("Undefined packet")
        };

        var totalSize = PacketWriter.GetVarIntSize(payloadSize) + payloadSize;


        var buffer = ArrayPool<byte>.Shared.Rent(totalSize);

        try
        {
            var writer = new PacketWriter(buffer.AsSpan(0, totalSize));

            writer.WriteVarInt(payloadSize);
            writer.WritePacketPayload(packet);

            await socket.SendAsync(buffer.AsMemory(0, totalSize), ct);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }


    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "[!] Dropped oversized packet from: requested {Length} bytes (Max: {MaxLength})")]
    public static partial void OversizedPacketAttempt(ILogger logger, int length, int maxLength);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "The connection was unexpectedly reset or an error occurred.")]
    private static partial void LogUnexpectedConnectionReset(ILogger logger, Exception ex);
}