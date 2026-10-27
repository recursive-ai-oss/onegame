using System.Numerics;
using OneGame.Core;
using OneGame.Gltf;
using OneGame.Render3D;

namespace OneGame.Host;

/// <summary>A ray in world space.</summary>
/// <param name="Origin">Where it starts.</param>
/// <param name="Direction">Its direction, of length one.</param>
public readonly record struct PickRay(Vector3 Origin, Vector3 Direction);

/// <summary>What a pick found.</summary>
/// <param name="Entity">The entity whose model was hit.</param>
/// <param name="Model">The name of the model.</param>
/// <param name="Distance">Distance from the ray origin to the hit.</param>
/// <param name="Point">The hit position in the world.</param>
public readonly record struct PickHit(EntityId Entity, string Model, float Distance, Vector3 Point);

/// <summary>
/// Turns a world of <see cref="IView3D"/> entities into a 3D scene and answers questions about it: which camera is in use, where a world
/// point lands on screen, which entity is under a screen point. It only reads the world; it never changes state. Everything but
/// <see cref="Build"/> works without a graphics device.
/// </summary>
public sealed class SceneView3D : IDisposable
{
    private readonly SceneWriter _writer;
    private readonly List<Light3D> _lightPool = [];
    private readonly Camera3D _camera = new();
    private readonly List<BillboardBatch3D> _billboardPool = [];

    /// <summary>Creates a view over a model library.</summary>
    /// <param name="models">The models entities can show; a new empty library when null.</param>
    public SceneView3D(ModelLibrary? models = null)
    {
        Models = models ?? new ModelLibrary();
        _writer = new SceneWriter(Models);
    }

    /// <summary>The models entities can show.</summary>
    public ModelLibrary Models { get; }

    /// <summary>
    /// A camera that replaces the one the entities describe, for tools: the <c>camera</c> operation sets it. It is a view setting, not
    /// simulation state, and recordings do not hold it. Null follows the entities again.
    /// </summary>
    public ViewCamera? CameraOverride { get; set; }

    /// <summary>
    /// Describes the scene from the world as a whole, after the <see cref="IView3D"/> entities have been asked. A game whose entities live in
    /// a library that cannot reference the host (a shared rules assembly) draws them here, from view code. It is called with the world being
    /// drawn; it may read any state and writes only into the <see cref="SceneWriter"/>.
    /// </summary>
    public Action<World, SceneWriter>? Describer { get; set; }

    /// <summary>The camera used when no entity offers one: back and up from the origin, looking at it.</summary>
    public static ViewCamera DefaultCamera { get; } = new(
        Matrix4x4.Invert(Matrix4x4.CreateLookAt(new Vector3(0f, 3f, 8f), Vector3.Zero, Vector3.UnitY), out var w) ? w : Matrix4x4.Identity,
        45f * MathF.PI / 180f, 0f, 0.1f, 1000f, default);

    /// <summary>Asks every <see cref="IView3D"/> entity of <paramref name="world"/> what it looks like at a moment. Later calls to the camera, projection and pick methods use the answer.</summary>
    /// <param name="world">The world to show: the authority's, or a replica's.</param>
    /// <param name="tick">The tick to show.</param>
    /// <param name="alpha">How far past the tick, from 0 to just under 1.</param>
    public void Prepare(World world, long tick, Fixed alpha)
    {
        ArgumentNullException.ThrowIfNull(world);
        _writer.Clear();
        _writer.Tick = tick;
        _writer.Alpha = alpha;
        _writer.BeginTime();
        var entities = world.Entities;
        for (var i = 0; i < entities.Count; i++)
        {
            if (entities[i] is IView3D view)
            {
                _writer.Entity = entities[i].Id;
                view.Describe3D(_writer);
            }
        }

        _writer.Entity = default;
        Describer?.Invoke(world, _writer);
        _writer.Entity = default;
    }

    /// <summary>The particle emitters entities showed in the last <see cref="Prepare"/>, in call order.</summary>
    public IReadOnlyList<OneGame.Particles.ParticleEmitter> ParticleEmitters => _writer.ParticleEmitters;

    /// <summary>The camera in use as of the last <see cref="Prepare"/>: the override, else the entity camera with the highest priority, else <see cref="DefaultCamera"/>.</summary>
    public ViewCamera Camera => CameraOverride ?? (_writer.HasCamera ? _writer.ChosenCamera : DefaultCamera);

    /// <summary>Fills a renderer scene from the last <see cref="Prepare"/>: camera, model instances and lights. Models are uploaded the first time they are drawn.</summary>
    /// <param name="scene">The scene to fill; its camera, models and lights are replaced. Environment and post settings are left alone.</param>
    /// <param name="renderer">The renderer that owns the GPU copies.</param>
    public void Build(Scene3D scene, Renderer3D renderer)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(renderer);
        var cam = Camera;
        _camera.World = cam.World;
        _camera.Orthographic = cam.IsOrthographic;
        _camera.YFov = cam.YFov;
        _camera.OrthoHeight = cam.OrthoHeight;
        _camera.Near = cam.Near;
        _camera.Far = cam.Far;
        scene.Camera = _camera;

        scene.Models.Clear();
        for (var i = 0; i < Models.Count; i++)
        {
            Models.At(i).Used = 0;
        }

        // Instances are pooled per model, in draw order, so a steady scene allocates nothing.
        var draws = _writer.Draws;
        for (var i = 0; i < draws.Count; i++)
        {
            var d = draws[i];
            var entry = Models.At(d.Model);
            entry.Gpu ??= renderer.Upload(entry.Source);
            ModelInstance3D instance;
            if (entry.Used < entry.Pool.Count)
            {
                instance = entry.Pool[entry.Used];
                scene.Models.Add(instance);
            }
            else
            {
                instance = scene.Add(entry.Gpu);
                entry.Pool.Add(instance);
            }

            entry.Used++;
            instance.Transform = d.World;
            instance.Visible = true;
        }

        // Instanced copies: one pooled batch per model, refilled each frame. The batch joins the scene's list once; batches the game added itself are left alone.
        for (var i = 0; i < Models.Count; i++)
        {
            Models.At(i).Batch?.Clear();
        }

        var instances = _writer.Instances;
        for (var i = 0; i < instances.Count; i++)
        {
            var d = instances[i];
            var entry = Models.At(d.Model);
            entry.Gpu ??= renderer.Upload(entry.Source);
            if (entry.Batch is null)
            {
                entry.Batch = scene.AddInstanced(entry.Gpu, 64);
            }
            else if (!scene.Batches.Contains(entry.Batch))
            {
                scene.Batches.Add(entry.Batch);
            }

            entry.Batch.Add(d.World, d.Tint);
        }

        BuildBillboards(scene);

        scene.Lights.Clear();
        var lights = _writer.Lights;
        for (var i = 0; i < lights.Count; i++)
        {
            if (_lightPool.Count <= i)
            {
                _lightPool.Add(new Light3D());
            }

            var l = lights[i];
            var light = _lightPool[i];
            light.Kind = l.Kind;
            light.Position = l.Position;
            light.Direction = l.Direction;
            light.Color = l.Color;
            light.Intensity = l.Intensity;
            light.Range = l.Range;
            light.CastShadows = l.Shadows;
            light.InnerConeAngle = l.Inner;
            light.OuterConeAngle = l.Outer > 0f ? l.Outer : light.OuterConeAngle;
            scene.Lights.Add(light);
        }
    }

    // Emitters shown this frame become quads in pooled batches, one batch per texture, refilled each frame.
    private void BuildBillboards(Scene3D scene)
    {
        for (var i = 0; i < _billboardPool.Count; i++)
        {
            _billboardPool[i].Clear();
        }

        var emitters = _writer.Emitters;
        for (var e = 0; e < emitters.Count; e++)
        {
            var (emitter, texture) = emitters[e];
            BillboardBatch3D? batch = null;
            for (var i = 0; i < _billboardPool.Count; i++)
            {
                if (ReferenceEquals(_billboardPool[i].Texture, texture))
                {
                    batch = _billboardPool[i];
                    break;
                }
            }

            if (batch is null)
            {
                batch = new BillboardBatch3D { Texture = texture, Name = "particles" };
                _billboardPool.Add(batch);
            }

            if (!scene.Billboards.Contains(batch))
            {
                scene.Billboards.Add(batch);
            }

            var s = emitter.Settings;
            var additive = s.Blend == OneGame.Particles.ParticleBlend.Additive;
            for (var i = 0; i < emitter.Count; i++)
            {
                var p = emitter.Sample(i);
                batch.Add(new Billboard3D
                {
                    Position = p.Position,
                    Size = new Vector2(p.Size),
                    Rotation = p.Rotation,
                    Color = new Vector4(Linear(p.Color.X) * s.Emission, Linear(p.Color.Y) * s.Emission, Linear(p.Color.Z) * s.Emission, p.Color.W),
                    Uv = p.Uv,
                    Additive = additive,
                });
            }
        }
    }

    private static float Linear(float c) => c <= 0.04045f ? c / 12.92f : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);

    /// <summary>Where a world point lands in a target of <paramref name="width"/> x <paramref name="height"/> pixels (origin top left).</summary>
    /// <param name="point">The world position.</param>
    /// <param name="width">Target width in pixels.</param>
    /// <param name="height">Target height in pixels.</param>
    /// <param name="pixel">The position in pixels; it may lie outside the target.</param>
    /// <param name="depth">Distance in front of the camera along its view axis.</param>
    /// <returns>False when the point is behind the camera or beyond its near and far planes, in which case it is not on screen at all.</returns>
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

    /// <summary>The ray through a pixel of a target of the given size.</summary>
    /// <param name="x">Pixels from the left.</param>
    /// <param name="y">Pixels from the top.</param>
    /// <param name="width">Target width in pixels.</param>
    /// <param name="height">Target height in pixels.</param>
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

    /// <summary>The nearest entity model under a pixel, from the last <see cref="Prepare"/>.</summary>
    /// <param name="x">Pixels from the left.</param>
    /// <param name="y">Pixels from the top.</param>
    /// <param name="width">Target width in pixels.</param>
    /// <param name="height">Target height in pixels.</param>
    /// <returns>The hit, or null when the pixel shows no pickable model.</returns>
    public PickHit? Pick(float x, float y, int width, int height) => Cast(Ray(x, y, width, height));

    /// <summary>The nearest entity model along a ray, from the last <see cref="Prepare"/>. Models are tested triangle by triangle at their rest pose, both sides.</summary>
    /// <param name="ray">The ray.</param>
    /// <returns>The hit, or null.</returns>
    public PickHit? Cast(PickRay ray)
    {
        PickHit? best = null;
        var bestT = float.PositiveInfinity;
        var draws = _writer.Draws;
        for (var i = 0; i < draws.Count; i++)
        {
            var d = draws[i];
            if (!d.Pickable || !Models.At(d.Model).Intersect(d.World, ray.Origin, ray.Direction, out var t) || t >= bestT)
            {
                continue;
            }

            bestT = t;
            best = new PickHit(d.Entity, Models.At(d.Model).Id, t, ray.Origin + (ray.Direction * t));
        }

        return best;
    }

    /// <summary>Releases the GPU copies of the models. The renderer must still be alive.</summary>
    public void Dispose() => Models.Dispose();

    private static float Aspect(int width, int height) => height > 0 ? (float)width / height : 1f;

    private static Vector3 Unproject(Matrix4x4 inverse, float x, float y, float z)
    {
        var p = Vector4.Transform(new Vector4(x, y, z, 1f), inverse);
        return new Vector3(p.X, p.Y, p.Z) / p.W;
    }
}
