using CommandLine;

namespace TiledToLB.CLI.CommandLine
{
    [Verb("unpack", HelpText = "Unpacks the graphical data from the game rom")]
    public class UnpackOptions : BaseOptions
    {
        [Option('o', "output", Required = true, HelpText = "The output directory for the generated Tiled files")]
        public required string OutputDirectoryPath { get; set; }

        [Option('i', "input", Required = true, HelpText = "The input rom file")]
        public required string InputFilePath { get; set; }
    }
}
