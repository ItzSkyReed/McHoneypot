using System;
using System.Buffers;
using McHoneypot.Adapters.MinecraftProtocol.Packets;
using McHoneypot.Adapters.MinecraftProtocol.Packets.Serverbound;

namespace McHoneypot.Adapters.MinecraftProtocol.Decoders;

public sealed class PingRequestDecoder : IPacketDecoder
{
    public static readonly PingRequestDecoder Instance = new();

    public IServerboundPacket Decode(ref SequenceReader<byte> reader)
    {
        return !reader.TryReadBigEndian(out long payload)
            ? throw new InvalidOperationException("Not enough data to read Ping payload")
            : new PingRequestPacket(payload);
    }
}