using CommandLine;

namespace TiledToLB.CLI.CommandLine
{
    [Verb("export", HelpText = "Processes a .tmx file into multiple files to be patched into the ROM")]
    public class ExportOptions : BaseOptions
    {
        [Option('o', "output", Required = true, HelpText = "The output directory of the fully processed data")]
        public required string OutputDirectoryPath { get; set; }

        [Option('i', "input", Required = true, HelpText = "The input tmx file")]
        public required string InputFilePath { get; set; }

        [Option('c', "skip-compression", Required = false, HelpText = "If this is given, the compression stage will be skipped. This file will not be directly usable in the game and will cause a crash")]
        public bool SkipCompression { get; set; } = false;
    }
}
