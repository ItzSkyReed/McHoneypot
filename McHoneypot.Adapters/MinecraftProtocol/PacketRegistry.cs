using McHoneypot.Core.Models;
using McHoneypot.Adapters.MinecraftProtocol.Decoders;


namespace McHoneypot.Adapters.MinecraftProtocol;

public static class PacketRegistry
{
    public static IPacketDecoder? GetDecoder(ConnectionState state, int packetId)
    {
        return state switch
        {
            ConnectionState.Handshaking when packetId == 0x00 => HandshakeDecoder.Instance,
            ConnectionState.Status when packetId == 0x00 => StatusRequestDecoder.Instance,
            ConnectionState.Status when packetId == 0x01 => PingRequestDecoder.Instance,
            _ => null
        };
    }
}