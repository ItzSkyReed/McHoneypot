using McHoneypot.Adapters.MinecraftProtocol.Packets;
using McHoneypot.Adapters.MinecraftProtocol.Packets.Serverbound;
using System.Buffers;
using McHoneypot.Adapters.MinecraftProtocol.Io;
using McHoneypot.Adapters.MinecraftProtocol.Types;

namespace McHoneypot.Adapters.MinecraftProtocol.Decoders;

public sealed class HandshakeDecoder : IPacketDecoder
{
    public static readonly HandshakeDecoder Instance = new();

    public IServerboundPacket Decode(ref SequenceReader<byte> reader)
    {
        if (!VarInt.TryRead(ref reader, out var protocolVersion))
            throw new InvalidOperationException("Unable to read ProtocolVersion");

        if (!reader.TryReadMinecraftString(255, out var serverAddress))
            throw new InvalidOperationException("Unable to read ServerAddress");

        if (!reader.TryReadBigEndian(out short port))
            throw new InvalidOperationException("Unable to read ServerPort");

        var serverPort = (ushort)port;

        if (!VarInt.TryRead(ref reader, out var intent))
            throw new InvalidOperationException("Unable to read Intent");

        return new HandshakePacket
        {
            ProtocolVersion = protocolVersion,
            ServerAddress = serverAddress,
            ServerPort = serverPort,
            Intent = intent
        };
    }
}