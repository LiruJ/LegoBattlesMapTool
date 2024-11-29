﻿using ContentUnpacker.DataTypes;
using ContentUnpacker.Decompressors;
using ContentUnpacker.Tilemaps;
using GlobalShared.DataTypes;
using GlobalShared.Tilemaps;
using TiledToLB.Core.LegoBattles.DataStructures;
using TiledToLB.Core.Tiled.Map;
using TiledToLB.Core.Tiled.Property;
using TiledToLB.Core.Tiled.Tileset;

namespace TiledToLB.Core.LegoBattles
{
    public static class LegoMapWriter
    {
        public static async Task<TilemapReader> CreateLegoMapFromTiledMap(TiledMap tiledMap, string mapPath, Stream outputStream, bool compressOutput, bool silent)
        {
            if (!tiledMap.Properties.TryGetValue("Tileset", out TiledProperty tilesetProperty))
                throw new ArgumentException("Map is missing tileset property!", nameof(tiledMap));

            TilemapReader legoMap = new((byte)tiledMap.Width, (byte)tiledMap.Height)
            {
                TilesetName = tilesetProperty.Value
            };

            setTerrainData(legoMap, tiledMap, mapPath);

            setEvents(legoMap, tiledMap, silent);
            setTriggers(legoMap, tiledMap, silent);
            setMarkers(legoMap, tiledMap, silent);
            setMines(legoMap, tiledMap, silent);

            if (compressOutput)
            {
                if (!silent)
                    Console.WriteLine("Uncompressed map saved successfully, now compressing");

                using MemoryStream mapStream = new(0x8000);
                legoMap.Save(mapStream);
                mapStream.Position = 0;
                await LegoDecompressor.Encode(mapStream, outputStream, LZXEncodeType.EWB, 4096);
            }
            else
                legoMap.Save(outputStream);

            if (!silent)
                Console.WriteLine("Map saved successfully");

            return legoMap;
        }

        private static void setTerrainData(TilemapReader legoMap, TiledMap tiledMap, string mapPath)
        {
            if (!tiledMap.TileLayers.TryGetValue("Details", out TiledMapTileLayer? detailsLayer))
                throw new InvalidDataException("Map was missing tiles layer named \"Details\"!");
            tiledMap.TileLayers.TryGetValue("Trees", out TiledMapTileLayer? treesLayer);

            IEnumerable<Tuple<TiledMapTileset, TiledTileset>> tilesets = tiledMap.LoadTilesets(mapPath);

            TiledTileset baseTileset = tilesets.First(x => x.Item1.FirstGID == 1).Item2;
            TiledTileset? extraTileset = tilesets.FirstOrDefault(x => x.Item1.FirstGID == TilemapBlockPalette.FactionPaletteCount + 1)?.Item2;

            TiledTilesetTile[] orderedBaseTiles = [.. baseTileset.Tiles.OrderBy(x => x.ID)];
            TiledTilesetTile[]? orderedExtraTiles = extraTileset?.Tiles.OrderBy(x => x.ID).ToArray();

            for (int i = 0; i < legoMap.Width * legoMap.Height; i++)
            {
                int x = i % legoMap.Width;
                int y = i / legoMap.Width;

                int tileIndex = detailsLayer.Data[x, y];
                bool hasTree = treesLayer != null && treesLayer.Data[x, y] != 0;

                TileType tileType = TileType.Grass;
                if (tileIndex <= 0)
                    throw new InvalidDataException($"Tile at {x}, {y} is missing! (Has a value of 0 or lower)");
                else if (tileIndex <= TilemapBlockPalette.FactionPaletteCount)
                {
                    TiledTilesetTile tilesetTile = orderedBaseTiles[tileIndex - 1];
                    if (tilesetTile.Properties.TryGetValue("Type", out TiledProperty typeProperty) && byte.TryParse(typeProperty.Value, out byte type))
                        tileType = (TileType)type;
                }
                else if (extraTileset != null)
                {
                    if (tileIndex > TilemapBlockPalette.FactionPaletteCount + extraTileset.TileCount)
                        throw new InvalidDataException($"Tile at {x}, {y} is out of range!");
                    else
                    {
                        TiledTilesetTile tilesetTile = orderedExtraTiles![tileIndex - (TilemapBlockPalette.FactionPaletteCount + 1)];
                        if (tilesetTile.Properties.TryGetValue("Type", out TiledProperty typeProperty) && byte.TryParse(typeProperty.Value, out byte type))
                            tileType = (TileType)type;
                    }
                }
                else
                    throw new InvalidDataException($"Tile at {x}, {y} is out of range!");

                legoMap.TileData[i] = new()
                {
                    Index = (ushort)(tileIndex - 1),
                    TileType = tileType,
                    HasTree = hasTree,
                };
            }
        }

        private static void setEvents(TilemapReader legoMap, TiledMap tiledMap, bool silent)
        {
            EventLayers eventLayers = EventLayers.LoadFromTiledMap(tiledMap);

            if (!silent)
                Console.WriteLine($"Saving events:" +
                    $"\n\t{eventLayers.PatrolPointsLayer?.Objects?.Count ?? 0} patrol points" +
                    $"\n\t{eventLayers.CameraBoundsLayer?.Objects?.Count ?? 0} camera bounds" +
                    $"\n\t{eventLayers.EntitiesLayer?.Objects?.Count ?? 0} entities" +
                    $"\n\t{eventLayers.PickupsLayer?.Objects?.Count ?? 0} pickups" +
                    $"\n\t{eventLayers.WallsLayer?.Objects?.Count ?? 0} walls");

            foreach (int sortKey in eventLayers.AllSortKeys)
            {
                SortKeyEventLayer sortKeyEventLayer = eventLayers.GetLayerFromSortKey(sortKey);
                foreach (byte eventID in sortKeyEventLayer.AllEventIDs)
                {
                    EventData eventData = new(eventID);

                    // Patrol points.
                    if (sortKeyEventLayer?.PatrolPointsByEventID?.TryGetValue(eventID, out List<TiledMapObject>? currentPatrolPointGroup) == true)
                        foreach (TiledMapObject patrolPointObject in currentPatrolPointGroup)
                            for (int i = 0; i < patrolPointObject.PolylinePoints.Count - (patrolPointObject.PolylinePoints.Count % 2); i += 2)
                            {
                                byte tileX = (byte)Math.Floor(((patrolPointObject.X + patrolPointObject.PolylinePoints[i]) - 12f) / 24f);
                                byte tileY = (byte)Math.Floor(((patrolPointObject.Y + patrolPointObject.PolylinePoints[i + 1]) - 8f) / 16f);
                                eventData.PatrolPoints.Add(new(tileX, tileY));
                            }

                    // Camera bounds.
                    if (sortKeyEventLayer?.CameraBoundsByEventID?.TryGetValue(eventID, out List<TiledMapObject>? currentCameraBoundsGroup) == true)
                    {
                        if (currentCameraBoundsGroup.Count > 1)
                            throw new InvalidDataException($"Event ID {eventID} has more than one camera bounds! Only one can be assigned per event ID!");
                        TiledMapObject cameraBoundsObject = currentCameraBoundsGroup[0];
                        if (cameraBoundsObject.Width == null || cameraBoundsObject.Height == null)
                            throw new InvalidDataException($"Camera bounds for event ID {eventID} has no width/height!");

                        eventData.CameraBounds = new RectU8()
                        {
                            MinX = (byte)MathF.Floor(cameraBoundsObject.X / 24f),
                            MinY = (byte)MathF.Floor(cameraBoundsObject.Y / 16f),
                            MaxX = (byte)MathF.Floor((cameraBoundsObject.X + (cameraBoundsObject.Width.Value - 24f)) / 24f),
                            MaxY = (byte)MathF.Floor((cameraBoundsObject.Y + (cameraBoundsObject.Height.Value - 16f)) / 16f),
                        };
                    }

                    // Entities.
                    if (sortKeyEventLayer?.EntitiesByEventID?.TryGetValue(eventID, out List<TiledMapObject>? currentEntitiesGroup) == true)
                        foreach (TiledMapObject entityObject in currentEntitiesGroup)
                        {
                            byte[] extraData =
                            [
                                (byte)entityObject.Properties.GetOrDefault("ExtraData0", 0),
                                (byte)entityObject.Properties.GetOrDefault("ExtraData1", 0),
                                (byte)entityObject.Properties.GetOrDefault("ExtraData2", 0),
                            ];

                            TilemapEntityData tilemapEntityData = new()
                            {
                                X = (byte)MathF.Floor(entityObject.X / 24f),
                                Y = (byte)MathF.Floor(entityObject.Y / 16f),
                                HealthPercent = (byte)Math.Clamp(MathF.Floor(entityObject.Properties.GetOrDefault("StartHealth", 1.0f)) * 100, 0, 100),
                                TeamIndex = (byte)entityObject.Properties.GetOrDefault("TeamIndex", 0),
                                TypeIndex = (EntityType)entityObject.Properties.GetOrDefault("Type", (byte)EntityType.Melee),
                                SubTypeIndex = (byte)entityObject.Properties.GetOrDefault("SubType", 0),
                                ExtraData = extraData
                            };
                            eventData.EntityData.Add(tilemapEntityData);
                        }

                    // Pickups.
                    if (sortKeyEventLayer?.PickupsByEventID?.TryGetValue(eventID, out List<TiledMapObject>? currentPickupsGroup) == true)
                        foreach (TiledMapObject pickupObject in currentPickupsGroup)
                        {
                            byte[] extraData =
                            [
                                (byte)pickupObject.Properties.GetOrDefault("ExtraData0", 0),
                                (byte)pickupObject.Properties.GetOrDefault("ExtraData1", 0),
                            ];

                            TilemapEntityData tilemapEntityData = new()
                            {
                                X = (byte)MathF.Floor(pickupObject.X / 24f),
                                Y = (byte)MathF.Floor(pickupObject.Y / 16f),
                                HealthPercent = byte.MaxValue,
                                TeamIndex = (byte)pickupObject.Properties.GetOrDefault("TeamIndex", 0),
                                TypeIndex = (EntityType)pickupObject.Properties.GetOrDefault("Type", (byte)EntityType.Pickup),
                                SubTypeIndex = (byte)pickupObject.Properties.GetOrDefault("SubType", 0),
                                ExtraData = extraData
                            };
                            eventData.PickupData.Add(tilemapEntityData);
                        }

                    // Walls.
                    if (sortKeyEventLayer?.WallsByEventID?.TryGetValue(eventID, out List<TiledMapObject>? currentWallsGroup) == true)
                        foreach (TiledMapObject wallObject in currentWallsGroup)
                        {
                            Vector2U8 position = new()
                            {
                                X = (byte)MathF.Floor((wallObject.X + ((wallObject.Width ?? 0) / 2f)) / 24f),
                                Y = (byte)MathF.Floor((wallObject.Y + ((wallObject.Height ?? 0) / 2f)) / 16f),
                            };
                            byte teamIndex = (byte)wallObject.Properties.GetOrDefault("TeamIndex", 0);

                            eventData.Walls.Add(new(position, teamIndex));
                        }

                    legoMap.EventSections.Add(eventData);
                }
            }
        }

        private static void setTriggers(TilemapReader legoMap, TiledMap tiledMap, bool silent)
        {
            if (!tiledMap.ObjectGroups.TryGetValue("Triggers", out TiledMapObjectGroup? triggersLayer))
            {
                if (!silent)
                    Console.WriteLine("Found no triggers");
                return;
            }
                if (!silent)
            Console.WriteLine($"Saving {triggersLayer.Objects.Count} triggers");

            foreach (TiledMapObject triggerObject in triggersLayer.Objects.OrderBy(x => x.Properties.GetOrDefault("SortKey", int.MaxValue)))
            {
                byte triggerID = (byte)triggerObject.Properties.GetOrDefault("TriggerID", byte.MaxValue);
                if (triggerObject.Width == null || triggerObject.Height == null)
                    throw new InvalidDataException($"Trigger bounds for trigger ID {triggerID} has no width/height!");

                TriggerData triggerData = new(triggerID)
                {
                    Area = new()
                    {
                        MinX = (byte)MathF.Floor(triggerObject.X / 24f),
                        MinY = (byte)MathF.Floor(triggerObject.Y / 16f),
                        MaxX = (byte)MathF.Floor((triggerObject.X + (triggerObject.Width.Value - 24f)) / 24f),
                        MaxY = (byte)MathF.Floor((triggerObject.Y + (triggerObject.Height.Value - 16f)) / 16f),
                    },
                    HasData = triggerObject.Properties.GetOrDefault("HasData", false),
                    TargetUnitIndex = (byte)triggerObject.Properties.GetOrDefault("TargetUnitIndex", byte.MaxValue),
                    TargetUnitType = (EntityType)triggerObject.Properties.GetOrDefault("TargetUnitType", (byte)EntityType.Melee),
                    TargetFactionIndex = (byte)triggerObject.Properties.GetOrDefault("TargetFactionIndex", byte.MaxValue),
                    TargetTeam = (sbyte)triggerObject.Properties.GetOrDefault("TargetTeam", sbyte.MaxValue),
                    Unknown1 = (byte)triggerObject.Properties.GetOrDefault("Unknown1", byte.MaxValue),
                    Unknown2 = (byte)triggerObject.Properties.GetOrDefault("Unknown2", byte.MaxValue),
                };
                legoMap.TriggerSections.Add(triggerData);
            }
        }

        private static void setMarkers(TilemapReader legoMap, TiledMap tiledMap, bool silent)
        {
            if (!tiledMap.ObjectGroups.TryGetValue("Markers", out TiledMapObjectGroup? markersLayer))
            {
                if (!silent)
                    Console.WriteLine("Found no markers");
                return;
            }
                if (!silent)
            Console.WriteLine($"Saving {markersLayer.Objects.Count} markers");

            foreach (IGrouping<int, TiledMapObject>? sortKeyGroup in markersLayer.Objects
                                                                        .GroupBy(x => x.Properties.GetOrDefault("SortKey", int.MaxValue))
                                                                        .OrderBy(x => x.Key))
            {
                int sortKey = sortKeyGroup.Key;
                foreach (IGrouping<int, TiledMapObject> markerIDGroup in sortKeyGroup.GroupBy(x => x.Properties.GetOrDefault("MarkerID", 0)))
                {
                    byte markerID = (byte)markerIDGroup.Key;
                    List<MarkerData> markers = [];
                    foreach (TiledMapObject markerObject in markerIDGroup)
                    {
                        Vector2U8 position = new()
                        {
                            X = (byte)MathF.Floor(markerObject.X / 24f),
                            Y = (byte)MathF.Floor(markerObject.Y / 16f),
                        };
                        bool unknownBool = markerObject.Properties.GetOrDefault("UnknownBool", false);

                        MarkerData markerData = new(position, unknownBool);
                        markers.Add(markerData);
                    }
                    legoMap.MarkerSections.Add(new(markerID, markers));
                }
            }
        }

        private static void setMines(TilemapReader legoMap, TiledMap tiledMap, bool silent)
        {
            if (!tiledMap.ObjectGroups.TryGetValue("Mines", out TiledMapObjectGroup? minesLayer))
            {
                if (!silent)
                    Console.WriteLine("Found no mines");
                for (int i = 0; i < 4; i++)
                    legoMap.Mines.Add([]);
                return;
            }
            if (!silent)
            Console.WriteLine($"Saving {minesLayer.Objects.Count} mines");

            IGrouping<int, TiledMapObject>[] mineGroups = [.. minesLayer.Objects
                                                            .GroupBy(x => x.Properties.GetOrDefault("SortKey", int.MaxValue))
                                                            .OrderBy(x => x.Key)];

            for (int i = 0; i < Math.Max(mineGroups.Length, 4); i++)
            {
                IGrouping<int, TiledMapObject>? sortKeyGroup = i < mineGroups.Length ? mineGroups[i] : null;

                List<Vector2U8> minePositions = [];
                if (sortKeyGroup != null)
                    foreach (TiledMapObject mineObject in sortKeyGroup)
                    {
                        Vector2U8 position = new()
                        {
                            X = (byte)MathF.Floor(mineObject.X / 24f),
                            Y = (byte)MathF.Floor(mineObject.Y / 16f),
                        };
                        minePositions.Add(position);
                    }
                legoMap.Mines.Add(minePositions);
            }
        }
    }
}