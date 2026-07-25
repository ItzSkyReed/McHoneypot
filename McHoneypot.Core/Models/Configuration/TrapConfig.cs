namespace McHoneypot.Core.Models.Configuration;

public class TrapConfig
{
    public int FakePlayersCount { get; set; } = 100;

    public bool EnableTarpit { get; set; } = true;

    public int InitialDelayMs { get; set; } = 5000;


    // Artificially limit the response rate (bytes per second).
    // Will force the attacker's scanner to read our short JSON response forever.
    public int MaxBytesPerSecond { get; set; } = 5;

    public List<string> BaseNames { get; set; } =
        ["Honda", "Brede", "Titkta", "SlopEd", "Brudd", "Jerr3", "Kokonito", "Franc", "BodyKamobebady", "HellDi"];

    public List<string> Prefixes { get; set; } = ["xX_", "Real_", "Pro_", "Super", "MC_", "Itz_", "1", "2"];

    public List<string> Suffixes { get; set; } = ["_Xx", "1337", "HD", "YT", "Gamer", "_Pro", "228", "SPUN"];
}