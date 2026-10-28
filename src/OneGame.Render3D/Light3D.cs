using OneGame.Gltf;

namespace OneGame.Render3D;

/// <summary>The kind of a punctual light (KHR_lights_punctual).</summary>
public enum LightKind
{
    /// <summary>Parallel rays from infinitely far away; intensity is in lux.</summary>
    Directional,

    /// <summary>Radiates equally in all directions from a point; intensity is in candela.</summary>
    Point,

    /// <summary>A point light limited to a cone; intensity is in candela.</summary>
    Spot,
}

/// <summary>
/// A punctual light. Intensities are scene-referred: a directional light of E lux lights a white diffuse surface facing it to radiance
/// E/pi, a point light of I candela at distance d gives illuminance I/d^2. The camera's exposure maps that to the display.
/// </summary>
public sealed class Light3D
{
    /// <summary>Kind of light.</summary>
    public LightKind Kind { get; set; }

    /// <summary>Position (point and spot).</summary>
    public Vector3 Position { get; set; }

    /// <summary>Direction the light shines in (directional and spot); it need not be normalized.</summary>
    public Vector3 Direction { get; set; } = -Vector3.UnitZ;

    /// <summary>Linear RGB colour.</summary>
    public Vector3 Color { get; set; } = Vector3.One;

    /// <summary>
    /// Whether the light casts shadows. A directional light uses cascaded shadow maps, a spot light one map, a point light six. At most
    /// <see cref="Renderer3D.MaxShadowLights"/> lights and <see cref="Renderer3D.MaxShadowTiles"/> maps are used per frame, in light order.
    /// </summary>
    public bool CastShadows { get; set; }

    /// <summary>Lux (directional) or candela (point, spot).</summary>
    public float Intensity { get; set; } = 1f;

    /// <summary>Distance at which a point or spot light is cut off with a smooth window; 0 means no limit.</summary>
    public float Range { get; set; }

    /// <summary>Spot: angle in radians from the axis inside which the light is at full strength.</summary>
    public float InnerConeAngle { get; set; }

    /// <summary>Spot: angle in radians from the axis at which the light reaches zero.</summary>
    public float OuterConeAngle { get; set; } = MathF.PI / 4f;

    /// <summary>A directional light: parallel rays, like the sun.</summary>
    /// <param name="direction">The way the light travels (from the light toward the scene); need not be normalized.</param>
    /// <param name="color">Linear RGB, each channel 0 to 1.</param>
    /// <param name="lux">Intensity in lux (the <c>onegame view</c> default is 3).</param>
    public static Light3D Directional(Vector3 direction, Vector3 color, float lux) =>
        new() { Kind = LightKind.Directional, Direction = direction, Color = color, Intensity = lux };

    /// <summary>A point light: radiates equally in every direction.</summary>
    /// <param name="position">World position.</param>
    /// <param name="color">Linear RGB.</param>
    /// <param name="candela">Intensity in candela; the lit surface receives candela divided by distance squared.</param>
    /// <param name="range">Distance in world units at which the light fades to zero; 0 for no limit.</param>
    public static Light3D Point(Vector3 position, Vector3 color, float candela, float range = 0f) =>
        new() { Kind = LightKind.Point, Position = position, Color = color, Intensity = candela, Range = range };

    /// <summary>A spot light: a point light limited to a cone.</summary>
    /// <param name="position">World position.</param>
    /// <param name="direction">The cone's axis, from the light toward the scene.</param>
    /// <param name="color">Linear RGB.</param>
    /// <param name="candela">Intensity in candela.</param>
    /// <param name="innerCone">Angle in radians from the axis inside which the light is at full strength.</param>
    /// <param name="outerCone">Angle in radians from the axis at which it reaches zero; at least <paramref name="innerCone"/>.</param>
    /// <param name="range">Distance at which the light fades to zero; 0 for no limit.</param>
    public static Light3D Spot(Vector3 position, Vector3 direction, Vector3 color, float candela, float innerCone, float outerCone, float range = 0f) =>
        new() { Kind = LightKind.Spot, Position = position, Direction = direction, Color = color, Intensity = candela, InnerConeAngle = innerCone, OuterConeAngle = outerCone, Range = range };

    /// <summary>The light a glTF file describes, placed by the world matrix of its node (glTF lights shine down the node's -Z axis).</summary>
    public static Light3D FromGltf(GltfLight light, Matrix4x4 nodeWorld)
    {
        ArgumentNullException.ThrowIfNull(light);
        var dir = Vector3.Normalize(Vector3.TransformNormal(-Vector3.UnitZ, nodeWorld));
        return new Light3D
        {
            Kind = light.Type switch { GltfLightType.Directional => LightKind.Directional, GltfLightType.Point => LightKind.Point, _ => LightKind.Spot },
            Position = nodeWorld.Translation,
            Direction = dir,
            Color = light.Color,
            Intensity = light.Intensity,
            Range = light.Range ?? 0f,
            InnerConeAngle = light.InnerConeAngle,
            OuterConeAngle = light.OuterConeAngle,
        };
    }
}
