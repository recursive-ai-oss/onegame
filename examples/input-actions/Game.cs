using System;
using System.Numerics;
using OneGame.Core;
using OneGame.Host;
using OneGame.Platform;
using OneGame.UI;

namespace InputActionsExample;

/// <summary>A turret that slides along a rail and fires. Only commands change it, so a replay or a server sees the same game.</summary>
[Entity]
public partial class Turret : Entity, IView2D
{
    [Synced] public partial Fixed X { get; private set; }
    [Synced] public partial int Shots { get; private set; }
    [Synced] public partial int Dx { get; private set; }

    [Command] public void Slide(int dx) => Dx = dx;
    [Command] public void Fire() => Shots++;

    protected override void OnTick() => X += Fixed.FromInt(3) * Dx;

    [View]
    public void Draw(Draw2D d)
    {
        d.FillRect(new Rect(X.ToSingle() - 16, 0, 32, 16), Color.Rgb(0xf2c14e));
        d.ScreenText($"shots: {Shots}", new Vector2(8, 8), 16, Color.White);
    }
}

public sealed class InputActionsGame : GameDefinition
{
    public override string Name => "Input actions";

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.InputActionsExample.EntityTypes.Register(registry);

    public override void Setup(World world) => world.Add(new Turret());

    // One declaration: the keyboard, a gamepad, Steam Input and the scenario step `action:` all answer it, and players can rebind it.
    public override void DefineActions(ActionMap actions) =>
        actions.Context("play", c => c
            .Vector("move", Bind.Arrows, Bind.Wasd, Bind.LeftStick)
            .Button("fire", Bind.Key(Key.Space), Bind.Pad(GamepadButton.South)));

    // Views read actions and send commands; they never write state.
    public override void OnEachFrame(GameInstance game)
    {
        var turret = (Turret)game.World.Entities[0];
        var dx = (int)MathF.Round(game.Actions.Vector("move").X);
        if (dx != turret.Dx)
        {
            turret.Send.Slide(dx);
        }

        if (game.Actions.Pressed("fire"))
        {
            turret.Send.Fire();
        }
    }

    public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => Ui.Column());
}
