using System.Buffers.Binary;
using McHoneypot.Adapters.MinecraftProtocol.Io;
using McHoneypot.Adapters.MinecraftProtocol.Packets;
using McHoneypot.Adapters.MinecraftProtocol.Packets.Serverbound;

namespace McHoneypot.Adapters.MinecraftProtocol.Decoders;

public class PingRequestDecoder : IPacketDecoder
{
    public IServerboundPacket Decode(ref PacketReader reader)
    {
        var payload = reader.ReadLong();

        return new PingRequestPacket(payload);
    }
}