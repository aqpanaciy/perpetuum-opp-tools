// Ignore Spelling: Perpetuum, jpeg

using McMaster.Extensions.CommandLineUtils;
using Perpetuum.ClassLibrary;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace Perpetuum.MapInit
{
    internal class Program
    {
        public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);

        [Option(Description = "Input file name (format: 16-bit gray-scale image).", LongName = "input", ShortName = "i")]
        public string InputFileName { get; } = "altitude.png";

        [Option(Description = "Output base file name (format: 8-bit gray-scale jpeg, 1-bit for mask).", LongName = "output", ShortName = "o")]
        public string OutputFileName { get; } = "altitude.bin";

        [Option(Description = "Radius of the fixed area at the center of the map.", LongName = "radius", ShortName = "r")]
        public int LandRadius { get; } = 512;

        [Option(Description = "Maximum height value.", LongName = "height", ShortName = "h")]
        [Range(0, ushort.MaxValue)]
        public int MaxHeight { get; } = 8196;

        [Option(Description = "Height levels for creating layer masks", LongName = "levels", ShortName = "l")]
        public string Levels { get; } = "0,1,1760,2048,4098,8196";

        private void OnExecute()
        {
            try
            {
                var levels = ClassUtility.ParseUShortArray(Levels);
                Console.WriteLine($"Parse numbers: {levels.Length}");

                var data = ClassUtility.LoadImageAsPNG(InputFileName, out uint size);
                Console.WriteLine($"Load: {InputFileName}");
                if (LandRadius < 0 || LandRadius > size / 2)
                {
                    throw new Exception($"Invalid land radius. Please specify a value between 0 and {size / 2}.");
                }

                data = ClassUtility.ProcessImage(data, size, LandRadius, MaxHeight);
                Console.WriteLine($"Process image. Size: {size}");

                var filename = ClassUtility.SaveAsJPEG(data, OutputFileName, size);
                Console.WriteLine($"Save: {filename}");
                // We recreate it from the JPEG to ensure that the data matches the data on the server.
                data = ClassUtility.LoadImageAsJPEG(OutputFileName, out size);
                Console.WriteLine($"Reload: {filename}");

                ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(new ReadOnlySpan<ushort>(data));
                // Write the byte array to the output file
                File.WriteAllBytes(OutputFileName, bytes);
                Console.WriteLine($"Save: {OutputFileName}");

                ushort prevLevel = 0;
                byte[] mask;
                for (var i = 0; i < levels.Length; i++)
                {
                    mask = ClassUtility.ProcessMask(data, prevLevel, levels[i]);
                    var maskFileName = OutputFileName + "." + prevLevel.ToString() + "-" + levels[i].ToString();
                    ClassUtility.SaveMaskAsPNG(mask, size, maskFileName);
                    Console.WriteLine($"Save: {maskFileName}");
                    prevLevel = levels[i];
                }
                mask = ClassUtility.ProcessMask(data, prevLevel, ushort.MaxValue);
                var endMaskFileName = OutputFileName + "." + prevLevel.ToString() + "-" + ushort.MaxValue.ToString();
                ClassUtility.SaveMaskAsPNG(mask, size, endMaskFileName);
                Console.WriteLine($"Save: {endMaskFileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
