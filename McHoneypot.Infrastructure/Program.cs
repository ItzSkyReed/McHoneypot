using System.Text.Json;
using McHoneypot.Application.Services;
using McHoneypot.Core.Models.Configuration;
using McHoneypot.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace McHoneypot.Infrastructure;

internal static class Program
{
    private const string ConfigPath = "config.json";

    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        var config = LoadConfiguration();

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(config.LogLevel);

        builder.Services.AddSingleton(config);
        builder.Services.AddSingleton<FakePlayerProvider>();
        builder.Services.AddSingleton<StatusPayloadProvider>();

        builder.Services.AddHostedService<HoneypotBackgroundService>();

        var host = builder.Build();
        await host.RunAsync();
    }

    private static ServerConfig LoadConfiguration()
    {
        if (File.Exists(ConfigPath))
        {
            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize(json, ConfigJsonContext.Default.ServerConfig)
                   ?? new ServerConfig();
        }

        var defaultConfig = new ServerConfig();
        var defaultJson = JsonSerializer.Serialize(defaultConfig, ConfigJsonContext.Default.ServerConfig);
        File.WriteAllText(ConfigPath, defaultJson);

        return defaultConfig;
    }
}