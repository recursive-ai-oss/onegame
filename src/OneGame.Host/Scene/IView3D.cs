namespace OneGame.Host;

/// <summary>
/// Implemented by an entity that shows itself in 3D. The host calls <see cref="Describe3D"/> on every such entity once per drawn frame, in
/// entity order, and builds the scene from what the entity says. Mark the implementation <c>[View]</c> so the analyzers
/// check that it only reads state.
/// </summary>
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
