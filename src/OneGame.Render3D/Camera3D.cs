using OneGame.Gltf;

namespace OneGame.Render3D;

/// <summary>
/// A camera. It looks down its own -Z axis with +Y up (the glTF convention); <see cref="World"/> places it. Matrices follow
/// <c>System.Numerics</c>: row vectors, right-handed, depth 0..1.
/// </summary>
public sealed class Camera3D
{
    /// <summary>Camera-to-world transform (rigid: rotation and translation only).</summary>
    public Matrix4x4 World { get; set; } = Matrix4x4.Identity;

    /// <summary>True for an orthographic projection (<see cref="OrthoHeight"/> sizes it) instead of a perspective one (<see cref="YFov"/>).</summary>
    public bool Orthographic { get; set; }

    /// <summary>Vertical field of view in radians (perspective).</summary>
    public float YFov { get; set; } = 45f * MathF.PI / 180f;

    /// <summary>Visible height in world units (orthographic).</summary>
    public float OrthoHeight { get; set; } = 10f;

    /// <summary>Distance of the near plane.</summary>
    public float Near { get; set; } = 0.1f;

    /// <summary>Distance of the far plane. Lighting clusters need a finite far plane; an infinite value is treated as 1000 there.</summary>
    public float Far { get; set; } = 1000f;

    /// <summary>Position in the world.</summary>
    public Vector3 Position => World.Translation;

    /// <summary>World-to-camera transform.</summary>
    public Matrix4x4 View => Matrix4x4.Invert(World, out var inverse) ? inverse : Matrix4x4.Identity;

    /// <summary>Projection for a render target of the given width over height.</summary>
    /// <param name="aspect">Width divided by height.</param>
    /// <returns>The projection matrix.</returns>
    public Matrix4x4 Projection(float aspect)
    {
        if (Orthographic)
        {
            return Matrix4x4.CreateOrthographic(OrthoHeight * aspect, OrthoHeight, Near, Far);
        }

        return Matrix4x4.CreatePerspectiveFieldOfView(YFov, aspect, Near, Far);
    }

    /// <summary>A camera at <paramref name="eye"/> looking at <paramref name="target"/>.</summary>
    /// <param name="eye">World position of the camera.</param>
    /// <param name="target">The point it looks at.</param>
    /// <param name="up">Which way is up, usually <see cref="Vector3.UnitY"/>.</param>
    public static Camera3D LookAt(Vector3 eye, Vector3 target, Vector3 up)
    {
        var view = Matrix4x4.CreateLookAt(eye, target, up);
        Matrix4x4.Invert(view, out var world);
        return new Camera3D { World = world };
    }

    /// <summary>
    /// A camera on a sphere around <paramref name="target"/>. Yaw 0 and pitch 0 look from +Z towards -Z (the front of a glTF model);
    /// positive yaw moves the camera towards +X, positive pitch raises it.
    /// </summary>
    /// <param name="target">The point the camera looks at.</param>
    /// <param name="distance">Distance from the target in world units (metres for glTF models).</param>
    /// <param name="yawDegrees">Angle around the vertical axis, in degrees.</param>
    /// <param name="pitchDegrees">Angle above the horizon, in degrees.</param>
    public static Camera3D Orbit(Vector3 target, float distance, float yawDegrees, float pitchDegrees)
    {
        var yaw = yawDegrees * MathF.PI / 180f;
        var pitch = pitchDegrees * MathF.PI / 180f;
        var offset = new Vector3(MathF.Sin(yaw) * MathF.Cos(pitch), MathF.Sin(pitch), MathF.Cos(yaw) * MathF.Cos(pitch)) * distance;
        return LookAt(target + offset, target, Vector3.UnitY);
    }

    /// <summary>
    /// An orbit camera placed so that a box fits the view whatever the angle: the distance is the one at which the box's bounding
    /// sphere fits the shorter side of the image. Near and far planes come from the box.
    /// </summary>
    /// <param name="min">Box minimum.</param>
    /// <param name="max">Box maximum.</param>
    /// <param name="yawDegrees">Yaw, as for <see cref="Orbit"/>.</param>
    /// <param name="pitchDegrees">Pitch, as for <see cref="Orbit"/>.</param>
    /// <param name="aspect">Width divided by height of the target.</param>
    /// <param name="yfov">Vertical field of view in radians.</param>
    /// <param name="margin">Extra distance as a factor (1 fits the sphere exactly).</param>
    public static Camera3D Frame(Vector3 min, Vector3 max, float yawDegrees, float pitchDegrees, float aspect, float yfov = 45f * MathF.PI / 180f, float margin = 1.1f)
    {
        var center = (min + max) * 0.5f;
        var radius = MathF.Max(Vector3.Distance(min, max) * 0.5f, 1e-4f);
        var halfFov = MathF.Min(yfov * 0.5f, MathF.Atan(MathF.Tan(yfov * 0.5f) * aspect));
        var distance = radius / MathF.Sin(halfFov) * margin;
        var camera = Orbit(center, distance, yawDegrees, pitchDegrees);
        camera.YFov = yfov;
        camera.Near = MathF.Max(distance - radius, distance * 0.01f);
        camera.Far = distance + radius;
        return camera;
    }

    /// <summary>The camera a glTF file describes, placed by the world matrix of the node that carries it.</summary>
    public static Camera3D FromGltf(GltfCamera camera, Matrix4x4 nodeWorld)
    {
        ArgumentNullException.ThrowIfNull(camera);

        // A node's scale does not scale the camera: keep rotation and translation.
        Matrix4x4.Decompose(nodeWorld, out _, out var rotation, out var translation);
        var world = Matrix4x4.CreateFromQuaternion(rotation);
        world.Translation = translation;
        return new Camera3D
        {
            World = world,
            Orthographic = !camera.Perspective,
            YFov = camera.Perspective ? camera.YFov : 45f * MathF.PI / 180f,
            OrthoHeight = camera.YMag * 2f,
            Near = camera.ZNear,
            Far = camera.ZFar ?? 1000f,
        };
    }
}
