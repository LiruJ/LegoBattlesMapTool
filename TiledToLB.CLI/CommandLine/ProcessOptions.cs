using CommandLine;

namespace TiledToLB.CLI.CommandLine
{
    [Verb("process", HelpText = "Processes a .tmx file into multiple files to be patched into the ROM")]
    public class ProcessOptions : BaseOptions
    {
        [Option('o', "output", Required = true, HelpText = "The output directory of the fully processed data")]
        public required string OutputDirectoryPath { get; set; }

        [Option('i', "input", Required = true, HelpText = "The input tmx file")]
        public required string InputFilePath { get; set; }
    }
}
