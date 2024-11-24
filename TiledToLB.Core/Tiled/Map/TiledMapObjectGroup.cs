using LiruGameHelper.XML;
using System.Xml;

namespace TiledToLB.Core.Tiled.Map
{
    public class TiledMapObjectGroup
    {
        #region Properties
        public int ID { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool Visible { get; set; } = true;

        public List<TiledMapObject> Objects { get; } = [];
        #endregion

        #region Constructors
        public TiledMapObjectGroup() { }

        public TiledMapObjectGroup(string name, bool visible)
        {
            Name = name;
            Visible = visible;
        }
        #endregion

        #region Load Functions
        public static TiledMapObjectGroup LoadFromNode(XmlNode node)
        {
            TiledMapObjectGroup objectGroup = new()
            {
                ID = int.TryParse(node.Attributes?["id"]?.Value, out int result) ? result : throw new ArgumentException("Object layer is missing valid id!", nameof(node)),
                Name = node.Attributes?["name"]?.Value ?? throw new ArgumentException("Object layer is missing valid name!", nameof(node)),
                Visible = node.Attributes?["visible"]?.Value != "0",
            };

            XmlNodeList? objectNodes = node.SelectNodes("object");
            if (objectNodes == null)
                return objectGroup;

            foreach (XmlNode objectNode in objectNodes)
            {
                TiledMapObject mapObject = TiledMapObject.LoadFromNode(objectNode);
                objectGroup.Objects.Add(mapObject);
            }

            return objectGroup;
        }
        #endregion

        #region Save Functions
        public void SaveToNode(XmlNode parentNode)
        {
            XmlNode node = parentNode.OwnerDocument!.CreateElement("objectgroup");

            node.AddAttribute("id", ID);
            node.AddAttribute("name", Name);
            node.AddAttribute("visible", Visible ? 1 : 0);

            foreach (TiledMapObject mapObject in Objects)
                mapObject.SaveToNode(node);

            parentNode.AppendChild(node);
        }
        #endregion
    }
}
