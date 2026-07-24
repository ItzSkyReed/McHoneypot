using System.Diagnostics.CodeAnalysis;
using McHoneypot.Core.Models;
using McHoneypot.Adapters.MinecraftProtocol.Decoders;


namespace McHoneypot.Adapters.MinecraftProtocol;


public static class PacketRegistry
{
    public static bool TryGetDecoder(ConnectionState state, int packetId, [NotNullWhen(true)] out IPacketDecoder? decoder)
    {
        decoder = state switch
        {
            ConnectionState.Handshaking when packetId == 0x00 => HandshakeDecoder.Instance,
            ConnectionState.Status when packetId == 0x00 => StatusRequestDecoder.Instance,
            ConnectionState.Status when packetId == 0x01 => PingRequestDecoder.Instance,
            _ => null
        };
        return decoder != null;
    }
}