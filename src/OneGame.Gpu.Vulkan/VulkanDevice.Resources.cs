using System.Runtime.InteropServices;
using OneGame.Gpu.Vulkan.Interop;

namespace OneGame.Gpu.Vulkan;

internal sealed unsafe partial class VulkanDevice
{
    readonly List<VkDescriptorPool> _descriptorPools = [];

    public override GpuBuffer CreateBuffer(in BufferDesc desc)
    {
        if (desc.Size == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(desc), "buffer size must be greater than zero");
        }

        var ci = new VkBufferCreateInfo { size = desc.Size, usage = VkMap.BufferUsage(desc.Usage), sharingMode = VkSharingMode.Exclusive };
        VkBuffer buf;
        Check(Fns.vkCreateBuffer(Handle, &ci, null, &buf), "vkCreateBuffer");
        VkMemoryRequirements req;
        Fns.vkGetBufferMemoryRequirements(Handle, buf, &req);
        var alloc = Memory.Allocate(req, MemoryKind.DeviceLocal);
        Check(Fns.vkBindBufferMemory(Handle, buf, alloc.Memory, alloc.Offset), "vkBindBufferMemory");
        return new VulkanBuffer(this, buf, alloc, desc);
    }

    public override GpuTexture CreateTexture(in TextureDesc desc)
    {
        if (desc.Width == 0 || desc.Height == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(desc), "texture size must be greater than zero");
        }

        if (desc.MipLevels == 0 || desc.ArrayLayers == 0 || desc.SampleCount == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(desc), "mip levels, array layers and sample count must be at least 1 (use new TextureDesc(...) rather than default)");
        }

        if (desc.SampleCount > 1 && (desc.MipLevels > 1 || desc.ArrayLayers > 1))
        {
            throw new ArgumentException("multisampled textures have one mip level and one layer", nameof(desc));
        }

        var depth = VkMap.IsDepth(desc.Format);
        var usage = (VkImageUsageFlagBits)0;
        if ((desc.Usage & TextureUsage.Sampled) != 0)
        {
            usage |= VkImageUsageFlagBits.Sampled;
        }

        if ((desc.Usage & TextureUsage.Storage) != 0)
        {
            usage |= VkImageUsageFlagBits.Storage;
        }

        if ((desc.Usage & TextureUsage.RenderTarget) != 0)
        {
            usage |= depth ? VkImageUsageFlagBits.DepthStencilAttachment : VkImageUsageFlagBits.ColorAttachment;
        }

        if ((desc.Usage & TextureUsage.CopySource) != 0)
        {
            usage |= VkImageUsageFlagBits.TransferSrc;
        }

        if ((desc.Usage & TextureUsage.CopyDestination) != 0)
        {
            usage |= VkImageUsageFlagBits.TransferDst;
        }

        if (desc.MipLevels > 1)
        {
            // Mip generation blits between levels of the same image.
            usage |= VkImageUsageFlagBits.TransferSrc | VkImageUsageFlagBits.TransferDst;
        }

        var ci = new VkImageCreateInfo
        {
            imageType = VkImageType._2D,
            format = VkMap.Format(desc.Format),
            extent = new VkExtent3D { width = desc.Width, height = desc.Height, depth = 1 },
            mipLevels = desc.MipLevels,
            arrayLayers = desc.ArrayLayers,
            samples = VkMap.Samples(desc.SampleCount),
            tiling = VkImageTiling.Optimal,
            usage = usage,
            sharingMode = VkSharingMode.Exclusive,
            initialLayout = VkImageLayout.Undefined,
        };
        VkImage image;
        Check(Fns.vkCreateImage(Handle, &ci, null, &image), "vkCreateImage");
        VkMemoryRequirements req;
        Fns.vkGetImageMemoryRequirements(Handle, image, &req);
        var alloc = Memory.Allocate(req, MemoryKind.DeviceLocal);
        Check(Fns.vkBindImageMemory(Handle, image, alloc.Memory, alloc.Offset), "vkBindImageMemory");

        // A sampled view must name one aspect, so depth-stencil textures that are sampled expose depth only.
        var aspect = VkMap.Aspect(desc.Format);
        if ((desc.Usage & TextureUsage.Sampled) != 0 && depth)
        {
            aspect = VkImageAspectFlagBits.Depth;
        }

        // Copy-only textures have no shader or attachment use, and Vulkan rejects views of such images.
        var needsView = (usage & (VkImageUsageFlagBits.Sampled | VkImageUsageFlagBits.Storage | VkImageUsageFlagBits.ColorAttachment | VkImageUsageFlagBits.DepthStencilAttachment)) != 0;
        var view = needsView ? CreateView(image, desc.Format, aspect, desc.MipLevels, desc.ArrayLayers) : default;
        return new VulkanTexture(this, image, view, alloc, desc, false);
    }

    internal VkImageView CreateView(VkImage image, GpuFormat format, VkImageAspectFlagBits aspect, uint mips, uint layers)
    {
        var vi = new VkImageViewCreateInfo
        {
            image = image,
            viewType = layers > 1 ? VkImageViewType._2DArray : VkImageViewType._2D,
            format = VkMap.Format(format),
            subresourceRange = new VkImageSubresourceRange { aspectMask = aspect, baseMipLevel = 0, levelCount = mips, baseArrayLayer = 0, layerCount = layers },
        };
        VkImageView view;
        Check(Fns.vkCreateImageView(Handle, &vi, null, &view), "vkCreateImageView");
        return view;
    }

    public override GpuSampler CreateSampler(in SamplerDesc desc)
    {
        var aniso = desc.MaxAnisotropy > 1f && MaxAnisotropy > 1f;
        var ci = new VkSamplerCreateInfo
        {
            magFilter = VkMap.Filter(desc.MagFilter),
            minFilter = VkMap.Filter(desc.MinFilter),
            mipmapMode = desc.MipFilter == FilterMode.Linear ? VkSamplerMipmapMode.Linear : VkSamplerMipmapMode.Nearest,
            addressModeU = VkMap.Address(desc.AddressU),
            addressModeV = VkMap.Address(desc.AddressV),
            addressModeW = VkMap.Address(desc.AddressV),
            anisotropyEnable = aniso ? 1u : 0u,
            maxAnisotropy = aniso ? Math.Min(desc.MaxAnisotropy, MaxAnisotropy) : 1f,
            minLod = 0f,
            maxLod = 1000f,
            borderColor = VkBorderColor.FloatTransparentBlack,
        };
        VkSampler s;
        Check(Fns.vkCreateSampler(Handle, &ci, null, &s), "vkCreateSampler");
        return new VulkanSampler(this, s);
    }

    public override GpuShader CreateShader(ShaderStage stage, ReadOnlySpan<byte> spirv, string entryPoint)
    {
        if (spirv.Length < 20 || spirv.Length % 4 != 0)
        {
            throw new ArgumentException("SPIR-V must be a whole number of 4-byte words with a 5-word header", nameof(spirv));
        }

        if (MemoryMarshal.Read<uint>(spirv) != 0x07230203u)
        {
            throw new ArgumentException("data does not start with the SPIR-V magic number", nameof(spirv));
        }

        if (stage is not (ShaderStage.Vertex or ShaderStage.Pixel or ShaderStage.Compute))
        {
            throw new ArgumentException("a shader has exactly one stage", nameof(stage));
        }

        using var arena = new Arena();
        var code = arena.Copy(spirv);
        var ci = new VkShaderModuleCreateInfo { codeSize = (nuint)spirv.Length, pCode = (uint*)code };
        VkShaderModule m;
        Check(Fns.vkCreateShaderModule(Handle, &ci, null, &m), "vkCreateShaderModule");
        return new VulkanShader(this, m, stage, entryPoint);
    }

    public override GpuPipelineLayout CreatePipelineLayout(ReadOnlySpan<ResourceBinding> bindings)
    {
        var maxSet = -1;
        foreach (var b in bindings)
        {
            maxSet = Math.Max(maxSet, (int)b.Set);
        }

        var sets = new List<ResourceBinding>[maxSet + 1];
        for (var i = 0; i < sets.Length; i++)
        {
            sets[i] = [];
        }

        foreach (var b in bindings)
        {
            var list = sets[b.Set];
            var at = list.FindIndex(x => x.Binding == b.Binding);
            if (at < 0)
            {
                list.Add(b);
            }
            else if (list[at].Kind != b.Kind || list[at].Count != b.Count)
            {
                throw new ArgumentException($"binding {b.Set}:{b.Binding} is declared with two different kinds or counts");
            }
            else
            {
                list[at] = list[at] with { Stages = list[at].Stages | b.Stages };
            }
        }

        var layouts = new VkDescriptorSetLayout[sets.Length];
        var result = new ResourceBinding[sets.Length][];
        using var arena = new Arena();
        try
        {
            for (var s = 0; s < sets.Length; s++)
            {
                var list = sets[s];
                list.Sort((a, b) => a.Binding.CompareTo(b.Binding));
                result[s] = list.ToArray();
                var vb = arena.Alloc<VkDescriptorSetLayoutBinding>(list.Count);
                for (var i = 0; i < list.Count; i++)
                {
                    vb[i] = new VkDescriptorSetLayoutBinding
                    {
                        binding = list[i].Binding,
                        descriptorType = VkMap.Descriptor(list[i].Kind),
                        descriptorCount = Math.Max(list[i].Count, 1),
                        stageFlags = VkMap.Stages(list[i].Stages),
                    };
                }

                var ci = new VkDescriptorSetLayoutCreateInfo { bindingCount = (uint)list.Count, pBindings = vb };
                VkDescriptorSetLayout l;
                Check(Fns.vkCreateDescriptorSetLayout(Handle, &ci, null, &l), "vkCreateDescriptorSetLayout");
                layouts[s] = l;
            }

            var handles = arena.Alloc<VkDescriptorSetLayout>(layouts.Length);
            for (var i = 0; i < layouts.Length; i++)
            {
                handles[i] = layouts[i];
            }

            var pl = new VkPipelineLayoutCreateInfo { setLayoutCount = (uint)layouts.Length, pSetLayouts = handles };
            VkPipelineLayout layout;
            Check(Fns.vkCreatePipelineLayout(Handle, &pl, null, &layout), "vkCreatePipelineLayout");
            return new VulkanPipelineLayout(this, layout, layouts, result);
        }
        catch
        {
            foreach (var l in layouts)
            {
                if (l.Value != 0)
                {
                    Fns.vkDestroyDescriptorSetLayout(Handle, l, null);
                }
            }

            throw;
        }
    }

    // ---- descriptor sets ----

    VkDescriptorPool NewDescriptorPool()
    {
        var sizes = stackalloc VkDescriptorPoolSize[5];
        sizes[0] = new VkDescriptorPoolSize { type = VkDescriptorType.UniformBuffer, descriptorCount = 1024 };
        sizes[1] = new VkDescriptorPoolSize { type = VkDescriptorType.StorageBuffer, descriptorCount = 1024 };
        sizes[2] = new VkDescriptorPoolSize { type = VkDescriptorType.SampledImage, descriptorCount = 2048 };
        sizes[3] = new VkDescriptorPoolSize { type = VkDescriptorType.Sampler, descriptorCount = 1024 };
        sizes[4] = new VkDescriptorPoolSize { type = VkDescriptorType.StorageImage, descriptorCount = 512 };
        var ci = new VkDescriptorPoolCreateInfo { flags = VkDescriptorPoolCreateFlagBits.FreeDescriptorSet, maxSets = 512, poolSizeCount = 5, pPoolSizes = sizes };
        VkDescriptorPool pool;
        Check(Fns.vkCreateDescriptorPool(Handle, &ci, null, &pool), "vkCreateDescriptorPool");
        _descriptorPools.Add(pool);
        return pool;
    }

    VkDescriptorSet AllocateDescriptorSet(VkDescriptorSetLayout layout, out int poolIndex)
    {
        if (_descriptorPools.Count == 0)
        {
            NewDescriptorPool();
        }

        // Newest pool first; fall back to older ones (sets are freed individually) and finally grow.
        for (var attempt = 0; attempt < 2; attempt++)
        {
            for (var i = _descriptorPools.Count - 1; i >= 0; i--)
            {
                var ai = new VkDescriptorSetAllocateInfo { descriptorPool = _descriptorPools[i], descriptorSetCount = 1, pSetLayouts = &layout };
                VkDescriptorSet set;
                var r = Fns.vkAllocateDescriptorSets(Handle, &ai, &set);
                if (r == VkResult.Success)
                {
                    poolIndex = i;
                    return set;
                }

                if (r is not (VkResult.ErrorOutOfPoolMemory or VkResult.ErrorFragmentedPool))
                {
                    Check(r, "vkAllocateDescriptorSets");
                }
            }

            NewDescriptorPool();
        }

        throw new GpuOutOfMemoryException("could not allocate a descriptor set");
    }

    internal void FreeDescriptorSet(int pool, VkDescriptorSet set)
    {
        Fns.vkFreeDescriptorSets(Handle, _descriptorPools[pool], 1, &set);
    }

    void DestroyDescriptorPools()
    {
        foreach (var p in _descriptorPools)
        {
            Fns.vkDestroyDescriptorPool(Handle, p, null);
        }

        _descriptorPools.Clear();
    }

    public override GpuBindGroup CreateBindGroup(GpuPipelineLayout layout, uint setIndex, ReadOnlySpan<BindGroupEntry> entries)
    {
        var pl = (VulkanPipelineLayout)layout;
        if (setIndex >= pl.Sets.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(setIndex), $"layout has {pl.Sets.Length} sets");
        }

        var decl = pl.Sets[setIndex];
        if (entries.Length != decl.Length)
        {
            throw new ArgumentException($"setIndex {setIndex} declares {decl.Length} bindings but {entries.Length} entries were given", nameof(entries));
        }

        using var arena = new Arena();
        var writes = arena.Alloc<VkWriteDescriptorSet>(decl.Length);
        var uses = new BoundUse[decl.Length];
        var dset = AllocateDescriptorSet(pl.SetLayouts[setIndex], out var pool);
        try
        {
            for (var i = 0; i < decl.Length; i++)
            {
                var d = decl[i];
                var found = false;
                BindGroupEntry e = default;
                foreach (var cand in entries)
                {
                    if (cand.Binding == d.Binding)
                    {
                        e = cand;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    throw new ArgumentException($"no entry for binding {d.Binding} of setIndex {setIndex}", nameof(entries));
                }

                if (d.Count > 1)
                {
                    throw new NotSupportedException("resource arrays in bind groups are not supported yet");
                }

                var w = new VkWriteDescriptorSet { dstSet = dset, dstBinding = d.Binding, descriptorCount = 1, descriptorType = VkMap.Descriptor(d.Kind) };
                switch (d.Kind)
                {
                    case ResourceKind.ConstantBuffer:
                    case ResourceKind.StorageBuffer:
                    case ResourceKind.ReadOnlyStorageBuffer:
                    {
                        var b = e.Buffer as VulkanBuffer ?? throw new ArgumentException($"binding {d.Binding} needs a buffer", nameof(entries));
                        var need = d.Kind == ResourceKind.ConstantBuffer ? BufferUsage.Uniform : BufferUsage.Storage;
                        if ((b.Usage & need) == 0)
                        {
                            throw new ArgumentException($"buffer at binding {d.Binding} lacks BufferUsage.{need}", nameof(entries));
                        }

                        var bi = arena.Alloc<VkDescriptorBufferInfo>();
                        *bi = new VkDescriptorBufferInfo { buffer = b.Handle, offset = e.Offset, range = e.Size == 0 ? WholeSize : e.Size };
                        w.pBufferInfo = bi;
                        uses[i] = new BoundUse(b, null, d.Kind == ResourceKind.ConstantBuffer ? Use.UniformBuffer : d.Kind == ResourceKind.StorageBuffer ? Use.StorageWrite : Use.StorageRead);
                        break;
                    }

                    case ResourceKind.SampledTexture:
                    case ResourceKind.StorageTexture:
                    {
                        var t = e.Texture as VulkanTexture ?? throw new ArgumentException($"binding {d.Binding} needs a texture", nameof(entries));
                        var need = d.Kind == ResourceKind.SampledTexture ? TextureUsage.Sampled : TextureUsage.Storage;
                        if ((t.Usage & need) == 0)
                        {
                            throw new ArgumentException($"texture at binding {d.Binding} lacks TextureUsage.{need}", nameof(entries));
                        }

                        var ii = arena.Alloc<VkDescriptorImageInfo>();
                        *ii = new VkDescriptorImageInfo
                        {
                            imageView = t.View,
                            imageLayout = d.Kind == ResourceKind.SampledTexture ? VkImageLayout.ShaderReadOnlyOptimal : VkImageLayout.General,
                        };
                        w.pImageInfo = ii;
                        uses[i] = new BoundUse(null, t, d.Kind == ResourceKind.SampledTexture ? Use.Sampled : Use.StorageWrite);
                        break;
                    }

                    default:
                    {
                        var s = e.Sampler as VulkanSampler ?? throw new ArgumentException($"binding {d.Binding} needs a sampler", nameof(entries));
                        var ii = arena.Alloc<VkDescriptorImageInfo>();
                        *ii = new VkDescriptorImageInfo { sampler = s.Handle };
                        w.pImageInfo = ii;
                        break;
                    }
                }

                writes[i] = w;
            }

            Fns.vkUpdateDescriptorSets(Handle, (uint)decl.Length, writes, 0, null);
        }
        catch
        {
            FreeDescriptorSet(pool, dset);
            throw;
        }

        // Samplers need no barriers; drop their empty slots.
        var used = uses.Where(u => u.Buffer is not null || u.Texture is not null).ToArray();
        return new VulkanBindGroup(this, dset, pool, pl, setIndex, used);
    }

    // ---- pipelines ----

    public override GpuGraphicsPipeline CreateGraphicsPipeline(GraphicsPipelineDesc desc)
    {
        var layout = (VulkanPipelineLayout)desc.Layout;
        var vs = (VulkanShader)desc.Vertex;
        if (vs.Stage != ShaderStage.Vertex)
        {
            throw new ArgumentException("Vertex must be a vertex shader", nameof(desc));
        }

        var ps = (VulkanShader?)desc.Pixel;
        if (ps is not null && ps.Stage != ShaderStage.Pixel)
        {
            throw new ArgumentException("Pixel must be a pixel shader", nameof(desc));
        }

        using var arena = new Arena();
        var stages = arena.Alloc<VkPipelineShaderStageCreateInfo>(2);
        var nStages = 0;
        stages[nStages++] = new VkPipelineShaderStageCreateInfo { stage = VkShaderStageFlagBits.Vertex, module = vs.Handle, pName = arena.Utf8(vs.EntryPoint) };
        if (ps is not null)
        {
            stages[nStages++] = new VkPipelineShaderStageCreateInfo { stage = VkShaderStageFlagBits.Fragment, module = ps.Handle, pName = arena.Utf8(ps.EntryPoint) };
        }

        var nAttr = 0;
        foreach (var vb in desc.VertexBuffers)
        {
            nAttr += vb.Attributes.Length;
        }

        var bindings = arena.Alloc<VkVertexInputBindingDescription>(desc.VertexBuffers.Count);
        var attrs = arena.Alloc<VkVertexInputAttributeDescription>(nAttr);
        var ai = 0;
        for (var i = 0; i < desc.VertexBuffers.Count; i++)
        {
            var vb = desc.VertexBuffers[i];
            bindings[i] = new VkVertexInputBindingDescription { binding = (uint)i, stride = vb.Stride, inputRate = vb.StepMode == VertexStepMode.Instance ? VkVertexInputRate.Instance : VkVertexInputRate.Vertex };
            foreach (var a in vb.Attributes.Span)
            {
                attrs[ai++] = new VkVertexInputAttributeDescription { location = a.Location, binding = (uint)i, format = VkMap.Vertex(a.Format), offset = a.Offset };
            }
        }

        var vertexInput = new VkPipelineVertexInputStateCreateInfo
        {
            vertexBindingDescriptionCount = (uint)desc.VertexBuffers.Count,
            pVertexBindingDescriptions = bindings,
            vertexAttributeDescriptionCount = (uint)nAttr,
            pVertexAttributeDescriptions = attrs,
        };
        var assembly = new VkPipelineInputAssemblyStateCreateInfo
        {
            topology = desc.Topology switch
            {
                PrimitiveTopology.TriangleList => VkPrimitiveTopology.TriangleList,
                PrimitiveTopology.TriangleStrip => VkPrimitiveTopology.TriangleStrip,
                PrimitiveTopology.LineList => VkPrimitiveTopology.LineList,
                _ => VkPrimitiveTopology.PointList,
            },
        };
        var viewport = new VkPipelineViewportStateCreateInfo { viewportCount = 1, scissorCount = 1 };
        var raster = new VkPipelineRasterizationStateCreateInfo
        {
            polygonMode = VkPolygonMode.Fill,
            cullMode = desc.Cull switch { CullMode.Front => VkCullModeFlagBits.Front, CullMode.Back => VkCullModeFlagBits.Back, _ => VkCullModeFlagBits.None },
            // Draws use a flipped viewport (clip-space +Y up), so the portable winding maps to Vulkan's unchanged.
            frontFace = desc.Front == FrontFace.Clockwise ? VkFrontFace.Clockwise : VkFrontFace.CounterClockwise,
            lineWidth = 1f,
        };
        var ms = new VkPipelineMultisampleStateCreateInfo { rasterizationSamples = VkMap.Samples(desc.SampleCount) };

        var colorCount = desc.ColorTargets.Count;
        var blends = arena.Alloc<VkPipelineColorBlendAttachmentState>(colorCount);
        var formats = arena.Alloc<VkFormat>(colorCount);
        for (var i = 0; i < colorCount; i++)
        {
            var t = desc.ColorTargets[i];
            formats[i] = VkMap.Format(t.Format);
            var st = new VkPipelineColorBlendAttachmentState
            {
                colorWriteMask = (VkColorComponentFlagBits)(int)t.WriteMask,
            };
            if (t.Blend is { } bs)
            {
                st.blendEnable = 1;
                st.srcColorBlendFactor = VkMap.Factor(bs.Color.Source);
                st.dstColorBlendFactor = VkMap.Factor(bs.Color.Destination);
                st.colorBlendOp = VkMap.Op(bs.Color.Operation);
                st.srcAlphaBlendFactor = VkMap.Factor(bs.Alpha.Source);
                st.dstAlphaBlendFactor = VkMap.Factor(bs.Alpha.Destination);
                st.alphaBlendOp = VkMap.Op(bs.Alpha.Operation);
            }

            blends[i] = st;
        }

        var blend = new VkPipelineColorBlendStateCreateInfo { attachmentCount = (uint)colorCount, pAttachments = blends };
        var ds = new VkPipelineDepthStencilStateCreateInfo();
        var rendering = new VkPipelineRenderingCreateInfo { colorAttachmentCount = (uint)colorCount, pColorAttachmentFormats = formats };
        if (desc.DepthStencil is { } dss)
        {
            ds.depthTestEnable = 1;
            ds.depthWriteEnable = dss.DepthWrite ? 1u : 0;
            ds.depthCompareOp = VkMap.Compare(dss.DepthCompare);
            rendering.depthAttachmentFormat = VkMap.Format(dss.Format);
            if (dss.Format == GpuFormat.Depth32FloatStencil8)
            {
                rendering.stencilAttachmentFormat = VkFormat.D32SfloatS8Uint;
            }
        }

        var dyn = stackalloc VkDynamicState[2] { VkDynamicState.Viewport, VkDynamicState.Scissor };
        var dynamic = new VkPipelineDynamicStateCreateInfo { dynamicStateCount = 2, pDynamicStates = dyn };

        var ci = new VkGraphicsPipelineCreateInfo
        {
            pNext = &rendering,
            stageCount = (uint)nStages,
            pStages = stages,
            pVertexInputState = &vertexInput,
            pInputAssemblyState = &assembly,
            pViewportState = &viewport,
            pRasterizationState = &raster,
            pMultisampleState = &ms,
            pDepthStencilState = &ds,
            pColorBlendState = &blend,
            pDynamicState = &dynamic,
            layout = layout.Handle,
        };
        VkPipeline pipe;
        Check(Fns.vkCreateGraphicsPipelines(Handle, default, 1, &ci, null, &pipe), "vkCreateGraphicsPipelines");
        return new VulkanGraphicsPipeline(this, pipe);
    }

    public override GpuComputePipeline CreateComputePipeline(ComputePipelineDesc desc)
    {
        var layout = (VulkanPipelineLayout)desc.Layout;
        var cs = (VulkanShader)desc.Compute;
        if (cs.Stage != ShaderStage.Compute)
        {
            throw new ArgumentException("Compute must be a compute shader", nameof(desc));
        }

        using var arena = new Arena();
        var ci = new VkComputePipelineCreateInfo
        {
            stage = new VkPipelineShaderStageCreateInfo { stage = VkShaderStageFlagBits.Compute, module = cs.Handle, pName = arena.Utf8(cs.EntryPoint) },
            layout = layout.Handle,
        };
        VkPipeline pipe;
        Check(Fns.vkCreateComputePipelines(Handle, default, 1, &ci, null, &pipe), "vkCreateComputePipelines");
        return new VulkanComputePipeline(this, pipe);
    }

    public override GpuSwapchain CreateSwapchain(in SurfaceSource surface, in SwapchainDesc desc) => new VulkanSwapchain(this, surface, desc);
}
