using LiruGameHelper.XML;
using System.Xml;

namespace TiledToLB.Core.Tiled.Tileset
{
    public struct TiledTilesetTile(int id, string? type)
    {
        #region Properties
        public int ID { get; set; } = id;

        public string? Type { get; set; } = type;

        public Dictionary<string, TiledProperty> Properties { get; } = [];
        #endregion

        #region Property Functions
        public readonly void AddProperty(string name, string value)
            => Properties.Add(name, new TiledProperty(name, value, TiledPropertyType.String, null));

        public readonly void AddProperty(string name, int value)
            => Properties.Add(name, new TiledProperty(name, value.ToString(), TiledPropertyType.Int, null));

        public readonly void AddProperty(string name, float value)
            => Properties.Add(name, new TiledProperty(name, value.ToString(), TiledPropertyType.Float, null));

        public readonly void AddProperty(string name, bool value)
            => Properties.Add(name, new TiledProperty(name, value.ToString().ToLower(), TiledPropertyType.Bool, null));

        public readonly void AddProperty(TiledProperty property)
            => Properties.Add(property.Name, property);
        #endregion

        #region Load Functions
        public static TiledTilesetTile LoadFromNode(XmlNode node)
        {
            int id = int.TryParse(node.Attributes?["id"]?.Value, out int result) ? result : throw new ArgumentException("Tileset tile is missing valid id!", nameof(node));
            string? type = node.Attributes?["type"]?.Value;

            TiledTilesetTile tile = new(id, type);

            XmlNodeList? propertyNodes = node.SelectNodes("properties/property");
            if (propertyNodes != null)
                foreach (XmlNode propertyNode in propertyNodes)
                {
                    TiledProperty property = TiledProperty.LoadFromNode(propertyNode);
                    tile.Properties.Add(property.Name, property);
                }

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
                foreach (TiledProperty property in Properties.Values)
                    property.SaveToNode(propertiesNode);
                node.AppendChild(propertiesNode);
            }

            parentNode.AppendChild(node);
        }
        #endregion
    }
}
