using LiruGameHelper.XML;
using System.Text;
using System.Xml;
using TiledToLB.Core.Tiled.Property;

namespace TiledToLB.Core.Tiled.Map
{
    public class TiledMapObject()
    {
        #region Properties
        public int ID { get; set; }

        public string? Name { get; set; }

        public string? Type { get; set; }

        public float X { get; set; }

        public float Y { get; set; }

        public float? Width { get; set; } = null;

        public float? Height { get; set; } = null;

        public TiledPropertyCollection Properties { get; } = [];

        public List<float> PolylinePoints { get; } = [];
        #endregion

        #region Polyline Functions
        public void AddPolylinePoint(float x, float y)
        {
            PolylinePoints.Add(x);
            PolylinePoints.Add(y);
        }
        #endregion

        #region Load Functions
        public static TiledMapObject LoadFromNode(XmlNode node)
        {
            TiledMapObject tiledMapObject = new()
            {
                ID = int.TryParse(node.Attributes?["id"]?.Value, out int result) ? result : throw new ArgumentException("Object is missing valid id!", nameof(node)),
                Name = node.Attributes?["name"]?.Value,
                Type = node.Attributes?["type"]?.Value,
                X = float.TryParse(node.Attributes?["x"]?.Value, out float floatResult) ? floatResult : throw new ArgumentException("Object is missing valid x position!", nameof(node)),
                Y = float.TryParse(node.Attributes?["y"]?.Value, out floatResult) ? floatResult : throw new ArgumentException("Object is missing valid y position!", nameof(node)),
                Width = float.TryParse(node.Attributes?["width"]?.Value, out floatResult) ? floatResult : null,
                Height = float.TryParse(node.Attributes?["height"]?.Value, out floatResult) ? floatResult : null,
            };

            XmlNodeList? propertyNodes = node.SelectNodes("properties/property");
            tiledMapObject.Properties.Load(propertyNodes);

            XmlNode? polylinePointsNode = node.SelectSingleNode("polyline");
            if (polylinePointsNode != null && polylinePointsNode.GetAttributeValue("points") is string polylinePointsString)
            {
                string[] coordPairs = polylinePointsString.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                foreach (string coordPair in coordPairs)
                {
                    string[] coords = coordPair.Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (coords.Length != 2 || !float.TryParse(coords[0], out float x) || !float.TryParse(coords[1], out float y))
                        throw new InvalidDataException($"Polyline of object {tiledMapObject.ID} has invalid coords!");

                    tiledMapObject.AddPolylinePoint(x, y);
                }
            }

            return tiledMapObject;
        }
        #endregion

        #region Save Functions
        public void SaveToNode(XmlNode parentNode)
        {
            XmlNode node = parentNode.OwnerDocument!.CreateElement("object");

            node.AddAttribute("id", ID);
            if (!string.IsNullOrWhiteSpace(Name))
                node.AddAttribute("name", Name);
            if (!string.IsNullOrWhiteSpace(Type))
                node.AddAttribute("type", Type);

            node.AddAttribute("x", X);
            node.AddAttribute("y", Y);
            if (Width != null && Height != null)
            {
                node.AddAttribute("width", Width);
                node.AddAttribute("height", Height);
            }
            else
                node.AppendChild(node.OwnerDocument!.CreateElement("point"));

            if (Properties.Count > 0)
            {
                XmlNode propertiesNode = parentNode.OwnerDocument!.CreateElement("properties");
                Properties.Save(propertiesNode);
                node.AppendChild(propertiesNode);
            }

            if (PolylinePoints.Count > 0)
            {
                if (PolylinePoints.Count % 2 != 0)
                    throw new InvalidDataException("Polyline points must be in pairs!");

                StringBuilder polylineStringBuilder = new(PolylinePoints.Count * 3);
                for (int i = 0; i < PolylinePoints.Count; i += 2)
                {
                    polylineStringBuilder.Append(PolylinePoints[i]);
                    polylineStringBuilder.Append(',');
                    polylineStringBuilder.Append(PolylinePoints[i + 1]);
                    polylineStringBuilder.Append(' ');
                }
                polylineStringBuilder.Remove(polylineStringBuilder.Length - 1, 1);

                XmlNode polylineNode = parentNode.OwnerDocument!.CreateElement("polyline");
                polylineNode.AddAttribute("points", polylineStringBuilder.ToString());
                node.AppendChild(polylineNode);
            }

            parentNode.AppendChild(node);
        }
        #endregion
    }
}
