using System.Numerics;
using OneGame.Core;
using OneGame.Render3D;

namespace OneGame.Host;

/// <summary>
/// What the <c>camera</c>, <c>pick</c> and <c>project</c> operations ask of a 3D scene: the camera in use, a view-only override of it, a world
/// point's place on screen and the model under a pixel. <see cref="SceneView3D"/> answers from the world's entities; <see cref="ManualScene3D"/>
/// answers for a scene a game fills by hand.
/// </summary>
public interface IScene3DProbe
{
    /// <summary>The camera in use.</summary>
    ViewCamera Camera { get; }

    /// <summary>A view-only camera that replaces the game's own; null follows the game again.</summary>
    ViewCamera? CameraOverride { get; set; }

    /// <summary>Brings the answers up to date with the world before a question is asked.</summary>
    /// <param name="world">The world being drawn.</param>
    /// <param name="tick">Its tick.</param>
    /// <param name="alpha">How far the frame is between ticks.</param>
    void Prepare(World world, long tick, Fixed alpha);

    /// <summary>Where a world point lands in a target of the given size.</summary>
    /// <param name="point">The world position.</param>
    /// <param name="width">Target width in pixels.</param>
    /// <param name="height">Target height in pixels.</param>
    /// <param name="pixel">The position in pixels.</param>
    /// <param name="depth">Distance in front of the camera.</param>
    /// <returns>False when the point is behind the camera or beyond its planes.</returns>
    bool TryProject(Vector3 point, int width, int height, out Vector2 pixel, out float depth);

    /// <summary>The ray through a pixel of a target of the given size.</summary>
    /// <param name="x">Pixels from the left.</param>
    /// <param name="y">Pixels from the top.</param>
    /// <param name="width">Target width in pixels.</param>
    /// <param name="height">Target height in pixels.</param>
    PickRay Ray(float x, float y, int width, int height);

    /// <summary>The nearest model along a ray, or null.</summary>
    /// <param name="ray">The ray.</param>
    PickHit? Cast(PickRay ray);

    /// <summary>What the last <see cref="Prepare"/> shows, one entry per model drawn (the <c>scene_models</c> operation lists it). Probes that cannot say return nothing.</summary>
    IReadOnlyList<SceneModelInfo> ListModels() => [];
}

/// <summary>One model drawn in a frame, as the <c>scene_models</c> operation reports it.</summary>
/// <param name="Model">The library name of the model or mesh (the instance's label for a hand-filled scene).</param>
/// <param name="Label">The label given with <see cref="ModelOptions.Label"/>, or null.</param>
/// <param name="Opacity">1 for solid; below 1 the model is blended.</param>
/// <param name="Entity">The entity that drew it, or the default id for a describer or a hand-filled scene.</param>
/// <param name="Dynamic">True for a mesh built at run time (<see cref="ModelLibrary.SetMesh"/>).</param>
/// <param name="Triangles">Triangles drawn (0 until a mesh's first upload).</param>
/// <param name="Position">Where the model is, in world space.</param>
public readonly record struct SceneModelInfo(string Model, string? Label, float Opacity, EntityId Entity, bool Dynamic, int Triangles, Vector3 Position);

/// <summary>
/// Lets the live-control operations <c>camera</c>, <c>pick</c> and <c>project</c> work on a scene a game fills by hand (a
/// <see cref="Render3DStage"/> built without a <see cref="SceneView3D"/>). The stage creates one over its <see cref="Render3DStage.Scene"/>;
/// a game with its own stage hands the scene to <see cref="ManualScene3D(Scene3D)"/> and returns it from <see cref="IScene3DStage.Manual"/>.
/// The camera is the scene's own, as the game last set it; an override set by the <c>camera</c> operation replaces it for drawing too, until reset.
/// Picking is triangle-exact at the models' rest pose and reports each hit by the instance's <see cref="ModelInstance3D.Label"/>; hits carry no entity.
/// </summary>
public sealed class ManualScene3D : IScene3DProbe
{
    private readonly Dictionary<Model3D, ModelLibrary.Entry> _entries = [];
    private readonly Camera3D _override = new();
    private Camera3D? _original;

    /// <summary>Creates the probe over a scene.</summary>
    /// <param name="scene">The scene the game draws each frame.</param>
    public ManualScene3D(Scene3D scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        Scene = scene;
    }

    /// <summary>The scene being probed.</summary>
    public Scene3D Scene { get; }

    /// <inheritdoc />
    public ViewCamera? CameraOverride { get; set; }

    /// <inheritdoc />
    public ViewCamera Camera => CameraOverride ?? From(Scene.Camera);

    /// <summary>Puts the override into the scene's camera. A stage calls it after the game's own per-frame update, so the override wins.</summary>
    public void ApplyOverride()
    {
        if (CameraOverride is not { } c)
        {
            // A game that sets its camera once (not every frame) gets it back when the override is cleared.
            if (_original is not null && ReferenceEquals(Scene.Camera, _override))
            {
                Scene.Camera = _original;
            }

            _original = null;
            return;
        }

        if (!ReferenceEquals(Scene.Camera, _override))
        {
            _original = Scene.Camera;
        }

        _override.World = c.World;
        _override.Orthographic = c.IsOrthographic;
        _override.YFov = c.YFov;
        _override.OrthoHeight = c.OrthoHeight;
        _override.Near = c.Near;
        _override.Far = c.Far;
        Scene.Camera = _override;
    }

    /// <inheritdoc />
    public void Prepare(World world, long tick, Fixed alpha)
    {
    }

    /// <inheritdoc />
    public bool TryProject(Vector3 point, int width, int height, out Vector2 pixel, out float depth)
    {
        var cam = Camera;
        var clip = Vector4.Transform(new Vector4(point, 1f), cam.View * cam.Projection(Aspect(width, height)));
        pixel = default;
        depth = clip.W;
        if (clip.W <= 0f)
        {
            return false;
        }

        var z = clip.Z / clip.W;
        if (z is < 0f or > 1f)
        {
            return false;
        }

        pixel = new Vector2(((clip.X / clip.W * 0.5f) + 0.5f) * width, (0.5f - (clip.Y / clip.W * 0.5f)) * height);
        return true;
    }

    /// <inheritdoc />
    public PickRay Ray(float x, float y, int width, int height)
    {
        var cam = Camera;
        Matrix4x4.Invert(cam.View * cam.Projection(Aspect(width, height)), out var inverse);
        var nx = (2f * x / width) - 1f;
        var ny = 1f - (2f * y / height);
        var near = Unproject(inverse, nx, ny, 0f);
        var far = Unproject(inverse, nx, ny, 1f);
        var dir = far - near;
        return new PickRay(near, dir.LengthSquared() > 0f ? Vector3.Normalize(dir) : -Vector3.UnitZ);
    }

    /// <inheritdoc />
    public PickHit? Cast(PickRay ray)
    {
        PickHit? best = null;
        var bestT = float.PositiveInfinity;
        var models = Scene.Models;
        for (var i = 0; i < models.Count; i++)
        {
            var instance = models[i];
            if (!instance.Visible || instance.Opacity <= 0f)
            {
                continue;
            }

            if (!_entries.TryGetValue(instance.Model, out var entry))
            {
                entry = new ModelLibrary.Entry(instance.Label ?? "model", instance.Model.Source);
                _entries[instance.Model] = entry;
            }

            if (!entry.Intersect(instance.Transform, ray.Origin, ray.Direction, out var t) || t >= bestT)
            {
                continue;
            }

            bestT = t;
            best = new PickHit(default, instance.Label ?? "model#" + i.ToString(System.Globalization.CultureInfo.InvariantCulture), t, ray.Origin + (ray.Direction * t));
        }

        return best;
    }

    /// <inheritdoc />
    public IReadOnlyList<SceneModelInfo> ListModels()
    {
        var list = new List<SceneModelInfo>();
        var models = Scene.Models;
        for (var i = 0; i < models.Count; i++)
        {
            var m = models[i];
            if (m.Visible)
            {
                list.Add(new SceneModelInfo(m.Label ?? "model#" + i.ToString(System.Globalization.CultureInfo.InvariantCulture), m.Label, m.Opacity, default, false, m.Model.TriangleCount, m.Transform.Translation));
            }
        }

        return list;
    }

    private static ViewCamera From(Camera3D c) => new(c.World, c.YFov, c.Orthographic ? c.OrthoHeight : 0f, c.Near, c.Far, default);

    private static float Aspect(int width, int height) => height > 0 ? (float)width / height : 1f;

    private static Vector3 Unproject(Matrix4x4 inverse, float x, float y, float z)
    {
        var p = Vector4.Transform(new Vector4(x, y, z, 1f), inverse);
        return new Vector3(p.X, p.Y, p.Z) / p.W;
    }
}
