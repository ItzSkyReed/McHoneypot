using McHoneypot.Core.Models;
using McHoneypot.Adapters.MinecraftProtocol.Decoders;


namespace McHoneypot.Adapters.MinecraftProtocol;

public static class PacketRegistry
{

    private static readonly Dictionary<(ConnectionState, int), IPacketDecoder> Decoders = new();

    static PacketRegistry()
    {
        Decoders.Add((ConnectionState.Handshaking, 0x00), new HandshakeDecoder());
        Decoders.Add((ConnectionState.Status, 0x00), new StatusRequestDecoder());
        Decoders.Add((ConnectionState.Status, 0x01), new PingRequestDecoder());
    }

    public static IPacketDecoder? GetDecoder(ConnectionState state, int packetId)
    {
        return Decoders.GetValueOrDefault((state, packetId));
    }
}