using CommandLine;
using TiledToLB.CLI;
using TiledToLB.Core;

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
        await Processor.ProcessMapAsync(options.InputFile!, options.OutputFile!);
        break;
    case ExecutionMode.UnpackRom:
        await Processor.UnpackRomAsync(options.RomFile!, options.TiledTemplateOutput!, options.Silent);
        break;
    case ExecutionMode.Invalid:
        throw new Exception("Invalid command line parameters!");
    default:
        break;
}