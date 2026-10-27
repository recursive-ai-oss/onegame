using OneGame.Host;
using OneRender.Image;
using OneRender.Painting;

namespace OneGame.Pages.Tests;

public class RenderTests
{
    internal const string BoxesHtml = Boxes;

    private const string Boxes = """
        <body style="margin:0;background:#fff">
        <div style="position:absolute;left:10px;top:10px;width:60px;height:40px;background:#e03030"></div>
        <div style="position:absolute;left:80px;top:10px;width:60px;height:40px;background:#3070e0;border-radius:12px"></div>
        <div style="position:absolute;left:10px;top:60px;width:130px;height:40px;background:linear-gradient(90deg,#f00,#00f);border:4px solid #202020"></div>
        <p style="position:absolute;left:10px;top:110px;margin:0;font:20px Body;color:#103060">Hello pages</p>
        </body>
        """;

    private const string Shapes = """
        <body style="margin:0;background:#fff;font:12px Body">
        <div style="position:absolute;left:8px;top:8px;width:70px;height:50px;background:#f4f4f4;border:5px solid;border-color:#c00 #0a0 #00c #a0a"></div>
        <div style="position:absolute;left:96px;top:8px;width:70px;height:50px;border:4px dashed #333"></div>
        <div style="position:absolute;left:184px;top:8px;width:70px;height:50px;border:4px dotted #333;border-radius:20px"></div>
        <div style="position:absolute;left:272px;top:8px;width:70px;height:50px;border:6px double #036"></div>
        <div style="position:absolute;left:8px;top:80px;width:70px;height:50px;background:#fff;box-shadow:6px 6px 10px rgba(0,0,0,.5);border-radius:10px"></div>
        <div style="position:absolute;left:96px;top:80px;width:70px;height:50px;background:#eee;box-shadow:inset 0 0 12px #036"></div>
        <div style="position:absolute;left:184px;top:80px;width:70px;height:50px;background:radial-gradient(circle at 30% 30%,#ff0,#f80 50%,#800)"></div>
        <div style="position:absolute;left:272px;top:80px;width:70px;height:50px;background:conic-gradient(#f00,#ff0,#0f0,#0ff,#00f,#f0f,#f00);border-radius:50%"></div>
        <div style="position:absolute;left:8px;top:152px;width:70px;height:50px;background:repeating-linear-gradient(45deg,#222 0 6px,#fc0 6px 12px)"></div>
        <div style="position:absolute;left:96px;top:152px;width:70px;height:50px;background:#369;border-radius:30px 5px 30px 5px / 20px 25px 20px 5px"></div>
        <div style="position:absolute;left:184px;top:152px;width:70px;height:50px;border:6px groove #b84"></div>
        <div style="position:absolute;left:272px;top:152px;width:70px;height:50px;border:6px ridge #48b"></div>
        </body>
        """;

    private const string Effects = """
        <body style="margin:0;background:#fff;font:12px Body">
        <div style="position:absolute;left:8px;top:8px;width:70px;height:50px;background:#e33;opacity:.5"><div style="width:30px;height:30px;background:#33e;margin:10px"></div></div>
        <div style="position:absolute;left:96px;top:8px;width:70px;height:50px;background:#fc0"><div style="width:50px;height:50px;background:#09f;mix-blend-mode:multiply;margin:15px 0 0 30px"></div></div>
        <div style="position:absolute;left:184px;top:8px;width:70px;height:50px;background:#223"><div style="width:50px;height:50px;background:#a42;mix-blend-mode:plus-lighter;margin:15px 0 0 30px"></div></div>
        <div style="position:absolute;left:272px;top:8px;width:70px;height:50px;filter:blur(3px)"><div style="width:40px;height:40px;background:#c0c;margin:5px"></div></div>
        <div style="position:absolute;left:8px;top:80px;width:70px;height:50px;filter:grayscale(1) contrast(1.4)"><div style="width:70px;height:50px;background:linear-gradient(90deg,#f33,#3f3)"></div></div>
        <div style="position:absolute;left:96px;top:80px;width:70px;height:50px;filter:drop-shadow(5px 5px 3px #000)"><div style="width:40px;height:30px;background:#6c6"></div></div>
        <div style="position:absolute;left:184px;top:80px;width:70px;height:50px;-webkit-mask-image:linear-gradient(90deg,#000,transparent);mask-image:linear-gradient(90deg,#000,transparent)"><div style="width:70px;height:50px;background:#d60"></div></div>
        <div style="position:absolute;left:272px;top:80px;width:70px;height:50px;clip-path:circle(24px at 35px 25px);background:#0a6"></div>
        <div style="position:absolute;left:8px;top:152px;width:70px;height:50px;background:#fc3"><div style="position:absolute;left:20px;top:10px;width:50px;height:36px;backdrop-filter:blur(4px);background:rgba(255,255,255,.2);border-radius:8px"></div><div style="width:35px;height:50px;background:#36c"></div></div>
        <div style="position:absolute;left:96px;top:152px;width:70px;height:50px;background:#a3c;transform:rotate(15deg) scale(.8)"></div>
        <div style="position:absolute;left:184px;top:152px;width:70px;height:50px;clip-path:polygon(50% 0,100% 100%,0 100%);background:#c33"></div>
        <div style="position:absolute;left:272px;top:152px;width:70px;height:50px;perspective:120px"><div style="width:70px;height:50px;background:linear-gradient(#fa0,#05a);transform:rotateY(40deg)"></div></div>
        </body>
        """;

    private static void Compare(string name, string html, int w, int h, int tolerance = 8, double budget = 0.01)
    {
        using var rig = new Rig(w, h);
        var screen = rig.Pages.Show("p", PageScreen.FromHtml(html, rig.Options()));
        var gpu = rig.Render();
        Shots.Save(name, w, h, gpu);
        var reference = rig.Reference(screen);
        Shots.Save(name + "-ref", w, h, reference);
        var (max, mean, share) = Shots.Diff(gpu, reference, tolerance);
        Shots.Note($"{name} vs reference: max {max}, mean {mean:F3}, over-{tolerance} {share:P3}");
        Assert.True(share < budget, $"{name}: {share:P3} of pixels differ by more than {tolerance} (max {max})");
        Shots.Golden(name, w, h, gpu);
    }

    [Fact]
    public void BoxesMatchTheReferenceRasterizer() => Compare("boxes", Boxes, 160, 150, 8, 0.02);

    [Fact]
    public void ShapesMatchTheReferenceRasterizer() => Compare("shapes", Shapes, 352, 212, 8, 0.03);

    [Fact]
    public void EffectsMatchTheReferenceRasterizer() => Compare("effects", Effects, 352, 212);
}

public class ContentTests
{
    private sealed class Resolver(Dictionary<int, RgbaImage> images) : IImageResolver
    {
        public RgbaImage? Resolve(ImageHandle handle) => images.GetValueOrDefault(handle.Value);
    }

    private static (byte[] Png, RgbaImage Image) Disc(int size)
    {
        var rgba = new byte[size * size * 4];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var d = MathF.Sqrt(((x - (size / 2f) + .5f) * (x - (size / 2f) + .5f)) + ((y - (size / 2f) + .5f) * (y - (size / 2f) + .5f)));
                var i = ((y * size) + x) * 4;
                var inside = d < (size / 2f) - 1;
                rgba[i] = (byte)(x * 255 / size);
                rgba[i + 1] = (byte)(y * 255 / size);
                rgba[i + 2] = 80;
                rgba[i + 3] = inside ? (byte)255 : (byte)0;
            }
        }

        return (OneGame.Host.Png.Encode(new CapturedFrame(size, size, rgba, "t")), new RgbaImage(size, size, rgba));
    }

    private const string Svg = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 20 20' width='40' height='40'><circle cx='10' cy='10' r='8' fill='currentColor'/><rect x='2' y='2' width='6' height='6' fill='#e33'/></svg>";

    [Fact]
    public void ImagesSvgAndTilesMatchTheReference()
    {
        using var rig = new Rig(240, 140);
        var (png, image) = Disc(32);
        var handle = rig.Images.Add("coin.png", png);
        var html = $$"""
            <body style="margin:0;background:#fff">
            <img src="coin.png" style="position:absolute;left:8px;top:8px;width:64px;height:64px">
            <img src="coin.png" style="position:absolute;left:84px;top:8px;width:64px;height:64px;image-rendering:pixelated">
            <div style="position:absolute;left:160px;top:8px;width:70px;height:64px;background:#eee url(coin.png) repeat;background-size:20px 20px"></div>
            <div style="position:absolute;left:8px;top:84px;width:60px;height:48px;color:#06c">{{Svg}}</div>
            <div style="position:absolute;left:84px;top:84px;width:60px;height:48px;color:#c60;font-size:12px">{{Svg}}</div>
            </body>
            """;
        var screen = rig.Pages.Show("p", PageScreen.FromHtml(html, rig.Options()));
        var gpu = rig.Render();
        Shots.Save("images", 240, 140, gpu);
        var reference = DisplayListRasterizer.Render(screen.Draw!.Value.List, 240, 140, new RasterOptions
        {
            Background = PaintColor.White,
            Images = new Resolver(new Dictionary<int, RgbaImage> { [handle.Value] = image }),
        }, TestContext.Current.CancellationToken).Pixels;
        Shots.Save("images-ref", 240, 140, reference);
        var (max, mean, share) = Shots.Diff(gpu, reference, 12);
        Shots.Note($"images vs reference: max {max}, mean {mean:F3}, over-12 {share:P3}");
        Assert.True(share < 0.02, $"{share:P3} differ (max {max})");
        Shots.Golden("images", 240, 140, gpu);
        Assert.True(rig.Renderer.Stats.ArtPages > 0);
        Assert.Equal(0, rig.Renderer.Stats.MissingImages);
    }

    [Fact]
    public void PathsAndHostScopesAreDrawn()
    {
        using var rig = new Rig(200, 110);
        var list = new DisplayList();
        var star = new byte[] { 0, 1, 1, 1, 1, 4 };
        var starPts = new float[] { 50, 8, 82, 100, 8, 40, 92, 40, 18, 100 };
        var id = 1UL;
        list.FillPath(new ItemId(id++), star.AsSpan(0, 5), starPts, PaintColor.FromBytes(200, 30, 30), FillRule.NonZero);
        var shifted = starPts.Select((v, i) => i % 2 == 0 ? v + 100 : v).ToArray();
        list.FillPath(new ItemId(id++), star.AsSpan(0, 5), shifted, PaintColor.FromBytes(30, 90, 200), FillRule.EvenOdd);
        var gpu = RenderList(rig, list, out var reference);
        Shots.Save("paths", 200, 110, gpu);
        Shots.Save("paths-ref", 200, 110, reference);
        var (max, mean, share) = Shots.Diff(gpu, reference, 12);
        Shots.Note($"paths vs reference: max {max}, mean {mean:F3}, over-12 {share:P3}");
        Assert.True(share < 0.04, $"{share:P3} differ (max {max})");
        Shots.Golden("paths", 200, 110, gpu);
    }

    [Fact]
    public void ReplacingAnImageShowsTheNewPixelsAndTheGraphListsEffectPasses()
    {
        using var rig = new Rig(80, 80);
        var (png, _) = Disc(32);
        rig.Images.Add("coin.png", png);
        rig.Pages.Show("p", PageScreen.FromHtml("""<body style="margin:0"><img src="coin.png" style="width:64px;height:64px;filter:blur(1px)"></body>""", rig.Options()));
        var before = rig.Render();
        var rgba = new byte[32 * 32 * 4];
        for (var i = 0; i < rgba.Length; i += 4)
        {
            (rgba[i], rgba[i + 1], rgba[i + 2], rgba[i + 3]) = (250, 20, 20, 255);
        }

        rig.Images.Replace("coin.png", OneGame.Host.Png.Encode(new CapturedFrame(32, 32, rgba, "t")));
        var after = rig.Render();
        Shots.Save("replace-before", 80, 80, before);
        Shots.Save("replace-after", 80, 80, after);
        // The disc's centre went from a green-blue gradient to solid red.
        var centre = ((40 * 80) + 40) * 4;
        Assert.True(after[centre] > 200 && after[centre + 1] < 80, $"centre is {after[centre]},{after[centre + 1]},{after[centre + 2]}");
        Assert.NotEqual(before[centre], after[centre]);
        var names = rig.Graph.Describe().Passes.Select(p => p.Name).ToArray();
        Assert.Contains("page.blur", names);
        Assert.Contains("page.layer", names);
    }

    [Fact]
    public void AFrameThatChangesNothingAllocatesNothing()
    {
        using var rig = new Rig(160, 150);
        rig.Pages.Show("p", PageScreen.FromHtml(RenderTests.BoxesHtml, rig.Options()));
        for (var i = 0; i < 5; i++)
        {
            rig.Draw();
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 20; i++)
        {
            rig.Draw();
        }

        var perFrame = (GC.GetAllocatedBytesForCurrentThread() - before) / 20;
        Shots.Note($"allocation per steady page frame: {perFrame} B");
        Assert.True(perFrame < 20_000, $"{perFrame} B per frame");
    }

    private static byte[] RenderList(Rig rig, DisplayList list, out byte[] reference)
    {
        var screen = rig.Pages.Show("list", PageScreen.FromList(list, null));
        var gpu = rig.Render();
        reference = rig.Reference(screen);
        return gpu;
    }
}
