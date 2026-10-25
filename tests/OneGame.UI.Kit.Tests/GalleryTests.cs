using System.Runtime.CompilerServices;
using OneGame.Atlas;
using OneGame.Render2D.Tests;
using OneGame.Text;

namespace OneGame.UI.Kit.Tests;

/// <summary>
/// The state galleries drawn by the 2D renderer at 720p and compared with committed references (lavapipe), plus checks of the computed styles behind them.
/// Set ONEGAME_UPDATE_GOLDENS=1 to rewrite the references, ONEGAME_CAPTURE_DIR to write each capture as a PNG.
/// </summary>
public class GalleryTests
{
    private const int Tolerance = 3;
    private const double EdgeBudget = 0.01;

    private static string Dir([CallerFilePath] string here = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "testdata"));

    private static void RequireGpu() =>
        Assert.SkipUnless(OneGame.Gpu.GpuBackends.IsAvailable && (OneGame.Gpu.GpuBackends.Selected == OneGame.Gpu.GpuBackend.Metal || Environment.GetEnvironmentVariable("VK_ADD_LAYER_PATH") is { Length: > 0 }), "needs a graphics driver and, on Vulkan, the validation layer");

    private static (FontTextMeasurer Measurer, GlyphAtlas Glyphs, ArtAtlas Art) Resources()
    {
        var lib = new FontLibrary();
        lib.Add(File.ReadAllBytes(Path.Combine(Dir(), "text", "fonts", "NotoSans-Subset-Regular.ttf")));
        lib.Add(File.ReadAllBytes(Path.Combine(Dir(), "text", "fonts", "NotoSans-Subset-Bold-kern.ttf")));
        var art = new ArtAtlas();
        foreach (var a in KitArt.Assets)
        {
            art.AddSvg(a.Id, a.Svg, new NineSlice(a.Slice, a.Slice, a.Slice, a.Slice));
        }

        var measurer = new FontTextMeasurer(lib);
        return (measurer, new GlyphAtlas(lib), art);
    }

    private static Screen NewScreen(KitTokens tokens, int width = 1280, int height = 720, ITextMeasurer? measurer = null)
    {
        var screen = new Screen(width, height, 1, measurer);
        screen.Host.SetTheme(KitTheme.Create(tokens));
        return screen;
    }

    private static byte[] Render(Screen screen, int width, int height)
    {
        var (measurer, glyphs, art) = Resources();
        using var rig = new Rig(width, height);
        var text = new TextQuadBuilder(measurer, glyphs);
        var pixels = rig.Render(() => rig.Renderer.Ui.Draw(screen.Ui.DisplayList, text, glyphs, art), new Color(0, 0, 0, 255));
        Assert.Equal(0, rig.Renderer.Ui.MissingArt);
        return pixels;
    }

    private static void Check(string name, int width, int height, byte[] pixels)
    {
        if (Environment.GetEnvironmentVariable("ONEGAME_CAPTURE_DIR") is { Length: > 0 } capture)
        {
            Directory.CreateDirectory(capture);
            Png.Write(Path.Combine(capture, name + ".png"), width, height, pixels);
        }

        var path = Path.Combine(Dir(), "ui-kit", "states", name + ".png");
        if (Environment.GetEnvironmentVariable("ONEGAME_UPDATE_GOLDENS") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            Png.Write(path, width, height, pixels);
        }

        var (w, h, expected) = Png.Read(path);
        Assert.Equal((w, h), (width, height));
        int max = 0, outside = 0;
        for (var p = 0; p < w * h; p++)
        {
            var worst = 0;
            for (var c = 0; c < 4; c++)
            {
                worst = Math.Max(worst, Math.Abs(pixels[(p * 4) + c] - expected[(p * 4) + c]));
            }

            max = Math.Max(max, worst);
            if (worst > Tolerance)
            {
                outside++;
            }
        }

        var share = outside / (double)(w * h);
        if (Environment.GetEnvironmentVariable("ONEGAME_GOLDEN_REPORT") is { Length: > 0 } report)
        {
            File.AppendAllText(report, $"{name} maxDiff={max} outsideTolerance={share:P3}\n");
        }

        Assert.True(share <= EdgeBudget, $"{name}: {share:P2} of pixels differ by more than {Tolerance} (max diff {max})");
    }

    private static Screen Arranged(string page, KitTokens tokens, ITextMeasurer? measurer)
    {
        var s = NewScreen(tokens, measurer: measurer);
        if (page == "overlay")
        {
            Gallery.Overlay(s.Host);
            s.Settle();
            s.Click(s.Get("size/head"));
            s.Key("ArrowDown");
            s.Settle();
            s.Get("confirm").IsEnabled = false;
            s.Settle();
            return s;
        }

        var (_, rows) = page switch
        {
            "buttons" => Gallery.Buttons(s.Host),
            "controls" => Gallery.Controls(s.Host),
            _ => Gallery.Collections(s.Host),
        };
        s.Settle();
        Gallery.State[] states = page == "buttons" ? [.. Gallery.Columns, Gallery.State.FocusedDisabled] : Gallery.Columns;
        foreach (var row in rows)
        {
            foreach (var st in states)
            {
                Gallery.Force(s, row, st);
            }
        }

        s.Settle();
        return s;
    }

    public static TheoryData<string, string> Captures => new()
    {
        { "buttons", "dark" }, { "buttons", "amber" }, { "buttons", "light" },
        { "controls", "dark" }, { "controls", "amber" },
        { "collections", "dark" }, { "collections", "amber" },
        { "overlay", "dark" }, { "overlay", "amber" }, { "overlay", "light" },
    };

    [Theory]
    [MemberData(nameof(Captures))]
    public void StateGalleryAt720p(string page, string theme)
    {
        RequireGpu();
        var (measurer, _, _) = Resources();
        var s = Arranged(page, ContrastTests.Tokens(theme), measurer);
        Check($"{page}-{theme}-720p", 1280, 720, Render(s, 1280, 720));
    }

    /// <summary>The Continue / New campaign pair of a title screen: a disabled primary beside a focused secondary, on the panel.</summary>
    private static (Screen Screen, Element Disabled, Element Focused) Pair(KitTokens tokens, ITextMeasurer? measurer = null)
    {
        var s = NewScreen(tokens, 640, 120, measurer);
        var cont = UiKit.Button("CONTINUE", () => { }, "continue", ButtonKind.Primary);
        cont.IsEnabled = false;
        var fresh = UiKit.Button("NEW CAMPAIGN", () => { }, "new");
        var row = Ui.Row(cont, fresh).Gap(Length.Ui(16)).Padding(Length.Ui(24)).Style(st => st.Anchor(AnchorPoint.Fill).Var("background-color", "--kit-panel"));
        cont.Style(st => st.Grow(1));
        fresh.Style(st => st.Grow(1));
        s.Host.Tree.Root.Add(row);
        s.Settle();
        s.Ui.Focus(fresh);
        s.Settle();
        return (s, cont, fresh);
    }

    private static (byte R, byte G, byte B) At(byte[] px, int width, float x, float y)
    {
        var i = (((int)y * width) + (int)x) * 4;
        return (px[i], px[i + 1], px[i + 2]);
    }

    private static double Ratio((byte R, byte G, byte B) a, (byte R, byte G, byte B) b) => KitContrast.Ratio(new Color(a.R, a.G, a.B), new Color(b.R, b.G, b.B));

    [Theory]
    [InlineData("dark")]
    [InlineData("amber")]
    [InlineData("light")]
    public void AFocusedSecondaryOutshinesADisabledPrimary(string theme)
    {
        RequireGpu();
        var (measurer, _, _) = Resources();
        var (s, cont, fresh) = Pair(ContrastTests.Tokens(theme), measurer);
        var px = Render(s, 640, 120);
        var panel = At(px, 640, 4, 4);
        var cb = cont.AbsoluteBounds;
        var fb = fresh.AbsoluteBounds;
        // A point inside the button, left of its text.
        var disabledFill = At(px, 640, cb.X + 6, cb.Y + (cb.Height / 2));
        // The ring is drawn 2 px outside the button and 2 px wide: its middle is 3 px out.
        var ring = At(px, 640, fb.X - 3, fb.Y + (fb.Height / 2));
        var ringVsPanel = Ratio(ring, panel);
        var fillVsPanel = Ratio(disabledFill, panel);
        if (Environment.GetEnvironmentVariable("ONEGAME_GOLDEN_REPORT") is { Length: > 0 } report)
        {
            File.AppendAllText(report, $"pair {theme}: focus ring vs panel {ringVsPanel:F2}:1, disabled primary fill vs panel {fillVsPanel:F2}:1\n");
        }

        if (Environment.GetEnvironmentVariable("ONEGAME_CAPTURE_DIR") is { Length: > 0 } capture)
        {
            Directory.CreateDirectory(capture);
            Png.Write(Path.Combine(capture, $"pair-{theme}.png"), 640, 120, px);
        }

        Assert.True(ringVsPanel >= 3, $"{theme}: the focus ring is {ringVsPanel:F2}:1 against the panel");
        Assert.True(fillVsPanel < ringVsPanel, $"{theme}: the disabled fill ({fillVsPanel:F2}:1) is stronger than the focus ring ({ringVsPanel:F2}:1)");
        Assert.True(fillVsPanel < 1.6, $"{theme}: the disabled fill is {fillVsPanel:F2}:1 against the panel and still reads as a button face");
    }

    // ---- computed styles, no GPU ----

    private static Color Fill(Element e) => e.Computed.C(StyleProp.BackgroundColor);

    private static float Ring(Element e) => e.Computed.L(StyleProp.OutlineWidth).Value;

    private static Color PaletteColor(string theme, string token) => KitTheme.Palette(ContrastTests.Tokens(theme)).First(p => p.Name == token).Color;

    [Theory]
    [MemberData(nameof(ContrastTests.Themes), MemberType = typeof(ContrastTests))]
    public void ButtonStatesAreDistinctForEveryVariant(string theme)
    {
        var s = NewScreen(ContrastTests.Tokens(theme));
        var (_, allRows) = Gallery.Buttons(s.Host);
        var rows = allRows.Where(r => !r.StartsWith("toggle", StringComparison.Ordinal)).ToArray();
        s.Settle();
        foreach (var row in rows)
        {
            foreach (var st in Gallery.Columns)
            {
                Gallery.Force(s, row, st);
            }
        }

        s.Settle();
        foreach (var row in rows)
        {
            Element Cell(Gallery.State st) => s.Get(Gallery.Id(row, st));
            var enabled = Cell(Gallery.State.Enabled);
            var hover = Cell(Gallery.State.Hover);
            var pressed = Cell(Gallery.State.Pressed);
            var focused = Cell(Gallery.State.Focused);
            var disabled = Cell(Gallery.State.Disabled);
            var tag = $"{theme}/{row}";

            // Hover and pressed change the fill.
            Assert.NotEqual(Fill(enabled), Fill(hover));
            Assert.NotEqual(Fill(hover), Fill(pressed));
            Assert.NotEqual(Fill(enabled), Fill(pressed));

            // Only focus draws a ring, on every variant, and it is the focus color.
            Assert.True(Ring(focused) > 0, tag + ": focus has no ring");
            Assert.All(new[] { enabled, hover, pressed, disabled }, e => Assert.Equal(0, Ring(e)));
            Assert.Equal(PaletteColor(theme, "--kit-focus"), focused.Computed.C(StyleProp.OutlineColor));

            // Disabled never keeps an accent or danger fill and never fades (opacity stays 1); its text is the disabled color.
            Assert.Equal(1f, disabled.Computed.N(StyleProp.Opacity));
            Assert.Equal(PaletteColor(theme, "--kit-disabled-text"), disabled.Computed.C(StyleProp.Color));
            if (row != "flat")
            {
                Assert.Equal(PaletteColor(theme, "--kit-disabled-fill"), Fill(disabled));
            }
            else
            {
                Assert.Equal(0, Fill(disabled).A);
            }

            // The label of every state that is meant to be read passes AA against the fill it sits on.
            foreach (var e in new[] { enabled, hover, pressed, focused })
            {
                var fill = Fill(e).A == 0 ? PaletteColor(theme, "--kit-panel") : Fill(e);
                var ratio = KitContrast.Ratio(e.Computed.C(StyleProp.Color), fill);
                Assert.True(ratio >= 4.5, $"{tag}: label is {ratio:F2}:1");
            }
        }
    }

    private static string Stroke(Element e) => e.Computed.C(StyleProp.BorderColor).ToString();

    [Theory]
    [MemberData(nameof(ContrastTests.Themes), MemberType = typeof(ContrastTests))]
    public void SwitchedOnToggleButtonsReadApartFromEveryOtherState(string theme)
    {
        var s = NewScreen(ContrastTests.Tokens(theme));
        var (_, rows) = Gallery.Buttons(s.Host);
        s.Settle();
        foreach (var row in rows)
        {
            foreach (var st in Gallery.Columns.Append(Gallery.State.FocusedDisabled))
            {
                Gallery.Force(s, row, st);
            }
        }

        s.Settle();
        Element Cell(string row, Gallery.State st) => s.Get(Gallery.Id(row, st));
        var pressedFill = PaletteColor(theme, "--kit-pressed-fill");

        foreach (var row in new[] { "toggle-on", "toggle-on-primary", "toggle-on-flat" })
        {
            var tag = $"{theme}/{row}";
            var on = Cell(row, Gallery.State.Enabled);
            var hover = Cell(row, Gallery.State.Hover);
            var down = Cell(row, Gallery.State.Pressed);
            var focused = Cell(row, Gallery.State.Focused);
            var disabled = Cell(row, Gallery.State.Disabled);

            // On: the one tint and the one outline, whatever the variant.
            Assert.Equal(pressedFill, Fill(on));
            Assert.Equal(PaletteColor(theme, "--kit-pressed-border"), on.Computed.C(StyleProp.BorderColor));
            Assert.Equal(PaletteColor(theme, "--kit-pressed-hover"), Fill(hover));
            Assert.Equal(PaletteColor(theme, "--kit-pressed-active"), Fill(down));
            Assert.Equal(3, new[] { Fill(on), Fill(hover), Fill(down) }.Distinct().Count());

            // Focus is still only the ring, and it does not change the tint.
            Assert.True(Ring(focused) > 0, tag + ": focus has no ring");
            Assert.Equal(Fill(on), Fill(focused));
            Assert.All(new[] { on, hover, down, disabled }, e => Assert.Equal(0, Ring(e)));

            // Disabled and on keeps the tint and the outline in the muted colors, apart from disabled and off and from enabled and on.
            Assert.Equal(PaletteColor(theme, "--kit-disabled-pressed-fill"), Fill(disabled));
            Assert.Equal(PaletteColor(theme, "--kit-disabled-text"), disabled.Computed.C(StyleProp.BorderColor));
            Assert.Equal(PaletteColor(theme, "--kit-disabled-text"), disabled.Computed.C(StyleProp.Color));
            Assert.NotEqual(Fill(on), Fill(disabled));
            Assert.NotEqual(Fill(Cell("normal", Gallery.State.Disabled)), Fill(disabled));
            Assert.Equal(1f, disabled.Computed.N(StyleProp.Opacity));

            // Every label that is meant to be read passes AA on the fill beneath it.
            foreach (var e in new[] { on, hover, down, focused })
            {
                var ratio = KitContrast.Ratio(e.Computed.C(StyleProp.Color), Fill(e));
                Assert.True(ratio >= 4.5, $"{tag}: label is {ratio:F2}:1");
            }

            var disabledRatio = KitContrast.Ratio(disabled.Computed.C(StyleProp.Color), Fill(disabled));
            Assert.True(disabledRatio >= 3, $"{tag}: disabled label is {disabledRatio:F2}:1");
        }

        // Against a toggle that is off: a different fill and a different outline, in every state a pointer can produce.
        var off = Cell("toggle-off", Gallery.State.Enabled);
        var onNormal = Cell("toggle-on", Gallery.State.Enabled);
        Assert.NotEqual(Fill(off), Fill(onNormal));
        Assert.NotEqual(Stroke(off), Stroke(onNormal));
        Assert.NotEqual(Fill(Cell("toggle-off", Gallery.State.Hover)), Fill(Cell("toggle-on", Gallery.State.Hover)));
        Assert.NotEqual(Fill(onNormal), Fill(Cell("normal", Gallery.State.Hover)));
        Assert.Equal(Fill(Cell("normal", Gallery.State.Enabled)), Fill(off));

        // The outline stands out from the tint inside it and the panel around it (3:1, WCAG 1.4.11).
        var border = PaletteColor(theme, "--kit-pressed-border");
        Assert.True(KitContrast.Ratio(border, pressedFill) >= 3, $"{theme}: outline vs tint {KitContrast.Ratio(border, pressedFill):F2}:1");
        Assert.True(KitContrast.Ratio(border, PaletteColor(theme, "--kit-panel")) >= 3, $"{theme}: outline vs panel");
    }

    [Fact]
    public void DisablingAGroupDisablesItsControls()
    {
        var s = NewScreen(KitTokens.Dark);
        var radio = UiKit.RadioGroup<int>([new Choice<int>(1, "A"), new Choice<int>(2, "B")], () => 1, _ => { }, "r");
        var card = UiKit.Card("Options", radio, UiKit.Button("Go", () => { }, "go"));
        s.Host.Tree.Root.Add(card);
        s.Settle();
        var enabledColor = s.Get("go").Computed.C(StyleProp.Color);
        card.IsEnabled = false;
        s.Settle();
        Assert.NotEqual(enabledColor, s.Get("go").Computed.C(StyleProp.Color));
        Assert.Equal(PaletteColor("dark", "--kit-disabled-fill"), Fill(s.Get("go")));
        Assert.Equal(PaletteColor("dark", "--kit-disabled-text"), s.Get("r/0").Computed.C(StyleProp.Color));
    }
}
