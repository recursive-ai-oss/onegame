using System;
using System.IO;
using OneGame.Platform;
using OneGame.Core;
using OneGame.Host;
using OneGame.UI;
using OneGame.UI.Kit;

namespace AndroidTextGame;

/// <summary>One text field and a label echoing it: the smallest game that needs the system keyboard. It drives the soft-keyboard checks on a device.</summary>
public sealed class TextGame : GameDefinition
{
    private string _text = "";
    private int _backs;
    private int _lastClaimW;
    private int _lastClaimH;

    public override string Name => "AndroidTextGame";

    public override void RegisterEntities(EntityRegistry registry)
    {
    }

    public override void Setup(World world)
    {
    }

    // Device checks for back and edge claims (see docs/interfaces/android.md): the first two backs are the game's (logged), the third is left
    // to the host, which leaves the app. A file named `noclaim` in the working directory turns the right-edge claim off.
    public override bool OnBack(GameInstance game)
    {
        _backs++;
        Console.WriteLine($"probe: back {_backs}");
        return _backs <= 2;
    }

    public override void OnEachFrame(GameInstance game)
    {
        var window = game.Window;
        if (window.Width == _lastClaimW && window.Height == _lastClaimH)
        {
            return;
        }

        _lastClaimW = window.Width;
        _lastClaimH = window.Height;
        if (File.Exists("noclaim"))
        {
            window.ClaimSystemGestures([]);
            Console.WriteLine("probe: claim off");
            return;
        }

        // A strip on the right edge, as tall as the system allows, centred.
        var limit = window.GestureClaimLimit > 0 ? Math.Min(window.GestureClaimLimit, window.Height) : window.Height;
        window.ClaimSystemGestures([new GestureRegion(window.Width - 120, (window.Height - limit) / 2, 120, limit)]);
        Console.WriteLine($"probe: size {window.Width}x{window.Height} safe {window.SafeArea} claim {window.ClaimedEdges} limit {window.GestureClaimLimit}");
    }

    public override void OnInput(GameInstance game, in PlatformEvent input)
    {
        if (input.Kind == PlatformEventKind.SafeAreaChanged)
        {
            Console.WriteLine($"probe: safe area {input.Insets}");
        }

        if (input.Kind is PlatformEventKind.TouchDown or PlatformEventKind.TouchMove or PlatformEventKind.TouchUp or PlatformEventKind.TouchCancel)
        {
            Console.WriteLine($"probe: {input.Kind} {input.X:0},{input.Y:0}");
        }
    }

    public override void DefineFonts(OneGame.Text.FontLibrary fonts)
    {
        ArgumentNullException.ThrowIfNull(fonts);
        fonts.Add(Read("fonts/NotoSans-Regular.ttf"));
    }

    public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => Ui.Column(
        UiKit.TextInput(() => _text, v => _text = v, placeholder: "Type here", id: "field").Width(Length.Ui(240)),
        Ui.Label(() => "echo: " + _text).Named("echo"))
        .Gap(Length.Ui(8)).Padding(Length.Ui(16)).TextColor(Color.White));

    private static byte[] Read(string name)
    {
        using var stream = typeof(TextGame).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException($"The program has no embedded file '{name}'.");
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }
}
