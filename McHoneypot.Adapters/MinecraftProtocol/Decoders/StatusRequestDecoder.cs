using McHoneypot.Adapters.MinecraftProtocol.Io;
using McHoneypot.Adapters.MinecraftProtocol.Packets;
using McHoneypot.Adapters.MinecraftProtocol.Packets.Serverbound;

namespace McHoneypot.Adapters.MinecraftProtocol.Decoders;


public class StatusRequestDecoder : IPacketDecoder
{
    public IServerboundPacket Decode(ref PacketReader reader)
    {
        return new StatusRequestPacket();
    }
}