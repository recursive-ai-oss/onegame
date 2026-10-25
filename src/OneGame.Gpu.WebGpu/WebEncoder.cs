namespace OneGame.Gpu.WebGpu;

internal sealed class WebEncoder : GpuCommandEncoder
{
    private readonly WebGpuDevice _device;
    private readonly OpStream _s;
    private bool _finished;
    private bool _passOpen;

    public WebEncoder(WebGpuDevice device)
    {
        _device = device;
        _s = device.RentStream();
        _s.U(Op.BeginEncoder);
    }

    internal void PassEnded() => _passOpen = false;

    private void Idle()
    {
        if (_finished)
        {
            throw new InvalidOperationException("this encoder was already submitted; call BeginCommands for a new one");
        }

        if (_passOpen)
        {
            throw new InvalidOperationException("a render or compute pass is open; call EndPass() on it first");
        }
    }

    internal void Finish(OpStream target)
    {
        Idle();
        _finished = true;
        _s.U(Op.Submit);
        target.Append(_s);
        _device.ReturnStream(_s);
    }

    /// <summary>
    /// Push constants on WebGPU: the pass keeps the whole block, and each change sends it as a fresh slot of a uniform ring that the
    /// pipeline's push group binds with a dynamic offset. Same range rules as the Vulkan backend.
    /// </summary>
    internal static void Push(OpStream s, WebPipelineLayout? layout, ref byte[]? shadow, uint offset, ReadOnlySpan<byte> data)
    {
        if (layout is null)
        {
            throw new InvalidOperationException("set a pipeline before pushing constants");
        }

        if (offset % 4 != 0 || data.Length % 4 != 0 || (ulong)offset + (ulong)data.Length > layout.PushSize)
        {
            throw new ArgumentOutOfRangeException(nameof(data), $"push constants must be 4-byte aligned and inside the {layout.PushSize} bytes the pipeline layout declared");
        }

        if (data.IsEmpty)
        {
            return;
        }

        // Uniform bindings are at least 16 bytes long.
        var padded = (layout.PushSize + 15) & ~15u;
        if (shadow is null || shadow.Length != padded)
        {
            shadow = new byte[padded];
        }

        data.CopyTo(shadow.AsSpan((int)offset));
        s.U(Op.PushBlock).Bytes(shadow);
    }

    internal static void CheckBindGroup(uint setIndex, WebBindGroup group, ReadOnlySpan<uint> dynamicOffsets)
    {
        if (group.Set != setIndex)
        {
            throw new ArgumentException($"bind group was created for set {group.Set}, not {setIndex}", nameof(group));
        }

        if (dynamicOffsets.Length != group.DynamicCount)
        {
            throw new ArgumentException($"the bind group has {group.DynamicCount} dynamic bindings but {dynamicOffsets.Length} offsets were given", nameof(dynamicOffsets));
        }

        foreach (var o in dynamicOffsets)
        {
            if (o % GpuDevice.DynamicOffsetAlignment != 0)
            {
                throw new ArgumentException($"dynamic offsets are multiples of {GpuDevice.DynamicOffsetAlignment}", nameof(dynamicOffsets));
            }
        }
    }

    internal static void CheckIndirect(GpuBuffer arguments, ulong offset, ulong size)
    {
        if ((arguments.Usage & BufferUsage.Indirect) == 0)
        {
            throw new ArgumentException("buffer lacks BufferUsage.Indirect", nameof(arguments));
        }

        if (offset % 4 != 0 || offset + size > arguments.Size)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "indirect arguments must be 4-byte aligned and lie inside the buffer");
        }
    }

    public override GpuRenderPass BeginRenderPass(ReadOnlySpan<ColorAttachment> colors, DepthAttachment? depth = null)
    {
        Idle();
        if (colors.Length == 0 && depth is null)
        {
            throw new ArgumentException("a render pass needs at least one attachment");
        }

        if (colors.Length > 8)
        {
            throw new ArgumentException("at most 8 color attachments", nameof(colors));
        }

        foreach (var c in colors)
        {
            if ((c.Target.Usage & TextureUsage.RenderTarget) == 0)
            {
                throw new ArgumentException("attachment texture lacks TextureUsage.RenderTarget");
            }

            if (c.Resolve is { } r && (c.Target.SampleCount < 2 || r.SampleCount != 1))
            {
                throw new ArgumentException("Resolve needs a multisampled Target and a single-sample Resolve texture");
            }
        }

        if (depth is { } dd && ((dd.Target.Usage & TextureUsage.RenderTarget) == 0 || dd.Target.Format is not (GpuFormat.Depth32Float or GpuFormat.Depth16Unorm or GpuFormat.Depth32FloatStencil8)))
        {
            throw new ArgumentException("depth attachment must be a depth texture with TextureUsage.RenderTarget");
        }

        _passOpen = true;
        _s.U(Op.BeginRenderPass).U((uint)colors.Length);
        foreach (var c in colors)
        {
            _s.U(((IWebGpuObject)c.Target).Id).U(c.MipLevel).U(c.Layer).U(c.Resolve is { } r ? ((IWebGpuObject)r).Id : 0)
                .U((uint)c.Load).U((uint)c.Store).F(c.ClearR).F(c.ClearG).F(c.ClearB).F(c.ClearA);
        }

        if (depth is { } d)
        {
            _s.U(1u).U(((IWebGpuObject)d.Target).Id).U(d.MipLevel).U(d.Layer).U((uint)d.Load).U((uint)d.Store).F(d.ClearDepth);
        }
        else
        {
            _s.U(0u);
        }

        return new WebRenderPass(this, _s);
    }

    public override GpuComputePass BeginComputePass()
    {
        Idle();
        _passOpen = true;
        _s.U(Op.BeginComputePass);
        return new WebComputePass(this, _s);
    }

    public override void CopyBuffer(GpuBuffer source, ulong sourceOffset, GpuBuffer destination, ulong destinationOffset, ulong size)
    {
        Idle();
        if ((source.Usage & BufferUsage.CopySource) == 0 || (destination.Usage & BufferUsage.CopyDestination) == 0)
        {
            throw new ArgumentException("CopyBuffer needs CopySource on the source and CopyDestination on the destination buffer");
        }

        if (sourceOffset + size > source.Size || destinationOffset + size > destination.Size)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "copy extends past the end of a buffer");
        }

        _s.U(Op.CopyBuffer).U(((WebBuffer)source).Id).U(sourceOffset).U(((WebBuffer)destination).Id).U(destinationOffset).U(size);
    }

    public override void CopyTextureToBuffer(GpuTexture source, uint mipLevel, uint layer, GpuBuffer destination, ulong destinationOffset)
    {
        Idle();
        Validate.Subresource(source, mipLevel, layer);
        _s.U(Op.CopyTextureToBuffer).U(((IWebGpuObject)source).Id).U(mipLevel).U(layer).U(((WebBuffer)destination).Id).U(destinationOffset);
    }

    public override void CopyBufferToTexture(GpuBuffer source, ulong sourceOffset, GpuTexture destination, uint mipLevel, uint layer)
    {
        Idle();
        Validate.Subresource(destination, mipLevel, layer);
        _s.U(Op.CopyBufferToTexture).U(((WebBuffer)source).Id).U(sourceOffset).U(((IWebGpuObject)destination).Id).U(mipLevel).U(layer);
    }

    public override void CopyTexture(GpuTexture source, GpuTexture destination)
    {
        Idle();
        if ((source.Usage & TextureUsage.CopySource) == 0 || (destination.Usage & TextureUsage.CopyDestination) == 0)
        {
            throw new ArgumentException("CopyTexture needs CopySource on the source and CopyDestination on the destination texture");
        }

        if (source.Width != destination.Width || source.Height != destination.Height || source.Format != destination.Format || source.Dimension != destination.Dimension
            || source.MipLevels != destination.MipLevels || source.ArrayLayers != destination.ArrayLayers || source.Depth != destination.Depth
            || source.SampleCount != 1 || destination.SampleCount != 1)
        {
            throw new ArgumentException("textures must match in size, format, dimension, mip levels and layers, and not be multisampled", nameof(destination));
        }

        _s.U(Op.CopyTexture).U(((IWebGpuObject)source).Id).U(((IWebGpuObject)destination).Id);
    }

    public override void GenerateMips(GpuTexture texture)
    {
        Idle();
        if (texture.Format is GpuFormat.Depth32Float or GpuFormat.Depth16Unorm or GpuFormat.Depth32FloatStencil8)
        {
            throw new ArgumentException("depth textures cannot generate mips", nameof(texture));
        }

        _s.U(Op.GenerateMips).U(((IWebGpuObject)texture).Id);
    }

    // The backend reports no timestamp support, so the graph never asks for pass timing.
    public override void BeginPassTiming(in PassTiming timing)
    {
    }

    public override uint EndPassTiming() => 0;
}

internal sealed class WebRenderPass : GpuRenderPass
{
    private readonly WebEncoder _e;
    private readonly OpStream _s;
    private bool _ended;
    private WebPipelineLayout? _layout;
    private byte[]? _push;

    public WebRenderPass(WebEncoder e, OpStream s)
    {
        _e = e;
        _s = s;
    }

    private void Live()
    {
        if (_ended)
        {
            throw new InvalidOperationException("the render pass has ended");
        }
    }

    public override void SetPipeline(GpuGraphicsPipeline pipeline)
    {
        Live();
        _layout = ((WebGraphicsPipeline)pipeline).Layout;
        _s.U(Op.SetPipeline).U(((WebGraphicsPipeline)pipeline).Id);
    }

    public override void SetBindGroup(uint setIndex, GpuBindGroup group, ReadOnlySpan<uint> dynamicOffsets)
    {
        Live();
        WebEncoder.CheckBindGroup(setIndex, (WebBindGroup)group, dynamicOffsets);
        _s.U(Op.SetBindGroup).U(setIndex).U(((WebBindGroup)group).Id).U((uint)dynamicOffsets.Length);
        foreach (var o in dynamicOffsets)
        {
            _s.U(o);
        }
    }

    public override void SetPushConstants(uint offset, ReadOnlySpan<byte> data)
    {
        Live();
        WebEncoder.Push(_s, _layout, ref _push, offset, data);
    }

    public override void SetVertexBuffer(uint slot, GpuBuffer buffer, ulong offset = 0)
    {
        Live();
        if ((buffer.Usage & BufferUsage.Vertex) == 0)
        {
            throw new ArgumentException("buffer lacks BufferUsage.Vertex", nameof(buffer));
        }

        _s.U(Op.SetVertexBuffer).U(slot).U(((WebBuffer)buffer).Id).U(offset);
    }

    public override void SetIndexBuffer(GpuBuffer buffer, IndexFormat format, ulong offset = 0)
    {
        Live();
        if ((buffer.Usage & BufferUsage.Index) == 0)
        {
            throw new ArgumentException("buffer lacks BufferUsage.Index", nameof(buffer));
        }

        _s.U(Op.SetIndexBuffer).U(((WebBuffer)buffer).Id).U((uint)format).U(offset);
    }

    public override void SetViewport(float x, float y, float width, float height, float minDepth = 0, float maxDepth = 1)
    {
        Live();
        _s.U(Op.SetViewport).F(x).F(y).F(width).F(height).F(minDepth).F(maxDepth);
    }

    public override void SetScissor(int x, int y, uint width, uint height)
    {
        Live();
        _s.U(Op.SetScissor).U(x).U(y).U(width).U(height);
    }

    public override void Draw(uint vertexCount, uint instanceCount = 1, uint firstVertex = 0, uint firstInstance = 0)
    {
        Live();
        _s.U(Op.Draw).U(vertexCount).U(instanceCount).U(firstVertex).U(firstInstance);
    }

    public override void DrawIndexed(uint indexCount, uint instanceCount = 1, uint firstIndex = 0, int baseVertex = 0, uint firstInstance = 0)
    {
        Live();
        _s.U(Op.DrawIndexed).U(indexCount).U(instanceCount).U(firstIndex).U(baseVertex).U(firstInstance);
    }

    public override void DrawIndirect(GpuBuffer arguments, ulong offset, uint drawCount = 1)
    {
        Live();
        WebEncoder.CheckIndirect(arguments, offset, DrawIndirectArgs.Size * (ulong)drawCount);
        _s.U(Op.DrawIndirect).U(((WebBuffer)arguments).Id).U(offset).U(drawCount);
    }

    public override void DrawIndexedIndirect(GpuBuffer arguments, ulong offset, uint drawCount = 1)
    {
        Live();
        WebEncoder.CheckIndirect(arguments, offset, DrawIndexedIndirectArgs.Size * (ulong)drawCount);
        _s.U(Op.DrawIndexedIndirect).U(((WebBuffer)arguments).Id).U(offset).U(drawCount);
    }

    public override void EndPass()
    {
        Live();
        _ended = true;
        _e.PassEnded();
        _s.U(Op.EndRenderPass);
    }
}

internal sealed class WebComputePass : GpuComputePass
{
    private readonly WebEncoder _e;
    private readonly OpStream _s;
    private bool _ended;
    private WebPipelineLayout? _layout;
    private byte[]? _push;

    public WebComputePass(WebEncoder e, OpStream s)
    {
        _e = e;
        _s = s;
    }

    private void Live()
    {
        if (_ended)
        {
            throw new InvalidOperationException("the compute pass has ended");
        }
    }

    public override void SetPipeline(GpuComputePipeline pipeline)
    {
        Live();
        _layout = ((WebComputePipeline)pipeline).Layout;
        _s.U(Op.SetPipeline).U(((WebComputePipeline)pipeline).Id);
    }

    public override void SetBindGroup(uint setIndex, GpuBindGroup group, ReadOnlySpan<uint> dynamicOffsets)
    {
        Live();
        WebEncoder.CheckBindGroup(setIndex, (WebBindGroup)group, dynamicOffsets);
        _s.U(Op.SetBindGroup).U(setIndex).U(((WebBindGroup)group).Id).U((uint)dynamicOffsets.Length);
        foreach (var o in dynamicOffsets)
        {
            _s.U(o);
        }
    }

    public override void SetPushConstants(uint offset, ReadOnlySpan<byte> data)
    {
        Live();
        WebEncoder.Push(_s, _layout, ref _push, offset, data);
    }

    public override void Dispatch(uint x, uint y = 1, uint z = 1)
    {
        Live();
        _s.U(Op.Dispatch).U(x).U(y).U(z);
    }

    public override void DispatchIndirect(GpuBuffer arguments, ulong offset)
    {
        Live();
        WebEncoder.CheckIndirect(arguments, offset, DispatchIndirectArgs.Size);
        _s.U(Op.DispatchIndirect).U(((WebBuffer)arguments).Id).U(offset);
    }

    public override void EndPass()
    {
        Live();
        _ended = true;
        _e.PassEnded();
        _s.U(Op.EndComputePass);
    }
}
