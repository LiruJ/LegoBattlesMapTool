using ContentUnpacker.Decompressors;
using ContentUnpacker.NDSFS;
using ContentUnpacker.Spritesheets;
using GlobalShared.Tilemaps;
using System.IO.Compression;
using System.Xml;
using TiledToLB.Core.Tilemap;

namespace TiledToLB.Core;

public static class Processor
{
    private const string temporaryDirectoryName = "Temporary";

    private static async Task<(Map map, string outputMapPath)> loadAndSaveMap(string inputFilePath, string outputFilePath)
    {
        XmlDocument tiledFile = new();
        tiledFile.Load(inputFilePath);

        Tuple<byte, byte> mapDimensions = Map.LoadMapDimensionsFromTiled(tiledFile);
        Map map = new(mapDimensions.Item1, mapDimensions.Item2);

        map.LoadFromTiled(tiledFile, inputFilePath);

        string? outputDirectory = Path.GetDirectoryName(outputFilePath);
        string uncompressedMapPath = Path.GetFileNameWithoutExtension(outputFilePath) + "_temp.bin";
        if (!string.IsNullOrWhiteSpace(outputDirectory))
            uncompressedMapPath = Path.Combine(outputDirectory, uncompressedMapPath);
        map.Save(uncompressedMapPath);

        string outputMapPath = Path.ChangeExtension(outputFilePath, "map");

        Directory.CreateDirectory(temporaryDirectoryName);
        await LegoDecompressor.CompressFileAsync(LZXEncodeType.EVB, uncompressedMapPath, outputMapPath, 4096, temporaryDirectoryName);
        File.Delete(uncompressedMapPath);
        File.Delete(uncompressedMapPath);

        return (map, outputMapPath);
    }

    private static async Task<IEnumerable<string>> saveMinimaps(Map map, string outputFilePath)
    {
        Directory.CreateDirectory(temporaryDirectoryName);
        List<string> minimapFilePaths = new(4);
        minimapFilePaths.AddRange(await MinimapGenerator.Save(map, outputFilePath, temporaryDirectoryName, false));
        minimapFilePaths.AddRange(await MinimapGenerator.Save(map, outputFilePath, temporaryDirectoryName, true));
        Directory.Delete(temporaryDirectoryName, true);

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

        Directory.CreateDirectory(temporaryDirectoryName);
        await MinimapGenerator.Save(map, outputFilePath, temporaryDirectoryName, false);
        await MinimapGenerator.Save(map, outputFilePath, temporaryDirectoryName, true);
        Directory.Delete(temporaryDirectoryName, true);
    }

    public static async Task UnpackRomAsync(string romInputFilePath, string tiledTemplateOutputPath, bool silent = true)
    {
        // Create the template directory.
        if (Directory.Exists(tiledTemplateOutputPath))
            Directory.Delete(tiledTemplateOutputPath, true);
        if (silent)
            Console.WriteLine($"Deleting existing templates folder at \"{Path.GetFullPath(tiledTemplateOutputPath)}\"");
        Directory.CreateDirectory(tiledTemplateOutputPath);
        if (silent)
            Console.WriteLine($"Created templates folder at \"{Path.GetFullPath(tiledTemplateOutputPath)}\"");

        // Copy over any template files.
        const string templateFilesDirectoryPath = "TemplateFiles";
        foreach (string templateFilePath in Directory.EnumerateFiles(templateFilesDirectoryPath))
            File.Copy(templateFilePath, Path.Combine(tiledTemplateOutputPath, Path.GetFileName(templateFilePath)));

        // Write the version file.
        File.CreateText(Path.Combine(tiledTemplateOutputPath, $"Generated by map tool version {typeof(Processor).Assembly.GetName().Version}"));

        // Read the rom file system.
        if (silent)
            Console.WriteLine("Reading ROM file system");
        NDSFileSystem fileSystem = NDSFileSystem.LoadFromRom(romInputFilePath);
        if (silent)
            Console.WriteLine($"Read ROM file system with {fileSystem.FilesById.Count} files and {fileSystem.DirectoriesById.Count} directories");

        using FileStream romFile = File.OpenRead(romInputFilePath);
        using BinaryReader romReader = new(romFile);

        // Load the tilesets from the rom, save the pngs to the templates folder.
        const string temporaryDirectoryPath = "Temporary";
        Directory.CreateDirectory(temporaryDirectoryPath);
        await loadTileset(fileSystem, romReader, "KingTileset", tiledTemplateOutputPath, temporaryDirectoryPath);
        await loadTileset(fileSystem, romReader, "MarsTileset", tiledTemplateOutputPath, temporaryDirectoryPath);
        await loadTileset(fileSystem, romReader, "PirateTileset", tiledTemplateOutputPath, temporaryDirectoryPath);
        Directory.Delete(temporaryDirectoryPath, true);
    }

    private static async Task loadTileset(NDSFileSystem fileSystem, BinaryReader romReader, string tilesetName, string tiledTemplateOutputPath, string temporaryDirectoryPath, bool silent = true)
    {
        string graphicsName = Path.ChangeExtension(tilesetName, "NCGR");
        NDSFile tilesetGraphics = fileSystem.FilesByPath[graphicsName];
        await LegoDecompressor.DecompressFileAsync(romReader, tilesetGraphics, Path.Combine(temporaryDirectoryPath, graphicsName), temporaryDirectoryPath);
        using NDSTileReader tileReader = NDSTileReader.Load(Path.Combine(temporaryDirectoryPath, graphicsName));

        string paletteName = Path.ChangeExtension(tilesetName, "NCLR");
        NDSFile kingTilesetPalette = fileSystem.FilesByPath[paletteName];
        await LegoDecompressor.DecompressFileAsync(romReader, kingTilesetPalette, Path.Combine(temporaryDirectoryPath, paletteName), temporaryDirectoryPath);
        NDSColourPalette colourPalette = NDSColourPalette.Load(Path.Combine(temporaryDirectoryPath, paletteName));

        if (!silent)
            await Console.Out.WriteLineAsync($"Loaded {tilesetName} tileset");

        using SpritesheetWriter spritesheetWriter = new((byte)tileReader.Width, (byte)tileReader.Height);
        for (ushort i = 0; i < tileReader.TileCount; i++)
            spritesheetWriter.WriteTileFromReader(tileReader, colourPalette, i);
        string tilesetFilePath = await spritesheetWriter.SaveAsync(tiledTemplateOutputPath ?? "", tilesetName, true, false);

        if (!silent)
            await Console.Out.WriteLineAsync($"Saved {tilesetName} tileset");

        string blockPaletteName = Path.ChangeExtension(tilesetName[..^2], "tbp");
        NDSFile blockPaletteFile = fileSystem.FilesByPath["BP/" + blockPaletteName];
        await LegoDecompressor.DecompressFileAsync(romReader, blockPaletteFile, Path.Combine(temporaryDirectoryPath, blockPaletteName), temporaryDirectoryPath);
        TilemapBlockPalette blockPalette = TilemapBlockPalette.LoadFromFile(Path.Combine(temporaryDirectoryPath, blockPaletteName));

        if (!silent)
            await Console.Out.WriteLineAsync($"Loaded {tilesetName} block palette");

        int blockCount = 368;
        byte sizeInTiles = (byte)MathF.Ceiling(MathF.Sqrt(blockCount));
        using SpritesheetWriter blockPaletteWriter = new((byte)(sizeInTiles * 3), (byte)(sizeInTiles * 2));
        blockPaletteWriter.WriteBlockPaletteFromReader(tileReader, colourPalette, blockPalette, blockCount, true);
        await blockPaletteWriter.SaveAsync(tiledTemplateOutputPath ?? "", blockPaletteName, true, false);

        if (!silent)
            await Console.Out.WriteLineAsync($"Saved {tilesetName} block palette");
    }
}
