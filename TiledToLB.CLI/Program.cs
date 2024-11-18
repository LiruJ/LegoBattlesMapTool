using CommandLine;
using TiledToLB.CLI.CommandLine;
using TiledToLB.Core;


var parseResult = Parser.Default.ParseArguments<ProcessOptions, ProcessLBZOptions, UnpackOptions>(args);

await parseResult.WithParsedAsync<ProcessOptions>(async options => await Processor.ProcessMapAsync(options.InputFilePath, options.OutputDirectoryPath));
await parseResult.WithParsedAsync<ProcessLBZOptions>(async options => await Processor.ProcessAndPackLBZAsync(options.InputFilePath, options.OutputDirectoryPath));
await parseResult.WithParsedAsync<UnpackOptions>(async options => await Processor.UnpackRomAsync(options.InputFilePath, options.OutputDirectoryPath, options.Silent));

await parseResult.WithNotParsedAsync((errors) => Console.Out.WriteLineAsync("Error parsing inputs!"));



//await Processor.UnpackRomAsync(p.InputFilePath, p.OutputDirectoryPath, p.Silent);

//CommandLineOptions? options = null;
//Parser.Default.ParseArguments<CommandLineOptions>(args).WithParsed((parsedOptions) =>
//{
//    // Set the options.
//    if (parsedOptions.Validate())
//        options = parsedOptions;
//}).WithNotParsed((errors) => Console.WriteLine("Error parsing inputs!"));

//if (options == null || !options.Silent)
//    await Console.Out.WriteLineAsync("Created by Liru. Credit goes to CUE for the decompressors for LZX.");

//if (options == null)
//    return;

//switch (options.ExecutionMode)
//{
//    case ExecutionMode.ProcessMap:
//        await Processor.ProcessMapAsync(options.InputFile!, options.OutputFile!);
//        break;
//    case ExecutionMode.ProcessMapLBZ:
//        await Processor.ProcessAndPackLBZAsync(options.InputFile!, options.OutputFile!);
//        break;
//    case ExecutionMode.UnpackRom:
//        await Processor.UnpackRomAsync(options.RomFile!, options.TiledTemplateOutput!, options.Silent);
//        break;
//    case ExecutionMode.Invalid:
//        throw new Exception("Invalid command line parameters!");
//    default:
//        break;
//}