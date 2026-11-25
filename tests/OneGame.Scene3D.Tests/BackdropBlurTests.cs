using System.IO.Compression;
using OneGame.Host;

namespace OneGame.Scene3D.Tests;

/// <summary>A translucent UI panel with a backdrop blur over a live 3D scene, drawn through the whole host path.</summary>
public class BackdropBlurTests
{
    private const int W = 640;
    private const int H = 360;

    // Panel box in framebuffer pixels (UI scale 1 at this size): left 200 + the column's 8 px padding is not applied to an absolute child.
    private static bool InsidePanel(int x, int y) => x >= 228 && x < 412 && y >= 94 && y < 224;

    private static double Roughness(CapturedFrame f)
    {
        // Sum of absolute horizontal and vertical neighbour differences of the green channel inside the panel, per pixel.
        double sum = 0;
        var n = 0;
        for (var y = 94; y < 224; y++)
        {
            for (var x = 228; x < 411; x++)
            {
                var i = ((y * f.Width) + x) * 4 + 1;
                sum += Math.Abs(f.Rgba[i] - f.Rgba[i + 4]) + Math.Abs(f.Rgba[i] - f.Rgba[i + (f.Width * 4)]);
                n++;
            }
        }

        return sum / n;
    }

    private static uint Crc(byte[] data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in data)
        {
            crc ^= b;
            for (var k = 0; k < 8; k++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
            }
        }

        return ~crc;
    }

    private static void Save(CapturedFrame f, string name)
    {
        if (Environment.GetEnvironmentVariable("ONEGAME_CAPTURE_DIR") is not { Length: > 0 } dir)
        {
            return;
        }

        Directory.CreateDirectory(dir);
        using var raw = new MemoryStream();
        for (var y = 0; y < f.Height; y++)
        {
            raw.WriteByte(0);
            raw.Write(f.Rgba, y * f.Width * 4, f.Width * 4);
        }

        using var png = new MemoryStream();
        png.Write([137, 80, 78, 71, 13, 10, 26, 10]);
        void Chunk(string type, byte[] body)
        {
            var len = new byte[4];
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(len, (uint)body.Length);
            png.Write(len);
            var typed = System.Text.Encoding.ASCII.GetBytes(type).Concat(body).ToArray();
            png.Write(typed);
            var crc = new byte[4];
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(crc, Crc(typed));
            png.Write(crc);
        }

        var head = new byte[13];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(head, (uint)f.Width);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(head.AsSpan(4), (uint)f.Height);
        head[8] = 8;
        head[9] = 6;
        Chunk("IHDR", head);
        using var z = new MemoryStream();
        using (var d = new ZLibStream(z, CompressionLevel.Fastest, true))
        {
            d.Write(raw.ToArray());
        }

        Chunk("IDAT", z.ToArray());
        Chunk("IEND", []);
        File.WriteAllBytes(Path.Combine(dir, name + ".png"), png.ToArray());
    }

    [Fact]
    public void TheSceneBehindATranslucentPanelIsBlurredAndTheRestIsUntouched()
    {
        CapturedFrame sharp, blurred;
        using (var cluster = Fixture.Start(W, H, glassBlur: 0f))
        {
            cluster.Client.Step(5);
            sharp = cluster.Client.Capture($"{W}x{H}");
        }

        using (var cluster = Fixture.Start(W, H, glassBlur: 7f))
        {
            cluster.Client.Step(5);
            blurred = cluster.Client.Capture($"{W}x{H}");
        }

        Save(sharp, "glass_off");
        Save(blurred, "glass_on");

        // Inside the panel the scene's edges are much softer; outside it every pixel is the same as without the panel.
        Assert.True(Roughness(blurred) < Roughness(sharp) * 0.5, $"panel interior roughness {Roughness(blurred):F2} against {Roughness(sharp):F2}");
        var changedOutside = 0;
        for (var y = 0; y < H; y++)
        {
            for (var x = 0; x < W; x++)
            {
                var inside = x >= 198 && x < 442 && y >= 68 && y < 242;   // the panel and its antialiased edge
                if (inside)
                {
                    continue;
                }

                var i = ((y * W) + x) * 4;
                if (Math.Abs(sharp.Rgba[i] - blurred.Rgba[i]) + Math.Abs(sharp.Rgba[i + 1] - blurred.Rgba[i + 1]) + Math.Abs(sharp.Rgba[i + 2] - blurred.Rgba[i + 2]) > 3)
                {
                    changedOutside++;
                }
            }
        }

        Assert.Equal(0, changedOutside);
        Assert.True(InsidePanel(300, 150));
    }
}
