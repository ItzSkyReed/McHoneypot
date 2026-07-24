using System.Text.Json.Serialization;

namespace McHoneypot.Application.Models;

public class FakePlayerItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}