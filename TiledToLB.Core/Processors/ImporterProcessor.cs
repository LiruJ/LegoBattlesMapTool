namespace TiledToLB.Core.Processors
{
    public static class ImporterProcessor
    {
        public static async Task ImportMapAsync(string inputFilePath, string outputFilePath)
        {
            if (string.IsNullOrWhiteSpace(inputFilePath))
            {
                Console.WriteLine("Missing input");
                return;
            }

            if (!File.Exists(inputFilePath))
            {
                Console.WriteLine("File does not exist!");
                return;
            }

            //await LegoDecompressor.DecompressFileAsync(inputFilePath, decompressedMapFilePath, temporaryDirectoryName);
        }
    }
}
