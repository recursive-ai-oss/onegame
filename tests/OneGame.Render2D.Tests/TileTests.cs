using System.Diagnostics;
using System.Numerics;
using OneGame.UI;

namespace OneGame.Render2D.Tests;

/// <summary>Tilemaps: near, middle and far zoom, edits, and a 4096 x 4096 map drawn zoomed out.</summary>
public class TileTests
{
    /// <summary>Eight 16x16 tiles in a 4x2 grid, each its own colour with a darker rim and a diagonal so orientation shows.</summary>
    private static byte[] TilesetPixels()
    {
        Color[] colors =
        [
            Color.Hex("#4c9f38"), Color.Hex("#2f6fd0"), Color.Hex("#d9a441"), Color.Hex("#9b59b6"),
            Color.Hex("#c0392b"), Color.Hex("#16a085"), Color.Hex("#7f8c8d"), Color.Hex("#e8e8e8"),
        ];
        var w = 64;
        var h = 32;
        var data = new byte[w * h * 4];
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var t = ((y / 16) * 4) + (x / 16);
                var lx = x % 16;
                var ly = y % 16;
                var c = colors[t];
                var k = lx == 0 || ly == 0 ? 0.6f : (lx == ly ? 0.8f : 1f);
                var i = ((y * w) + x) * 4;
                data[i] = (byte)(c.R * k);
                data[i + 1] = (byte)(c.G * k);
                data[i + 2] = (byte)(c.B * k);
                data[i + 3] = 255;
            }
        }

        return data;
    }

    private static ushort Pattern(int x, int y)
    {
        // Regions of 16 tiles with a variation that is not periodic at the chunk size; every ninth tile is empty.
        var v = ((x >> 4) + ((y >> 4) * 3) + (((x * 7) + (y * 13)) % 5 == 0 ? 1 : 0)) & 7;
        return (x + y) % 9 == 0 ? (ushort)0 : (ushort)(v + 1);
    }

    private static TileMap BigMap(Rig rig, int size)
    {
        var set = rig.Own(rig.Renderer.CreateTileSet(16, 16, 4, 2, TilesetPixels()));
        var map = rig.Own(new TileMap(set, size, size, 16));
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                map.Set(x, y, Pattern(x, y));
            }
        }

        return map;
    }

    private static byte[] Draw(Rig rig, TileMap map, Vector2 center, float zoom)
    {
        var scene = new Scene2D();
        scene.Draw(scene.DefaultLayer, map);
        return rig.Render(() => rig.Renderer.DrawScene(scene, Camera2D.Default with { Position = center, Zoom = zoom }), new Color(10, 10, 14, 255));
    }

    [Fact]
    public void ThreeZoomLevelsOfALargeMapMatchTheReferences()
    {
        using var rig = new Rig(320, 240);
        var sw = Stopwatch.StartNew();
        var map = BigMap(rig, 4096);
        var fill = sw.ElapsedMilliseconds;
        var centre = new Vector2(2048 * 16, 2048 * 16);

        var near = Draw(rig, map, centre, 1f);
        Assert.Equal(0, map.LastStats.Level);
        Golden.Check("tiles-near", 320, 240, near);

        sw.Restart();
        var mid = Draw(rig, map, centre, 0.2f);
        var midFirst = sw.ElapsedMilliseconds;
        Assert.Equal(1, map.LastStats.Level);
        Golden.Check("tiles-mid", 320, 240, mid);

        // The whole 65536-unit map fits the screen height (229 px): the coarsest level, a single chunk. The zoom keeps coarse-tile edges off pixel
        // centres (3.584 px per tile), where nearest sampling is a coin toss between rasterizers.
        sw.Restart();
        var far = Draw(rig, map, centre, 0.0035f);
        var farFirst = sw.ElapsedMilliseconds;
        Assert.Equal(map.MaxLevel, map.LastStats.Level);
        Assert.True(map.LastStats.ChunkQuads <= 4, $"{map.LastStats.ChunkQuads} chunk quads");
        Golden.Check("tiles-far", 320, 240, far);
        Assert.Equal(1, rig.Stats.DrawCalls);

        // Drawing it again costs nothing on the GPU side beyond the single quad: no uploads, one draw.
        Draw(rig, map, centre, 0.0035f);
        Assert.Equal(0, map.LastStats.ChunkUploads);
        Golden.Note(
            $"4096x4096 map: filled in {fill} ms; mid (level 1) first frame {midFirst} ms; far (level {map.MaxLevel}) first frame {farFirst} ms incl. building every coarse level; " +
            $"far frame: {rig.Stats}");
    }

    [Fact]
    public void WorkPerFrameDependsOnTheScreenNotTheMapSize()
    {
        using var rig = new Rig(640, 360);
        var map = BigMap(rig, 4096);
        var centre = new Vector2(2048 * 16, 2048 * 16);
        foreach (var zoom in new[] { 2f, 1f, 0.5f, 0.25f, 0.1f, 0.05f, 0.02f, 0.005f, 0.001f })
        {
            Draw(rig, map, centre, zoom);
            var st = map.LastStats;
            // At most the chunks that tile a 640x360 screen, with the coarse tile always at least four pixels: 640/256 + 2 by 360/256 + 2.
            Assert.True(st.ChunkQuads <= 20, $"zoom {zoom}: level {st.Level}, {st.ChunkQuads} chunk quads");
            Assert.Equal(0, st.DroppedChunks);
            Assert.True(rig.Stats.DrawCalls <= 1);
        }
    }

    [Fact]
    public void EditingATileChangesTheNearViewAndTheAveragedFarView()
    {
        using var rig = new Rig(128, 128);
        var set = rig.Own(rig.Renderer.CreateTileSet(16, 16, 4, 2, TilesetPixels()));
        var map = rig.Own(new TileMap(set, 256, 256, 16));
        for (var y = 0; y < 256; y++)
        {
            for (var x = 0; x < 256; x++)
            {
                map.Set(x, y, 1);
            }
        }

        var centre = new Vector2(8 * 16, 8 * 16);
        var before = Draw(rig, map, centre, 2f);
        var cell = Pixels.At(before, 128, 64, 64);
        map.Set(8, 8, 5);
        var after = Draw(rig, map, centre, 2f);
        Assert.NotEqual(cell, Pixels.At(after, 128, 64, 64));
        Assert.Equal(1, map.LastStats.ChunkUploads);

        // Far away, a block of tile 5 over the whole map turns the averaged colour into that tile's mean colour.
        for (var y = 0; y < 256; y++)
        {
            for (var x = 0; x < 256; x++)
            {
                map.Set(x, y, 5);
            }
        }

        var far = Draw(rig, map, new Vector2(128 * 16, 128 * 16), 0.05f);
        Assert.True(map.LastStats.Level >= 2);
        var (r, g, b, _) = Pixels.At(far, 128, 64, 64);
        // Tile 5 is #c0392b with its rim and diagonal darkening: the mean is a little darker than the base colour but clearly the same hue.
        Assert.InRange(r, 150, 200);
        Assert.InRange(g, 30, 65);
        Assert.InRange(b, 25, 55);
    }

    [Fact]
    public void BadTilesAreRejectedWithAUsefulMessage()
    {
        using var rig = new Rig(16, 16);
        var set = rig.Own(rig.Renderer.CreateTileSet(16, 16, 4, 2, TilesetPixels()));
        var map = rig.Own(new TileMap(set, 10, 10, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => map.Set(10, 0, 1));
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => map.Set(0, 0, 9));
        Assert.Contains("not in the tile set", ex.Message, StringComparison.Ordinal);
        Assert.Equal(0, map.Get(-1, 3));
    }
}
