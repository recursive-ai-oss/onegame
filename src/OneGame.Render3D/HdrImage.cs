using System.Globalization;
using System.Text;

namespace OneGame.Render3D;

/// <summary>Limits applied before a Radiance file allocates anything.</summary>
public sealed class HdrLimits
{
    /// <summary>Largest width or height in pixels.</summary>
    public int MaxDimension { get; init; } = 8192;

    /// <summary>Most pixels in the image.</summary>
    public long MaxPixels { get; init; } = 16L * 1024 * 1024;

    /// <summary>Longest header line, in bytes.</summary>
    public int MaxHeaderLine { get; init; } = 256;

    /// <summary>Most header lines.</summary>
    public int MaxHeaderLines { get; init; } = 64;
}

/// <summary>The file is not a valid Radiance (.hdr) image, or exceeds a limit.</summary>
public sealed class HdrException : Exception
{
    /// <summary>Creates the exception.</summary>
    public HdrException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// A floating-point panorama in linear RGB, rows from the top. Used as the source of an environment: the panorama maps direction
/// <c>d</c> (+Y up) to <c>u = atan2(d.x, -d.z) / 2pi + 0.5</c> and <c>v = acos(d.y) / pi</c>, so the middle of the image looks down -Z.
/// </summary>
public sealed class HdrImage
{
    /// <summary>Creates an image from existing data.</summary>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="rgb">Width x height x 3 floats, linear.</param>
    public HdrImage(int width, int height, float[] rgb)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        ArgumentNullException.ThrowIfNull(rgb);
        if (rgb.Length != checked(width * height * 3))
        {
            throw new ArgumentException("The pixel array does not match the size.", nameof(rgb));
        }

        Width = width;
        Height = height;
        Rgb = rgb;
    }

    /// <summary>Width in pixels.</summary>
    public int Width { get; }

    /// <summary>Height in pixels.</summary>
    public int Height { get; }

    /// <summary>Linear red, green, blue per pixel, rows from the top.</summary>
    public float[] Rgb { get; }

    /// <summary>The panorama coordinates of a direction (see the type remarks).</summary>
    public static Vector2 DirectionToUv(Vector3 d) =>
        new(MathF.Atan2(d.X, -d.Z) / (2f * MathF.PI) + 0.5f, MathF.Acos(Math.Clamp(d.Y, -1f, 1f)) / MathF.PI);

    /// <summary>
    /// A procedural sky: a gradient from <paramref name="ground"/> below the horizon through <paramref name="horizon"/> to
    /// <paramref name="zenith"/>, with an optional sun disc. Deterministic, so tests and tools can make environments without a file.
    /// </summary>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="zenith">Radiance straight up.</param>
    /// <param name="horizon">Radiance at the horizon.</param>
    /// <param name="ground">Radiance straight down.</param>
    /// <param name="sunDirection">Direction towards the sun, or null for none.</param>
    /// <param name="sunColor">Radiance added inside the sun disc.</param>
    /// <param name="sunAngle">Angular radius of the sun disc in radians.</param>
    /// <returns>The panorama.</returns>
    public static HdrImage CreateSky(int width, int height, Vector3 zenith, Vector3 horizon, Vector3 ground, Vector3? sunDirection = null, Vector3 sunColor = default, float sunAngle = 0.05f)
    {
        var rgb = new float[checked(width * height * 3)];
        var sun = sunDirection is { } s ? Vector3.Normalize(s) : Vector3.Zero;
        var cosSun = MathF.Cos(sunAngle);
        for (var y = 0; y < height; y++)
        {
            var theta = (y + 0.5f) / height * MathF.PI;
            for (var x = 0; x < width; x++)
            {
                var phi = (((x + 0.5f) / width) - 0.5f) * 2f * MathF.PI;
                var d = new Vector3(MathF.Sin(theta) * MathF.Sin(phi), MathF.Cos(theta), -MathF.Sin(theta) * MathF.Cos(phi));
                var t = Math.Clamp(MathF.Abs(d.Y), 0f, 1f);
                var c = d.Y >= 0 ? Vector3.Lerp(horizon, zenith, MathF.Sqrt(t)) : Vector3.Lerp(horizon, ground, MathF.Sqrt(t));
                if (sunDirection is not null && Vector3.Dot(d, sun) > cosSun)
                {
                    c += sunColor;
                }

                var o = ((y * width) + x) * 3;
                rgb[o] = c.X;
                rgb[o + 1] = c.Y;
                rgb[o + 2] = c.Z;
            }
        }

        return new HdrImage(width, height, rgb);
    }

    /// <summary>Decodes a Radiance RGBE file (flat or run-length encoded). Everything is bounds-checked and sized from the header before allocating.</summary>
    /// <param name="data">The file bytes.</param>
    /// <param name="limits">Size limits, or null for the defaults.</param>
    /// <returns>The image.</returns>
    /// <exception cref="HdrException">The data is not a supported Radiance file or exceeds a limit.</exception>
    public static HdrImage Decode(ReadOnlySpan<byte> data, HdrLimits? limits = null)
    {
        limits ??= new HdrLimits();
        var pos = 0;
        var first = ReadLine(data, ref pos, limits.MaxHeaderLine);
        if (!first.StartsWith("#?", StringComparison.Ordinal))
        {
            throw new HdrException("Not a Radiance file: the first line must start with #?.");
        }

        var lines = 0;
        while (true)
        {
            var line = ReadLine(data, ref pos, limits.MaxHeaderLine);
            if (++lines > limits.MaxHeaderLines)
            {
                throw new HdrException("The header has too many lines.");
            }

            if (line.Length == 0)
            {
                break;
            }

            if (line.StartsWith("FORMAT=", StringComparison.Ordinal) && line != "FORMAT=32-bit_rle_rgbe")
            {
                throw new HdrException($"Unsupported format '{line}'; only 32-bit_rle_rgbe is read.");
            }
        }

        var resolution = ReadLine(data, ref pos, limits.MaxHeaderLine).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (resolution.Length != 4 || resolution[0] != "-Y" || resolution[2] != "+X"
            || !int.TryParse(resolution[1], NumberStyles.None, CultureInfo.InvariantCulture, out var height)
            || !int.TryParse(resolution[3], NumberStyles.None, CultureInfo.InvariantCulture, out var width))
        {
            throw new HdrException("Unsupported resolution line; only '-Y height +X width' is read.");
        }

        if (width < 1 || height < 1 || width > limits.MaxDimension || height > limits.MaxDimension || (long)width * height > limits.MaxPixels)
        {
            throw new HdrException($"The image size {width}x{height} exceeds the limits.");
        }

        var rgb = new float[checked(width * height * 3)];
        var scan = new byte[checked(width * 4)];
        for (var y = 0; y < height; y++)
        {
            ReadScanline(data, ref pos, scan, width);
            for (var x = 0; x < width; x++)
            {
                var e = scan[(x * 4) + 3];
                var o = ((y * width) + x) * 3;
                if (e == 0)
                {
                    continue;
                }

                var f = MathF.Pow(2f, e - 136f);   // 2^(e - 128) / 256: the mantissa byte is a fraction of 256
                rgb[o] = scan[x * 4] * f;
                rgb[o + 1] = scan[(x * 4) + 1] * f;
                rgb[o + 2] = scan[(x * 4) + 2] * f;
            }
        }

        return new HdrImage(width, height, rgb);
    }

    private static string ReadLine(ReadOnlySpan<byte> data, ref int pos, int max)
    {
        var start = pos;
        while (pos < data.Length && data[pos] != (byte)'\n')
        {
            if (pos - start > max)
            {
                throw new HdrException("A header line is too long.");
            }

            pos++;
        }

        if (pos >= data.Length)
        {
            throw new HdrException("The header is cut short.");
        }

        var line = Encoding.Latin1.GetString(data.Slice(start, pos - start)).TrimEnd('\r');
        pos++;
        return line;
    }

    private static void ReadScanline(ReadOnlySpan<byte> data, ref int pos, byte[] scan, int width)
    {
        if (width is < 8 or > 32767 || pos + 4 > data.Length || data[pos] != 2 || data[pos + 1] != 2 || (data[pos + 2] & 0x80) != 0)
        {
            // Flat pixels: old files, or an image too narrow for run-length coding. Old-style run markers are not supported.
            var need = (long)width * 4;
            if (pos + need > data.Length)
            {
                throw new HdrException("The pixel data is cut short.");
            }

            data.Slice(pos, (int)need).CopyTo(scan);
            pos += (int)need;
            return;
        }

        if (((data[pos + 2] << 8) | data[pos + 3]) != width)
        {
            throw new HdrException("A scanline's width does not match the image.");
        }

        pos += 4;
        for (var channel = 0; channel < 4; channel++)
        {
            var x = 0;
            while (x < width)
            {
                if (pos >= data.Length)
                {
                    throw new HdrException("The pixel data is cut short.");
                }

                int count = data[pos++];
                if (count > 128)
                {
                    count -= 128;
                    if (pos >= data.Length || x + count > width)
                    {
                        throw new HdrException("A run goes past the end of its scanline.");
                    }

                    var value = data[pos++];
                    for (var i = 0; i < count; i++)
                    {
                        scan[((x + i) * 4) + channel] = value;
                    }
                }
                else
                {
                    if (count == 0 || pos + count > data.Length || x + count > width)
                    {
                        throw new HdrException("A literal run goes past the end of its scanline.");
                    }

                    for (var i = 0; i < count; i++)
                    {
                        scan[((x + i) * 4) + channel] = data[pos++];
                    }
                }

                x += count;
            }
        }
    }
}
