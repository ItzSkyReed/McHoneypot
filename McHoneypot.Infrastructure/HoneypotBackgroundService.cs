using System.Net;
using System.Net.Sockets;
using McHoneypot.Adapters.Controllers;
using McHoneypot.Application.Services;
using McHoneypot.Core.Models.Configuration;
using McHoneypot.Infrastructure.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace McHoneypot.Infrastructure;

public class HoneypotBackgroundService(
    ServerConfig config,
    StatusPayloadProvider statusPayloadProvider,
    IServiceProvider serviceProvider,
    ILogger<HoneypotBackgroundService> logger) : BackgroundService
{
    private const int MaxPayloadLength = 32767;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ServerLogs.Initializing(logger);

        var bindAddress = IPAddress.Parse(config.BindAddress);
        var listener = new TcpListener(bindAddress, config.Port);

        var payloadLength = statusPayloadProvider.GetPayload(config).Length;
        if (payloadLength > MaxPayloadLength)
            ServerLogs.PayloadTooLong(logger, MaxPayloadLength, payloadLength);

        try
        {
            listener.Start();
            ServerLogs.ServerStarted(logger, config.BindAddress, config.Port);
            ServerLogs.ProtocolMode(logger, config.ProtocolBehavior.ToString());


            while (!stoppingToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(stoppingToken);

                _ = HandleClientSafelyAsync(client, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ServerLogs.CriticalError(logger, ex);
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task HandleClientSafelyAsync(TcpClient client, CancellationToken ct)
    {
        var clientIp = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
        ServerLogs.ClientConnected(logger, clientIp);

        try
        {
            client.ReceiveTimeout = config.TimeoutMs;
            client.SendTimeout = config.TimeoutMs;

            var handler = ActivatorUtilities.CreateInstance<ClientConnectionHandler>(
                serviceProvider,
                client.Client
            );

            await handler.HandleConnectionAsync(ct);
        }
        catch (EndOfStreamException)
        {
        }
        catch (Exception ex)
        {
            ServerLogs.ClientError(logger, ex, clientIp);
        }
        finally
        {
            client.Close();
            ServerLogs.ClientDisconnected(logger, clientIp);
        }
    }
}