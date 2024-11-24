using GlobalShared.Tilemaps;
using System.Xml;

namespace TiledToLB.Core.Tilemap
{
    public readonly struct TilesetTile(ushort index, TileType tileType)
    {
        #region Properties
        public ushort Index { get; } = index;

        public TileType TileType { get; } = tileType;
        #endregion

        #region Load Functions
        public static TilesetTile LoadFromNode(XmlNode tileNode)
        {
            if (!ushort.TryParse(tileNode.Attributes?["id"]?.Value, out ushort index))
                throw new Exception("Tile node is missing an id!");

            XmlNode? typeNode = tileNode.SelectSingleNode("properties/property[@name='Type']");
            TileType tileType = byte.TryParse(typeNode?.Attributes?["value"]?.Value, out byte tileTypeValue) ? (TileType)tileTypeValue : TileType.Grass;

            return new(index, tileType);
        }
        #endregion
    }
}
