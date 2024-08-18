using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace TiledToLB.Tilemap
{
    internal class Tileset
    {
        #region Backing Fields
        private readonly TilesetTile[] tilesetData;
        #endregion

        #region Properties
        public IReadOnlyList<TilesetTile> TilesetData => tilesetData;

        public uint FirstIndex { get; }
        #endregion

        #region Constructors
        public Tileset()
        {
            tilesetData = Array.Empty<TilesetTile>();
            FirstIndex = 0;
        }

        private Tileset(TilesetTile[] tilesetData, uint firstIndex)
        {
            this.tilesetData = tilesetData ?? throw new ArgumentNullException(nameof(tilesetData));
            FirstIndex = firstIndex;
        }
        #endregion

        #region Load Functions
        public static Tileset LoadFromTiledTileset(XmlDocument tilesetFile, uint firstIndex)
        {
            if (!int.TryParse(tilesetFile.SelectSingleNode("/tileset")?.Attributes?["tilecount"]?.Value, out int count))
                throw new Exception("Tileset file had invalid or missing tile count!");

            TilesetTile[] tilesetData = new TilesetTile[count];
            foreach (XmlNode tileNode in tilesetFile.SelectNodes("/tileset/tile") ?? throw new Exception("Tileset file had missing tiles!"))
            {
                TilesetTile tile = TilesetTile.LoadFromNode(tileNode);
                tilesetData[tile.Index] = tile;
            }

            return new(tilesetData, firstIndex);
        }
        #endregion
    }
}
