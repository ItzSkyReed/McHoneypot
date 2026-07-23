using McHoneypot.Adapters.MinecraftProtocol.Io;
using McHoneypot.Adapters.MinecraftProtocol.Packets;
using McHoneypot.Adapters.MinecraftProtocol.Packets.Serverbound;

namespace McHoneypot.Adapters.MinecraftProtocol.Decoders;

public class HandshakeDecoder : IPacketDecoder
{
    public IServerboundPacket Decode(ref PacketReader reader)
    {
        return new HandshakePacket
        {
            ProtocolVersion = reader.ReadVarInt(),
            ServerAddress = reader.ReadMinecraftString(255),
            ServerPort = reader.ReadUShort(),
            Intent = reader.ReadVarInt()
        };
    }
}