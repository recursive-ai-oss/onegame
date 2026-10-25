using System;
using System.IO;
using OneGame.Core;
using OneGame.Host;
using OneGame.UI;
using OneGame.UI.Kit;

namespace AndroidTextGame;

/// <summary>One text field and a label echoing it: the smallest game that needs the system keyboard. It drives the soft-keyboard checks on a device.</summary>
public sealed class TextGame : GameDefinition
{
    private string _text = "";

    public override string Name => "AndroidTextGame";

    public override void RegisterEntities(EntityRegistry registry)
    {
    }

    public override void Setup(World world)
    {
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
