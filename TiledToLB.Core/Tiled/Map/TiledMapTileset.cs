using LiruGameHelper.XML;
using System.Xml;

namespace TiledToLB.Core.Tiled.Map
{
    public struct TiledMapTileset
    {
        #region Properties
        public int FirstGID { get; set; }

        public string Source { get; set; }
        #endregion

        #region Load Functions
        public static TiledMapTileset LoadFromNode(XmlNode node) => new()
        {
            FirstGID = int.TryParse(node.Attributes?["firstgid"]?.Value, out int result) ? result : 1,
            Source = node.Attributes?["source"]?.Value ?? string.Empty,
        };
        #endregion

        #region Save Functions
        public void SaveToNode(XmlNode parentNode)
        {
            XmlNode node = parentNode.OwnerDocument!.CreateElement("tileset");

            node.AddAttribute("firstgid", FirstGID);
            node.AddAttribute("source", Source);

            parentNode.AppendChild(node);
        }
        #endregion
    }
}
