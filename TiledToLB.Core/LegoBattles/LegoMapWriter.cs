using ContentUnpacker.Tilemaps;
using TiledToLB.Core.Tiled;
using TiledToLB.Core.Tiled.Map;
using TiledToLB.Core.Tiled.Tileset;
using TiledToLB.Core.Tilemap;

namespace TiledToLB.Core.LegoBattles
{
    public static class LegoMapWriter
    {
        public static void SaveTiledMap(TiledMap tiledMap, string mapPath, Stream outputStream)
        {
            //TilemapReader legoMap = new((byte)tiledMap.Width, (byte)tiledMap.Height);
            

            // Don't use "using" as the caller takes care of their stream.
            BinaryWriter mapWriter = new(outputStream);

            foreach (char character in "MAP")
                mapWriter.Write(character);

            saveMapData(tiledMap, mapWriter);
            saveTiledLayers(tiledMap, mapPath, mapWriter);

            saveEvents(tiledMap, mapWriter);
            saveTriggers(tiledMap, mapWriter);
            //saveMarkers(mapWriter);
            //saveMines(mapWriter);

            foreach (char character in "PAM")
                mapWriter.Write(character);

            //legoMap.Save(outputStream);
        }

        private static void saveMapData(TiledMap tiledMap, BinaryWriter mapWriter)
        {
            if (tiledMap.PropertiesByName.TryGetValue("Tileset", out TiledProperty tilesetProperty))
                throw new ArgumentException("Map is missing tileset property!", nameof(tiledMap));

            foreach (char character in "TERR")
                mapWriter.Write(character);
            mapWriter.Write(tiledMap.Width);
            mapWriter.Write(tiledMap.Height);
            mapWriter.Write((byte)3);
            mapWriter.Write((byte)2);
            foreach (char character in tilesetProperty!.Value)
                mapWriter.Write(character);

            while (mapWriter.BaseStream.Position < 0x2B)
                mapWriter.Write((byte)0);
        }

        private static void saveTiledLayers(TiledMap tiledMap, string mapPath, BinaryWriter mapWriter)
        {
            if (!tiledMap.TileLayers.TryGetValue("Details", out TiledMapTileLayer? detailsLayer))
                throw new InvalidDataException("Map was missing tiles layer named \"Details\"!");

            saveTiledTypeLayer(tiledMap, mapPath, detailsLayer, mapWriter);
            saveUnknownLayer(tiledMap.Width, tiledMap.Height, mapWriter);

            if (tiledMap.TileLayers.TryGetValue("Trees", out TiledMapTileLayer? treeLayer))
                saveTrees(treeLayer.Width, treeLayer.Height, treeLayer.Data, mapWriter);
            else
                throw new InvalidDataException("Map was missing trees layer named \"Trees\"!");

            saveDetailLayer(detailsLayer.Width, detailsLayer.Height, detailsLayer.Data, mapWriter);

            foreach (char character in "RRET")
                mapWriter.Write(character);
        }

        private static void saveTiledTypeLayer(TiledMap tiledMap, string mapPath, TiledMapTileLayer detailsLayer, BinaryWriter mapWriter)
        {
            IEnumerable<Tuple<TiledMapTileset, TiledTileset>> tilesets = tiledMap.LoadTilesets(mapPath);
            TiledTileset baseTileset = tilesets.First(x => x.Item1.FirstGID == 1).Item2;
            TiledTileset? extraTileset = tilesets.FirstOrDefault(x => x.Item1.FirstGID == baseTileset.TileCount + 1)?.Item2;

            for (int y = 0; y < tiledMap.Height; y++)
                for (int x = 0; x < tiledMap.Width; x++)
                {
                    int index = detailsLayer.Data[x, y];

                    byte tileType = 0;
                    if (index <= 0)
                        throw new InvalidDataException($"Tile at {x}, {y} is missing! (Has a value of 0 or lower)");
                    else if (index < baseTileset.TileCount + 1)
                    {

                        TiledTilesetTile tilesetTile = baseTileset.Tiles[index + 1];
                        if (tilesetTile.Properties.TryGetValue("Type", out TiledProperty typeProperty))
                            tileType = byte.TryParse(typeProperty.Value, out byte type) ? type : (byte)0;
                    }
                    else if (extraTileset != null)
                    {
                        if (index >= baseTileset.TileCount + extraTileset.TileCount + 1)
                            throw new InvalidDataException($"Tile at {x}, {y} is out of range!");
                        else
                        {
                            TiledTilesetTile tilesetTile = extraTileset.Tiles[baseTileset.TileCount + index + 1];
                            if (tilesetTile.Properties.TryGetValue("Type", out TiledProperty typeProperty))
                                tileType = byte.TryParse(typeProperty.Value, out byte type) ? type : (byte)0;
                        }
                    }
                    else
                        throw new InvalidDataException($"Tile at {x}, {y} is out of range!");

                    mapWriter.Write(tileType);
                }
        }

        private static void saveUnknownLayer(int width, int height, BinaryWriter mapWriter)
        {
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    mapWriter.Write((ushort)0);
        }

        private static void saveTrees(int width, int height, int[,] treeLayer, BinaryWriter mapWriter)
        {
            List<byte> treeStrips = [];
            bool placeTrees = false;
            int currentStripLength = 0;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    bool hasTrees = treeLayer[x, y] > 0;

                    if (hasTrees == placeTrees)
                    {
                        currentStripLength++;
                        if (currentStripLength > byte.MaxValue)
                        {
                            treeStrips.Add(byte.MaxValue);
                            treeStrips.Add(0);
                            currentStripLength = 1;
                        }
                    }
                    else
                    {
                        treeStrips.Add((byte)currentStripLength);
                        placeTrees = hasTrees;
                        currentStripLength = 1;
                    }
                }
            treeStrips.Add((byte)currentStripLength);

            mapWriter.Write((ushort)treeStrips.Count);
            foreach (byte treeStrip in treeStrips)
                mapWriter.Write(treeStrip);
        }

        private static void saveDetailLayer(int width, int height, int[,] data, BinaryWriter mapWriter)
        {
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    mapWriter.Write(data[x, y] - 1);
        }

        private static void saveEvents(TiledMap tiledMap, BinaryWriter mapWriter)
        {
            foreach (char character in "EVNT")
                mapWriter.Write(character);

            savePickups(tiledMap, mapWriter);
            saveEntities(tiledMap, mapWriter);

            mapWriter.Write('!');
        }

        private static void savePickups(TiledMap tiledMap, BinaryWriter mapWriter)
        {
            if (!tiledMap.ObjectGroups.TryGetValue("Pickups", out TiledMapObjectGroup? pickupsLayer))
                throw new InvalidDataException("Map is missing pickups layer!");
            IEnumerable<EntityData> pickups = pickupsLayer.Objects.Select(EntityData.LoadFromTiledMapObject);

            // Minikits, then blue studs, then red bricks, then golden bricks, then the team spawns, then anything else.
            saveEvent(0, null, pickups.Where(x => x.SubType == 6), mapWriter);
            saveEvent(0, null, pickups.Where(x => x.SubType == 7), mapWriter);
            saveEvent(0, null, pickups.Where(x => x.SubType == 8), mapWriter);
            saveEvent(0, null, pickups.Where(x => x.SubType == 9), mapWriter);
        }

        private static void saveEntities(TiledMap tiledMap, BinaryWriter mapWriter)
        {
            if (!tiledMap.ObjectGroups.TryGetValue("Entities", out TiledMapObjectGroup? entitiesLayer))
                throw new InvalidDataException("Map is missing entities layer!");
            IEnumerable<EntityData> entities = entitiesLayer.Objects.Select(EntityData.LoadFromTiledMapObject);

            // TODO: Get patrol points and bundle them together with their linked entities.

             //entities.OrderBy(x => x.EventID).ThenBy(x => x.TeamIndex)
        }

        private static void saveEvent(byte eventID, IEnumerable<EntityData>? entities, IEnumerable<EntityData>? pickups, BinaryWriter mapWriter)
        {
            mapWriter.Write('L'); // Separator.
            mapWriter.Write(eventID); // Event ID.
            mapWriter.Write((byte)0); // Patrol points.
            mapWriter.Write((byte)0); // Camera bounds.

            // Entities.
            mapWriter.Write((byte)(entities?.Count() ?? 0));
            if (entities != null)
                foreach (EntityData entity in entities)
                    entity.SaveToWriter(mapWriter);

            // Pickups.
            mapWriter.Write((byte)(pickups?.Count() ?? 0));
            if (pickups != null)
                foreach (EntityData pickup in pickups)
                    pickup.SaveToWriter(mapWriter);

            mapWriter.Write((byte)0); // Walls.
        }

        private static void saveTriggers(TiledMap tiledMap, BinaryWriter mapWriter)
        {
            foreach (char character in "TRIG")
                mapWriter.Write(character);

            mapWriter.Write('!');
        }

        //private void saveMarkers(BinaryWriter mapWriter)
        //{
        //    foreach (char character in "MARK")
        //        mapWriter.Write(character);

        //    if (MarkerPositions.Count > 0)
        //    {
        //        mapWriter.Write('L');
        //        mapWriter.Write((byte)0);
        //        mapWriter.Write((byte)MarkerPositions.Count);
        //        foreach ((byte markerX, byte markerY) in MarkerPositions)
        //        {
        //            mapWriter.Write(markerX);
        //            mapWriter.Write(markerY);
        //            mapWriter.Write(false);
        //        }
        //    }


        //    IEnumerable<Tuple<byte, byte, bool>> horizontalBridges = BridgePositions.Where((b) => b.Item3);
        //    if (horizontalBridges.Any())
        //    {
        //        mapWriter.Write('L');
        //        mapWriter.Write((byte)7);
        //        mapWriter.Write((byte)horizontalBridges.Count());
        //        foreach ((byte bridgeX, byte bridgeY, bool _) in horizontalBridges)
        //        {
        //            mapWriter.Write(bridgeX);
        //            mapWriter.Write(bridgeY);
        //            mapWriter.Write(false);
        //        }
        //    }

        //    IEnumerable<Tuple<byte, byte, bool>> verticalBridges = BridgePositions.Where((b) => !b.Item3);
        //    if (verticalBridges.Any())
        //    {
        //        mapWriter.Write('L');
        //        mapWriter.Write((byte)8);
        //        mapWriter.Write((byte)verticalBridges.Count());
        //        foreach ((byte bridgeX, byte bridgeY, bool _) in verticalBridges)
        //        {
        //            mapWriter.Write(bridgeX);
        //            mapWriter.Write(bridgeY);
        //            mapWriter.Write(false);
        //        }
        //    }

        //    mapWriter.Write('!');
        //}

        //private void saveMines(BinaryWriter mapWriter)
        //{
        //    foreach (char character in "MINE")
        //        mapWriter.Write(character);

        //    if (MinePositions.Count > 0)
        //    {
        //        mapWriter.Write('L');
        //        mapWriter.Write((byte)MinePositions.Count);
        //        foreach ((byte mineX, byte mineY) in MinePositions)
        //        {
        //            mapWriter.Write(mineX);
        //            mapWriter.Write(mineY);
        //        }
        //    }

        //    mapWriter.Write('!');
        //}
    }
}
