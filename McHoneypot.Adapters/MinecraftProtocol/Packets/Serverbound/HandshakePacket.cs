namespace McHoneypot.Adapters.MinecraftProtocol.Packets.Serverbound;

public class HandshakePacket : IServerboundPacket
{
    public int ProtocolVersion { get; set; }
    public string ServerAddress { get; set; } = string.Empty;
    public ushort ServerPort { get; set; }

    /// <summary>
    /// 1 for Status, 2 for Login, 3 for Transfer
    /// </summary>
    public int Intent { get; set; }
}