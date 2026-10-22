// Ignore Spelling: Perpetuum

using McMaster.Extensions.CommandLineUtils;
using Perpetuum.ClassLibrary;

namespace Perpetuum.MapBlocking
{
    internal class Program
    {
        public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);

        [Option(Description = "Input base file name (Files expected: <filename>.island.png & <filename>.obstacle.png).", LongName = "input", ShortName = "i")]
        public string InputFileName { get; } = "altitude.bin";

        [Option(Description = "Output base file name (Data is 2 byte per tile).", LongName = "output", ShortName = "o")]
        public string OutputFileName { get; } = "blocks.bin";

        private void OnExecute()
        {
            try
            {
                var islandFileName = InputFileName + ".island.png";
                var island = ClassUtility.LoadImageAsPNG(InputFileName + ".island.png", out uint size);
                Console.WriteLine($"Load: {islandFileName}");
                var result = new byte[size * size * 2];
                for (var i = 0; i < island.Length; i++)
                {
                    result[i * 2] = island[i] == 0 ? (byte)ClassUtility.BlockingFlags.Island : byte.MinValue;
                }

                var obstacleFileName = InputFileName + ".obstacle.png";
                var obstacle = ClassUtility.LoadImageAsPNG(InputFileName + ".obstacle.png", out uint sizeTow);
                Console.WriteLine($"Load: {obstacleFileName}");
                if (sizeTow != size)
                {
                    throw new Exception("The image dimensions must be the same.");
                }

                for (var i = 0; i < obstacle.Length; i++)
                {
                    var value = result[i * 2];
                    result[i * 2] = obstacle[i] == 0 ? (byte)((ClassUtility.BlockingFlags)value | ClassUtility.BlockingFlags.Obstacle) : value;
                }

                File.WriteAllBytes(OutputFileName, result);
                Console.WriteLine($"Save: {OutputFileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
