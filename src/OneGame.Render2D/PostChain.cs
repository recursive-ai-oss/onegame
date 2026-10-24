using System.Numerics;
using OneGame.UI;

namespace OneGame.Render2D;

/// <summary>
/// A list of full-screen effects run, in order, over a finished scene. Make one with <see cref="Renderer2D.CreatePostChain"/>, add effects, and pass it to
/// <see cref="Renderer2D.DrawScene"/>. The effects are ordinary materials with built-in shaders, so a chain costs one full-screen pass per effect.
/// </summary>
public sealed class PostChain : IDisposable
{
    internal sealed class Pass(Material material, Vector2 direction, float radius)
    {
        public Material Material { get; } = material;

        public bool IsBlur => direction != Vector2.Zero;

        /// <summary>Blur taps are spaced in texture units, so they depend on the target size; set them when the size is known.</summary>
        public void Prepare(int width, int height)
        {
            if (IsBlur)
            {
                Material.SetParameter(0, new Vector4(direction.X / width * radius, direction.Y / height * radius, 0, 0));
            }
        }
    }

    private readonly Renderer2D _renderer;
    private readonly List<Pass> _passes = [];

    internal PostChain(Renderer2D renderer) => _renderer = renderer;

    /// <summary>The number of full-screen passes.</summary>
    public int PassCount => _passes.Count;

    internal List<Pass> Passes => _passes;

    /// <summary>Adds a separable Gaussian-like blur (a horizontal and a vertical pass).</summary>
    /// <param name="radius">Spacing of the blur's taps in pixels; the visible softening reaches about three times this far.</param>
    /// <returns>This chain.</returns>
    public PostChain AddBlur(float radius)
    {
        _passes.Add(new Pass(_renderer.CreateBlurMaterial(), Vector2.UnitX, radius));
        _passes.Add(new Pass(_renderer.CreateBlurMaterial(), Vector2.UnitY, radius));
        return this;
    }

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
        _passes.Add(new Pass(m, Vector2.Zero, 0));
        return this;
    }

    /// <summary>Releases the effects' GPU objects.</summary>
    public void Dispose()
    {
        foreach (var p in _passes)
        {
            p.Material.Dispose();
        }

        _passes.Clear();
    }
}
