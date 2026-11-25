using System.Numerics;
using System.Runtime.InteropServices;
using RecursiveAI.OneGpu;

namespace OneGame.Render3D;

public sealed partial class Renderer3D
{
    /// <summary>Most layers one layered material may have (the device may allow fewer).</summary>
    public const int MaxMaterialLayers = 2048;

    /// <summary>
    /// Makes a material whose base colour is a stack of same-size images, with the layer chosen per vertex (<see cref="MeshVertex3D.Layer"/>, or
    /// the second texture coordinate's first component of a glTF mesh). One such material draws every block texture of a voxel world in one batch.
    /// </summary>
    /// <remarks>
    /// Sampling is nearest, and coordinates repeat inside each layer: a quad whose coordinates run 0 to 4 shows the layer four times. Layers
    /// never mix, even in the mip chain. Everything else (style, outline, fog, shadows, instancing, alpha mode) works as for
    /// <see cref="CreateMaterial"/>. The images are copied; nothing of the arguments is kept.
    /// </remarks>
    /// <param name="name">A name for tools.</param>
    /// <param name="width">Width in texels of every layer.</param>
    /// <param name="height">Height in texels of every layer.</param>
    /// <param name="layers">One tightly packed RGBA8 image (sRGB-encoded colour, straight alpha) per layer, <c>width * height * 4</c> bytes each.</param>
    /// <param name="alpha">How alpha is treated.</param>
    /// <param name="doubleSided">Draw back faces too.</param>
    /// <param name="baseColor">Linear RGBA multiplied into the texels; null for white.</param>
    /// <param name="alphaCutoff">Threshold for <see cref="MaterialAlpha3D.Mask"/>.</param>
    /// <param name="mipmaps">Build a mip chain (each mip a box average of its layer, picked without blending between levels) so distant blocks do not shimmer. False keeps every texel sharp at any distance.</param>
    /// <returns>The material. Like every material it lives as long as the renderer.</returns>
    /// <exception cref="ArgumentException">There are no layers or too many, the size is not positive or exceeds the device's limit, or a layer has the wrong byte count.</exception>
    public Material3D CreateLayeredMaterial(string name, int width, int height, ReadOnlySpan<ReadOnlyMemory<byte>> layers, MaterialAlpha3D alpha = MaterialAlpha3D.Opaque, bool doubleSided = false, Vector4? baseColor = null, float alphaCutoff = 0.5f, bool mipmaps = true)
    {
        ArgumentNullException.ThrowIfNull(name);
        var caps = Device.Capabilities;
        if (layers.Length == 0 || layers.Length > MaxMaterialLayers || (uint)layers.Length > caps.MaxArrayLayers)
        {
            throw new ArgumentException($"A layered material needs 1 to {Math.Min(MaxMaterialLayers, caps.MaxArrayLayers)} layers; got {layers.Length}.", nameof(layers));
        }

        if (width <= 0 || height <= 0 || (uint)width > caps.MaxTextureSize || (uint)height > caps.MaxTextureSize)
        {
            throw new ArgumentException($"The layer size {width}x{height} must be positive and at most {caps.MaxTextureSize}.", nameof(width));
        }

        var layerBytes = checked(width * height * 4);
        for (var i = 0; i < layers.Length; i++)
        {
            if (layers[i].Length != layerBytes)
            {
                throw new ArgumentException($"Layer {i} has {layers[i].Length} bytes; a {width}x{height} RGBA8 image has {layerBytes}.", nameof(layers));
            }
        }

        var material = CreateMaterial(name, alpha, doubleSided, baseColor, alphaCutoff);
        var mips = mipmaps ? GpuTexture.FullMipCount((uint)width, (uint)height) : 1;
        var texture = material.OwnerModel.Own(Device.CreateTexture(new TextureDesc((uint)width, (uint)height, GpuFormat.Rgba8UnormSrgb, TextureUsage.Sampled | TextureUsage.CopyDestination)
        {
            MipLevels = mips,
            ArrayLayers = (uint)layers.Length,
            Label = "layers " + name,
        }));
        var sampler = material.OwnerModel.Own(Device.CreateSampler(new SamplerDesc
        {
            MagFilter = FilterMode.Nearest,
            MinFilter = FilterMode.Nearest,
            MipFilter = FilterMode.Nearest,
            AddressU = AddressMode.Repeat,
            AddressV = AddressMode.Repeat,
            MaxLod = mips > 1 ? 1000f : 0f,
            MaxAnisotropy = 1f,
        }));

        for (var layer = 0; layer < layers.Length; layer++)
        {
            var level = layers[layer].ToArray();
            int w = width, h = height;
            for (uint mip = 0; mip < mips; mip++)
            {
                Device.WriteTexture(texture, mip, (uint)layer, level);
                if (mip + 1 < mips)
                {
                    level = Downsample(level, ref w, ref h);
                }
            }
        }

        material.SetLayers(texture, sampler, layers.Length);
        return material;
    }

    // Box average of 2x2 texels in linear light (alpha straight, averaged as is), as the sRGB format is decoded when sampled.
    private static byte[] Downsample(byte[] src, ref int w, ref int h)
    {
        var nw = Math.Max(1, w / 2);
        var nh = Math.Max(1, h / 2);
        var dst = new byte[nw * nh * 4];
        for (var y = 0; y < nh; y++)
        {
            var y0 = Math.Min(y * 2, h - 1);
            var y1 = Math.Min((y * 2) + 1, h - 1);
            for (var x = 0; x < nw; x++)
            {
                var x0 = Math.Min(x * 2, w - 1);
                var x1 = Math.Min((x * 2) + 1, w - 1);
                var o = ((y * nw) + x) * 4;
                for (var c = 0; c < 4; c++)
                {
                    var a = src[(((y0 * w) + x0) * 4) + c];
                    var b = src[(((y0 * w) + x1) * 4) + c];
                    var d = src[(((y1 * w) + x0) * 4) + c];
                    var e = src[(((y1 * w) + x1) * 4) + c];
                    dst[o + c] = c == 3
                        ? (byte)((a + b + d + e + 2) / 4)
                        : LinearToSrgb((SrgbToLinear[a] + SrgbToLinear[b] + SrgbToLinear[d] + SrgbToLinear[e]) * 0.25f);
                }
            }
        }

        w = nw;
        h = nh;
        return dst;
    }

    private static readonly float[] SrgbToLinear = BuildSrgbTable();

    private static float[] BuildSrgbTable()
    {
        var t = new float[256];
        for (var i = 0; i < 256; i++)
        {
            var c = i / 255f;
            t[i] = c <= 0.04045f ? c / 12.92f : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
        }

        return t;
    }

    private static byte LinearToSrgb(float l)
    {
        var c = l <= 0.0031308f ? l * 12.92f : (1.055f * MathF.Pow(l, 1f / 2.4f)) - 0.055f;
        return (byte)Math.Clamp((int)MathF.Round(c * 255f), 0, 255);
    }
}
