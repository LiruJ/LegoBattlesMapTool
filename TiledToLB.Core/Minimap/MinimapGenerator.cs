using ContentUnpacker.Decompressors;
using ContentUnpacker.Tilemaps;
using GlobalShared.Tilemaps;

namespace TiledToLB.Core.Minimap
{
    public static class MinimapGenerator
    {
        #region Constants
        /// <summary>
        /// The magic number of the tile graphic section.
        /// </summary>
        public const uint MagicWord = 0x4E434752;

        /// <summary>
        /// The magic number of the tile graphic data section.
        /// </summary>
        private const uint tileGraphicMagicWord = 0x43484152;

        private const uint footerMagicWord = 0x43504F53;

        public const string NCBRFileExtension = "NCBR";

        public const string NCGRFileExtension = "NCGR";
        #endregion

        #region Path Functions
        public static string CalculateFileName(string outputFilePath, string extension, bool includeTrees)
        {
            string? directoryPathName = Path.GetDirectoryName(outputFilePath);
            string fileName = Path.ChangeExtension(Path.GetFileNameWithoutExtension(outputFilePath) + (includeTrees ? "mini" : "miniNT"), extension);

            return directoryPathName == null
                ? fileName
                : Path.Combine(directoryPathName, fileName);
        }

        public static IEnumerable<(string filePath, bool includeTrees, Func<TilemapReader, Stream, bool, Task> saveFunction)> EnumerateAllMinimapVariations(string outputFilePath, bool compressOutput = true)
        {
            yield return (CalculateFileName(outputFilePath, NCBRFileExtension, true), true, compressOutput ? SaveAndCompressNCBR : SaveUncompressedNCBR);
            yield return (CalculateFileName(outputFilePath, NCBRFileExtension, false), false, compressOutput ? SaveAndCompressNCBR : SaveUncompressedNCBR);
            yield return (CalculateFileName(outputFilePath, NCGRFileExtension, true), true, compressOutput ? SaveAndCompressNCGR : SaveUncompressedNCGR);
            yield return (CalculateFileName(outputFilePath, NCGRFileExtension, false), false, compressOutput ? SaveAndCompressNCGR : SaveUncompressedNCGR);
        }
        #endregion

        #region NCGR Functions
        public static async Task SaveAndCompressNCGR(TilemapReader map, Stream outputStream, bool includeTrees)
        {
            using MemoryStream minimapStream = new(0x2000);
            SaveNCGR(map, minimapStream, includeTrees);
            minimapStream.Position = 0;

            await LegoDecompressor.Encode(minimapStream, outputStream, LZXEncodeType.EVB, 4096);
        }

        public static Task SaveUncompressedNCGR(TilemapReader map, Stream outputStream, bool includeTrees)
        {
            SaveNCGR(map, outputStream, includeTrees);
            return Task.CompletedTask;
        }

        public static void SaveNCGR(TilemapReader map, Stream outputStream, bool includeTrees)
        {
            // Write the data to the stream.
            BinaryWriter writer = new(outputStream);
            writeHeader(writer, true);
            writeTiledData(map, writer, includeTrees);
            writeFooter(writer);
        }

        private static void writeTiledData(TilemapReader map, BinaryWriter writer, bool includeTrees)
        {
            byte currentByte = 0;
            bool hasCurrentByte = false;

            for (int tileY = 0; tileY < 16; tileY++)
                for (int tileX = 0; tileX < 16; tileX++)
                {
                    int pixelX = tileX * 8;
                    int pixelY = tileY * 8;

                    for (int y = pixelY; y < pixelY + 8; y++)
                        for (int x = pixelX; x < pixelX + 8; x++)
                            writePixel(map, writer, x, y, includeTrees, ref hasCurrentByte, ref currentByte);
                }
        }
        #endregion

        #region NCBR Functions
        public static async Task SaveAndCompressNCBR(TilemapReader map, Stream outputStream, bool includeTrees)
        {
            using MemoryStream minimapStream = new(0x2000);
            SaveNCBR(map, minimapStream, includeTrees);
            minimapStream.Position = 0;

            await LegoDecompressor.Encode(minimapStream, outputStream, LZXEncodeType.EVB, 4096);
        }

        public static Task SaveUncompressedNCBR(TilemapReader map, Stream outputStream, bool includeTrees)
        {
            SaveNCBR(map, outputStream, includeTrees);
            return Task.CompletedTask;
        }

        public static void SaveNCBR(TilemapReader map, Stream outputStream, bool includeTrees)
        {
            // Write the data to the stream.
            BinaryWriter writer = new(outputStream);
            writeHeader(writer, false);
            writeSequentialData(map, writer, includeTrees);
            writeFooter(writer);
        }

        private static void writeSequentialData(TilemapReader map, BinaryWriter writer, bool includeTrees)
        {
            byte currentByte = 0;
            bool hasCurrentByte = false;

            for (int y = 0; y < 16 * 8; y++)
                for (int x = 0; x < 16 * 8; x++)
                    writePixel(map, writer, x, y, includeTrees, ref hasCurrentByte, ref currentByte);
        }
        #endregion

        #region Common Save Functions
        private static void writeHeader(BinaryWriter writer, bool isTiled)
        {
            // Write "RGCN" (NCGR).
            writer.Write(MagicWord);

            // Endianess and constant.
            writer.Write((ushort)0xFEFF);
            writer.Write((ushort)0x0101);

            // File and header size.
            writer.Write((uint)0);
            writer.Write((ushort)0x10);

            // Section length.
            writer.Write((ushort)0x2);

            // Write "RAHC" (CHAR).
            writer.Write(tileGraphicMagicWord);

            // Section length.
            writer.Write((uint)0x2020);

            // Width/height. (Always 16x16).
            writer.Write((ushort)0x10);
            writer.Write((ushort)0x10);

            // Bit depth.
            writer.Write((uint)0x3);

            // Empty.
            writer.Write((uint)0x0);

            // Tiled.
            writer.Write((uint)(!isTiled ? 0x1 : 0x0));

            // Tile size in bytes. 16 tiles width/height, 8x8 pixels per tile, 2 pixels per byte.
            writer.Write((uint)(0x80 * 0x80 / 0x2));

            // Header offset.
            writer.Write((uint)0x18);
        }

        private static void writeFooter(BinaryWriter writer)
        {
            // Write "SOPC" (CPOS).
            writer.Write(footerMagicWord);

            writer.Write((uint)0x10);
            writer.Write((uint)0x0);
            writer.Write((uint)0x100010);
        }

        private static void writePixel(TilemapReader map, BinaryWriter writer, int x, int y, bool includeTrees, ref bool hasCurrentByte, ref byte currentByte)
        {
            MinimapTileType value = calculateTileTypePaletteIndex(map, x, y, includeTrees);

            if (hasCurrentByte)
            {
                currentByte |= (byte)((byte)value << 4);
                writer.Write(currentByte);
                currentByte = 0;
            }
            else
                currentByte = (byte)value;
            hasCurrentByte = !hasCurrentByte;
        }

        private static MinimapTileType calculateTileTypePaletteIndex(TilemapReader map, int x, int y, bool includeTrees)
        {
            float xScale = (float)x / (12 * 8);
            float yScale = (float)y / (12 * 8);

            // Use none for anything out of range.
            if (xScale >= 1 || yScale >= 1)
                return MinimapTileType.None;

            int mapX = (int)Math.Clamp(MathF.Floor(map.Width * xScale), 0, map.Width - 1);
            int mapY = (int)Math.Clamp(MathF.Floor(map.Height * yScale), 0, map.Height - 1);
            int mapIndex = mapY * map.Width + mapX;

            // If the tile has a tree and trees are enabled, use a tree/grass tile type based on the pixel's position within the pattern.
            if (includeTrees && map.TileData[mapIndex].HasTree)
            {
                int offsetMapX = mapX + (int)Math.Floor(mapY / 2f) * 2;
                return mapY % 2 == 0
                    ? offsetMapX % 3 != 0 ? MinimapTileType.Tree : MinimapTileType.Grass
                    : offsetMapX % 3 == 1 ? MinimapTileType.Tree : MinimapTileType.Grass;
            }

            // Handle the tile types.
            return map.TileData[mapIndex].TileType switch
            {
                TileType.Grass => calculateTileAndAdjacentIs(map, mapX, mapY, TileType.Grass) ? MinimapTileType.Grass : MinimapTileType.Stone,
                TileType.Water => calculateFromAdjacent(map, mapX, mapY, TileType.Water, MinimapTileType.Water, MinimapTileType.Shore),
                TileType.Mountain => calculateFromAdjacent(map, mapX, mapY, TileType.Mountain, MinimapTileType.Mountain, MinimapTileType.Mountainside),
                _ => MinimapTileType.Stone,
            };
        }

        private static MinimapTileType calculateFromAdjacent(TilemapReader map, int mapX, int mapY, TileType tileType, MinimapTileType centre, MinimapTileType border)
        {
            // If the left and top tiles aren't the given type, it's stone.
            if (!calculateTileAndAdjacentIs(map, mapX, mapY, tileType))
                return MinimapTileType.Stone;

            // The pixel is a border if any of its directly adjacent pixels aren't the given type.
            return calculateTileAndAdjacentIs(map, mapX, mapY == 0 ? 0 : mapY - 1, tileType)
                && calculateTileAndAdjacentIs(map, mapX == 0 ? 0 : mapX - 1, mapY, tileType)
                && calculateTileAndAdjacentIs(map, mapX == map.Width - 1 ? map.Width - 1 : mapX + 1, mapY, tileType)
                && calculateTileAndAdjacentIs(map, mapX, mapY == map.Height - 1 ? map.Height - 1 : mapY + 1, tileType)
                ? centre
                : border;
        }

        private static bool calculateTileAndAdjacentIs(TilemapReader map, int mapX, int mapY, TileType tileType)
        {
            int mapIndex = mapY * map.Width + mapX;
            TileType tileData = map.TileData[mapIndex].TileType;

            TileType aboveTileData = map.TileData[(mapY == 0 ? 0 : (mapY - 1) * map.Width) + mapX].TileType;
            TileType leftTileData = map.TileData[mapY * map.Width + (mapX == 0 ? 0 : mapX - 1)].TileType;
            TileType aboveLeftTileData = map.TileData[(mapY == 0 ? 0 : (mapY - 1) * map.Width) + (mapX == 0 ? 0 : mapX - 1)].TileType;

            return tileData == tileType
                && aboveTileData == tileType
                && leftTileData == tileType
                && aboveLeftTileData == tileType;
        }
        #endregion
    }
}
