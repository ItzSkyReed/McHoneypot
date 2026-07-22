namespace McHoneypot.Adapters.MinecraftProtocol.Packets;

public class RawPacket(int length, int packetId, byte[] data)
{
    public int Length { get; } = length;
    public int PacketId { get; } = packetId;
    public byte[] Data { get; } = data;
}