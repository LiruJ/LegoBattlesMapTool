﻿using CommandLine;

namespace TiledToLB.CLI.CommandLine
{
    [Verb("upgrade", HelpText = "Upgrades an existing tmx file to the current version")]
    public class UpgradeExistingOptions : BaseOptions
    {
        [Option('w', "workspace", Required = true, HelpText = "The directory generated by unpacking. The map should be in the \"Maps\" folder")]
        public required string WorkspaceDirectoryPath { get; set; }

        [Option('i', "input", Required = true, HelpText = "The input tmx file name")]
        public required string MapName { get; set; }
    }
}