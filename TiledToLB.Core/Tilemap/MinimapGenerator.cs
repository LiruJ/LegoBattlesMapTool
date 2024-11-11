using ContentUnpacker.Decompressors;

namespace TiledToLB.Core.Tilemap
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

        #region Save Functions
        public static async Task<IEnumerable<string>> Save(Map map, string outputFilePath, string temporaryDirectoryPath, bool includeTrees)
        {
            // Calculate output paths.
            string outputFileName = Path.GetFileNameWithoutExtension(outputFilePath) + (includeTrees ? "mini" : "miniNT");
            string outputDirectory = Path.GetDirectoryName(outputFilePath) ?? "";
            outputFilePath = Path.Combine(outputDirectory, outputFileName);

            return new List<string>()
            {
                await saveNCBR(map, outputFilePath, temporaryDirectoryPath, includeTrees),
                await saveNCGR(map, outputFilePath, temporaryDirectoryPath, includeTrees),
            };
        }

        private static async Task<string> saveNCBR(Map map, string outputFilePath, string temporaryDirectoryPath, bool includeTrees)
        {
            // Create the file.
            string uncompressedFilePath = outputFilePath + "_temp.bin";
            FileStream fileStream = File.Create(uncompressedFilePath);
            BinaryWriter writer = new(fileStream);

            // Write the header.
            writeHeader(writer, false);

            writeSequentialData(map, writer, includeTrees);

            writeFooter(writer);

            writer.Close();

            outputFilePath = Path.ChangeExtension(outputFilePath, NCBRFileExtension);
            await LegoDecompressor.CompressFileAsync(LZXEncodeType.EVB, uncompressedFilePath, outputFilePath, 4096, temporaryDirectoryPath);

            // Delete the uncompressed file, now that it is compressed.
            File.Delete(uncompressedFilePath);

            return outputFilePath;
        }

        private static async Task<string> saveNCGR(Map map, string outputFilePath, string temporaryDirectoryPath, bool includeTrees)
        {
            // Create the file.
            string uncompressedFilePath = outputFilePath + "_temp.bin";
            FileStream fileStream = File.Create(uncompressedFilePath);
            using BinaryWriter writer = new(fileStream);

            // Write the header.
            writeHeader(writer, true);

            writeTiledData(map, writer, includeTrees);

            writeFooter(writer);

            writer.Close();

            outputFilePath = Path.ChangeExtension(outputFilePath, NCGRFileExtension);
            await LegoDecompressor.CompressFileAsync(LZXEncodeType.EVB, uncompressedFilePath, outputFilePath, 4096, temporaryDirectoryPath);

            // Delete the uncompressed file, now that it is compressed.
            File.Delete(uncompressedFilePath);

            return outputFilePath;
        }

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

        private static void writeSequentialData(Map map, BinaryWriter writer, bool includeTrees)
        {
            byte currentByte = 0;
            bool hasCurrentByte = false;

            for (int y = 0; y < 16 * 8; y++)
                for (int x = 0; x < 16 * 8; x++)
                    writePixel(map, writer, x, y, includeTrees, ref hasCurrentByte, ref currentByte);
        }

        private static void writeTiledData(Map map, BinaryWriter writer, bool includeTrees)
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

        private static void writePixel(Map map, BinaryWriter writer, int x, int y, bool includeTrees, ref bool hasCurrentByte, ref byte currentByte)
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

        private static MinimapTileType calculateTileTypePaletteIndex(Map map, int x, int y, bool includeTrees)
        {
            float xScale = (float)x / (12 * 8);
            float yScale = (float)y / (12 * 8);

            // Use none for anything out of range.
            if (xScale >= 1 || yScale >= 1)
                return MinimapTileType.None;

            int mapX = (int)Math.Clamp(MathF.Floor(map.Width * xScale), 0, map.Width - 1);
            int mapY = (int)Math.Clamp(MathF.Floor(map.Height * yScale), 0, map.Height - 1);

            // If the tile has a tree and trees are enabled, use a tree/grass tile type based on the pixel's position within the pattern.
            if (includeTrees && map.TreeLayer[mapX, mapY])
            {
                int offsetMapX = mapX + (int)Math.Floor(mapY / 2f) * 2;
                return mapY % 2 == 0
                    ? offsetMapX % 3 != 0 ? MinimapTileType.Tree : MinimapTileType.Grass
                    : offsetMapX % 3 == 1 ? MinimapTileType.Tree : MinimapTileType.Grass;
            }

            // Handle the tile types.
            return map.DetailLayer[mapX, mapY].TileType switch
            {
                TileType.Grass => calculateTileAndAdjacentIs(map, mapX, mapY, TileType.Grass) ? MinimapTileType.Grass : MinimapTileType.Stone,
                TileType.Water => calculateFromAdjacent(map, mapX, mapY, TileType.Water, MinimapTileType.Water, MinimapTileType.Shore),
                TileType.Mountain => calculateFromAdjacent(map, mapX, mapY, TileType.Mountain, MinimapTileType.Mountain, MinimapTileType.Mountainside),
                _ => MinimapTileType.Stone,
            };
        }

        private static MinimapTileType calculateFromAdjacent(Map map, int mapX, int mapY, TileType tileType, MinimapTileType centre, MinimapTileType border)
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

        private static bool calculateTileAndAdjacentIs(Map map, int mapX, int mapY, TileType tileType)
        {
            TilesetTile tileData = map.DetailLayer[mapX, mapY];
            TilesetTile aboveTileData = map.DetailLayer[mapX, mapY == 0 ? 0 : mapY - 1];
            TilesetTile leftTileData = map.DetailLayer[mapX == 0 ? 0 : mapX - 1, mapY];
            TilesetTile aboveLeftTileData = map.DetailLayer[mapX == 0 ? 0 : mapX - 1, mapY == 0 ? 0 : mapY - 1];

            return tileData.TileType == tileType
                && aboveTileData.TileType == tileType
                && leftTileData.TileType == tileType
                && aboveLeftTileData.TileType == tileType;
        }
        #endregion
    }
}
