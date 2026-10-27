using System.Numerics;
using System.Runtime.InteropServices;
using OneGame.Gpu;
using OneGame.UI;

namespace OneGame.Render2D;

/// <summary>One textured quad as the GPU reads it: the corner and two edge vectors in framebuffer pixels, a uv rectangle, a tint and a free word.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct QuadInstance
{
    public const int Size = 48;

    public Vector2 Origin;
    public Vector2 AxisX;
    public Vector2 AxisY;
    public Vector4 Uv;
    public uint Color;
    public uint Extra;
}

/// <summary>One UI primitive as the GPU reads it. See Ui.hlsl for what each field means per kind.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct UiInstance
{
    public const int Size = 128;

    public Vector4 Rect;
    public Vector4 M;
    public Vector2 T;
    public Vector4 Radii;
    public Vector4 Widths;
    public Vector4 Uv;
    public uint C0;
    public uint C1;
    public Vector4 Grad;
    public uint Kind;
    public uint GradKind;
    public uint ClipId;
    public float Pad;
}

/// <summary>Colour helpers. Colours are sRGB bytes with straight alpha everywhere in the public API.</summary>
internal static class Colors
{
    /// <summary>Packs a colour as R, G, B, A in increasing byte order, which is how a unorm8x4 vertex attribute reads it.</summary>
    public static uint Pack(Color c) => c.R | ((uint)c.G << 8) | ((uint)c.B << 16) | ((uint)c.A << 24);

    public static uint Pack(Color c, float opacity) =>
        Pack(new Color(c.R, c.G, c.B, (byte)Math.Clamp((int)MathF.Round(c.A * opacity), 0, 255)));

    private static readonly float[] Decode = BuildDecode();
    private static readonly byte[] Encode = BuildEncode();

    private static float[] BuildDecode()
    {
        var t = new float[256];
        for (var i = 0; i < 256; i++)
        {
            var c = i / 255f;
            t[i] = c <= 0.04045f ? c / 12.92f : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
        }

        return t;
    }

    private static byte[] BuildEncode()
    {
        // 4096 steps of linear light are enough to land on the right byte at the dark end, where the curve is steepest.
        var t = new byte[4096];
        for (var i = 0; i < t.Length; i++)
        {
            var l = i / 4095f;
            var c = l <= 0.0031308f ? l * 12.92f : (1.055f * MathF.Pow(l, 1f / 2.4f)) - 0.055f;
            t[i] = (byte)Math.Clamp((int)MathF.Round(c * 255f), 0, 255);
        }

        return t;
    }

    /// <summary>
    /// Converts straight-alpha sRGB pixels to the form the renderer stores: the colour is decoded to linear light, multiplied by alpha
    /// and encoded again, so the hardware's sRGB decode gives premultiplied linear values and filtering blends in linear light.
    /// </summary>
    public static void PremultiplySrgb(ReadOnlySpan<byte> straight, Span<byte> destination)
    {
        for (var i = 0; i + 3 < straight.Length; i += 4)
        {
            var a = straight[i + 3] / 255f;
            for (var c = 0; c < 3; c++)
            {
                var lin = Decode[straight[i + c]] * a;
                destination[i + c] = Encode[(int)(lin * 4095f + 0.5f)];
            }

            destination[i + 3] = straight[i + 3];
        }
    }

    /// <summary>Linear value of an sRGB-encoded byte.</summary>
    public static float ToLinear(byte b) => Decode[b];

    /// <summary>sRGB byte of a linear value in 0..1.</summary>
    public static byte ToSrgb(float linear) => Encode[(int)(Math.Clamp(linear, 0f, 1f) * 4095f + 0.5f)];
}

/// <summary>Whole pixels of a texture or sub-rectangle.</summary>
/// <param name="X">Left edge in pixels.</param>
/// <param name="Y">Top edge in pixels.</param>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
public readonly record struct PixelRect(int X, int Y, int Width, int Height);

/// <summary>How a texture is filtered when it is drawn at a size other than its own.</summary>
public enum TextureFilter
{
    /// <summary>Nearest texel: crisp pixel art.</summary>
    Nearest,

    /// <summary>Bilinear.</summary>
    Linear,
}

/// <summary>An image on the GPU that sprites, tilesets and materials draw from. Created by <see cref="Renderer2D.CreateTexture(int, int, ReadOnlySpan{byte}, TextureFilter)"/>.</summary>
public sealed class Texture2D : IDisposable
{
    internal Texture2D(GpuTexture texture, GpuSampler sampler, GpuFormat format)
    {
        Texture = texture;
        Sampler = sampler;
        Width = (int)texture.Width;
        Height = (int)texture.Height;
        Format = format;
    }

    /// <summary>Width in pixels.</summary>
    public int Width { get; private set; }

    /// <summary>Height in pixels.</summary>
    public int Height { get; private set; }

    internal GpuFormat Format { get; }

    internal GpuTexture Texture { get; private set; }

    internal GpuSampler Sampler { get; private set; }

    /// <summary>Swaps in a new GPU image (possibly of another size) and returns the old ones for the caller to dispose once the device is idle.</summary>
    internal (GpuTexture Texture, GpuSampler Sampler, GpuBindGroup? Group) Swap(GpuTexture texture, GpuSampler sampler)
    {
        var old = (Texture, Sampler, Group);
        Texture = texture;
        Sampler = sampler;
        Group = null;
        Width = (int)texture.Width;
        Height = (int)texture.Height;
        return old;
    }

    /// <summary>The set 1 bind group, made the first time the texture is drawn.</summary>
    internal GpuBindGroup? Group { get; set; }

    /// <summary>Releases the GPU memory. Do not draw the texture afterwards.</summary>
    public void Dispose()
    {
        Group?.Dispose();
        Group = null;
        Texture.Dispose();
        Sampler.Dispose();
    }
}
