namespace OneGame.Render3D;

/// <summary>
/// Light shafts (also called god rays or volumetric light): lights scattering through the air, so a sun through a gap in a wall or a
/// flashlight's cone shows as a beam, and objects that block the light leave dark shafts in it. Off by default; with
/// <see cref="Enabled"/> false (or no light opting in through <see cref="Light3D.Volumetric"/>) the frame has no extra pass and is
/// identical to one drawn without the feature.
/// </summary>
/// <remarks>
/// Each opted-in light is integrated along every view ray with single scattering, at a fraction of the frame size and then
/// upsampled edge-aware, using the light's shadow maps (a light without <see cref="Light3D.CastShadows"/> fills the air evenly).
/// The cost is bounded by the quality level (<see cref="QualitySettings.VolumetricSteps"/>, <see cref="QualitySettings.VolumetricScale"/>):
/// off at Low. At most <see cref="Renderer3D.MaxVolumetricLights"/> lights are used. Shafts are added to the lit scene before bloom and
/// tone mapping, over surfaces, the backdrop and blended surfaces alike; the planar reflection does not show them.
/// </remarks>
public sealed class VolumetricSettings
{
    private float _density = 0.02f;
    private float _intensity = 1f;
    private float _anisotropy = 0.5f;
    private float _maxDistance = 60f;

    /// <summary>Whether shafts are drawn (they still need a level that allows them and at least one opted-in light).</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// How much the air scatters, per world unit along the ray (default 0.02). It sets how bright the shafts are and, since the same
    /// amount also dims light on its way to the camera, how far they reach: the visible depth is about 1 / Density. Negative and
    /// non-finite values are 0.
    /// </summary>
    public float Density { get => _density; set => _density = float.IsFinite(value) ? Math.Max(value, 0f) : 0f; }

    /// <summary>A multiplier on the scattered light (default 1), in the light's own units: a sun of 3 lux and an intensity of 1 gives faint shafts, 5 to 20 bold ones.</summary>
    public float Intensity { get => _intensity; set => _intensity = float.IsFinite(value) ? Math.Max(value, 0f) : 0f; }

    /// <summary>
    /// Henyey-Greenstein anisotropy g, -0.95 to 0.95 (default 0.5): positive values scatter toward the direction the light travels, so shafts
    /// are brightest when you look toward the light; 0 scatters evenly; negative values favour scattering back toward the light's source.
    /// </summary>
    public float Anisotropy { get => _anisotropy; set => _anisotropy = float.IsFinite(value) ? Math.Clamp(value, -0.95f, 0.95f) : 0f; }

    /// <summary>
    /// Longest stretch of a view ray, from the camera, that is marched (default 60). Rays that hit a surface stop there; rays into the sky
    /// stop here. Keep it at or below <see cref="ShadowSettings.MaxDistance"/>: a directional light is unshadowed beyond its last cascade,
    /// so the air past it would glow evenly. Larger values spread the fixed number of steps over more distance.
    /// </summary>
    public float MaxDistance { get => _maxDistance; set => _maxDistance = float.IsFinite(value) ? Math.Max(value, 0.1f) : 60f; }

    /// <summary>
    /// When true and <see cref="Scene3D.Fog"/> is on, the shafts fade with the fog's amount at each point of the ray, so a surface lost in
    /// fog does not sit behind bright shafts and the two read as one medium. The fog's own colour is not added here: it is already part of
    /// the surfaces. With fog off this has no effect. Fog modes added later follow automatically, since the shader asks the fog for its amount.
    /// </summary>
    public bool LinkToFog { get; set; }
}
