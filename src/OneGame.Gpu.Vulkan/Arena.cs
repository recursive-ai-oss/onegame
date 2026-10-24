using System.Runtime.InteropServices;
using System.Text;
using OneGame.Gpu.Vulkan.Interop;

namespace OneGame.Gpu.Vulkan;

/// <summary>Short-lived native memory for building Vulkan create-info chains; freed all at once.</summary>
internal sealed unsafe class Arena : IDisposable
{
    readonly List<nint> _blocks = [];

    public T* Alloc<T>(int count = 1)
        where T : unmanaged
    {
        var bytes = (nuint)(Math.Max(count, 1) * sizeof(T));
        var p = NativeMemory.AllocZeroed(bytes);
        _blocks.Add((nint)p);
        return (T*)p;
    }

    public T* Copy<T>(ReadOnlySpan<T> items)
        where T : unmanaged
    {
        var p = Alloc<T>(items.Length);
        items.CopyTo(new Span<T>(p, items.Length));
        return p;
    }

    public byte* Utf8(string text)
    {
        var bytes = Encoding.UTF8.GetByteCount(text);
        var p = Alloc<byte>(bytes + 1);
        Encoding.UTF8.GetBytes(text, new Span<byte>(p, bytes));
        return p;
    }

    public void Dispose()
    {
        foreach (var b in _blocks)
        {
            NativeMemory.Free((void*)b);
        }

        _blocks.Clear();
    }
}

/// <summary>Maps portable enums to Vulkan.</summary>
internal static class VkMap
{
    public static VkFormat Format(GpuFormat f) => f switch
    {
        GpuFormat.R8Unorm => VkFormat.R8Unorm,
        GpuFormat.Rg8Unorm => VkFormat.R8G8Unorm,
        GpuFormat.Rgba8Unorm => VkFormat.R8G8B8A8Unorm,
        GpuFormat.Rgba8UnormSrgb => VkFormat.R8G8B8A8Srgb,
        GpuFormat.Bgra8Unorm => VkFormat.B8G8R8A8Unorm,
        GpuFormat.Bgra8UnormSrgb => VkFormat.B8G8R8A8Srgb,
        GpuFormat.R16Float => VkFormat.R16Sfloat,
        GpuFormat.Rgba16Float => VkFormat.R16G16B16A16Sfloat,
        GpuFormat.R32Float => VkFormat.R32Sfloat,
        GpuFormat.Rg32Float => VkFormat.R32G32Sfloat,
        GpuFormat.Rgba32Float => VkFormat.R32G32B32A32Sfloat,
        GpuFormat.R32Uint => VkFormat.R32Uint,
        GpuFormat.Depth32Float => VkFormat.D32Sfloat,
        GpuFormat.Depth32FloatStencil8 => VkFormat.D32SfloatS8Uint,
        _ => throw new ArgumentOutOfRangeException(nameof(f), f, "unsupported format"),
    };

    public static GpuFormat FromVk(VkFormat f) => f switch
    {
        VkFormat.B8G8R8A8Unorm => GpuFormat.Bgra8Unorm,
        VkFormat.B8G8R8A8Srgb => GpuFormat.Bgra8UnormSrgb,
        VkFormat.R8G8B8A8Unorm => GpuFormat.Rgba8Unorm,
        VkFormat.R8G8B8A8Srgb => GpuFormat.Rgba8UnormSrgb,
        _ => GpuFormat.Undefined,
    };

    public static bool IsDepth(GpuFormat f) => f is GpuFormat.Depth32Float or GpuFormat.Depth32FloatStencil8;

    public static VkImageAspectFlagBits Aspect(GpuFormat f) => f switch
    {
        GpuFormat.Depth32Float => VkImageAspectFlagBits.Depth,
        GpuFormat.Depth32FloatStencil8 => VkImageAspectFlagBits.Depth | VkImageAspectFlagBits.Stencil,
        _ => VkImageAspectFlagBits.Color,
    };

    public static VkFormat Vertex(VertexFormat f) => f switch
    {
        VertexFormat.Float1 => VkFormat.R32Sfloat,
        VertexFormat.Float2 => VkFormat.R32G32Sfloat,
        VertexFormat.Float3 => VkFormat.R32G32B32Sfloat,
        VertexFormat.Float4 => VkFormat.R32G32B32A32Sfloat,
        VertexFormat.Half2 => VkFormat.R16G16Sfloat,
        VertexFormat.Half4 => VkFormat.R16G16B16A16Sfloat,
        VertexFormat.UInt1 => VkFormat.R32Uint,
        VertexFormat.UInt2 => VkFormat.R32G32Uint,
        VertexFormat.UInt4 => VkFormat.R32G32B32A32Uint,
        VertexFormat.SInt1 => VkFormat.R32Sint,
        VertexFormat.SInt2 => VkFormat.R32G32Sint,
        VertexFormat.UNorm8x4 => VkFormat.R8G8B8A8Unorm,
        VertexFormat.UInt8x4 => VkFormat.R8G8B8A8Uint,
        VertexFormat.UNorm16x2 => VkFormat.R16G16Unorm,
        VertexFormat.UNorm16x4 => VkFormat.R16G16B16A16Unorm,
        _ => throw new ArgumentOutOfRangeException(nameof(f), f, "unsupported vertex format"),
    };

    public static VkShaderStageFlagBits Stages(ShaderStage s)
    {
        var r = (VkShaderStageFlagBits)0;
        if ((s & ShaderStage.Vertex) != 0)
        {
            r |= VkShaderStageFlagBits.Vertex;
        }

        if ((s & ShaderStage.Pixel) != 0)
        {
            r |= VkShaderStageFlagBits.Fragment;
        }

        if ((s & ShaderStage.Compute) != 0)
        {
            r |= VkShaderStageFlagBits.Compute;
        }

        return r;
    }

    public static VkDescriptorType Descriptor(ResourceKind k) => k switch
    {
        ResourceKind.ConstantBuffer => VkDescriptorType.UniformBuffer,
        ResourceKind.StorageBuffer or ResourceKind.ReadOnlyStorageBuffer => VkDescriptorType.StorageBuffer,
        ResourceKind.SampledTexture => VkDescriptorType.SampledImage,
        ResourceKind.Sampler => VkDescriptorType.Sampler,
        ResourceKind.StorageTexture => VkDescriptorType.StorageImage,
        _ => throw new ArgumentOutOfRangeException(nameof(k)),
    };

    public static VkBlendFactor Factor(BlendFactor f) => f switch
    {
        BlendFactor.Zero => VkBlendFactor.Zero,
        BlendFactor.One => VkBlendFactor.One,
        BlendFactor.Source => VkBlendFactor.SrcColor,
        BlendFactor.OneMinusSource => VkBlendFactor.OneMinusSrcColor,
        BlendFactor.SourceAlpha => VkBlendFactor.SrcAlpha,
        BlendFactor.OneMinusSourceAlpha => VkBlendFactor.OneMinusSrcAlpha,
        BlendFactor.Destination => VkBlendFactor.DstColor,
        BlendFactor.OneMinusDestination => VkBlendFactor.OneMinusDstColor,
        BlendFactor.DestinationAlpha => VkBlendFactor.DstAlpha,
        BlendFactor.OneMinusDestinationAlpha => VkBlendFactor.OneMinusDstAlpha,
        _ => throw new ArgumentOutOfRangeException(nameof(f)),
    };

    public static VkBlendOp Op(BlendOperation o) => o switch
    {
        BlendOperation.Add => VkBlendOp.Add,
        BlendOperation.Subtract => VkBlendOp.Subtract,
        BlendOperation.ReverseSubtract => VkBlendOp.ReverseSubtract,
        BlendOperation.Min => VkBlendOp.Min,
        BlendOperation.Max => VkBlendOp.Max,
        _ => throw new ArgumentOutOfRangeException(nameof(o)),
    };

    public static VkCompareOp Compare(CompareFunction c) => c switch
    {
        CompareFunction.Never => VkCompareOp.Never,
        CompareFunction.Less => VkCompareOp.Less,
        CompareFunction.Equal => VkCompareOp.Equal,
        CompareFunction.LessEqual => VkCompareOp.LessOrEqual,
        CompareFunction.Greater => VkCompareOp.Greater,
        CompareFunction.NotEqual => VkCompareOp.NotEqual,
        CompareFunction.GreaterEqual => VkCompareOp.GreaterOrEqual,
        CompareFunction.Always => VkCompareOp.Always,
        _ => throw new ArgumentOutOfRangeException(nameof(c)),
    };

    public static VkSamplerAddressMode Address(AddressMode a) => a switch
    {
        AddressMode.ClampToEdge => VkSamplerAddressMode.ClampToEdge,
        AddressMode.Repeat => VkSamplerAddressMode.Repeat,
        AddressMode.MirrorRepeat => VkSamplerAddressMode.MirroredRepeat,
        _ => throw new ArgumentOutOfRangeException(nameof(a)),
    };

    public static VkFilter Filter(FilterMode f) => f == FilterMode.Linear ? VkFilter.Linear : VkFilter.Nearest;

    public static VkSampleCountFlagBits Samples(uint n) => n switch
    {
        1 => VkSampleCountFlagBits._1,
        2 => VkSampleCountFlagBits._2,
        4 => VkSampleCountFlagBits._4,
        8 => VkSampleCountFlagBits._8,
        _ => throw new ArgumentOutOfRangeException(nameof(n), n, "sample count must be 1, 2, 4 or 8"),
    };

    public static VkBufferUsageFlagBits BufferUsage(BufferUsage u)
    {
        var r = (VkBufferUsageFlagBits)0;
        if ((u & Gpu.BufferUsage.Vertex) != 0)
        {
            r |= VkBufferUsageFlagBits.VertexBuffer;
        }

        if ((u & Gpu.BufferUsage.Index) != 0)
        {
            r |= VkBufferUsageFlagBits.IndexBuffer;
        }

        if ((u & Gpu.BufferUsage.Uniform) != 0)
        {
            r |= VkBufferUsageFlagBits.UniformBuffer;
        }

        if ((u & Gpu.BufferUsage.Storage) != 0)
        {
            r |= VkBufferUsageFlagBits.StorageBuffer;
        }

        if ((u & Gpu.BufferUsage.CopySource) != 0)
        {
            r |= VkBufferUsageFlagBits.TransferSrc;
        }

        if ((u & Gpu.BufferUsage.CopyDestination) != 0)
        {
            r |= VkBufferUsageFlagBits.TransferDst;
        }

        return r;
    }
}
