namespace OneGame.Render3D;

/// <summary>A named rung of the quality ladder. Switch it at runtime with <see cref="Scene3D.Quality"/>.</summary>
public enum QualityLevel
{
    /// <summary>Small shadow maps with two cascades and no filtering, no ambient occlusion, no multisampling and no bloom.</summary>
    Low = 0,

    /// <summary>1024 texel shadow maps, three cascades, half-resolution ambient occlusion, 4x multisampling.</summary>
    Medium = 1,

    /// <summary>2048 texel shadow maps, four cascades with 3x3 filtering, full-resolution ambient occlusion, 4x multisampling. The default.</summary>
    High = 2,

    /// <summary>4096 texel shadow maps with 5x5 filtering, more occlusion samples, 8x multisampling, a 512 texel environment.</summary>
    Ultra = 3,
}

/// <summary>What a <see cref="QualityLevel"/> means in resources and work. Features still have to be asked for by the scene; a level only bounds them.</summary>
/// <param name="Level">The level these settings belong to.</param>
/// <param name="ShadowTileSize">Edge of one shadow map tile in texels (one cascade, one spot light, one face of a point light).</param>
/// <param name="MaxCascades">Most cascades a directional light splits into.</param>
/// <param name="ShadowFilterRadius">Percentage-closer filter radius in texels: 0 is one hardware-filtered tap, 1 is 3x3 taps, 2 is 5x5.</param>
/// <param name="Ssao">Whether ambient occlusion may run.</param>
/// <param name="SsaoSamples">Occlusion samples per pixel.</param>
/// <param name="SsaoHalfResolution">Whether occlusion is computed at half resolution.</param>
/// <param name="MaxSamples">Most multisampling samples (1, 4 or 8; 2 is not used because many formats lack it); the device may allow fewer.</param>
/// <param name="Bloom">Whether bloom may run.</param>
/// <param name="EnvironmentSize">Face size of the specular cube of environments created without an explicit size.</param>
public readonly record struct QualitySettings(
    QualityLevel Level,
    int ShadowTileSize,
    int MaxCascades,
    int ShadowFilterRadius,
    bool Ssao,
    int SsaoSamples,
    bool SsaoHalfResolution,
    int MaxSamples,
    bool Bloom,
    int EnvironmentSize)
{
    /// <summary>The settings of a level.</summary>
    /// <param name="level">The level.</param>
    public static QualitySettings For(QualityLevel level) => level switch
    {
        QualityLevel.Low => new(level, 512, 2, 0, false, 0, true, 1, false, 64),
        QualityLevel.Medium => new(level, 1024, 3, 0, true, 8, true, 4, true, 128),
        QualityLevel.Ultra => new(level, 4096, 4, 2, true, 24, false, 8, true, 512),
        _ => new(QualityLevel.High, 2048, 4, 1, true, 14, false, 4, true, 256),
    };

    /// <summary>Parses a level name (case-insensitive); false for an unknown name.</summary>
    /// <param name="name">low, medium, high or ultra.</param>
    /// <param name="level">The level.</param>
    public static bool TryParse(string? name, out QualityLevel level)
    {
        level = QualityLevel.High;
        return name is not null && Enum.TryParse(name, ignoreCase: true, out level) && Enum.IsDefined(level);
    }
}

/// <summary>Scene-wide shadow controls. A light casts shadows when its <see cref="Light3D.CastShadows"/> is set.</summary>
public sealed class ShadowSettings
{
    /// <summary>Farthest distance from the camera, in world units, that directional light shadows reach (capped by the camera's far plane).</summary>
    public float MaxDistance { get; set; } = 60f;

    /// <summary>Mix of logarithmic (1) and uniform (0) placement of the cascade splits.</summary>
    public float SplitLambda { get; set; } = 0.75f;

    /// <summary>How far, in shadow map texels, a surface point is moved along its normal before the lookup. It removes self-shadowing acne.</summary>
    public float NormalOffset { get; set; } = 1.5f;
}

/// <summary>Screen-space ambient occlusion, computed from the depth and normals of the scene. Off by default.</summary>
public sealed class SsaoSettings
{
    /// <summary>Whether the effect runs (also needs a <see cref="QualitySettings.Ssao"/> level).</summary>
    public bool Enabled { get; set; }

    /// <summary>Radius in world units inside which occluders count.</summary>
    public float Radius { get; set; } = 0.5f;

    /// <summary>Strength: 0 none, 1 full.</summary>
    public float Intensity { get; set; } = 1f;
}

/// <summary>How a material is lit.</summary>
public enum MaterialStyle3D
{
    /// <summary>Physically based metallic-roughness shading, as authored.</summary>
    Pbr = 0,

    /// <summary>Cel shading: light is split into a few flat bands, with a hard specular highlight and an optional outline.</summary>
    Toon = 1,

    /// <summary>Unlit: the base colour (texture and vertex colour included) as is. An outline is still available.</summary>
    Flat = 2,
}
