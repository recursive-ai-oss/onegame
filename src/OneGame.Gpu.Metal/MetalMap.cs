using OneGame.Gpu.Metal.Interop;

namespace OneGame.Gpu.Metal;

/// <summary>Conversions from the backend-neutral enums to Metal's.</summary>
internal static class MetalMap
{
    public static nuint Pixel(GpuFormat f) => f switch
    {
        GpuFormat.R8Unorm => MT.R8Unorm,
        GpuFormat.Rg8Unorm => MT.Rg8Unorm,
        GpuFormat.Rgba8Unorm => MT.Rgba8Unorm,
        GpuFormat.Rgba8UnormSrgb => MT.Rgba8UnormSrgb,
        GpuFormat.Bgra8Unorm => MT.Bgra8Unorm,
        GpuFormat.Bgra8UnormSrgb => MT.Bgra8UnormSrgb,
        GpuFormat.R16Float => MT.R16Float,
        GpuFormat.Rg16Float => MT.Rg16Float,
        GpuFormat.Rgba16Float => MT.Rgba16Float,
        GpuFormat.R32Float => MT.R32Float,
        GpuFormat.Rg32Float => MT.Rg32Float,
        GpuFormat.Rgba32Float => MT.Rgba32Float,
        GpuFormat.R32Uint => MT.R32Uint,
        GpuFormat.Rg11B10Float => MT.Rg11B10Float,
        GpuFormat.Rgb10A2Unorm => MT.Rgb10A2Unorm,
        GpuFormat.Depth16Unorm => MT.Depth16Unorm,
        GpuFormat.Depth32Float => MT.Depth32Float,
        GpuFormat.Depth32FloatStencil8 => MT.Depth32FloatStencil8,
        _ => throw new ArgumentException($"format {f} is not supported", nameof(f)),
    };

    public static bool IsDepth(GpuFormat f) => f is GpuFormat.Depth16Unorm or GpuFormat.Depth32Float or GpuFormat.Depth32FloatStencil8;

    public static bool HasStencil(GpuFormat f) => f == GpuFormat.Depth32FloatStencil8;

    // MTLVertexFormat
    public static nuint Vertex(VertexFormat f) => f switch
    {
        VertexFormat.Float1 => 28,
        VertexFormat.Float2 => 29,
        VertexFormat.Float3 => 30,
        VertexFormat.Float4 => 31,
        VertexFormat.Half2 => 25,
        VertexFormat.Half4 => 27,
        VertexFormat.UInt1 => 36,
        VertexFormat.UInt2 => 37,
        VertexFormat.UInt4 => 39,
        VertexFormat.SInt1 => 32,
        VertexFormat.SInt2 => 33,
        VertexFormat.UNorm8x4 => 9,
        VertexFormat.UInt8x4 => 3,
        VertexFormat.UNorm16x2 => 19,
        VertexFormat.UNorm16x4 => 21,
        _ => throw new ArgumentException($"vertex format {f} is not supported", nameof(f)),
    };

    public static nuint Primitive(PrimitiveTopology t) => t switch
    {
        PrimitiveTopology.TriangleList => MT.Triangle,
        PrimitiveTopology.TriangleStrip => MT.TriangleStrip,
        PrimitiveTopology.LineList => MT.Line,
        _ => MT.Point,
    };

    // MTLPrimitiveTopologyClass
    public static nuint TopologyClass(PrimitiveTopology t) => t switch
    {
        PrimitiveTopology.LineList => 2,
        PrimitiveTopology.PointList => 1,
        _ => 3,
    };

    public static nuint Compare(CompareFunction c) => c switch
    {
        CompareFunction.Never => 0,
        CompareFunction.Less => 1,
        CompareFunction.Equal => 2,
        CompareFunction.LessEqual => 3,
        CompareFunction.Greater => 4,
        CompareFunction.NotEqual => 5,
        CompareFunction.GreaterEqual => 6,
        _ => 7,
    };

    public static nuint Blend(BlendFactor f) => f switch
    {
        BlendFactor.Zero => 0,
        BlendFactor.One => 1,
        BlendFactor.Source => 2,
        BlendFactor.OneMinusSource => 3,
        BlendFactor.SourceAlpha => 4,
        BlendFactor.OneMinusSourceAlpha => 5,
        BlendFactor.Destination => 6,
        BlendFactor.OneMinusDestination => 7,
        BlendFactor.DestinationAlpha => 8,
        _ => 9,
    };

    public static nuint BlendOp(BlendOperation o) => o switch
    {
        BlendOperation.Add => 0,
        BlendOperation.Subtract => 1,
        BlendOperation.ReverseSubtract => 2,
        BlendOperation.Min => 3,
        _ => 4,
    };

    // MTLColorWriteMask: alpha is bit 0, blue bit 1, green bit 2, red bit 3.
    public static nuint WriteMask(ColorWriteMask m)
    {
        nuint r = 0;
        if ((m & ColorWriteMask.Red) != 0)
        {
            r |= 8;
        }

        if ((m & ColorWriteMask.Green) != 0)
        {
            r |= 4;
        }

        if ((m & ColorWriteMask.Blue) != 0)
        {
            r |= 2;
        }

        if ((m & ColorWriteMask.Alpha) != 0)
        {
            r |= 1;
        }

        return r;
    }

    public static nuint Filter(FilterMode f) => f == FilterMode.Linear ? 1u : 0u;

    // MTLSamplerAddressMode: ClampToEdge 0, Repeat 2, MirrorRepeat 3.
    public static nuint Address(AddressMode a) => a switch
    {
        AddressMode.Repeat => 2,
        AddressMode.MirrorRepeat => 3,
        _ => 0,
    };

    public static nuint Load(LoadOp l) => l switch
    {
        LoadOp.Clear => MT.LoadClear,
        LoadOp.Load => MT.LoadLoad,
        _ => MT.LoadDontCare,
    };
}
