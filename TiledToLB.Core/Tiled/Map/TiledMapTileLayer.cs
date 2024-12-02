using LiruGameHelper.XML;
using System.Text;
using System.Xml;

namespace TiledToLB.Core.Tiled.Map
{
    public class TiledMapTileLayer(string name, int width, int height)
    {
        #region Properties
        public int ID { get; set; } = 0;

        public string Name { get; set; } = name;

        public int Width { get; set; } = width;

        public int Height { get; set; } = height;

        public float Opacity { get; set; } = 1f;

        public string Encoding { get; set; } = "csv";

        public int[,] Data { get; } = new int[width, height];
        #endregion

        #region Load Functions
        public static TiledMapTileLayer LoadFromNode(XmlNode node)
        {
            int id = int.TryParse(node.Attributes?["id"]?.Value, out int result) ? result : throw new ArgumentException("Layer node is missing valid id!", nameof(node));
            string name = node.Attributes?["name"]?.Value ?? throw new ArgumentException("Layer node is missing valid name!", nameof(node));
            int width = int.TryParse(node.Attributes?["width"]?.Value, out result) ? result : throw new ArgumentException("Layer node is missing valid width!", nameof(node));
            int height = int.TryParse(node.Attributes?["height"]?.Value, out result) ? result : throw new ArgumentException("Layer node is missing valid width!", nameof(node));
            float opacity = float.TryParse(node.Attributes?["opacity"]?.Value, out float opacityResult) ? opacityResult : 1.0f;

            XmlNode dataNode = node.SelectSingleNode("data") ?? throw new ArgumentException("Layer node is missing data child node!", nameof(node));
            string encoding = dataNode.Attributes?["encoding"]?.Value ?? "csv";

            if (encoding != "csv")
                throw new InvalidDataException("Cannot load non-csv layer ID: {id} \"{name}\"!");

            TiledMapTileLayer layer = new(name, width, height)
            {
                ID = id,
                Opacity = opacity,
                Encoding = encoding,
            };

            string[] dataStrings = dataNode.InnerText.Split(',', StringSplitOptions.TrimEntries);
            for (int i = 0; i < dataStrings.Length; i++)
            {
                int x = i % width, y = i / width;
                string dataString = dataStrings[i];
                if (!int.TryParse(dataString, out int index))
                    throw new Exception($"Data value at {x},{y} on layer ID: {id} \"{name}\" is invalid!");

                layer.Data[x, y] = index;
            }

            return layer;
        }
        #endregion

        #region Save Functions
        public void SaveToNode(XmlNode parentNode)
        {
            XmlNode node = parentNode.OwnerDocument!.CreateElement("layer");

            node.AddAttribute("id", ID);
            node.AddAttribute("name", Name);
            node.AddAttribute("opacity", Opacity);
            node.AddAttribute("width", Width);
            node.AddAttribute("height", Height);

            StringBuilder dataStringBuilder = new(Width * Height * 3);
            for (int y = 0; y < Height; y++)
            {
                dataStringBuilder.AppendLine();
                for (int x = 0; x < Width; x++)
                {
                    dataStringBuilder.Append(Data[x, y]);
                    dataStringBuilder.Append(',');
                }
            }
            dataStringBuilder.Remove(dataStringBuilder.Length - 1, 1);

            XmlNode dataNode = parentNode.OwnerDocument!.CreateElement("data");
            dataNode.AddAttribute("encoding", Encoding);
            dataNode.InnerText = dataStringBuilder.ToString();
            node.AppendChild(dataNode);

            parentNode.AppendChild(node);
        }
        #endregion
    }
}
