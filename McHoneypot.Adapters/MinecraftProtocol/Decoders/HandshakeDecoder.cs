using McHoneypot.Adapters.MinecraftProtocol.Io;
using McHoneypot.Adapters.MinecraftProtocol.Packets;
using McHoneypot.Adapters.MinecraftProtocol.Packets.Serverbound;

namespace McHoneypot.Adapters.MinecraftProtocol.Decoders;

public class HandshakeDecoder : IPacketDecoder
{
    public IServerboundPacket Decode(Stream stream)
    {
        return new HandshakePacket
        {
            ProtocolVersion = stream.ReadVarInt(),
            ServerAddress = stream.ReadMinecraftString(255),
            ServerPort = stream.ReadBigEndianUShort(),
            Intent = stream.ReadVarInt()
        };
    }
}