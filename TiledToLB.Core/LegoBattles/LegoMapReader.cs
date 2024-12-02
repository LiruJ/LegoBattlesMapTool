using ContentUnpacker.DataTypes;
using ContentUnpacker.Tilemaps;
using GlobalShared.DataTypes;
using GlobalShared.Tilemaps;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Numerics;
using TiledToLB.Core.Processors;
using TiledToLB.Core.Tiled.Map;
using TiledToLB.Core.Tiled.Property;
using TiledToLB.Core.Tiled.Tileset;

namespace TiledToLB.Core.LegoBattles
{
    public static class LegoMapReader
    {
        public static void CreateTiledMapFromLegoMap(Stream inputStream, Stream? blueprintStream, string workspacePath, string mapName, bool silent = true)
        {
            if (!silent)
                Console.WriteLine("Reading original map data");
            LegoTilemap legoMap = LegoTilemap.Load(inputStream, mapName);
            if (!silent)
                Console.WriteLine($"Read map data. Tileset: \"{legoMap.TilesetName}\" Size: {legoMap.Width}x{legoMap.Height}");

            // Create the map.
            TiledMap tiledMap = new(legoMap.Width, legoMap.Height)
            {
                TileWidth = 24,
                TileHeight = 16,
            };

            tiledMap.Properties.Add("Name", mapName);
            tiledMap.Properties.Add("Creator", "Hellbent Games");
            tiledMap.Properties.Add("ReplacesMPIndex", 0);
            tiledMap.Properties.Add("Tileset", legoMap.TilesetName);

            tiledMap.AddTileset($"../{CommonProcessor.TemplateTilesetsFolderName}/{legoMap.TilesetName}.tsx", 1);
            if (blueprintStream != null)
            {
                TilemapBlockPalette extraTiles = TilemapBlockPalette.LoadFromFile(blueprintStream);
                createAndAddExtraTileset(legoMap, tiledMap, extraTiles, workspacePath);
            }

            addTileData(legoMap, tiledMap);

            addEvents(legoMap, tiledMap);
            addTriggers(legoMap, tiledMap);
            addMarkers(legoMap, tiledMap);
            addMines(legoMap, tiledMap);

            // Save the file.
            string outputMapPath = Path.Combine(workspacePath, CommonProcessor.TemplateMapsFolderName, Path.ChangeExtension(Path.GetFileNameWithoutExtension(mapName), "tmx"));
            tiledMap.Save(outputMapPath);
        }

        private static void createAndAddExtraTileset(LegoTilemap legoMap, TiledMap tiledMap, TilemapBlockPalette extraTiles, string workspacePath)
        {
            // Create the tileset file, which describes how the big tiles are gained from the main image.
            TiledTileset extraTileset = new()
            {
                TileWidth = 24,
                TileHeight = 16,
                Columns = 20,
                Name = legoMap.MapName,
                TiledVersion = TiledMap.TargetTiledVersion,
                Version = TiledMap.TargetVersion,
            };
            extraTileset.Tiles.Capacity = extraTiles.Count;
            for (int i = 0; i < extraTiles.Count; i++)
            {
                TiledTilesetTile tile = new(i, "TypedTile");
                extraTileset.AddTile(tile);
            }

            // Go over the entire map and assign the tile type to the tiles. Basically, if a tile is found on the map that exists within the extra tileset, set its tile type to the one in the map data.
            for (int y = 0; y < legoMap.Height; y++)
                for (int x = 0; x < legoMap.Width; x++)
                {
                    TileData tileData = legoMap.TileData[(y * legoMap.Width) + x];
                    if (tileData.Index < TilemapBlockPalette.FactionPaletteCount)
                        continue;
                    byte treeData = legoMap.TreeSpriteData[(y * legoMap.Width) + x];

                    TiledTilesetTile tilesetTile = extraTileset.Tiles[tileData.Index - TilemapBlockPalette.FactionPaletteCount];
                    if (tileData.TileType != TileType.Grass && !tilesetTile.Properties.ContainsKey("Type"))
                        tilesetTile.Properties.Add(new TiledProperty("Type", ((int)tileData.TileType).ToString(), TiledPropertyType.Int, "TileType"));
                    if (treeData != 0 && !tilesetTile.Properties.ContainsKey("TreeData"))
                        tilesetTile.Properties.Add("TreeData", treeData);

                    extraTileset.Tiles[tileData.Index - TilemapBlockPalette.FactionPaletteCount] = tilesetTile;
                }

            // Create the tileset image.
            extraTileset.SourceImagePath = Path.ChangeExtension($"{legoMap.MapName}_{CommonProcessor.DetailTilesName}", "png");
            string tilesetImageOutputPath = Path.Combine(workspacePath, CommonProcessor.TemplateTileBlueprintsFolderName, extraTileset.SourceImagePath);
            string miniTilesInputPath = Path.Combine(workspacePath, CommonProcessor.TemplateTilesetsFolderName, Path.ChangeExtension(legoMap.TilesetName, "png"));
            (int sourceWidth, int sourceHeight) = createExtraTilesetImage(miniTilesInputPath, tilesetImageOutputPath, extraTiles);
            extraTileset.SourceImageWidth = sourceWidth;
            extraTileset.SourceImageHeight = sourceHeight;

            // Create the tileset tilemap, which helps users add to the detail tiles.
            createExtraTilesetTilemap(sourceWidth / 8, sourceHeight / 8, workspacePath, $"{legoMap.MapName}_{CommonProcessor.DetailTilesName}", legoMap.TilesetName, extraTiles);

            // Add the tileset reference to the map. Note that the path is relative to the map.
            string tilesetPath = $"../{CommonProcessor.TemplateTileBlueprintsFolderName}/{legoMap.MapName}_{CommonProcessor.DetailTilesName}.tsx";
            tiledMap.AddTileset(tilesetPath, TilemapBlockPalette.FactionPaletteCount + 1);
            extraTileset.Save(Path.Combine(workspacePath, CommonProcessor.TemplateMapsFolderName, tilesetPath));
        }

        private static Tuple<int, int> createExtraTilesetImage(string miniTilesInputPath, string outputFilePath, TilemapBlockPalette extraTiles)
        {
            byte sizeInTiles = (byte)MathF.Ceiling(MathF.Sqrt(extraTiles.Count));

            using Image<Rgba32> miniTilesImage = Image.Load<Rgba32>(miniTilesInputPath);
            using Image<Rgba32> tilesetImage = new(sizeInTiles * 24, sizeInTiles * 16);

            int currentBlockX = 0;
            int currentBlockY = 0;

            // Helpers for calculating source/dest.
            Point calculateDestination(int subTileX, int subTileY)
                => new((currentBlockX * 24) + (subTileX * 8), (currentBlockY * 16) + (subTileY * 8));
            Rectangle calculateSource(ushort subTileIndex)
                => new((subTileIndex * 8) % miniTilesImage.Width, ((subTileIndex * 8) / miniTilesImage.Width) * 8, 8, 8);

            // Save each block to the image.
            tilesetImage.Mutate(x =>
            {
                foreach (TilemapPaletteBlock block in extraTiles)
                {
                    x.DrawImage(miniTilesImage, calculateDestination(0, 0), calculateSource(block.TopLeft), PixelColorBlendingMode.Normal, PixelAlphaCompositionMode.Src, 1f);
                    x.DrawImage(miniTilesImage, calculateDestination(1, 0), calculateSource(block.TopMiddle), PixelColorBlendingMode.Normal, PixelAlphaCompositionMode.Src, 1f);
                    x.DrawImage(miniTilesImage, calculateDestination(2, 0), calculateSource(block.TopRight), PixelColorBlendingMode.Normal, PixelAlphaCompositionMode.Src, 1f);

                    x.DrawImage(miniTilesImage, calculateDestination(0, 1), calculateSource(block.BottomLeft), PixelColorBlendingMode.Normal, PixelAlphaCompositionMode.Src, 1f);
                    x.DrawImage(miniTilesImage, calculateDestination(1, 1), calculateSource(block.BottomMiddle), PixelColorBlendingMode.Normal, PixelAlphaCompositionMode.Src, 1f);
                    x.DrawImage(miniTilesImage, calculateDestination(2, 1), calculateSource(block.BottomRight), PixelColorBlendingMode.Normal, PixelAlphaCompositionMode.Src, 1f);

                    currentBlockX++;
                    if (currentBlockX >= sizeInTiles)
                    {
                        currentBlockX = 0;
                        currentBlockY++;
                    }
                }
            });

            // Save the tileset image and return its size.
            tilesetImage.SaveAsPng(outputFilePath);
            return new(tilesetImage.Width, tilesetImage.Height);
        }

        private static void createExtraTilesetTilemap(int widthInMiniTiles, int heightInMiniTiles, string workspacePath, string tilesetName, string baseTilesetName, TilemapBlockPalette extraTiles)
        {
            // Create the tileset tilemap.
            TiledMap extraTilesetTilemap = new(widthInMiniTiles, heightInMiniTiles)
            {
                TileWidth = 8,
                TileHeight = 8,
            };

            // Create the tile layer for the mini tiles.
            TiledMapTileLayer tilesLayer = new()
            {
                Name = "Mini Tiles",
                Width = widthInMiniTiles,
                Height = heightInMiniTiles,
                Data = new int[widthInMiniTiles, heightInMiniTiles],
            };

            // Add the mini tiles tileset to the tileset tilemap.
            string miniTilesetName = baseTilesetName[..baseTilesetName.IndexOf('T')] + "MiniTiles.tsx";
            extraTilesetTilemap.AddTileset($"../{CommonProcessor.TemplateTilesetsFolderName}/{miniTilesetName}", 1);

            // Add each tile to the tileset.
            int widthInTiles = widthInMiniTiles / 3;
            for (int i = 0; i < extraTiles.Count; i++)
            {
                // Get the block and calculate where it should start.
                TilemapPaletteBlock block = extraTiles[i];
                int miniTileX = (i % widthInTiles) * 3;
                int miniTileY = (i / widthInTiles) * 2;

                // Set the tiles.
                tilesLayer.Data[miniTileX, miniTileY] = block.TopLeft + 1;
                tilesLayer.Data[miniTileX + 1, miniTileY] = block.TopMiddle + 1;
                tilesLayer.Data[miniTileX + 2, miniTileY] = block.TopRight + 1;

                tilesLayer.Data[miniTileX, miniTileY + 1] = block.BottomLeft + 1;
                tilesLayer.Data[miniTileX + 1, miniTileY + 1] = block.BottomMiddle + 1;
                tilesLayer.Data[miniTileX + 2, miniTileY + 1] = block.BottomRight + 1;
            }

            // Add the tile layer, and save the map.
            extraTilesetTilemap.AddTileLayer(tilesLayer);
            extraTilesetTilemap.Save(Path.Combine(workspacePath, CommonProcessor.TemplateTileBlueprintsFolderName, Path.ChangeExtension(tilesetName, "tmx")));
        }

        private static void addTileData(LegoTilemap legoMap, TiledMap tiledMap)
        {
            TiledMapTileLayer detailsLayer = new()
            {
                Name = "Details",
                Width = legoMap.Width,
                Height = legoMap.Height,
                Data = new int[legoMap.Width, legoMap.Height],
            };
            TiledMapTileLayer treesLayer = new()
            {
                Name = "Trees",
                Width = legoMap.Width,
                Height = legoMap.Height,
                Opacity = 0.5f,
                Data = new int[legoMap.Width, legoMap.Height],
            };

            for (int y = 0; y < legoMap.Height; y++)
                for (int x = 0; x < legoMap.Width; x++)
                {
                    detailsLayer.Data[x, y] = legoMap.TileData[(y * legoMap.Width) + x].Index + 1;
                    treesLayer.Data[x, y] = legoMap.TileData[(y * legoMap.Width) + x].HasTree ? 7 : 0;
                }

            tiledMap.AddTileLayer(detailsLayer);
            tiledMap.AddTileLayer(treesLayer);
        }

        private static void addEvents(LegoTilemap legoMap, TiledMap tiledMap)
        {
            TiledMapObjectGroup patrolPointsGroup = new("Patrol Points", true);
            TiledMapObjectGroup cameraBoundsGroup = new("Camera Bounds", false);
            TiledMapObjectGroup entitiesGroup = new("Entities", true);
            TiledMapObjectGroup pickupGroup = new("Pickups", true);
            TiledMapObjectGroup wallsGroup = new("Walls", true);

            // Go over each event group with pickups.
            int sortKey = 0;

            // Go over each event within the current group, then each pickup within that group.
            foreach (EventData eventData in legoMap.EventSections)
            {
                // Patrol points.
                int? patrolPointID = null;
                if (eventData.PatrolPoints.Count > 0)
                {
                    Vector2 origin = new(eventData.PatrolPoints[0].X, eventData.PatrolPoints[0].Y);
                    TiledMapObject patrolPointObject = new()
                    {
                        ID = tiledMap.NextObjectID,
                        X = (origin.X * 24f) + 12f,
                        Y = (origin.Y * 16f) + 8f,
                        Width = null,
                        Height = null,
                    };
                    tiledMap.NextObjectID++;
                    patrolPointID = patrolPointObject.ID;

                    patrolPointObject.Properties.Add("EventID", eventData.ID);
                    patrolPointObject.Properties.Add("SortKey", sortKey);

                    foreach (Vector2U8 point in eventData.PatrolPoints)
                    {
                        Vector2 position = new(((point.X - origin.X) * 24f), ((point.Y - origin.Y) * 16f));
                        patrolPointObject.AddPolylinePoint(position.X, position.Y);
                    }

                    patrolPointsGroup.Objects.Add(patrolPointObject);
                }

                // Camera bounds.
                if (eventData.CameraBounds != null)
                {
                    TiledMapObject cameraBoundsObject = new()
                    {
                        ID = tiledMap.NextObjectID,
                        X = eventData.CameraBounds.Value.MinX * 24f,
                        Y = eventData.CameraBounds.Value.MinY * 16f,
                        Width = ((eventData.CameraBounds.Value.MaxX + 1) - eventData.CameraBounds.Value.MinX) * 24f,
                        Height = ((eventData.CameraBounds.Value.MaxY + 1) - eventData.CameraBounds.Value.MinY) * 16f,
                    };

                    cameraBoundsObject.Properties.Add("EventID", eventData.ID);
                    cameraBoundsObject.Properties.Add("SortKey", sortKey);

                    cameraBoundsGroup.Objects.Add(cameraBoundsObject);
                }

                // Entities.
                foreach (TilemapEntityData entityData in eventData.EntityData)
                {
                    TiledMapObject entityObject = entityData.ToTiledMapObject(tiledMap, eventData.ID, sortKey);

                    entityObject.Name = entityData.TypeIndex.ToString();
                    entityObject.Properties.Add(new TiledProperty("TeamIndex", entityData.TeamIndex.ToString(), TiledPropertyType.Int, null));
                    entityObject.Properties.Add(new TiledProperty("StartHealth", (entityData.HealthPercent / 100f).ToString(), TiledPropertyType.Float, null));

                    if (patrolPointID != null)
                        entityObject.Properties.Add(new TiledProperty("PatrolPoint", patrolPointID.Value.ToString(), TiledPropertyType.Object, null));

                    switch (entityData.TypeIndex)
                    {
                        case EntityType.Base:
                            entityObject.X -= 12f;
                            entityObject.Y -= 8f;
                            entityObject.Width = 24 * 3;
                            entityObject.Height = 16 * 3;
                            break;
                        case EntityType.Harvester:
                        case EntityType.Mine:
                        case EntityType.Farm:
                        case EntityType.Barracks:
                        case EntityType.SpecialFactory:
                        case EntityType.Shipyard:
                            entityObject.X -= 12f;
                            entityObject.Y -= 8f;
                            entityObject.Width = 24 * 2;
                            entityObject.Height = 16 * 2;
                            break;
                        case EntityType.Gate:
                        case EntityType.Wall:
                        case EntityType.Tower:
                        case EntityType.Tower2:
                        case EntityType.Tower3:
                            entityObject.X -= 12f;
                            entityObject.Y -= 8f;
                            entityObject.Width = 24;
                            entityObject.Height = 16;
                            break;
                        case EntityType.Bridge:
                        case EntityType.Pickup:
                        case EntityType.Hero:
                        case EntityType.Builder:
                        case EntityType.Melee:
                        case EntityType.Ranged:
                        case EntityType.Mounted:
                        case EntityType.Transport:
                        case EntityType.Special:
                        default:
                            break;
                    }

                    entitiesGroup.Objects.Add(entityObject);
                }

                // Pickups.
                foreach (TilemapEntityData pickupData in eventData.PickupData)
                {
                    TiledMapObject pickupObject = pickupData.ToTiledMapObject(tiledMap, eventData.ID, sortKey);
                    pickupObject.Name = pickupData.SubTypeIndex switch
                    {
                        5 => "Red Brick",
                        6 => "Minikit",
                        7 => "Blue Stud",
                        8 => "Golden Brick",
                        _ => "Mission Pickup",
                    };
                    pickupGroup.Objects.Add(pickupObject);
                }

                // Walls.
                foreach (Tuple<Vector2U8, byte> wallData in eventData.Walls)
                {
                    TiledMapObject wallObject = new()
                    {
                        ID = tiledMap.NextObjectID,
                        X = wallData.Item1.X * 24f,
                        Y = wallData.Item1.Y * 16f,
                        Width = 24f,
                        Height = 16f,
                    };
                    tiledMap.NextObjectID++;

                    wallObject.Properties.Add("TeamIndex", wallData.Item2);
                    wallObject.Properties.Add("EventID", eventData.ID);
                    wallObject.Properties.Add("SortKey", sortKey);

                    wallsGroup.Objects.Add(wallObject);
                }

                sortKey++;
            }

            tiledMap.AddObjectGroup(patrolPointsGroup);
            tiledMap.AddObjectGroup(cameraBoundsGroup);
            tiledMap.AddObjectGroup(entitiesGroup);
            tiledMap.AddObjectGroup(pickupGroup);
            tiledMap.AddObjectGroup(wallsGroup);
        }

        private static void addTriggers(LegoTilemap legoMap, TiledMap tiledMap)
        {
            TiledMapObjectGroup triggerGroup = new("Triggers", false);

            int sortKey = 0;
            foreach (TriggerData triggerData in legoMap.TriggerSections)
            {
                TiledMapObject areaObject = new()
                {
                    ID = tiledMap.NextObjectID,
                    X = triggerData.Area.MinX * 24f,
                    Y = triggerData.Area.MinY * 16f,
                    Width = ((triggerData.Area.MaxX + 1) - triggerData.Area.MinX) * 24f,
                    Height = ((triggerData.Area.MaxY + 1) - triggerData.Area.MinY) * 16f,
                };
                tiledMap.NextObjectID++;

                areaObject.Properties.Add("TriggerID", triggerData.ID);
                areaObject.Properties.Add("SortKey", sortKey);

                areaObject.Properties.Add("HasData", triggerData.HasData);
                areaObject.Properties.Add("TargetUnitIndex", triggerData.TargetUnitIndex);
                areaObject.Properties.Add("TargetUnitType", (int)triggerData.TargetUnitType);
                areaObject.Properties.Add("TargetFactionIndex", triggerData.TargetFactionIndex);
                areaObject.Properties.Add("TargetTeam", triggerData.TargetTeam);

                areaObject.Properties.Add("Unknown1", triggerData.Unknown1);
                areaObject.Properties.Add("Unknown2", triggerData.Unknown2);

                triggerGroup.Objects.Add(areaObject);
                sortKey++;
            }

            tiledMap.AddObjectGroup(triggerGroup);
        }

        private static void addMarkers(LegoTilemap legoMap, TiledMap tiledMap)
        {
            TiledMapObjectGroup markersGroup = new("Markers", true);

            int sortKey = 0;
            foreach ((byte markerID, List<MarkerData> markers) in legoMap.MarkerSections)
            {
                foreach (MarkerData markerData in markers)
                {
                    TiledMapObject markerObject = new()
                    {
                        ID = tiledMap.NextObjectID,
                        X = (markerData.Position.X * 24f) + 12f,
                        Y = (markerData.Position.Y * 16f) + 8f,
                        Width = null,
                        Height = null,
                    };
                    tiledMap.NextObjectID++;

                    markerObject.Properties.Add("SortKey", sortKey);
                    markerObject.Properties.Add("MarkerID", markerID);

                    markerObject.Properties.Add("UnknownBool", markerData.UnknownBool);

                    markersGroup.Objects.Add(markerObject);
                }
                sortKey++;
            }

            tiledMap.AddObjectGroup(markersGroup);
        }

        private static void addMines(LegoTilemap legoMap, TiledMap tiledMap)
        {
            TiledMapObjectGroup minesGroup = new("Mines", true);

            int sortKey = 0;
            foreach (List<Vector2U8> mines in legoMap.Mines)
            {
                foreach (Vector2U8 minePosition in mines)
                {
                    TiledMapObject mineObject = new()
                    {
                        ID = tiledMap.NextObjectID,
                        X = minePosition.X * 24f,
                        Y = minePosition.Y * 16f,
                        Width = 24f * 2,
                        Height = 16f * 2,
                    };
                    tiledMap.NextObjectID++;

                    mineObject.Properties.Add("SortKey", sortKey);

                    minesGroup.Objects.Add(mineObject);
                }
                sortKey++;
            }

            tiledMap.AddObjectGroup(minesGroup);
        }

        private static TiledMapObject ToTiledMapObject(this TilemapEntityData entityData, TiledMap tiledMap, int eventID, int sortKey)
        {
            TiledMapObject entityObject = new()
            {
                ID = tiledMap.NextObjectID,
                X = (entityData.X * 24f) + 12f,
                Y = (entityData.Y * 16f) + 8f,
                Width = null,
                Height = null,
                Type = "Entity",
            };
            tiledMap.NextObjectID++;

            entityObject.Properties.Add("EventID", eventID);
            entityObject.Properties.Add("SortKey", sortKey);
            entityObject.Properties.Add(new TiledProperty("Type", ((int)entityData.TypeIndex).ToString(), TiledPropertyType.Int, "EntityType"));
            entityObject.Properties.Add("SubType", entityData.SubTypeIndex);

            for (int i = 0; i < entityData.ExtraData.Length; i++)
                entityObject.Properties.Add($"ExtraData{i}", entityData.ExtraData[i]);

            return entityObject;
        }
    }
}
