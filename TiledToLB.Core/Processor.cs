using ContentUnpacker.Decompressors;
using ContentUnpacker.NDSFS;
using ContentUnpacker.Spritesheets;
using GlobalShared.Tilemaps;
using System.Xml;
using TiledToLB.Core.Tilemap;

namespace TiledToLB.Core;

public static class Processor
{
    private const string temporaryDirectoryName = "Temporary";

    public static async Task ProcessMapAsync(string inputFilePath, string outputFilePath)
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

        Directory.CreateDirectory(temporaryDirectoryName);

        await MinimapGenerator.Save(map, outputFilePath, temporaryDirectoryName, false);
        await MinimapGenerator.Save(map, outputFilePath, temporaryDirectoryName, true);

        await LegoDecompressor.CompressFileAsync(LZXEncodeType.EVB, uncompressedMapPath, Path.ChangeExtension(outputFilePath, "map"), 4096, temporaryDirectoryName);
        Directory.Delete(temporaryDirectoryName, true);
        File.Delete(uncompressedMapPath);
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
