using McHoneypot.Adapters.MinecraftProtocol.Packets;
using McHoneypot.Adapters.MinecraftProtocol.Packets.Clientbound;

namespace McHoneypot.Adapters.MinecraftProtocol.Io;

public static class PacketWriter
{
    public static void SendPacket(Stream networkStream, IClientboundPacket packet)
    {
        using var payloadStream = new MemoryStream();

        payloadStream.WriteVarInt(packet.PacketId);

        switch (packet)
        {
            case StatusResponsePacket statusPacket:
                payloadStream.WriteMinecraftString(statusPacket.JsonResponse);
                break;

            case PongResponsePacket pongPacket:
                payloadStream.WriteLong(pongPacket.Payload);
                break;
        }


        var payloadBytes = payloadStream.ToArray();
        var packetLength = payloadBytes.Length;

        networkStream.WriteVarInt(packetLength);
        networkStream.Write(payloadBytes, 0, payloadBytes.Length);
        networkStream.Flush();
    }
}