using McHoneypot.Application.Services;
using McHoneypot.Infrastructure.Configuration;
using McHoneypot.Infrastructure.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace McHoneypot.Infrastructure;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        var config = ConfigManager.Load("config.json", out var addedProperties);

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(config.LogLevel);

        builder.Services.AddSingleton(config);
        builder.Services.AddSingleton<FakePlayerProvider>();
        builder.Services.AddSingleton<StatusPayloadProvider>();

        builder.Services.AddHostedService<HoneypotBackgroundService>();

        var host = builder.Build();

        if (addedProperties.Count > 0)
        {
            var logger = host.Services.GetRequiredService<ILogger<Program>>();

            var formattedProps = string.Join("\n", addedProperties.Select(p => $"  + {p}"));

            ServerLogs.ConfigurationUpdated(logger, formattedProps);
        }

        await host.RunAsync();
    }
}