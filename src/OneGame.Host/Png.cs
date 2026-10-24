using System.Runtime.InteropServices;
using OneImage;

namespace OneGame.Host;

/// <summary>PNG reading and writing for captures, through OneImage.</summary>
public static class Png
{
    /// <summary>Largest image the reader accepts, in pixels per side.</summary>
    public const int MaxSide = 16384;

    /// <summary>Encodes a frame as PNG bytes.</summary>
    public static byte[] Encode(CapturedFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        var pixels = MemoryMarshal.Cast<byte, Rgba32>(frame.Rgba);
        var image = Image.FromPixels<Rgba32>(pixels, frame.Width, frame.Height);
        using var ms = new MemoryStream();
        image.Save(ms, PngFormat.Instance);
        return ms.ToArray();
    }

    /// <summary>Writes a frame to a PNG file, creating the folder.</summary>
    public static void Save(CapturedFrame frame, string path)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllBytes(path, Encode(frame));
    }

    /// <summary>Decodes a PNG (or any format OneImage reads) into RGBA8. The size is checked before pixels are allocated.</summary>
    /// <exception cref="InvalidDataException">The bytes are not a readable image, or the image is larger than <see cref="MaxSide"/>.</exception>
    public static CapturedFrame Decode(byte[] bytes, string renderer = "file")
    {
        ArgumentNullException.ThrowIfNull(bytes);
        try
        {
            var info = Image.Identify(bytes);
            if (info.Width is < 1 or > MaxSide || info.Height is < 1 or > MaxSide)
            {
                throw new InvalidDataException($"The image is {info.Width}x{info.Height}; the limit is {MaxSide} per side.");
            }

            var image = Image.Load(bytes);
            var pixels = image.ToArray<Rgba32>();
            return new CapturedFrame(info.Width, info.Height, MemoryMarshal.AsBytes(pixels.AsSpan()).ToArray(), renderer);
        }
        catch (Exception ex) when (ex is not InvalidDataException and not OutOfMemoryException)
        {
            throw new InvalidDataException("The file is not a readable image: " + ex.Message, ex);
        }
    }

    /// <summary>Reads an image file.</summary>
    public static CapturedFrame Load(string path) => Decode(File.ReadAllBytes(path), path);
}
