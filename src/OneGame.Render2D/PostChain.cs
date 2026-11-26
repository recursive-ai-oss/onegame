using System.Numerics;
using OneGame.UI;

namespace OneGame.Render2D;

/// <summary>
/// A list of full-screen effects run, in order, over a finished image: the whole scene (<see cref="Renderer2D.DrawScene"/>) or one layer
/// (<see cref="Layer2D.Post"/>). Make one with <see cref="Renderer2D.CreatePostChain"/>, add effects, and hand it over. A chain with no effects costs
/// nothing; each effect costs one full-screen pass (blur is two, bloom is several at reduced size). A chain keeps GPU objects, so make it once and keep it.
/// </summary>
public sealed class PostChain : IDisposable
{
    internal enum Kind
    {
        Single,
        Bloom,
    }

    internal sealed class Pass(Kind kind, Material material, Vector2 direction, float radius, bool owned)
    {
        public Kind Kind { get; } = kind;

        public Material Material { get; } = material;

        public bool Owned { get; } = owned;

        public bool IsBlur => direction != Vector2.Zero;

        // Bloom: the extra materials and shape.
        public Material? BlurH { get; set; }

        public Material? BlurV { get; set; }

        public int BlurPasses { get; set; } = 1;

        public int Downscale { get; set; } = 2;

        public float Radius => radius;

        /// <summary>Blur taps are spaced in texture units, so they depend on the target size; set them when the size is known.</summary>
        public void Prepare(int width, int height)
        {
            if (IsBlur)
            {
                Material.SetParameter(0, new Vector4(direction.X / width * radius, direction.Y / height * radius, 0, 0));
            }
        }

        /// <summary>Sets a bloom blur material's taps for the size of the image it reads.</summary>
        public static void PrepareBlur(Material m, Vector2 direction, float radius, int width, int height) =>
            m.SetParameter(0, new Vector4(direction.X / width * radius, direction.Y / height * radius, 0, 0));
    }

    private readonly Renderer2D _renderer;
    private readonly List<Pass> _passes = [];
    private Material? _fluidSurface;

    internal PostChain(Renderer2D renderer) => _renderer = renderer;

    /// <summary>The number of effects added (a blur or a bloom counts as one).</summary>
    public int PassCount => _passes.Count;

    internal List<Pass> Passes => _passes;


    /// <summary>Adds a separable Gaussian-like blur (a horizontal and a vertical pass).</summary>
    /// <param name="radius">Spacing of the blur's taps in pixels; the visible softening reaches about three times this far.</param>
    /// <returns>This chain.</returns>
    public PostChain AddBlur(float radius)
    {
        _passes.Add(new Pass(Kind.Single, _renderer.CreateBlurMaterial(), Vector2.UnitX, radius, owned: true));
        _passes.Add(new Pass(Kind.Single, _renderer.CreateBlurMaterial(), Vector2.UnitY, radius, owned: true));
        return this;
    }

    /// <summary>
    /// Adds bloom: the parts of the image brighter than a threshold are blurred and added back, so bright things glow. It works in linear light
    /// on an image at reduced size, and adds the glow to what is there instead of replacing it.
    /// </summary>
    /// <param name="threshold">Brightness (linear light, 0 to 1) above which a pixel contributes; the brightest colour channel decides.</param>
    /// <param name="intensity">How strongly the glow is added; 1 adds it as found. Above about 1 the glow clips in an 8-bit renderer.</param>
    /// <param name="radius">Spacing of the blur's taps, in pixels of the reduced image; the glow reaches several times this far per blur pass.</param>
    /// <param name="softness">Width of the threshold's ramp in linear brightness; larger values include dimmer pixels gently.</param>
    /// <param name="blurPasses">How many times the image is blurred (each is a horizontal and a vertical pass); more is wider and smoother.</param>
    /// <param name="downscale">The factor the glow image is smaller than the source by (1 = full size, 2 = half, 4 = quarter).</param>
    /// <returns>This chain.</returns>
    public PostChain AddBloom(float threshold = 0.6f, float intensity = 1f, float radius = 1f, float softness = 0.3f, int blurPasses = 3, int downscale = 2)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(blurPasses, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(blurPasses, 8);
        ArgumentOutOfRangeException.ThrowIfLessThan(downscale, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(downscale, 16);
        var bright = _renderer.CreateBrightMaterial();
        bright.SetParameter(0, new Vector4(threshold, MathF.Max(softness, 0.0001f), MathF.Max(intensity, 0f), 0));
        _passes.Add(new Pass(Kind.Bloom, bright, Vector2.Zero, radius, owned: true)
        {
            BlurH = _renderer.CreateBlurMaterial(),
            BlurV = _renderer.CreateBlurMaterial(),
            BlurPasses = blurPasses,
            Downscale = downscale,
        });
        return this;
    }

    /// <summary>
    /// Adds a pass of your own: a pixel shader (HLSL compiled with the game's shaders, made into a <see cref="Material"/> with
    /// <see cref="Renderer2D.CreateMaterial"/>) that runs over the whole image. It reads the image so far as <c>Tex</c>/<c>Samp</c> (set 1), its
    /// parameters as <c>P[0..7]</c> (set 2) and the size of the image being drawn as <c>Viewport</c> (set 0), and returns premultiplied colour.
    /// Change its parameters with <see cref="Material.SetParameter"/> any time; they apply to the next frame drawn. The chain does not own the
    /// material: dispose it yourself after the chain.
    /// </summary>
    /// <param name="material">A material whose shader reads the previous image.</param>
    /// <returns>This chain.</returns>
    public PostChain AddMaterial(Material material)
    {
        ArgumentNullException.ThrowIfNull(material);
        _passes.Add(new Pass(Kind.Single, material, Vector2.Zero, 0, owned: false));
        return this;
    }

    /// <summary>
    /// Adds a liquid surface: the image's alpha is read as a field (particles drawn as soft discs, then blurred) and cut at a level, so
    /// separate discs merge into one smooth body with a lighter or darker rim. Put it after <see cref="AddBlur"/>; <c>OneGame.Host.FluidSurface2D</c> sets up the whole
    /// chain. The pass replaces the image's colour with <paramref name="fill"/> and <paramref name="rim"/>.
    /// </summary>
    /// <param name="threshold">The field level where the surface sits, 0 to 1. Lower grows the body and bridges larger gaps; higher shrinks it.</param>
    /// <param name="softness">Half width of the outline's blend, in field units (about 0.04 keeps it smooth without blurring).</param>
    /// <param name="fill">The body colour (sRGB, straight alpha).</param>
    /// <param name="rim">The colour of the band just inside the outline (sRGB, straight alpha).</param>
    /// <param name="rimWidth">How far inside the outline the rim reaches, in field units.</param>
    /// <param name="rimStrength">0 for no rim, 1 for the full rim colour at the outline.</param>
    /// <returns>This chain.</returns>
    public PostChain AddFluidSurface(float threshold, float softness, Color fill, Color rim, float rimWidth = 0.2f, float rimStrength = 1f)
    {
        var m = _renderer.CreateFluidMaterial();
        _passes.Add(new Pass(Kind.Single, m, Vector2.Zero, 0, owned: true));
        _fluidSurface = m;
        return UpdateFluidSurface(threshold, softness, fill, rim, rimWidth, rimStrength);
    }

    /// <summary>Changes the settings of the liquid surface pass added with <see cref="AddFluidSurface"/>; they apply to the next frame drawn. Allocates nothing.</summary>
    /// <param name="threshold">The field level where the surface sits, 0 to 1.</param>
    /// <param name="softness">Half width of the outline's blend, in field units.</param>
    /// <param name="fill">The body colour (sRGB, straight alpha).</param>
    /// <param name="rim">The colour of the band just inside the outline (sRGB, straight alpha).</param>
    /// <param name="rimWidth">How far inside the outline the rim reaches, in field units.</param>
    /// <param name="rimStrength">0 for no rim, 1 for the full rim colour at the outline.</param>
    /// <returns>This chain.</returns>
    /// <exception cref="InvalidOperationException">The chain has no liquid surface pass.</exception>
    public PostChain UpdateFluidSurface(float threshold, float softness, Color fill, Color rim, float rimWidth = 0.2f, float rimStrength = 1f)
    {
        var m = _fluidSurface ?? throw new InvalidOperationException("The chain has no liquid surface pass; add one with AddFluidSurface.");
        m.SetParameter(0, new Vector4(Math.Clamp(threshold, 0.01f, 0.99f), Math.Max(softness, 0.0001f), Math.Max(rimWidth, 0.0001f), Math.Clamp(rimStrength, 0f, 1f)));
        m.SetParameter(1, Linear(fill));
        m.SetParameter(2, Linear(rim));
        return this;
    }

    private static Vector4 Linear(Color c) => new(Colors.ToLinear(c.R), Colors.ToLinear(c.G), Colors.ToLinear(c.B), c.A / 255f);

    /// <summary>Adds a colour grade.</summary>
    /// <param name="exposure">Brightness change in stops (0 leaves it, +1 doubles the light).</param>
    /// <param name="contrast">1 leaves contrast alone, above 1 increases it.</param>
    /// <param name="saturation">1 leaves colour, 0 is grey, above 1 is more vivid.</param>
    /// <param name="vignette">0 for none; 1 darkens the corners fully.</param>
    /// <param name="tint">A colour to multiply in, with <paramref name="tintAmount"/>; sRGB.</param>
    /// <param name="tintAmount">0 for no tint, 1 for the full tint.</param>
    /// <returns>This chain.</returns>
    public PostChain AddGrade(float exposure = 0, float contrast = 1, float saturation = 1, float vignette = 0, Color? tint = null, float tintAmount = 0)
    {
        var m = _renderer.CreateGradeMaterial();
        m.SetParameter(0, new Vector4(exposure, contrast, saturation, vignette));
        var t = tint ?? Color.White;
        m.SetParameter(1, new Vector4(t.R / 255f, t.G / 255f, t.B / 255f, tintAmount));
        _passes.Add(new Pass(Kind.Single, m, Vector2.Zero, 0, owned: true));
        return this;
    }

    /// <summary>Releases the effects' GPU objects (not the materials passed to <see cref="AddMaterial"/>).</summary>
    public void Dispose()
    {
        foreach (var p in _passes)
        {
            if (p.Owned)
            {
                p.Material.Dispose();
            }

            p.BlurH?.Dispose();
            p.BlurV?.Dispose();
        }

        _passes.Clear();
    }
}
