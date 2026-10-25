namespace OneGame.Gltf;

/// <summary>How the alpha of the base colour is used.</summary>
public enum GltfAlphaMode
{
    /// <summary>Alpha is ignored.</summary>
    Opaque,

    /// <summary>Pixels below <see cref="GltfMaterial.AlphaCutoff"/> are dropped.</summary>
    Mask,

    /// <summary>Alpha blends with what is behind.</summary>
    Blend,
}

/// <summary>A KHR_texture_transform: offset, rotation and scale of the texture coordinates.</summary>
/// <param name="Offset">Offset in texture-size units.</param>
/// <param name="Rotation">Rotation in radians; positive turns the image clockwise on screen (see <see cref="ToMatrix"/>).</param>
/// <param name="Scale">Scale of the coordinates.</param>
public readonly record struct TextureTransform(Vector2 Offset, float Rotation, Vector2 Scale)
{
    /// <summary>The identity transform.</summary>
    public static TextureTransform Identity => new(Vector2.Zero, 0, Vector2.One);

    /// <summary>The matrix <c>T * R * S</c> of the extension, in row-vector convention: <c>uv' = uv * M</c>.</summary>
    public Matrix3x2 ToMatrix()
    {
        // KHR_texture_transform: matrix = translation * rotation * scale. Texture space has v pointing down, so the extension's
        // counter-clockwise rotation of the coordinates (the image turns clockwise on screen) is x' = c*x + s*y, y' = -s*x + c*y.
        // The TextureTransformTest sample marks this direction: its arrow must point at the green marker.
        var c = MathF.Cos(Rotation);
        var s = MathF.Sin(Rotation);
        return new Matrix3x2(c * Scale.X, -s * Scale.X, s * Scale.Y, c * Scale.Y, Offset.X, Offset.Y);
    }
}

/// <summary>A reference from a material slot to a texture.</summary>
public sealed class TextureRef
{
    /// <summary>Index into <see cref="GltfModel.Textures"/>.</summary>
    public int Texture { get; internal set; }

    /// <summary>Which texture coordinate set to use (0 or 1 are drawn; others are kept as written).</summary>
    public int TexCoord { get; internal set; }

    /// <summary>The KHR_texture_transform, or null.</summary>
    public TextureTransform? Transform { get; internal set; }

    /// <summary>The coordinate-transform matrix (identity without an extension), <c>uv' = uv * M</c>.</summary>
    public Matrix3x2 UvMatrix => Transform?.ToMatrix() ?? Matrix3x2.Identity;
}

/// <summary>KHR_materials_clearcoat.</summary>
public sealed class ClearcoatInfo
{
    /// <summary>Clearcoat strength.</summary>
    public float Factor { get; internal set; }

    /// <summary>Strength texture (red channel), or null.</summary>
    public TextureRef? Texture { get; internal set; }

    /// <summary>Clearcoat roughness.</summary>
    public float RoughnessFactor { get; internal set; }

    /// <summary>Roughness texture (green channel), or null.</summary>
    public TextureRef? RoughnessTexture { get; internal set; }

    /// <summary>Normal map for the coat, or null.</summary>
    public TextureRef? NormalTexture { get; internal set; }

    /// <summary>Scale of the coat normal map.</summary>
    public float NormalScale { get; internal set; } = 1;
}

/// <summary>KHR_materials_transmission.</summary>
public sealed class TransmissionInfo
{
    /// <summary>The fraction of light transmitted.</summary>
    public float Factor { get; internal set; }

    /// <summary>Transmission texture (red channel), or null.</summary>
    public TextureRef? Texture { get; internal set; }
}

/// <summary>KHR_materials_specular.</summary>
public sealed class SpecularInfo
{
    /// <summary>Strength of the dielectric specular reflection.</summary>
    public float Factor { get; internal set; } = 1;

    /// <summary>Strength texture (alpha channel), or null.</summary>
    public TextureRef? Texture { get; internal set; }

    /// <summary>Tint of the specular reflection (linear RGB).</summary>
    public Vector3 ColorFactor { get; internal set; } = Vector3.One;

    /// <summary>Tint texture (sRGB RGB), or null.</summary>
    public TextureRef? ColorTexture { get; internal set; }
}

/// <summary>KHR_materials_sheen.</summary>
public sealed class SheenInfo
{
    /// <summary>Sheen colour (linear RGB).</summary>
    public Vector3 ColorFactor { get; internal set; }

    /// <summary>Sheen colour texture (sRGB RGB), or null.</summary>
    public TextureRef? ColorTexture { get; internal set; }

    /// <summary>Sheen roughness.</summary>
    public float RoughnessFactor { get; internal set; }

    /// <summary>Sheen roughness texture (alpha channel), or null.</summary>
    public TextureRef? RoughnessTexture { get; internal set; }
}

/// <summary>KHR_materials_volume.</summary>
public sealed class VolumeInfo
{
    /// <summary>Thickness of the volume in model units.</summary>
    public float ThicknessFactor { get; internal set; }

    /// <summary>Thickness texture (green channel), or null.</summary>
    public TextureRef? ThicknessTexture { get; internal set; }

    /// <summary>Distance at which light is attenuated to <see cref="AttenuationColor"/>; infinity when absent.</summary>
    public float AttenuationDistance { get; internal set; } = float.PositiveInfinity;

    /// <summary>Colour light has after travelling <see cref="AttenuationDistance"/> (linear RGB).</summary>
    public Vector3 AttenuationColor { get; internal set; } = Vector3.One;
}

/// <summary>A metal/rough material with the supported extensions.</summary>
public sealed class GltfMaterial
{
    /// <summary>The material's name, or empty.</summary>
    public string Name { get; internal set; } = "";

    /// <summary>Linear RGBA multiplier of the base colour.</summary>
    public Vector4 BaseColorFactor { get; internal set; } = Vector4.One;

    /// <summary>Base colour texture (sRGB), or null.</summary>
    public TextureRef? BaseColorTexture { get; internal set; }

    /// <summary>Metalness multiplier.</summary>
    public float MetallicFactor { get; internal set; } = 1;

    /// <summary>Roughness multiplier.</summary>
    public float RoughnessFactor { get; internal set; } = 1;

    /// <summary>Metal (blue) and roughness (green) texture, or null.</summary>
    public TextureRef? MetallicRoughnessTexture { get; internal set; }

    /// <summary>Tangent-space normal map, or null.</summary>
    public TextureRef? NormalTexture { get; internal set; }

    /// <summary>Scale of the normal map's xy.</summary>
    public float NormalScale { get; internal set; } = 1;

    /// <summary>Ambient occlusion map (red channel), or null.</summary>
    public TextureRef? OcclusionTexture { get; internal set; }

    /// <summary>How strongly occlusion applies (0 to 1).</summary>
    public float OcclusionStrength { get; internal set; } = 1;

    /// <summary>Linear RGB emission multiplier (before <see cref="EmissiveStrength"/>).</summary>
    public Vector3 EmissiveFactor { get; internal set; }

    /// <summary>Emission texture (sRGB), or null.</summary>
    public TextureRef? EmissiveTexture { get; internal set; }

    /// <summary>KHR_materials_emissive_strength: multiplies the emission (1 when absent).</summary>
    public float EmissiveStrength { get; internal set; } = 1;

    /// <summary>How alpha is used.</summary>
    public GltfAlphaMode AlphaMode { get; internal set; }

    /// <summary>Cutoff for <see cref="GltfAlphaMode.Mask"/>.</summary>
    public float AlphaCutoff { get; internal set; } = 0.5f;

    /// <summary>Draw back faces too, lit from the other side.</summary>
    public bool DoubleSided { get; internal set; }

    /// <summary>KHR_materials_unlit: draw the base colour without lighting.</summary>
    public bool Unlit { get; internal set; }

    /// <summary>KHR_materials_ior: index of refraction (1.5 when absent).</summary>
    public float Ior { get; internal set; } = 1.5f;

    /// <summary>KHR_materials_clearcoat, or null.</summary>
    public ClearcoatInfo? Clearcoat { get; internal set; }

    /// <summary>KHR_materials_transmission, or null.</summary>
    public TransmissionInfo? Transmission { get; internal set; }

    /// <summary>KHR_materials_specular, or null.</summary>
    public SpecularInfo? Specular { get; internal set; }

    /// <summary>KHR_materials_sheen, or null.</summary>
    public SheenInfo? Sheen { get; internal set; }

    /// <summary>KHR_materials_volume, or null.</summary>
    public VolumeInfo? Volume { get; internal set; }
}

/// <summary>Texture magnification filter.</summary>
public enum GltfMagFilter
{
    /// <summary>The file does not say.</summary>
    Unspecified,

    /// <summary>Nearest texel.</summary>
    Nearest,

    /// <summary>Bilinear.</summary>
    Linear,
}

/// <summary>Texture minification filter.</summary>
public enum GltfMinFilter
{
    /// <summary>The file does not say.</summary>
    Unspecified,

    /// <summary>Nearest texel.</summary>
    Nearest,

    /// <summary>Bilinear.</summary>
    Linear,

    /// <summary>Nearest texel in the nearest mip.</summary>
    NearestMipmapNearest,

    /// <summary>Bilinear in the nearest mip.</summary>
    LinearMipmapNearest,

    /// <summary>Nearest texel blended between mips.</summary>
    NearestMipmapLinear,

    /// <summary>Trilinear.</summary>
    LinearMipmapLinear,
}

/// <summary>Texture coordinate wrapping.</summary>
public enum GltfWrap
{
    /// <summary>Tile the texture.</summary>
    Repeat,

    /// <summary>Clamp to the edge texel.</summary>
    ClampToEdge,

    /// <summary>Tile, mirroring every other tile.</summary>
    MirroredRepeat,
}

/// <summary>Texture sampling state.</summary>
public sealed class GltfSampler
{
    /// <summary>The sampler's name, or empty.</summary>
    public string Name { get; internal set; } = "";

    /// <summary>Magnification filter.</summary>
    public GltfMagFilter MagFilter { get; internal set; }

    /// <summary>Minification filter.</summary>
    public GltfMinFilter MinFilter { get; internal set; }

    /// <summary>Wrapping along U.</summary>
    public GltfWrap WrapS { get; internal set; }

    /// <summary>Wrapping along V.</summary>
    public GltfWrap WrapT { get; internal set; }
}

/// <summary>A texture: an image with a sampler.</summary>
public sealed class GltfTexture
{
    /// <summary>The texture's name, or empty.</summary>
    public string Name { get; internal set; } = "";

    /// <summary>Index into <see cref="GltfModel.Images"/>, or -1 when the source needs an unsupported extension.</summary>
    public int Source { get; internal set; } = -1;

    /// <summary>Index into <see cref="GltfModel.Samplers"/>, or -1 for the default (repeat, linear).</summary>
    public int Sampler { get; internal set; } = -1;
}

/// <summary>The outcome of reading an image.</summary>
public enum ImageStatus
{
    /// <summary>Decoded; <see cref="GltfImage.Pixels"/> holds RGBA8.</summary>
    Decoded,

    /// <summary>Only the header was read because decoding was switched off.</summary>
    HeaderOnly,

    /// <summary>The format is not PNG or JPEG; there are no pixels.</summary>
    Unsupported,
}

/// <summary>An image of the model.</summary>
public sealed class GltfImage
{
    /// <summary>The image's name, or empty.</summary>
    public string Name { get; internal set; } = "";

    /// <summary>The mime type as written in the file, or empty.</summary>
    public string MimeType { get; internal set; } = "";

    /// <summary>The format found in the bytes: <c>png</c>, <c>jpeg</c> or <c>unknown</c>.</summary>
    public string Format { get; internal set; } = "unknown";

    /// <summary>What happened to the image.</summary>
    public ImageStatus Status { get; internal set; }

    /// <summary>Width in pixels (0 when unknown).</summary>
    public int Width { get; internal set; }

    /// <summary>Height in pixels (0 when unknown).</summary>
    public int Height { get; internal set; }

    /// <summary>Straight-alpha RGBA8 pixels, top row first, or null.</summary>
    public byte[]? Pixels { get; internal set; }

    /// <summary>True when a material uses the image as colour (sRGB): base colour, emissive, sheen or specular colour.</summary>
    public bool UsedAsColor { get; internal set; }

    /// <summary>True when a material uses the image as data (linear): normals, occlusion, metal/rough, clearcoat and the like.</summary>
    public bool UsedAsData { get; internal set; }
}
