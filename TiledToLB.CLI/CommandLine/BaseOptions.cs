using CommandLine;

namespace TiledToLB.CLI.CommandLine
{
    public abstract class BaseOptions
    {
        [Option('s', "silent", Required = false, HelpText = "If this is given, no console output will be created")]
        public bool Silent { get; set; } = false;

        public virtual bool Validate() => true;
    }
}
