using System.Numerics;

namespace OneGame.UI.Tests;

/// <summary>Background repeat, size and position resolve to tile geometry in the display list.</summary>
public class BackgroundTests
{
    private sealed class Sizes : IArtSizes
    {
        public bool TryGetNaturalSize(string id, out float width, out float height)
        {
            (width, height) = id switch
            {
                "tile" => (8f, 4f),
                "wide" => (200f, 100f),
                _ => (0f, 0f),
            };
            return width > 0;
        }
    }

    private static DrawCommand Image(Action<StyleBuilder> style, float scale = 1, bool sizes = true)
    {
        var ui = new UiTree { ArtSizes = sizes ? new Sizes() : null };
        ui.SetViewport(300 * scale, 200 * scale, scale);
        var box = Ui.Box().Style(s => s.Size(Length.Ui(100), Length.Ui(60)).Position(PositionMode.Absolute).Left(Length.Ui(10)).Top(Length.Ui(20)));
        box.Style(style);
        ui.Root.With(box);
        return ui.Frame().Commands.ToArray().Single(c => c.Kind == DrawKind.Image);
    }

    [Fact]
    public void ThePlainBackgroundStaysStretchedAndNeedsNoSizes()
    {
        var c = Image(s => s.BackgroundImage("tile"), sizes: false);
        Assert.Equal(default, c.TileSize);
        Assert.Equal(new Rect(10, 20, 100, 60), c.Bounds);
        Assert.Equal(default, Image(s => s.BackgroundImage("tile")).TileSize);
    }

    [Fact]
    public void RepeatUsesTheNaturalSizeTimesTheUiScale()
    {
        var c = Image(s => s.BackgroundImage("tile").BackgroundRepeat(BackgroundRepeat.Repeat).BackgroundSize(BackgroundSizing.Auto), scale: 1.5f);
        Assert.Equal(new Vector2(12, 6), c.TileSize);
        Assert.Equal(TileAxes.X | TileAxes.Y, c.Repeat);
        Assert.Equal(new Vector2(15, 30), c.TileOrigin);   // the box's own corner at scale 1.5
    }

    [Fact]
    public void ExplicitSizeKeepsTheAspectRatioWhenOneSideIsAuto()
    {
        var c = Image(s => s.BackgroundImage("tile").BackgroundRepeat(BackgroundRepeat.RepeatX).BackgroundSize(Length.Ui(20), Length.Auto));
        Assert.Equal(new Vector2(20, 10), c.TileSize);
        Assert.Equal(TileAxes.X, c.Repeat);
        var d = Image(s => s.BackgroundImage("tile").BackgroundSize(Length.Auto, Length.Percent(50)));
        Assert.Equal(new Vector2(60, 30), d.TileSize);
        Assert.Equal(TileAxes.None, d.Repeat);
    }

    [Fact]
    public void CoverAndContainScaleToTheBox()
    {
        // 200×100 into 100×60: cover scales by max(0.5, 0.6) = 0.6, contain by 0.5.
        var cover = Image(s => s.BackgroundImage("wide").BackgroundSize(BackgroundSizing.Cover)).TileSize;
        Assert.Equal((120f, 60f), (MathF.Round(cover.X, 2), MathF.Round(cover.Y, 2)));
        var contain = Image(s => s.BackgroundImage("wide").BackgroundSize(BackgroundSizing.Contain)).TileSize;
        Assert.Equal((100f, 50f), (MathF.Round(contain.X, 2), MathF.Round(contain.Y, 2)));
    }

    [Fact]
    public void PercentPositionAlignsTheSameFractionOfTileAndBox()
    {
        var c = Image(s => s.BackgroundImage("wide").BackgroundSize(BackgroundSizing.Contain).BackgroundPosition(Length.Percent(50), Length.Percent(100)));
        // The 100×50 image has 0 free width and 10 free height inside the 100×60 box.
        Assert.Equal(new Vector2(10, 30), c.TileOrigin);
        var d = Image(s => s.BackgroundImage("tile").BackgroundRepeat(BackgroundRepeat.Repeat).BackgroundSize(BackgroundSizing.Auto).BackgroundPosition(Length.Ui(3), Length.Px(2)));
        Assert.Equal(new Vector2(13, 22), d.TileOrigin);
    }

    [Fact]
    public void RepeatedTilesAreWholePixels()
    {
        var c = Image(s => s.BackgroundImage("tile").BackgroundRepeat(BackgroundRepeat.Repeat).BackgroundSize(Length.Ui(10.4f), Length.Ui(5.4f)));
        Assert.Equal(new Vector2(10, 5), c.TileSize);
    }

    [Fact]
    public void TheDisplayListTextShowsTheTile()
    {
        var ui = new UiTree { ArtSizes = new Sizes() };
        ui.SetViewport(100, 100);
        ui.Root.With(Ui.Box().Style(s => s.Size(40, 40).BackgroundImage("tile").BackgroundRepeat(BackgroundRepeat.RepeatY).BackgroundSize(BackgroundSizing.Auto)));
        Assert.Contains("tile=8x4@0,0 repeat=Y", ui.Frame().ToText());
    }

    [Fact]
    public void StyleSheetsTakeTheNewPropertiesByTheirCssNames()
    {
        var ui = new UiTree { ArtSizes = new Sizes() };
        ui.SetViewport(100, 100);
        ui.AddStyleSheet(new StyleSheet().Rule(".bg", s => s.BackgroundImage("tile").BackgroundRepeat(BackgroundRepeat.Repeat).BackgroundSize(BackgroundSizing.Auto)));
        var e = Ui.Box().Class("bg").Style(s => s.Size(40, 40));
        ui.Root.With(e);
        ui.Frame();
        Assert.Equal("repeat", e.Computed.Get("background-repeat"));
        Assert.Equal("auto", e.Computed.Get("background-size"));
    }
}
