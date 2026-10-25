using System.IO.Compression;

namespace OneGame.Render2D.Tests;

/// <summary>Minimal 8-bit RGBA PNG reader and writer for golden images (tests only).</summary>
internal static class Png
{
    static readonly byte[] Signature = [137, 80, 78, 71, 13, 10, 26, 10];

    /// <summary>Encodes 8-bit RGBA pixels as PNG bytes (an asset for the atlas to decode).</summary>
    public static byte[] Encode(int width, int height, ReadOnlySpan<byte> rgba)
    {
        var path = Path.GetTempFileName();
        try
        {
            Write(path, width, height, rgba);
            return File.ReadAllBytes(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    public static void Write(string path, int width, int height, ReadOnlySpan<byte> rgba)
    {
        using var raw = new MemoryStream();
        for (var y = 0; y < height; y++)
        {
            raw.WriteByte(0);
            raw.Write(rgba.Slice(y * width * 4, width * 4));
        }

        using var z = new MemoryStream();
        using (var zs = new ZLibStream(z, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            zs.Write(raw.GetBuffer(), 0, (int)raw.Length);
        }

        using var file = File.Create(path);
        file.Write(Signature);
        var ihdr = new byte[13];
        WriteBe(ihdr, 0, (uint)width);
        WriteBe(ihdr, 4, (uint)height);
        ihdr[8] = 8;
        ihdr[9] = 6;
        Chunk(file, "IHDR", ihdr);
        Chunk(file, "IDAT", z.ToArray());
        Chunk(file, "IEND", []);
    }

    public static (int Width, int Height, byte[] Rgba) Read(string path)
    {
        var data = File.ReadAllBytes(path);
        var pos = 8;
        int w = 0, h = 0;
        using var idat = new MemoryStream();
        while (pos < data.Length)
        {
            var len = (int)ReadBe(data, pos);
            var type = System.Text.Encoding.ASCII.GetString(data, pos + 4, 4);
            if (type == "IHDR")
            {
                w = (int)ReadBe(data, pos + 8);
                h = (int)ReadBe(data, pos + 12);
                if (data[pos + 16] != 8 || data[pos + 17] != 6)
                {
                    throw new InvalidDataException("only 8-bit RGBA PNG is supported");
                }
            }
            else if (type == "IDAT")
            {
                idat.Write(data, pos + 8, len);
            }

            pos += 12 + len;
        }

        idat.Position = 0;
        using var zs = new ZLibStream(idat, CompressionMode.Decompress);
        var stride = w * 4;
        var rgba = new byte[h * stride];
        var prev = new byte[stride];
        var line = new byte[stride];
        for (var y = 0; y < h; y++)
        {
            var filter = zs.ReadByte();
            zs.ReadExactly(line);
            for (var i = 0; i < stride; i++)
            {
                int a = i >= 4 ? line[i - 4] : 0, b = prev[i], c = i >= 4 ? prev[i - 4] : 0;
                line[i] = (byte)(line[i] + filter switch
                {
                    0 => 0,
                    1 => a,
                    2 => b,
                    3 => (a + b) / 2,
                    _ => Paeth(a, b, c),
                });
            }

            line.CopyTo(rgba, y * stride);
            // Unfiltering of the next line uses this decoded line.
            line.CopyTo(prev, 0);
        }

        return (w, h, rgba);
    }

    static int Paeth(int a, int b, int c)
    {
        var p = a + b - c;
        int pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
        return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
    }

    static uint ReadBe(byte[] d, int o) => (uint)(d[o] << 24 | d[o + 1] << 16 | d[o + 2] << 8 | d[o + 3]);

    static void WriteBe(byte[] d, int o, uint v)
    {
        d[o] = (byte)(v >> 24);
        d[o + 1] = (byte)(v >> 16);
        d[o + 2] = (byte)(v >> 8);
        d[o + 3] = (byte)v;
    }

    static void Chunk(Stream s, string type, byte[] body)
    {
        var len = new byte[4];
        WriteBe(len, 0, (uint)body.Length);
        s.Write(len);
        var t = System.Text.Encoding.ASCII.GetBytes(type);
        s.Write(t);
        s.Write(body);
        uint crc = 0xFFFFFFFF;
        foreach (var b in t.Concat(body))
        {
            crc = Crc[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }

        var c = new byte[4];
        WriteBe(c, 0, ~crc);
        s.Write(c);
    }

    static readonly uint[] Crc = Enumerable.Range(0, 256).Select(n =>
    {
        var c = (uint)n;
        for (var k = 0; k < 8; k++)
        {
            c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
        }

        return c;
    }).ToArray();
}
