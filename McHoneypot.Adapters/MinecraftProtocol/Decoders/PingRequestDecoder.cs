using System.Buffers.Binary;
using McHoneypot.Adapters.MinecraftProtocol.Packets;
using McHoneypot.Adapters.MinecraftProtocol.Packets.Serverbound;

namespace McHoneypot.Adapters.MinecraftProtocol.Decoders;

public class PingRequestDecoder : IPacketDecoder
{
    public IServerboundPacket Decode(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[8];
        stream.ReadExactly(buffer);

        var payload = BinaryPrimitives.ReadInt64BigEndian(buffer);

        return new PingRequestPacket(payload);
    }
}