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
        Label = desc.Label;
        Aspect = VkMap.Aspect(desc.Format);
    }

    /// <summary>A view of a single mip level and layer, for rendering into.</summary>
    public VkImageView AttachmentView(uint mip, uint layer)
    {
        if (mip == 0 && layer == 0 && ArrayLayers == 1)
        {
            return View;
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
    bool _disposed;

    public VulkanPipelineLayout(VulkanDevice dev, VkPipelineLayout handle, VkDescriptorSetLayout[] setLayouts, ResourceBinding[][] sets)
    {
        _dev = dev;
        Handle = handle;
        SetLayouts = setLayouts;
        Sets = sets;
        SetCount = sets.Length;
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
    bool _disposed;

    public VulkanBindGroup(VulkanDevice dev, VkDescriptorSet handle, int pool, VulkanPipelineLayout layout, uint setIndex, BoundUse[] uses)
    {
        _dev = dev;
        Handle = handle;
        Pool = pool;
        Layout = layout;
        Set = setIndex;
        Uses = uses;
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
    bool _disposed;

    public VulkanGraphicsPipeline(VulkanDevice dev, VkPipeline handle)
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

    public void DestroyNow() => _dev.Fns.vkDestroyPipeline(_dev.Handle, Handle, null);
}

internal sealed unsafe class VulkanComputePipeline : GpuComputePipeline, IRetirable
{
    readonly VulkanDevice _dev;
    public readonly VkPipeline Handle;
    bool _disposed;

    public VulkanComputePipeline(VulkanDevice dev, VkPipeline handle)
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

    public void DestroyNow() => _dev.Fns.vkDestroyPipeline(_dev.Handle, Handle, null);
}
