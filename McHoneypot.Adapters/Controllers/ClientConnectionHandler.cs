using McHoneypot.Adapters.MinecraftProtocol;
using McHoneypot.Adapters.MinecraftProtocol.Io;
using McHoneypot.Adapters.MinecraftProtocol.Packets;
using McHoneypot.Adapters.MinecraftProtocol.Packets.Clientbound;
using McHoneypot.Adapters.MinecraftProtocol.Packets.Serverbound;
using McHoneypot.Core.Models;

namespace McHoneypot.Adapters.Controllers;

public class ClientConnectionHandler
{
    private ConnectionState _currentState = ConnectionState.Handshaking;
    private readonly PacketRegistry _registry = new();

    public void HandleStream(Stream stream)
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

            ProcessPacket(packet);
        }
    }

    private void ProcessPacket(IServerboundPacket packet, Stream networkStream)
    {
        switch (packet)
        {
            case HandshakePacket handshake:
                _currentState = (ConnectionState)handshake.Intent;
                break;

            case StatusRequestPacket:

                string poisonJson = GeneratePoisonJson();

                var responsePacket = new StatusResponsePacket(poisonJson);

                PacketWriter.SendPacket(networkStream, responsePacket);
                break;
        }
    }

    private void ExecuteTrap(Stream stream)
    {
    }


}

