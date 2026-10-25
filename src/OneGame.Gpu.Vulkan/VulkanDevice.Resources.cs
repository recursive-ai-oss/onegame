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

        if (desc.SampleCount > 1 && (desc.MipLevels > 1 || desc.ArrayLayers > 1 || desc.Dimension != TextureDimension.D2))
        {
            throw new ArgumentException("multisampled textures are 2D with one mip level and one layer", nameof(desc));
        }

        if (desc.SampleCount > _caps.MaxSampleCount)
        {
            throw new GpuUnsupportedException($"{Adapter.Name} supports at most {_caps.MaxSampleCount} samples per texel, not {desc.SampleCount}");
        }

        var caps = _caps;
        var cube = desc.Dimension == TextureDimension.Cube;
        var volume = desc.Dimension == TextureDimension.D3;
        if (cube && (desc.Width != desc.Height || desc.ArrayLayers % 6 != 0))
        {
            throw new ArgumentException("a cube texture is square with a multiple of 6 layers", nameof(desc));
        }

        if (cube && desc.ArrayLayers > 6 && !_cubeArrays)
        {
            throw new GpuUnsupportedException($"{Adapter.Name} has no cube array textures");
        }

        if (volume && (desc.ArrayLayers != 1 || desc.Depth == 0 || (desc.Usage & TextureUsage.RenderTarget) != 0))
        {
            throw new ArgumentException("a 3D texture has one layer, a depth of at least 1, and cannot be a render target", nameof(desc));
        }

        if (!volume && desc.Depth != 1)
        {
            throw new ArgumentException("only 3D textures have a depth", nameof(desc));
        }

        if (Math.Max(desc.Width, desc.Height) > (volume ? caps.MaxTextureSize3D : caps.MaxTextureSize) || (volume && desc.Depth > caps.MaxTextureSize3D) || desc.ArrayLayers > caps.MaxArrayLayers)
        {
            throw new GpuUnsupportedException($"texture size {desc.Width}x{desc.Height}x{(volume ? desc.Depth : desc.ArrayLayers)} exceeds what {Adapter.Name} supports");
        }

        if (desc.MipLevels > (volume ? GpuTexture.FullMipCount(desc.Width, desc.Height, desc.Depth) : GpuTexture.FullMipCount(desc.Width, desc.Height)))
        {
            throw new ArgumentException("more mip levels than the texture size has", nameof(desc));
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
            flags = cube ? VkImageCreateFlagBits.CubeCompatible : 0,
            imageType = volume ? VkImageType._3D : VkImageType._2D,
            format = VkMap.Format(desc.Format),
            extent = new VkExtent3D { width = desc.Width, height = desc.Height, depth = volume ? desc.Depth : 1 },
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
        var view = needsView ? CreateView(image, desc.Format, aspect, desc.MipLevels, desc.ArrayLayers, desc.Dimension) : default;
        return new VulkanTexture(this, image, view, alloc, desc, false);
    }

    internal VkImageView CreateView(VkImage image, GpuFormat format, VkImageAspectFlagBits aspect, uint mips, uint layers, TextureDimension dimension = TextureDimension.D2)
    {
        var vi = new VkImageViewCreateInfo
        {
            image = image,
            viewType = dimension switch
            {
                TextureDimension.D3 => VkImageViewType._3D,
                TextureDimension.Cube => layers > 6 ? VkImageViewType.CubeArray : VkImageViewType.Cube,
                _ => layers > 1 ? VkImageViewType._2DArray : VkImageViewType._2D,
            },
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
            addressModeW = VkMap.Address(desc.AddressW),
            anisotropyEnable = aniso && desc.Compare is null ? 1u : 0u,
            maxAnisotropy = aniso && desc.Compare is null ? Math.Min(desc.MaxAnisotropy, MaxAnisotropy) : 1f,
            compareEnable = desc.Compare is null ? 0u : 1u,
            compareOp = VkMap.Compare(desc.Compare ?? CompareFunction.Never),
            minLod = desc.MinLod,
            maxLod = desc.MaxLod,
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

    public override GpuPipelineLayout CreatePipelineLayout(ReadOnlySpan<ResourceBinding> bindings) => CreatePipelineLayout(bindings, default(PushConstantRange));

    public override GpuPipelineLayout CreatePipelineLayout(ReadOnlySpan<ResourceBinding> bindings, in PushConstantRange pushConstants)
    {
        if (pushConstants.Size > _maxPushConstants || pushConstants.Size % 4 != 0)
        {
            throw new ArgumentException($"push constants must be a multiple of 4 bytes and at most {_maxPushConstants}", nameof(pushConstants));
        }

        if (pushConstants.Size > 0 && pushConstants.Stages == ShaderStage.None)
        {
            throw new ArgumentException("push constants need at least one stage", nameof(pushConstants));
        }

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
            else if (list[at].Kind != b.Kind || list[at].Count != b.Count || list[at].Dynamic != b.Dynamic)
            {
                throw new ArgumentException($"binding {b.Set}:{b.Binding} is declared with two different kinds, counts or dynamic settings");
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
                        descriptorType = VkMap.Descriptor(list[i].Kind, list[i].Dynamic),
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
            var range = new VkPushConstantRange { stageFlags = VkMap.Stages(pushConstants.Stages), offset = 0, size = pushConstants.Size };
            if (pushConstants.Size > 0)
            {
                pl.pushConstantRangeCount = 1;
                pl.pPushConstantRanges = &range;
            }

            VkPipelineLayout layout;
            Check(Fns.vkCreatePipelineLayout(Handle, &pl, null, &layout), "vkCreatePipelineLayout");
            return new VulkanPipelineLayout(this, layout, layouts, result, pushConstants);
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
        var sizes = stackalloc VkDescriptorPoolSize[7];
        sizes[0] = new VkDescriptorPoolSize { type = VkDescriptorType.UniformBuffer, descriptorCount = 1024 };
        sizes[1] = new VkDescriptorPoolSize { type = VkDescriptorType.StorageBuffer, descriptorCount = 1024 };
        sizes[2] = new VkDescriptorPoolSize { type = VkDescriptorType.SampledImage, descriptorCount = 2048 };
        sizes[3] = new VkDescriptorPoolSize { type = VkDescriptorType.Sampler, descriptorCount = 1024 };
        sizes[4] = new VkDescriptorPoolSize { type = VkDescriptorType.StorageImage, descriptorCount = 512 };
        sizes[5] = new VkDescriptorPoolSize { type = VkDescriptorType.UniformBufferDynamic, descriptorCount = 256 };
        sizes[6] = new VkDescriptorPoolSize { type = VkDescriptorType.StorageBufferDynamic, descriptorCount = 256 };
        var ci = new VkDescriptorPoolCreateInfo { flags = VkDescriptorPoolCreateFlagBits.FreeDescriptorSet, maxSets = 512, poolSizeCount = 7, pPoolSizes = sizes };
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
        var uses = new List<BoundUse>();
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

                var count = Math.Max(d.Count, 1u);
                if (count > _caps.MaxResourceArrayCount)
                {
                    throw new GpuUnsupportedException($"binding {d.Binding} has {count} elements; {Adapter.Name} allows resource arrays of up to {_caps.MaxResourceArrayCount}");
                }

                var w = new VkWriteDescriptorSet { dstSet = dset, dstBinding = d.Binding, descriptorCount = count, descriptorType = VkMap.Descriptor(d.Kind, d.Dynamic) };
                switch (d.Kind)
                {
                    case ResourceKind.ConstantBuffer:
                    case ResourceKind.StorageBuffer:
                    case ResourceKind.ReadOnlyStorageBuffer:
                    {
                        var need = d.Kind == ResourceKind.ConstantBuffer ? BufferUsage.Uniform : BufferUsage.Storage;
                        var use = d.Kind == ResourceKind.ConstantBuffer ? Use.UniformBuffer : d.Kind == ResourceKind.StorageBuffer ? Use.StorageWrite : Use.StorageRead;
                        if (d.Dynamic && e.Size == 0)
                        {
                            throw new ArgumentException($"dynamic binding {d.Binding} needs an explicit Size: the range that moves with each offset", nameof(entries));
                        }

                        var bi = arena.Alloc<VkDescriptorBufferInfo>((int)count);
                        for (var k = 0; k < count; k++)
                        {
                            var buffer = count > 1 || e.Buffers is not null ? ElementOf(e.Buffers, k, count, d.Binding, "Buffers") : e.Buffer;
                            var b = buffer as VulkanBuffer ?? throw new ArgumentException($"binding {d.Binding} needs a buffer", nameof(entries));
                            if ((b.Usage & need) == 0)
                            {
                                throw new ArgumentException($"buffer at binding {d.Binding} lacks BufferUsage.{need}", nameof(entries));
                            }

                            var offset = count > 1 || e.Buffers is not null ? 0 : e.Offset;
                            bi[k] = new VkDescriptorBufferInfo { buffer = b.Handle, offset = offset, range = e.Size == 0 ? WholeSize : e.Size };
                            uses.Add(new BoundUse(b, null, use));
                        }

                        w.pBufferInfo = bi;
                        break;
                    }

                    case ResourceKind.SampledTexture:
                    case ResourceKind.StorageTexture:
                    {
                        var need = d.Kind == ResourceKind.SampledTexture ? TextureUsage.Sampled : TextureUsage.Storage;
                        var ii = arena.Alloc<VkDescriptorImageInfo>((int)count);
                        for (var k = 0; k < count; k++)
                        {
                            var texture = count > 1 || e.Textures is not null ? ElementOf(e.Textures, k, count, d.Binding, "Textures") : e.Texture;
                            var t = texture as VulkanTexture ?? throw new ArgumentException($"binding {d.Binding} needs a texture", nameof(entries));
                            if ((t.Usage & need) == 0)
                            {
                                throw new ArgumentException($"texture at binding {d.Binding} lacks TextureUsage.{need}", nameof(entries));
                            }

                            ii[k] = new VkDescriptorImageInfo
                            {
                                imageView = t.ShaderView(e.View),
                                imageLayout = d.Kind == ResourceKind.SampledTexture ? VkImageLayout.ShaderReadOnlyOptimal : VkImageLayout.General,
                            };
                            uses.Add(new BoundUse(null, t, d.Kind == ResourceKind.SampledTexture ? Use.Sampled : Use.StorageWrite));
                        }

                        w.pImageInfo = ii;
                        break;
                    }

                    default:
                    {
                        var ii = arena.Alloc<VkDescriptorImageInfo>((int)count);
                        for (var k = 0; k < count; k++)
                        {
                            var sampler = count > 1 || e.Samplers is not null ? ElementOf(e.Samplers, k, count, d.Binding, "Samplers") : e.Sampler;
                            var smp = sampler as VulkanSampler ?? throw new ArgumentException($"binding {d.Binding} needs a sampler", nameof(entries));
                            ii[k] = new VkDescriptorImageInfo { sampler = smp.Handle };
                        }

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

        return new VulkanBindGroup(this, dset, pool, pl, setIndex, uses.ToArray());
    }

    static T ElementOf<T>(IReadOnlyList<T>? list, int index, uint declared, uint binding, string member)
        where T : class
    {
        if (list is null || list.Count != declared)
        {
            throw new ArgumentException($"binding {binding} is a resource array of {declared}; BindGroupEntry.{member} must hold exactly {declared} elements (it holds {list?.Count ?? 0})");
        }

        return list[index] ?? throw new ArgumentException($"element {index} of binding {binding} is null");
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
        if (desc.DepthClamp)
        {
            if (!_caps.DepthClamp)
            {
                throw new GpuUnsupportedException($"{Adapter.Name} cannot clamp depth (check GpuCapabilities.DepthClamp)");
            }

            raster.depthClampEnable = 1;
        }

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
            if (dss.DepthBias != 0 || dss.DepthBiasSlopeScale != 0)
            {
                if (dss.DepthBiasClamp != 0 && !_caps.DepthBiasClamp)
                {
                    throw new GpuUnsupportedException($"{Adapter.Name} cannot clamp depth bias (check GpuCapabilities.DepthBiasClamp)");
                }

                raster.depthBiasEnable = 1;
                raster.depthBiasConstantFactor = dss.DepthBias;
                raster.depthBiasSlopeFactor = dss.DepthBiasSlopeScale;
                raster.depthBiasClamp = dss.DepthBiasClamp;
            }

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
        return new VulkanGraphicsPipeline(this, pipe, layout);
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
        return new VulkanComputePipeline(this, pipe, layout);
    }

    // ---- timestamps ----

    public override GpuTimestampSet CreateTimestampSet(uint count)
    {
        if (!_caps.Timestamps)
        {
            throw new GpuUnsupportedException($"{Adapter.Name} cannot write timestamps from graphics and compute queues");
        }

        if (count == 0 || count > 8192)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "a timestamp set holds 1 to 8192 queries");
        }

        var ci = new VkQueryPoolCreateInfo { queryType = VkQueryType.Timestamp, queryCount = count };
        VkQueryPool pool;
        Check(Fns.vkCreateQueryPool(Handle, &ci, null, &pool), "vkCreateQueryPool");

        // Queries must be reset before the first read; the reset runs ahead of the next submission.
        Fns.vkCmdResetQueryPool(UploadEncoder.Current, pool, 0, count);
        return new VulkanTimestampSet(this, pool, count);
    }

    public override bool TryReadTimestamps(GpuTimestampSet timestamps, uint first, Span<ulong> nanoseconds)
    {
        var s = (VulkanTimestampSet)timestamps;
        if (nanoseconds.IsEmpty)
        {
            return true;
        }

        if ((ulong)first + (ulong)nanoseconds.Length > s.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(nanoseconds), "read extends past the end of the timestamp set");
        }

        if (_upload is not null)
        {
            Submit(BeginCommands());
        }

        var n = nanoseconds.Length;
        if (_timestampScratch.Length < n * 2)
        {
            _timestampScratch = new ulong[n * 2];
        }

        VkResult r;
        fixed (ulong* data = _timestampScratch)
        {
            r = Fns.vkGetQueryPoolResults(Handle, s.Handle, first, (uint)n, (nuint)(n * 16), data, 16, VkQueryResultFlagBits._64 | VkQueryResultFlagBits.WithAvailability);
        }

        if (r == VkResult.NotReady)
        {
            // Some results are missing; a partial read is of no use to the caller.
            return false;
        }

        Check(r, "vkGetQueryPoolResults");
        for (var i = 0; i < n; i++)
        {
            if (_timestampScratch[i * 2 + 1] == 0)
            {
                return false;
            }
        }

        for (var i = 0; i < n; i++)
        {
            nanoseconds[i] = (ulong)((_timestampScratch[i * 2] & _timestampMask) * (double)_timestampPeriod);
        }

        return true;
    }

    ulong[] _timestampScratch = new ulong[64];

    public override GpuSwapchain CreateSwapchain(in SurfaceSource surface, in SwapchainDesc desc) => new VulkanSwapchain(this, surface, desc);
}
