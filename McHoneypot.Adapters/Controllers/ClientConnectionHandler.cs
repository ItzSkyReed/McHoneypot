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

namespace McHoneypot.Adapters.Controllers;

public sealed class ClientConnectionHandler(ServerConfig config, StatusPayloadProvider statusPayloadProvider, Socket socket)
{
    private readonly NetworkStream _networkStream = new(socket, ownsSocket: false);
    private ConnectionState _currentState = ConnectionState.Handshaking;
    private int _clientProtocolVersion = config.FixedProtocolVersion;

    public async Task HandleConnectionAsync(CancellationToken ct = default)
    {
        var pipe = new Pipe();

        var writing = FillPipeAsync(socket, pipe.Writer, ct);
        var reading = ReadPipeAsync(pipe.Reader, ct);

        await Task.WhenAll(reading, writing);
    }

    private static async Task FillPipeAsync(Socket socket, PipeWriter writer, CancellationToken ct)
    {
        const int minimumBufferSize = 512;

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

                if (result.IsCompleted)
                    break;
            }
        }
        finally
        {
            await writer.CompleteAsync();
        }
    }

    private async Task ReadPipeAsync(PipeReader reader, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var result = await reader.ReadAsync(ct);
                var buffer = result.Buffer;

                while (MinecraftPacketParser.TryParse(ref buffer, out var packetId, out var payload, out var consumedTo))
                {
                    if (PacketRegistry.TryGetDecoder(_currentState, packetId, out var decoder))
                    {
                        var payloadReader = new SequenceReader<byte>(payload);
                        var packet = decoder.Decode(ref payloadReader);

                        await ProcessPacketAsync(packet, ct);
                    }
                    else
                        return;


                    buffer = buffer.Slice(consumedTo);
                }

                reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                    break;
            }
        }
        finally
        {
            await reader.CompleteAsync();
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

            await _networkStream.WriteAsync(buffer.AsMemory(0, totalSize), ct);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}