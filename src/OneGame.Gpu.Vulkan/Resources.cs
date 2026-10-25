using OneGame.Gpu.Vulkan.Interop;

namespace OneGame.Gpu.Vulkan;

internal sealed unsafe class VulkanBuffer : GpuBuffer, IRetirable
{
    readonly VulkanDevice _dev;
    public readonly VkBuffer Handle;
    MemoryAllocation _alloc;
    public SyncState Sync;
    bool _disposed;

    public VulkanBuffer(VulkanDevice dev, VkBuffer handle, MemoryAllocation alloc, in BufferDesc desc)
    {
        _dev = dev;
        Handle = handle;
        _alloc = alloc;
        Size = desc.Size;
        Usage = desc.Usage;
        Label = desc.Label;
    }

    public override void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _dev.Retire(this);
        }
    }

    public void DestroyNow()
    {
        _dev.Fns.vkDestroyBuffer(_dev.Handle, Handle, null);
        _dev.Memory.Free(_alloc);
    }
}

internal sealed unsafe class VulkanTexture : GpuTexture, IRetirable
{
    readonly VulkanDevice _dev;
    public readonly VkImage Handle;
    public readonly VkImageView View;
    public readonly bool IsSwapchainImage;
    public readonly VkImageAspectFlagBits Aspect;
    public SyncState Sync;
    MemoryAllocation _alloc;
    readonly List<(uint Mip, uint Layer, VkImageView View)> _views = [];
    readonly List<(TextureViewDesc Desc, VkImageView View)> _sampledViews = [];
    bool _disposed;

    public VulkanTexture(VulkanDevice dev, VkImage handle, VkImageView view, MemoryAllocation alloc, in TextureDesc desc, bool swapchainImage)
    {
        _dev = dev;
        Handle = handle;
        View = view;
        _alloc = alloc;
        IsSwapchainImage = swapchainImage;
        Width = desc.Width;
        Height = desc.Height;
        Format = desc.Format;
        Usage = desc.Usage;
        MipLevels = desc.MipLevels;
        ArrayLayers = desc.ArrayLayers;
        SampleCount = desc.SampleCount;
        Dimension = desc.Dimension;
        Depth = desc.Dimension == TextureDimension.D3 ? desc.Depth : 1;
        Label = desc.Label;
        Aspect = VkMap.Aspect(desc.Format);
    }

    /// <summary>The view a shader binds for <paramref name="desc"/>; the whole-texture view when null.</summary>
    public VkImageView ShaderView(TextureViewDesc? desc)
    {
        if (desc is not { } d)
        {
            return View;
        }

        foreach (var v in _sampledViews)
        {
            if (v.Desc == d)
            {
                return v.View;
            }
        }

        var levels = d.MipCount == 0 ? MipLevels - Math.Min(d.BaseMip, MipLevels) : d.MipCount;
        var layers = Dimension == TextureDimension.D3 ? 1u : d.LayerCount == 0 ? ArrayLayers - Math.Min(d.BaseLayer, ArrayLayers) : d.LayerCount;
        if (d.BaseMip + levels > MipLevels || d.BaseLayer + layers > ArrayLayers || levels == 0 || layers == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(desc), "texture view range extends past the texture's mip levels or layers");
        }

        var kind = d.Kind;
        if (kind == TextureViewKind.Auto)
        {
            kind = Dimension switch
            {
                TextureDimension.D3 => TextureViewKind.D3,
                TextureDimension.Cube => layers == 1 ? TextureViewKind.D2 : layers == 6 ? TextureViewKind.Cube : layers % 6 == 0 ? TextureViewKind.CubeArray : TextureViewKind.D2Array,
                _ => layers == 1 ? TextureViewKind.D2 : TextureViewKind.D2Array,
            };
        }

        if (kind == TextureViewKind.D3 != (Dimension == TextureDimension.D3))
        {
            throw new ArgumentException("only 3D textures have 3D views, and 3D textures only have 3D views", nameof(desc));
        }

        if ((kind == TextureViewKind.Cube && layers != 6) || (kind == TextureViewKind.CubeArray && (layers % 6 != 0 || layers == 0)) || (kind == TextureViewKind.D2 && layers != 1))
        {
            throw new ArgumentException($"a {kind} view cannot show {layers} layers", nameof(desc));
        }

        if (kind is TextureViewKind.Cube or TextureViewKind.CubeArray && Dimension != TextureDimension.Cube)
        {
            throw new ArgumentException("cube views need a cube texture", nameof(desc));
        }

        var aspect = (Usage & TextureUsage.Sampled) != 0 && VkMap.IsDepth(Format) ? VkImageAspectFlagBits.Depth : Aspect;
        var ci = new VkImageViewCreateInfo
        {
            image = Handle,
            viewType = VkMap.ViewType(kind),
            format = VkMap.Format(Format),
            subresourceRange = new VkImageSubresourceRange { aspectMask = aspect, baseMipLevel = d.BaseMip, levelCount = levels, baseArrayLayer = d.BaseLayer, layerCount = layers },
        };
        VkImageView view;
        _dev.Check(_dev.Fns.vkCreateImageView(_dev.Handle, &ci, null, &view), "vkCreateImageView");
        _sampledViews.Add((d, view));
        return view;
    }

    /// <summary>A view of a single mip level and layer, for rendering into.</summary>
    public VkImageView AttachmentView(uint mip, uint layer)
    {
        // The whole-image view can stand in only when it covers a single mip: an attachment view must have levelCount 1
        // (VUID-VkRenderingAttachmentInfo-imageView-12486), so a texture with a mip chain gets its own view of mip 0.
        if (mip == 0 && layer == 0 && ArrayLayers == 1 && MipLevels == 1 && Dimension == TextureDimension.D2)
        {
            return View;
        }

        if (Dimension == TextureDimension.D3)
        {
            throw new ArgumentException("3D textures cannot be render targets");
        }

        foreach (var v in _views)
        {
            if (v.Mip == mip && v.Layer == layer)
            {
                return v.View;
            }
        }

        var ci = new VkImageViewCreateInfo
        {
            image = Handle,
            viewType = VkImageViewType._2D,
            format = VkMap.Format(Format),
            subresourceRange = new VkImageSubresourceRange { aspectMask = Aspect, baseMipLevel = mip, levelCount = 1, baseArrayLayer = layer, layerCount = 1 },
        };
        VkImageView view;
        _dev.Check(_dev.Fns.vkCreateImageView(_dev.Handle, &ci, null, &view), "vkCreateImageView");
        _views.Add((mip, layer, view));
        return view;
    }

    public override void Dispose()
    {
        if (!_disposed && !IsSwapchainImage)
        {
            _disposed = true;
            _dev.Retire(this);
        }
    }

    public void DestroyNow()
    {
        foreach (var v in _views)
        {
            _dev.Fns.vkDestroyImageView(_dev.Handle, v.View, null);
        }

        foreach (var v in _sampledViews)
        {
            _dev.Fns.vkDestroyImageView(_dev.Handle, v.View, null);
        }

        DestroyView();
        if (!IsSwapchainImage)
        {
            _dev.Fns.vkDestroyImage(_dev.Handle, Handle, null);
            _dev.Memory.Free(_alloc);
        }
    }

    /// <summary>Destroys only the image view; used when a swapchain rebuild retires its images.</summary>
    public void DestroyView()
    {
        if (View.Value != 0)
        {
            _dev.Fns.vkDestroyImageView(_dev.Handle, View, null);
        }
    }
}

internal sealed unsafe class VulkanSampler : GpuSampler, IRetirable
{
    readonly VulkanDevice _dev;
    public readonly VkSampler Handle;
    bool _disposed;

    public VulkanSampler(VulkanDevice dev, VkSampler handle)
    {
        _dev = dev;
        Handle = handle;
    }

    public override void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _dev.Retire(this);
        }
    }

    public void DestroyNow() => _dev.Fns.vkDestroySampler(_dev.Handle, Handle, null);
}

internal sealed unsafe class VulkanShader : GpuShader, IRetirable
{
    readonly VulkanDevice _dev;
    public readonly VkShaderModule Handle;
    bool _disposed;

    public VulkanShader(VulkanDevice dev, VkShaderModule handle, ShaderStage stage, string entry)
    {
        _dev = dev;
        Handle = handle;
        Stage = stage;
        EntryPoint = entry;
    }

    public override void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _dev.Retire(this);
        }
    }

    public void DestroyNow() => _dev.Fns.vkDestroyShaderModule(_dev.Handle, Handle, null);
}

internal sealed unsafe class VulkanPipelineLayout : GpuPipelineLayout, IRetirable
{
    readonly VulkanDevice _dev;
    public readonly VkPipelineLayout Handle;
    public readonly VkDescriptorSetLayout[] SetLayouts;
    public readonly ResourceBinding[][] Sets;
    public readonly int[] DynamicCounts;
    public readonly PushConstantRange Push;
    bool _disposed;

    public VulkanPipelineLayout(VulkanDevice dev, VkPipelineLayout handle, VkDescriptorSetLayout[] setLayouts, ResourceBinding[][] sets, PushConstantRange push)
    {
        _dev = dev;
        Handle = handle;
        SetLayouts = setLayouts;
        Sets = sets;
        Push = push;
        SetCount = sets.Length;
        DynamicCounts = new int[sets.Length];
        for (var i = 0; i < sets.Length; i++)
        {
            DynamicCounts[i] = sets[i].Count(b => b.Dynamic);
        }
    }

    public override void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _dev.Retire(this);
        }
    }

    public void DestroyNow()
    {
        _dev.Fns.vkDestroyPipelineLayout(_dev.Handle, Handle, null);
        foreach (var s in SetLayouts)
        {
            _dev.Fns.vkDestroyDescriptorSetLayout(_dev.Handle, s, null);
        }
    }
}

/// <summary>What a bind group makes the GPU touch, so the encoder can insert barriers before the draw or dispatch.</summary>
internal readonly record struct BoundUse(VulkanBuffer? Buffer, VulkanTexture? Texture, Use Use);

internal sealed unsafe class VulkanBindGroup : GpuBindGroup, IRetirable
{
    readonly VulkanDevice _dev;
    public readonly VkDescriptorSet Handle;
    public readonly int Pool;
    public readonly VulkanPipelineLayout Layout;
    public readonly uint Set;
    public readonly BoundUse[] Uses;
    public readonly int DynamicCount;
    bool _disposed;

    public VulkanBindGroup(VulkanDevice dev, VkDescriptorSet handle, int pool, VulkanPipelineLayout layout, uint setIndex, BoundUse[] uses)
    {
        _dev = dev;
        Handle = handle;
        Pool = pool;
        Layout = layout;
        Set = setIndex;
        Uses = uses;
        DynamicCount = layout.DynamicCounts[setIndex];
    }

    public override void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _dev.Retire(this);
        }
    }

    public void DestroyNow() => _dev.FreeDescriptorSet(Pool, Handle);
}

internal sealed unsafe class VulkanGraphicsPipeline : GpuGraphicsPipeline, IRetirable
{
    readonly VulkanDevice _dev;
    public readonly VkPipeline Handle;
    public readonly VulkanPipelineLayout Layout;
    bool _disposed;

    public VulkanGraphicsPipeline(VulkanDevice dev, VkPipeline handle, VulkanPipelineLayout layout)
    {
        _dev = dev;
        Handle = handle;
        Layout = layout;
    }

    public override void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _dev.Retire(this);
        }
    }

    public void DestroyNow() => _dev.Fns.vkDestroyPipeline(_dev.Handle, Handle, null);
}

internal sealed unsafe class VulkanComputePipeline : GpuComputePipeline, IRetirable
{
    readonly VulkanDevice _dev;
    public readonly VkPipeline Handle;
    public readonly VulkanPipelineLayout Layout;
    bool _disposed;

    public VulkanComputePipeline(VulkanDevice dev, VkPipeline handle, VulkanPipelineLayout layout)
    {
        _dev = dev;
        Handle = handle;
        Layout = layout;
    }

    public override void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _dev.Retire(this);
        }
    }

    public void DestroyNow() => _dev.Fns.vkDestroyPipeline(_dev.Handle, Handle, null);
}

internal sealed unsafe class VulkanTimestampSet : GpuTimestampSet, IRetirable
{
    readonly VulkanDevice _dev;
    public readonly VkQueryPool Handle;
    bool _disposed;

    public VulkanTimestampSet(VulkanDevice dev, VkQueryPool handle, uint count)
    {
        _dev = dev;
        Handle = handle;
        Count = count;
    }

    public override void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _dev.Retire(this);
        }
    }

    public void DestroyNow() => _dev.Fns.vkDestroyQueryPool(_dev.Handle, Handle, null);
}
