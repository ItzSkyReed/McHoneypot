using System.Security.Cryptography;
using System.Text;

namespace McHoneypot.Application.Services;

public static class OfflinePlayerGenerator
{
    public static (string Name, string Id) GeneratePlayer(string playerName)
    {
        var data = Encoding.UTF8.GetBytes($"OfflinePlayer:{playerName}");

        var hash = MD5.HashData(data);

        hash[6] = (byte)(hash[6] & 0x0f | 0x30);
        hash[8] = (byte)(hash[8] & 0x3f | 0x80);

        var uuid = new Guid(hash);

        return (playerName, uuid.ToString());
    }
}