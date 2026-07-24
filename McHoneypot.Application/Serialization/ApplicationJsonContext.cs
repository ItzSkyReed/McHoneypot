using System.Text.Json.Serialization;
using McHoneypot.Application.Models;

namespace McHoneypot.Application.Serialization;

[JsonSerializable(typeof(List<FakePlayerItem>))]
internal partial class ApplicationJsonContext : JsonSerializerContext
{
}