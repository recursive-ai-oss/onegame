using System.Numerics;
using OneGame.Core;
using OneGame.Render3D;

namespace OneGame.Host;

/// <summary>A camera as resolved for a frame: where it is and how it projects.</summary>
/// <param name="World">Camera-to-world transform (looks down its -Z).</param>
/// <param name="YFov">Vertical field of view in radians (perspective).</param>
/// <param name="OrthoHeight">Visible height in world units; 0 for a perspective camera.</param>
/// <param name="Near">Near plane distance.</param>
/// <param name="Far">Far plane distance.</param>
/// <param name="Source">The entity that described the camera; none for the default or an override.</param>
public readonly record struct ViewCamera(Matrix4x4 World, float YFov, float OrthoHeight, float Near, float Far, EntityId Source)
{
    /// <summary>True for an orthographic projection.</summary>
    public bool IsOrthographic => OrthoHeight > 0f;

    /// <summary>Position in the world.</summary>
    public Vector3 Position => World.Translation;

    /// <summary>The world-to-camera transform.</summary>
    public Matrix4x4 View => Matrix4x4.Invert(World, out var inverse) ? inverse : Matrix4x4.Identity;

    /// <summary>The projection for a target of the given width over height.</summary>
    public Matrix4x4 Projection(float aspect) => IsOrthographic
        ? Matrix4x4.CreateOrthographic(OrthoHeight * aspect, OrthoHeight, Near, Far)
        : Matrix4x4.CreatePerspectiveFieldOfView(YFov, aspect, Near, Far);
}

/// <summary>
/// What an entity's <see cref="IView3D.Describe3D"/> writes into: models, lights and cameras for one frame. Calls only collect; the host
/// turns them into a renderer scene afterwards, so describing costs no allocation once the lists have grown.
/// </summary>
public sealed class SceneWriter
{
    private const float Deg = MathF.PI / 180f;
    private readonly ModelLibrary _models;
    private readonly List<ModelDraw> _draws = [];
    private readonly List<LightDraw> _lights = [];
    private ViewCamera _camera;
    private int _cameraPriority = int.MinValue;
    private bool _hasCamera;

    internal SceneWriter(ModelLibrary models)
    {
        _models = models;
    }

    /// <summary>The tick being shown.</summary>
    public long Tick { get; internal set; }

    /// <summary>How far past <see cref="Tick"/> the frame is, from 0 to just under 1: pass both to <c>Timed&lt;T&gt;.ValueAt(tick, alpha)</c> to move smoothly between ticks.</summary>
    public Fixed Alpha { get; internal set; }

    /// <summary>The entity being described.</summary>
    public EntityId Entity { get; internal set; }

    internal List<ModelDraw> Draws => _draws;

    internal List<LightDraw> Lights => _lights;

    internal bool HasCamera => _hasCamera;

    internal ViewCamera ChosenCamera => _camera;

    internal void Clear()
    {
        _draws.Clear();
        _lights.Clear();
        _hasCamera = false;
        _cameraPriority = int.MinValue;
    }

    /// <summary>Shows a model from the <see cref="ModelLibrary"/> at a transform.</summary>
    /// <param name="model">The name the model was added under.</param>
    /// <param name="transform">Where it is.</param>
    /// <param name="pickable">Whether <c>pick</c> can hit it.</param>
    /// <exception cref="UnknownModelException">The library has no such model.</exception>
    public void Model(string model, Transform3 transform, bool pickable = true)
        => _draws.Add(new ModelDraw(_models.IndexOf(model), Entity, ToMatrix(transform), pickable));

    /// <summary>Shows a model with an extra view-only scale on top of its transform, for a pulse or a hover highlight.</summary>
    /// <param name="model">The name the model was added under.</param>
    /// <param name="transform">Where it is.</param>
    /// <param name="extraScale">Uniform factor applied in the model's own space.</param>
    /// <param name="pickable">Whether <c>pick</c> can hit it.</param>
    public void Model(string model, Transform3 transform, float extraScale, bool pickable = true)
        => _draws.Add(new ModelDraw(_models.IndexOf(model), Entity, Matrix4x4.CreateScale(extraScale) * ToMatrix(transform), pickable));

    /// <summary>A light that shines in all directions from the transform's position.</summary>
    /// <param name="transform">Where it is.</param>
    /// <param name="color">Linear RGB.</param>
    /// <param name="candela">Intensity.</param>
    /// <param name="range">Distance at which it fades out; 0 for none.</param>
    public void PointLight(Transform3 transform, Vector3 color, float candela, float range = 0f)
        => _lights.Add(new LightDraw(LightKind.Point, ToVector(transform.Position), default, color, candela, range, 0f, 0f));

    /// <summary>A light that shines along the transform's forward direction, as from infinitely far away.</summary>
    /// <param name="transform">Its rotation gives the direction.</param>
    /// <param name="color">Linear RGB.</param>
    /// <param name="lux">Intensity.</param>
    public void DirectionalLight(Transform3 transform, Vector3 color, float lux)
        => _lights.Add(new LightDraw(LightKind.Directional, default, ToVector(transform.Forward), color, lux, 0f, 0f, 0f));

    /// <summary>A cone of light from the transform's position along its forward direction.</summary>
    /// <param name="transform">Where it is and which way it points.</param>
    /// <param name="color">Linear RGB.</param>
    /// <param name="candela">Intensity.</param>
    /// <param name="innerDegrees">Half-angle of the full-strength cone.</param>
    /// <param name="outerDegrees">Half-angle at which the light reaches zero.</param>
    /// <param name="range">Distance at which it fades out; 0 for none.</param>
    public void SpotLight(Transform3 transform, Vector3 color, float candela, float innerDegrees, float outerDegrees, float range = 0f)
        => _lights.Add(new LightDraw(LightKind.Spot, ToVector(transform.Position), ToVector(transform.Forward), color, candela, range, innerDegrees * Deg, outerDegrees * Deg));

    /// <summary>
    /// Offers a camera. The frame uses the offered camera with the highest <paramref name="priority"/> (the first one on a tie), so a game can
    /// keep a spectator camera and a player camera and choose one by state.
    /// </summary>
    /// <param name="transform">Where it is; it looks along its forward direction.</param>
    /// <param name="fovDegrees">Vertical field of view.</param>
    /// <param name="priority">Higher wins.</param>
    /// <param name="near">Near plane distance.</param>
    /// <param name="far">Far plane distance.</param>
    /// <param name="orthoHeight">Greater than zero for an orthographic camera that shows this much world height.</param>
    public void Camera(Transform3 transform, float fovDegrees = 45f, int priority = 0, float near = 0.1f, float far = 1000f, float orthoHeight = 0f)
    {
        if (_hasCamera && priority <= _cameraPriority)
        {
            return;
        }

        _hasCamera = true;
        _cameraPriority = priority;

        // A camera is rigid: scale never applies to it.
        var rigid = Matrix4x4.CreateFromQuaternion(ToQuaternion(transform.Rotation));
        rigid.Translation = ToVector(transform.Position);
        _camera = new ViewCamera(rigid, fovDegrees * Deg, orthoHeight, near, far, Entity);
    }

    internal static Vector3 ToVector(FVec3 v) => new(v.X.ToSingle(), v.Y.ToSingle(), v.Z.ToSingle());

    internal static Quaternion ToQuaternion(FQuat q) =>
        Quaternion.Normalize(new Quaternion(q.X.ToSingle(), q.Y.ToSingle(), q.Z.ToSingle(), q.W.ToSingle()));

    /// <summary>The matrix that places a thing with this transform in the world (scale, then rotation, then translation; row vectors).</summary>
    /// <param name="t">The transform.</param>
    /// <returns>The world matrix.</returns>
    public static Matrix4x4 ToMatrix(Transform3 t)
    {
        var m = Matrix4x4.CreateScale(ToVector(t.Scale)) * Matrix4x4.CreateFromQuaternion(ToQuaternion(t.Rotation));
        m.Translation = ToVector(t.Position);
        return m;
    }

    internal readonly record struct ModelDraw(int Model, EntityId Entity, Matrix4x4 World, bool Pickable);

    internal readonly record struct LightDraw(LightKind Kind, Vector3 Position, Vector3 Direction, Vector3 Color, float Intensity, float Range, float Inner, float Outer);
}
