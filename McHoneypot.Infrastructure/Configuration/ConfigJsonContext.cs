using System.Text.Json.Serialization;
using McHoneypot.Core.Models.Configuration;

namespace McHoneypot.Infrastructure.Configuration;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower
)]
[JsonSerializable(typeof(ServerConfig))]
[JsonSerializable(typeof(TrapConfig))]
internal partial class ConfigJsonContext : JsonSerializerContext
{
}