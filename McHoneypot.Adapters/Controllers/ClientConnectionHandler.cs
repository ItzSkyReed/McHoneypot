using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
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
    private string ClientIp = "Unknown IP";

    public async Task HandleConnectionAsync(CancellationToken ct = default)
    {
        if (socket.RemoteEndPoint is IPEndPoint remoteEndPoint)
            ClientIp = remoteEndPoint.Address.ToString();


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

                LogHandshake(logger, ClientIp, handshake.ProtocolVersion, handshake.ServerAddress, handshake.ServerPort);

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

                if (config.Trap.EnableTarpit)
                    await SendTarpitPacketAsync(responsePacket, cancellationToken);
                else
                    await SendPacketAsync(responsePacket, cancellationToken);
                break;

            case PingRequestPacket ping:
                var pongPacket = new PongResponsePacket(ping.Payload);
                await SendPacketAsync(pongPacket, cancellationToken);
                break;
        }
    }


    private static (int PayloadSize, int TotalSize) GetPacketSizes(IClientboundPacket packet)
    {
        var payloadSize = packet switch
        {
            StatusResponsePacket s => PacketWriter.GetVarIntSize(s.PacketId) + PacketWriter.GetMinecraftStringSize(s.JsonResponse),
            PongResponsePacket p => PacketWriter.GetVarIntSize(p.PacketId) + 8,
            _ => throw new InvalidOperationException($"Undefined packet type: {packet.GetType().Name}")
        };

        var totalSize = PacketWriter.GetVarIntSize(payloadSize) + payloadSize;
        return (payloadSize, totalSize);
    }

    private static void SerializePacketToSpan(IClientboundPacket packet, int payloadSize, Span<byte> buffer)
    {
        var writer = new PacketWriter(buffer);
        writer.WriteVarInt(payloadSize);
        writer.WritePacketPayload(packet);
    }

    private async Task SendPacketAsync(IClientboundPacket packet, CancellationToken ct)
    {
        var (payloadSize, totalSize) = GetPacketSizes(packet);
        var buffer = ArrayPool<byte>.Shared.Rent(totalSize);

        try
        {
            SerializePacketToSpan(packet, payloadSize, buffer.AsSpan(0, totalSize));

            await socket.SendAsync(buffer.AsMemory(0, totalSize), ct);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async Task SendTarpitPacketAsync(IClientboundPacket packet, CancellationToken cancellationToken)
    {
        var (payloadSize, totalSize) = GetPacketSizes(packet);


        var buffer = new byte[totalSize];

        var stopwatch = Stopwatch.StartNew();

        try
        {
            SerializePacketToSpan(packet, payloadSize, buffer);

            if (config.Trap is { EnableTarpit: true, InitialDelayMs: > 0 })
                await Task.Delay(config.Trap.InitialDelayMs, cancellationToken);

            if (!config.Trap.EnableTarpit || config.Trap.MaxBytesPerSecond <= 0)
            {
                await socket.SendAsync(buffer, SocketFlags.None, cancellationToken);
                return;
            }

            var delayPerByteMs = 1000 / config.Trap.MaxBytesPerSecond;

            for (var i = 0; i < buffer.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await socket.SendAsync(buffer.AsMemory(i, 1), SocketFlags.None, cancellationToken);

                await Task.Delay(delayPerByteMs, cancellationToken);
            }

            stopwatch.Stop();
            LogTarpitCompleted(logger, ClientIp, stopwatch.Elapsed.TotalSeconds);
        }
        catch (Exception ex) when (ex is SocketException or IOException or OperationCanceledException)
        {
            stopwatch.Stop();
            LogTarpitDropped(logger, ClientIp, stopwatch.Elapsed.TotalSeconds);
        }
    }


    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "[!] Dropped oversized packet from: requested {Length} bytes (Max: {MaxLength})")]
    public static partial void OversizedPacketAttempt(ILogger logger, int length, int maxLength);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "The connection was unexpectedly reset or an error occurred.")]
    private static partial void LogUnexpectedConnectionReset(ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "[{IpAddress}] Handshake: Protocol={ProtocolVersion}, Address='{ServerAddress}', Port={Port})")
    ]
    private static partial void LogHandshake(ILogger logger, string ipAddress, int protocolVersion, string serverAddress, ushort port);


    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "[{IpAddress}] The scanner has fallen out of the tarpit. Held for {Seconds:F2} sec.")]
    private static partial void LogTarpitDropped(ILogger logger, string ipAddress, double seconds);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "[{IpAddress}] Tarpit completed (data sent). Scanner hung for {Seconds:F2} sec.")]
    private static partial void LogTarpitCompleted(ILogger logger, string ipAddress, double seconds);
}