using McHoneypot.Adapters.MinecraftProtocol;
using McHoneypot.Adapters.MinecraftProtocol.Io;
using McHoneypot.Adapters.MinecraftProtocol.Packets;
using McHoneypot.Adapters.MinecraftProtocol.Packets.Clientbound;
using McHoneypot.Adapters.MinecraftProtocol.Packets.Serverbound;
using McHoneypot.Core.Models;
using McHoneypot.Core.Models.Configuration;

namespace McHoneypot.Adapters.Controllers;

public class ClientConnectionHandler
{
    public ClientConnectionHandler(ServerConfig config)
    {
        _config = config;
        _clientProtocolVersion = config.FixedProtocolVersion;
    }

    private readonly ServerConfig _config;
    private ConnectionState _currentState = ConnectionState.Handshaking;
    private readonly PacketRegistry _registry = new();

    private int _clientProtocolVersion;

    public void HandleStream(Stream stream)
    {
        try
        {
            while (true)
            {
                int packetLength = stream.ReadVarInt();

                if (packetLength > 2097151) throw new InvalidDataException("Packet too large");

                var packetBuffer = new byte[packetLength];
                stream.ReadExactly(packetBuffer, 0, packetLength);

                using var memoryStream = new MemoryStream(packetBuffer);

                int packetId = memoryStream.ReadVarInt();

                var decoder = _registry.GetDecoder(_currentState, packetId);

                if (decoder == null)
                {
                    return;
                }

                var packet = decoder.Decode(memoryStream);

                ProcessPacket(packet, stream);
            }
        }
        catch (EndOfStreamException)
        {
        }
    }

    private void ProcessPacket(IServerboundPacket packet, Stream networkStream)
    {
        switch (packet)
        {
            case HandshakePacket handshake:
                _currentState = (ConnectionState)handshake.Intent;

                _clientProtocolVersion = _config.ProtocolBehavior switch
                {
                    ProtocolMode.Chameleon => handshake.ProtocolVersion,
                    ProtocolMode.Fixed => _config.FixedProtocolVersion,
                    _ => _clientProtocolVersion
                };

                break;

            case StatusRequestPacket _:
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
                PacketWriter.SendPacket(networkStream, responsePacket);
                break;

            case PingRequestPacket ping:
                var pongPacket = new PongResponsePacket(ping.Payload);
                PacketWriter.SendPacket(networkStream, pongPacket);
                break;
        }
    }

    private void ExecuteTrap(Stream stream)
    {
    }
}