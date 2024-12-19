using TiledToLB.Core.Tiled.Map;
using TiledToLB.Core.Tiled.Property;
using TiledToLB.Core.Upgraders;

namespace TiledToLB.Core.Processors
{
    public static class UpgradeProcessor
    {
        public static Task UpgradeExistingAsync(string workspacePath, string mapName, bool silent)
        {
            string filePath = Path.Combine(workspacePath, CommonProcessor.TemplateMapsFolderName, Path.ChangeExtension(mapName, "tmx"));
            TiledMap map = TiledMap.Load(filePath);

            // Get the version of the map, and the version of the tool. If the map has no version, assume it's 0.0.0.
            Version mapVersion = map.Properties.TryGetValue("ToolVersion", out TiledProperty versionProperty) && Version.TryParse(versionProperty.Value, out mapVersion) ? mapVersion : new(0, 0, 0);
            Version currentVersion = typeof(UpgradeProcessor).Assembly.GetName().Version ?? throw new InvalidOperationException("Tool is missing version");
            
            // Avoid upgrading an already up to date map.
            if (mapVersion.Major == currentVersion.Major && mapVersion.Minor == currentVersion.Minor)
            {
                Console.WriteLine($"Map is already the most recent version, {currentVersion}");
                return Task.CompletedTask;
            }

            // One day, when there are multiple versions, this will be a chain of upgrades, starting at the map's version and ending at the current version. This way, every old map version will be supported.
            // For now, just upgrade the map from 1.x to 2.0.
            map = V1_1ToV2Upgrader.Upgrade(map, filePath, silent);

            // Save the map.
            map.Save(filePath);
            return Task.CompletedTask;
        }
    }
}
