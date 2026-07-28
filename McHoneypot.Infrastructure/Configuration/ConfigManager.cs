using System.Text.Json;
using System.Text.Json.Nodes;
using McHoneypot.Core.Models.Configuration;

namespace McHoneypot.Infrastructure.Configuration;

public static partial class ConfigManager
{
    public static ServerConfig Load(string configPath, out List<string> addedProperties)
    {
        var defaultConfig = new ServerConfig();
        addedProperties = [];

        return !File.Exists(configPath)
            ? CreateAndSaveDefaultConfig(defaultConfig, configPath)
            : LoadAndUpdateExistingConfig(defaultConfig, configPath, addedProperties);
    }

    private static ServerConfig CreateAndSaveDefaultConfig(ServerConfig defaultConfig, string configPath)
    {
        var defaultJson = JsonSerializer.Serialize(defaultConfig, ConfigJsonContext.Default.ServerConfig);
        File.WriteAllText(configPath, defaultJson);
        return defaultConfig;
    }

    private static ServerConfig LoadAndUpdateExistingConfig(ServerConfig defaultConfig, string configPath, List<string> addedProperties)
    {
        var existingJson = File.ReadAllText(configPath);
        var existingNode = JsonNode.Parse(existingJson)?.AsObject() ?? new JsonObject();

        var defaultJson = JsonSerializer.Serialize(defaultConfig, ConfigJsonContext.Default.ServerConfig);
        var defaultNode = JsonNode.Parse(defaultJson)!.AsObject();

        var hasUpdates = MergeMissingProperties(existingNode, defaultNode, addedProperties);

        if (!hasUpdates)
            return JsonSerializer.Deserialize(existingNode.ToJsonString(), ConfigJsonContext.Default.ServerConfig)
                   ?? defaultConfig;

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(configPath, existingNode.ToJsonString(options));

        return JsonSerializer.Deserialize(existingNode.ToJsonString(), ConfigJsonContext.Default.ServerConfig)
               ?? defaultConfig;
    }

    private static bool MergeMissingProperties(JsonObject target, JsonObject source, List<string> addedKeys, string currentPath = "")
    {
        var isUpdated = false;

        foreach (var (key, sourceValue) in source)
        {
            var fullPath = string.IsNullOrEmpty(currentPath) ? key : $"{currentPath}.{key}";

            if (!target.ContainsKey(key))
            {
                target.Add(key, CloneNode(sourceValue));
                addedKeys.Add(fullPath);
                isUpdated = true;
            }
            else
            {
                var targetValue = target[key];

                if (sourceValue is not JsonObject sourceObj || targetValue is not JsonObject targetObj) continue;

                if (MergeMissingProperties(targetObj, sourceObj, addedKeys, fullPath))
                    isUpdated = true;
            }
        }

        return isUpdated;
    }

    private static JsonNode? CloneNode(JsonNode? node)
    {
        return node == null
            ? null
            : JsonNode.Parse(node.ToJsonString());
    }
}