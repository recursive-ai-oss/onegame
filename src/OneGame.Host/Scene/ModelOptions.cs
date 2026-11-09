namespace OneGame.Host;

/// <summary>Per-draw settings for <c>SceneWriter.Model</c> beyond placement: how see-through the model is, and the name tools show for it.</summary>
public readonly record struct ModelOptions
{
    /// <summary>Settings that change nothing: fully opaque, no label, pickable.</summary>
    public ModelOptions()
    {
    }

    /// <summary>
    /// 1 (default) is solid; below 1 the whole model is multiplied by this alpha and drawn in the blended pass (far to near, no depth write, no
    /// shadow), as for <c>ModelInstance3D.Opacity</c>. At 0 nothing is drawn.
    /// </summary>
    public float Opacity { get; init; } = 1f;

    /// <summary>A name for tools; it is the instance's <c>ModelInstance3D.Label</c>. Null leaves it empty.</summary>
    public string? Label { get; init; }

    /// <summary>Whether <c>pick</c> can hit the model (meshes made at run time are never pickable).</summary>
    public bool Pickable { get; init; } = true;
}
