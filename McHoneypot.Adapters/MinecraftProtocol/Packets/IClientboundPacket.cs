namespace McHoneypot.Adapters.MinecraftProtocol.Packets;

public interface IClientboundPacket
{
    int PacketId { get; }
}