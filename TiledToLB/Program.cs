using CommandLine;
using ContentUnpacker.Decompressors;
using ContentUnpacker.NDSFS;
using ContentUnpacker.Spritesheets;
using GlobalShared.Tilemaps;
using System.Xml;
using TiledToLB;
using TiledToLB.Tilemap;

CommandLineOptions? options = null;
Parser.Default.ParseArguments<CommandLineOptions>(args).WithParsed((parsedOptions) =>
{
    // Set the options.
    if (parsedOptions.Validate())
        options = parsedOptions;
}).WithNotParsed((parsedOptions) =>
{

});

if (options == null || !options.Silent)
    await Console.Out.WriteLineAsync("Created by Liru. Credit goes to CUE for the decompressors for LZX.");

if (options == null)
    return;

switch (options.ExecutionMode)
{
    case ExecutionMode.ProcessMap:
        await processMapAsync();
        break;
    case ExecutionMode.UnpackRom:
        await unpackRomAsync();
        break;
    case ExecutionMode.Invalid:
        throw new Exception("Invalid command line parameters!");
    default:
        break;
}

async Task processMapAsync()
{
    XmlDocument tiledFile = new();
    tiledFile.Load(options.InputFile);

    Tuple<byte, byte> mapDimensions = Map.LoadMapDimensionsFromTiled(tiledFile);
    Map map = new(mapDimensions.Item1, mapDimensions.Item2);

    map.LoadFromTiled(tiledFile, options.InputFile);

    string uncompressedMapPath = Path.Combine(Path.GetDirectoryName(options.OutputFile), Path.GetFileNameWithoutExtension(options.OutputFile) + "_temp.bin");
    map.Save(uncompressedMapPath);

    Directory.CreateDirectory("Temporary");
    await LegoDecompressor.CompressFileAsync(LZXEncodeType.EVB, uncompressedMapPath, options.OutputFile, 4096, "Temporary");
    Directory.Delete("Temporary", true);
    File.Delete(uncompressedMapPath);
}

async Task unpackRomAsync()
{
    // Create the template directory.
    if (Directory.Exists(options.TiledTemplateOutput))
        Directory.Delete(options.TiledTemplateOutput, true);
    if (!options.Silent)
        Console.WriteLine($"Deleting existing templates folder at \"{Path.GetFullPath(options.TiledTemplateOutput)}\"");
    Directory.CreateDirectory(options.TiledTemplateOutput);
    if (!options.Silent)
        Console.WriteLine($"Created templates folder at \"{Path.GetFullPath(options.TiledTemplateOutput)}\"");

    // Copy over any template files.
    const string templateFilesDirectoryPath = "TemplateFiles";
    foreach (string templateFilePath in Directory.EnumerateFiles(templateFilesDirectoryPath))
        File.Copy(templateFilePath, Path.Combine(options.TiledTemplateOutput, Path.GetFileName(templateFilePath)));

    // Read the rom file system.
    if (!options.Silent)
        Console.WriteLine("Reading ROM file system");
    NDSFileSystem fileSystem = NDSFileSystem.LoadFromRom(options.RomFile);
    if (!options.Silent)
        Console.WriteLine($"Read ROM file system with {fileSystem.FilesById.Count} files and {fileSystem.DirectoriesById.Count} directories");

    using FileStream romFile = File.OpenRead(options.RomFile);
    using BinaryReader romReader = new(romFile);

    // Load the tilesets from the rom, save the pngs to the templates folder.
    const string temporaryDirectoryPath = "Temporary";
    Directory.CreateDirectory(temporaryDirectoryPath);
    await loadTileset(fileSystem, romReader, "KingTileset", temporaryDirectoryPath);
    await loadTileset(fileSystem, romReader, "MarsTileset", temporaryDirectoryPath);
    await loadTileset(fileSystem, romReader, "PirateTileset", temporaryDirectoryPath);
    Directory.Delete(temporaryDirectoryPath, true);
}

async Task loadTileset(NDSFileSystem fileSystem, BinaryReader romReader, string tilesetName, string temporaryDirectoryPath)
{
    string graphicsName = Path.ChangeExtension(tilesetName, "NCGR");
    NDSFile tilesetGraphics = fileSystem.FilesByPath[graphicsName];
    await LegoDecompressor.DecompressFileAsync(romReader, tilesetGraphics, Path.Combine(temporaryDirectoryPath, graphicsName), temporaryDirectoryPath);
    using NDSTileReader tileReader = NDSTileReader.Load(Path.Combine(temporaryDirectoryPath, graphicsName));

    string paletteName = Path.ChangeExtension(tilesetName, "NCLR");
    NDSFile kingTilesetPalette = fileSystem.FilesByPath[paletteName];
    await LegoDecompressor.DecompressFileAsync(romReader, kingTilesetPalette, Path.Combine(temporaryDirectoryPath, paletteName), temporaryDirectoryPath);
    NDSColourPalette colourPalette = NDSColourPalette.Load(Path.Combine(temporaryDirectoryPath, paletteName));

    if (!options.Silent)
        await Console.Out.WriteLineAsync($"Loaded {tilesetName} tileset");

    using SpritesheetWriter spritesheetWriter = new((byte)tileReader.Width, (byte)tileReader.Height);
    for (ushort i = 0; i < tileReader.TileCount; i++)
        spritesheetWriter.WriteTileFromReader(tileReader, colourPalette, i);
    string tilesetFilePath = await spritesheetWriter.SaveAsync(options.TiledTemplateOutput ?? "", tilesetName, true, false);

    if (!options.Silent)
        await Console.Out.WriteLineAsync($"Saved {tilesetName} tileset");

    string blockPaletteName = Path.ChangeExtension(tilesetName[..^2], "tbp");
    NDSFile blockPaletteFile = fileSystem.FilesByPath["BP/" + blockPaletteName];
    await LegoDecompressor.DecompressFileAsync(romReader, blockPaletteFile, Path.Combine(temporaryDirectoryPath, blockPaletteName), temporaryDirectoryPath);
    TilemapBlockPalette blockPalette = TilemapBlockPalette.LoadFromFile(Path.Combine(temporaryDirectoryPath, blockPaletteName));

    if (!options.Silent)
        await Console.Out.WriteLineAsync($"Loaded {tilesetName} block palette");

    int blockCount = 368;
    byte sizeInTiles = (byte)MathF.Ceiling(MathF.Sqrt(blockCount));
    using SpritesheetWriter blockPaletteWriter = new((byte)(sizeInTiles * 3), (byte)(sizeInTiles * 2));
    blockPaletteWriter.WriteBlockPaletteFromReader(tileReader, colourPalette, blockPalette, blockCount, true);
    await blockPaletteWriter.SaveAsync(options.TiledTemplateOutput ?? "", blockPaletteName, true, false);

    if (!options.Silent)
        await Console.Out.WriteLineAsync($"Saved {tilesetName} block palette");
}