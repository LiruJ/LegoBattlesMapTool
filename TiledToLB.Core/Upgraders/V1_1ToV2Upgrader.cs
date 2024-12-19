using GlobalShared.DataTypes;
using TiledToLB.Core.LegoBattles;
using TiledToLB.Core.Tiled.Map;
using TiledToLB.Core.Tiled.Property;

namespace TiledToLB.Core.Upgraders
{
    /// <summary>
    /// Upgrader for all maps before the major overhaul. These maps are very simple, with no support for campaign map logic.
    /// </summary>
    public static class V1_1ToV2Upgrader
    {
        public static Version TargetVersion { get; } = new Version(2, 0, 0);

        public static TiledMap Upgrade(TiledMap map, string filePath, bool silent)
        {
            string mapName = Path.GetFileNameWithoutExtension(filePath);
            upgradeMapProperties(map, mapName);

            addMissingLayers(map);
            upgradeEntities(map);
            upgradeMarkers(map);
            upgradeMines(map);

            if (!silent)
                Console.WriteLine($"Upgraded to version {TargetVersion}");
            return map;
        }

        private static void upgradeMapProperties(TiledMap map, string mapName)
        {
            // Always upgrade the version.
            map.Properties.Set("ToolVersion", TargetVersion.ToString());

            // Add any properties that don't exist.
            if (!map.Properties.ContainsKey("Name"))
                map.Properties.Add("Name", mapName);
            if (!map.Properties.ContainsKey("Creator"))
                map.Properties.Add("Creator", Environment.UserName);
            if (!map.Properties.ContainsKey("ReplacesMPIndex"))
                map.Properties.Add("ReplacesMPIndex", 0);
            if (!map.Properties.ContainsKey("Tileset"))
                map.Properties.Add("Tileset", "KingTileset");
        }

        private static void addMissingLayers(TiledMap map)
        {
            TiledMapObjectGroup patrolPointsGroup = map.AddObjectGroup("Patrol Points");
            TiledMapObjectGroup cameraBoundsGroup = map.AddObjectGroup("Camera Bounds", false);
            TiledMapObjectGroup pickupGroup = map.AddObjectGroup("Pickups");
            TiledMapObjectGroup wallsGroup = map.AddObjectGroup("Walls");
            TiledMapObjectGroup triggerGroup = map.AddObjectGroup("Triggers", false);
        }

        private static void upgradeEntities(TiledMap map)
        {
            // Version 1.x had only layers for entities, which included pickups. However, there was a separate layer for bridges.
            if (!map.ObjectGroups.TryGetValue("Entities", out TiledMapObjectGroup? entitiesGroup))
                throw new InvalidDataException("Map was missing entities layer!");
            entitiesGroup.Visible = true;

            TiledMapObjectGroup wallsGroup = map.ObjectGroups["Walls"];
            TiledMapObjectGroup pickupGroup = map.ObjectGroups["Pickups"];

            for (int i = entitiesGroup.Objects.Count - 1; i >= 0; i--)
            {
                TiledMapObject entityObject = entitiesGroup.Objects[i];
                entityObject.Name = Helpers.CalculateName(entityObject);
                entityObject.Type = "Entity";

                EntityType entityType = entityObject.GetEntityType(EntityType.Hero);
                entityObject.SetEntityType(entityType);

                switch (entityType)
                {
                    case EntityType.Hero:
                    case EntityType.Builder:
                    case EntityType.Melee:
                    case EntityType.Ranged:
                    case EntityType.Mounted:
                    case EntityType.Transport:
                    case EntityType.Special:
                    case EntityType.Base:
                    case EntityType.Harvester:
                    case EntityType.Mine:
                    case EntityType.Tower:
                    case EntityType.Tower2:
                    case EntityType.Tower3:
                    case EntityType.Barracks:
                    case EntityType.Farm:
                    case EntityType.SpecialFactory:
                    case EntityType.Shipyard:
                        entityObject.Properties.Set("ExtraData0", 1);
                        entityObject.Properties.Set("ExtraData1", 1);
                        entityObject.Properties.Set("ExtraData2", 1);
                        break;
                    case EntityType.Bridge:
                    case EntityType.Gate:
                    default:
                        break;
                    case EntityType.Wall:
                        wallsGroup.Objects.Add(entityObject);
                        entitiesGroup.Objects.RemoveAt(i);
                        break;
                    case EntityType.Pickup:
                        entityObject.Name = "Golden Brick";
                        entityObject.Properties.Set("SubType", 8);
                        entityObject.Properties.Set("ExtraData0", 2);
                        entityObject.Properties.Set("ExtraData1", 1);
                        entityObject.Properties.Set("ExtraData2", 0);

                        pickupGroup.Objects.Add(entityObject);
                        entitiesGroup.Objects.RemoveAt(i);
                        break;
                }

            }
        }

        private static void upgradeMarkers(TiledMap map)
        {
            if (!map.ObjectGroups.TryGetValue("Markers", out TiledMapObjectGroup? markersGroup))
                throw new InvalidDataException("Map was missing markers layer!");
            TiledMapObjectGroup? bridgesGroup = map.ObjectGroups.Values.FirstOrDefault(x => x.Name == "Bridges");

            // Get the bridges into the main markers layer.
            if (bridgesGroup != null)
            {
                foreach (TiledMapObject bridgeObject in bridgesGroup.Objects)
                {
                    bool isHorizontal = bridgeObject.Properties.TryGetValue("IsHorizontal", out TiledProperty horizontalProperty) && bool.TryParse(horizontalProperty.Value, out isHorizontal) && isHorizontal;

                    bridgeObject.SetMarkerIDAndSortKey(isHorizontal ? 7 : 8, 0);
                    bridgeObject.Properties.Set("UnknownBool", false);
                    bridgeObject.Properties.Remove("IsHorizontal");

                    markersGroup.Objects.Add(bridgeObject);
                }

                map.ObjectGroups.Remove("Bridges");
                bridgesGroup = null;
            }

            foreach (TiledMapObject markerObject in markersGroup.Objects)
                markerObject.Type = "Marker";
        }

        private static void upgradeMines(TiledMap map)
        {
            TiledMapObjectGroup minesGroup = map.ObjectGroups["Mines"];

            foreach (TiledMapObject mineObject in minesGroup.Objects)
                mineObject.SetSizeFromTiles(2, 2);
        }
    }
}
