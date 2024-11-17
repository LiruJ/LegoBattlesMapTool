using CommandLine;

namespace TiledToLB.CLI;

internal enum ExecutionMode
{
    Help,
    ImportMap,
    ProcessMap,
    ProcessMapLBZ,
    UnpackRom,
    Invalid,
}

internal class CommandLineOptions
{
    [Option('o', "output", Required = false, HelpText = "The output directory of the fully processed data")]
    public string? OutputFile { get; set; }

    [Option('i', "input", Required = false, HelpText = "The input tmx file")]
    public string? InputFile { get; set; }

    [Option('r', "rom", Required = false, HelpText = "The input rom file")]
    public string? RomFile { get; set; }

    [Option('t', "template", Required = false, HelpText = "The output directory for the generated Tiled files")]
    public string? TiledTemplateOutput { get; set; }

    [Option('s', "silent", Required = false, HelpText = "If this is given, no console output will be created")]
    public bool Silent { get; set; } = false;

    [Option('l', "lbz", Required = false, HelpText = "If this is given, the map files will be packed into an LBZ file for online play")]
    public bool PackLBZ { get; set; } = false;

    public ExecutionMode ExecutionMode
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(OutputFile) && !string.IsNullOrWhiteSpace(InputFile))
                return PackLBZ ? ExecutionMode.ProcessMapLBZ : ExecutionMode.ProcessMap;
            else if (!string.IsNullOrWhiteSpace(RomFile) && !string.IsNullOrWhiteSpace(TiledTemplateOutput))
                return ExecutionMode.UnpackRom;
            else return ExecutionMode.Invalid;
        }
    }

    public bool Validate()
    {
        switch (ExecutionMode)
        {
            case ExecutionMode.ProcessMap:
            case ExecutionMode.ProcessMapLBZ:
                if (string.IsNullOrWhiteSpace(InputFile))
                {
                    Console.WriteLine("Missing tiled file path!");
                    return false;
                }
                if (!File.Exists(InputFile))
                {
                    Console.WriteLine("Tiled file was not found!");
                    return false;
                }
                if (string.IsNullOrWhiteSpace(OutputFile))
                {
                    Console.WriteLine("Missing output path!");
                    return false;
                }
                return true;

            case ExecutionMode.UnpackRom:

                if (string.IsNullOrWhiteSpace(RomFile) || !File.Exists(RomFile))
                {
                    Console.WriteLine("Missing rom file!");
                    return false;
                }
                if (string.IsNullOrWhiteSpace(TiledTemplateOutput))
                {
                    Console.WriteLine("Missing generated Tiled output path!");
                    return false;
                }
                return true;
            case ExecutionMode.Invalid:
                Console.WriteLine("Invalid execution mode. Either needs input and output parameters to process a map, or a rom parameter to unpack.\nType --help to see how to use this tool.");
                return false;
            default:
                throw new InvalidOperationException("Missing execution mode case!");
        }
    }
}
