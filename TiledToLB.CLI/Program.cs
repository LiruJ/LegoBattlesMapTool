using CommandLine;
using TiledToLB.CLI.CommandLine;
using TiledToLB.Core.Processors;

var parseResult = Parser.Default.ParseArguments<ExportOptions, ExportLBZOptions, UnpackOptions, ImportFromRomOptions, CreateNewOptions, UpgradeExistingOptions>(args);

await parseResult.WithParsedAsync<ExportOptions>(async options => await ExporterProcessor.ProcessMapAsync(options.WorkspaceDirectoryPath, options.InputMapName, options.OutputMapName, !options.SkipCompression, options.Silent));
await parseResult.WithParsedAsync<ExportLBZOptions>(async options => await ExporterProcessor.ProcessAndPackLBZAsync(options.WorkspaceDirectoryPath, options.InputMapName, options.OutputMapName, options.Silent));
await parseResult.WithParsedAsync<CreateNewOptions>(async options => await CreateNewProcessor.CreateNewAsync(options.WorkspaceDirectoryPath, options.MapName, options.TilesetName, options.CreatorName, options.Silent));
await parseResult.WithParsedAsync<UnpackOptions>(async options => await UnpackerProcessor.UnpackRomAsync(options.InputFilePath, options.OutputDirectoryPath, options.Overwrite, options.Silent));
await parseResult.WithParsedAsync<ImportFromRomOptions>(async options => await ImporterProcessor.ImportMapFromRomAsync(options.InputFilePath, options.WorkspaceDirectoryPath, options.RomMapName, options.OutputMapName, options.Silent));
await parseResult.WithParsedAsync<UpgradeExistingOptions>(async options => await UpgradeProcessor.UpgradeExistingAsync(options.WorkspaceDirectoryPath, options.MapName, options.Silent));

await parseResult.WithNotParsedAsync((errors) => Console.Out.WriteLineAsync("Error parsing inputs!"));