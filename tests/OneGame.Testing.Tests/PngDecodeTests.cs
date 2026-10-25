using System.Buffers.Binary;
using System.IO.Compression;
using OneGame.Host;

namespace OneGame.Testing.Tests;

/// <summary>Capture PNGs must decode to the same pixels as an independent zlib implementation.</summary>
public sealed class PngDecodeTests
{
    private static byte[] Fixture() => File.ReadAllBytes(Path.Combine(GpuPaths.RepoRoot, "testdata", "png", "inflate-input-boundary.png"));

    /// <summary>A valid stream from OneImage's own encoder that its inflater decodes wrongly (see testdata/png/README.md).</summary>
    [Fact(Skip = "OneImage 1.10.0's inflater corrupts this valid stream; waiting for the fixed release.")]
    public void ValidEncoderOutputDecodesLikeAnIndependentInflater()
    {
        var bytes = Fixture();
        var expected = ReferenceDecode(bytes, out var w, out var h);

        var frame = Png.Decode(bytes);

        Assert.Equal(w, frame.Width);
        Assert.Equal(h, frame.Height);
        Assert.True(expected.AsSpan().SequenceEqual(frame.Rgba));
    }

    /// <summary>The fixture is a valid PNG: the shared framework's inflater reads it and every filter resolves.</summary>
    [Fact]
    public void FixtureIsAValidPng()
    {
        var pixels = ReferenceDecode(Fixture(), out var w, out var h);
        Assert.Equal(449, w);
        Assert.Equal(59, h);
        Assert.Equal(w * h * 4, pixels.Length);
    }

    /// <summary>Minimal 8-bit RGBA, non-interlaced PNG reader on <see cref="ZLibStream"/>; test-only.</summary>
    private static byte[] ReferenceDecode(byte[] png, out int width, out int height)
    {
        width = height = 0;
        var idat = new MemoryStream();
        var pos = 8;
        while (pos < png.Length)
        {
            var len = (int)BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(pos));
            var type = System.Text.Encoding.ASCII.GetString(png, pos + 4, 4);
            if (type == "IHDR")
            {
                width = (int)BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(pos + 8));
                height = (int)BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(pos + 12));
            }
            else if (type == "IDAT")
            {
                idat.Write(png, pos + 8, len);
            }

            pos += 12 + len;
        }

        idat.Position = 0;
        var raw = new MemoryStream();
        using (var z = new ZLibStream(idat, CompressionMode.Decompress))
        {
            z.CopyTo(raw);
        }

        var data = raw.ToArray();
        var stride = width * 4;
        var output = new byte[stride * height];
        for (var y = 0; y < height; y++)
        {
            var filter = data[y * (stride + 1)];
            for (var i = 0; i < stride; i++)
            {
                int cur = data[(y * (stride + 1)) + 1 + i];
                int a = i >= 4 ? output[(y * stride) + i - 4] : 0;
                int b = y > 0 ? output[((y - 1) * stride) + i] : 0;
                int c = i >= 4 && y > 0 ? output[((y - 1) * stride) + i - 4] : 0;
                var add = filter switch
                {
                    0 => 0,
                    1 => a,
                    2 => b,
                    3 => (a + b) / 2,
                    4 => Paeth(a, b, c),
                    _ => throw new InvalidDataException("filter"),
                };
                output[(y * stride) + i] = (byte)(cur + add);
            }
        }

        return output;
    }

    private static int Paeth(int a, int b, int c)
    {
        var p = a + b - c;
        var pa = Math.Abs(p - a);
        var pb = Math.Abs(p - b);
        var pc = Math.Abs(p - c);
        return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
    }
}
