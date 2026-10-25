using System.Buffers;

namespace OneGame.Gpu.WebGpu;

/// <summary>
/// The WebGPU device. Everything it does is recorded into a byte stream that the browser-side script runs when the stream is
/// flushed (on every submit, and before a read). Reads are asynchronous because a browser cannot block on the GPU; use
/// <see cref="ReadTextureAsync"/> and <see cref="ReadBufferAsync"/>.
/// </summary>
internal sealed class WebGpuDevice : GpuDevice
{
    private const int FlushThreshold = 8 * 1024 * 1024;

    private readonly GpuAdapterInfo _adapter;
    private readonly GpuCapabilities _caps;
    private readonly OpStream _stream = new();
    private readonly Stack<OpStream> _spare = new();
    private int _nextId = 1;
    private bool _disposed;

    public WebGpuDevice(GpuAdapterInfo adapter, GpuCapabilities caps)
    {
        _adapter = adapter;
        _caps = caps;
    }

    public override GpuAdapterInfo Adapter => _adapter;

    public override GpuCapabilities Capabilities => _caps;

    /// <inheritdoc />
    public override GpuBackend Backend => GpuBackend.WebGpu;

    internal int NextId() => _nextId++;

    internal OpStream Stream => _stream;

    internal OpStream RentStream() => _spare.Count > 0 ? _spare.Pop() : new OpStream();

    internal void ReturnStream(OpStream s)
    {
        s.Clear();
        _spare.Push(s);
    }

    /// <summary>Runs everything recorded so far in the browser.</summary>
    internal void Flush()
    {
        if (_stream.Length == 0)
        {
            return;
        }

        Js.Exec(_stream.Mutable);
        _stream.Clear();
    }

    internal void Destroy(int id, uint kind)
    {
        if (_disposed)
        {
            return;
        }

        _stream.U(Op.Destroy).U(id).U(kind);
    }

    /// <inheritdoc />
    public override GpuBuffer CreateBuffer(in BufferDesc desc)
    {
        if (desc.Size == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(desc), "buffer size must be greater than zero");
        }

        var id = NextId();
        _stream.U(Op.CreateBuffer).U(id).U(desc.Size).U((uint)desc.Usage).Str(desc.Label);
        return new WebBuffer(this, id, desc);
    }

    /// <inheritdoc />
    public override GpuTexture CreateTexture(in TextureDesc desc)
    {
        Validate.TextureDesc(desc, _caps);

        var id = NextId();
        var layersOrDepth = desc.Dimension == TextureDimension.D3 ? desc.Depth : desc.ArrayLayers;
        _stream.U(Op.CreateTexture).U(id).U(desc.Width).U(desc.Height).U(layersOrDepth).U((uint)desc.Format).U((uint)desc.Dimension)
            .U(desc.MipLevels).U(desc.SampleCount).U((uint)desc.Usage).Str(desc.Label);
        return new WebTexture(this, id, desc);
    }

    /// <inheritdoc />
    public override GpuSampler CreateSampler(in SamplerDesc desc)
    {
        var id = NextId();
        _stream.U(Op.CreateSampler).U(id).U((uint)desc.MagFilter).U((uint)desc.MinFilter).U((uint)desc.MipFilter)
            .U((uint)desc.AddressU).U((uint)desc.AddressV).U((uint)desc.AddressW)
            .U(desc.Compare is { } c ? (uint)c : 255u).F(desc.MinLod).F(desc.MaxLod).F(desc.MaxAnisotropy);
        return new WebSampler(this, id);
    }

    /// <inheritdoc />
    public override GpuShader CreateShader(ShaderStage stage, ReadOnlySpan<byte> spirv, string entryPoint) =>
        throw new GpuUnsupportedException("WebGPU runs WGSL, not SPIR-V. Create the shader from a ShaderSource that carries Wgsl.");

    /// <inheritdoc />
    public override GpuShader CreateShader(in ShaderSource source)
    {
        var (stage, entryPoint) = (source.Stage, source.EntryPoint);
        var wgsl = source.Wgsl;
        var wgslEntryPoint = source.WgslEntryPoint ?? entryPoint;
        if (wgsl.IsEmpty)
        {
            throw new GpuUnsupportedException($"The shader '{entryPoint}' has no WGSL. Build it with OneShaderTargets including Wgsl.");
        }

        var id = NextId();
        _stream.U(Op.CreateShader).U(id).U((uint)stage).Str(wgslEntryPoint).Bytes(wgsl).Str(entryPoint)
            .U(source.WgslPushGroup < 0 ? 0u : (uint)source.WgslPushGroup + 1).U(source.WgslPushBinding < 0 ? 0u : (uint)source.WgslPushBinding + 1);
        return new WebShader(this, id, stage, entryPoint);
    }

    /// <inheritdoc />
    public override GpuPipelineLayout CreatePipelineLayout(ReadOnlySpan<ResourceBinding> bindings) => CreatePipelineLayout(bindings, default);

    /// <inheritdoc />
    public override GpuPipelineLayout CreatePipelineLayout(ReadOnlySpan<ResourceBinding> bindings, in PushConstantRange pushConstants)
    {
        if (pushConstants.Size > MaxPushConstantBytes || pushConstants.Size % 4 != 0)
        {
            throw new ArgumentException($"push constants are a multiple of 4 bytes, at most {MaxPushConstantBytes}", nameof(pushConstants));
        }

        var id = NextId();
        var sets = 0;
        _stream.U(Op.CreateLayout).U(id).U((uint)bindings.Length).Str(null).U(pushConstants.Size).U((uint)pushConstants.Stages);
        foreach (var b in bindings)
        {
            if (b.Count != 1)
            {
                throw new GpuUnsupportedException("WebGPU has no binding arrays: declare each resource separately or use a texture array.");
            }

            sets = Math.Max(sets, (int)b.Set + 1);
            _stream.U(b.Set).U(b.Binding).U((uint)b.Kind).U((uint)b.Stages).U(b.Dynamic ? 1u : 0u).U(0u);
        }

        return new WebPipelineLayout(this, id, sets, bindings.ToArray(), pushConstants.Size);
    }

    /// <inheritdoc />
    public override GpuTimestampSet CreateTimestampSet(uint count) => new WebTimestampSet(count);

    /// <inheritdoc />
    public override bool TryReadTimestamps(GpuTimestampSet timestamps, uint first, Span<ulong> nanoseconds) => false;

    /// <inheritdoc />
    public override GpuBindGroup CreateBindGroup(GpuPipelineLayout layout, uint setIndex, ReadOnlySpan<BindGroupEntry> entries)
    {
        var wl = (WebPipelineLayout)layout;
        var dynamicCount = Validate.BindGroup(wl, setIndex, entries);
        var id = NextId();
        _stream.U(Op.CreateBindGroup).U(id).U(((WebPipelineLayout)layout).Id).U(setIndex).U((uint)entries.Length).Str(null);
        foreach (var e in entries)
        {
            if (e.Textures is not null || e.Samplers is not null || e.Buffers is not null)
            {
                throw new GpuUnsupportedException("WebGPU has no binding arrays.");
            }

            if (e.Buffer is { } buffer)
            {
                _stream.U(e.Binding).U(0u).U(((WebBuffer)buffer).Id).U(e.Offset).U(e.Size);
            }
            else if (e.Texture is { } texture)
            {
                _stream.U(e.Binding).U(1u).U(((IWebGpuObject)texture).Id).U(0UL).U(0UL);
                var v = e.View ?? default;
                var kind = v.Kind;
                if (kind == TextureViewKind.Auto && e.View is not null && v.LayerCount == 1 && texture.Dimension != TextureDimension.D3)
                {
                    kind = TextureViewKind.D2;
                }

                _stream.U((uint)kind).U(v.BaseMip).U(v.MipCount).U(v.BaseLayer).U(v.LayerCount);
            }
            else if (e.Sampler is { } sampler)
            {
                _stream.U(e.Binding).U(2u).U(((WebSampler)sampler).Id).U(0UL).U(0UL);
            }
            else
            {
                throw new ArgumentException($"Bind group entry {e.Binding} names no resource.", nameof(entries));
            }
        }

        return new WebBindGroup(this, id, setIndex, dynamicCount);
    }

    /// <inheritdoc />
    public override GpuGraphicsPipeline CreateGraphicsPipeline(GraphicsPipelineDesc desc)
    {
        ArgumentNullException.ThrowIfNull(desc);
        var id = NextId();
        _stream.U(Op.CreateGraphicsPipeline).U(id).U(((WebPipelineLayout)desc.Layout).Id).U(((WebShader)desc.Vertex).Id)
            .U(desc.Pixel is { } ps ? ((WebShader)ps).Id : 0)
            .U((uint)desc.VertexBuffers.Count);
        foreach (var vb in desc.VertexBuffers)
        {
            var attrs = vb.Attributes.Span;
            _stream.U(vb.Stride).U((uint)vb.StepMode).U((uint)attrs.Length);
            foreach (var a in attrs)
            {
                _stream.U(a.Location).U((uint)a.Format).U(a.Offset);
            }
        }

        _stream.U((uint)desc.Topology).U((uint)desc.Cull).U((uint)desc.Front).U((uint)desc.ColorTargets.Count);
        foreach (var t in desc.ColorTargets)
        {
            _stream.U((uint)t.Format);
            if (t.Blend is { } b)
            {
                _stream.U(1u)
                    .U((uint)b.Color.Source).U((uint)b.Color.Destination).U((uint)b.Color.Operation)
                    .U((uint)b.Alpha.Source).U((uint)b.Alpha.Destination).U((uint)b.Alpha.Operation);
            }
            else
            {
                _stream.U(0u);
            }

            _stream.U((uint)t.WriteMask);
        }

        if (desc.DepthStencil is { } d)
        {
            _stream.U(1u).U((uint)d.Format).U(d.DepthWrite ? 1u : 0u).U((uint)d.DepthCompare).U(d.DepthBias).F(d.DepthBiasSlopeScale).F(d.DepthBiasClamp);
        }
        else
        {
            _stream.U(0u);
        }

        _stream.U(desc.SampleCount).U(desc.DepthClamp ? 1u : 0u).Str(desc.Label);
        return new WebGraphicsPipeline(this, id, (WebPipelineLayout)desc.Layout);
    }

    /// <inheritdoc />
    public override GpuComputePipeline CreateComputePipeline(ComputePipelineDesc desc)
    {
        ArgumentNullException.ThrowIfNull(desc);
        var id = NextId();
        _stream.U(Op.CreateComputePipeline).U(id).U(((WebPipelineLayout)desc.Layout).Id).U(((WebShader)desc.Compute).Id).Str(desc.Label);
        return new WebComputePipeline(this, id, (WebPipelineLayout)desc.Layout);
    }

    /// <inheritdoc />
    public override GpuSwapchain CreateSwapchain(in SurfaceSource surface, in SwapchainDesc desc)
    {
        if (surface.Kind != SurfaceKind.Canvas || string.IsNullOrEmpty(surface.CanvasId))
        {
            throw new GpuUnsupportedException("The WebGPU backend draws to a canvas: use SurfaceSource.Canvas(id).");
        }

        return new WebSwapchain(this, surface.CanvasId, desc);
    }

    /// <inheritdoc />
    public override GpuCommandEncoder BeginCommands() => new WebEncoder(this);

    /// <inheritdoc />
    public override void Submit(GpuCommandEncoder commands)
    {
        var e = (WebEncoder)commands;
        e.Finish(_stream);
        Flush();
    }

    /// <inheritdoc />
    public override void WriteBuffer(GpuBuffer buffer, ulong offset, ReadOnlySpan<byte> data)
    {
        var wb = (WebBuffer)buffer;
        if ((wb.Usage & BufferUsage.CopyDestination) == 0)
        {
            throw new ArgumentException("buffer was not created with BufferUsage.CopyDestination", nameof(buffer));
        }

        if (offset + (ulong)data.Length > wb.Size)
        {
            throw new ArgumentOutOfRangeException(nameof(data), "write extends past the end of the buffer");
        }

        if (data.IsEmpty)
        {
            return;
        }

        _stream.U(Op.WriteBuffer).U(((WebBuffer)buffer).Id).U(offset).Bytes(data);
        if (_stream.Length > FlushThreshold)
        {
            Flush();
        }
    }

    /// <inheritdoc />
    public override void WriteTexture(GpuTexture texture, uint mipLevel, uint layer, ReadOnlySpan<byte> data)
    {
        Validate.Subresource(texture, mipLevel, layer);
        var w = Math.Max(texture.Width >> (int)mipLevel, 1u);
        var h = Math.Max(texture.Height >> (int)mipLevel, 1u);
        var d = texture.Dimension == TextureDimension.D3 ? Math.Max(texture.Depth >> (int)mipLevel, 1u) : 1u;
        WriteTexture(texture, new TextureRegion(mipLevel, layer, 0, 0, 0, w, h, d), data);
    }

    /// <inheritdoc />
    public override void WriteTexture(GpuTexture texture, in TextureRegion region, ReadOnlySpan<byte> data)
    {
        Validate.Write(texture, region, data.Length);

        _stream.U(Op.WriteTexture).U(((IWebGpuObject)texture).Id).U(region.MipLevel).U(region.Layer).U(region.X).U(region.Y).U(region.Z)
            .U(region.Width).U(region.Height).U(region.Depth).Bytes(data);
        if (_stream.Length > FlushThreshold)
        {
            Flush();
        }
    }

    /// <inheritdoc />
    public override void ReadBuffer(GpuBuffer buffer, ulong offset, Span<byte> destination) =>
        throw new GpuUnsupportedException("A browser cannot wait for the GPU: use ReadBufferAsync.");

    /// <inheritdoc />
    public override void ReadTexture(GpuTexture texture, uint mipLevel, uint layer, Span<byte> destination) =>
        throw new GpuUnsupportedException("A browser cannot wait for the GPU: use ReadTextureAsync.");

    /// <inheritdoc />
    public override async ValueTask ReadBufferAsync(GpuBuffer buffer, ulong offset, Memory<byte> destination)
    {
        if ((buffer.Usage & BufferUsage.CopySource) == 0)
        {
            throw new ArgumentException("buffer was not created with BufferUsage.CopySource", nameof(buffer));
        }

        if (offset + (ulong)destination.Length > buffer.Size)
        {
            throw new ArgumentOutOfRangeException(nameof(destination), "read extends past the end of the buffer");
        }

        Flush();
        var ticket = await Js.ReadBuffer(((WebBuffer)buffer).Id, offset, destination.Length).ConfigureAwait(true);
        Js.TakeRead(ticket, destination.Span[..destination.Length]);
    }

    /// <inheritdoc />
    public override async ValueTask ReadTextureAsync(GpuTexture texture, uint mipLevel, uint layer, Memory<byte> destination)
    {
        if ((texture.Usage & TextureUsage.CopySource) == 0)
        {
            throw new ArgumentException("texture was not created with TextureUsage.CopySource", nameof(texture));
        }

        Validate.Subresource(texture, mipLevel, layer);
        Flush();
        var w = Math.Max(texture.Width >> (int)mipLevel, 1u);
        var h = Math.Max(texture.Height >> (int)mipLevel, 1u);
        var d = texture.Dimension == TextureDimension.D3 ? Math.Max(texture.Depth >> (int)mipLevel, 1u) : 1u;
        var bpt = BytesPerTexel(texture.Format);
        var need = (long)w * h * d * bpt;
        if (destination.Length != need)
        {
            throw new ArgumentException($"expected a destination of {need} bytes for mip {mipLevel}, got {destination.Length}", nameof(destination));
        }

        var ticket = await Js.ReadTexture(((IWebGpuObject)texture).Id, (int)mipLevel, (int)layer, (int)w, (int)h, (int)d, (int)bpt).ConfigureAwait(true);
        Js.TakeRead(ticket, destination.Span[..(int)need]);
    }

    /// <inheritdoc />
    public override void WaitIdle() => Flush();

    /// <inheritdoc />
    public override string[] TakeValidationMessages()
    {
        Flush();
        var text = Js.TakeMessages();
        return string.IsNullOrEmpty(text) ? [] : text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Flush();
        _disposed = true;
    }
}
