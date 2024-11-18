using CommandLine;
using TiledToLB.CLI.CommandLine;
using TiledToLB.Core.Processors;

var parseResult = Parser.Default.ParseArguments<ProcessOptions, ProcessLBZOptions, UnpackOptions>(args);

await parseResult.WithParsedAsync<ProcessOptions>(async options => await ExporterProcessor.ProcessMapAsync(options.InputFilePath, options.OutputDirectoryPath));
await parseResult.WithParsedAsync<ProcessLBZOptions>(async options => await ExporterProcessor.ProcessAndPackLBZAsync(options.InputFilePath, options.OutputDirectoryPath));
await parseResult.WithParsedAsync<UnpackOptions>(async options => await UnpackerProcessor.UnpackRomAsync(options.InputFilePath, options.OutputDirectoryPath, options.Overwrite, options.Silent));

await parseResult.WithNotParsedAsync((errors) => Console.Out.WriteLineAsync("Error parsing inputs!"));