# Particles

Sparks, smoke, dust, explosions and trails are **emitters**: small objects that make short-lived particles and move them. An emitter is view-only. It reads no simulation state, uses its own random stream and never touches a synced property, so replays, snapshots and drift hashes are the same with or without it. You keep an emitter in a field of the view (an entity's `[View]` method), move it each frame, and hand it to the renderer, which steps and draws it.

## A fountain of sparks in 2D

```csharp
using System.Numerics;
using OneGame.Core;
using OneGame.Host;
using OneGame.Particles;

[Entity]
public partial class Torch : Entity, IView2D
{
    [Synced] public partial Fixed X { get; private set; }
    [Synced] public partial int Hits { get; private set; }

    [Command]
    public void Hit() => Hits++;

    private ParticleEmitter? _sparks;
    private ParticleEmitter? _flash;
    private int _seenHits;

    [View]
    public void Draw(Draw2D d)
    {
        _sparks ??= new ParticleEmitter(new ParticleSettings
        {
            Rate = 90,                                    // particles per second
            Prewarm = 1.5f,                               // already running on the first frame
            Lifetime = new FloatRange(0.6f, 1.0f),        // seconds
            Speed = new FloatRange(120, 200),             // world units per second
            Direction = -Vector3.UnitY,                   // up on screen
            Spread = 0.35f,                               // half-angle in radians
            Gravity = new Vector3(0, 320, 0),             // +Y is down in 2D
            Size = new FloatRange(8, 14),
            SizeOverLife = ParticleCurve.Linear(1f, 0.3f),
            Color = ParticleGradient.Of(
                (0f, ParticleGradient.Rgb(0xfff2b0)),
                (0.4f, ParticleGradient.Rgb(0xff9a30)),
                (1f, ParticleGradient.Rgb(0xd02010, alpha: 0f))),
            Blend = ParticleBlend.Additive,               // glows add up instead of covering
        }, seed: 11, name: "sparks");

        _flash ??= new ParticleEmitter(new ParticleSettings { Rate = 0, Lifetime = 0.4f, Speed = new FloatRange(40, 120), Spread = MathF.PI, Size = 10f }, seed: 12, name: "flash");

        var at = new Vector2(X.ToSingle(), 0);
        if (Hits != _seenHits)                            // the view notices a change and reacts; it never writes state
        {
            _seenHits = Hits;
            _flash.Burst(20, new Vector3(at, 0));
        }

        d.Particles(_sparks, at, z: 2);                   // moves it to `at`, steps it by this frame's time, draws it
        d.Particles(_flash, at, z: 3);
    }
}
```

`d.Particles(emitter, position)` does three things on the first call of a frame: sets `emitter.Position`, steps the emitter by `d.DeltaTime`, and draws every live particle as a sprite on the current layer, so the 2D camera and `z` apply as for any sprite. An emitter you do not pass in a frame neither moves nor draws. Time is the shown tick's view time, so captures and replays repeat exactly and a paused game holds its particles still. A moving emitter leaves a trail: particles stay where they were born, and births between two frames are placed along the path.

Use `Burst(count)` for one-off effects (a hit, a pickup, an explosion), `Rate` for streams, `Stop()` to let a stream run out and `Alive` to know when a one-shot effect is finished. For one-shots set `Loop = false` and `Duration`.

## Settings

Everything is in `ParticleSettings` and is read as the emitter runs, so you can change `Rate`, `Gravity` and the rest from view code (a rate that follows a throttle). `Capacity` is fixed when the emitter is made: births past it are dropped and counted in `Dropped`, never reallocated.

| Group | Settings |
|---|---|
| Birth | `Rate`, `Loop`, `Duration`, `Prewarm`, `SpawnRadius` |
| Life | `Lifetime` |
| Motion | `Speed`, `Direction`, `Spread`, `Gravity`, `Drag`, `LocalSpace` |
| Look | `Size`, `SizeOverLife`, `Color` (gradient), `Rotation`, `AngularVelocity`, `Blend`, `Emission` (3D) |
| Frames | `SheetColumns`, `SheetRows`, `FrameCount`, `AnimateFrames` |

Ranges (`FloatRange`) draw one value per particle; a plain number is a range of one value. Curves and gradients are straight lines between stops over the particle's life (0 to 1); colours are sRGB with straight alpha. `Space = Plane` (the default) is for 2D: spread is an angle in the plane and `SpawnRadius` a disc. `Space = Volume` is for 3D: spread is a cone and the spawn area a ball.

The default image is a soft round spot. Pass a texture or an asset path to draw something else, and describe a sheet with `SheetColumns` and `SheetRows`:

```csharp
using System.Numerics;
using OneGame.Core;
using OneGame.Host;
using OneGame.Particles;

[Entity]
public partial class Fireball : Entity, IView2D
{
    private ParticleEmitter? _blast;

    [View]
    public void Draw(Draw2D d)
    {
        _blast ??= new ParticleEmitter(new ParticleSettings
        {
            Rate = 0,
            Lifetime = 0.8f,
            Speed = 0f,
            Size = 96f,
            SheetColumns = 4,          // art/explosion.png is a 4 x 4 sheet
            SheetRows = 4,
            AnimateFrames = true,      // play the frames once over each particle's life
            Blend = ParticleBlend.Additive,
        });

        d.Particles(_blast, new Vector2(100, 100), "art/explosion.png");
    }
}
```

## In 3D

In a `Render3DStage` game, entities that implement `IView3D` call `scene.Particles(emitter, position)`. The particles are camera-facing quads on the 3D render graph: depth tested against the scene, never writing depth, sorted far to near (additive ones need no sorting). Use `ParticleSpace.Volume`, 3D units (a metre is one unit) and gravity along -Y. `Emission` above 1 makes additive particles brighter than the display range, so bloom picks them up.

```csharp
using System.Numerics;
using OneGame.Core;
using OneGame.Host;
using OneGame.Particles;

[Entity]
public partial class Campfire : Entity, IView3D
{
    [Synced] public partial FVec3 Position { get; private set; }

    private ParticleEmitter? _embers;

    [View]
    public void Describe3D(SceneWriter scene)
    {
        _embers ??= new ParticleEmitter(new ParticleSettings
        {
            Space = ParticleSpace.Volume,
            Rate = 40,
            Prewarm = 2f,
            Lifetime = new FloatRange(1f, 2f),
            Speed = new FloatRange(0.8f, 1.6f),
            Direction = Vector3.UnitY,
            Spread = 0.4f,
            Gravity = new Vector3(0, -0.5f, 0),
            Size = new FloatRange(0.03f, 0.06f),
            Color = ParticleGradient.FadeOut(ParticleGradient.Rgb(0xff8a2a)),
            Emission = 4f,
            Blend = ParticleBlend.Additive,
        }, seed: 3);

        scene.Particles(_embers, new Vector3(Position.X.ToSingle(), Position.Y.ToSingle(), Position.Z.ToSingle()));
    }
}
```

## Dropping down

Everything above sits on public low-level pieces you can use directly.

- **2D:** `ParticleEmitter.Sample(i)` gives each live particle (position, size, rotation, colour, frame, texture rectangle). Draw them yourself as `Sprite`s. Additive drawing is a built-in material: set `Sprite.Material = renderer.AdditiveMaterial`.
- **3D:** a `BillboardBatch3D` is a list of `Billboard3D` quads (position, size, rotation, linear colour, texture rectangle, additive or not). Add it to `Scene3D.Billboards` and refill it each frame. `renderer.CreateBillboardTexture` uploads an image for it; `renderer.SoftSpot` is the default.

```csharp
using System.Numerics;
using OneGame.Particles;
using OneGame.Render3D;

public static class LowLevel
{
    public static void Fill(Scene3D scene, BillboardBatch3D batch, ParticleEmitter emitter)
    {
        if (!scene.Billboards.Contains(batch))
        {
            scene.Billboards.Add(batch);
        }

        batch.Clear();
        for (var i = 0; i < emitter.Count; i++)
        {
            var p = emitter.Sample(i);
            batch.Add(new Billboard3D
            {
                Position = p.Position,
                Size = new Vector2(p.Size),
                Rotation = p.Rotation,
                Color = new Vector4(p.Color.X, p.Color.Y, p.Color.Z, p.Color.W),   // already sRGB here; convert to linear for correct colour
                Uv = p.Uv,
                Additive = true,
            });
        }
    }
}
```

## Look at it, count it

```
onegame capture --out shots/fire.png --size 720p --after 30 --json
```

The control operation `particles` reports the emitters the last drawn frame showed: name, live particles, capacity, whether still emitting, spawned and dropped, blend and position (`{"name":"sparks"}` limits it to one). Scenarios assert on it:

```yaml
- step: 30
- expect: {particles: {emitter: sparks, min: 50, max: 100}}
- expect: {particles: {emitter: flash, count: 0}}
```

Name your emitters (`name:` in the constructor) so the harness can tell them apart. A headless run draws a frame only when something asks for one (a capture, `particles`, `draw_view`), so a headless capture shows the emitter as of its first drawn frame plus the time since: give steady effects a `Prewarm` so they are already full. `capture_sequence` draws a frame every few ticks and shows an effect developing. Seed your emitters (`seed:`) and captures repeat exactly. `tests/ParticleGame` is a worked example in 2D and 3D, with scenarios and capture references.

## Budget

The simulation runs on the CPU: about 5,000 live particles step and sample in a fraction of a millisecond, and the 2D and 3D draw paths allocate nothing in steady frames (tests run 2,000 live particles). Size capacities for the peak; a `dropped` count above zero in the `particles` operation means an emitter is too small. Contracts: `docs/interfaces/particles.md`.
