namespace OneGame.Gpu.Graph;

/// <summary>What a pass may do while it records: get the physical textures and buffers it declared, and the render or compute pass the graph opened for it.</summary>
public sealed class PassContext
{
    readonly RenderGraph _graph;
    RenderGraph.PassInfo? _pass;
    GpuCommandEncoder? _encoder;
    GpuRenderPass? _render;
    GpuComputePass? _compute;

    internal PassContext(RenderGraph graph, GpuDevice device)
    {
        _graph = graph;
        Device = device;
    }

    /// <summary>The device.</summary>
    public GpuDevice Device { get; }

    /// <summary>Name of the running pass.</summary>
    public string PassName => _pass?.Name ?? "";

    /// <summary>Number of the frame being recorded.</summary>
    public int Frame => _graph.Frame;

    /// <summary>
    /// The encoder everything records into. Use it directly in a custom pass (copies, mip generation, or a renderer that opens its own render
    /// passes). In a raster or compute pass the graph has a pass open already; use <see cref="Render"/> or <see cref="Compute"/>.
    /// </summary>
    public GpuCommandEncoder Encoder => _encoder ?? throw new InvalidOperationException("no pass is running");

    /// <summary>The render pass the graph opened for a pass that declared targets. Do not end it; the graph does.</summary>
    public GpuRenderPass Render => _render ?? throw new InvalidOperationException($"pass '{PassName}' declared no render targets, so the graph opened no render pass");

    /// <summary>The compute pass the graph opened for a pass that called <see cref="PassBuilder.Compute"/>. Do not end it.</summary>
    public GpuComputePass Compute => _compute ?? throw new InvalidOperationException($"pass '{PassName}' did not call Compute(), so the graph opened no compute pass");

    /// <summary>The texture behind a handle. The pass must have declared it.</summary>
    /// <exception cref="RenderGraphException">The handle is stale, or this pass did not declare it.</exception>
    public GpuTexture Texture(GraphTexture handle)
    {
        var index = _graph.Resolve(handle);
        CheckDeclared(index);
        return _graph.Get(index).Texture ?? throw new RenderGraphException($"texture '{_graph.Get(index).Name}' has no memory: nothing that runs uses it");
    }

    /// <summary>The buffer behind a handle. The pass must have declared it.</summary>
    /// <exception cref="RenderGraphException">The handle is stale, or this pass did not declare it.</exception>
    public GpuBuffer Buffer(GraphBuffer handle)
    {
        var index = _graph.Resolve(handle);
        CheckDeclared(index);
        return _graph.Get(index).Buffer ?? throw new RenderGraphException($"buffer '{_graph.Get(index).Name}' has no memory: nothing that runs uses it");
    }

    void CheckDeclared(int index)
    {
        if (_pass is null)
        {
            throw new InvalidOperationException("no pass is running");
        }

        foreach (var a in _pass.Accesses)
        {
            if (a.Resource == index)
            {
                return;
            }
        }

        throw new RenderGraphException($"pass '{_pass.Name}' uses '{_graph.Get(index).Name}' without declaring it in Setup, so the graph could not order or cull around it");
    }

    internal void Begin(RenderGraph.PassInfo pass, GpuCommandEncoder encoder)
    {
        _pass = pass;
        _encoder = encoder;
        _render = null;
        _compute = null;
    }

    internal void SetRender(GpuRenderPass pass) => _render = pass;

    internal void SetCompute(GpuComputePass pass) => _compute = pass;

    internal void End()
    {
        _pass = null;
        _encoder = null;
        _render = null;
        _compute = null;
    }
}

/// <summary>Declares what one pass touches. Only valid inside <see cref="IGraphPass.Setup"/> (or the setup delegate).</summary>
public sealed class PassBuilder
{
    readonly RenderGraph _graph;
    RenderGraph.PassInfo? _pass;

    internal PassBuilder(RenderGraph graph) => _graph = graph;

    internal void Attach(RenderGraph.PassInfo pass) => _pass = pass;

    internal void Detach() => _pass = null;

    RenderGraph.PassInfo Pass => _pass ?? throw new InvalidOperationException("a PassBuilder can only be used while a pass is being set up");

    // Rendering into one mip level or layer leaves the rest of the texture as it was, so earlier writers stay needed.
    bool IsPartial(int resource)
    {
        var d = _graph.Get(resource).TextureDesc;
        return d.MipLevels > 1 || d.ArrayLayers > 1;
    }

    /// <summary>Marks the pass as one that must run even if nothing reads its results (readback, presentation, a debug overlay).</summary>
    public void SideEffect() => Pass.SideEffect = true;

    /// <summary>Makes this a compute pass: the graph opens a compute pass and hands it over as <see cref="PassContext.Compute"/>.</summary>
    public void Compute() => Pass.ComputeFlag = true;

    /// <summary>The pass samples the texture in a shader.</summary>
    public void Read(GraphTexture texture) =>
        _graph.AddAccess(Pass, _graph.Resolve(texture), RenderGraph.AccessKind.Sampled, writes: false, needsContent: true, preserves: true);

    /// <summary>The pass reads the texture as a storage image.</summary>
    public void ReadStorage(GraphTexture texture) =>
        _graph.AddAccess(Pass, _graph.Resolve(texture), RenderGraph.AccessKind.StorageRead, writes: false, needsContent: true, preserves: true);

    /// <summary>The pass writes the texture as a storage image. What was there before is kept for passes that only write part of it.</summary>
    public void WriteStorage(GraphTexture texture) =>
        _graph.AddAccess(Pass, _graph.Resolve(texture), RenderGraph.AccessKind.StorageWrite, writes: true, needsContent: false, preserves: true);

    /// <summary>The pass reads the buffer as a storage buffer.</summary>
    public void ReadStorage(GraphBuffer buffer) =>
        _graph.AddAccess(Pass, _graph.Resolve(buffer), RenderGraph.AccessKind.StorageRead, writes: false, needsContent: true, preserves: true);

    /// <summary>The pass writes the buffer as a storage buffer.</summary>
    public void WriteStorage(GraphBuffer buffer) =>
        _graph.AddAccess(Pass, _graph.Resolve(buffer), RenderGraph.AccessKind.StorageWrite, writes: true, needsContent: false, preserves: true);

    /// <summary>The pass reads the buffer in the given way.</summary>
    public void Read(GraphBuffer buffer, BufferRead how)
    {
        var kind = how switch
        {
            BufferRead.Uniform => RenderGraph.AccessKind.Uniform,
            BufferRead.Storage => RenderGraph.AccessKind.StorageRead,
            BufferRead.Vertex => RenderGraph.AccessKind.Vertex,
            BufferRead.Index => RenderGraph.AccessKind.Index,
            BufferRead.Indirect => RenderGraph.AccessKind.Indirect,
            _ => RenderGraph.AccessKind.CopySource,
        };
        _graph.AddAccess(Pass, _graph.Resolve(buffer), kind, writes: false, needsContent: true, preserves: true);
    }

    /// <summary>The pass copies from the texture.</summary>
    public void CopySource(GraphTexture texture) =>
        _graph.AddAccess(Pass, _graph.Resolve(texture), RenderGraph.AccessKind.CopySource, writes: false, needsContent: true, preserves: true);

    /// <summary>The pass copies into the texture (all of it, or part: what was there before is kept).</summary>
    public void CopyDestination(GraphTexture texture) =>
        _graph.AddAccess(Pass, _graph.Resolve(texture), RenderGraph.AccessKind.CopyDestination, writes: true, needsContent: false, preserves: true);

    /// <summary>The pass copies into the buffer.</summary>
    public void CopyDestination(GraphBuffer buffer) =>
        _graph.AddAccess(Pass, _graph.Resolve(buffer), RenderGraph.AccessKind.CopyDestination, writes: true, needsContent: false, preserves: true);

    /// <summary>
    /// A custom pass writes every texel of the texture without reading it (for example, the 2D renderer clears the target, then draws).
    /// Everything written before is dead.
    /// </summary>
    public void Write(GraphTexture texture) =>
        _graph.AddAccess(Pass, _graph.Resolve(texture), RenderGraph.AccessKind.Write, writes: true, needsContent: false, preserves: false);

    /// <summary>A custom pass reads and modifies the texture (draws over it, generates its mips).</summary>
    public void ReadWrite(GraphTexture texture) =>
        _graph.AddAccess(Pass, _graph.Resolve(texture), RenderGraph.AccessKind.ReadWrite, writes: true, needsContent: true, preserves: true);

    /// <summary>A custom pass reads and modifies the buffer.</summary>
    public void ReadWrite(GraphBuffer buffer) =>
        _graph.AddAccess(Pass, _graph.Resolve(buffer), RenderGraph.AccessKind.ReadWrite, writes: true, needsContent: true, preserves: true);

    /// <summary>
    /// Renders into a color target (slot order is declaration order, up to 8). With <see cref="LoadOp.Load"/> the pass also reads the target, so
    /// an earlier pass must have written it.
    /// </summary>
    /// <param name="target">The texture.</param>
    /// <param name="load">What happens to its contents when the pass starts.</param>
    /// <param name="r">Clear red.</param>
    /// <param name="g">Clear green.</param>
    /// <param name="b">Clear blue.</param>
    /// <param name="a">Clear alpha.</param>
    /// <param name="store">Whether the result is kept; <see cref="StoreOp.Discard"/> is for multisampled targets that are only resolved.</param>
    /// <param name="mip">Mip level rendered into.</param>
    /// <param name="layer">Array layer or cube face rendered into.</param>
    /// <param name="resolve">A single-sample texture that receives the resolved image of a multisampled <paramref name="target"/>.</param>
    public void ColorTarget(GraphTexture target, LoadOp load = LoadOp.Clear, float r = 0, float g = 0, float b = 0, float a = 1, StoreOp store = StoreOp.Store, uint mip = 0, uint layer = 0, GraphTexture resolve = default)
    {
        var pass = Pass;
        if (pass.ColorCount == 8)
        {
            throw new RenderGraphException($"pass '{pass.Name}' has more than 8 color targets");
        }

        var res = _graph.Resolve(target);
        var load2 = load == LoadOp.Load;
        _graph.AddAccess(pass, res, RenderGraph.AccessKind.ColorTarget, writes: true, needsContent: load2, preserves: load2 || IsPartial(res), mip, layer);
        var resolveIndex = -1;
        if (resolve.IsValid)
        {
            resolveIndex = _graph.Resolve(resolve);
            _graph.AddAccess(pass, resolveIndex, RenderGraph.AccessKind.ResolveTarget, writes: true, needsContent: false, preserves: false, mip, layer);
        }

        pass.Colors[pass.ColorCount++] = new RenderGraph.ColorDecl { Resource = res, Resolve = resolveIndex, Mip = mip, Layer = layer, Load = load, Store = store, R = r, G = g, B = b, A = a };
    }

    /// <summary>Renders with a depth target (one per pass). With <see cref="LoadOp.Load"/> the pass also reads it.</summary>
    /// <param name="target">A depth texture.</param>
    /// <param name="load">What happens to its contents when the pass starts.</param>
    /// <param name="clearDepth">Depth to clear to.</param>
    /// <param name="store">Whether the result is kept (shadow maps keep it; a scene's depth buffer may discard it).</param>
    /// <param name="mip">Mip level rendered into.</param>
    /// <param name="layer">Array layer rendered into (a shadow cascade).</param>
    public void DepthTarget(GraphTexture target, LoadOp load = LoadOp.Clear, float clearDepth = 1f, StoreOp store = StoreOp.Store, uint mip = 0, uint layer = 0)
    {
        var pass = Pass;
        if (pass.HasDepth)
        {
            throw new RenderGraphException($"pass '{pass.Name}' has two depth targets");
        }

        var res = _graph.Resolve(target);
        var keep = load == LoadOp.Load;
        _graph.AddAccess(pass, res, RenderGraph.AccessKind.DepthTarget, writes: true, needsContent: keep, preserves: keep || IsPartial(res), mip, layer);
        pass.HasDepth = true;
        pass.DepthResource = res;
        pass.DepthMip = mip;
        pass.DepthLayer = layer;
        pass.DepthLoad = load;
        pass.DepthStore = store;
        pass.ClearDepth = clearDepth;
    }
}
