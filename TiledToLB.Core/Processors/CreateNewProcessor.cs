using GlobalShared.DataTypes;
using GlobalShared.Tilemaps;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TiledToLB.Core.LegoBattles;
using TiledToLB.Core.Tiled.Map;
using TiledToLB.Core.Tiled.Tileset;

namespace TiledToLB.Core.Processors
{
    public static class CreateNewProcessor
    {
        public static Task CreateNewAsync(string workspacePath, string mapName, string tilesetName, string creatorName, bool silent)
        {
            // Normalise the tileset name and ensure it's valid.
            if (!normaliseTilesetName(ref tilesetName))
            {
                Console.WriteLine("Invalid tileset!");
                return Task.CompletedTask;
            }

            // Create the map and add the layers.
            TiledMap tiledMap = new(64, 64, 24, 16);
            createTilesets(tiledMap, workspacePath, mapName, tilesetName);
            setProperties(tiledMap, mapName, tilesetName, creatorName);
            createTerrainLayers(tiledMap);
            createEntityLayers(tiledMap);

            // Save the map.
            string mapOutputFilePath = Path.Combine(workspacePath, CommonProcessor.TemplateMapsFolderName, Path.ChangeExtension(mapName, "tmx"));
            tiledMap.Save(mapOutputFilePath);
            return Task.CompletedTask;
        }

        private static void setProperties(TiledMap tiledMap, string mapName, string tilesetName, string creatorName)
        {
            tiledMap.Properties.Add("Name", Path.GetFileNameWithoutExtension(mapName));
            tiledMap.Properties.Add("ToolVersion", typeof(CreateNewProcessor).Assembly.GetName().Version?.ToString() ?? "0.0.0");
            tiledMap.Properties.Add("Creator", string.IsNullOrWhiteSpace(creatorName) ? Environment.UserName : creatorName);
            tiledMap.Properties.Add("ReplacesMPIndex", 0);
            tiledMap.Properties.Add("Tileset", tilesetName);
        }

        private static void createTilesets(TiledMap tiledMap, string workspacePath, string mapName, string tilesetName)
        {
            string extraTilesetName = $"{CommonProcessor.DetailTilesName}_{mapName}";

            // Add the tilesets to the map.
            tiledMap.AddTileset($"../{CommonProcessor.TemplateTilesetsFolderName}/{tilesetName}.tsx", 1);
            tiledMap.AddTileset($"../{CommonProcessor.TemplateTileBlueprintsFolderName}/{extraTilesetName}.tsx", TilemapBlockPalette.FactionPaletteCount + 1);

            // Create empty tileset files.
            TiledTileset tileset = new();
            tileset.Save(Path.Combine(workspacePath, CommonProcessor.TemplateTileBlueprintsFolderName, $"{extraTilesetName}.tsx"));

            const int widthInTiles = 16;
            const int heightInTiles = 8;

            TiledMap tilesetMap = new(widthInTiles * 3, heightInTiles * 2, 8, 8);
            tilesetMap.AddTileLayer("Mini Tiles");
            tilesetMap.Save(Path.Combine(workspacePath, CommonProcessor.TemplateTileBlueprintsFolderName, $"{extraTilesetName}.tmx"));

            // Add the mini tiles tileset to the tileset tilemap.
            string miniTilesetName = tilesetName[..tilesetName.IndexOf('T')] + "MiniTiles.tsx";
            tilesetMap.AddTileset($"../{CommonProcessor.TemplateTilesetsFolderName}/{miniTilesetName}", 1);

            using Image<Rgba32> tilesetImage = new(widthInTiles * 24, heightInTiles * 16, new Rgba32(0, 0, 0, 0));
            tilesetImage.Save(Path.Combine(workspacePath, CommonProcessor.TemplateTileBlueprintsFolderName, $"{extraTilesetName}.png"));
        }

        private static void createTerrainLayers(TiledMap tiledMap)
        {
            // Create the layers.
            TiledMapTileLayer detailsLayer = tiledMap.AddTileLayer("Details");
            TiledMapTileLayer treesLayer = tiledMap.AddTileLayer("Trees");

            // Set the terrain to plain grass and add the trees.
            for (int y = 0; y < tiledMap.Height; y++)
                for (int x = 0; x < tiledMap.Width; x++)
                {
                    detailsLayer.Data[x, y] = 117;

                    treesLayer.Data[x, y] = (x < 2 || x >= tiledMap.Width - 2 || y < 2 || y >= tiledMap.Height - 2)
                        ? 7
                        : 0;
                }

            // Create the mine spot.
            detailsLayer.Data[31, 31] = 14;
            detailsLayer.Data[32, 31] = 15;
            detailsLayer.Data[31, 32] = 19;
            detailsLayer.Data[32, 32] = 20;
        }

        private static void createEntityLayers(TiledMap tiledMap)
        {
            TiledMapObjectGroup patrolPointsGroup = tiledMap.AddObjectGroup("Patrol Points");
            TiledMapObjectGroup cameraBoundsGroup = tiledMap.AddObjectGroup("Camera Bounds", false);
            TiledMapObjectGroup entitiesGroup = tiledMap.AddObjectGroup("Entities");
            TiledMapObjectGroup pickupGroup = tiledMap.AddObjectGroup("Pickups");
            TiledMapObjectGroup wallsGroup = tiledMap.AddObjectGroup("Walls");
            TiledMapObjectGroup triggerGroup = tiledMap.AddObjectGroup("Triggers", false);
            TiledMapObjectGroup markersGroup = tiledMap.AddObjectGroup("Markers", false);
            TiledMapObjectGroup minesGroup = tiledMap.AddObjectGroup("Mines");

            TiledMapObject mine = tiledMap.CreateObject(minesGroup);
            mine.SetPositionTopLeftPoint(31, 31);

            // Create the bases.
            createBase(tiledMap, entitiesGroup, 4, 3, 0, 7, 0, 0);
            createBase(tiledMap, entitiesGroup, 57, 3, 0, 7, 1, 2);
            createBase(tiledMap, entitiesGroup, 4, 55, 0, -3, 2, 1);
            createBase(tiledMap, entitiesGroup, 57, 55, 0, -3, 3, 3);

            // Create the golden bricks.
            createGoldenBrick(tiledMap, pickupGroup, 60, 31, 4);
            createGoldenBrick(tiledMap, pickupGroup, 3, 31, 4);
            createGoldenBrick(tiledMap, pickupGroup, 31, 3, 4);
            createGoldenBrick(tiledMap, pickupGroup, 31, 60, 4);
        }

        private static void createGoldenBrick(TiledMap tiledMap, TiledMapObjectGroup pickupGroup, int x, int y, int sortKey)
        {
            TiledMapObject pickup = tiledMap.CreateObject(pickupGroup);
            pickup.SetPositionCentredPoint(x, y);
            pickup.Type = "Entity";

            pickup.Name = Helpers.CalculateName(EntityType.Pickup, 8);
            pickup.SetEventIDAndSortKey(0, sortKey);
            pickup.SetEntityType(EntityType.Pickup, 8);
        }

        private static void createBase(TiledMap tiledMap, TiledMapObjectGroup entitiesGroup, int startX, int startY, int unitsOffsetX, int unitsOffsetY, int sortKey, int teamIndex)
        {
            TiledMapObject createObject(int x, int y, EntityType entityType)
            {
                TiledMapObject entityObject = tiledMap.CreateObject(entitiesGroup);
                entityObject.Type = "Entity";

                entityObject.SetPositionTopLeftPoint(startX, startY);
                (int offsetX, int offsetY, int width, int height) = Helpers.CalculateOffsetAndSize(entityType);
                entityObject.X = (x * 24) + offsetX;
                entityObject.Y = (y * 16) + offsetY;
                entityObject.Width = width;
                entityObject.Height = height;

                entityObject.Name = Helpers.CalculateName(entityType, 0);
                entityObject.SetSortKey(sortKey);
                entityObject.SetEntityType(entityType);
                entityObject.SetTeamIndex(teamIndex);
                entityObject.SetExtraData(1, 1, 1);

                return entityObject;
            }

            // Create the base and farms.
            createObject(startX, startY, EntityType.Base);
            createObject(startX - 1, startY + 4, EntityType.Barracks);
            createObject(startX + 2, startY + 4, EntityType.Farm);

            // Create the hero and two builders.
            createObject((startX + unitsOffsetX) + 1, startY + unitsOffsetY, EntityType.Hero);
            createObject(startX + unitsOffsetX, (startY + unitsOffsetY) + 1, EntityType.Builder);
            createObject((startX + unitsOffsetX) + 2, (startY + unitsOffsetY) + 1, EntityType.Builder);
        }

        private static bool normaliseTilesetName(ref string tilesetName)
        {
            tilesetName = tilesetName.ToLower();
            switch (tilesetName)
            {
                case "k":
                case "king":
                case "kingtileset":
                case "kingtiles":
                    tilesetName = "KingTileset";
                    break;
                case "m":
                case "mars":
                case "marstileset":
                case "marstiles":
                    tilesetName = "MarsTileset";
                    break;
                case "p":
                case "pirate":
                case "piratetileset":
                case "piratetiles":
                    tilesetName = "PirateTileset";
                    break;
                default:
                    return false;
            }
            return true;
        }
    }
}
