using System;
using System.Globalization;
using System.IO;
using System.Numerics;
using OneGame.Core;
using OneGame.Host;
using OneGame.Platform;
using OneGame.Render2D;
using OneGame.UI;
using WebGame.Shaders;

namespace WebGame;

/// <summary>
/// Reports what the pointer did, frame by frame, on the console as <c>edge frame=N kind id x y</c> lines (the positions each press, move and lift
/// really had) and the window's size and safe area whenever they change, and draws a square with its own HLSL material as the first thing of every frame.
/// </summary>
public sealed class WebGameDefinition : GameDefinition
{
    public override string Name => "Web Fixture";

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.WebGame.EntityTypes.Register(registry);

    public override void Setup(World world)
    {
        world.Add(new Swatch());
        world.Add(new Glow());
        world.Add(new TextLoad());
    }

    public override void DefineScreens(ScreenSet screens) => screens.Add("main", game => Ui.Column(
        Ui.Box().Named("glass").Style(s => s.Position(PositionMode.Absolute).Left(0).Top(0).Right(0).Bottom(0).BackdropBlur(Length.Px(6)))
            .BindVisible(() => game.World.Entities[0] is Swatch { Glass: true })).Padding(Length.Px(8)).Size(Length.Percent(100), Length.Percent(100)));

    public override void DefineFonts(OneGame.Text.FontLibrary fonts)
    {
        ArgumentNullException.ThrowIfNull(fonts);
        using var stream = typeof(WebGameDefinition).Assembly.GetManifestResourceStream("fonts/NotoSans-Regular.ttf") ?? throw new InvalidOperationException("The program has no embedded font.");
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        fonts.Add(copy.ToArray());
    }

    // Unsafe code on purpose: the project asks for AllowUnsafeBlocks, and a web export has to honour that.
    internal static unsafe int Peek(int* p) => *p;

    private string _lastWindow = "";

    public override void OnEachFrame(GameInstance game)
    {
        ArgumentNullException.ThrowIfNull(game);
        var w = game.Window;
        var now = string.Create(CultureInfo.InvariantCulture, $"window {w.Width}x{w.Height} scale={w.Scale:0.###} safe={w.SafeArea.Left},{w.SafeArea.Top},{w.SafeArea.Right},{w.SafeArea.Bottom}");
        if (now != _lastWindow)
        {
            _lastWindow = now;
            Console.WriteLine(now);
        }

        foreach (var e in game.Platform.Input.PointerEdges)
        {
            if (e.FromTouch)
            {
                continue;
            }

            var kind = e.Kind.ToString().ToLowerInvariant();
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"edge frame={game.Frame} {kind} id={e.TouchId} x={e.X:0.###} y={e.Y:0.###}"));
        }
    }
}
