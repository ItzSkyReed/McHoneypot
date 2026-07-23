namespace McHoneypot.Adapters.MinecraftProtocol.Packets.Clientbound;

public class PongResponsePacket(long payload) : IClientboundPacket
{
    public int PacketId => 0x01;
    public long Payload { get; } = payload;
}