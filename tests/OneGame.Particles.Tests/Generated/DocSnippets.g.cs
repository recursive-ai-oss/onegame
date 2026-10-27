// Generated from docs/particles.md by DocSnippetTests. Do not edit.
#pragma warning disable
namespace ParticleDocs.Block1
{
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
}

namespace ParticleDocs.Block2
{
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
}

namespace ParticleDocs.Block3
{
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
}

namespace ParticleDocs.Block4
{
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
}

