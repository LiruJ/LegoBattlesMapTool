using LiruGameHelper.XML;
using System.Xml;
using TiledToLB.Core.Tiled.Property;
using TiledToLB.Core.Tiled.Tileset;

namespace TiledToLB.Core.Tiled.Map
{
    public class TiledMap(int width, int height)
    {
        #region Constants
        public const string TargetVersion = "1.10";
        public const string TargetTiledVersion = "1.11.0";
        #endregion

        #region Fields

        #endregion

        #region Properties
        public int Width { get; } = width;

        public int Height { get; } = height;

        public int TileWidth { get; set; }

        public int TileHeight { get; set; }

        public int NextLayerID { get; set; } = 0;

        public int NextObjectID { get; set; } = 0;

        public TiledPropertyCollection Properties { get; } = [];

        public List<TiledMapTileset> Tilesets { get; } = [];

        public Dictionary<string, TiledMapTileLayer> TileLayers { get; } = [];

        public Dictionary<string, TiledMapObjectGroup> ObjectGroups { get; } = [];
        #endregion

        #region Tileset Functions
        public IEnumerable<Tuple<TiledMapTileset, TiledTileset>> LoadTilesets(string mapFilePath)
        {
            List<Tuple<TiledMapTileset, TiledTileset>> tilesets = new(Tilesets.Count);
            foreach (TiledMapTileset tilesetInfo in Tilesets)
            {
                TiledTileset tileset = TiledTileset.LoadFromPaths(mapFilePath, tilesetInfo.Source);
                tilesets.Add(new(tilesetInfo, tileset));
            }

            return tilesets;
        }

        public void AddTileset(string source, int firstGID)
            => Tilesets.Add(new() { FirstGID = firstGID, Source = source });

        public void AddTileset(TiledMapTileset tileset) => Tilesets.Add(tileset);
        #endregion

        #region Tile Layer Functions
        public void AddTileLayer(TiledMapTileLayer layer)
        {
            layer.ID = NextLayerID;
            NextLayerID++;

            TileLayers.Add(layer.Name, layer);
        }
        #endregion

        #region Object Group Functions
        public void AddObjectGroup(TiledMapObjectGroup group)
            => ObjectGroups.Add(group.Name, group);
        #endregion

        #region Load Functions
        public static TiledMap Load(string filePath)
        {
            using FileStream fileStream = File.OpenRead(filePath);
            return Load(fileStream);
        }

        public static TiledMap Load(Stream stream)
        {
            // Load the xml file.
            XmlDocument tiledFile = new();
            tiledFile.Load(stream);

            // Get the main node.
            XmlNode mapNode = tiledFile.SelectSingleNode("/map") ?? throw new InvalidDataException("Tiled file has missing map node!");

            // Read the size of the map.
            byte width = byte.Parse(mapNode.Attributes?["width"]?.Value ?? throw new InvalidDataException("Tiled file has missing width attribute!"));
            byte height = byte.Parse(mapNode.Attributes?["height"]?.Value ?? throw new InvalidDataException("Tiled file has missing height attribute!"));
            byte tileWidth = byte.Parse(mapNode.Attributes?["tilewidth"]?.Value ?? throw new InvalidDataException("Tiled file has missing tile width attribute!"));
            byte tileHeight = byte.Parse(mapNode.Attributes?["tileheight"]?.Value ?? throw new InvalidDataException("Tiled file has missing tile height attribute!"));
            byte nextLayerID = byte.TryParse(mapNode.Attributes?["nextlayerid"]?.Value, out byte value) ? value : (byte)0;
            byte nextObjectID = byte.TryParse(mapNode.Attributes?["nextobjectid"]?.Value, out value) ? value : (byte)0;

            // Create, load, and return the map.
            TiledMap map = new(width, height)
            {
                NextLayerID = nextLayerID,
                NextObjectID = nextObjectID,
                TileWidth = tileWidth,
                TileHeight = tileHeight,
            };
            map.Load(tiledFile);
            return map;
        }

        public void Load(XmlDocument tiledFile)
        {
            loadProperties(tiledFile);
            loadTilesets(tiledFile);
            loadTileLayers(tiledFile);
            loadObjectLayers(tiledFile);
        }

        private void loadProperties(XmlDocument tiledFile)
        {
            // Load each property.
            XmlNodeList? propertyNodes = tiledFile.SelectNodes("/map/properties/property") ?? throw new InvalidDataException("Tiled file has missing map properties!");
            Properties.Load(propertyNodes);
        }

        private void loadTilesets(XmlDocument tiledFile)
        {
            XmlNodeList? tilesetNodes = tiledFile.SelectNodes("/map/tileset");
            if (tilesetNodes == null)
                return;

            foreach (XmlNode tilesetNode in tilesetNodes)
            {
                if (tilesetNode.NodeType != XmlNodeType.Element)
                    continue;
                TiledMapTileset tileset = TiledMapTileset.LoadFromNode(tilesetNode);
                Tilesets.Add(tileset);
            }
        }

        private void loadTileLayers(XmlDocument tiledFile)
        {
            XmlNodeList? layerNodes = tiledFile.SelectNodes("/map/layer");
            if (layerNodes == null)
                return;

            foreach (XmlNode layerNode in layerNodes)
            {
                if (layerNode.NodeType != XmlNodeType.Element)
                    continue;
                TiledMapTileLayer layer = TiledMapTileLayer.LoadFromNode(layerNode);
                TileLayers.Add(layer.Name, layer);
            }
        }

        private void loadObjectLayers(XmlDocument tiledFile)
        {
            XmlNodeList? objectGroupNodes = tiledFile.SelectNodes("/map/objectgroup");
            if (objectGroupNodes == null)
                return;

            foreach (XmlNode objectGroupNode in objectGroupNodes)
            {
                if (objectGroupNode.NodeType != XmlNodeType.Element)
                    continue;
                TiledMapObjectGroup layer = TiledMapObjectGroup.LoadFromNode(objectGroupNode);
                ObjectGroups.Add(layer.Name, layer);
            }
        }
        #endregion

        #region Save Functions
        public void Save(string filePath)
        {
            using FileStream file = File.Create(filePath);
            Save(file);
        }

        public void Save(Stream outputStream)
        {
            XmlDocument tiledDocument = new();

            XmlNode mapNode = createMapNode(tiledDocument);

            saveProperties(mapNode);
            saveTilesets(mapNode);
            saveTileLayers(mapNode);
            saveObjectGroups(mapNode);

            tiledDocument.AppendChild(mapNode);
            tiledDocument.Save(outputStream);
        }

        private XmlNode createMapNode(XmlDocument tiledDocument)
        {
            XmlNode mapNode = tiledDocument.CreateElement("map");
            mapNode.AddAttribute("version", TargetVersion);
            mapNode.AddAttribute("tiledversion", TargetTiledVersion);
            mapNode.AddAttribute("orientation", "orthogonal");
            mapNode.AddAttribute("renderorder", "right-down");
            mapNode.AddAttribute("width", Width);
            mapNode.AddAttribute("height", Height);
            mapNode.AddAttribute("tilewidth", TileWidth);
            mapNode.AddAttribute("tileheight", TileHeight);
            mapNode.AddAttribute("infinite", 0);
            mapNode.AddAttribute("nextlayerid", NextLayerID);
            mapNode.AddAttribute("nextobjectid", NextObjectID);
            return mapNode;
        }

        private void saveProperties(XmlNode mapNode)
        {
            XmlNode propertiesNode = mapNode.OwnerDocument!.CreateElement("properties");
            Properties.Save(propertiesNode);
            mapNode.AppendChild(propertiesNode);
        }

        private void saveTilesets(XmlNode mapNode)
        {
            foreach (TiledMapTileset tileset in Tilesets)
                tileset.SaveToNode(mapNode);
        }

        private void saveTileLayers(XmlNode mapNode)
        {
            foreach (TiledMapTileLayer layer in TileLayers.Values)
                layer.SaveToNode(mapNode);
        }

        private void saveObjectGroups(XmlNode mapNode)
        {
            foreach (TiledMapObjectGroup objectGroup in ObjectGroups.Values)
                objectGroup.SaveToNode(mapNode);
        }
        #endregion
    }
}
