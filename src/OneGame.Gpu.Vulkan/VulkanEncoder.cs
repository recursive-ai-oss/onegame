using OneGame.Gpu.Vulkan.Interop;

namespace OneGame.Gpu.Vulkan;

/// <summary>
/// Records commands into a pool of Vulkan command buffers. A render pass uses two buffers: one for the barriers that must precede
/// it (they cannot be recorded inside a pass) and one for the pass itself; they are submitted in that order.
/// </summary>
internal sealed unsafe class VulkanEncoder : GpuCommandEncoder
{
    readonly VulkanDevice _dev;
    VkCommandPool _pool;
    readonly List<VkCommandBuffer> _all = [];
    int _used;
    VkCommandBuffer _cur;
    bool _curOpen;
    bool _finished;
    readonly List<VulkanTexture> _swapTouched = [];
    readonly VulkanRenderPass _render;
    readonly VulkanComputePass _compute;
    internal bool InPass;

    /// <summary>Command buffers to submit, in order.</summary>
    internal readonly List<VkCommandBuffer> Done = [];

    internal VulkanDevice Device => _dev;

    public VulkanEncoder(VulkanDevice dev)
    {
        _dev = dev;
        var ci = new VkCommandPoolCreateInfo { queueFamilyIndex = dev.QueueFamily };
        VkCommandPool pool;
        dev.Check(dev.Fns.vkCreateCommandPool(dev.Handle, &ci, null, &pool), "vkCreateCommandPool");
        _pool = pool;
        _render = new VulkanRenderPass(this);
        _compute = new VulkanComputePass(this);
    }

    VkCommandBuffer NextBuffer(bool begin = true)
    {
        VkCommandBuffer cb;
        if (_used < _all.Count)
        {
            cb = _all[_used];
        }
        else
        {
            var ai = new VkCommandBufferAllocateInfo { commandPool = _pool, level = VkCommandBufferLevel.Primary, commandBufferCount = 1 };
            _dev.Check(_dev.Fns.vkAllocateCommandBuffers(_dev.Handle, &ai, &cb), "vkAllocateCommandBuffers");
            _all.Add(cb);
        }

        _used++;
        if (begin)
        {
            Begin(cb);
        }

        return cb;
    }

    void Begin(VkCommandBuffer cb)
    {
        var bi = new VkCommandBufferBeginInfo { flags = VkCommandBufferUsageFlagBits.OneTimeSubmit };
        _dev.Check(_dev.Fns.vkBeginCommandBuffer(cb, &bi), "vkBeginCommandBuffer");
    }

    void End(VkCommandBuffer cb) => _dev.Check(_dev.Fns.vkEndCommandBuffer(cb), "vkEndCommandBuffer");

    /// <summary>The buffer for work outside render passes, opened on demand.</summary>
    internal VkCommandBuffer Current
    {
        get
        {
            if (_finished)
            {
                throw new InvalidOperationException("this encoder was already submitted; call BeginCommands for a new one");
            }

            if (!_curOpen)
            {
                _cur = NextBuffer();
                _curOpen = true;
            }

            return _cur;
        }
    }

    void CloseCurrent()
    {
        if (_curOpen)
        {
            End(_cur);
            Done.Add(_cur);
            _curOpen = false;
        }
    }

    void EnsureNoPass()
    {
        if (InPass)
        {
            throw new InvalidOperationException("a render or compute pass is open; call End() on it first");
        }
    }

    internal void Finish()
    {
        EnsureNoPass();
        if (_finished)
        {
            throw new InvalidOperationException("this encoder was already submitted");
        }

        // Backbuffers go back to the presentation layout once all drawing is recorded.
        foreach (var t in _swapTouched)
        {
            Barrier(t, Use.Present);
        }

        CloseCurrent();
        _finished = true;
    }

    internal void Reset()
    {
        _dev.Check(_dev.Fns.vkResetCommandPool(_dev.Handle, _pool, 0), "vkResetCommandPool");
        _used = 0;
        Done.Clear();
        _swapTouched.Clear();
        _curOpen = false;
        _finished = false;
        InPass = false;
    }

    internal void Destroy()
    {
        if (_pool.Value != 0)
        {
            _dev.Fns.vkDestroyCommandPool(_dev.Handle, _pool, null);
            _pool = default;
        }
    }

    // ---- barriers ----

    internal void Barrier(VulkanTexture t, Use use, VkCommandBuffer? into = null)
    {
        if (t.IsSwapchainImage && !_swapTouched.Contains(t))
        {
            _swapTouched.Add(t);
        }

        if (!t.Sync.Transition(use, out var ss, out var sa, out var ds, out var da, out var ol, out var nl))
        {
            return;
        }

        var b = new VkImageMemoryBarrier2
        {
            srcStageMask = ss,
            srcAccessMask = sa,
            dstStageMask = ds,
            dstAccessMask = da,
            oldLayout = ol,
            newLayout = nl,
            srcQueueFamilyIndex = VulkanDevice.QueueFamilyIgnored,
            dstQueueFamilyIndex = VulkanDevice.QueueFamilyIgnored,
            image = t.Handle,
            subresourceRange = new VkImageSubresourceRange { aspectMask = t.Aspect, baseMipLevel = 0, levelCount = t.MipLevels, baseArrayLayer = 0, layerCount = t.ArrayLayers },
        };
        var dep = new VkDependencyInfo { imageMemoryBarrierCount = 1, pImageMemoryBarriers = &b };
        _dev.Fns.vkCmdPipelineBarrier2(into ?? Current, &dep);
    }

    internal void Barrier(VulkanBuffer buf, Use use, VkCommandBuffer? into = null)
    {
        if (!buf.Sync.Transition(use, out var ss, out var sa, out var ds, out var da, out _, out _))
        {
            return;
        }

        var b = new VkBufferMemoryBarrier2
        {
            srcStageMask = ss,
            srcAccessMask = sa,
            dstStageMask = ds,
            dstAccessMask = da,
            srcQueueFamilyIndex = VulkanDevice.QueueFamilyIgnored,
            dstQueueFamilyIndex = VulkanDevice.QueueFamilyIgnored,
            buffer = buf.Handle,
            offset = 0,
            size = VulkanDevice.WholeSize,
        };
        var dep = new VkDependencyInfo { bufferMemoryBarrierCount = 1, pBufferMemoryBarriers = &b };
        _dev.Fns.vkCmdPipelineBarrier2(into ?? Current, &dep);
    }

    internal void SyncGroup(VulkanBindGroup g, VkCommandBuffer into)
    {
        foreach (var u in g.Uses)
        {
            if (u.Texture is not null)
            {
                Barrier(u.Texture, u.Use, into);
            }
            else if (u.Buffer is not null)
            {
                Barrier(u.Buffer, u.Use, into);
            }
        }
    }

    // ---- copies ----

    static VkImageSubresourceLayers Layers(VulkanTexture t, uint mip, uint layer) =>
        new() { aspectMask = t.Aspect, mipLevel = mip, baseArrayLayer = layer, layerCount = 1 };

    static VkExtent3D LevelExtent(VulkanTexture t, uint mip) =>
        new() { width = Math.Max(t.Width >> (int)mip, 1), height = Math.Max(t.Height >> (int)mip, 1), depth = 1 };

    public override void CopyBuffer(GpuBuffer source, ulong sourceOffset, GpuBuffer destination, ulong destinationOffset, ulong size)
    {
        EnsureNoPass();
        var s = (VulkanBuffer)source;
        var d = (VulkanBuffer)destination;
        if ((s.Usage & BufferUsage.CopySource) == 0 || (d.Usage & BufferUsage.CopyDestination) == 0)
        {
            throw new ArgumentException("copy needs CopySource on the source and CopyDestination on the destination buffer");
        }

        if (sourceOffset + size > s.Size || destinationOffset + size > d.Size)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "copy extends past the end of a buffer");
        }

        Barrier(s, Use.CopySource);
        Barrier(d, Use.CopyDestination);
        var region = new VkBufferCopy { srcOffset = sourceOffset, dstOffset = destinationOffset, size = size };
        _dev.Fns.vkCmdCopyBuffer(Current, s.Handle, d.Handle, 1, &region);
    }

    internal void CopyRawToBuffer(VkBuffer staging, VulkanBuffer dst, ulong dstOffset, ulong size)
    {
        Barrier(dst, Use.CopyDestination);
        var region = new VkBufferCopy { srcOffset = 0, dstOffset = dstOffset, size = size };
        _dev.Fns.vkCmdCopyBuffer(Current, staging, dst.Handle, 1, &region);
    }

    internal void CopyBufferToRaw(VulkanBuffer src, ulong srcOffset, VkBuffer staging, ulong size)
    {
        Barrier(src, Use.CopySource);
        var region = new VkBufferCopy { srcOffset = srcOffset, dstOffset = 0, size = size };
        _dev.Fns.vkCmdCopyBuffer(Current, src.Handle, staging, 1, &region);
    }

    internal void CopyRawToTexture(VkBuffer staging, VulkanTexture dst, uint mip, uint layer)
    {
        Barrier(dst, Use.CopyDestination);
        var region = new VkBufferImageCopy { bufferOffset = 0, imageSubresource = Layers(dst, mip, layer), imageExtent = LevelExtent(dst, mip) };
        _dev.Fns.vkCmdCopyBufferToImage(Current, staging, dst.Handle, VkImageLayout.TransferDstOptimal, 1, &region);
    }

    internal void CopyTextureToRaw(VulkanTexture src, uint mip, uint layer, VkBuffer staging)
    {
        Barrier(src, Use.CopySource);
        var region = new VkBufferImageCopy { bufferOffset = 0, imageSubresource = Layers(src, mip, layer), imageExtent = LevelExtent(src, mip) };
        _dev.Fns.vkCmdCopyImageToBuffer(Current, src.Handle, VkImageLayout.TransferSrcOptimal, staging, 1, &region);
    }

    public override void CopyTextureToBuffer(GpuTexture source, uint mipLevel, uint layer, GpuBuffer destination, ulong destinationOffset)
    {
        EnsureNoPass();
        var t = (VulkanTexture)source;
        var d = (VulkanBuffer)destination;
        if ((t.Usage & TextureUsage.CopySource) == 0 || (d.Usage & BufferUsage.CopyDestination) == 0)
        {
            throw new ArgumentException("copy needs CopySource on the texture and CopyDestination on the buffer");
        }

        Barrier(t, Use.CopySource);
        Barrier(d, Use.CopyDestination);
        var region = new VkBufferImageCopy { bufferOffset = destinationOffset, imageSubresource = Layers(t, mipLevel, layer), imageExtent = LevelExtent(t, mipLevel) };
        _dev.Fns.vkCmdCopyImageToBuffer(Current, t.Handle, VkImageLayout.TransferSrcOptimal, d.Handle, 1, &region);
    }

    public override void CopyBufferToTexture(GpuBuffer source, ulong sourceOffset, GpuTexture destination, uint mipLevel, uint layer)
    {
        EnsureNoPass();
        var s = (VulkanBuffer)source;
        var t = (VulkanTexture)destination;
        if ((s.Usage & BufferUsage.CopySource) == 0 || (t.Usage & TextureUsage.CopyDestination) == 0)
        {
            throw new ArgumentException("copy needs CopySource on the buffer and CopyDestination on the texture");
        }

        Barrier(s, Use.CopySource);
        Barrier(t, Use.CopyDestination);
        var region = new VkBufferImageCopy { bufferOffset = sourceOffset, imageSubresource = Layers(t, mipLevel, layer), imageExtent = LevelExtent(t, mipLevel) };
        _dev.Fns.vkCmdCopyBufferToImage(Current, s.Handle, t.Handle, VkImageLayout.TransferDstOptimal, 1, &region);
    }

    public override void GenerateMips(GpuTexture texture)
    {
        EnsureNoPass();
        var t = (VulkanTexture)texture;
        if (t.MipLevels < 2)
        {
            return;
        }

        if (VkMap.IsDepth(t.Format))
        {
            throw new ArgumentException("depth textures cannot generate mips", nameof(texture));
        }

        // All levels start in the transfer-destination layout; each level is flipped to source just before it feeds the next.
        Barrier(t, Use.CopyDestination);
        var cb = Current;
        for (var level = 1u; level < t.MipLevels; level++)
        {
            LevelBarrier(cb, t, level - 1);
            var blit = new VkImageBlit
            {
                srcSubresource = new VkImageSubresourceLayers { aspectMask = t.Aspect, mipLevel = level - 1, baseArrayLayer = 0, layerCount = t.ArrayLayers },
                dstSubresource = new VkImageSubresourceLayers { aspectMask = t.Aspect, mipLevel = level, baseArrayLayer = 0, layerCount = t.ArrayLayers },
            };
            var se = LevelExtent(t, level - 1);
            var de = LevelExtent(t, level);
            blit.srcOffsets[1] = new VkOffset3D { x = (int)se.width, y = (int)se.height, z = 1 };
            blit.dstOffsets[1] = new VkOffset3D { x = (int)de.width, y = (int)de.height, z = 1 };
            _dev.Fns.vkCmdBlitImage(cb, t.Handle, VkImageLayout.TransferSrcOptimal, t.Handle, VkImageLayout.TransferDstOptimal, 1, &blit, VkFilter.Linear);
        }

        LevelBarrier(cb, t, t.MipLevels - 1);

        // The whole image is now a transfer source, last written by the blits.
        t.Sync = new SyncState
        {
            Layout = VkImageLayout.TransferSrcOptimal,
            WriteStage = VkPipelineStageFlagBits2.AllTransfer,
            WriteAccess = VkAccessFlagBits2.TransferWrite,
            ReadStages = VkPipelineStageFlagBits2.None,
        };
    }

    void LevelBarrier(VkCommandBuffer cb, VulkanTexture t, uint level)
    {
        var b = new VkImageMemoryBarrier2
        {
            srcStageMask = VkPipelineStageFlagBits2.AllTransfer,
            srcAccessMask = VkAccessFlagBits2.TransferWrite,
            dstStageMask = VkPipelineStageFlagBits2.AllTransfer,
            dstAccessMask = VkAccessFlagBits2.TransferRead,
            oldLayout = VkImageLayout.TransferDstOptimal,
            newLayout = VkImageLayout.TransferSrcOptimal,
            srcQueueFamilyIndex = VulkanDevice.QueueFamilyIgnored,
            dstQueueFamilyIndex = VulkanDevice.QueueFamilyIgnored,
            image = t.Handle,
            subresourceRange = new VkImageSubresourceRange { aspectMask = t.Aspect, baseMipLevel = level, levelCount = 1, baseArrayLayer = 0, layerCount = t.ArrayLayers },
        };
        var dep = new VkDependencyInfo { imageMemoryBarrierCount = 1, pImageMemoryBarriers = &b };
        _dev.Fns.vkCmdPipelineBarrier2(cb, &dep);
    }

    // ---- passes ----

    public override GpuRenderPass BeginRenderPass(ReadOnlySpan<ColorAttachment> colors, DepthAttachment? depth = null)
    {
        EnsureNoPass();
        if (_finished)
        {
            throw new InvalidOperationException("this encoder was already submitted");
        }

        if (colors.Length == 0 && depth is null)
        {
            throw new ArgumentException("a render pass needs at least one attachment");
        }

        if (colors.Length > 8)
        {
            throw new ArgumentException("at most 8 color attachments", nameof(colors));
        }

        CloseCurrent();
        var pre = NextBuffer();
        var pass = NextBuffer();
        _render.Begin(pre, pass);
        InPass = true;

        VulkanTexture? first = null;
        uint mip0 = 0;
        var infos = stackalloc VkRenderingAttachmentInfo[8];
        for (var i = 0; i < colors.Length; i++)
        {
            ref readonly var c = ref colors[i];
            var t = (VulkanTexture)c.Target;
            if ((t.Usage & TextureUsage.RenderTarget) == 0)
            {
                throw new ArgumentException("attachment texture lacks TextureUsage.RenderTarget");
            }

            Barrier(t, Use.ColorAttachment, pre);
            var info = new VkRenderingAttachmentInfo
            {
                imageView = t.AttachmentView(c.MipLevel, c.Layer),
                imageLayout = VkImageLayout.ColorAttachmentOptimal,
                loadOp = LoadOpOf(c.Load),
                storeOp = c.Store == StoreOp.Store ? VkAttachmentStoreOp.Store : VkAttachmentStoreOp.DontCare,
            };
            info.clearValue.color.float32[0] = c.ClearR;
            info.clearValue.color.float32[1] = c.ClearG;
            info.clearValue.color.float32[2] = c.ClearB;
            info.clearValue.color.float32[3] = c.ClearA;
            if (c.Resolve is VulkanTexture r)
            {
                Barrier(r, Use.ColorAttachment, pre);
                info.resolveMode = VkResolveModeFlagBits.Average;
                info.resolveImageView = r.AttachmentView(c.MipLevel, c.Layer);
                info.resolveImageLayout = VkImageLayout.ColorAttachmentOptimal;
            }

            infos[i] = info;
            if (first is null)
            {
                first = t;
                mip0 = c.MipLevel;
            }
        }

        VkRenderingAttachmentInfo depthInfo = default;
        var hasDepth = false;
        var stencil = false;
        if (depth is { } d)
        {
            var t = (VulkanTexture)d.Target;
            if ((t.Usage & TextureUsage.RenderTarget) == 0 || !VkMap.IsDepth(t.Format))
            {
                throw new ArgumentException("depth attachment must be a depth texture with TextureUsage.RenderTarget");
            }

            Barrier(t, Use.DepthAttachment, pre);
            depthInfo = new VkRenderingAttachmentInfo
            {
                imageView = t.AttachmentView(0, 0),
                imageLayout = VkImageLayout.DepthStencilAttachmentOptimal,
                loadOp = LoadOpOf(d.Load),
                storeOp = d.Store == StoreOp.Store ? VkAttachmentStoreOp.Store : VkAttachmentStoreOp.DontCare,
            };
            depthInfo.clearValue.depthStencil.depth = d.ClearDepth;
            hasDepth = true;
            stencil = t.Format == GpuFormat.Depth32FloatStencil8;
            first ??= t;
        }

        var target = first!;
        var area = LevelExtent(target, mip0);
        var ri = new VkRenderingInfo
        {
            renderArea = new VkRect2D { offset = default, extent = new VkExtent2D { width = area.width, height = area.height } },
            layerCount = 1,
            colorAttachmentCount = (uint)colors.Length,
            pColorAttachments = infos,
            pDepthAttachment = hasDepth ? &depthInfo : null,
            pStencilAttachment = hasDepth && stencil ? &depthInfo : null,
        };
        _dev.Fns.vkCmdBeginRendering(pass, &ri);
        _render.SetViewport(0, 0, area.width, area.height);
        _render.SetScissor(0, 0, area.width, area.height);
        return _render;
    }

    static VkAttachmentLoadOp LoadOpOf(LoadOp op) => op switch
    {
        LoadOp.Load => VkAttachmentLoadOp.Load,
        LoadOp.Clear => VkAttachmentLoadOp.Clear,
        _ => VkAttachmentLoadOp.DontCare,
    };

    internal void EndRender(VkCommandBuffer pre, VkCommandBuffer pass)
    {
        _dev.Fns.vkCmdEndRendering(pass);
        End(pre);
        End(pass);
        Done.Add(pre);
        Done.Add(pass);
        InPass = false;
    }

    public override GpuComputePass BeginComputePass()
    {
        EnsureNoPass();
        _compute.Begin(Current);
        InPass = true;
        return _compute;
    }

    internal void EndCompute() => InPass = false;
}

internal sealed unsafe class VulkanRenderPass : GpuRenderPass
{
    readonly VulkanEncoder _enc;
    VkCommandBuffer _pre, _pass;
    bool _open;

    public VulkanRenderPass(VulkanEncoder enc) => _enc = enc;

    public void Begin(VkCommandBuffer pre, VkCommandBuffer pass)
    {
        _pre = pre;
        _pass = pass;
        _open = true;
    }

    void Check()
    {
        if (!_open)
        {
            throw new InvalidOperationException("the render pass has ended");
        }
    }

    VulkanDevice Dev => _enc.Device;

    public override void SetPipeline(GpuGraphicsPipeline pipeline)
    {
        Check();
        Dev.Fns.vkCmdBindPipeline(_pass, VkPipelineBindPoint.Graphics, ((VulkanGraphicsPipeline)pipeline).Handle);
    }

    public override void SetBindGroup(uint setIndex, GpuBindGroup group)
    {
        Check();
        var g = (VulkanBindGroup)group;
        if (g.Set != setIndex)
        {
            throw new ArgumentException($"bind group was created for set {g.Set}, not {setIndex}", nameof(group));
        }

        _enc.SyncGroup(g, _pre);
        var h = g.Handle;
        Dev.Fns.vkCmdBindDescriptorSets(_pass, VkPipelineBindPoint.Graphics, g.Layout.Handle, setIndex, 1, &h, 0, null);
    }

    public override void SetVertexBuffer(uint slot, GpuBuffer buffer, ulong offset = 0)
    {
        Check();
        var b = (VulkanBuffer)buffer;
        if ((b.Usage & BufferUsage.Vertex) == 0)
        {
            throw new ArgumentException("buffer lacks BufferUsage.Vertex", nameof(buffer));
        }

        _enc.Barrier(b, Use.VertexBuffer, _pre);
        var h = b.Handle;
        Dev.Fns.vkCmdBindVertexBuffers(_pass, slot, 1, &h, &offset);
    }

    public override void SetIndexBuffer(GpuBuffer buffer, IndexFormat format, ulong offset = 0)
    {
        Check();
        var b = (VulkanBuffer)buffer;
        if ((b.Usage & BufferUsage.Index) == 0)
        {
            throw new ArgumentException("buffer lacks BufferUsage.Index", nameof(buffer));
        }

        _enc.Barrier(b, Use.IndexBuffer, _pre);
        Dev.Fns.vkCmdBindIndexBuffer(_pass, b.Handle, offset, format == IndexFormat.Index16 ? VkIndexType.Uint16 : VkIndexType.Uint32);
    }

    public override void SetViewport(float x, float y, float width, float height, float minDepth = 0, float maxDepth = 1)
    {
        Check();
        // A negative height flips Y (Vulkan 1.1 core, maintenance1) so clip space is +Y up like Metal and WebGPU.
        var vp = new VkViewport { x = x, y = y + height, width = width, height = -height, minDepth = minDepth, maxDepth = maxDepth };
        Dev.Fns.vkCmdSetViewport(_pass, 0, 1, &vp);
    }

    public override void SetScissor(int x, int y, uint width, uint height)
    {
        Check();
        var r = new VkRect2D { offset = new VkOffset2D { x = x, y = y }, extent = new VkExtent2D { width = width, height = height } };
        Dev.Fns.vkCmdSetScissor(_pass, 0, 1, &r);
    }

    public override void Draw(uint vertexCount, uint instanceCount = 1, uint firstVertex = 0, uint firstInstance = 0)
    {
        Check();
        Dev.Fns.vkCmdDraw(_pass, vertexCount, instanceCount, firstVertex, firstInstance);
    }

    public override void DrawIndexed(uint indexCount, uint instanceCount = 1, uint firstIndex = 0, int baseVertex = 0, uint firstInstance = 0)
    {
        Check();
        Dev.Fns.vkCmdDrawIndexed(_pass, indexCount, instanceCount, firstIndex, baseVertex, firstInstance);
    }

    public override void EndPass()
    {
        Check();
        _open = false;
        _enc.EndRender(_pre, _pass);
    }
}

internal sealed unsafe class VulkanComputePass : GpuComputePass
{
    readonly VulkanEncoder _enc;
    VkCommandBuffer _cb;
    bool _open;
    readonly VulkanBindGroup?[] _bound = new VulkanBindGroup?[8];

    public VulkanComputePass(VulkanEncoder enc) => _enc = enc;

    public void Begin(VkCommandBuffer cb)
    {
        _cb = cb;
        _open = true;
        Array.Clear(_bound);
    }

    void Check()
    {
        if (!_open)
        {
            throw new InvalidOperationException("the compute pass has ended");
        }
    }

    public override void SetPipeline(GpuComputePipeline pipeline)
    {
        Check();
        _enc.Device.Fns.vkCmdBindPipeline(_cb, VkPipelineBindPoint.Compute, ((VulkanComputePipeline)pipeline).Handle);
    }

    public override void SetBindGroup(uint setIndex, GpuBindGroup group)
    {
        Check();
        var g = (VulkanBindGroup)group;
        if (g.Set != setIndex || setIndex >= _bound.Length)
        {
            throw new ArgumentException($"bind group was created for set {g.Set}, not {setIndex} (or the set index is above 7)", nameof(group));
        }

        _bound[setIndex] = g;
        var h = g.Handle;
        _enc.Device.Fns.vkCmdBindDescriptorSets(_cb, VkPipelineBindPoint.Compute, g.Layout.Handle, setIndex, 1, &h, 0, null);
    }

    public override void Dispatch(uint x, uint y = 1, uint z = 1)
    {
        Check();
        // Earlier dispatches may have written what this one reads; barriers go between dispatches, outside any render pass.
        foreach (var g in _bound)
        {
            if (g is not null)
            {
                _enc.SyncGroup(g, _cb);
            }
        }

        _enc.Device.Fns.vkCmdDispatch(_cb, x, y, z);
    }

    public override void EndPass()
    {
        Check();
        _open = false;
        _enc.EndCompute();
    }
}
