using System.Text;
using OneGame.Gpu.Metal.Interop;

namespace OneGame.Gpu.Metal;

internal sealed unsafe partial class MetalDevice
{
    /// <inheritdoc />
    public override GpuBuffer CreateBuffer(in BufferDesc desc)
    {
        if (desc.Size == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(desc), "buffer size must be greater than zero");
        }

        using var pool = new Pool();
        var handle = Msg.Obj(_device, Sel.newBufferWithLength_options_, (nuint)desc.Size, MT.StoragePrivate);
        if (handle == 0)
        {
            throw new GpuOutOfMemoryException($"could not allocate a buffer of {desc.Size} bytes");
        }

        if (desc.Label is { } label)
        {
            Msg.Void(handle, Sel.setLabel_, Objc.NSString(label));
        }

        return new MetalBuffer(handle, desc);
    }

    /// <inheritdoc />
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

        var cube = desc.Dimension == TextureDimension.Cube;
        var volume = desc.Dimension == TextureDimension.D3;
        if (cube && (desc.Width != desc.Height || desc.ArrayLayers % 6 != 0))
        {
            throw new ArgumentException("a cube texture is square with a multiple of 6 layers", nameof(desc));
        }

        if (volume && (desc.ArrayLayers != 1 || desc.Depth == 0 || (desc.Usage & TextureUsage.RenderTarget) != 0))
        {
            throw new ArgumentException("a 3D texture has one layer, a depth of at least 1, and cannot be a render target", nameof(desc));
        }

        if (!volume && desc.Depth != 1)
        {
            throw new ArgumentException("only 3D textures have a depth", nameof(desc));
        }

        if (Math.Max(desc.Width, desc.Height) > (volume ? _caps.MaxTextureSize3D : _caps.MaxTextureSize) || (volume && desc.Depth > _caps.MaxTextureSize3D) || desc.ArrayLayers > _caps.MaxArrayLayers)
        {
            throw new GpuUnsupportedException($"texture size {desc.Width}x{desc.Height}x{(volume ? desc.Depth : desc.ArrayLayers)} exceeds what {Adapter.Name} supports");
        }

        if (desc.MipLevels > (volume ? GpuTexture.FullMipCount(desc.Width, desc.Height, desc.Depth) : GpuTexture.FullMipCount(desc.Width, desc.Height)))
        {
            throw new ArgumentException("more mip levels than the texture size has", nameof(desc));
        }

        using var pool = new Pool();
        var td = Msg.Obj(Cls.MTLTextureDescriptor, Sel.@new);
        try
        {
            var type = volume ? MT.Tex3D
                : cube ? (desc.ArrayLayers > 6 ? MT.TexCubeArray : MT.TexCube)
                : desc.SampleCount > 1 ? MT.Tex2DMultisample
                : desc.ArrayLayers > 1 ? MT.Tex2DArray : MT.Tex2D;
            nuint usage = 0;
            if ((desc.Usage & TextureUsage.Sampled) != 0)
            {
                usage |= MT.UsageShaderRead;
            }

            if ((desc.Usage & TextureUsage.Storage) != 0)
            {
                usage |= MT.UsageShaderRead | MT.UsageShaderWrite;
            }

            if ((desc.Usage & TextureUsage.RenderTarget) != 0)
            {
                usage |= MT.UsageRenderTarget;
            }

            if (usage == 0)
            {
                usage = MT.UsageShaderRead;
            }

            Msg.Void(td, Sel.setTextureType_, type);
            Msg.Void(td, Sel.setPixelFormat_, MetalMap.Pixel(desc.Format));
            Msg.Void(td, Sel.setWidth_, (nuint)desc.Width);
            Msg.Void(td, Sel.setHeight_, (nuint)desc.Height);
            Msg.Void(td, Sel.setDepth_, (nuint)(volume ? desc.Depth : 1));
            Msg.Void(td, Sel.setMipmapLevelCount_, (nuint)desc.MipLevels);
            Msg.Void(td, Sel.setSampleCount_, (nuint)desc.SampleCount);
            Msg.Void(td, Sel.setArrayLength_, (nuint)(cube ? desc.ArrayLayers / 6 : desc.ArrayLayers));
            Msg.Void(td, Sel.setStorageMode_, MT.StorageModePrivate);
            Msg.Void(td, Sel.setUsage_, usage);
            var handle = Msg.Obj(_device, Sel.newTextureWithDescriptor_, td);
            if (handle == 0)
            {
                throw new GpuOutOfMemoryException($"could not create a {desc.Width}x{desc.Height} {desc.Format} texture");
            }

            if (desc.Label is { } label)
            {
                Msg.Void(handle, Sel.setLabel_, Objc.NSString(label));
            }

            return new MetalTexture(handle, true, desc);
        }
        finally
        {
            Objc.objc_release(td);
        }
    }

    /// <inheritdoc />
    public override GpuSampler CreateSampler(in SamplerDesc desc)
    {
        using var pool = new Pool();
        var sd = Msg.Obj(Cls.MTLSamplerDescriptor, Sel.@new);
        try
        {
            Msg.Void(sd, Sel.setMagFilter_, MetalMap.Filter(desc.MagFilter));
            Msg.Void(sd, Sel.setMinFilter_, MetalMap.Filter(desc.MinFilter));
            // MTLSamplerMipFilter: NotMipmapped 0, Nearest 1, Linear 2.
            Msg.Void(sd, Sel.setMipFilter_, desc.MipFilter == FilterMode.Linear ? (nuint)2 : 1);
            Msg.Void(sd, Sel.setSAddressMode_, MetalMap.Address(desc.AddressU));
            Msg.Void(sd, Sel.setTAddressMode_, MetalMap.Address(desc.AddressV));
            Msg.Void(sd, Sel.setRAddressMode_, MetalMap.Address(desc.AddressW));
            Msg.Void(sd, Sel.setLodMinClamp_, desc.MinLod);
            Msg.Void(sd, Sel.setLodMaxClamp_, desc.MaxLod);
            if (desc.Compare is { } compare)
            {
                Msg.Void(sd, Sel.setCompareFunction_, MetalMap.Compare(compare));
            }
            else
            {
                // Metal allows 1..16 samples of anisotropy.
                Msg.Void(sd, Sel.setMaxAnisotropy_, (nuint)Math.Clamp(desc.MaxAnisotropy, 1f, 16f));
            }

            var handle = Msg.Obj(_device, Sel.newSamplerStateWithDescriptor_, sd);
            if (handle == 0)
            {
                throw new GpuException("could not create a sampler");
            }

            return new MetalSampler(handle);
        }
        finally
        {
            Objc.objc_release(sd);
        }
    }

    /// <inheritdoc />
    public override GpuShader CreateShader(ShaderStage stage, ReadOnlySpan<byte> spirv, string entryPoint) =>
        throw new GpuUnsupportedException("the Metal backend runs Metal Shading Language: create shaders with CreateShader(ShaderSource) and the MSL the shader build generates");

    /// <inheritdoc />
    public override GpuShader CreateShader(in ShaderSource source)
    {
        if (source.Stage is not (ShaderStage.Vertex or ShaderStage.Pixel or ShaderStage.Compute))
        {
            throw new ArgumentException("a shader has exactly one stage", nameof(source));
        }

        if (source.Msl.IsEmpty)
        {
            throw new GpuUnsupportedException($"shader {source.EntryPoint} has no Metal Shading Language code (build with OneShaderTargets including Msl)");
        }

        using var pool = new Pool();
        nint text;
        fixed (byte* p = source.Msl)
        {
            text = Msg.Obj(Msg.Obj(Cls.NSString, Sel.alloc), Sel.initWithBytes_length_encoding_, (nint)p, (nuint)source.Msl.Length, (nuint)4);
        }

        var options = Msg.Obj(Cls.MTLCompileOptions, Sel.@new);
        // Match the reference behaviour of the other backends for NaN and infinity handling.
        Msg.Void(options, Sel.setFastMathEnabled_, (byte)0);
        nint error = 0;
        var library = Msg.Obj(_device, Sel.newLibraryWithSource_options_error_, text, options, (nint)(&error));
        Objc.objc_release(text);
        Objc.objc_release(options);
        if (library == 0)
        {
            throw new GpuException($"Metal could not compile shader {source.EntryPoint}: {Objc.ErrorText(error)}");
        }

        var name = source.MslFunctionName ?? source.EntryPoint;
        var function = Msg.Obj(library, Sel.newFunctionWithName_, Objc.NSString(name));
        if (function == 0)
        {
            Objc.objc_release(library);
            throw new GpuException($"compiled Metal library has no function {name}");
        }

        var layout = source.Layout;
        var (gx, gy, gz) = layout?.WorkgroupSize ?? default;
        var push = layout?.PushConstant is { } pc && pc.MetalIndex != OneShader.Layouts.LayoutSlots.None ? (int)pc.MetalIndex : -1;
        return new MetalShader(library, function, source.Stage, source.EntryPoint, new SlotTable(layout), (gx, gy, gz), VertexBufferBase, push);
    }

    /// <inheritdoc />
    public override GpuPipelineLayout CreatePipelineLayout(ReadOnlySpan<ResourceBinding> bindings) => CreatePipelineLayout(bindings, default(PushConstantRange));

    /// <inheritdoc />
    public override GpuPipelineLayout CreatePipelineLayout(ReadOnlySpan<ResourceBinding> bindings, in PushConstantRange pushConstants)
    {
        if (pushConstants.Size > MaxPushConstantBytes || pushConstants.Size % 4 != 0)
        {
            throw new ArgumentException($"push constants must be a multiple of 4 bytes and at most {MaxPushConstantBytes}", nameof(pushConstants));
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

        var result = new ResourceBinding[sets.Length][];
        for (var s = 0; s < sets.Length; s++)
        {
            sets[s].Sort((a, b) => a.Binding.CompareTo(b.Binding));
            result[s] = sets[s].ToArray();
        }

        return new MetalPipelineLayout(result, pushConstants);
    }

    /// <inheritdoc />
    public override GpuTimestampSet CreateTimestampSet(uint count) =>
        throw new GpuUnsupportedException($"{Adapter.Name} cannot time passes through this backend yet (GpuCapabilities.Timestamps is false)");

    /// <inheritdoc />
    public override bool TryReadTimestamps(GpuTimestampSet timestamps, uint first, Span<ulong> nanoseconds) =>
        throw new GpuUnsupportedException("timestamps are not supported by the Metal backend yet");

    /// <inheritdoc />
    public override GpuBindGroup CreateBindGroup(GpuPipelineLayout layout, uint setIndex, ReadOnlySpan<BindGroupEntry> entries)
    {
        var pl = layout as MetalPipelineLayout ?? throw new ArgumentException("layout belongs to another backend", nameof(layout));
        if (setIndex >= pl.Sets.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(setIndex), $"layout has {pl.Sets.Length} sets");
        }

        var decl = pl.Sets[setIndex];
        if (entries.Length != decl.Length)
        {
            throw new ArgumentException($"setIndex {setIndex} declares {decl.Length} bindings but {entries.Length} entries were given", nameof(entries));
        }

        using var pool = new Pool();
        var bound = new BoundBinding[decl.Length];
        var retained = new List<nint>();
        var dynamicCount = 0;
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

                var bb = new BoundBinding { Binding = d.Binding, Kind = d.Kind, Stages = d.Stages, DynamicIndex = -1, Handles = new nint[count], Offsets = new ulong[count] };
                switch (d.Kind)
                {
                    case ResourceKind.ConstantBuffer:
                    case ResourceKind.StorageBuffer:
                    case ResourceKind.ReadOnlyStorageBuffer:
                    {
                        var need = d.Kind == ResourceKind.ConstantBuffer ? BufferUsage.Uniform : BufferUsage.Storage;
                        if (d.Dynamic && e.Size == 0)
                        {
                            throw new ArgumentException($"dynamic binding {d.Binding} needs an explicit Size: the range that moves with each offset", nameof(entries));
                        }

                        for (var k = 0; k < count; k++)
                        {
                            var buffer = count > 1 || e.Buffers is not null ? ElementOf(e.Buffers, k, count, d.Binding, "Buffers") : e.Buffer;
                            var b = buffer as MetalBuffer ?? throw new ArgumentException($"binding {d.Binding} needs a buffer", nameof(entries));
                            if ((b.Usage & need) == 0)
                            {
                                throw new ArgumentException($"buffer at binding {d.Binding} lacks BufferUsage.{need}", nameof(entries));
                            }

                            bb.Handles[k] = Objc.objc_retain(b.Handle);
                            retained.Add(bb.Handles[k]);
                            bb.Offsets[k] = e.Offset;
                        }

                        if (d.Dynamic)
                        {
                            bb.DynamicIndex = dynamicCount++;
                        }

                        break;
                    }

                    case ResourceKind.SampledTexture:
                    case ResourceKind.StorageTexture:
                    {
                        var need = d.Kind == ResourceKind.SampledTexture ? TextureUsage.Sampled : TextureUsage.Storage;
                        for (var k = 0; k < count; k++)
                        {
                            var texture = count > 1 || e.Textures is not null ? ElementOf(e.Textures, k, count, d.Binding, "Textures") : e.Texture;
                            var t = texture as MetalTexture ?? throw new ArgumentException($"binding {d.Binding} needs a texture", nameof(entries));
                            if ((t.Usage & need) == 0)
                            {
                                throw new ArgumentException($"texture at binding {d.Binding} lacks TextureUsage.{need}", nameof(entries));
                            }

                            bb.Handles[k] = Objc.objc_retain(t.ShaderView(e.View));
                            retained.Add(bb.Handles[k]);
                        }

                        break;
                    }

                    default:
                    {
                        for (var k = 0; k < count; k++)
                        {
                            var sampler = count > 1 || e.Samplers is not null ? ElementOf(e.Samplers, k, count, d.Binding, "Samplers") : e.Sampler;
                            var s = sampler as MetalSampler ?? throw new ArgumentException($"binding {d.Binding} needs a sampler", nameof(entries));
                            bb.Handles[k] = Objc.objc_retain(s.Handle);
                            retained.Add(bb.Handles[k]);
                        }

                        break;
                    }
                }

                bound[i] = bb;
            }
        }
        catch
        {
            foreach (var h in retained)
            {
                Objc.objc_release(h);
            }

            throw;
        }

        return new MetalBindGroup(pl, setIndex, bound, dynamicCount);
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

    /// <inheritdoc />
    public override GpuGraphicsPipeline CreateGraphicsPipeline(GraphicsPipelineDesc desc)
    {
        var layout = desc.Layout as MetalPipelineLayout ?? throw new ArgumentException("layout belongs to another backend", nameof(desc));
        var vs = desc.Vertex as MetalShader ?? throw new ArgumentException("shader belongs to another backend", nameof(desc));
        if (vs.Stage != ShaderStage.Vertex)
        {
            throw new ArgumentException("Vertex must be a vertex shader", nameof(desc));
        }

        var ps = (MetalShader?)desc.Pixel;
        if (ps is not null && ps.Stage != ShaderStage.Pixel)
        {
            throw new ArgumentException("Pixel must be a pixel shader", nameof(desc));
        }

        if (desc.DepthClamp && !_caps.DepthClamp)
        {
            throw new GpuUnsupportedException($"{Adapter.Name} cannot clamp depth (check GpuCapabilities.DepthClamp)");
        }

        using var pool = new Pool();
        var pd = Msg.Obj(Cls.MTLRenderPipelineDescriptor, Sel.@new);
        var vd = Msg.Obj(Cls.MTLVertexDescriptor, Sel.vertexDescriptor);
        nint state = 0;
        nint depthState = 0;
        try
        {
            Msg.Void(pd, Sel.setVertexFunction_, vs.Function);
            if (ps is not null)
            {
                Msg.Void(pd, Sel.setFragmentFunction_, ps.Function);
            }

            // Vertex buffer slot n is bound at Metal buffer index (base - n); attributes read it through the vertex descriptor.
            var attributes = Msg.Obj(vd, Sel.attributes);
            var layouts = Msg.Obj(vd, Sel.layouts);
            for (var slot = 0; slot < desc.VertexBuffers.Count; slot++)
            {
                var vb = desc.VertexBuffers[slot];
                var index = (nuint)(vs.VertexBufferBase - (uint)slot);
                var l = Msg.Obj(layouts, Sel.objectAtIndexedSubscript_, index);
                Msg.Void(l, Sel.setStride_, (nuint)vb.Stride);
                Msg.Void(l, Sel.setStepFunction_, vb.StepMode == VertexStepMode.Instance ? MT.StepPerInstance : MT.StepPerVertex);
                Msg.Void(l, Sel.setStepRate_, (nuint)1);
                foreach (var a in vb.Attributes.Span)
                {
                    var at = Msg.Obj(attributes, Sel.objectAtIndexedSubscript_, (nuint)a.Location);
                    Msg.Void(at, Sel.setFormat_, MetalMap.Vertex(a.Format));
                    Msg.Void(at, Sel.setOffset_, (nuint)a.Offset);
                    Msg.Void(at, Sel.setBufferIndex_, index);
                }
            }

            Msg.Void(pd, Sel.setVertexDescriptor_, vd);
            Msg.Void(pd, Sel.setRasterSampleCount_, (nuint)desc.SampleCount);
            Msg.Void(pd, Sel.setInputPrimitiveTopology_, MetalMap.TopologyClass(desc.Topology));
            var colors = Msg.Obj(pd, Sel.colorAttachments);
            for (var i = 0; i < desc.ColorTargets.Count; i++)
            {
                var ct = desc.ColorTargets[i];
                var ca = Msg.Obj(colors, Sel.objectAtIndexedSubscript_, (nuint)i);
                Msg.Void(ca, Sel.setPixelFormat_, MetalMap.Pixel(ct.Format));
                Msg.Void(ca, Sel.setWriteMask_, MetalMap.WriteMask(ct.WriteMask));
                if (ct.Blend is { } blend)
                {
                    Msg.Void(ca, Sel.setBlendingEnabled_, (byte)1);
                    Msg.Void(ca, Sel.setSourceRGBBlendFactor_, MetalMap.Blend(blend.Color.Source));
                    Msg.Void(ca, Sel.setDestinationRGBBlendFactor_, MetalMap.Blend(blend.Color.Destination));
                    Msg.Void(ca, Sel.setRgbBlendOperation_, MetalMap.BlendOp(blend.Color.Operation));
                    Msg.Void(ca, Sel.setSourceAlphaBlendFactor_, MetalMap.Blend(blend.Alpha.Source));
                    Msg.Void(ca, Sel.setDestinationAlphaBlendFactor_, MetalMap.Blend(blend.Alpha.Destination));
                    Msg.Void(ca, Sel.setAlphaBlendOperation_, MetalMap.BlendOp(blend.Alpha.Operation));
                }
            }

            var pipe = new MetalGraphicsPipeline
            {
                Layout = layout,
                VertexSlots = vs.Slots,
                PixelSlots = ps?.Slots ?? SlotTable.Empty,
                Primitive = MetalMap.Primitive(desc.Topology),
                Cull = desc.Cull switch { CullMode.Front => MT.CullFront, CullMode.Back => MT.CullBack, _ => MT.CullNone },
                // Clip space is +Y up on Metal as on the API, so counter-clockwise stays counter-clockwise.
                Winding = desc.Front == FrontFace.CounterClockwise ? MT.WindingCounterClockwise : MT.WindingClockwise,
                DepthClip = desc.DepthClamp ? MT.DepthClipClamp : MT.DepthClipClip,
                VertexBufferBase = vs.VertexBufferBase,
                VertexPushIndex = vs.PushIndex,
                PixelPushIndex = ps?.PushIndex ?? -1,
            };
            if (desc.DepthStencil is { } dss)
            {
                Msg.Void(pd, Sel.setDepthAttachmentPixelFormat_, MetalMap.Pixel(dss.Format));
                if (MetalMap.HasStencil(dss.Format))
                {
                    Msg.Void(pd, Sel.setStencilAttachmentPixelFormat_, MetalMap.Pixel(dss.Format));
                }

                var dd = Msg.Obj(Cls.MTLDepthStencilDescriptor, Sel.@new);
                Msg.Void(dd, Sel.setDepthCompareFunction_, MetalMap.Compare(dss.DepthCompare));
                Msg.Void(dd, Sel.setDepthWriteEnabled_, dss.DepthWrite ? (byte)1 : (byte)0);
                depthState = Msg.Obj(_device, Sel.newDepthStencilStateWithDescriptor_, dd);
                Objc.objc_release(dd);
                pipe.DepthBias = dss.DepthBias;
                pipe.SlopeScale = dss.DepthBiasSlopeScale;
                pipe.BiasClamp = dss.DepthBiasClamp;
            }

            nint error = 0;
            state = Msg.Obj(_device, Sel.newRenderPipelineStateWithDescriptor_error_, pd, (nint)(&error));
            if (state == 0)
            {
                throw new GpuException($"Metal could not create the render pipeline: {Objc.ErrorText(error)}");
            }

            pipe.State = state;
            pipe.DepthState = depthState;
            return pipe;
        }
        catch
        {
            Objc.Release(ref state);
            Objc.Release(ref depthState);
            throw;
        }
        finally
        {
            Objc.objc_release(pd);
        }
    }

    /// <inheritdoc />
    public override GpuComputePipeline CreateComputePipeline(ComputePipelineDesc desc)
    {
        var layout = desc.Layout as MetalPipelineLayout ?? throw new ArgumentException("layout belongs to another backend", nameof(desc));
        var cs = desc.Compute as MetalShader ?? throw new ArgumentException("shader belongs to another backend", nameof(desc));
        if (cs.Stage != ShaderStage.Compute)
        {
            throw new ArgumentException("Compute must be a compute shader", nameof(desc));
        }

        if (cs.GroupSize.X == 0)
        {
            throw new ArgumentException("a Metal compute shader needs its thread-group size (ShaderSource.Layout of the generated entry point carries it)", nameof(desc));
        }

        using var pool = new Pool();
        nint error = 0;
        var state = Msg.Obj(_device, Sel.newComputePipelineStateWithFunction_error_, cs.Function, (nint)(&error));
        if (state == 0)
        {
            throw new GpuException($"Metal could not create the compute pipeline: {Objc.ErrorText(error)}");
        }

        return new MetalComputePipeline { State = state, Layout = layout, Slots = cs.Slots, Group = cs.GroupSize, PushIndex = cs.PushIndex };
    }

    /// <inheritdoc />
    public override GpuSwapchain CreateSwapchain(in SurfaceSource surface, in SwapchainDesc desc)
    {
        if (surface.Kind != SurfaceKind.Metal)
        {
            throw new GpuUnsupportedException($"the Metal backend presents to a CAMetalLayer (SurfaceSource.MetalLayer), not {surface.Kind}");
        }

        return new MetalSwapchain(this, surface.Window, desc);
    }
}
