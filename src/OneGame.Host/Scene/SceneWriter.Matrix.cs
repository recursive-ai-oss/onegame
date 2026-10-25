using System.Numerics;
using OneGame.Core;
using OneGame.Render3D;

namespace OneGame.Host;

// Float forms for view code that places things from smoothed, view-only positions (a describer, see SceneView3D.Describer).
public sealed partial class SceneWriter
{
    /// <summary>Shows a model at a world matrix. For view code that animates with floats; synced transforms use the <see cref="Transform3"/> form.</summary>
    /// <param name="model">The name the model was added under.</param>
    /// <param name="world">Where it is (row vectors, +Y up).</param>
    /// <param name="pickable">Whether <c>pick</c> can hit it.</param>
    /// <exception cref="UnknownModelException">The library has no such model.</exception>
    public void Model(string model, in Matrix4x4 world, bool pickable = true)
        => _draws.Add(new ModelDraw(_models.IndexOf(model), Entity, world, pickable));

    /// <summary>A light that shines in all directions from a position.</summary>
    /// <param name="position">Where it is.</param>
    /// <param name="color">Linear RGB.</param>
    /// <param name="candela">Intensity.</param>
    /// <param name="range">Distance at which it fades out; 0 for none.</param>
    public void PointLight(Vector3 position, Vector3 color, float candela, float range = 0f)
        => _lights.Add(new LightDraw(LightKind.Point, position, default, color, candela, range, 0f, 0f));

    /// <summary>A light from infinitely far away.</summary>
    /// <param name="direction">The way the light travels.</param>
    /// <param name="color">Linear RGB.</param>
    /// <param name="lux">Intensity.</param>
    public void DirectionalLight(Vector3 direction, Vector3 color, float lux)
        => _lights.Add(new LightDraw(LightKind.Directional, default, Vector3.Normalize(direction), color, lux, 0f, 0f, 0f));

    /// <summary>Offers a camera placed by a rigid camera-to-world matrix (it looks down its -Z). Same choice rule as the transform form.</summary>
    /// <param name="world">Camera-to-world matrix.</param>
    /// <param name="fovDegrees">Vertical field of view.</param>
    /// <param name="priority">Higher wins.</param>
    /// <param name="near">Near plane distance.</param>
    /// <param name="far">Far plane distance.</param>
    public void Camera(in Matrix4x4 world, float fovDegrees = 45f, int priority = 0, float near = 0.1f, float far = 1000f)
    {
        if (_hasCamera && priority <= _cameraPriority)
        {
            return;
        }

        _hasCamera = true;
        _cameraPriority = priority;
        _camera = new ViewCamera(world, fovDegrees * Deg, 0f, near, far, Entity);
    }
}
