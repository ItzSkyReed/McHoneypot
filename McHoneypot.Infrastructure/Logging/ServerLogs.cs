using Microsoft.Extensions.Logging;

namespace McHoneypot.Infrastructure.Logging;

public static partial class ServerLogs
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Initializing Minecraft Honeypot...")]
    public static partial void Initializing(ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "[+] Server started at {Address}:{Port}")]
    public static partial void ServerStarted(ILogger logger, string address, int port);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "[+] Protocol Mode: {Mode}")]
    public static partial void ProtocolMode(ILogger logger, string mode);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "[!] File {ConfigPath} not found. Creating new one...")]
    public static partial void ConfigNotFound(ILogger logger, string configPath);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "[>] Connection from: {ClientIp}")]
    public static partial void ClientConnected(ILogger logger, string clientIp);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "[<] Disconnected: {ClientIp}")]
    public static partial void ClientDisconnected(ILogger logger, string clientIp);

    [LoggerMessage(EventId = 7, Level = LogLevel.Error, Message = "[X] Error clientIp: {ClientIp}")]
    public static partial void ClientError(ILogger logger, Exception ex, string clientIp);

    [LoggerMessage(EventId = 8, Level = LogLevel.Critical, Message = "[!] Critical Error during server execution.")]
    public static partial void CriticalError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 9, Level = LogLevel.Warning, Message = "[!] Dropped oversized packet from {ClientIp}: requested {Length} bytes (Max: {MaxLength})")]
    public static partial void OversizedPacketAttempt(ILogger logger, string clientIp, int length, int maxLength);
}