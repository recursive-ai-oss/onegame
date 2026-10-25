using OneGame.Core;
using OneGame.Host;
using OneGame.Text;
using OneGame.UI;

namespace OneGame.Windowed.Tests;

/// <summary>A counter the button bumps.</summary>
[Entity]
public partial class Clicker : Entity
{
    [Synced] public partial int Clicks { get; private set; }

    [Command]
    public void Click() => Clicks++;
}

/// <summary>
/// The game the windowed tests run: a label, a button and a text field. The field is built from plain UI events (typed text, pre-edit text,
/// Backspace, Control+C and Control+V on the system clipboard), the way a game's own text input would be.
/// </summary>
public sealed class WindowedGame : GameDefinition
{
    public override string Name => "WindowedGame";

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.OneGame_Windowed_Tests.EntityTypes.Register(registry);

    public override void Setup(World world) => world.Add(new Clicker());

    public override void DefineFonts(FontLibrary fonts) =>
        fonts.Add(File.ReadAllBytes(Path.Combine(Paths.RepoRoot, "testdata", "text", "fonts", "NotoSans-Subset-Regular.ttf")));

    public override void DefineScreens(ScreenSet screens) =>
        screens.Add("main", game =>
        {
            var text = "";
            var preedit = "";
            var field = Ui.Label(() => "Name: " + text + (preedit.Length > 0 ? "[" + preedit + "]" : ""))
                .Named("name")
                .Focusable()
                .Size(Length.Ui(220), Length.Ui(32))
                .Padding(Length.Px(4))
                .Border(Length.Px(2), Color.Rgb(0x88AACC))
                .Background(Color.Rgb(0x101820));
            field.AcceptsText = true;
            field.On(UiEventKind.TextInput, (_, e) =>
            {
                text += e.Text;
                return true;
            });
            field.On(UiEventKind.Composition, (_, e) =>
            {
                preedit = e.Text ?? "";
                return true;
            });
            field.On(UiEventKind.KeyDown, (_, e) =>
            {
                switch (e.Key)
                {
                    case "Backspace" when text.Length > 0:
                        text = text[..^1];
                        return true;
                    case "v" when (e.Modifiers & KeyModifiers.Ctrl) != 0:
                        text += game.Clipboard.GetText() ?? "";
                        return true;
                    case "c" when (e.Modifiers & KeyModifiers.Ctrl) != 0:
                        game.Clipboard.SetText(text);
                        return true;
                    default:
                        return false;
                }
            });

            return Ui.Column(
                Ui.Label(() => "Clicks: " + ((Clicker)game.World.Entities[0]).Clicks).Named("clicks"),
                Ui.Button("Click me", () => ((Clicker)game.World.Entities[0]).Send.Click()).Named("click").Size(Length.Ui(80), Length.Ui(20)).Background(Color.Rgb(0x3060A0)),
                field)
                .Padding(Length.Px(16)).Gap(Length.Px(8)).Background(Color.Rgb(0x203040)).TextColor(Color.White);
        });
}

internal static class Paths
{
    private static string SourceDir([System.Runtime.CompilerServices.CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;

    public static string RepoRoot => Path.GetFullPath(Path.Combine(SourceDir(), "..", ".."));

    public static string Shaders => Path.Combine(RepoRoot, "tests", "OneGame.Render2D.Tests", "Shaders");
}
