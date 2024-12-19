using CommandLine;

namespace TiledToLB.CLI.CommandLine
{
    [Verb("upgrade", HelpText = "Upgrades an existing tmx file to the current version")]
    public class UpgradeExistingOptions : BaseOptions
    {
        [Option('i', "input", Required = true, HelpText = "The input tmx file")]
        public required string InputFilePath { get; set; }
    }
}
