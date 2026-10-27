using System;
using System.Numerics;
using OneGame.Core;
using OneGame.Host;
using OneGame.Particles;
using OneGame.Platform;
using OneGame.UI;

namespace ParticlesExample;

/// <summary>A torch that throws sparks all the time and a burst when it is hit. Particles are view-only: the emitters live in the
/// view, read state, never write it, and leave the state hash alone.</summary>
[Entity]
public partial class Torch : Entity, IView2D
{
    [Synced] public partial int Hits { get; private set; }

    [Command] public void Hit() => Hits++;

    private ParticleEmitter? _sparks;
    private ParticleEmitter? _flash;
    private int _seen;

    [View]
    public void Draw(Draw2D d)
    {
        _sparks ??= new ParticleEmitter(new ParticleSettings
        {
            Rate = 90,                                   // particles per second
            Prewarm = 1.5f,                              // already running on the first frame
            Lifetime = new FloatRange(0.6f, 1.0f),
            Speed = new FloatRange(120, 200),
            Direction = -Vector3.UnitY,                  // up on screen
            Spread = 0.35f,
            Gravity = new Vector3(0, 320, 0),            // +Y is down in 2D
            Size = new FloatRange(8, 14),
            Color = ParticleGradient.Of((0f, ParticleGradient.Rgb(0xfff2b0)), (1f, ParticleGradient.Rgb(0xd02010, alpha: 0f))),
            Blend = ParticleBlend.Additive,
        }, seed: 11, name: "sparks");
        _flash ??= new ParticleEmitter(new ParticleSettings { Rate = 0, Lifetime = 0.4f, Speed = new FloatRange(40, 120), Spread = MathF.PI, Size = 10f }, seed: 12, name: "flash");

        if (Hits != _seen)
        {
            _seen = Hits;
            _flash.Burst(20, Vector3.Zero);              // the view notices a change and reacts
        }

        d.Particles(_sparks, Vector2.Zero, z: 2);        // moves the emitter, steps it by this frame's time, draws it
        d.Particles(_flash, Vector2.Zero, z: 3);
    }
}

public sealed class ParticlesGame : GameDefinition
{
    public override string Name => "Particles";

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.ParticlesExample.EntityTypes.Register(registry);

    public override void Setup(World world) => world.Add(new Torch());

    public override void DefineActions(ActionMap actions) => actions.Context("play", c => c.Button("hit", Bind.Key(Key.Space)));

    public override void OnEachFrame(GameInstance game)
    {
        if (game.Actions.Pressed("hit"))
        {
            ((Torch)game.World.Entities[0]).Send.Hit();
        }
    }

    public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => Ui.Column());
}
