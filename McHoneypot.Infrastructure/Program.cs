using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using McHoneypot.Adapters.Controllers;
using McHoneypot.Core.Models.Configuration;
using McHoneypot.Infrastructure.Configuration;

namespace McHoneypot.Infrastructure;

internal static class Program
{
    private const string ConfigPath = "config.json";
    private static ServerConfig _config = null!;

    private static async Task Main()
    {
        Console.WriteLine("Initializing Minecraft Honeypot...");
        LoadConfiguration();

        var bindAddress = IPAddress.Parse(_config.BindAddress);
        var listener = new TcpListener(bindAddress, _config.Port);

        try
        {
            listener.Start();
            Console.WriteLine($"[+] Server started at {_config.BindAddress}:{_config.Port}");
            Console.WriteLine($"[+] Protocol Mode: {_config.ProtocolBehavior}");

            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleClientAsync(client));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[!] Critical Error: {ex.Message}");
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
            Console.WriteLine($"[!] File {ConfigPath} not found. Creating new one...");
            _config = new ServerConfig();


            var defaultJson = JsonSerializer.Serialize(_config, ConfigJsonContext.Default.ServerConfig);
            File.WriteAllText(ConfigPath, defaultJson);
        }
    }

    private static void HandleClientAsync(TcpClient client)
    {
        var clientIp = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
        Console.WriteLine($"[>] Connection from: {clientIp}");

        try
        {
            client.ReceiveTimeout = _config.TimeoutMs;
            client.SendTimeout = _config.TimeoutMs;

            using var stream = client.GetStream();

            var handler = new ClientConnectionHandler(_config);
            handler.HandleStream(stream);
        }
        catch (EndOfStreamException)
        {
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[X] Error clientIp: {ex.Message}");
        }
        finally
        {
            client.Close();
            Console.WriteLine($"[<] Disconnected: {clientIp}");
        }
    }
}