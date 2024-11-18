using CommandLine;

namespace TiledToLB.CLI.CommandLine
{
    [Verb("import-rom", HelpText = "Imports a map from the given game rom and processes it into a .tmx file")]
    public class ImportFromRomOptions : BaseOptions
    {
        [Option('o', "output", Required = true, HelpText = "The output directory for the generated Tiled files")]
        public required string OutputDirectoryPath { get; set; }

        [Option('i', "input", Required = true, HelpText = "The input rom file")]
        public required string InputFilePath { get; set; }

        [Option('m', "map", Required = true, HelpText = "The map's file name inside the rom, such as \"ck1_1\"")]
        public required string MapName { get; set; }
    }
}
