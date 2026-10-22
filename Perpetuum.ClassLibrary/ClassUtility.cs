// Ignore Spelling: Perpetuum, xc

using ImageMagick;
using System.Globalization;

namespace Perpetuum.ClassLibrary
{
    /// <summary>
    /// A class for tools used to create maps of game zones.
    /// </summary>
    public static class ClassUtility
    {
        [Flags]
        private enum BlockingFlags : byte
        {
            Undefined = 0,
            Obstacle = 1,
            Plant = 1 << 1,
            Decor = 1 << 2,
            Island = 1 << 3,

            NonNaturally = Decor | Island | Obstacle
        }

        /// <summary>
        /// Loads height maps as 16-bit gray-scale images.
        /// </summary>
        /// <param name="filename">Path to the image file.</param>
        /// <param name="size">Image size (side of the square).</param>
        /// <returns>A linear array of height samples.</returns>
        /// <exception cref="Exception"></exception>
        public static ushort[] LoadImageAsPNG(string filename, out uint size)
        {
            // We work with images using the ImageMagick library.
            using var image = new MagickImage(filename);
            using var pixels = image.GetPixels();

            // The image must be in gray-scale.
            var grayIndex = pixels.GetChannelIndex(PixelChannel.Gray);
            if (!grayIndex.HasValue)
            {
                throw new Exception($"The image must be in gray-scale: {filename}");
            }

            // The image must be strictly square.
            if (image.Width != image.Height)
            {
                throw new Exception($"The image must be square: {filename}");
            }
            size = image.Width;

            // We map the shades of gray directly to height.
            var result = new ushort[size * size];
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    result[y * size + x] = pixels.GetPixel(x, y).GetChannel(grayIndex.Value);
                }

            return result;
        }

        /// <summary>
        /// Generating a height map from a JPEG file.
        /// </summary>
        /// <param name="path">Base filename. If the file is `name.12288.jpeg`, then the base name is `name`.</param>
        /// <param name="size"></param>
        /// <returns></returns>
        /// <exception cref="IOException">Image size (side of the square).</exception>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="Exception"></exception>
        public static ushort[] LoadImageAsJPEG(string path, out uint size)
        {
            // We obtain the path to the directory containing the file.
            var prefix = Path.GetFileName(path) + ".";
            string imagePath = string.Empty;
            ushort maximum = 0;
            var directoryName = Path.GetDirectoryName(Path.GetFullPath(path));
            if (string.IsNullOrEmpty(directoryName))
            {
                throw new IOException($"Directory not found: {path}.");
            }

            // We iterate through all matching JPEG files that have a base name followed by a number.
            foreach (var candidate in Directory.EnumerateFiles(directoryName))
            {
                var name = Path.GetFileName(candidate);
                if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                    !name.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Let’s decode the number from the name—the maximum height.
                var numberLength = name.Length - prefix.Length - ".jpeg".Length;
                if (numberLength <= 0 || !ushort.TryParse(name.AsSpan(prefix.Length, numberLength),
                        NumberStyles.None, CultureInfo.InvariantCulture, out var value))
                {
                    continue;
                }

                // There should be only one suitable file.
                if (!string.IsNullOrEmpty(imagePath))
                {
                    throw new IOException($"Multiple JPEG layers found for '{path}'.");
                }

                imagePath = candidate;
                maximum = value;
            }

            if (imagePath == null)
            {
                throw new FileNotFoundException($"Layer and fallback '{path}.<0-65535>.jpeg' not found.", path);
            }

            byte[] grayscale;
            using (var image = new MagickImage(imagePath))
            using (var pixels = image.GetPixels())
            {
                var grayIndex = pixels.GetChannelIndex(PixelChannel.Gray);
                if (!grayIndex.HasValue)
                {
                    throw new Exception($"The image must be in gray-scale: {imagePath}");
                }

                if (image.Width != image.Height)
                {
                    throw new Exception($"The image must be square: {imagePath}");
                }
                size = image.Width;

                grayscale = new byte[size * size];
                for (var y = 0; y < size; y++)
                    for (var x = 0; x < size; x++)
                    {
                        // In JPEG, gray-scale tones are 8-bit.
                        grayscale[y * size + x] = (byte)(pixels.GetPixel(x, y).GetChannel(grayIndex.Value) / 257);
                    }
            }

            // Smooth and scale the height to 16-bit, taking the maximum height into account.
            var result = new ushort[grayscale.Length];
            for (var y = 0; y < size; y++)
            {
                var minY = Math.Max(0, y - 2);
                var maxY = Math.Min(size - 1, y + 2);
                for (var x = 0; x < size; x++)
                {
                    var minX = Math.Max(0, x - 2);
                    var maxX = Math.Min(size - 1, x + 2);
                    var sum = 0;
                    for (var neighborY = minY; neighborY <= maxY; neighborY++)
                    {
                        var rowOffset = neighborY * size;
                        for (var neighborX = minX; neighborX <= maxX; neighborX++)
                        {
                            sum += grayscale[rowOffset + neighborX];
                        }
                    }

                    // Average the available 5x5 neighbors and scale before rounding to ushort.
                    var divisor = 255 * (maxX - minX + 1) * (maxY - minY + 1);
                    result[y * size + x] = (ushort)((sum * maximum + divisor / 2) / divisor);
                }
            }

            return result;
        }

        /// <summary>
        /// We save the height map with the highest possible level of detail for 8-bit.
        /// </summary>
        /// <param name="data">16-bit terrain elevation data.</param>
        /// <param name="path">Base filename.</param>
        /// <param name="size">Size of the image square.</param>
        /// <returns>File name in the format: path.DDDDD.jpeg (DDDDD - Maximum height.)</returns>
        /// <exception cref="ArgumentException"></exception>
        public static string SaveAsJPEG(ushort[] data, string path, uint size)
        {
            // The image must be square.
            if (data.Length != size * size)
            {
                throw new ArgumentException("The number of elements in the array does not correspond to the image size.");
            }

            // We calculate the maximum height.
            var maximum = 0;
            for (var i = 0; i < data.Length; i++)
            {
                if (data[i] > maximum)
                {
                    maximum = data[i];
                }
            }
            var filename = path + "." + maximum.ToString() + ".jpeg";

            var settings = new MagickReadSettings
            {
                Width = size,
                Height = size
            };

            using (var image = new MagickImage("xc:black", settings))
            {
                image.ColorSpace = ColorSpace.Gray;
                image.ColorType = ColorType.Grayscale;

                using (var pixels = image.GetPixels())
                {
                    for (var y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            // We scale, taking the maximum height into account.
                            var pixelValue = (uint)data[y * size + x] * ushort.MaxValue / maximum;
                            pixels.GetPixel(x, y).SetChannel(0, (ushort)pixelValue);
                        }
                    }
                }
                image.Write(filename);
            }

            return filename;
        }

        /// <summary>
        /// Save the height map.
        /// </summary>
        /// <param name="data">16-bit terrain elevation data.</param>
        /// <param name="path">Base filename.</param>
        /// <param name="size">Size of the image square.</param>
        /// <exception cref="ArgumentException"></exception>
        public static void SaveAsPNG(ushort[] data, string path, uint size)
        {
            // The image must be square.
            if (data.Length != size * size)
            {
                throw new ArgumentException("The number of elements in the array does not correspond to the image size.");
            }

            var filename = path + ".png";

            var settings = new MagickReadSettings
            {
                Width = size,
                Height = size
            };

            using var image = new MagickImage("xc:black", settings);

            image.ColorSpace = ColorSpace.Gray;
            image.ColorType = ColorType.Grayscale;
            image.Depth = 16;

            using (var pixels = image.GetPixels())
            {
                for (var y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        pixels.GetPixel(x, y).SetChannel(0, data[y * size + x]);
                    }
                }
            }
            image.Write(filename);
        }

        public static ushort[] ProcessImage(ushort[] data, uint size, int landRadius, int maxHeight)
        {
            var size_half = size / 2;
            var result = new ushort[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Initial height value
                    ushort pixelValue = data[y * size + x];
                    // By default, the height is zero.
                    ushort newPixelValue = 0;

                    var d = (y - size_half) * (y - size_half) + (x - size_half) * (x - size_half);
                    if (d < landRadius * landRadius)
                    {
                        // Inside the land radius, we keep the original height value.
                        newPixelValue = pixelValue;
                    }
                    else if (d < size_half * size_half)
                    {
                        // Outside the land radius, we apply a smooth transition to zero height using a polynomial function.
                        var r = 1.0 - (Math.Sqrt(d) - landRadius) / landRadius;
                        var h = 6.0 * Math.Pow(r, 5) - 15.0 * Math.Pow(r, 4) + 10.0 * Math.Pow(r, 3);
                        newPixelValue = (ushort)(h * pixelValue);
                    }

                    // We place the new value into the array.
                    result[y * size + x] = newPixelValue;
                }
            }

            // We find the maximum value in the array to normalize the height values.
            var maxArray = 0;
            for (var k = 0; k < result.Length; k++)
            {
                var value = result[k];
                if (value > maxArray)
                {
                    maxArray = value;
                }
            }

            for (var k = 0; k < result.Length; k++)
            {
                // Normalize the height values to the specified maximum height.
                result[k] = (ushort)((long)result[k] * maxHeight / maxArray);
            }

            return result;
        }

        public static ushort[] ParseUShortArray(string input)
        {
            ArgumentNullException.ThrowIfNull(input);

            if (string.IsNullOrWhiteSpace(input))
            {
                return [];
            }

            string[] parts = input.Split(',');
            ushort[] result = new ushort[parts.Length];

            for (int i = 0; i < parts.Length; i++)
            {
                if (!ushort.TryParse(parts[i].Trim(), out ushort value))
                {
                    throw new FormatException($"Invalid number: '{parts[i]}'");
                }

                if (i > 0 && value <= result[i - 1])
                {
                    throw new FormatException($"The numbers must be strictly increasing: {result[i - 1]} >= {value}");
                }

                result[i] = value;
            }

            return result;
        }

        /// <summary>
        /// Generating a mask from height values.
        /// </summary>
        /// <param name="data">An array containing height values.</param>
        /// <param name="minimum">The minimum (is excluded).</param>
        /// <param name="maximum">The maximum (is included in the mask).</param>
        /// <returns>Byte array. 0 — included, 255 — excluded.</returns>
        public static byte[] ProcessMask(ushort[] data, ushort minimum, ushort maximum)
        {
            var result = new byte[data.Length];
            for (var i = 0; i < data.Length; i++)
            {
                if (data[i] > minimum && data[i] <= maximum)
                {
                    result[i] = byte.MinValue;
                }
                else
                {
                    result[i] = byte.MaxValue;
                }
            }

            return result;
        }

        /// <summary>
        /// Save the mask as a 1-bit image file.
        /// </summary>
        /// <param name="data">Byte array. 0 — included, 255 — excluded.</param>
        /// <param name="size">The side length of the square image.</param>
        /// <param name="path">Base path to the saved image.</param>
        /// <exception cref="ArgumentException"></exception>
        public static void SaveMaskAsPNG(byte[] data, uint size, string path)
        {
            if (size <= 0 || size * size != data.Length)
                throw new ArgumentException("Invalid mask dimensions.");

            var settings = new PixelReadSettings(size, size, StorageType.Char, "I");

            // We create an image from a single-channel array.
            using var image = new MagickImage(data, settings);

            // Black-and-white image, 1 bit.
            image.ColorType = ColorType.Bilevel;
            image.Depth = 1;
            image.Format = MagickFormat.Png;

            image.Write(path + ".png");
        }
    }
}
