using ContentUnpacker.Decompressors;
using System.IO.Compression;
using System.Xml;
using TiledToLB.Core.Tilemap;

namespace TiledToLB.Core.Processors
{
    public static class ExporterProcessor
    {
        private static async Task<(Map map, string outputMapPath)> loadAndSaveMap(string inputFilePath, string outputFilePath)
        {
            XmlDocument tiledFile = new();
            tiledFile.Load(inputFilePath);

            // Load the map.
            Tuple<byte, byte> mapDimensions = Map.LoadMapDimensionsFromTiled(tiledFile);
            Map map = new(mapDimensions.Item1, mapDimensions.Item2);
            map.LoadFromTiled(tiledFile, inputFilePath);

            // Save the map to a memory stream.
            using MemoryStream mapStream = new(0x8000);
            map.SaveToStream(mapStream);

            // Compress the memory stream and save the result to the output file.
            mapStream.Position = 0;
            string outputMapPath = Path.ChangeExtension(outputFilePath, "map");
            await LegoDecompressor.Encode(mapStream, outputMapPath, LZXEncodeType.EWB, 4096);

            return (map, outputMapPath);
        }

        private static async Task<IEnumerable<string>> saveMinimaps(Map map, string outputFilePath)
        {
            List<string> minimapFilePaths = new(4);
            minimapFilePaths.AddRange(await MinimapGenerator.Save(map, outputFilePath, false));
            minimapFilePaths.AddRange(await MinimapGenerator.Save(map, outputFilePath, true));

            return minimapFilePaths;
        }

        public static async Task ProcessMapAsync(string inputFilePath, string outputFilePath)
        {
            Map map = (await loadAndSaveMap(inputFilePath, outputFilePath)).map;
            await saveMinimaps(map, outputFilePath);
        }

        public static async Task ProcessAndPackLBZAsync(string inputFilePath, string outputFilePath)
        {
            (Map map, string outputMapPath) = await loadAndSaveMap(inputFilePath, outputFilePath);
            IEnumerable<string> minimapFilePaths = await saveMinimaps(map, outputFilePath);

            string archiveFilePath;
            if (Path.GetDirectoryName(outputFilePath) is string outputDirectory)
                archiveFilePath = Path.Combine(outputDirectory, map.Name);
            else
                archiveFilePath = map.Name;

            using FileStream lbzFile = File.Create(Path.ChangeExtension(archiveFilePath, "lbz"));
            using ZipArchive lbzArchive = new(lbzFile, ZipArchiveMode.Create);

            // Add the map to the archive.
            lbzArchive.CreateEntryFromFile(outputMapPath, "map.map");

            // Add the minimaps to the archive.
            foreach (string minimapFile in minimapFilePaths)
            {
                string mapName = minimapFile[minimapFile.LastIndexOf("mini")..];
                lbzArchive.CreateEntryFromFile(minimapFile, $"mapimages\\@{mapName}");
            }

            ZipArchiveEntry manifestEntry = lbzArchive.CreateEntry("manifest.toml");
            using Stream manifestStream = manifestEntry.Open();
            using StreamWriter manifestWriter = new(manifestStream);

            manifestWriter.WriteLine($"type = \"map\"");
            manifestWriter.WriteLine($"name = \"{map.Name}\"");
            manifestWriter.WriteLine($"args = [ \"mp{map.ReplacesMPIndex:00}\" ]");

            File.Delete(outputMapPath);
            foreach (string minimapFile in minimapFilePaths)
                File.Delete(minimapFile);
        }

        public static async Task ProcessMinimapAsync(string inputFilePath, string outputFilePath)
        {
            XmlDocument tiledFile = new();
            tiledFile.Load(inputFilePath);

            Tuple<byte, byte> mapDimensions = Map.LoadMapDimensionsFromTiled(tiledFile);
            Map map = new(mapDimensions.Item1, mapDimensions.Item2);

            map.LoadFromTiled(tiledFile, inputFilePath);

            await saveMinimaps(map, outputFilePath);
        }
    }
}
