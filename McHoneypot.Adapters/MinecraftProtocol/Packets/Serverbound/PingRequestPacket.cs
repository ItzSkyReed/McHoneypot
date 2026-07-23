using McHoneypot.Adapters.MinecraftProtocol.Packets;

namespace McHoneypot.Adapters.MinecraftProtocol.Packets.Serverbound;

public class PingRequestPacket(long payload) : IServerboundPacket
{
    public long Payload { get; } = payload;
}