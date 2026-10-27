using System.Numerics;
using OneGame.Core;
using OneGame.Host;
using OneGame.Render2D;
using OneGame.UI;

namespace SpriteExample;

/// <summary>A ship that patrols a line. The simulation moves it; the view only draws it.</summary>
[Entity]
public partial class Patrol : Entity, IView2D
{
    [Synced] public partial Fixed X { get; private set; }
    [Synced] public partial bool Back { get; private set; }

    protected override void OnTick()
    {
        X += Fixed.FromInt(Back ? -2 : 2);
        if (X.ToInt() >= 120 || X.ToInt() <= -120)
        {
            Back = !Back;
        }
    }

    // Draw runs every frame and reads state only. Assigning X here would not compile.
    [View]
    public void Draw(Draw2D d)
    {
        d.FillRect(new Rect(-140, 24, 280, 4), Color.Rgb(0x35506b));
        d.Sprite("art/ship.png", new Vector2(X.ToSingle(), 0), scale: 2, flip: Back ? SpriteFlip.Horizontal : SpriteFlip.None);
        d.Text("patrol", new Vector2(0, -40), 14, Color.White, new Vector2(0.5f, 1));
    }
}

public sealed class SpriteGame : GameDefinition
{
    public override string Name => "Sprite";

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.SpriteExample.EntityTypes.Register(registry);

    public override void Setup(World world) => world.Add(new Patrol());

    public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => Ui.Column());
}
