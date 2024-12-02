using GlobalShared.Tilemaps;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiledToLB.Core.Tiled.Map;
using TiledToLB.Core.Tiled.Tileset;
using System.Globalization;
using TiledToLB.Core.Tiled.Property;
using System.Runtime.InteropServices;
using GlobalShared.DataTypes;
using System.Reflection.Metadata.Ecma335;

namespace TiledToLB.Core.Processors
{
    public static class CreateNewProcessor
    {
        public static async Task CreateNewAsync(string outputDirectoryPath, string mapName, string tilesetName, string creatorName, bool silent)
        {
            // Normalise the tileset name and ensure it's valid.
            if (!normaliseTilesetName(ref tilesetName))
            {
                Console.WriteLine("Invalid tileset!");
                return;
            }

            // Create the map and add the layers.
            TiledMap tiledMap = new(64, 64, 24, 16);
            createTilesets(tiledMap, outputDirectoryPath, mapName, tilesetName);
            setProperties(tiledMap, mapName, tilesetName, creatorName);
            createTerrainLayers(tiledMap);
            createEntityLayers(tiledMap);

            // Save the map.
            string mapOutputFilePath = Path.Combine(outputDirectoryPath, CommonProcessor.TemplateMapsFolderName, Path.ChangeExtension(mapName, "tmx"));
            tiledMap.Save(mapOutputFilePath);
        }

        private static void setProperties(TiledMap tiledMap, string mapName, string tilesetName, string creatorName)
        {
            tiledMap.Properties.Add("Name", Path.GetFileNameWithoutExtension(mapName));
            tiledMap.Properties.Add("Creator", string.IsNullOrWhiteSpace(creatorName) ? Environment.UserName : creatorName);
            tiledMap.Properties.Add("ReplacesMPIndex", 0);
            tiledMap.Properties.Add("Tileset", tilesetName);
        }

        private static void createTilesets(TiledMap tiledMap, string outputDirectoryPath, string mapName, string tilesetName)
        {
            string extraTilesetName = $"{CommonProcessor.DetailTilesName}_{mapName}";

            // Add the tilesets to the map.
            tiledMap.AddTileset($"../{CommonProcessor.TemplateTilesetsFolderName}/{tilesetName}.tsx", 1);
            tiledMap.AddTileset($"../{CommonProcessor.TemplateTileBlueprintsFolderName}/{extraTilesetName}.tsx", TilemapBlockPalette.FactionPaletteCount + 1);

            // Create empty tileset files.
            TiledTileset tileset = new();
            tileset.Save(Path.Combine(outputDirectoryPath, CommonProcessor.TemplateTileBlueprintsFolderName, $"{extraTilesetName}.tsx"));

            const int widthInTiles = 16;
            const int heightInTiles = 8;

            TiledMap tilesetMap = new(widthInTiles * 3, heightInTiles * 2, 8, 8);
            tilesetMap.AddTileLayer("Mini Tiles");
            tilesetMap.Save(Path.Combine(outputDirectoryPath, CommonProcessor.TemplateTileBlueprintsFolderName, $"{extraTilesetName}.tmx"));

            // Add the mini tiles tileset to the tileset tilemap.
            string miniTilesetName = tilesetName[..tilesetName.IndexOf('T')] + "MiniTiles.tsx";
            tilesetMap.AddTileset($"../{CommonProcessor.TemplateTilesetsFolderName}/{miniTilesetName}", 1);

            using Image<Rgba32> tilesetImage = new(widthInTiles * 24, heightInTiles * 16, new Rgba32(0, 0, 0, 0));
            tilesetImage.Save(Path.Combine(outputDirectoryPath, CommonProcessor.TemplateTileBlueprintsFolderName, $"{extraTilesetName}.png"));
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

            TiledMapObject mine = new()
            {
                X = 31 * 24,
                Y = 31 * 16,
                Width = 2 * 24,
                Height = 2 * 16,
                ID = tiledMap.NextObjectID
            };
            tiledMap.NextObjectID++;
            minesGroup.Objects.Add(mine);

            // Create the bases.
            createBase(tiledMap, entitiesGroup, 4, 3, 0, 0);
            createBase(tiledMap, entitiesGroup, 57, 3, 1, 2);
            createBase(tiledMap, entitiesGroup, 4, 55, 2, 1);
            createBase(tiledMap, entitiesGroup, 57, 55, 3, 3);

            // Create the golden bricks.
            createGoldenBrick(tiledMap, pickupGroup, 60, 31, 4);
            createGoldenBrick(tiledMap, pickupGroup, 3, 31, 4);
            createGoldenBrick(tiledMap, pickupGroup, 31, 3, 4);
            createGoldenBrick(tiledMap, pickupGroup, 31, 60, 4);
        }

        private static void createGoldenBrick(TiledMap tiledMap, TiledMapObjectGroup pickupGroup, int x, int y, int sortKey)
        {
            TiledMapObject pickup = new()
            {
                X = (x * 24) + 12,
                Y = (y * 16) + 8,
                ID = tiledMap.NextObjectID,
                Type = "Entity",
            };
            tiledMap.NextObjectID++;
            pickupGroup.Objects.Add(pickup);

            pickup.Properties.Add("EventID", 0);
            pickup.Properties.Add("SortKey", sortKey);
            pickup.Properties.Add(new TiledProperty("Type", ((int)EntityType.Pickup).ToString(), TiledPropertyType.Int, "EntityType"));
            pickup.Properties.Add("SubType", 8);
        }

        private static void createBase(TiledMap tiledMap, TiledMapObjectGroup entitiesGroup, int startX, int startY, int sortKey, int teamIndex)
        {
            TiledMapObject createObject(int x, int y, int width, int height, EntityType entityType)
            {
                TiledMapObject entityObject = new()
                {
                    X = x * 24,
                    Y = y * 16,
                    Width = width * 24,
                    Height = height * 16,
                    ID = tiledMap.NextObjectID,
                    Type = "Entity",
                };
                entityObject.Properties.Add("EventID", 0);
                entityObject.Properties.Add("SortKey", sortKey);
                entityObject.Properties.Add(new TiledProperty("Type", ((int)entityType).ToString(), TiledPropertyType.Int, "EntityType"));
                entityObject.Properties.Add("SubType", 0);
                entityObject.Properties.Add("TeamIndex", teamIndex);
                tiledMap.NextObjectID++;
                entitiesGroup.Objects.Add(entityObject);

                return entityObject;
            }

            createObject(startX, startY, 3, 3, EntityType.Base);
            createObject(startX - 1, startY + 4, 2, 2, EntityType.Farm);
            createObject(startX + 2, startY + 4, 2, 2, EntityType.Farm);
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
