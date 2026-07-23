using System.Text.Json.Serialization;
using McHoneypot.Core.Models.Configuration;

namespace McHoneypot.Infrastructure.Configuration;


[JsonSerializable(typeof(ServerConfig))]
// [JsonSerializable(typeof(TrapConfig))]
internal partial class ConfigJsonContext : JsonSerializerContext
{
}