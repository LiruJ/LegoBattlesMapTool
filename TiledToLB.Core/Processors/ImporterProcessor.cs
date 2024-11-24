using ContentUnpacker.Decompressors;
using ContentUnpacker.NDSFS;
using TiledToLB.Core.LegoBattles;

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

        public static async Task ImportMapFromRomAsync(string romInputFilePath, string workspacePath, string romMapName, string outputMapName, bool silent = true)
        {
            if (string.IsNullOrWhiteSpace(romInputFilePath) || !File.Exists(romInputFilePath))
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
            string mapPath = "Maps/" + Path.ChangeExtension(romMapName, "map");
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

            // Get the map's tile NDS file.
            string mapBPPath = "BP/DetailTiles_" + Path.ChangeExtension(romMapName, "tbp");
            if (!fileSystem.FilesByPath.TryGetValue(mapBPPath, out NDSFile? mapBPFileEntry) && !silent)
                Console.WriteLine($"Could not find any blueprints file under {mapBPPath}, skipping extra tiles");

            MemoryStream? mapBPStream = null;
            if (mapBPFileEntry != null)
            {
                mapBPStream = new MemoryStream(0x2000);
                await LegoDecompressor.Decode(romStream, mapBPStream, mapBPFileEntry);
                mapBPStream.Position = 0;
            }

            legoMapStream.Position = 0;
            LegoMapReader.CreateTiledMapFromLegoMap(legoMapStream, mapBPStream, workspacePath, outputMapName, silent);
            mapBPStream?.Dispose();
        }
    }
}
