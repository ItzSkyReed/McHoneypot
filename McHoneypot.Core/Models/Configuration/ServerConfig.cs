using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace McHoneypot.Core.Models.Configuration;

[JsonConverter(typeof(JsonStringEnumConverter<LogLevel>))]
public class ServerConfig
{
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    public string BindAddress { get; set; } = "0.0.0.0";
    public ushort Port { get; set; } = 25565;

    public string VersionName { get; set; } = "Paper 1.20.4";
    public string Description { get; set; } = "§aVanilla Survival §c[1.20.4]§r\n§eWelcome!";


    public int TimeoutMs { get; set; } = 10000;

    public ProtocolMode ProtocolBehavior { get; set; } = ProtocolMode.Chameleon;
    public int FixedProtocolVersion { get; set; } = 765; // 1.20.4 by default

    public int MaxClientPacketLength { get; set; } = 65536;

    public int MaxPlayers { get; set; } = 1000;
    public int OnlinePlayers { get; set; } = 874;

    public TrapConfig Trap { get; set; } = new();
}