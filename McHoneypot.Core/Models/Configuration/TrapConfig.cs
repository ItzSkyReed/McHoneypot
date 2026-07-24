namespace McHoneypot.Core.Models.Configuration;

public class TrapConfig
{
    // Количество фейковых игроков, которых мы будем генерировать в ответе
    public int FakePlayersCount { get; set; } = 100;

    // Включает режим "смоляной ямы" (Tarpit) для замедления ответов
    public bool EnableTarpit { get; set; } = true;

    // Задержка перед отправкой первого байта ответа (в миллисекундах)
    public int InitialDelayMs { get; set; } = 5000;

    // Искусственное ограничение скорости ответа (байт в секунду).
    // Заставит сканер атакующего читать наш короткий JSON-ответ целую вечность.
    public int MaxBytesPerSecond { get; set; } = 5;

    public List<string> BaseNames { get; set; } =
        ["Honda", "Brede", "Titkta", "SlopEd", "Brudd", "Jerr3", "Kokonito", "Franc", "BodyKamobebady", "HellDi"];

    public List<string> Prefixes { get; set; } = ["xX_", "Real_", "Pro_", "Super", "MC_", "Itz_", "1", "2"];

    public List<string> Suffixes { get; set; } = ["_Xx", "1337", "HD", "YT", "Gamer", "_Pro", "228", "SPUN"];
}