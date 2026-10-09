using ImageMagick;
using McMaster.Extensions.CommandLineUtils;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace ConsoleApp
{
    internal class Program
    {
        public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);

        [Option(Description = "Input file name (format: 16-bit grayscale image).", LongName = "input", ShortName = "i")]
        public string InputFileName { get; } = "altitude.png";

        [Option(Description = "Output file name (format: 16-bit grayscale raw binary).", LongName = "output", ShortName = "o")]
        public string OutputFileName { get; } = "altitude.bin";

        [Option(Description = "Radius of the fixed area at the center of the map.", LongName = "radius", ShortName = "r")]
        public int LandRadius { get; } = 512;

        [Option(Description = "Maximum height value.", LongName = "height", ShortName = "h")]
        [Range(0, ushort.MaxValue)]
        [Required]
        public int MaxHeight { get; } = 8196;

        private void OnExecute()
        {
            ushort[] array;
            uint size;

            try
            {
                using (var image = new MagickImage(InputFileName))
                using (var pixels = image.GetPixels())
                {
                    // Validate the input parameters
                    if (image.Width != image.Height)
                    {
                        Console.WriteLine("Invalid image width or height. Please specify a square image.");
                        return;
                    }
                    size = image.Width;

                    if (image.ColorType != ColorType.Grayscale)
                    {
                        Console.WriteLine("Invalid image color type. Please specify a grayscale image.");
                        return;
                    }
                    if (LandRadius < 0 || LandRadius > size / 2)
                    {
                        Console.WriteLine("Invalid land radius. Please specify a value between 0 and {0}.", size / 2);
                        return;
                    }

                    array = ProcessImage(pixels, size, LandRadius, MaxHeight);
                }

                SaveImage(array, size, OutputFileName);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void SaveImage(ushort[] array, uint size, string outputFile)
        {
            ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(new ReadOnlySpan<ushort>(array));
            // Write the byte array to the output file
            File.WriteAllBytes(outputFile, bytes);

            // Also save the image as a PNG for visualization
            var settings = new MagickReadSettings
            {
                Width = size,
                Height = size
            };

            using (var image = new MagickImage("xc:black", settings))
            {
                image.ColorSpace = ColorSpace.Gray;
                image.ColorType = ColorType.Grayscale;
                image.Depth = 16;

                using (var pixels = image.GetPixels())
                { 
                    for (var i = 0; i < size; i++)
                    {
                        for (int j = 0; j < size; j++)
                        {
                            var pixelValue = array[i * size + j];
                            pixels.GetPixel(i, j).SetChannel(0, pixelValue);
                        }
                    }
                }
                image.Write(outputFile + ".png");
            }
        }

        private static ushort[] ProcessImage(IPixelCollection<ushort> pixels, uint size, int landRadius, int maxHeight)
        {
            var size_half = size / 2;
            var array = new ushort[size * size];

            // Find the maximum pixel value in the image
            var maxArray = 0;
            for (var i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    var pixel = pixels.GetPixel(i, j).GetChannel(0);

                    array[i * size + j] = pixel;
                    if (pixel > maxArray)
                    {
                        maxArray = pixel;
                    }
                }
            }

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    // Initial height value
                    ushort pixelValue = array[i * size + j];
                    // By default, the height is zero.
                    ushort newPixelValue = 0;

                    var d = (i - size_half) * (i - size_half) + (j - size_half) * (j - size_half);
                    if (d < landRadius * landRadius)
                    {
                        // Within the central circle, we only scale the heights.
                        newPixelValue = (ushort)(pixelValue * maxHeight / maxArray);
                    }
                    else if (d < size_half * size_half)
                    {
                        // Between the land radius and the edge of the circle, apply a smooth transition using a polynomial function.
                        var r = 1.0 - (Math.Sqrt(d) - landRadius) / landRadius;
                        var h = 6.0 * Math.Pow(r, 5) - 15.0 * Math.Pow(r, 4) + 10.0 * Math.Pow(r, 3);
                        newPixelValue = (ushort)(h * pixelValue * maxHeight / maxArray);
                    }

                    // We place the new value into the array.
                    array[i * size + j] = newPixelValue;
                }
            }

            return array;
        }
    }
}
