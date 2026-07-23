using McHoneypot.Adapters.MinecraftProtocol.Io;
using McHoneypot.Adapters.MinecraftProtocol.Packets;

namespace McHoneypot.Adapters.MinecraftProtocol.Decoders;

public interface IPacketDecoder
{
    IServerboundPacket Decode(ref PacketReader reader);
}