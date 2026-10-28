namespace OneGame.Host;

/// <summary>
/// Implemented by an entity that shows itself in 3D. The host calls <see cref="Describe3D"/> on every such entity once per drawn frame, in
/// entity order, and builds the scene from what the entity says. Mark the implementation <c>[View]</c> so the analyzers
/// check that it only reads state.
/// </summary>
/// <example>
/// A crate that shows a model, and a rig that offers the camera. The model name is one added to the stage's <see cref="SceneView3D.Models"/>; a game's
/// <c>Render3DStage</c> is created with that <see cref="SceneView3D"/>.
/// <code>
/// [Entity]
/// public partial class Crate : Entity, IView3D
/// {
///     [Synced] public partial Transform3 Transform { get; private set; }
///
///     [View]
///     public void Describe3D(SceneWriter scene)
///     {
///         scene.Model("box", Transform);
///         scene.PointLight(Transform3.At(Transform.Position + new FVec3(0, 2, 0)), new Vector3(1f, 0.8f, 0.5f), candela: 40f, range: 8f);
///     }
/// }
///
/// [Entity]
/// public partial class Rig : Entity, IView3D
/// {
///     [Synced] public partial Transform3 Transform { get; private set; }
///
///     [View]
///     public void Describe3D(SceneWriter scene) =&gt; scene.Camera(Transform, fovDegrees: 40f);
/// }
/// </code>
/// </example>
public interface IView3D
{
    /// <summary>Says what this entity looks like right now: its model, lights and camera. Read state; never change it.</summary>
    /// <param name="scene">Collects the answer. <see cref="SceneWriter.Tick"/> and <see cref="SceneWriter.Alpha"/> say which moment to show, for <c>Timed&lt;T&gt;.ValueAt</c>.</param>
    void Describe3D(SceneWriter scene);
}

/// <summary>A model name that no <see cref="ModelLibrary"/> entry has.</summary>
public sealed class UnknownModelException : InvalidOperationException
{
    /// <summary>Creates the exception.</summary>
    public UnknownModelException(string id, IEnumerable<string> known)
        : base($"No model named '{id}'. Register it with SceneView3D.Models.Add(\"{id}\", gltfModel). Known models: {string.Join(", ", known)}.")
    {
        Id = id;
    }

    /// <summary>The name that was asked for.</summary>
    public string Id { get; }
}
