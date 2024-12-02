using LiruGameHelper.XML;
using System.Text;
using System.Xml;

namespace TiledToLB.Core.Tiled.Map
{
    public class TiledMapTileLayer()
    {
        #region Properties
        public int ID { get; set; } = 0;

        public required string Name { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public float Opacity { get; set; } = 1f;

        public string Encoding { get; set; } = "csv";

        public required int[,] Data { get; init; }
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

            string[] dataStrings = dataNode.InnerText.Split(',', StringSplitOptions.TrimEntries);
            int[,] data = new int[width, height];
            for (int i = 0; i < dataStrings.Length; i++)
            {
                int x = i % width, y = i / width;
                string dataString = dataStrings[i];
                if (!int.TryParse(dataString, out int index))
                    throw new Exception($"Data value at {x},{y} on layer ID: {id} \"{name}\" is invalid!");

                data[x, y] = index;
            }

            return new()
            {
                Name = name,
                ID = id,
                Width = width,
                Height = height,
                Opacity = opacity,
                Encoding = encoding,
                Data = data
            };
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
