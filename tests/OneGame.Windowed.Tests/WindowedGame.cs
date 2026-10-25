using OneGame.Core;
using OneGame.Host;
using OneGame.Text;
using OneGame.UI;
using OneGame.UI.Kit;

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
/// The game the windowed tests run: a label, a button and a text field. The field is the kit's text input.
/// </summary>
public sealed class WindowedGame : GameDefinition
{
    public override string Name => "WindowedGame";

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.OneGame_Windowed_Tests.EntityTypes.Register(registry);

    public override void Setup(World world) => world.Add(new Clicker());

    public override void DefineFonts(FontLibrary fonts) =>
        fonts.Add(File.ReadAllBytes(Path.Combine(Paths.RepoRoot, "testdata", "text", "fonts", "NotoSans-Subset-Regular.ttf")));

    public override void DefineScreens(ScreenSet screens)
    {
        var installed = false;
        screens.Add("main", game =>
        {
            if (!installed)
            {
                installed = true;
                _ = new KitHost(game.Ui);
            }

            var field = UiKit.TextInput(id: "name", placeholder: "Name");

            return Ui.Column(
                Ui.Label(() => "Clicks: " + ((Clicker)game.World.Entities[0]).Clicks).Named("clicks"),
                Ui.Button("Click me", () => ((Clicker)game.World.Entities[0]).Send.Click()).Named("click").Size(Length.Ui(80), Length.Ui(20)).Background(Color.Rgb(0x3060A0)),
                field.Size(Length.Ui(220), Length.Ui(32)))
                .Padding(Length.Px(16)).Gap(Length.Px(8)).Background(Color.Rgb(0x203040)).TextColor(Color.White);
        });
    }
}

internal static class Paths
{
    private static string SourceDir([System.Runtime.CompilerServices.CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;

    public static string RepoRoot => Path.GetFullPath(Path.Combine(SourceDir(), "..", ".."));
}
