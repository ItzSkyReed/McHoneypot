using McHoneypot.Adapters.MinecraftProtocol.Packets;
using McHoneypot.Adapters.MinecraftProtocol.Packets.Serverbound;

namespace McHoneypot.Adapters.MinecraftProtocol.Decoders;


public class StatusRequestDecoder : IPacketDecoder
{
    public IServerboundPacket Decode(Stream stream)
    {
        return new StatusRequestPacket();
    }
}