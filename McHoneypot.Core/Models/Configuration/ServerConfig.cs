namespace McHoneypot.Core.Models.Configuration;

public class ServerConfig
{
    public string BindAddress { get; set; } = "0.0.0.0";
    public int Port { get; set; } = 25565;

    public int TimeoutMs { get; set; } = 10000;

    public ProtocolMode ProtocolBehavior { get; set; } = ProtocolMode.Chameleon;
    public int FixedProtocolVersion { get; set; } = 765; // 1.20.4 by default

    public int MaxClientPacketLength { get; set; } = 65536;

    // For future
    // public TrapConfig Traps { get; set; } = new TrapConfig();
}