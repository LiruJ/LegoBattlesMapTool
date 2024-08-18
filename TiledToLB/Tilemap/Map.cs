using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TiledToLB.Tilemap
{

    internal class Map
    {
        #region Layers
        public TilesetTile[,] DetailLayer { get; }

        public bool[,] TreeLayer { get; }
        #endregion

        #region Properties
        public string TilesetName { get; private set; } = string.Empty;

        /// <summary>
        /// The width of the map in tiles.
        /// </summary>
        public byte Width { get; }

        /// <summary>
        /// The height of the map in tiles.
        /// </summary>
        public byte Height { get; }

        public Tileset DetailsTileset { get; private set; }

        public List<Tuple<byte, byte>> MinePositions { get; } = new();

        public List<Tuple<byte, byte>> MarkerPositions { get; } = new();

        public List<Tuple<byte, byte, bool>> BridgePositions { get; } = new();

        public List<EntityData> Pickups { get; } = new();

        public List<List<EntityData>> EntitiesPerTeam { get; } = new();
        #endregion

        #region Constructors
        public Map(byte width, byte height)
        {
            Width = width;
            Height = height;

            DetailsTileset = new();

            DetailLayer = new TilesetTile[Width, Height];
            TreeLayer = new bool[Width, Height];
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    TreeLayer[x, y] = false;

            EntitiesPerTeam.Add(new());
            EntitiesPerTeam.Add(new());
            EntitiesPerTeam.Add(new());
            EntitiesPerTeam.Add(new());
        }
        #endregion

        #region Load Functions
        public static Tuple<byte, byte> LoadMapDimensionsFromTiled(XmlDocument tiledFile)
        {
            XmlNode mapNode = tiledFile.SelectSingleNode("/map") ?? throw new Exception("Tiled file has missing map node!");

            byte width = byte.Parse(mapNode.Attributes?["width"]?.Value ?? throw new Exception("Tiled file has missing width attribute!"));
            byte height = byte.Parse(mapNode.Attributes?["height"]?.Value ?? throw new Exception("Tiled file has missing height attribute!"));
            return Tuple.Create(width, height);
        }

        public void LoadFromTiled(XmlDocument tiledFile, string inputFilePath)
        {
            loadMapProperties(tiledFile);
            loadTilesets(tiledFile, inputFilePath);
            loadLayers(tiledFile);

            loadEntities(tiledFile);
            loadPositionedEntity(tiledFile, "Bridges", (node, x, y) =>
            {
                bool isHorizontal = bool.TryParse(node.SelectSingleNode("properties/property[@name='IsHorizontal']")?.Attributes?["value"]?.Value, out isHorizontal) && isHorizontal;
                BridgePositions.Add(new(x, y, isHorizontal));
            });
            loadPositionedEntity(tiledFile, "Markers", MarkerPositions);
            loadPositionedEntity(tiledFile, "Mines", MinePositions);
        }

        private void loadMapProperties(XmlDocument tiledFile)
        {
            XmlNode mapPropertiesNode = tiledFile.SelectSingleNode("/map/properties") ?? throw new Exception("Tiled file has missing map properties!");

            TilesetName = mapPropertiesNode.SelectSingleNode("property[@name='Tileset']")?.Attributes?["value"]?.InnerText ?? throw new Exception("Tiled file has missing tileset property on the main map object!");
        }

        private void loadTilesets(XmlDocument tiledFile, string inputFilePath)
        {
            XmlNode tilesetNode = tiledFile.SelectSingleNode("/map/tileset") ?? throw new Exception("Tiled file has missing tileset!");

            uint tilesetFirstIndex = uint.TryParse(tilesetNode.Attributes?["firstgid"]?.Value, out uint firstGID) ? firstGID : 0;

            string tilesetSourcePath = tilesetNode.Attributes?["source"]?.Value ?? throw new Exception("Tiled file's tileset is missing source!");
            tilesetSourcePath = Path.Combine(Path.GetDirectoryName(inputFilePath), tilesetSourcePath);

            XmlDocument tilesetFile = new();
            tilesetFile.Load(tilesetSourcePath);

            DetailsTileset = Tileset.LoadFromTiledTileset(tilesetFile, tilesetFirstIndex);
        }

        private void loadLayers(XmlDocument tiledFile)
        {
            XmlNode detailLayerNode = tiledFile.SelectSingleNode("/map/layer[@name='Details']/data") ?? throw new Exception("Tiled file has missing details layer!");
            string[] splitDetailValue = detailLayerNode.InnerText.Split(',', StringSplitOptions.TrimEntries);
            for (int i = 0; i < splitDetailValue.Length; i++)
            {
                int x = i % Width, y = i / Width;
                string detailStringValue = splitDetailValue[i];
                int index = int.TryParse(detailStringValue, out int detailValue) ? detailValue - 1 : throw new Exception($"Detail value at {x},{y} is invalid!");

                DetailLayer[x, y] = DetailsTileset.TilesetData[index];
            }

            XmlNode treesLayerNode = tiledFile.SelectSingleNode("/map/layer[@name='Trees']/data") ?? throw new Exception("Tiled file has missing trees layer!");
            string[] splitTreesValue = treesLayerNode.InnerText.Split(',', StringSplitOptions.TrimEntries);
            for (int i = 0; i < splitTreesValue.Length; i++)
            {
                int x = i % Width, y = i / Width;
                TreeLayer[x, y] = int.TryParse(splitTreesValue[i], out int treeValue) && treeValue != 0;


                TilesetTile tilesetTile = DetailLayer[x, y];
                if (TreeLayer[x, y] && tilesetTile.TileType != TileType.Grass)
                    throw new Exception($"Tree at {x},{y} was not placed on grass!");
            }
        }

        private void loadEntities(XmlDocument tiledFile)
        {
            XmlNodeList? entityLayerNodes = tiledFile.SelectNodes("/map/objectgroup[@name='Entities']/object");
            if (entityLayerNodes == null)
                return;

            foreach (XmlNode entityNode in entityLayerNodes)
            {
                EntityData entityData = EntityData.LoadFromTiledNode(entityNode);

                if (entityData.Type == EntityType.Pickup)
                    Pickups.Add(entityData);
                else
                    EntitiesPerTeam[entityData.TeamIndex].Add(entityData);
            }
        }

        private static void loadPositionedEntity(XmlDocument tiledFile, string layerName, Action<XmlNode, byte, byte> nodeReader)
        {
            XmlNodeList? layerNodes = tiledFile.SelectNodes($"/map/objectgroup[@name='{layerName}']/object");
            if (layerNodes == null)
                return;

            foreach (XmlNode entityNode in layerNodes)
            {
                byte x = float.TryParse(entityNode.Attributes?["x"]?.Value, out float xValue) ? (byte)MathF.Floor(xValue / 24f) : throw new Exception("Entity has missing x position!");
                byte y = float.TryParse(entityNode.Attributes?["y"]?.Value, out float yValue) ? (byte)MathF.Floor(yValue / 16f) : throw new Exception("Entity has missing y position!");
                nodeReader(entityNode, x, y);
            }
        }

        private static void loadPositionedEntity(XmlDocument tiledFile, string layerName, List<Tuple<byte, byte>> positions)
        {
            XmlNodeList? layerNodes = tiledFile.SelectNodes($"/map/objectgroup[@name='{layerName}']/object");
            if (layerNodes == null)
                return;

            foreach (XmlNode entityNode in layerNodes)
            {
                byte x = float.TryParse(entityNode.Attributes?["x"]?.Value, out float xValue) ? (byte)MathF.Floor(xValue / 24f) : throw new Exception("Entity has missing x position!");
                byte y = float.TryParse(entityNode.Attributes?["y"]?.Value, out float yValue) ? (byte)MathF.Floor(yValue / 16f) : throw new Exception("Entity has missing y position!");

                positions.Add(new(x, y));
            }
        }
        #endregion

        #region Save Functions
        public void Save(string filePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            using FileStream mapFile = File.Create(filePath);
            using BinaryWriter mapWriter = new(mapFile);

            foreach (char character in "MAP")
                mapWriter.Write(character);

            saveMapData(mapWriter);
            saveLayers(mapWriter);
            saveEntities(mapWriter);

            foreach (char character in "PAM")
                mapWriter.Write(character);
        }

        private void saveMapData(BinaryWriter mapWriter)
        {
            foreach (char character in "TERR")
                mapWriter.Write(character);
            mapWriter.Write(Width);
            mapWriter.Write(Height);
            mapWriter.Write((byte)3);
            mapWriter.Write((byte)2);
            foreach (char character in TilesetName)
                mapWriter.Write(character);

            while (mapWriter.BaseStream.Position < 0x2B)
                mapWriter.Write((byte)0);
        }

        private void saveLayers(BinaryWriter mapWriter)
        {
            saveTypeLayer(mapWriter);
            saveUnknownLayer(mapWriter);
            saveTrees(mapWriter);
            saveDetailLayer(mapWriter);

            foreach (char character in "RRET")
                mapWriter.Write(character);


        }

        private void saveTypeLayer(BinaryWriter mapWriter)
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    mapWriter.Write((byte)DetailLayer[x, y].TileType);
        }

        private void saveUnknownLayer(BinaryWriter mapWriter)
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    mapWriter.Write((ushort)0);
        }

        private void saveTrees(BinaryWriter mapWriter)
        {
            List<byte> treeStrips = new();
            bool placeTrees = false;
            int currentStripLength = 0;
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    bool hasTrees = TreeLayer[x, y];

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

        private void saveDetailLayer(BinaryWriter mapWriter)
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    mapWriter.Write(DetailLayer[x, y].Index);
        }

        private void saveEntities(BinaryWriter mapWriter)
        {
            saveEvents(mapWriter);
            foreach (char character in "TRIG!")
                mapWriter.Write(character);
            saveMarkers(mapWriter);
            saveMines(mapWriter);
        }

        private void saveEvents(BinaryWriter mapWriter)
        {
            foreach (char character in "EVNT")
                mapWriter.Write(character);

            // Write the pickups first.
            mapWriter.Write('L');
            mapWriter.Write((byte)0); // ID of 0.
            mapWriter.Write((byte)0); // Patrol points.
            mapWriter.Write((byte)0); // Camera bounds.
            mapWriter.Write((byte)0); // Entities.
            mapWriter.Write((byte)Pickups.Count); // Pickups.
            foreach (EntityData pickup in Pickups)
                pickup.SaveToWriter(mapWriter);
            mapWriter.Write((byte)0); // Walls.

            foreach (List<EntityData> teamEntities in EntitiesPerTeam)
            {
                if (teamEntities.Count == 0)
                    continue;

                mapWriter.Write('L');
                mapWriter.Write((byte)0); // ID of 0.
                mapWriter.Write((byte)0); // Patrol points.
                mapWriter.Write((byte)0); // Camera bounds.
                mapWriter.Write((byte)teamEntities.Count); // Entities.
                foreach (EntityData entity in teamEntities)
                    entity.SaveToWriter(mapWriter);
                mapWriter.Write((byte)0); // Pickups.
                mapWriter.Write((byte)0); // Walls.
            }

            mapWriter.Write('!');
        }

        private void saveMarkers(BinaryWriter mapWriter)
        {
            foreach (char character in "MARK")
                mapWriter.Write(character);

            if (MarkerPositions.Count > 0)
            {
                mapWriter.Write('L');
                mapWriter.Write((byte)0);
                mapWriter.Write((byte)MarkerPositions.Count);
                foreach ((byte markerX, byte markerY) in MarkerPositions)
                {
                    mapWriter.Write(markerX);
                    mapWriter.Write(markerY);
                    mapWriter.Write(false);
                }
            }


            IEnumerable<Tuple<byte, byte, bool>> horizontalBridges = BridgePositions.Where((b) => b.Item3);
            if (horizontalBridges.Any())
            {
                mapWriter.Write('L');
                mapWriter.Write((byte)7);
                mapWriter.Write((byte)horizontalBridges.Count());
                foreach ((byte bridgeX, byte bridgeY, bool _) in horizontalBridges)
                {
                    mapWriter.Write(bridgeX);
                    mapWriter.Write(bridgeY);
                    mapWriter.Write(false);
                }
            }

            IEnumerable<Tuple<byte, byte, bool>> verticalBridges = BridgePositions.Where((b) => !b.Item3);
            if (verticalBridges.Any())
            {
                mapWriter.Write('L');
                mapWriter.Write((byte)8);
                mapWriter.Write((byte)verticalBridges.Count());
                foreach ((byte bridgeX, byte bridgeY, bool _) in verticalBridges)
                {
                    mapWriter.Write(bridgeX);
                    mapWriter.Write(bridgeY);
                    mapWriter.Write(false);
                }
            }

            mapWriter.Write('!');
        }

        private void saveMines(BinaryWriter mapWriter)
        {
            foreach (char character in "MINE")
                mapWriter.Write(character);

            if (MinePositions.Count > 0)
            {
                mapWriter.Write('L');
                mapWriter.Write((byte)MinePositions.Count);
                foreach ((byte mineX, byte mineY) in MinePositions)
                {
                    mapWriter.Write(mineX);
                    mapWriter.Write(mineY);
                }
            }

            mapWriter.Write('!');
        }
        #endregion
    }
}
