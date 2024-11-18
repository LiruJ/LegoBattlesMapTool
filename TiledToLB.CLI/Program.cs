using CommandLine;
using TiledToLB.CLI.CommandLine;
using TiledToLB.Core;

var parseResult = Parser.Default.ParseArguments<ProcessOptions, ProcessLBZOptions, UnpackOptions>(args);

await parseResult.WithParsedAsync<ProcessOptions>(async options => await Processor.ProcessMapAsync(options.InputFilePath, options.OutputDirectoryPath));
await parseResult.WithParsedAsync<ProcessLBZOptions>(async options => await Processor.ProcessAndPackLBZAsync(options.InputFilePath, options.OutputDirectoryPath));
await parseResult.WithParsedAsync<UnpackOptions>(async options => await Processor.UnpackRomAsync(options.InputFilePath, options.OutputDirectoryPath, options.Silent));

await parseResult.WithNotParsedAsync((errors) => Console.Out.WriteLineAsync("Error parsing inputs!"));