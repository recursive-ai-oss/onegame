namespace OneGame.Render3D.Tests;

public class HdrTests
{
    private static byte[] File(int w, int h, bool rle)
    {
        var bytes = new List<byte>(System.Text.Encoding.ASCII.GetBytes($"#?RADIANCE\nFORMAT=32-bit_rle_rgbe\n\n-Y {h} +X {w}\n"));
        for (var y = 0; y < h; y++)
        {
            if (rle)
            {
                bytes.AddRange(new byte[] { 2, 2, (byte)(w >> 8), (byte)w });
                // Four channels, each one run of the same value: r = 128 (0.5 at exponent 129), g = 64, b = 32, e = 129.
                foreach (var v in new byte[] { 128, 64, 32, 129 })
                {
                    bytes.AddRange(new byte[] { (byte)(128 + w), v });
                }
            }
            else
            {
                for (var x = 0; x < w; x++)
                {
                    bytes.AddRange(new byte[] { 128, 64, 32, 129 });
                }
            }
        }

        return [.. bytes];
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DecodesFlatAndRunLengthPixels(bool rle)
    {
        var image = HdrImage.Decode(File(rle ? 16 : 4, 3, rle));
        Assert.Equal(3, image.Height);

        // mantissa / 256 * 2^(129 - 128) = 0.5 * 128 / 128 ... 128/256 * 2 = 1.0, 64/256 * 2 = 0.5, 32/256 * 2 = 0.25
        Assert.Equal(1.0f, image.Rgb[0], 4);
        Assert.Equal(0.5f, image.Rgb[1], 4);
        Assert.Equal(0.25f, image.Rgb[2], 4);
    }

    [Fact]
    public void RefusesWhatIsNotAnImageOrIsTooBig()
    {
        Assert.Throws<HdrException>(() => HdrImage.Decode("not an image"u8));
        Assert.Throws<HdrException>(() => HdrImage.Decode(System.Text.Encoding.ASCII.GetBytes("#?RADIANCE\n\n-Y 100000 +X 100000\n")));
        Assert.Throws<HdrException>(() => HdrImage.Decode(File(16, 3, true).AsSpan(0, 60)));
        Assert.Throws<HdrException>(() => HdrImage.Decode(System.Text.Encoding.ASCII.GetBytes("#?RADIANCE\nFORMAT=32-bit_rle_xyze\n\n-Y 4 +X 4\n")));
    }

    [Fact]
    public void DamagedFilesOnlyEverThrowTheTypedException()
    {
        var seed = File(16, 8, true);
        var rng = new Random(7);
        for (var i = 0; i < 3000; i++)
        {
            var data = (byte[])seed.Clone();
            for (var k = rng.Next(1, 5); k > 0; k--)
            {
                data[rng.Next(data.Length)] = (byte)rng.Next(256);
            }

            var cut = rng.Next(3) == 0 ? rng.Next(data.Length) : data.Length;
            try
            {
                HdrImage.Decode(data.AsSpan(0, cut), new HdrLimits { MaxPixels = 1 << 16 });
            }
            catch (HdrException)
            {
            }
        }
    }

    [Fact]
    public void SkyPanoramaPutsTheSunWhereItWasAskedFor()
    {
        var sun = Vector3.Normalize(new Vector3(0.3f, 0.6f, -0.7f));
        var image = HdrImage.CreateSky(256, 128, new Vector3(0.2f), new Vector3(0.4f), new Vector3(0.1f), sun, new Vector3(50f), 0.1f);
        var uv = HdrImage.DirectionToUv(sun);
        var o = (((int)(uv.Y * 128) * 256) + (int)(uv.X * 256)) * 3;
        Assert.True(image.Rgb[o] > 40f);
    }
}
