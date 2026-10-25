namespace OneGame.Gpu.Graph;

/// <summary>A texture inside one frame of a <see cref="RenderGraph"/>: either created by the graph (transient) or imported. Valid only for the frame that made it.</summary>
public readonly struct GraphTexture : IEquatable<GraphTexture>
{
    internal GraphTexture(int frame, int index)
    {
        Frame = frame;
        Index = index;
    }

    internal int Frame { get; }

    internal int Index { get; }

    /// <summary>False for <c>default</c>.</summary>
    public bool IsValid => Frame != 0;

    /// <inheritdoc />
    public bool Equals(GraphTexture other) => Frame == other.Frame && Index == other.Index;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is GraphTexture other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Frame, Index);

    /// <summary>Equality.</summary>
    public static bool operator ==(GraphTexture left, GraphTexture right) => left.Equals(right);

    /// <summary>Inequality.</summary>
    public static bool operator !=(GraphTexture left, GraphTexture right) => !left.Equals(right);
}

/// <summary>A buffer inside one frame of a <see cref="RenderGraph"/>. Valid only for the frame that made it.</summary>
public readonly struct GraphBuffer : IEquatable<GraphBuffer>
{
    internal GraphBuffer(int frame, int index)
    {
        Frame = frame;
        Index = index;
    }

    internal int Frame { get; }

    internal int Index { get; }

    /// <summary>False for <c>default</c>.</summary>
    public bool IsValid => Frame != 0;

    /// <inheritdoc />
    public bool Equals(GraphBuffer other) => Frame == other.Frame && Index == other.Index;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is GraphBuffer other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Frame, Index);

    /// <summary>Equality.</summary>
    public static bool operator ==(GraphBuffer left, GraphBuffer right) => left.Equals(right);

    /// <summary>Inequality.</summary>
    public static bool operator !=(GraphBuffer left, GraphBuffer right) => !left.Equals(right);
}

/// <summary>How a pass reads a buffer.</summary>
public enum BufferRead
{
    /// <summary>As a constant (uniform) buffer.</summary>
    Uniform,
    /// <summary>As a storage buffer it only reads.</summary>
    Storage,
    /// <summary>As vertex data.</summary>
    Vertex,
    /// <summary>As index data.</summary>
    Index,
    /// <summary>As indirect draw or dispatch arguments.</summary>
    Indirect,
    /// <summary>As the source of a copy.</summary>
    CopySource,
}

/// <summary>What kind of work a pass records, which decides how the graph runs it.</summary>
public enum PassType
{
    /// <summary>The pass declared color or depth targets; the graph opens the render pass and the pass draws in it.</summary>
    Raster,
    /// <summary>The pass called <see cref="PassBuilder.Compute"/>; the graph opens a compute pass.</summary>
    Compute,
    /// <summary>The pass records on the encoder itself: copies, mip generation, or code that opens its own passes (such as the 2D renderer).</summary>
    Custom,
}

/// <summary>A pass of the render graph. Keep one instance per pass and reuse it every frame: a graph built from such objects allocates nothing per frame.</summary>
public interface IGraphPass
{
    /// <summary>Name shown in reports and timings.</summary>
    string Name { get; }

    /// <summary>Declares what the pass reads, writes and renders into. Runs every frame while the graph is built, before anything executes.</summary>
    void Setup(PassBuilder builder);

    /// <summary>Records the pass's commands. Runs only if the pass was not culled.</summary>
    void Execute(PassContext context);
}

/// <summary>A problem in how a graph was declared: reading something no pass wrote, using a stale handle, touching a resource a pass did not declare.</summary>
public sealed class RenderGraphException : Exception
{
    /// <summary>Creates the exception.</summary>
    public RenderGraphException(string message)
        : base(message)
    {
    }
}
