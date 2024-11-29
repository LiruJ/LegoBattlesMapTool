using System.Xml;
using LiruGameHelper.XML;

namespace TiledToLB.Core.Tiled.Property
{
    public struct TiledProperty(string name, string value, TiledPropertyType? type, string? propertyType)
    {
        #region Properties
        public string Name { get; set; } = name;

        public string Value { get; set; } = value;

        public TiledPropertyType? Type { get; set; } = type;

        public string? PropertyType { get; set; } = propertyType;
        #endregion

        #region Load Functions
        public static TiledProperty LoadFromNode(XmlNode node)
            => new()
            {
                Name = node.Attributes?["name"]?.Value ?? throw new ArgumentException("Given property node is missing a name!", nameof(node)),
                Value = node.Attributes?["value"]?.Value ?? throw new ArgumentException("Given property node is missing a value!", nameof(node)),
                Type = Enum.TryParse(node.Attributes?["type"]?.Value, true, out TiledPropertyType result) ? result : TiledPropertyType.String,
                PropertyType = node.Attributes?["propertytype"]?.Value,
            };
        #endregion

        #region Save Functions
        public readonly void SaveToNode(XmlNode parentNode)
        {
            XmlNode node = parentNode.OwnerDocument!.CreateElement("property");

            node.AddAttribute("name", Name);
            node.AddAttribute("value", Value);
            if (Type.HasValue)
                node.AddAttribute("type", Type.Value.ToString().ToLower());
            if (PropertyType != null)
                node.AddAttribute("propertytype", PropertyType);

            parentNode.AppendChild(node);
        }
        #endregion
    }
}
