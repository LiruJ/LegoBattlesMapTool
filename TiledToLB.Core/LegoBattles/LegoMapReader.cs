using ContentUnpacker.DataTypes;
using ContentUnpacker.Tilemaps;
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
            TiledMap tiledMap = new(legoMap.Width, legoMap.Height, 24, 16);

            tiledMap.Properties.Add("Name", mapName);
            tiledMap.Properties.Add("ToolVersion", typeof(LegoMapReader).Assembly.GetName().Version?.ToString() ?? "0.0.0");
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
            TiledMap extraTilesetTilemap = new(widthInMiniTiles, heightInMiniTiles, 8, 8);

            // Create the tile layer for the mini tiles.
            TiledMapTileLayer tilesLayer = extraTilesetTilemap.AddTileLayer("Mini Tiles");

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

            // Save the map.
            extraTilesetTilemap.Save(Path.Combine(workspacePath, CommonProcessor.TemplateTileBlueprintsFolderName, Path.ChangeExtension(tilesetName, "tmx")));
        }

        private static void addTileData(LegoTilemap legoMap, TiledMap tiledMap)
        {
            TiledMapTileLayer detailsLayer = tiledMap.AddTileLayer("Details");
            TiledMapTileLayer treesLayer = tiledMap.AddTileLayer("Trees");
            treesLayer.Opacity = 0.5f;

            for (int y = 0; y < legoMap.Height; y++)
                for (int x = 0; x < legoMap.Width; x++)
                {
                    detailsLayer.Data[x, y] = legoMap.TileData[(y * legoMap.Width) + x].Index + 1;
                    treesLayer.Data[x, y] = legoMap.TileData[(y * legoMap.Width) + x].HasTree ? 7 : 0;
                }
        }

        private static void addEvents(LegoTilemap legoMap, TiledMap tiledMap)
        {
            // Create the layers.
            TiledMapObjectGroup patrolPointsGroup = tiledMap.AddObjectGroup("Patrol Points");
            TiledMapObjectGroup cameraBoundsGroup = tiledMap.AddObjectGroup("Camera Bounds", false);
            TiledMapObjectGroup entitiesGroup = tiledMap.AddObjectGroup("Entities");
            TiledMapObjectGroup pickupGroup = tiledMap.AddObjectGroup("Pickups");
            TiledMapObjectGroup wallsGroup = tiledMap.AddObjectGroup("Walls");

            // Go over each event group with pickups.
            int sortKey = 0;

            // Go over each event within the current group, then each pickup within that group.
            foreach (EventData eventData in legoMap.EventSections)
            {
                // Patrol points.
                int? patrolPointID = null;
                if (eventData.PatrolPoints.Count > 0)
                {
                    TiledMapObject patrolPointObject = tiledMap.CreateObject(patrolPointsGroup);
                    patrolPointObject.SetPositionCentredPoint(eventData.PatrolPoints[0]);
                    patrolPointObject.SetEventIDAndSortKey(eventData.ID, sortKey);

                    // Save the patrol point ID so the other events can link to it.
                    patrolPointID = patrolPointObject.ID;

                    // Create a polyline with the points.
                    Vector2 origin = new(eventData.PatrolPoints[0].X, eventData.PatrolPoints[0].Y);
                    foreach (Vector2U8 point in eventData.PatrolPoints)
                    {
                        Vector2 position = new(((point.X - origin.X) * 24f), ((point.Y - origin.Y) * 16f));
                        patrolPointObject.AddPolylinePoint(position.X, position.Y);
                    }
                }

                // Camera bounds.
                if (eventData.CameraBounds != null)
                {
                    TiledMapObject cameraBoundsObject = tiledMap.CreateObject(cameraBoundsGroup);
                    cameraBoundsObject.SetPositionAndSizeFromRectU8(eventData.CameraBounds.Value);
                    cameraBoundsObject.SetEventIDAndSortKey(eventData.ID, sortKey);
                }

                // Entities.
                foreach (TilemapEntityData entityData in eventData.EntityData)
                {
                    TiledMapObject entityObject = Helpers.CreateEntityFrom(entityData, tiledMap, entitiesGroup, eventData.ID, sortKey);

                    entityObject.SetTeamIndex(entityData.TeamIndex);
                    entityObject.Properties.Set("StartHealth", entityData.HealthPercent / 100f);

                    if (patrolPointID != null)
                        entityObject.Properties.Add("PatrolPoint", patrolPointID.Value);
                }

                // Pickups.
                foreach (TilemapEntityData pickupData in eventData.PickupData)
                    Helpers.CreateEntityFrom(pickupData, tiledMap, pickupGroup, eventData.ID, sortKey);

                // Walls.
                foreach (Tuple<Vector2U8, byte> wallData in eventData.Walls)
                {
                    TiledMapObject wallObject = tiledMap.CreateObject(wallsGroup);
                    wallObject.SetPositionTopLeftPoint(wallData.Item1);
                    wallObject.SetSizeFromTiles(1, 1);
                    wallObject.SetTeamIndex(wallData.Item2);
                    wallObject.SetEventIDAndSortKey(eventData.ID, sortKey);
                }

                sortKey++;
            }
        }

        private static void addTriggers(LegoTilemap legoMap, TiledMap tiledMap)
        {
            TiledMapObjectGroup triggerGroup = tiledMap.AddObjectGroup("Triggers", false);

            int sortKey = 0;
            foreach (TriggerData triggerData in legoMap.TriggerSections)
            {
                TiledMapObject areaObject = tiledMap.CreateObject(triggerGroup);
                areaObject.SetPositionAndSizeFromRectU8(triggerData.Area);
                areaObject.SetTriggerIDAndSortKey(triggerData.ID, sortKey);

                areaObject.Properties.Add("HasData", triggerData.HasData);
                areaObject.Properties.Add("TargetUnitIndex", triggerData.TargetUnitIndex);
                areaObject.Properties.Add("TargetUnitType", (int)triggerData.TargetUnitType);
                areaObject.Properties.Add("TargetFactionIndex", triggerData.TargetFactionIndex);
                areaObject.Properties.Add("TargetTeam", triggerData.TargetTeam);

                areaObject.Properties.Add("Unknown1", triggerData.Unknown1);
                areaObject.Properties.Add("Unknown2", triggerData.Unknown2);

                sortKey++;
            }
        }

        private static void addMarkers(LegoTilemap legoMap, TiledMap tiledMap)
        {
            TiledMapObjectGroup markersGroup = tiledMap.AddObjectGroup("Markers", true);

            int sortKey = 0;
            foreach ((byte markerID, List<MarkerData> markers) in legoMap.MarkerSections)
            {
                foreach (MarkerData markerData in markers)
                {
                    TiledMapObject markerObject = tiledMap.CreateObject(markersGroup);
                    markerObject.SetPositionCentredPoint(markerData.Position);
                    markerObject.SetMarkerIDAndSortKey(markerID, sortKey);

                    markerObject.Properties.Add("UnknownBool", markerData.UnknownBool);

                    // Bridges should have a width and height.
                    if (markerID == 7 || markerID == 8)
                    {
                        // Remove the offset.
                        markerObject.X -= 12f;
                        markerObject.Y -= 8f;

                        // Horizontal bridges.
                        if (markerID == 7)
                        {
                            markerObject.Width = 0;
                            markerObject.Height = 2 * 16f;

                            int x = markerData.Position.X;
                            while (x < legoMap.Width && legoMap.TileData[(markerData.Position.Y * legoMap.Width) + x].TileType == TileType.Water)
                            {
                                markerObject.Width += 24f;
                                x++;
                            }
                        }
                        // Vertical bridges.
                        else
                        {
                            markerObject.Width = 2 * 24f;
                            markerObject.Height = 0;

                            int y = markerData.Position.Y;
                            while (y < legoMap.Height && legoMap.TileData[(y * legoMap.Width) + markerData.Position.X].TileType == TileType.Water)
                            {
                                markerObject.Height += 16f;
                                y++;
                            }
                        }
                    }
                }
                sortKey++;
            }
        }

        private static void addMines(LegoTilemap legoMap, TiledMap tiledMap)
        {
            TiledMapObjectGroup minesGroup = tiledMap.AddObjectGroup("Mines");

            int sortKey = 0;
            foreach (List<Vector2U8> mines in legoMap.Mines)
            {
                foreach (Vector2U8 minePosition in mines)
                {
                    TiledMapObject mineObject = tiledMap.CreateObject(minesGroup);
                    mineObject.SetPositionTopLeftPoint(minePosition);
                    mineObject.SetSizeFromTiles(2, 2);
                    mineObject.SetSortKey(sortKey);
                }
                sortKey++;
            }
        }
    }
}
