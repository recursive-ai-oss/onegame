using OneGame.Gpu.Metal.Interop;

namespace OneGame.Gpu.Metal;

/// <summary>An NSRange.</summary>
[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
internal struct NSRange
{
    public nuint Location, Length;

    public NSRange(nuint location, nuint length)
    {
        Location = location;
        Length = length;
    }
}

internal sealed class MetalBuffer : GpuBuffer
{
    public nint Handle;

    public MetalBuffer(nint handle, in BufferDesc desc)
    {
        Handle = handle;
        Size = desc.Size;
        Usage = desc.Usage;
        Label = desc.Label;
    }

    public override void Dispose() => Objc.Release(ref Handle);
}

internal sealed class MetalTexture : GpuTexture
{
    public nint Handle;
    readonly bool _owned;
    readonly List<(TextureViewDesc Desc, nint View)> _views = [];

    public MetalTexture(nint handle, bool owned, in TextureDesc desc)
    {
        Handle = handle;
        _owned = owned;
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
    }

    /// <summary>The Metal texture a shader binds: the texture itself, or a view of part of it.</summary>
    public nint ShaderView(TextureViewDesc? desc)
    {
        if (desc is not { } d)
        {
            return Handle;
        }

        foreach (var v in _views)
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

        var type = kind switch
        {
            TextureViewKind.D2 => MT.Tex2D,
            TextureViewKind.D2Array => MT.Tex2DArray,
            TextureViewKind.Cube => MT.TexCube,
            TextureViewKind.CubeArray => MT.TexCubeArray,
            _ => MT.Tex3D,
        };
        using var pool = new Pool();
        var view = Msg.Obj(Handle, Sel.newTextureViewWithPixelFormat_textureType_levels_slices_,
            MetalMap.Pixel(Format), type, new NSRange(d.BaseMip, levels), new NSRange(d.BaseLayer, layers));
        if (view == 0)
        {
            throw new GpuException("could not create a texture view");
        }

        _views.Add((d, view));
        return view;
    }

    public override void Dispose()
    {
        foreach (var (_, v) in _views)
        {
            Objc.objc_release(v);
        }

        _views.Clear();
        if (_owned)
        {
            Objc.Release(ref Handle);
        }
        else
        {
            Handle = 0;
        }
    }
}

internal sealed class MetalSampler : GpuSampler
{
    public nint Handle;

    public MetalSampler(nint handle) => Handle = handle;

    public override void Dispose() => Objc.Release(ref Handle);
}

/// <summary>Where a shader file's resource sits in each Metal argument table, found by (set, binding).</summary>
internal sealed class SlotTable
{
    // [set][binding] -> Metal index, or -1.
    readonly int[][] _index;

    public SlotTable(ReadOnlySpan<OneShader.ResourceBinding> bindings)
    {
        var maxSet = -1;
        foreach (var b in bindings)
        {
            if (b.MetalIndex != OneShader.ResourceBinding.NoMetalIndex)
            {
                maxSet = Math.Max(maxSet, (int)b.Set);
            }
        }

        _index = new int[maxSet + 1][];
        for (var s = 0; s <= maxSet; s++)
        {
            var maxBinding = -1;
            foreach (var b in bindings)
            {
                if (b.Set == s && b.MetalIndex != OneShader.ResourceBinding.NoMetalIndex)
                {
                    maxBinding = Math.Max(maxBinding, (int)b.Binding);
                }
            }

            var row = new int[maxBinding + 1];
            Array.Fill(row, -1);
            foreach (var b in bindings)
            {
                if (b.Set == s && b.MetalIndex != OneShader.ResourceBinding.NoMetalIndex)
                {
                    row[b.Binding] = (int)b.MetalIndex;
                }
            }

            _index[s] = row;
        }
    }

    public static SlotTable Empty { get; } = new(default);

    /// <summary>The Metal index of a resource, or -1 when the shader file does not declare it.</summary>
    public int Lookup(uint set, uint binding) =>
        set < _index.Length && binding < _index[set].Length ? _index[set][binding] : -1;
}

internal sealed class MetalShader : GpuShader
{
    public nint Library, Function;
    public readonly SlotTable Slots;
    public readonly (uint X, uint Y, uint Z) GroupSize;
    public readonly uint VertexBufferBase;

    public MetalShader(nint library, nint function, ShaderStage stage, string entry, SlotTable slots, (uint, uint, uint) group, uint vertexBufferBase)
    {
        Library = library;
        Function = function;
        Stage = stage;
        EntryPoint = entry;
        Slots = slots;
        GroupSize = group;
        VertexBufferBase = vertexBufferBase;
    }

    public override void Dispose()
    {
        Objc.Release(ref Function);
        Objc.Release(ref Library);
    }
}

internal sealed class MetalPipelineLayout : GpuPipelineLayout
{
    public readonly ResourceBinding[][] Sets;
    public readonly PushConstantRange Push;

    public MetalPipelineLayout(ResourceBinding[][] sets, PushConstantRange push)
    {
        Sets = sets;
        Push = push;
        SetCount = sets.Length;
    }

    public override void Dispose()
    {
    }
}

/// <summary>One binding of a bind group: the Metal objects (with offsets for buffers) behind it.</summary>
internal struct BoundBinding
{
    public uint Binding;
    public ResourceKind Kind;
    public ShaderStage Stages;
    public int DynamicIndex;
    public nint[] Handles;
    public ulong[] Offsets;
}

internal sealed class MetalBindGroup : GpuBindGroup
{
    public readonly MetalPipelineLayout Layout;
    public readonly uint Set;
    public readonly BoundBinding[] Bindings;
    public readonly int DynamicCount;
    bool _disposed;

    public MetalBindGroup(MetalPipelineLayout layout, uint set, BoundBinding[] bindings, int dynamicCount)
    {
        Layout = layout;
        Set = set;
        Bindings = bindings;
        DynamicCount = dynamicCount;
    }

    public override void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var b in Bindings)
        {
            foreach (var h in b.Handles)
            {
                Objc.objc_release(h);
            }
        }
    }
}

internal sealed class MetalGraphicsPipeline : GpuGraphicsPipeline
{
    public nint State, DepthState;
    public MetalPipelineLayout Layout = null!;
    public SlotTable VertexSlots = null!;
    public SlotTable PixelSlots = null!;
    public nuint Primitive, Cull, Winding, DepthClip;
    public float DepthBias, SlopeScale, BiasClamp;
    public uint VertexBufferBase;

    public override void Dispose()
    {
        Objc.Release(ref State);
        Objc.Release(ref DepthState);
    }
}

internal sealed class MetalComputePipeline : GpuComputePipeline
{
    public nint State;
    public MetalPipelineLayout Layout = null!;
    public SlotTable Slots = null!;
    public (uint X, uint Y, uint Z) Group;

    public override void Dispose() => Objc.Release(ref State);
}

internal sealed class MetalTimestampSet : GpuTimestampSet
{
    public override void Dispose()
    {
    }
}
