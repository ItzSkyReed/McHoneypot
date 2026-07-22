using McHoneypot.Core.Models;
using McHoneypot.Adapters.MinecraftProtocol.Decoders;

namespace McHoneypot.Adapters.MinecraftProtocol;

public class PacketRegistry
{
    // Словарь: (Состояние, ID Пакета) -> Экземпляр декодера
    private readonly Dictionary<(ConnectionState, int), IPacketDecoder> _decoders = new();

    public PacketRegistry()
    {
        // Packet Registry
        _decoders.Add((ConnectionState.Handshaking, 0x00), new HandshakeDecoder());

        _decoders.Add((ConnectionState.Status, 0x00), new StatusRequestDecoder());
        // _decoders.Add((ConnectionState.Status, 0x01), new PingDecoder());
    }

    public IPacketDecoder? GetDecoder(ConnectionState state, int packetId)
    {
        return _decoders.GetValueOrDefault((state, packetId)); // Пакет неизвестен или не ожидается в этом состоянии
    }
}