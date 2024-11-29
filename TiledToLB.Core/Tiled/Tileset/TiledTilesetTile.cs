using LiruGameHelper.XML;
using System.Xml;
using TiledToLB.Core.Tiled.Property;

namespace TiledToLB.Core.Tiled.Tileset
{
    public struct TiledTilesetTile(int id, string? type)
    {
        #region Properties
        public int ID { get; set; } = id;

        public string? Type { get; set; } = type;

        public TiledPropertyCollection Properties { get; } = [];
        #endregion

        #region Load Functions
        public static TiledTilesetTile LoadFromNode(XmlNode node)
        {
            int id = int.TryParse(node.Attributes?["id"]?.Value, out int result) ? result : throw new ArgumentException("Tileset tile is missing valid id!", nameof(node));
            string? type = node.Attributes?["type"]?.Value;

            TiledTilesetTile tile = new(id, type);

            XmlNodeList? propertyNodes = node.SelectNodes("properties/property");
            tile.Properties.Load(propertyNodes);

            return tile;
        }
        #endregion

        #region Save Functions
        public readonly void SaveToNode(XmlNode parentNode)
        {
            XmlNode node = parentNode.OwnerDocument!.CreateElement("tile");

            node.AddAttribute("id", ID);
            if (Type != null)
                node.AddAttribute("type", Type);

            if (Properties.Count > 0)
            {
                XmlNode propertiesNode = parentNode.OwnerDocument!.CreateElement("properties");
                Properties.Save(propertiesNode);
                node.AppendChild(propertiesNode);
            }

            parentNode.AppendChild(node);
        }
        #endregion
    }
}
