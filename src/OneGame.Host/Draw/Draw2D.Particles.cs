using System.Numerics;
using OneGame.Particles;
using OneGame.Render2D;
using OneGame.UI;

namespace OneGame.Host;

public sealed partial class Draw2D
{
    /// <summary>True once a frame has been drawn through this object.</summary>
    public bool HasDrawn => _lastTick != long.MinValue;

    /// <summary>The emitters handed to <see cref="Particles(ParticleEmitter, Vector2, Texture2D?, float)"/> in the last drawn frame, in call order. The harness reads it.</summary>
    public IReadOnlyList<ParticleEmitter> ParticleEmitters => _emitters;

    /// <summary>
    /// Shows a particle emitter at a point. Call it every frame from view code, with the emitter you keep in a field: the first call of a frame
    /// moves it to <paramref name="position"/>, steps it by the frame's <see cref="DeltaTime"/> and draws every live particle as a sprite on the
    /// current <see cref="Layer"/>, so the camera and <paramref name="z"/> apply as for any sprite. Additive emitters use <see cref="Renderer2D.AdditiveMaterial"/>.
    /// Time comes from the shown tick, so captures and replays repeat exactly, and a paused game holds its particles still. An emitter that is not passed
    /// in a frame neither moves nor draws. Steady frames allocate nothing.
    /// </summary>
    /// <param name="emitter">The emitter. Its settings should use <see cref="ParticleSpace.Plane"/>.</param>
    /// <param name="position">Where it is, in world units.</param>
    /// <param name="texture">The image, or the sheet its settings describe; null draws a soft round spot.</param>
    /// <param name="z">Draw order inside the layer: lower is behind.</param>
    public void Particles(ParticleEmitter emitter, Vector2 position, Texture2D? texture = null, float z = 0)
    {
        ArgumentNullException.ThrowIfNull(emitter);
        var tex = texture ?? _soft ?? throw new InvalidOperationException("Particles can only be drawn from a view that is being drawn.");
        emitter.Position = new Vector3(position, 0f);
        var seen = false;
        for (var i = 0; i < _emitters.Count; i++)
        {
            if (ReferenceEquals(_emitters[i], emitter))
            {
                seen = true;
                break;
            }
        }

        if (!seen)
        {
            _emitters.Add(emitter);
            emitter.Step(DeltaTime);
        }

        var s = emitter.Settings;
        var cols = Math.Max(s.SheetColumns, 1);
        var rows = Math.Max(s.SheetRows, 1);
        var cw = Math.Max(tex.Width / cols, 1);
        var ch = Math.Max(tex.Height / rows, 1);
        var material = s.Blend == ParticleBlend.Additive ? Renderer!.AdditiveMaterial : null;
        for (var i = 0; i < emitter.Count; i++)
        {
            var p = emitter.Sample(i);
            var c = p.Color;
            var col = p.Frame % cols;
            var row = p.Frame / cols;
            _scene.Draw(Layer, new Sprite
            {
                Texture = tex,
                Source = cols * rows > 1 ? new PixelRect(col * cw, row * ch, cw, ch) : default,
                Position = new Vector2(p.Position.X, p.Position.Y),
                Pivot = new Vector2(0.5f, 0.5f),
                Scale = new Vector2(p.Size / cw, p.Size / ch),
                Rotation = p.Rotation,
                Tint = new Color(Byte(c.X), Byte(c.Y), Byte(c.Z), Byte(c.W)),
                Z = z,
                Material = material,
            });
            SpriteCount++;
        }
    }

    /// <summary>Shows a particle emitter at a point, with an image loaded from the game's content folders; see <see cref="Particles(ParticleEmitter, Vector2, Texture2D?, float)"/>.</summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="position">Where it is, in world units.</param>
    /// <param name="texturePath">Path under a content folder, like <c>art/spark.png</c>.</param>
    /// <param name="z">Draw order inside the layer: lower is behind.</param>
    public void Particles(ParticleEmitter emitter, Vector2 position, string texturePath, float z = 0) =>
        Particles(emitter, position, Texture(texturePath), z);

    private static byte Byte(float v) => (byte)Math.Clamp((int)MathF.Round(v * 255f), 0, 255);

    // A round spot that fades smoothly to nothing at its edge: the default particle image.
    private static byte[] MakeSoft()
    {
        var px = new byte[SoftSize * SoftSize * 4];
        for (var y = 0; y < SoftSize; y++)
        {
            for (var x = 0; x < SoftSize; x++)
            {
                var dx = ((x + 0.5f) / SoftSize * 2f) - 1f;
                var dy = ((y + 0.5f) / SoftSize * 2f) - 1f;
                var a = Math.Clamp(1f - MathF.Sqrt((dx * dx) + (dy * dy)), 0f, 1f);
                a = a * a * (3f - (2f * a));
                var i = ((y * SoftSize) + x) * 4;
                px[i] = px[i + 1] = px[i + 2] = 255;
                px[i + 3] = (byte)MathF.Round(a * 255f);
            }
        }

        return px;
    }
}
