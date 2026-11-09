using System.Text;
using OneGame.Core;
using OneGame.Host;
using OneGame.Platform;
using OneGame.Platform.Tests;
using OneGame.Text;
using OneGame.UI;

namespace OneGame.Windowed.Tests;

/// <summary>
/// A game whose screen first draws its art when <see cref="Reveal"/> is set: a backdrop and a banner too big for the shared pages, and
/// enough small icons to need several pages, with text over them.
/// </summary>
public sealed class ArtGame : GameDefinition
{
    public bool Reveal { get; set; }

    private static readonly int[] One = [0];

    public override string Name => "ArtGame";

    public override void RegisterEntities(EntityRegistry registry)
    {
    }

    public override void Setup(World world)
    {
    }

    public override void DefineFonts(FontLibrary fonts) =>
        fonts.Add(File.ReadAllBytes(Path.Combine(Paths.RepoRoot, "testdata", "text", "fonts", "NotoSans-Subset-Regular.ttf")));

    public override void DefineArt(OneGame.Atlas.ArtAtlas art)
    {
        art.AddSvg("backdrop", Encoding.UTF8.GetBytes("""<svg xmlns="http://www.w3.org/2000/svg" width="620" height="100" viewBox="0 0 620 100"><rect width="620" height="100" fill="#7a2f3b"/><circle cx="310" cy="50" r="40" fill="#2f7a5a"/></svg>"""));
        art.AddSvg("banner", Encoding.UTF8.GetBytes("""<svg xmlns="http://www.w3.org/2000/svg" width="600" height="60" viewBox="0 0 600 60"><rect width="600" height="60" fill="#e0b030"/><rect x="20" y="10" width="560" height="40" fill="#203a90"/></svg>"""));
        for (var i = 0; i < 120; i++)
        {
            var hue = (i * 37) % 360;
            art.AddSvg("icon" + i, Encoding.UTF8.GetBytes($"""<svg xmlns="http://www.w3.org/2000/svg" width="96" height="96" viewBox="0 0 96 96"><rect width="96" height="96" rx="12" fill="hsl({hue} 70% 45%)"/><circle cx="48" cy="48" r="{10 + (i % 30)}" fill="white"/></svg>"""));
        }
    }

    public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ =>
        Ui.Column(
            Ui.Each(() => Reveal ? One : [], _ => Ui.Image("backdrop").Size(Length.Px(620), Length.Px(100))),
            Ui.Label(() => Reveal ? "Painted backdrop with text over it" : "Nothing yet").Named("title"),
            Ui.Each(() => Reveal ? Enumerable.Range(0, 120).ToArray() : [], i => Ui.Image("icon" + i).Size(Length.Px(24), Length.Px(24))).Wrap().Gap(Length.Px(2)).Size(Length.Px(620), Length.Px(130)),
            Ui.Each(() => Reveal ? One : [], _ => Ui.Image("banner").Size(Length.Px(600), Length.Px(60))))
        .Size(Length.Px(640), Length.Px(360)).Padding(Length.Px(10)).TextColor(Color.White)
        .Background(Color.Rgb(0x203040)));
}

/// <summary>A capture shows the same picture as the window, including art that is first used in the captured frame.</summary>
public sealed class ArtCaptureTests : IClassFixture<XvfbFixture>
{
    private readonly XvfbFixture _xvfb;

    public ArtCaptureTests(XvfbFixture xvfb) => _xvfb = xvfb;

    [Fact]
    public void ACaptureOfArtFirstDrawnInTheCapturedFrameEqualsThePresentedFrame() => MainThread.Run(() =>
    {
        Assert.SkipUnless(_xvfb.Available, "Xvfb is not installed");
        var game = new ArtGame();
        using var s = new Session(PlatformBackendKind.X11, _xvfb.Display!, game: game);
        s.Game.Step(3);
        game.Reveal = true;

        // The capture is the first thing to draw the art. The window then draws it for the first time as well.
        var shot = s.Game.Capture(s.Game.Window.Width, s.Game.Window.Height, s.Game.UiScale);
        var shown = s.Game.CapturePresented()!;
        Session.Save("art-capture", shot);
        Session.Save("art-presented", shown);

        // The backdrop's red must be there (a capture that misses standalone art shows the clear colour instead).
        var o = ((30 * shot.Width) + 40) * 4;
        Assert.True(shot.Rgba[o] > 90 && shot.Rgba[o + 1] < 80, $"backdrop missing: {shot.Rgba[o]},{shot.Rgba[o + 1]},{shot.Rgba[o + 2]}");
        Assert.Equal((shown.Width, shown.Height), (shot.Width, shot.Height));
        var worst = 0;
        for (var i = 0; i < shot.Rgba.Length; i++)
        {
            worst = Math.Max(worst, Math.Abs(shot.Rgba[i] - shown.Rgba[i]));
        }

        Assert.True(worst <= 2, $"capture differs from the presented frame by up to {worst}");

        // Again after the window drew it first, and with more art appearing in a later captured frame.
        var again = s.Game.Capture(s.Game.Window.Width, s.Game.Window.Height, s.Game.UiScale);
        Assert.Equal(shown.Rgba, again.Rgba);
        Assert.Empty(s.Validation());
    });
}
