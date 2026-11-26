using System.Numerics;
using OneGame.Render2D;
using OneGame.UI;

namespace OneGame.Host;

/// <summary>How a <see cref="FluidSurface2D"/> looks. All lengths are in the units of the layer the fluid is drawn on (the units you pass to <see cref="FluidSurface2D.Draw"/>).</summary>
public sealed class FluidSurfaceStyle
{
    /// <summary>The body colour (sRGB, straight alpha). Default a mid blue.</summary>
    public Color Fill { get; set; } = Color.Rgb(0x2f86d6);

    /// <summary>The colour of the band just inside the outline, which reads as a lit edge or foam. Default a light blue.</summary>
    public Color Rim { get; set; } = Color.Rgb(0x9fd8ff);

    /// <summary>The radius of each particle's soft splat, where its falloff reaches zero. About 1.2 to 1.8 times the particle spacing fills the body without lumps; smaller lets a thin stream break into drops. Default 0.2.</summary>
    public float ParticleRadius { get; set; } = 0.2f;

    /// <summary>How far the blur softens the splats, in layer units (the visible softening reaches about twice this). Roughly the particle spacing; larger merges more and rounds off detail. Default 0.1.</summary>
    public float Blur { get; set; } = 0.1f;

    /// <summary>The field level (0 to 1) where the surface sits. Lower swells the body and bridges gaps between particles; higher shrinks it and lets thin streams break up. Default 0.45.</summary>
    public float Threshold { get; set; } = 0.45f;

    /// <summary>How smoothly the outline blends, in field units. Default 0.05.</summary>
    public float Softness { get; set; } = 0.05f;

    /// <summary>How far inside the outline the rim reaches, in field units. Default 0.25.</summary>
    public float RimWidth { get; set; } = 0.25f;

    /// <summary>How much of the rim colour shows at the outline, 0 to 1 (0 turns the rim off). Default 1.</summary>
    public float RimStrength { get; set; } = 1f;
}

/// <summary>
/// Draws a particle fluid as one smooth body instead of separate dots. Each frame the particles are splatted as soft round blobs onto a layer of their own,
/// the layer is blurred, and a threshold turns the blurred field into a filled shape with a rim: neighbouring particles merge, a stream
/// stays one piece, and a pool is a flat sheet. It is view code, fed from a copy of the particle positions
/// (<c>PhysicsSpace2D.GetFluidPositions</c>), and allocates nothing per frame once it has been made.
/// <para>
/// Make one and keep it; call <see cref="Draw"/> every frame from a view. The surface lives on a named layer, which draws in the order layers were
/// made, so call <see cref="Draw"/> at the point of the frame where the liquid should sit between the world behind it and what is in front.
/// The cost is three full-layer passes (blur in two, threshold in one) plus one draw per particle, so on small screens it is cheap and
/// on large ones <see cref="FluidSurfaceStyle.Blur"/> and a smaller layer <see cref="Layer2D.Viewport"/> are the levers.
/// </para>
/// </summary>
public sealed class FluidSurface2D : IDisposable
{
    private const int SplatSize = 64;
    private readonly string _layerName;
    private PostChain? _chain;
    private Renderer2D? _renderer;
    private Texture2D? _splat;
    private float _builtScale;
    private float _builtBlur;
    private (float, float, float, float, float, float, Color, Color) _applied;
    private bool _disposed;

    /// <summary>Creates a surface that draws on the layer with this name.</summary>
    /// <param name="layerName">The layer's name. A layer of this name is made the first time <see cref="Draw"/> runs.</param>
    /// <param name="style">How it looks; null for the defaults. The style can be changed later through <see cref="Style"/>.</param>
    public FluidSurface2D(string layerName = "fluid", FluidSurfaceStyle? style = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(layerName);
        _layerName = layerName;
        Style = style ?? new FluidSurfaceStyle();
    }

    /// <summary>The look; change a value any time, it applies to the next frame drawn.</summary>
    public FluidSurfaceStyle Style { get; }

    /// <summary>The layer the surface draws on, once <see cref="Draw"/> has run; set its <see cref="Layer2D.Camera"/> or <see cref="Layer2D.Viewport"/> if the liquid needs its own view.</summary>
    public Layer2D? Layer { get; private set; }

    /// <summary>Draws the particles as one surface.</summary>
    /// <param name="d">The frame's drawing surface, from a view.</param>
    /// <param name="points">The particle positions (for example from <c>PhysicsSpace2D.GetFluidPositions</c>), in sim units with y up when <paramref name="flipY"/> is true.</param>
    /// <param name="unit">Layer units per sim unit (pixels per metre when the layer is in pixels). Default 1.</param>
    /// <param name="flipY">True to draw the sim's y-up positions on a y-down layer.</param>
    /// <param name="z">Draw order of the splats inside the surface's layer.</param>
    /// <exception cref="InvalidOperationException">Called when no frame is being rendered.</exception>
    public void Draw(Draw2D d, ReadOnlySpan<Vector2> points, float unit = 1f, bool flipY = true, float z = 0)
    {
        ArgumentNullException.ThrowIfNull(d);
        ObjectDisposedException.ThrowIf(_disposed, this);
        var renderer = d.Renderer ?? throw new InvalidOperationException("A fluid surface can only be drawn from a view that is being drawn.");
        if (!ReferenceEquals(renderer, _renderer))
        {
            // A new renderer (a new device or window) frees what the old one held; start again on the new one.
            _renderer = renderer;
            _chain = null;
            _splat = renderer.CreateTexture(SplatSize, SplatSize, MakeSplat(), TextureFilter.Linear);
        }

        var layer = d.GetLayer(_layerName);
        Layer = layer;

        // The blur is measured in screen pixels, so it follows how large a layer unit is on screen; the chain is rebuilt only when that changes.
        var origin = d.WorldToScreen(layer, Vector2.Zero);
        var scale = Vector2.Distance(origin, d.WorldToScreen(layer, new Vector2(1, 0)));
        var blur = Math.Max(Style.Blur, 0.0001f);
        if (_chain is null || MathF.Abs(scale - _builtScale) > _builtScale * 0.01f || blur != _builtBlur)
        {
            Rebuild(renderer, scale, blur);
        }

        ApplyStyle();
        layer.Post = _chain;

        var before = d.Layer;
        d.Layer = layer;
        var radius = Math.Max(Style.ParticleRadius, 0.0001f);
        var white = Color.White;
        var sy = flipY ? -unit : unit;
        var splat = _splat!;
        var size = radius * 2f / SplatSize;
        for (var i = 0; i < points.Length; i++)
        {
            d.Sprite(splat, new Vector2(points[i].X * unit, points[i].Y * sy), 0, size, white, null, SpriteFlip.None, z);
        }

        d.Layer = before;
    }

    // A round blob with a smooth falloff (1 - t^2)^2, t = distance / radius, which is zero with zero slope at the edge. A hard-edged disc
    // under a blur whose taps are wider than the disc leaves repeated copies of its edge (a comb), which reads as blocks; a smooth blob does not.
    private static byte[] MakeSplat()
    {
        var px = new byte[SplatSize * SplatSize * 4];
        const float r = SplatSize / 2f;
        for (var y = 0; y < SplatSize; y++)
        {
            for (var x = 0; x < SplatSize; x++)
            {
                var dx = (x + 0.5f - r) / r;
                var dy = (y + 0.5f - r) / r;
                var t = Math.Min((dx * dx) + (dy * dy), 1f);
                var a = (1f - t) * (1f - t);
                var i = ((y * SplatSize) + x) * 4;
                px[i] = px[i + 1] = px[i + 2] = 255;
                px[i + 3] = (byte)((a * 255f) + 0.5f);
            }
        }

        return px;
    }

    private void Rebuild(Renderer2D renderer, float scale, float blur)
    {
        _chain?.Dispose();
        _builtScale = scale;
        _builtBlur = blur;
        var s = Style;
        var chain = renderer.CreatePostChain();
        // The taps sit closer than the blur width so the sparse nine-tap kernel stays smooth over the splats.
        chain.AddBlur(Math.Max(blur * scale * 0.5f, 0.5f));
        chain.AddFluidSurface(s.Threshold, s.Softness, s.Fill, s.Rim, s.RimWidth, s.RimStrength);
        _chain = chain;
        _applied = default;
    }

    // The chain's last pass is the surface material; its parameters are written again only when the style changed.
    private void ApplyStyle()
    {
        var s = Style;
        var now = (s.Threshold, s.Softness, s.RimWidth, s.RimStrength, 0f, 0f, s.Fill, s.Rim);
        if (now.Equals(_applied))
        {
            return;
        }

        _chain!.UpdateFluidSurface(s.Threshold, s.Softness, s.Fill, s.Rim, s.RimWidth, s.RimStrength);
        _applied = now;
    }

    /// <summary>Releases the GPU objects of the surface's post chain. The layer stays in the scene and draws unprocessed if drawn to again.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (Layer is { } layer && ReferenceEquals(layer.Post, _chain))
        {
            layer.Post = null;
        }

        _chain?.Dispose();
        _chain = null;
        _splat?.Dispose();
        _splat = null;
    }
}
