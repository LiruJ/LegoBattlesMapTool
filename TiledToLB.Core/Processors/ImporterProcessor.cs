using ContentUnpacker.Decompressors;
using ContentUnpacker.NDSFS;

namespace TiledToLB.Core.Processors
{
    public static class ImporterProcessor
    {
        public static async Task ImportMapAsync(string inputFilePath, string outputFilePath)
        {
            if (string.IsNullOrWhiteSpace(inputFilePath))
            {
                Console.WriteLine("Missing input");
                return;
            }

            if (!File.Exists(inputFilePath))
            {
                Console.WriteLine("File does not exist!");
                return;
            }

            //await LegoDecompressor.DecompressFileAsync(inputFilePath, decompressedMapFilePath, temporaryDirectoryName);
        }

        public static async Task ImportMapFromRomAsync(string romInputFilePath, string workspacePath, string mapName, bool silent = true)
        {
            if (string.IsNullOrWhiteSpace (romInputFilePath) || !File.Exists(romInputFilePath))
            {
                Console.WriteLine("ROM filepath was empty or file was not found!");
                return;
            }

            // Read the rom file system.
            if (!silent)
                Console.WriteLine("Reading ROM file system");
            using FileStream romStream = File.OpenRead(romInputFilePath);
            NDSFileSystem fileSystem = NDSFileSystem.LoadFromRom(romStream);

            // Get the map NDS file.
            string mapPath = "Maps/" + Path.ChangeExtension(mapName, "map");
            if (!fileSystem.FilesByPath.TryGetValue(mapPath, out NDSFile? mapFileEntry))
            {
                Console.WriteLine($"Could not find map file under {mapPath}");
                return;
            }

            // Load and decompress the map from the rom.
            if (!silent)
                Console.WriteLine("Found map, decompressing");
            using MemoryStream legoMapStream = new(0x8000);
            await LegoDecompressor.Decode(romStream, legoMapStream, mapFileEntry);
            if (!silent)
                Console.WriteLine($"Decompressed map. Compressed/decompressed size: 0x{mapFileEntry.Size:X6}/0x{legoMapStream.Length:X6} ({legoMapStream.Length / (float)mapFileEntry.Size:P0} bigger)");
        }
    }
}
