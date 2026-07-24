using System.Text.Json;
using McHoneypot.Application.Models;
using McHoneypot.Application.Serialization;
using McHoneypot.Core.Models.Configuration;

namespace McHoneypot.Application.Services;

// Этот класс должен быть зарегистрирован как Singleton в DI контейнере
public class FakePlayerProvider(ServerConfig config)
{
    // Здесь мы храним уже готовый кусок JSON-а
    public string CachedSampleJson { get; } = GenerateCachedJson(config.Trap);

    private static string GenerateCachedJson(TrapConfig trapConfig)
    {
        if (trapConfig.FakePlayersCount <= 0)
            return "[]";

        var random = new Random();
        var playersList = new List<FakePlayerItem>(trapConfig.FakePlayersCount);


        var generatedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);


        var maxAttempts = trapConfig.FakePlayersCount * 10;
        var attempts = 0;

        while (playersList.Count < trapConfig.FakePlayersCount && attempts < maxAttempts)
        {
            attempts++;

            var baseName = trapConfig.BaseNames[random.Next(trapConfig.BaseNames.Count)];
            var prefix = random.Next(2) == 0 ? trapConfig.Prefixes[random.Next(trapConfig.Prefixes.Count)] : "";
            var suffix = random.Next(2) == 0 ? trapConfig.Suffixes[random.Next(trapConfig.Suffixes.Count)] : "";

            var fullName = $"{prefix}{baseName}{suffix}";

            if (fullName.Length > 16)
                fullName = fullName[..16];

            if (!generatedNames.Add(fullName)) continue;
            var (name, id) = OfflinePlayerGenerator.GeneratePlayer(fullName);
            playersList.Add(new FakePlayerItem { Name = name, Id = id });
        }

        return JsonSerializer.Serialize(playersList, ApplicationJsonContext.Default.ListFakePlayerItem);
    }
}