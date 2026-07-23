using McHoneypot.Adapters.MinecraftProtocol.Io;
using McHoneypot.Adapters.MinecraftProtocol.Packets;
using McHoneypot.Adapters.MinecraftProtocol.Packets.Serverbound;

namespace McHoneypot.Adapters.MinecraftProtocol.Decoders;


public sealed class StatusRequestDecoder : IPacketDecoder
{
    public static readonly StatusRequestDecoder Instance = new();

    public IServerboundPacket Decode(ref PacketReader reader)
    {
        return new StatusRequestPacket();
    }
}