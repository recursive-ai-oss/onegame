namespace OneGame.Gpu.Graph;

/// <summary>Tuning for a <see cref="RenderGraph"/>.</summary>
public sealed class RenderGraphOptions
{
    /// <summary>Measure GPU time per pass when the device can (<see cref="GpuCapabilities.Timestamps"/>). On by default.</summary>
    public bool Timing { get; init; } = true;

    /// <summary>How many frames a pooled texture or buffer may sit unused before it is released. Default 8.</summary>
    public int KeepUnusedFrames { get; init; } = 8;
}

/// <summary>
/// One frame structure for every renderer. Each frame: <see cref="BeginFrame"/>, create or import resources, add passes that declare what
/// they read and write, then <see cref="Execute"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>What the graph does with the declarations.</b> It checks them (a pass may not read what no earlier pass wrote), culls passes whose
/// results nobody uses (a pass survives if it is marked <see cref="PassBuilder.SideEffect"/>, or writes an imported resource that nothing
/// later overwrites, or writes something a surviving pass reads), allocates the transient textures and buffers, and records the
/// survivors into one encoder in declaration order. Declaration order is dependency order: a pass can only name resources that exist, and
/// reading a resource means reading what the passes declared before it wrote.
/// </para>
/// <para>
/// <b>Aliasing.</b> Transient resources whose lifetimes do not overlap and whose shapes (size, format, mips, layers, samples) match share one
/// physical texture or buffer. Physical resources stay in a pool between frames, so a steady graph creates nothing after its first frames.
/// </para>
/// <para>
/// <b>Barriers and layouts</b> are the backend's job: it tracks every resource's last use and inserts what the order of the recorded passes
/// needs, so the graph only has to put passes in a valid order and give reused memory a defined first write.
/// </para>
/// <para>
/// <b>Allocation.</b> Pass objects that implement <see cref="IGraphPass"/> and are kept between frames make a graph that allocates nothing per
/// frame once its pools have warmed up. Lambdas that capture variables allocate every frame.
/// </para>
/// </remarks>
public sealed class RenderGraph : IDisposable
{
    const int MaxColorTargets = 8;
    const int TimingRing = GpuDevice.MaxFramesInFlight + 1;
    const uint TimingQueriesPerFrame = 1024;
    const uint CustomPassTimingPairs = 8;

    internal enum AccessKind : byte
    {
        Sampled,
        StorageRead,
        StorageWrite,
        ColorTarget,
        DepthTarget,
        ResolveTarget,
        CopySource,
        CopyDestination,
        Uniform,
        Vertex,
        Index,
        Indirect,
        Write,
        ReadWrite,
    }

    internal struct Access
    {
        public int Resource;
        public AccessKind Kind;
        public bool Writes;
        public bool NeedsContent;
        public bool Preserves;
        public uint Mip;
        public uint Layer;
    }

    internal struct ColorDecl
    {
        public int Resource;
        public int Resolve;
        public uint Mip;
        public uint Layer;
        public LoadOp Load;
        public StoreOp Store;
        public float R, G, B, A;
    }

    internal sealed class PassInfo
    {
        public string Name = "";
        public IGraphPass? Pass;
        public Action<PassContext>? Execute;
        public bool SideEffect;
        public bool ComputeFlag;
        public bool Live;
        public int ExecIndex = -1;
        public PassType Type;
        public readonly List<Access> Accesses = [];
        public readonly ColorDecl[] Colors = new ColorDecl[MaxColorTargets];
        public int ColorCount;
        public bool HasDepth;
        public int DepthResource;
        public uint DepthMip;
        public uint DepthLayer;
        public LoadOp DepthLoad;
        public StoreOp DepthStore;
        public float ClearDepth;

        public void Reset()
        {
            Name = "";
            Pass = null;
            Execute = null;
            SideEffect = false;
            ComputeFlag = false;
            Live = false;
            ExecIndex = -1;
            Type = PassType.Custom;
            Accesses.Clear();
            ColorCount = 0;
            HasDepth = false;
        }
    }

    internal sealed class Resource
    {
        public string Name = "";
        public bool IsTexture;
        public bool Imported;
        public TextureDesc TextureDesc;
        public BufferDesc BufferDesc;
        public GpuTexture? Texture;
        public GpuBuffer? Buffer;
        public bool Written;
        public bool Live;
        public int First;
        public int Last;
        public int Slot;
        public TextureUsage TextureUsage;
        public BufferUsage BufferUsage;
        public int LastWriter;
    }

    sealed class Slot
    {
        public bool IsTexture;
        public TextureDesc TextureDesc;
        public BufferDesc BufferDesc;
        public int End;
        public int Count;
        public TextureUsage TextureUsage;
        public BufferUsage BufferUsage;
        public string Name = "";
        public GpuTexture? Texture;
        public GpuBuffer? Buffer;
        public bool Reused;
    }

    sealed class PooledTexture
    {
        public GpuTexture Texture = null!;
        public TextureDesc Desc;
        public bool Taken;
        public int LastUsed;
    }

    sealed class PooledBuffer
    {
        public GpuBuffer Buffer = null!;
        public BufferDesc Desc;
        public bool Taken;
        public int LastUsed;
    }

    sealed class FrameTiming
    {
        public bool Pending;
        public int Frame;
        public int PassCount;
        public string[] Names = new string[16];
        public uint[] First = new uint[16];
        public uint[] Pairs = new uint[16];
        public ulong[] Raw = new ulong[2 * (int)CustomPassTimingPairs];
        public ulong[] Nanos = new ulong[16];
        public bool Resolved;
    }

    readonly GpuDevice _device;
    readonly RenderGraphOptions _options;
    readonly List<PassInfo> _passes = [];
    readonly List<Resource> _resources = [];
    readonly List<Slot> _slots = [];
    readonly List<PooledTexture> _texturePool = [];
    readonly List<PooledBuffer> _bufferPool = [];
    readonly PassBuilder _builder;
    readonly PassContext _context;
    readonly List<PassInfo> _order = [];
    readonly ColorAttachment[] _attachments = new ColorAttachment[MaxColorTargets];
    readonly GpuTimestampSet?[] _timestampSets = new GpuTimestampSet?[TimingRing];
    readonly FrameTiming[] _timings = new FrameTiming[TimingRing];
    int _passCount;
    int _resourceCount;
    int _frame;
    bool _building;
    bool _executed;
    GraphStats _stats;
    int _latestResolved = -1;
    bool _disposed;

    /// <summary>Creates a graph that records on <paramref name="device"/>.</summary>
    public RenderGraph(GpuDevice device, RenderGraphOptions? options = null)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
        _options = options ?? new RenderGraphOptions();
        _builder = new PassBuilder(this);
        _context = new PassContext(this, device);
        for (var i = 0; i < TimingRing; i++)
        {
            _timings[i] = new FrameTiming();
        }

        if (_options.Timing && device.Capabilities.Timestamps)
        {
            for (var i = 0; i < TimingRing; i++)
            {
                _timestampSets[i] = device.CreateTimestampSet(TimingQueriesPerFrame);
            }
        }
    }

    /// <summary>The device the graph records on.</summary>
    public GpuDevice Device => _device;

    /// <summary>Number of the current (or last) frame, counting from 1.</summary>
    public int Frame => _frame;

    /// <summary>Whether per-pass GPU time is being measured.</summary>
    public bool TimingEnabled => _timestampSets[0] is not null;

    /// <summary>Counts from the last executed frame.</summary>
    public GraphStats Stats => _stats;

    /// <summary>Starts a frame. Handles from earlier frames stop being valid.</summary>
    public void BeginFrame()
    {
        ThrowIfDisposed();
        if (_building)
        {
            throw new InvalidOperationException("the previous frame was never executed; call Execute (or Cancel) first");
        }

        _frame++;
        _building = true;
        _executed = false;
        _passCount = 0;
        _resourceCount = 0;
        _order.Clear();
        _stats = default;
    }

    /// <summary>Drops the frame being built without recording anything.</summary>
    public void Cancel()
    {
        _building = false;
        _builder.Detach();
    }

    // ---- resources ----

    /// <summary>Declares a texture the graph allocates for this frame. It may share memory with other transients whose use does not overlap. Its contents start undefined: a pass must write it before any pass reads it.</summary>
    public GraphTexture CreateTexture(string name, in TextureDesc desc)
    {
        EnsureBuilding();
        var r = NewResource(name, true);
        r.TextureDesc = desc with { Label = null };
        return new GraphTexture(_frame, _resourceCount - 1);
    }

    /// <summary>Brings a texture the graph does not own into this frame: a swapchain image, a capture target, a persistent shadow atlas. Its contents are defined; the graph never frees it.</summary>
    public GraphTexture ImportTexture(string name, GpuTexture texture)
    {
        ArgumentNullException.ThrowIfNull(texture);
        EnsureBuilding();
        var r = NewResource(name, true);
        r.Imported = true;
        r.Texture = texture;
        r.TextureDesc = new TextureDesc(texture.Width, texture.Height, texture.Format, texture.Usage)
        {
            MipLevels = texture.MipLevels,
            ArrayLayers = texture.ArrayLayers,
            SampleCount = texture.SampleCount,
            Dimension = texture.Dimension,
            Depth = texture.Depth,
        };
        return new GraphTexture(_frame, _resourceCount - 1);
    }

    /// <summary>Declares a buffer the graph allocates for this frame. See <see cref="CreateTexture"/>.</summary>
    public GraphBuffer CreateBuffer(string name, ulong size)
    {
        EnsureBuilding();
        var r = NewResource(name, false);
        r.BufferDesc = new BufferDesc(size, BufferUsage.None);
        return new GraphBuffer(_frame, _resourceCount - 1);
    }

    /// <summary>Brings a buffer the graph does not own into this frame.</summary>
    public GraphBuffer ImportBuffer(string name, GpuBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        EnsureBuilding();
        var r = NewResource(name, false);
        r.Imported = true;
        r.Buffer = buffer;
        r.BufferDesc = new BufferDesc(buffer.Size, buffer.Usage);
        return new GraphBuffer(_frame, _resourceCount - 1);
    }

    Resource NewResource(string name, bool texture)
    {
        if (_resourceCount == _resources.Count)
        {
            _resources.Add(new Resource());
        }

        var r = _resources[_resourceCount++];
        r.Name = name;
        r.IsTexture = texture;
        r.Imported = false;
        r.Texture = null;
        r.Buffer = null;
        r.Written = false;
        r.Live = false;
        r.First = -1;
        r.Last = -1;
        r.Slot = -1;
        r.TextureUsage = TextureUsage.None;
        r.BufferUsage = BufferUsage.None;
        r.LastWriter = -1;
        return r;
    }

    internal Resource Get(int index) => _resources[index];

    internal int Resolve(GraphTexture t)
    {
        if (t.Frame != _frame || t.Index >= _resourceCount || !t.IsValid)
        {
            throw new RenderGraphException("this texture handle belongs to another frame; handles are valid for one frame only");
        }

        return t.Index;
    }

    internal int Resolve(GraphBuffer b)
    {
        if (b.Frame != _frame || b.Index >= _resourceCount || !b.IsValid)
        {
            throw new RenderGraphException("this buffer handle belongs to another frame; handles are valid for one frame only");
        }

        return b.Index;
    }

    // ---- passes ----

    /// <summary>Adds a pass: calls its <see cref="IGraphPass.Setup"/> now to learn what it touches, and its <see cref="IGraphPass.Execute"/> later if it survives culling.</summary>
    public void AddPass(IGraphPass pass)
    {
        ArgumentNullException.ThrowIfNull(pass);
        var info = BeginPass(pass.Name);
        info.Pass = pass;
        pass.Setup(_builder);
        EndSetup(info);
    }

    /// <summary>Adds a pass from two delegates. Cache them (static lambdas) to avoid a per-frame allocation.</summary>
    public void AddPass(string name, Action<PassBuilder> setup, Action<PassContext> execute)
    {
        ArgumentNullException.ThrowIfNull(setup);
        ArgumentNullException.ThrowIfNull(execute);
        var info = BeginPass(name);
        info.Execute = execute;
        setup(_builder);
        EndSetup(info);
    }

    PassInfo BeginPass(string name)
    {
        EnsureBuilding();
        if (_passCount == _passes.Count)
        {
            _passes.Add(new PassInfo());
        }

        var info = _passes[_passCount++];
        info.Reset();
        info.Name = name;
        _builder.Attach(info);
        return info;
    }

    void EndSetup(PassInfo info)
    {
        _builder.Detach();
        info.Type = info.ColorCount > 0 || info.HasDepth ? PassType.Raster : info.ComputeFlag ? PassType.Compute : PassType.Custom;
        if (info.ComputeFlag && info.Type == PassType.Raster)
        {
            throw new RenderGraphException($"pass '{info.Name}' declares both render targets and Compute()");
        }
    }

    internal void AddAccess(PassInfo pass, int resource, AccessKind kind, bool writes, bool needsContent, bool preserves, uint mip = 0, uint layer = 0)
    {
        // A resource that is written cannot also be read in the same pass: the backend gives it one state at a time.
        foreach (var a in pass.Accesses)
        {
            if (a.Resource == resource && (a.Writes || writes))
            {
                throw new RenderGraphException($"pass '{pass.Name}' uses '{_resources[resource].Name}' twice, once as a write: split it into two passes or use two textures");
            }
        }

        pass.Accesses.Add(new Access { Resource = resource, Kind = kind, Writes = writes, NeedsContent = needsContent, Preserves = preserves, Mip = mip, Layer = layer });
    }

    void EnsureBuilding()
    {
        ThrowIfDisposed();
        if (!_building)
        {
            throw new InvalidOperationException("call BeginFrame first");
        }
    }

    void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    // ---- compile ----

    void Validate()
    {
        for (var i = 0; i < _passCount; i++)
        {
            var p = _passes[i];
            foreach (var a in p.Accesses)
            {
                var r = _resources[a.Resource];
                if (a.NeedsContent && !r.Imported && !r.Written)
                {
                    throw new RenderGraphException($"pass '{p.Name}' reads '{r.Name}' but no earlier pass writes it");
                }
            }

            foreach (var a in p.Accesses)
            {
                if (a.Writes)
                {
                    _resources[a.Resource].Written = true;
                }
            }
        }
    }

    void Cull()
    {
        for (var i = 0; i < _resourceCount; i++)
        {
            _resources[i].Live = false;
        }

        // The last writer of an imported resource is a root: someone outside the graph will look at it.
        for (var i = 0; i < _passCount; i++)
        {
            foreach (var a in _passes[i].Accesses)
            {
                if (a.Writes && _resources[a.Resource].Imported)
                {
                    _resources[a.Resource].Live = true;
                }
            }
        }

        for (var i = _passCount - 1; i >= 0; i--)
        {
            var p = _passes[i];
            var live = p.SideEffect;
            foreach (var a in p.Accesses)
            {
                if (a.Writes && _resources[a.Resource].Live)
                {
                    live = true;
                    break;
                }
            }

            p.Live = live;
            if (!live)
            {
                continue;
            }

            // A pass that overwrites a resource completely makes everything written before it dead, unless it also reads what was there.
            foreach (var a in p.Accesses)
            {
                if (a.Writes && !a.Preserves)
                {
                    _resources[a.Resource].Live = false;
                }
            }

            foreach (var a in p.Accesses)
            {
                if (!a.Writes || a.Preserves)
                {
                    _resources[a.Resource].Live = true;
                }
            }
        }

        var exec = 0;
        for (var i = 0; i < _passCount; i++)
        {
            var p = _passes[i];
            if (p.Live)
            {
                p.ExecIndex = exec++;
                _order.Add(p);
            }
        }

        _stats.Passes = _passCount;
        _stats.LivePasses = exec;
        _stats.CulledPasses = _passCount - exec;
    }

    static bool SameShape(in TextureDesc a, in TextureDesc b) =>
        a.Width == b.Width && a.Height == b.Height && a.Format == b.Format && a.MipLevels == b.MipLevels && a.ArrayLayers == b.ArrayLayers
        && a.SampleCount == b.SampleCount && a.Dimension == b.Dimension && a.Depth == b.Depth;

    static TextureUsage UsageOf(AccessKind k) => k switch
    {
        AccessKind.Sampled => TextureUsage.Sampled,
        AccessKind.StorageRead or AccessKind.StorageWrite => TextureUsage.Storage,
        AccessKind.ColorTarget or AccessKind.DepthTarget or AccessKind.ResolveTarget => TextureUsage.RenderTarget,
        AccessKind.CopySource => TextureUsage.CopySource,
        AccessKind.CopyDestination => TextureUsage.CopyDestination,
        _ => TextureUsage.None,
    };

    static BufferUsage BufferUsageOf(AccessKind k) => k switch
    {
        AccessKind.Uniform => BufferUsage.Uniform,
        AccessKind.StorageRead or AccessKind.StorageWrite => BufferUsage.Storage,
        AccessKind.Vertex => BufferUsage.Vertex,
        AccessKind.Index => BufferUsage.Index,
        AccessKind.Indirect => BufferUsage.Indirect,
        AccessKind.CopySource => BufferUsage.CopySource,
        AccessKind.CopyDestination => BufferUsage.CopyDestination,
        _ => BufferUsage.None,
    };

    void Allocate()
    {
        // Lifetimes: first and last surviving pass that touches each resource.
        foreach (var p in _order)
        {
            foreach (var a in p.Accesses)
            {
                var r = _resources[a.Resource];
                if (r.First < 0)
                {
                    r.First = p.ExecIndex;
                }

                r.Last = p.ExecIndex;
                if (r.IsTexture)
                {
                    r.TextureUsage |= UsageOf(a.Kind);
                }
                else
                {
                    r.BufferUsage |= BufferUsageOf(a.Kind);
                }
            }
        }

        foreach (var t in _texturePool)
        {
            t.Taken = false;
        }

        foreach (var b in _bufferPool)
        {
            b.Taken = false;
        }

        var slotCount = 0;
        for (var s = 0; s < _slots.Count; s++)
        {
            _slots[s].Count = 0;
        }

        // Resources are visited in order of first use, so a slot whose current occupant ended before this resource starts is free.
        foreach (var p in _order)
        {
            foreach (var a in p.Accesses)
            {
                var r = _resources[a.Resource];
                if (r.Imported || r.Slot >= 0 || r.First != p.ExecIndex)
                {
                    continue;
                }

                var found = -1;
                for (var s = 0; s < slotCount; s++)
                {
                    var slot = _slots[s];
                    if (slot.IsTexture == r.IsTexture && slot.End < r.First && (r.IsTexture ? SameShape(slot.TextureDesc, r.TextureDesc) : slot.BufferDesc.Size == r.BufferDesc.Size))
                    {
                        found = s;
                        break;
                    }
                }

                if (found < 0)
                {
                    if (slotCount == _slots.Count)
                    {
                        _slots.Add(new Slot());
                    }

                    found = slotCount++;
                    var fresh = _slots[found];
                    fresh.IsTexture = r.IsTexture;
                    fresh.TextureDesc = r.TextureDesc;
                    fresh.BufferDesc = r.BufferDesc;
                    fresh.TextureUsage = r.IsTexture ? r.TextureDesc.Usage : TextureUsage.None;
                    fresh.BufferUsage = r.IsTexture ? BufferUsage.None : r.BufferDesc.Usage;
                    fresh.Name = r.Name;
                    fresh.Texture = null;
                    fresh.Buffer = null;
                    fresh.Reused = false;
                    fresh.Count = 0;
                }

                var sl = _slots[found];
                sl.End = r.Last;
                sl.Count++;
                sl.TextureUsage |= r.TextureUsage | (r.IsTexture ? r.TextureDesc.Usage : TextureUsage.None);
                sl.BufferUsage |= r.BufferUsage | (r.IsTexture ? BufferUsage.None : r.BufferDesc.Usage);
                r.Slot = found;
            }
        }

        // Physical memory for each slot: from last frame's pool when an identical one is free, otherwise new.
        ulong bytes = 0;
        ulong unaliased = 0;
        var created = 0;
        var reused = 0;
        var textures = 0;
        var buffers = 0;
        for (var s = 0; s < slotCount; s++)
        {
            var slot = _slots[s];
            if (slot.IsTexture)
            {
                var desc = slot.TextureDesc with { Usage = slot.TextureUsage, Label = null };
                PooledTexture? hit = null;
                foreach (var t in _texturePool)
                {
                    if (!t.Taken && t.Desc == desc)
                    {
                        hit = t;
                        break;
                    }
                }

                if (hit is null)
                {
                    hit = new PooledTexture { Texture = _device.CreateTexture(desc with { Label = slot.Name }), Desc = desc };
                    _texturePool.Add(hit);
                    created++;
                }
                else
                {
                    reused++;
                    slot.Reused = true;
                }

                hit.Taken = true;
                hit.LastUsed = _frame;
                slot.Texture = hit.Texture;
                bytes += TextureBytes(desc);
                textures++;
            }
            else
            {
                var desc = new BufferDesc(slot.BufferDesc.Size, slot.BufferUsage);
                PooledBuffer? hit = null;
                foreach (var b in _bufferPool)
                {
                    if (!b.Taken && b.Desc == desc)
                    {
                        hit = b;
                        break;
                    }
                }

                if (hit is null)
                {
                    hit = new PooledBuffer { Buffer = _device.CreateBuffer(desc with { Label = slot.Name }), Desc = desc };
                    _bufferPool.Add(hit);
                    created++;
                }
                else
                {
                    reused++;
                    slot.Reused = true;
                }

                hit.Taken = true;
                hit.LastUsed = _frame;
                slot.Buffer = hit.Buffer;
                bytes += desc.Size;
                buffers++;
            }
        }

        for (var i = 0; i < _resourceCount; i++)
        {
            var r = _resources[i];
            if (r.Imported || r.Slot < 0)
            {
                continue;
            }

            var slot = _slots[r.Slot];
            r.Texture = slot.Texture;
            r.Buffer = slot.Buffer;
            unaliased += r.IsTexture ? TextureBytes(slot.TextureDesc with { Usage = slot.TextureUsage }) : slot.BufferDesc.Size;
            _stats.TransientResources++;
        }

        _stats.PhysicalTextures = textures;
        _stats.PhysicalBuffers = buffers;
        _stats.TransientBytes = bytes;
        _stats.UnaliasedBytes = unaliased;
        _stats.Created = created;
        _stats.Reused = reused;

        // Pooled memory nobody asked for lately goes back.
        for (var i = _texturePool.Count - 1; i >= 0; i--)
        {
            if (!_texturePool[i].Taken && _frame - _texturePool[i].LastUsed > _options.KeepUnusedFrames)
            {
                _texturePool[i].Texture.Dispose();
                _texturePool.RemoveAt(i);
            }
        }

        for (var i = _bufferPool.Count - 1; i >= 0; i--)
        {
            if (!_bufferPool[i].Taken && _frame - _bufferPool[i].LastUsed > _options.KeepUnusedFrames)
            {
                _bufferPool[i].Buffer.Dispose();
                _bufferPool.RemoveAt(i);
            }
        }
    }

    static ulong TextureBytes(in TextureDesc d)
    {
        ulong total = 0;
        for (uint m = 0; m < d.MipLevels; m++)
        {
            total += GpuDevice.LevelByteSize(d.Format, d.Width, d.Height, d.Dimension == TextureDimension.D3 ? d.Depth : 1, m);
        }

        return total * d.ArrayLayers * d.SampleCount;
    }

    // ---- execute ----

    /// <summary>Checks, culls and allocates, then records every surviving pass into one encoder and submits it.</summary>
    /// <exception cref="RenderGraphException">A pass reads something no earlier pass wrote, or uses a resource twice.</exception>
    public void Execute()
    {
        EnsureBuilding();
        _building = false;
        Validate();
        Cull();
        Allocate();
        ResolveTimings();

        var ring = _frame % TimingRing;
        var timing = _timings[ring];
        timing.Pending = false;
        timing.Resolved = false;
        timing.Frame = _frame;
        timing.PassCount = 0;
        var set = _timestampSets[ring];
        uint cursor = 0;

        var enc = _device.BeginCommands();
        GpuRenderPass? openRender = null;
        GpuComputePass? openCompute = null;
        try
        {
            foreach (var p in _order)
            {
                uint pairs = set is null ? 0 : p.Type == PassType.Custom ? CustomPassTimingPairs : 1;
                if (pairs > 0 && cursor + pairs * 2 > TimingQueriesPerFrame)
                {
                    pairs = 0;
                }

                if (pairs > 0)
                {
                    enc.BeginPassTiming(new PassTiming(set!, cursor, pairs));
                }

                _context.Begin(p, enc);
                switch (p.Type)
                {
                    case PassType.Raster:
                        openRender = BeginRaster(enc, p);
                        _context.SetRender(openRender);
                        Run(p);
                        openRender.EndPass();
                        openRender = null;
                        break;
                    case PassType.Compute:
                        openCompute = enc.BeginComputePass();
                        _context.SetCompute(openCompute);
                        Run(p);
                        openCompute.EndPass();
                        openCompute = null;
                        break;
                    default:
                        Run(p);
                        break;
                }

                if (pairs > 0)
                {
                    var used = enc.EndPassTiming();
                    RecordTiming(timing, p.Name, cursor, used);
                    cursor += pairs * 2;
                }
            }
        }
        catch
        {
            // The encoder cannot be abandoned; close what is open so it can be submitted empty-handed, then report the failure.
            try
            {
                openRender?.EndPass();
                openCompute?.EndPass();
                _device.Submit(enc);
            }
            catch (InvalidOperationException)
            {
                // The failing pass left the encoder in a state that cannot be submitted; the original error matters more.
            }

            _context.End();
            throw;
        }

        _context.End();
        _device.Submit(enc);
        timing.Pending = timing.PassCount > 0;
        _executed = true;
    }

    void Run(PassInfo p)
    {
        if (p.Pass is { } pass)
        {
            pass.Execute(_context);
        }
        else
        {
            p.Execute!(_context);
        }
    }

    GpuRenderPass BeginRaster(GpuCommandEncoder enc, PassInfo p)
    {
        for (var i = 0; i < p.ColorCount; i++)
        {
            ref readonly var c = ref p.Colors[i];
            _attachments[i] = new ColorAttachment
            {
                Target = _resources[c.Resource].Texture!,
                Resolve = c.Resolve >= 0 ? _resources[c.Resolve].Texture : null,
                MipLevel = c.Mip,
                Layer = c.Layer,
                Load = c.Load,
                Store = c.Store,
                ClearR = c.R,
                ClearG = c.G,
                ClearB = c.B,
                ClearA = c.A,
            };
        }

        DepthAttachment? depth = p.HasDepth
            ? new DepthAttachment { Target = _resources[p.DepthResource].Texture!, Load = p.DepthLoad, Store = p.DepthStore, ClearDepth = p.ClearDepth, MipLevel = p.DepthMip, Layer = p.DepthLayer }
            : null;
        return enc.BeginRenderPass(new ReadOnlySpan<ColorAttachment>(_attachments, 0, p.ColorCount), depth);
    }

    // ---- timing ----

    static void RecordTiming(FrameTiming t, string name, uint first, uint pairs)
    {
        if (t.PassCount == t.Names.Length)
        {
            var n = t.Names.Length * 2;
            Array.Resize(ref t.Names, n);
            Array.Resize(ref t.First, n);
            Array.Resize(ref t.Pairs, n);
            Array.Resize(ref t.Nanos, n);
        }

        t.Names[t.PassCount] = name;
        t.First[t.PassCount] = first;
        t.Pairs[t.PassCount] = pairs;
        t.PassCount++;
    }

    /// <summary>Collects the GPU times of frames whose work has finished. Called by <see cref="Execute"/> and by reports; cheap when nothing is pending.</summary>
    public void ResolveTimings()
    {
        for (var i = 0; i < TimingRing; i++)
        {
            var t = _timings[i];
            var set = _timestampSets[i];
            if (!t.Pending || set is null)
            {
                continue;
            }

            var all = true;
            for (var k = 0; k < t.PassCount && all; k++)
            {
                var pairs = t.Pairs[k];
                if (pairs == 0)
                {
                    t.Nanos[k] = 0;
                    continue;
                }

                var raw = t.Raw.AsSpan(0, (int)pairs * 2);
                if (!_device.TryReadTimestamps(set, t.First[k], raw))
                {
                    all = false;
                    break;
                }

                ulong sum = 0;
                for (var q = 0; q < pairs; q++)
                {
                    sum += raw[q * 2 + 1] - raw[q * 2];
                }

                t.Nanos[k] = sum;
            }

            if (all)
            {
                t.Pending = false;
                t.Resolved = true;
                if (t.Frame > _latestResolved)
                {
                    _latestResolved = t.Frame;
                }
            }
        }
    }

    internal bool TryTiming(string passName, out ulong nanoseconds, out int frame)
    {
        FrameTiming? best = null;
        foreach (var t in _timings)
        {
            if (t.Resolved && (best is null || t.Frame > best.Frame))
            {
                best = t;
            }
        }

        nanoseconds = 0;
        frame = best?.Frame ?? 0;
        if (best is null)
        {
            return false;
        }

        for (var k = 0; k < best.PassCount; k++)
        {
            if (best.Names[k] == passName && best.Pairs[k] > 0)
            {
                nanoseconds = best.Nanos[k];
                return true;
            }
        }

        return false;
    }

    // ---- inspection ----

    /// <summary>
    /// Describes the last frame: passes (in order, with what they read and write, whether they were culled, GPU time), resources (lifetime,
    /// which physical memory they share) and totals. Allocates; meant for tools, not for the frame loop.
    /// </summary>
    public RenderGraphReport Describe()
    {
        ThrowIfDisposed();
        if (_building)
        {
            throw new InvalidOperationException("describe a graph after Execute, not while it is being built");
        }

        ResolveTimings();
        var passes = new List<PassReport>();
        var timingFrame = 0;
        for (var i = 0; i < _passCount; i++)
        {
            var p = _passes[i];
            var reads = new List<AccessReport>();
            var writes = new List<AccessReport>();
            foreach (var a in p.Accesses)
            {
                var r = _resources[a.Resource];
                var report = new AccessReport(r.Name, a.Kind.ToString(), a.Mip, a.Layer);
                (a.Writes ? writes : reads).Add(report);
                if (a.Writes && a.NeedsContent)
                {
                    reads.Add(report);
                }
            }

            double? gpu = null;
            if (p.Live && _executed && TryTiming(p.Name, out var ns, out var tf))
            {
                gpu = ns / 1000.0;
                timingFrame = tf;
            }

            passes.Add(new PassReport(p.Name, p.Type.ToString().ToLowerInvariant(), !p.Live, p.ExecIndex, [.. reads], [.. writes], gpu));
        }

        var resources = new List<ResourceReport>();
        for (var i = 0; i < _resourceCount; i++)
        {
            var r = _resources[i];
            string? first = r.First >= 0 ? NameAt(r.First) : null;
            string? last = r.Last >= 0 ? NameAt(r.Last) : null;
            if (r.IsTexture)
            {
                var d = r.TextureDesc;
                var dims = d.Dimension == TextureDimension.D3 ? $"{d.Width}x{d.Height}x{d.Depth}" : d.ArrayLayers > 1 ? $"{d.Width}x{d.Height}x{d.ArrayLayers}" : $"{d.Width}x{d.Height}";
                resources.Add(new ResourceReport(r.Name, "texture", r.Imported, r.Imported || r.First >= 0, d.Format.ToString(), dims, TextureBytes(d), r.Slot, first, last, r.Imported ? d.Usage.ToString() : (r.TextureUsage | d.Usage).ToString()));
            }
            else
            {
                resources.Add(new ResourceReport(r.Name, "buffer", r.Imported, r.Imported || r.First >= 0, null, r.BufferDesc.Size.ToString(System.Globalization.CultureInfo.InvariantCulture), r.BufferDesc.Size, r.Slot, first, last, (r.BufferUsage | r.BufferDesc.Usage).ToString()));
            }
        }

        return new RenderGraphReport(_frame, TimingEnabled, timingFrame, [.. passes], [.. resources], _stats);
    }

    string NameAt(int execIndex) => _order[execIndex].Name;

    // ---- teardown ----

    /// <summary>Releases the pooled textures and buffers and the timestamp queries.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var t in _texturePool)
        {
            t.Texture.Dispose();
        }

        foreach (var b in _bufferPool)
        {
            b.Buffer.Dispose();
        }

        _texturePool.Clear();
        _bufferPool.Clear();
        foreach (var s in _timestampSets)
        {
            s?.Dispose();
        }
    }
}

/// <summary>Counts and sizes from one executed frame of a <see cref="RenderGraph"/>.</summary>
public struct GraphStats
{
    /// <summary>Passes added.</summary>
    public int Passes { get; internal set; }

    /// <summary>Passes that ran.</summary>
    public int LivePasses { get; internal set; }

    /// <summary>Passes dropped because nothing used their results.</summary>
    public int CulledPasses { get; internal set; }

    /// <summary>Transient textures and buffers that were used by a surviving pass.</summary>
    public int TransientResources { get; internal set; }

    /// <summary>Physical textures behind the transient ones (fewer when lifetimes do not overlap).</summary>
    public int PhysicalTextures { get; internal set; }

    /// <summary>Physical buffers behind the transient ones.</summary>
    public int PhysicalBuffers { get; internal set; }

    /// <summary>Bytes of transient memory in use.</summary>
    public ulong TransientBytes { get; internal set; }

    /// <summary>Bytes the same resources would need without sharing memory.</summary>
    public ulong UnaliasedBytes { get; internal set; }

    /// <summary>Physical resources created this frame.</summary>
    public int Created { get; internal set; }

    /// <summary>Physical resources taken from the pool this frame.</summary>
    public int Reused { get; internal set; }
}
