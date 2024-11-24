using LiruGameHelper.XML;
using System.Net.NetworkInformation;
using System.Xml;
using TiledToLB.Core.Tiled.Map;

namespace TiledToLB.Core.Tiled.Tileset
{
    public class TiledTileset
    {
        #region Properties
        public string Version { get; set; } = string.Empty;

        public string TiledVersion { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public int TileWidth { get; set; }

        public int TileHeight { get; set; }

        public int TileCount { get; set; }

        public int Columns { get; set; }

        public List<TiledTilesetTile> Tiles { get; } = [];

        public string SourceImagePath { get; set; } = string.Empty;

        public int SourceImageWidth { get; set; }

        public int SourceImageHeight { get; set; }
        #endregion

        #region Tile Functions
        public void AddTile(TiledTilesetTile tile)
        {
            Tiles.Add(tile);
            TileCount++;
        }
        #endregion

        #region Load Functions
        public static TiledTileset LoadFromPaths(string mapPath, string tilesetPath)
        {
            if (Path.GetDirectoryName(mapPath) is string mapDirectory)
                return LoadFromFile(Path.Combine(mapDirectory, tilesetPath));
            else
                return LoadFromFile(tilesetPath);
        }

        public static TiledTileset LoadFromFile(string filePath)
        {
            using FileStream file = File.OpenRead(filePath);
            return LoadFromStream(file);
        }

        public static TiledTileset LoadFromStream(Stream stream)
        {
            XmlDocument tilesetFile = new();
            tilesetFile.Load(stream);

            XmlNode tilesetNode = tilesetFile.SelectSingleNode("/tileset") ?? throw new InvalidDataException("Tileset file has missing tileset node!");

            XmlNode? sourceImageNode = tilesetNode.SelectSingleNode("image");

            TiledTileset tileset = new()
            {
                Version = tilesetNode.Attributes?["version"]?.Value ?? string.Empty,
                TiledVersion = tilesetNode.Attributes?["tiledversion"]?.Value ?? string.Empty,
                Name = tilesetNode.Attributes?["name"]?.Value ?? string.Empty,
                TileWidth = int.TryParse(tilesetNode.Attributes?["tilewidth"]?.Value, out int tileWidth) ? tileWidth : 0,
                TileHeight = int.TryParse(tilesetNode.Attributes?["tileheight"]?.Value, out int tileHeight) ? tileHeight : 0,
                TileCount = int.TryParse(tilesetNode.Attributes?["tilecount"]?.Value, out int tileCount) ? tileCount : 0,
                Columns = int.TryParse(tilesetNode.Attributes?["columns"]?.Value, out int columns) ? columns : 0,

                SourceImagePath = sourceImageNode?.Attributes?["source"]?.Value ?? string.Empty,
                SourceImageWidth = int.TryParse(sourceImageNode?.Attributes?["width"]?.Value, out int sourceImageWidth) ? sourceImageWidth : 0,
                SourceImageHeight = int.TryParse(sourceImageNode?.Attributes?["height"]?.Value, out int sourceImageHeight) ? sourceImageHeight : 0,
            };

            tileset.loadTiles(tilesetNode);
            return tileset;
        }

        private void loadTiles(XmlNode tilesetNode)
        {
            Tiles.Capacity = TileCount;

            XmlNodeList? tileNodes = tilesetNode.SelectNodes("tile");
            if (tileNodes == null)
            {
                if (TileCount != 0)
                    throw new InvalidDataException($"Tileset says it has {TileCount} tiles, yet there are no tile nodes!");
                else
                    return;
            }

            if (TileCount != tileNodes.Count)
                throw new InvalidDataException($"Tileset says it has {TileCount} tiles, yet there are {tileNodes.Count} tile nodes!");

            foreach (XmlNode tileNode in tileNodes)
            {
                TiledTilesetTile tile = TiledTilesetTile.LoadFromNode(tileNode);
                Tiles.Add(tile);
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
            XmlDocument tilesetDocument = new();

            XmlNode tilesetNode = createTilesetNode(tilesetDocument);

            createTransformationsNode(tilesetNode);
            createSourceImageNode(tilesetNode);
            saveTiles(tilesetNode);

            tilesetDocument.AppendChild(tilesetNode);
            tilesetDocument.Save(outputStream);
        }

        private XmlNode createTilesetNode(XmlDocument tilesetDocument)
        {
            XmlNode tilesetNode = tilesetDocument.CreateElement("tileset");
            tilesetNode.AddAttribute("version", TiledMap.TargetVersion);
            tilesetNode.AddAttribute("tiledversion", TiledMap.TargetTiledVersion);
            tilesetNode.AddAttribute("name", Name);
            tilesetNode.AddAttribute("tilewidth", TileWidth);
            tilesetNode.AddAttribute("tileheight", TileHeight);
            tilesetNode.AddAttribute("tilecount", TileCount);
            tilesetNode.AddAttribute("columns", Columns);
            return tilesetNode;
        }

        private XmlNode createTransformationsNode(XmlNode tilesetNode)
        {
            XmlNode transformationsNode = tilesetNode.OwnerDocument!.CreateElement("transformations");

            transformationsNode.AddAttribute("hflip", 0);
            transformationsNode.AddAttribute("vflip", 0);
            transformationsNode.AddAttribute("rotate", 0);
            transformationsNode.AddAttribute("preferuntransformed", 1);

            tilesetNode.AppendChild(transformationsNode);
            return transformationsNode;
        }

        private XmlNode createSourceImageNode(XmlNode tilesetNode)
        {
            XmlNode sourceImageNode = tilesetNode.OwnerDocument!.CreateElement("image");
            if (string.IsNullOrWhiteSpace(SourceImagePath))
                return sourceImageNode;

            sourceImageNode.AddAttribute("source", SourceImagePath);
            sourceImageNode.AddAttribute("width", SourceImageWidth);
            sourceImageNode.AddAttribute("height", SourceImageHeight);

            tilesetNode.AppendChild(sourceImageNode);
            return sourceImageNode;
        }

        private void saveTiles(XmlNode tilesetNode)
        {
            foreach (TiledTilesetTile tile in Tiles)
                tile.SaveToNode(tilesetNode);
        }
        #endregion
    }
}
