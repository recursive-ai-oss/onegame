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

    /// <summary>Lux (directional) or candela (point, spot).</summary>
    public float Intensity { get; set; } = 1f;

    /// <summary>Distance at which a point or spot light is cut off with a smooth window; 0 means no limit.</summary>
    public float Range { get; set; }

    /// <summary>Spot: angle in radians from the axis inside which the light is at full strength.</summary>
    public float InnerConeAngle { get; set; }

    /// <summary>Spot: angle in radians from the axis at which the light reaches zero.</summary>
    public float OuterConeAngle { get; set; } = MathF.PI / 4f;

    /// <summary>A directional light.</summary>
    public static Light3D Directional(Vector3 direction, Vector3 color, float lux) =>
        new() { Kind = LightKind.Directional, Direction = direction, Color = color, Intensity = lux };

    /// <summary>A point light.</summary>
    public static Light3D Point(Vector3 position, Vector3 color, float candela, float range = 0f) =>
        new() { Kind = LightKind.Point, Position = position, Color = color, Intensity = candela, Range = range };

    /// <summary>A spot light.</summary>
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
