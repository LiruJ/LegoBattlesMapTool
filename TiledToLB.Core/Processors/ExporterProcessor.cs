﻿using ContentUnpacker.Tilemaps;
using System.IO.Compression;
using TiledToLB.Core.LegoBattles;
using TiledToLB.Core.Minimap;
using TiledToLB.Core.Tiled.Map;
using TiledToLB.Core.Tiled.Property;

namespace TiledToLB.Core.Processors
{
    public static class ExporterProcessor
    {
        public static async Task ProcessMapAsync(string inputFilePath, string outputFilePath, bool compressOutput = true, bool silent = true)
        {
            // Load the map.
            TiledMap map = TiledMap.Load(inputFilePath);

            // Create the output file and save the map to it.
            using FileStream outputStream = File.Create(outputFilePath);
            LegoTilemap legoMap = await LegoMapWriter.CreateLegoMapFromTiledMap(map, inputFilePath, outputStream, compressOutput, silent);

            if (!silent)
                Console.WriteLine("Saving minimaps");

            // Save each minimap.
            foreach ((string filePath, bool includeTrees, Func<LegoTilemap, Stream, bool, Task> saveFunction) in MinimapGenerator.EnumerateAllMinimapVariations(outputFilePath, compressOutput))
            {
                using FileStream minimapOutputStream = File.Create(filePath);
                await saveFunction(legoMap, minimapOutputStream, includeTrees);
            }

            if (!silent)
                Console.WriteLine("All files saved successfully");
        }

        public static async Task ProcessAndPackLBZAsync(string inputFilePath, string outputFilePath, bool silent = true)
        {
            // Load the map.
            TiledMap map = TiledMap.Load(inputFilePath);

            string mapName = Path.GetFileNameWithoutExtension(outputFilePath);
            string? outputDirectoryPath = Path.GetDirectoryName(outputFilePath);

            // Calculate the archive path.
            string archiveFilePath = outputDirectoryPath != null ? Path.Combine(outputDirectoryPath, mapName) : mapName;

            // Create the archive.
            using FileStream lbzFile = File.Create(Path.ChangeExtension(archiveFilePath, "lbz"));
            using ZipArchive lbzArchive = new(lbzFile, ZipArchiveMode.Create);

            // Save the map to a stream.
            using MemoryStream mapOutputStream = new(0x6000);
            LegoTilemap legoMap = await LegoMapWriter.CreateLegoMapFromTiledMap(map, inputFilePath, mapOutputStream, true, silent);

            // Add the map to the archive.
            ZipArchiveEntry mapEntry = lbzArchive.CreateEntry("map.map");
            using Stream mapEntryStream = mapEntry.Open();
            mapOutputStream.Position = 0;
            mapOutputStream.CopyTo(mapEntryStream);
            mapEntryStream.Close();

            // Add each minimap to the archive.
            foreach ((string filePath, bool includeTrees, Func<LegoTilemap, Stream, bool, Task> saveFunction) in MinimapGenerator.EnumerateAllMinimapVariations("mapimages/@"))
            {
                ZipArchiveEntry minimapEntry = lbzArchive.CreateEntry(filePath);
                using Stream minimapEntryStream = minimapEntry.Open();

                using MemoryStream minimapOutputStream = new(0x4000);
                await saveFunction(legoMap, minimapOutputStream, includeTrees);
                minimapOutputStream.Position = 0;
                minimapOutputStream.CopyTo(minimapEntryStream);
                minimapEntryStream.Close();
            }

            // Add the manifest file to the archive.
            ZipArchiveEntry manifestEntry = lbzArchive.CreateEntry("manifest.toml");
            using Stream manifestEntryStream = manifestEntry.Open();
            using StreamWriter manifestWriter = new(manifestEntryStream);

            // Write the manifest file.
            manifestWriter.WriteLine($"type = \"map\"");
            manifestWriter.WriteLine($"name = \"{(map.Properties.TryGetValue("Name", out TiledProperty nameProperty) ? nameProperty.Value : mapName)}\"");
            manifestWriter.WriteLine($"args = [ \"mp{(map.Properties.TryGetValue("ReplacesMPIndex", out TiledProperty mpIndexProperty) ? int.Parse(mpIndexProperty.Value) : 0):00}\" ]");
        }
    }
}
