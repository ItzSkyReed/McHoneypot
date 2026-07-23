using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using McHoneypot.Adapters.Controllers;
using McHoneypot.Core.Models.Configuration;
using McHoneypot.Infrastructure.Configuration;
using McHoneypot.Infrastructure.Logging;
using Microsoft.Extensions.Logging;

namespace McHoneypot.Infrastructure;

internal static class Program
{
    private const string ConfigPath = "config.json";
    private static ServerConfig _config = null!;
    private static ILogger _logger = null!;

    private static async Task Main()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        _logger = loggerFactory.CreateLogger("McHoneypot");

        ServerLogs.Initializing(_logger);

        LoadConfiguration();

        var bindAddress = IPAddress.Parse(_config.BindAddress);
        var listener = new TcpListener(bindAddress, _config.Port);

        try
        {
            listener.Start();

            ServerLogs.ServerStarted(_logger, _config.BindAddress, _config.Port);
            ServerLogs.ProtocolMode(_logger, _config.ProtocolBehavior.ToString());

            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();

                _ = HandleClientAsync(client);
            }
        }
        catch (Exception ex)
        {
            ServerLogs.CriticalError(_logger, ex);
        }
        finally
        {
            listener.Stop();
        }
    }

    private static void LoadConfiguration()
    {
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            TypeInfoResolver = ConfigJsonContext.Default
        };

        if (File.Exists(ConfigPath))
        {
            var json = File.ReadAllText(ConfigPath);

            _config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.ServerConfig) ?? new ServerConfig();
        }
        else
        {
            ServerLogs.ConfigNotFound(_logger, ConfigPath);
            _config = new ServerConfig();


            var defaultJson = JsonSerializer.Serialize(_config, ConfigJsonContext.Default.ServerConfig);
            File.WriteAllText(ConfigPath, defaultJson);
        }
    }

    private static async Task HandleClientAsync(TcpClient client)
    {
        var clientIp = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
        ServerLogs.ClientConnected(_logger, clientIp);

        try
        {
            client.ReceiveTimeout = _config.TimeoutMs;
            client.SendTimeout = _config.TimeoutMs;

            await using var stream = client.GetStream();
            var handler = new ClientConnectionHandler(_config);

            await handler.HandleStreamAsync(stream);
        }
        catch (EndOfStreamException)
        {
        }
        catch (Exception ex)
        {
            ServerLogs.ClientError(_logger, ex, clientIp);
        }
        finally
        {
            client.Close();
            ServerLogs.ClientDisconnected(_logger, clientIp);
        }
    }
}