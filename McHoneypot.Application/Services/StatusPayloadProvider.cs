using McHoneypot.Core.Models.Configuration;

namespace McHoneypot.Application.Services;

public class StatusPayloadProvider
{
    private readonly string? _jsonPrefix;
    private readonly string? _jsonSuffix;

    private readonly string? _cachedFullJson;

    public StatusPayloadProvider(ServerConfig config, FakePlayerProvider fakePlayers)
    {
        if (config.ProtocolBehavior == ProtocolMode.Chameleon)
        {
            _jsonPrefix = $$"""
                          {
                              "version": {
                                  "name": "{{config.VersionName}}",
                                  "protocol":
                          """;

            _jsonSuffix = $$"""
                          
                              },
                              "players": {
                                  "max": {{config.MaxPlayers}},
                                  "online": {{config.OnlinePlayers}},
                                  "sample": {{fakePlayers.CachedSampleJson}}
                              },
                              "description": {
                                  "text": "{{config.Description}}"
                              }
                          }
                          """;
        }
        else
        {
            _cachedFullJson = $$"""
                              {
                                  "version": {
                                      "name": "{{config.VersionName}}",
                                      "protocol": {{config.FixedProtocolVersion}}
                                  },
                                  "players": {
                                      "max": {{config.MaxPlayers}},
                                      "online": {{config.OnlinePlayers}},
                                      "sample": {{fakePlayers.CachedSampleJson}}
                                  },
                                  "description": {
                                      "text": "{{config.Description}}"
                                  }
                              }
                              """;
        }
    }

    public string GetPayload(int clientProtocolVersion)
    {

        if (_cachedFullJson != null)
            return _cachedFullJson;

        return string.Concat(
            _jsonPrefix,
            clientProtocolVersion.ToString(),
            _jsonSuffix
        );
    }
}