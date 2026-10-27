using System;
using System.IO;
using OneGame.Core;
using OneGame.Host;
using OneGame.UI;

namespace Draw2DGame;

/// <summary>A hero that walks along a floor, drawn with the 2D happy path: no render stage of its own.</summary>
public sealed class Draw2DGameDefinition : GameDefinition
{
    public override string Name => "Draw2D";

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.Draw2DGame.EntityTypes.Register(registry);

    public override void Setup(World world)
    {
        world.Add(new Backdrop());
        world.Add(new Hero());
    }

    public override void DefineScreens(ScreenSet screens) => screens.Add("main", game => Ui.Column().Padding(Length.Px(8)));

    public override void DefineFonts(OneGame.Text.FontLibrary fonts)
    {
        ArgumentNullException.ThrowIfNull(fonts);
        using var stream = typeof(Draw2DGameDefinition).Assembly.GetManifestResourceStream("fonts/NotoSans-Regular.ttf") ?? throw new InvalidOperationException("The program has no embedded font.");
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        fonts.Add(copy.ToArray());
    }
}
