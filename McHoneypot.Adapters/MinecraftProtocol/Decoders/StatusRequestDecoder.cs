using System.Buffers;
using McHoneypot.Adapters.MinecraftProtocol.Packets;
using McHoneypot.Adapters.MinecraftProtocol.Packets.Serverbound;

namespace McHoneypot.Adapters.MinecraftProtocol.Decoders;

public sealed class StatusRequestDecoder : IPacketDecoder
{
    public static readonly StatusRequestDecoder Instance = new();
    private static readonly StatusRequestPacket StatusRequestInstance = new();

    public IServerboundPacket Decode(ref SequenceReader<byte> reader)
    {
        return StatusRequestInstance;
    }
}