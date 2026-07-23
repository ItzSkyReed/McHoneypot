using System.Buffers;
using McHoneypot.Adapters.MinecraftProtocol;
using McHoneypot.Adapters.MinecraftProtocol.Io;
using McHoneypot.Adapters.MinecraftProtocol.Packets;
using McHoneypot.Adapters.MinecraftProtocol.Packets.Clientbound;
using McHoneypot.Adapters.MinecraftProtocol.Packets.Serverbound;
using McHoneypot.Core.Models;
using McHoneypot.Core.Models.Configuration;

namespace McHoneypot.Adapters.Controllers;

public class ClientConnectionHandler(ServerConfig config)
{
    private ConnectionState _currentState = ConnectionState.Handshaking;

    private int _clientProtocolVersion = config.FixedProtocolVersion;

    public async Task HandleStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        try
        {
            while (true)
            {
                var packetLength = await stream.ReadVarIntAsync(cancellationToken);

                if (packetLength > 2097151) throw new InvalidDataException("Packet too large");

                var packetBuffer = ArrayPool<byte>.Shared.Rent(packetLength);
                try
                {
                    await stream.ReadExactlyAsync(packetBuffer, 0, packetLength, cancellationToken);

                    var packetData = new ReadOnlySpan<byte>(packetBuffer, 0, packetLength);
                    var reader = new PacketReader(packetData);

                    var packetId = reader.ReadVarInt();
                    var decoder = PacketRegistry.GetDecoder(_currentState, packetId);
                    if (decoder == null) return;

                    var packet = decoder.Decode(ref reader);
                    await ProcessPacketAsync(packet, stream, cancellationToken);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(packetBuffer);
                }
            }
        }
        catch (EndOfStreamException)
        {
        }
    }

    private async Task ProcessPacketAsync(IServerboundPacket packet, Stream networkStream, CancellationToken cancellationToken = default)
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
                var validJson = $$"""
                                  {
                                      "version": {
                                          "name": "Any Version",
                                          "protocol": {{_clientProtocolVersion}}
                                      },
                                      "players": {
                                          "max": {{int.MinValue}},
                                          "online": {{int.MaxValue}},
                                          "sample": [
                                              { "name": "Notch", "id": "069a79f4-44e9-4726-a5be-fca90e38aaf5" }
                                          ]
                                      },
                                      "description": {
                                          "text": "§aHoneyPot Server Test!"
                                      }
                                  }
                                  """;

                var responsePacket = new StatusResponsePacket(validJson);
                await PacketWriter.SendPacketAsync(networkStream, responsePacket, cancellationToken);
                break;

            case PingRequestPacket ping:
                var pongPacket = new PongResponsePacket(ping.Payload);
                await PacketWriter.SendPacketAsync(networkStream, pongPacket, cancellationToken);
                break;
        }
    }

    private void ExecuteTrap(Stream stream)
    {
    }
}