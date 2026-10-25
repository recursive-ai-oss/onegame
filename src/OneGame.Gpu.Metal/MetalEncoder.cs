using OneGame.Gpu.Metal.Interop;

namespace OneGame.Gpu.Metal;

/// <summary>Records one command buffer. Passes and copies encode straight into Metal encoders; nothing is committed until the device submits it.</summary>
internal sealed unsafe class MetalEncoder : GpuCommandEncoder
{
    readonly MetalDevice _dev;
    nint _cb;
    nint _blit;
    bool _inPass;
    bool _finished;
    MetalRenderPass? _renderPass;
    MetalComputePass? _computePass;

    internal MetalEncoder(MetalDevice dev)
    {
        _dev = dev;
        _finished = true;
    }

    /// <summary>Starts a recording on a recycled encoder object, so steady-state frames allocate nothing.</summary>
    internal void Begin(nint commandBuffer)
    {
        _cb = commandBuffer;
        _blit = 0;
        _inPass = false;
        _finished = false;
    }

    internal MetalDevice Device => _dev;

    internal void PassEnded() => _inPass = false;

    /// <summary>Ends any open blit encoder and hands the command buffer (and its one reference) to the caller.</summary>
    internal nint Finish()
    {
        if (_finished)
        {
            throw new InvalidOperationException("this encoder was already submitted");
        }

        if (_inPass)
        {
            throw new InvalidOperationException("a render or compute pass is still open");
        }

        EndBlit();
        _finished = true;
        var cb = _cb;
        _cb = 0;
        return cb;
    }

    void EnsureOpen()
    {
        if (_finished)
        {
            throw new InvalidOperationException("this encoder was already submitted");
        }

        if (_inPass)
        {
            throw new InvalidOperationException("end the current pass before recording other commands");
        }
    }

    void EndBlit()
    {
        if (_blit != 0)
        {
            Msg.Void(_blit, Sel.endEncoding);
            Objc.objc_release(_blit);
            _blit = 0;
        }
    }

    nint Blit()
    {
        EnsureOpen();
        if (_blit == 0)
        {
            using var pool = new Pool();
            _blit = Objc.objc_retain(Msg.Obj(_cb, Sel.blitCommandEncoder));
        }

        return _blit;
    }

    // ---- passes ----

    /// <inheritdoc />
    public override GpuRenderPass BeginRenderPass(ReadOnlySpan<ColorAttachment> colors, DepthAttachment? depth = null)
    {
        EnsureOpen();
        if (colors.Length == 0 && depth is null)
        {
            throw new ArgumentException("a render pass needs at least one attachment");
        }

        if (colors.Length > 8)
        {
            throw new ArgumentException("at most 8 color attachments", nameof(colors));
        }

        EndBlit();
        using var pool = new Pool();
        var rpd = Msg.Obj(Cls.MTLRenderPassDescriptor, Sel.renderPassDescriptor);
        var cas = Msg.Obj(rpd, Sel.colorAttachments);
        for (var i = 0; i < colors.Length; i++)
        {
            var c = colors[i];
            var t = c.Target as MetalTexture ?? throw new ArgumentException("attachment belongs to another backend");
            if ((t.Usage & TextureUsage.RenderTarget) == 0)
            {
                throw new ArgumentException("attachment texture lacks TextureUsage.RenderTarget");
            }

            if (t.Dimension == TextureDimension.D3)
            {
                throw new ArgumentException("3D textures cannot be render targets");
            }

            var ca = Msg.Obj(cas, Sel.objectAtIndexedSubscript_, (nuint)i);
            Msg.Void(ca, Sel.setTexture_, t.Handle);
            Msg.Void(ca, Sel.setLevel_, (nuint)c.MipLevel);
            Msg.Void(ca, Sel.setSlice_, (nuint)c.Layer);
            Msg.Void(ca, Sel.setLoadAction_, MetalMap.Load(c.Load));
            if (c.Resolve is { } resolve)
            {
                var r = resolve as MetalTexture ?? throw new ArgumentException("attachment belongs to another backend");
                if (t.SampleCount <= 1 || r.SampleCount != 1)
                {
                    throw new ArgumentException("Resolve needs a multisampled Target and a single-sample Resolve texture");
                }

                Msg.Void(ca, Sel.setResolveTexture_, r.Handle);
                Msg.Void(ca, Sel.setStoreAction_, c.Store == StoreOp.Store ? MT.StoreAndResolve : MT.StoreResolve);
            }
            else
            {
                Msg.Void(ca, Sel.setStoreAction_, c.Store == StoreOp.Store ? MT.StoreStore : MT.StoreDontCare);
            }

            if (c.Load == LoadOp.Clear)
            {
                Msg.Void(ca, Sel.setClearColor_, new MTLClearColor { Red = c.ClearR, Green = c.ClearG, Blue = c.ClearB, Alpha = c.ClearA });
            }
        }

        if (depth is { } d)
        {
            var t = d.Target as MetalTexture ?? throw new ArgumentException("attachment belongs to another backend");
            if (!MetalMap.IsDepth(t.Format) || (t.Usage & TextureUsage.RenderTarget) == 0)
            {
                throw new ArgumentException("depth attachment must be a depth texture with TextureUsage.RenderTarget");
            }

            var da = Msg.Obj(rpd, Sel.depthAttachment);
            Msg.Void(da, Sel.setTexture_, t.Handle);
            Msg.Void(da, Sel.setLevel_, (nuint)d.MipLevel);
            Msg.Void(da, Sel.setSlice_, (nuint)d.Layer);
            Msg.Void(da, Sel.setLoadAction_, MetalMap.Load(d.Load));
            Msg.Void(da, Sel.setStoreAction_, d.Store == StoreOp.Store ? MT.StoreStore : MT.StoreDontCare);
            Msg.Void(da, Sel.setClearDepth_, (double)d.ClearDepth);
            if (MetalMap.HasStencil(t.Format))
            {
                var sa = Msg.Obj(rpd, Sel.stencilAttachment);
                Msg.Void(sa, Sel.setTexture_, t.Handle);
                Msg.Void(sa, Sel.setLevel_, (nuint)d.MipLevel);
                Msg.Void(sa, Sel.setSlice_, (nuint)d.Layer);
                Msg.Void(sa, Sel.setLoadAction_, MT.LoadDontCare);
                Msg.Void(sa, Sel.setStoreAction_, MT.StoreDontCare);
            }
        }

        var enc = Msg.Obj(_cb, Sel.renderCommandEncoderWithDescriptor_, rpd);
        if (enc == 0)
        {
            throw new GpuException("Metal could not begin a render pass");
        }

        _inPass = true;
        var pass = _renderPass ??= new MetalRenderPass(this);
        pass.Begin(Objc.objc_retain(enc));
        return pass;
    }

    /// <inheritdoc />
    public override GpuComputePass BeginComputePass()
    {
        EnsureOpen();
        EndBlit();
        using var pool = new Pool();
        var enc = Msg.Obj(_cb, Sel.computeCommandEncoder);
        if (enc == 0)
        {
            throw new GpuException("Metal could not begin a compute pass");
        }

        _inPass = true;
        var pass = _computePass ??= new MetalComputePass(this);
        pass.Begin(Objc.objc_retain(enc));
        return pass;
    }

    // ---- copies ----

    /// <inheritdoc />
    public override void CopyBuffer(GpuBuffer source, ulong sourceOffset, GpuBuffer destination, ulong destinationOffset, ulong size)
    {
        var s = source as MetalBuffer ?? throw new ArgumentException("buffer belongs to another backend", nameof(source));
        var d = destination as MetalBuffer ?? throw new ArgumentException("buffer belongs to another backend", nameof(destination));
        if ((s.Usage & BufferUsage.CopySource) == 0 || (d.Usage & BufferUsage.CopyDestination) == 0)
        {
            throw new ArgumentException("buffers need CopySource on the source and CopyDestination on the destination");
        }

        if (sourceOffset + size > s.Size || destinationOffset + size > d.Size)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "copy extends past the end of a buffer");
        }

        if (size == 0)
        {
            return;
        }

        Msg.Void(Blit(), Sel.copyFromBuffer_sourceOffset_toBuffer_destinationOffset_size_, s.Handle, (nuint)sourceOffset, d.Handle, (nuint)destinationOffset, (nuint)size);
    }

    static (uint W, uint H, uint D) LevelSize(MetalTexture t, uint mip) =>
        (Math.Max(t.Width >> (int)mip, 1), Math.Max(t.Height >> (int)mip, 1), Math.Max(t.Depth >> (int)mip, 1));

    internal static void EncodeTextureToBuffer(nint blit, MetalTexture t, uint mip, uint layer, nint buffer, ulong offset)
    {
        var (w, h, d) = LevelSize(t, mip);
        var bpt = GpuDevice.BytesPerTexel(t.Format);
        var size = new MTLSize(w, h, d);
        var rowBytes = (nuint)(w * bpt);
        var imageBytes = (nuint)((ulong)w * h * bpt);
        if (t.Format == GpuFormat.Depth32FloatStencil8)
        {
            Msg.Void(blit, Sel.copyFromTexture_sourceSlice_sourceLevel_sourceOrigin_sourceSize_toBuffer_destinationOffset_destinationBytesPerRow_destinationBytesPerImage_options_,
                t.Handle, (nuint)layer, (nuint)mip, new MTLOrigin(0, 0, 0), size, buffer, (nuint)offset, rowBytes, imageBytes, MT.BlitDepthFromDepthStencil);
        }
        else
        {
            Msg.Void(blit, Sel.copyFromTexture_sourceSlice_sourceLevel_sourceOrigin_sourceSize_toBuffer_destinationOffset_destinationBytesPerRow_destinationBytesPerImage_,
                t.Handle, (nuint)layer, (nuint)mip, new MTLOrigin(0, 0, 0), size, buffer, (nuint)offset, rowBytes, imageBytes);
        }
    }

    /// <inheritdoc />
    public override void CopyTextureToBuffer(GpuTexture source, uint mipLevel, uint layer, GpuBuffer destination, ulong destinationOffset)
    {
        var t = source as MetalTexture ?? throw new ArgumentException("texture belongs to another backend", nameof(source));
        var b = destination as MetalBuffer ?? throw new ArgumentException("buffer belongs to another backend", nameof(destination));
        if ((t.Usage & TextureUsage.CopySource) == 0 || (b.Usage & BufferUsage.CopyDestination) == 0)
        {
            throw new ArgumentException("the texture needs CopySource and the buffer CopyDestination");
        }

        if (mipLevel >= t.MipLevels || layer >= t.ArrayLayers)
        {
            throw new ArgumentOutOfRangeException(nameof(mipLevel), "mip level or layer out of range");
        }

        var (w, h, d) = LevelSize(t, mipLevel);
        if (destinationOffset + (ulong)w * h * d * GpuDevice.BytesPerTexel(t.Format) > b.Size)
        {
            throw new ArgumentOutOfRangeException(nameof(destinationOffset), "copy extends past the end of the buffer");
        }

        EncodeTextureToBuffer(Blit(), t, mipLevel, layer, b.Handle, destinationOffset);
    }

    /// <inheritdoc />
    public override void CopyBufferToTexture(GpuBuffer source, ulong sourceOffset, GpuTexture destination, uint mipLevel, uint layer)
    {
        var b = source as MetalBuffer ?? throw new ArgumentException("buffer belongs to another backend", nameof(source));
        var t = destination as MetalTexture ?? throw new ArgumentException("texture belongs to another backend", nameof(destination));
        if ((b.Usage & BufferUsage.CopySource) == 0 || (t.Usage & TextureUsage.CopyDestination) == 0)
        {
            throw new ArgumentException("the buffer needs CopySource and the texture CopyDestination");
        }

        if (mipLevel >= t.MipLevels || layer >= t.ArrayLayers)
        {
            throw new ArgumentOutOfRangeException(nameof(mipLevel), "mip level or layer out of range");
        }

        var (w, h, d) = LevelSize(t, mipLevel);
        var bpt = GpuDevice.BytesPerTexel(t.Format);
        if (sourceOffset + (ulong)w * h * d * bpt > b.Size)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceOffset), "copy extends past the end of the buffer");
        }

        Msg.Void(Blit(), Sel.copyFromBuffer_sourceOffset_sourceBytesPerRow_sourceBytesPerImage_sourceSize_toTexture_destinationSlice_destinationLevel_destinationOrigin_,
            b.Handle, (nuint)sourceOffset, (nuint)(w * bpt), (nuint)((ulong)w * h * bpt), new MTLSize(w, h, d), t.Handle, (nuint)layer, (nuint)mipLevel, new MTLOrigin(0, 0, 0));
    }

    /// <inheritdoc />
    public override void CopyTexture(GpuTexture source, GpuTexture destination)
    {
        var s = source as MetalTexture ?? throw new ArgumentException("texture belongs to another backend", nameof(source));
        var d = destination as MetalTexture ?? throw new ArgumentException("texture belongs to another backend", nameof(destination));
        if ((s.Usage & TextureUsage.CopySource) == 0 || (d.Usage & TextureUsage.CopyDestination) == 0)
        {
            throw new ArgumentException("textures need CopySource on the source and CopyDestination on the destination texture");
        }

        if (s.Width != d.Width || s.Height != d.Height || s.Format != d.Format || s.Dimension != d.Dimension || s.MipLevels != d.MipLevels
            || s.ArrayLayers != d.ArrayLayers || s.Depth != d.Depth || s.SampleCount != 1 || d.SampleCount != 1)
        {
            throw new ArgumentException("textures must match in size, format, dimension, mip levels and layers, and not be multisampled", nameof(destination));
        }

        Msg.Void(Blit(), Sel.copyFromTexture_sourceSlice_sourceLevel_toTexture_destinationSlice_destinationLevel_sliceCount_levelCount_,
            s.Handle, (nuint)0, (nuint)0, d.Handle, (nuint)0, (nuint)0, (nuint)s.ArrayLayers, (nuint)s.MipLevels);
    }

    /// <inheritdoc />
    public override void GenerateMips(GpuTexture texture)
    {
        var t = texture as MetalTexture ?? throw new ArgumentException("texture belongs to another backend", nameof(texture));
        if (MetalMap.IsDepth(t.Format))
        {
            throw new ArgumentException("depth textures cannot generate mips", nameof(texture));
        }

        if (t.MipLevels <= 1)
        {
            return;
        }

        Msg.Void(Blit(), Sel.generateMipmapsForTexture_, t.Handle);
    }

    /// <inheritdoc />
    public override void BeginPassTiming(in PassTiming timing) =>
        throw new GpuUnsupportedException("pass timing is not supported by the Metal backend yet (GpuCapabilities.Timestamps is false)");

    /// <inheritdoc />
    public override uint EndPassTiming() => 0;
}

/// <summary>State shared by render and compute passes: the bound groups and which still need to be written to the encoder.</summary>
internal static class BindingRules
{
    public const int MaxSets = 8;

    /// <summary>Dynamic offsets one bind group may take.</summary>
    public const int MaxDynamic = 16;

    public static void CheckGroup(GpuBindGroup group, uint setIndex, ReadOnlySpan<uint> dynamicOffsets, out MetalBindGroup g)
    {
        g = group as MetalBindGroup ?? throw new ArgumentException("bind group belongs to another backend", nameof(group));
        if (g.Set != setIndex || setIndex >= MaxSets)
        {
            throw new ArgumentException($"bind group was created for set {g.Set}, not {setIndex} (or the set index is above 7)", nameof(group));
        }

        if (dynamicOffsets.Length != g.DynamicCount)
        {
            throw new ArgumentException($"the bind group has {g.DynamicCount} dynamic bindings but {dynamicOffsets.Length} offsets were given", nameof(dynamicOffsets));
        }

        if (dynamicOffsets.Length > MaxDynamic)
        {
            throw new ArgumentException($"a bind group takes at most {MaxDynamic} dynamic offsets", nameof(dynamicOffsets));
        }

        foreach (var o in dynamicOffsets)
        {
            if (o % GpuDevice.DynamicOffsetAlignment != 0)
            {
                throw new ArgumentException($"dynamic offsets must be multiples of {GpuDevice.DynamicOffsetAlignment}", nameof(dynamicOffsets));
            }
        }
    }
}

/// <summary>The push-constant bytes of a pass. Metal has no push constants of its own: the block goes to the shader as an inline buffer argument at the index the shader build reports.</summary>
internal struct PushBlock
{
    public byte[] Bytes;
    public bool Dirty;

    public void Set(MetalPipelineLayout? layout, uint offset, ReadOnlySpan<byte> data)
    {
        var size = layout?.Push.Size ?? 0u;
        if (offset % 4 != 0 || data.Length % 4 != 0 || (ulong)offset + (ulong)data.Length > size)
        {
            throw new ArgumentOutOfRangeException(nameof(data), "pushed data must be 4-byte aligned and fit the range the pipeline layout declares");
        }

        Bytes ??= new byte[GpuDevice.MaxPushConstantBytes];
        data.CopyTo(Bytes.AsSpan((int)offset));
        Dirty = true;
    }
}

internal sealed unsafe class MetalRenderPass : GpuRenderPass
{
    readonly MetalEncoder _owner;
    nint _enc;
    MetalGraphicsPipeline? _pipeline;
    readonly MetalBindGroup?[] _groups = new MetalBindGroup?[BindingRules.MaxSets];
    readonly uint[] _offsets = new uint[BindingRules.MaxSets * BindingRules.MaxDynamic];
    uint _dirty;
    nint _indexBuffer;
    nuint _indexType;
    ulong _indexOffset;
    bool _ended;

    internal MetalRenderPass(MetalEncoder owner) => _owner = owner;

    internal void Begin(nint encoder)
    {
        _enc = encoder;
        _pipeline = null;
        Array.Clear(_groups);
        _dirty = 0;
        _indexBuffer = 0;
        _push.Dirty = false;
        _ended = false;
    }

    void Check()
    {
        if (_ended)
        {
            throw new InvalidOperationException("the render pass has ended");
        }
    }

    public override void SetPipeline(GpuGraphicsPipeline pipeline)
    {
        Check();
        var p = pipeline as MetalGraphicsPipeline ?? throw new ArgumentException("pipeline belongs to another backend", nameof(pipeline));
        _pipeline = p;
        Msg.Void(_enc, Sel.setRenderPipelineState_, p.State);
        Msg.Void(_enc, Sel.setDepthStencilState_, p.DepthState != 0 ? p.DepthState : _owner.Device.NoDepthState);
        Msg.Void(_enc, Sel.setCullMode_, p.Cull);
        Msg.Void(_enc, Sel.setFrontFacingWinding_, p.Winding);
        Msg.Void(_enc, Sel.setDepthBias_slopeScale_clamp_, p.DepthBias, p.SlopeScale, p.BiasClamp);
        Msg.Void(_enc, Sel.setDepthClipMode_, p.DepthClip);

        // Another pipeline may number the same resources differently, so everything bound is written again.
        _dirty = 0xFF;
        _push.Dirty = _push.Bytes is not null;
    }

    public override void SetBindGroup(uint setIndex, GpuBindGroup group, ReadOnlySpan<uint> dynamicOffsets)
    {
        Check();
        BindingRules.CheckGroup(group, setIndex, dynamicOffsets, out var g);
        _groups[setIndex] = g;
        dynamicOffsets.CopyTo(_offsets.AsSpan((int)setIndex * BindingRules.MaxDynamic));
        _dirty |= 1u << (int)setIndex;
    }

    PushBlock _push;

    public override void SetPushConstants(uint offset, ReadOnlySpan<byte> data)
    {
        Check();
        _push.Set(_pipeline?.Layout, offset, data);
    }

    public override void SetVertexBuffer(uint slot, GpuBuffer buffer, ulong offset = 0)
    {
        Check();
        var b = buffer as MetalBuffer ?? throw new ArgumentException("buffer belongs to another backend", nameof(buffer));
        if ((b.Usage & BufferUsage.Vertex) == 0)
        {
            throw new ArgumentException("buffer lacks BufferUsage.Vertex", nameof(buffer));
        }

        if (slot > 15 || offset > b.Size)
        {
            throw new ArgumentOutOfRangeException(nameof(slot), "vertex buffer slot must be below 16 and the offset inside the buffer");
        }

        Msg.Void(_enc, Sel.setVertexBuffer_offset_atIndex_, b.Handle, (nuint)offset, (nuint)(MetalDevice.VertexBufferBase - slot));
    }

    public override void SetIndexBuffer(GpuBuffer buffer, IndexFormat format, ulong offset = 0)
    {
        Check();
        var b = buffer as MetalBuffer ?? throw new ArgumentException("buffer belongs to another backend", nameof(buffer));
        if ((b.Usage & BufferUsage.Index) == 0)
        {
            throw new ArgumentException("buffer lacks BufferUsage.Index", nameof(buffer));
        }

        _indexBuffer = b.Handle;
        _indexType = format == IndexFormat.Index16 ? MT.Index16 : MT.Index32;
        _indexOffset = offset;
    }

    public override void SetViewport(float x, float y, float width, float height, float minDepth = 0, float maxDepth = 1)
    {
        Check();
        Msg.Void(_enc, Sel.setViewport_, new MTLViewport { OriginX = x, OriginY = y, Width = width, Height = height, ZNear = minDepth, ZFar = maxDepth });
    }

    public override void SetScissor(int x, int y, uint width, uint height)
    {
        Check();
        // Metal rejects rectangles outside the attachment; clamp the origin to zero like a rasterizer would clip.
        var x0 = Math.Max(x, 0);
        var y0 = Math.Max(y, 0);
        Msg.Void(_enc, Sel.setScissorRect_, new MTLScissorRect { X = (nuint)x0, Y = (nuint)y0, Width = width - (uint)(x0 - x), Height = height - (uint)(y0 - y) });
    }

    /// <summary>Writes the resources of every group whose binding changed to the vertex and fragment stages that use them.</summary>
    void Apply()
    {
        var p = _pipeline ?? throw new InvalidOperationException("set a pipeline before drawing");
        if (_push.Dirty && p.Layout.Push.Size > 0)
        {
            fixed (byte* bytes = _push.Bytes)
            {
                if ((p.Layout.Push.Stages & ShaderStage.Vertex) != 0 && p.VertexPushIndex >= 0)
                {
                    Msg.Void(_enc, Sel.setVertexBytes_length_atIndex_, (nint)bytes, (nuint)p.Layout.Push.Size, (nuint)p.VertexPushIndex);
                }

                if ((p.Layout.Push.Stages & ShaderStage.Pixel) != 0 && p.PixelPushIndex >= 0)
                {
                    Msg.Void(_enc, Sel.setFragmentBytes_length_atIndex_, (nint)bytes, (nuint)p.Layout.Push.Size, (nuint)p.PixelPushIndex);
                }
            }

            _push.Dirty = false;
        }

        if (_dirty == 0)
        {
            return;
        }

        for (var s = 0; s < BindingRules.MaxSets; s++)
        {
            if ((_dirty & (1u << s)) == 0 || _groups[s] is not { } g)
            {
                continue;
            }

            var offsets = _offsets.AsSpan(s * BindingRules.MaxDynamic, BindingRules.MaxDynamic);
            foreach (var b in g.Bindings)
            {
                var vi = (b.Stages & ShaderStage.Vertex) != 0 ? p.VertexSlots.Lookup((uint)s, b.Binding) : -1;
                var fi = (b.Stages & ShaderStage.Pixel) != 0 ? p.PixelSlots.Lookup((uint)s, b.Binding) : -1;
                for (var k = 0; k < b.Handles.Length; k++)
                {
                    var offset = b.Offsets[k] + (b.DynamicIndex >= 0 ? offsets[b.DynamicIndex] : 0);
                    switch (b.Kind)
                    {
                        case ResourceKind.ConstantBuffer:
                        case ResourceKind.StorageBuffer:
                        case ResourceKind.ReadOnlyStorageBuffer:
                            if (vi >= 0)
                            {
                                Msg.Void(_enc, Sel.setVertexBuffer_offset_atIndex_, b.Handles[k], (nuint)offset, (nuint)(vi + k));
                            }

                            if (fi >= 0)
                            {
                                Msg.Void(_enc, Sel.setFragmentBuffer_offset_atIndex_, b.Handles[k], (nuint)offset, (nuint)(fi + k));
                            }

                            break;
                        case ResourceKind.SampledTexture:
                        case ResourceKind.StorageTexture:
                            if (vi >= 0)
                            {
                                Msg.Void(_enc, Sel.setVertexTexture_atIndex_, b.Handles[k], (nuint)(vi + k));
                            }

                            if (fi >= 0)
                            {
                                Msg.Void(_enc, Sel.setFragmentTexture_atIndex_, b.Handles[k], (nuint)(fi + k));
                            }

                            break;
                        default:
                            if (vi >= 0)
                            {
                                Msg.Void(_enc, Sel.setVertexSamplerState_atIndex_, b.Handles[k], (nuint)(vi + k));
                            }

                            if (fi >= 0)
                            {
                                Msg.Void(_enc, Sel.setFragmentSamplerState_atIndex_, b.Handles[k], (nuint)(fi + k));
                            }

                            break;
                    }
                }
            }
        }

        _dirty = 0;
    }

    public override void Draw(uint vertexCount, uint instanceCount = 1, uint firstVertex = 0, uint firstInstance = 0)
    {
        Check();
        Apply();
        Msg.Void(_enc, Sel.drawPrimitives_vertexStart_vertexCount_instanceCount_baseInstance_, _pipeline!.Primitive, (nuint)firstVertex, (nuint)vertexCount, (nuint)instanceCount, (nuint)firstInstance);
    }

    public override void DrawIndexed(uint indexCount, uint instanceCount = 1, uint firstIndex = 0, int baseVertex = 0, uint firstInstance = 0)
    {
        Check();
        if (_indexBuffer == 0)
        {
            throw new InvalidOperationException("set an index buffer before DrawIndexed");
        }

        Apply();
        var size = _indexType == MT.Index16 ? 2ul : 4ul;
        Msg.Void(_enc, Sel.drawIndexedPrimitives_indexCount_indexType_indexBuffer_indexBufferOffset_instanceCount_baseVertex_baseInstance_,
            _pipeline!.Primitive, (nuint)indexCount, _indexType, _indexBuffer, (nuint)(_indexOffset + firstIndex * size), (nuint)instanceCount, (nint)baseVertex, (nuint)firstInstance);
    }

    public override void DrawIndirect(GpuBuffer arguments, ulong offset, uint drawCount = 1) => Indirect(arguments, offset, drawCount, indexed: false);

    public override void DrawIndexedIndirect(GpuBuffer arguments, ulong offset, uint drawCount = 1) => Indirect(arguments, offset, drawCount, indexed: true);

    void Indirect(GpuBuffer arguments, ulong offset, uint count, bool indexed)
    {
        Check();
        var b = arguments as MetalBuffer ?? throw new ArgumentException("buffer belongs to another backend", nameof(arguments));
        var stride = (ulong)(indexed ? DrawIndexedIndirectArgs.Size : DrawIndirectArgs.Size);
        if ((b.Usage & BufferUsage.Indirect) == 0)
        {
            throw new ArgumentException("buffer lacks BufferUsage.Indirect", nameof(arguments));
        }

        if (offset % 4 != 0 || offset + count * stride > b.Size)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "indirect arguments must be 4-byte aligned and lie inside the buffer");
        }

        if (indexed && _indexBuffer == 0)
        {
            throw new InvalidOperationException("set an index buffer before DrawIndexedIndirect");
        }

        Apply();
        for (uint i = 0; i < count; i++)
        {
            var o = (nuint)(offset + i * stride);
            if (indexed)
            {
                Msg.Void(_enc, Sel.drawIndexedPrimitives_indexType_indexBuffer_indexBufferOffset_indirectBuffer_indirectBufferOffset_,
                    _pipeline!.Primitive, _indexType, _indexBuffer, (nuint)_indexOffset, b.Handle, o);
            }
            else
            {
                Msg.Void(_enc, Sel.drawPrimitives_indirectBuffer_indirectBufferOffset_, _pipeline!.Primitive, b.Handle, o);
            }
        }
    }

    public override void EndPass()
    {
        if (_ended)
        {
            return;
        }

        _ended = true;
        Msg.Void(_enc, Sel.endEncoding);
        Objc.Release(ref _enc);
        _owner.PassEnded();
    }
}

internal sealed unsafe class MetalComputePass : GpuComputePass
{
    readonly MetalEncoder _owner;
    nint _enc;
    MetalComputePipeline? _pipeline;
    readonly MetalBindGroup?[] _groups = new MetalBindGroup?[BindingRules.MaxSets];
    readonly uint[] _offsets = new uint[BindingRules.MaxSets * BindingRules.MaxDynamic];
    uint _dirty;
    bool _ended;

    internal MetalComputePass(MetalEncoder owner) => _owner = owner;

    internal void Begin(nint encoder)
    {
        _enc = encoder;
        _pipeline = null;
        Array.Clear(_groups);
        _dirty = 0;
        _push.Dirty = false;
        _ended = false;
    }

    void Check()
    {
        if (_ended)
        {
            throw new InvalidOperationException("the compute pass has ended");
        }
    }

    public override void SetPipeline(GpuComputePipeline pipeline)
    {
        Check();
        var p = pipeline as MetalComputePipeline ?? throw new ArgumentException("pipeline belongs to another backend", nameof(pipeline));
        _pipeline = p;
        Msg.Void(_enc, Sel.setComputePipelineState_, p.State);
        _dirty = 0xFF;
        _push.Dirty = _push.Bytes is not null;
    }

    public override void SetBindGroup(uint setIndex, GpuBindGroup group, ReadOnlySpan<uint> dynamicOffsets)
    {
        Check();
        BindingRules.CheckGroup(group, setIndex, dynamicOffsets, out var g);
        _groups[setIndex] = g;
        dynamicOffsets.CopyTo(_offsets.AsSpan((int)setIndex * BindingRules.MaxDynamic));
        _dirty |= 1u << (int)setIndex;
    }

    PushBlock _push;

    public override void SetPushConstants(uint offset, ReadOnlySpan<byte> data)
    {
        Check();
        _push.Set(_pipeline?.Layout, offset, data);
    }

    void Apply()
    {
        var p = _pipeline ?? throw new InvalidOperationException("set a pipeline before dispatching");
        if (_push.Dirty && p.Layout.Push.Size > 0 && (p.Layout.Push.Stages & ShaderStage.Compute) != 0 && p.PushIndex >= 0)
        {
            fixed (byte* bytes = _push.Bytes)
            {
                Msg.Void(_enc, Sel.setBytes_length_atIndex_, (nint)bytes, (nuint)p.Layout.Push.Size, (nuint)p.PushIndex);
            }

            _push.Dirty = false;
        }

        if (_dirty == 0)
        {
            return;
        }

        for (var s = 0; s < BindingRules.MaxSets; s++)
        {
            if ((_dirty & (1u << s)) == 0 || _groups[s] is not { } g)
            {
                continue;
            }

            var offsets = _offsets.AsSpan(s * BindingRules.MaxDynamic, BindingRules.MaxDynamic);
            foreach (var b in g.Bindings)
            {
                var index = (b.Stages & ShaderStage.Compute) != 0 ? p.Slots.Lookup((uint)s, b.Binding) : -1;
                if (index < 0)
                {
                    continue;
                }

                for (var k = 0; k < b.Handles.Length; k++)
                {
                    switch (b.Kind)
                    {
                        case ResourceKind.ConstantBuffer:
                        case ResourceKind.StorageBuffer:
                        case ResourceKind.ReadOnlyStorageBuffer:
                            Msg.Void(_enc, Sel.setBuffer_offset_atIndex_, b.Handles[k], (nuint)(b.Offsets[k] + (b.DynamicIndex >= 0 ? offsets[b.DynamicIndex] : 0)), (nuint)(index + k));
                            break;
                        case ResourceKind.SampledTexture:
                        case ResourceKind.StorageTexture:
                            Msg.Void(_enc, Sel.setTexture_atIndex_, b.Handles[k], (nuint)(index + k));
                            break;
                        default:
                            Msg.Void(_enc, Sel.setSamplerState_atIndex_, b.Handles[k], (nuint)(index + k));
                            break;
                    }
                }
            }
        }

        _dirty = 0;
    }

    public override void Dispatch(uint x, uint y = 1, uint z = 1)
    {
        Check();
        Apply();
        var g = _pipeline!.Group;
        if (x == 0 || y == 0 || z == 0)
        {
            return;
        }

        Msg.Void(_enc, Sel.dispatchThreadgroups_threadsPerThreadgroup_, new MTLSize(x, y, z), new MTLSize(g.X, g.Y, g.Z));
    }

    public override void DispatchIndirect(GpuBuffer arguments, ulong offset)
    {
        Check();
        var b = arguments as MetalBuffer ?? throw new ArgumentException("buffer belongs to another backend", nameof(arguments));
        if ((b.Usage & BufferUsage.Indirect) == 0)
        {
            throw new ArgumentException("buffer lacks BufferUsage.Indirect", nameof(arguments));
        }

        if (offset % 4 != 0 || offset + DispatchIndirectArgs.Size > b.Size)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "indirect arguments must be 4-byte aligned and lie inside the buffer");
        }

        Apply();
        var g = _pipeline!.Group;
        Msg.Void(_enc, Sel.dispatchThreadgroupsWithIndirectBuffer_indirectBufferOffset_threadsPerThreadgroup_, b.Handle, (nuint)offset, new MTLSize(g.X, g.Y, g.Z));
    }

    public override void EndPass()
    {
        if (_ended)
        {
            return;
        }

        _ended = true;
        Msg.Void(_enc, Sel.endEncoding);
        Objc.Release(ref _enc);
        _owner.PassEnded();
    }
}
