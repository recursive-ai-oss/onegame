using OneGame.Atlas;
using OneGame.Core;
using OneGame.Gpu;
using OneGame.Host;
using OneGame.Pages;
using OneGame.Render2D;
using OneGame.Text;
using OneGame.UI;

namespace OneGame.AotSmoke;

/// <summary>A OneRender page under the native publish: layout, style, the walker, the shaders and the draw path through the 2D stage.</summary>
internal static class PagesSmoke
{
    public static string? Run(GpuDevice device, byte[] font)
    {
        var fonts = new FontLibrary();
        fonts.Add(font);
        using var stage = new Render2DStage(new HostResources(fonts, new FontTextMeasurer(fonts), new ArtAtlas(), GeneratedShaders.Instance));
        var pages = new PageSet();
        stage.Pages = pages;
        var pageFonts = new PageFonts(fonts);
        pageFonts.Add(font, "Body");
        var options = new PageOptions(pageFonts);
        pages.Show("menu", PageScreen.FromHtml("""<body style="margin:0;font:14px Body"><div style="position:absolute;left:4px;top:4px;width:60px;height:30px;background:linear-gradient(#e33,#33e);border-radius:6px;filter:blur(1px);opacity:.9">Hi</div></body>""", options));
        using var target = device.CreateTexture(new TextureDesc(96, 64, GpuFormat.Rgba8UnormSrgb, TextureUsage.RenderTarget | TextureUsage.CopySource));
        var world = new World(1, new EntityRegistry());
        stage.Render(device, target, new FrameInfo(96, 64, 1, world.Tick, new DisplayList(), world, Fixed.Zero));
        var px = new byte[96 * 64 * 4];
        device.ReadTexture(target, 0, 0, px);
        var lit = 0;
        for (var i = 0; i < px.Length; i += 4)
        {
            lit += px[i] > 120 && px[i + 2] < 120 ? 1 : 0;
        }

        return lit < 100 ? $"the page drew {lit} red pixels" : null;
    }
}
