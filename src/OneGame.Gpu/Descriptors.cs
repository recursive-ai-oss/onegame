namespace OneGame.Gpu;

/// <summary>Describes one vertex attribute: where a shader input comes from inside a vertex.</summary>
/// <param name="Location">Shader input location.</param>
/// <param name="Format">Memory format.</param>
/// <param name="Offset">Byte offset inside the vertex.</param>
public readonly record struct VertexElement(uint Location, VertexFormat Format, uint Offset);

/// <summary>Describes one shader resource binding. Shader tooling emits these; pipeline layouts are built from them.</summary>
/// <param name="Set">Bind group index.</param>
/// <param name="Binding">Binding index inside the group.</param>
/// <param name="Count">Array size (1 for a single resource).</param>
/// <param name="Kind">Resource kind.</param>
/// <param name="Stages">Stages that use the resource.</param>
public readonly record struct ResourceBinding(uint Set, uint Binding, uint Count, ResourceKind Kind, ShaderStage Stages);

/// <summary>Describes a buffer to create.</summary>
/// <param name="Size">Size in bytes.</param>
/// <param name="Usage">Allowed uses.</param>
public readonly record struct BufferDesc(ulong Size, BufferUsage Usage)
{
    /// <summary>Optional name shown in debugging tools.</summary>
    public string? Label { get; init; }
}

/// <summary>Describes a 2D texture (or 2D array) to create.</summary>
/// <param name="Width">Width in texels.</param>
/// <param name="Height">Height in texels.</param>
/// <param name="Format">Pixel format.</param>
/// <param name="Usage">Allowed uses.</param>
public readonly record struct TextureDesc(uint Width, uint Height, GpuFormat Format, TextureUsage Usage)
{
    /// <summary>Number of mip levels; 1 by default. <see cref="GpuTexture.FullMipCount"/> computes the full chain.</summary>
    public uint MipLevels { get; init; } = 1;

    /// <summary>Number of array layers; more than 1 makes the texture an array that shaders sample as a 2D array.</summary>
    public uint ArrayLayers { get; init; } = 1;

    /// <summary>Samples per texel for multisampled render targets (1, 2, 4 or 8).</summary>
    public uint SampleCount { get; init; } = 1;

    /// <summary>Optional name shown in debugging tools.</summary>
    public string? Label { get; init; }
}

/// <summary>Describes a sampler to create.</summary>
public readonly record struct SamplerDesc
{
    /// <summary>Filter when magnifying.</summary>
    public FilterMode MagFilter { get; init; }

    /// <summary>Filter when minifying.</summary>
    public FilterMode MinFilter { get; init; }

    /// <summary>Filter between mip levels.</summary>
    public FilterMode MipFilter { get; init; }

    /// <summary>Addressing in U.</summary>
    public AddressMode AddressU { get; init; }

    /// <summary>Addressing in V.</summary>
    public AddressMode AddressV { get; init; }

    /// <summary>Maximum anisotropy; 1 disables it.</summary>
    public float MaxAnisotropy { get; init; }

    /// <summary>Linear filtering with clamped edges.</summary>
    public static SamplerDesc Linear => new() { MagFilter = FilterMode.Linear, MinFilter = FilterMode.Linear, MipFilter = FilterMode.Linear, MaxAnisotropy = 1 };

    /// <summary>Nearest filtering with clamped edges.</summary>
    public static SamplerDesc Nearest => new() { MaxAnisotropy = 1 };
}

/// <summary>Blend settings for one of color or alpha.</summary>
/// <param name="Source">Factor for the source value.</param>
/// <param name="Destination">Factor for the destination value.</param>
/// <param name="Operation">How the two combine.</param>
public readonly record struct BlendComponent(BlendFactor Source, BlendFactor Destination, BlendOperation Operation);

/// <summary>Blending of one color target.</summary>
/// <param name="Color">Color channels.</param>
/// <param name="Alpha">Alpha channel.</param>
public readonly record struct BlendState(BlendComponent Color, BlendComponent Alpha)
{
    /// <summary>Straight-alpha blending: source * alpha + destination * (1 - alpha).</summary>
    public static BlendState AlphaBlend { get; } = new(
        new BlendComponent(BlendFactor.SourceAlpha, BlendFactor.OneMinusSourceAlpha, BlendOperation.Add),
        new BlendComponent(BlendFactor.One, BlendFactor.OneMinusSourceAlpha, BlendOperation.Add));

    /// <summary>Premultiplied-alpha blending: source + destination * (1 - alpha).</summary>
    public static BlendState Premultiplied { get; } = new(
        new BlendComponent(BlendFactor.One, BlendFactor.OneMinusSourceAlpha, BlendOperation.Add),
        new BlendComponent(BlendFactor.One, BlendFactor.OneMinusSourceAlpha, BlendOperation.Add));
}

/// <summary>A color target of a graphics pipeline.</summary>
/// <param name="Format">Format of the attachment this pipeline renders to.</param>
public readonly record struct ColorTargetState(GpuFormat Format)
{
    /// <summary>Blending, or null to overwrite.</summary>
    public BlendState? Blend { get; init; }

    /// <summary>Channels written.</summary>
    public ColorWriteMask WriteMask { get; init; } = ColorWriteMask.All;
}

/// <summary>Depth testing of a graphics pipeline.</summary>
/// <param name="Format">Depth attachment format.</param>
public readonly record struct DepthStencilState(GpuFormat Format)
{
    /// <summary>Whether passing fragments write depth.</summary>
    public bool DepthWrite { get; init; } = true;

    /// <summary>Depth comparison.</summary>
    public CompareFunction DepthCompare { get; init; } = CompareFunction.Less;
}

/// <summary>Layout of one vertex buffer binding.</summary>
public sealed class VertexBufferLayout
{
    /// <summary>Bytes from one element to the next.</summary>
    public required uint Stride { get; init; }

    /// <summary>Whether the buffer advances per vertex or per instance.</summary>
    public VertexStepMode StepMode { get; init; }

    /// <summary>Attributes read from this buffer.</summary>
    public required ReadOnlyMemory<VertexElement> Attributes { get; init; }
}

/// <summary>Describes a graphics pipeline.</summary>
public sealed class GraphicsPipelineDesc
{
    /// <summary>Resource layout shared with bind groups.</summary>
    public required GpuPipelineLayout Layout { get; init; }

    /// <summary>Vertex stage.</summary>
    public required GpuShader Vertex { get; init; }

    /// <summary>Pixel stage; null for depth-only pipelines.</summary>
    public GpuShader? Pixel { get; init; }

    /// <summary>Vertex buffers, in binding order.</summary>
    public IReadOnlyList<VertexBufferLayout> VertexBuffers { get; init; } = [];

    /// <summary>Primitive assembly.</summary>
    public PrimitiveTopology Topology { get; init; }

    /// <summary>Face culling.</summary>
    public CullMode Cull { get; init; }

    /// <summary>Front-face winding.</summary>
    public FrontFace Front { get; init; }

    /// <summary>Color targets, matching the pass's color attachments.</summary>
    public IReadOnlyList<ColorTargetState> ColorTargets { get; init; } = [];

    /// <summary>Depth testing, or null for none.</summary>
    public DepthStencilState? DepthStencil { get; init; }

    /// <summary>Samples per pixel; must match the pass's attachments.</summary>
    public uint SampleCount { get; init; } = 1;

    /// <summary>Optional name shown in debugging tools.</summary>
    public string? Label { get; init; }
}

/// <summary>Describes a compute pipeline.</summary>
public sealed class ComputePipelineDesc
{
    /// <summary>Resource layout shared with bind groups.</summary>
    public required GpuPipelineLayout Layout { get; init; }

    /// <summary>Compute shader.</summary>
    public required GpuShader Compute { get; init; }

    /// <summary>Optional name shown in debugging tools.</summary>
    public string? Label { get; init; }
}

/// <summary>One resource bound at a binding index of a bind group.</summary>
public readonly record struct BindGroupEntry
{
    /// <summary>Binding index inside the group.</summary>
    public uint Binding { get; init; }

    /// <summary>Bound buffer, for buffer kinds.</summary>
    public GpuBuffer? Buffer { get; init; }

    /// <summary>Byte offset into <see cref="Buffer"/>.</summary>
    public ulong Offset { get; init; }

    /// <summary>Bytes visible to the shader; 0 means the rest of the buffer.</summary>
    public ulong Size { get; init; }

    /// <summary>Bound texture, for texture kinds.</summary>
    public GpuTexture? Texture { get; init; }

    /// <summary>Bound sampler, for sampler kinds.</summary>
    public GpuSampler? Sampler { get; init; }

    /// <summary>Binds a whole buffer.</summary>
    public static BindGroupEntry ForBuffer(uint binding, GpuBuffer buffer) => new() { Binding = binding, Buffer = buffer };

    /// <summary>Binds a range of a buffer.</summary>
    public static BindGroupEntry ForBuffer(uint binding, GpuBuffer buffer, ulong offset, ulong size) =>
        new() { Binding = binding, Buffer = buffer, Offset = offset, Size = size };

    /// <summary>Binds a texture.</summary>
    public static BindGroupEntry ForTexture(uint binding, GpuTexture texture) => new() { Binding = binding, Texture = texture };

    /// <summary>Binds a sampler.</summary>
    public static BindGroupEntry ForSampler(uint binding, GpuSampler sampler) => new() { Binding = binding, Sampler = sampler };
}

/// <summary>A color attachment of a render pass.</summary>
public readonly record struct ColorAttachment
{
    /// <summary>Texture rendered into; must have <see cref="TextureUsage.RenderTarget"/>.</summary>
    public required GpuTexture Target { get; init; }

    /// <summary>Mip level rendered into.</summary>
    public uint MipLevel { get; init; }

    /// <summary>Array layer rendered into.</summary>
    public uint Layer { get; init; }

    /// <summary>Single-sample texture that receives the resolved image when <see cref="Target"/> is multisampled.</summary>
    public GpuTexture? Resolve { get; init; }

    /// <summary>What happens at the start of the pass.</summary>
    public LoadOp Load { get; init; }

    /// <summary>What happens at the end of the pass.</summary>
    public StoreOp Store { get; init; }

    /// <summary>Clear red.</summary>
    public float ClearR { get; init; }

    /// <summary>Clear green.</summary>
    public float ClearG { get; init; }

    /// <summary>Clear blue.</summary>
    public float ClearB { get; init; }

    /// <summary>Clear alpha.</summary>
    public float ClearA { get; init; }

    /// <summary>A color attachment that clears to the given color and keeps the result.</summary>
    public static ColorAttachment Clear(GpuTexture target, float r, float g, float b, float a) =>
        new() { Target = target, Load = LoadOp.Clear, Store = StoreOp.Store, ClearR = r, ClearG = g, ClearB = b, ClearA = a };
}

/// <summary>The depth attachment of a render pass.</summary>
public readonly record struct DepthAttachment
{
    /// <summary>Depth texture; must have <see cref="TextureUsage.RenderTarget"/>.</summary>
    public required GpuTexture Target { get; init; }

    /// <summary>What happens at the start of the pass.</summary>
    public LoadOp Load { get; init; }

    /// <summary>What happens at the end of the pass.</summary>
    public StoreOp Store { get; init; }

    /// <summary>Depth value to clear to.</summary>
    public float ClearDepth { get; init; }
}

/// <summary>Where a swapchain draws: the native handles of a window the platform layer created.</summary>
public readonly record struct SurfaceSource
{
    /// <summary>Window system.</summary>
    public SurfaceKind Kind { get; init; }

    /// <summary>First handle: Display*, wl_display*, or HINSTANCE.</summary>
    public nint Display { get; init; }

    /// <summary>Second handle: the X11 Window id, wl_surface*, or HWND.</summary>
    public nint Window { get; init; }

    /// <summary>An X11 window.</summary>
    public static SurfaceSource X11(nint display, nint window) => new() { Kind = SurfaceKind.X11, Display = display, Window = window };

    /// <summary>A Wayland surface.</summary>
    public static SurfaceSource Wayland(nint display, nint surface) => new() { Kind = SurfaceKind.Wayland, Display = display, Window = surface };

    /// <summary>A Win32 window.</summary>
    public static SurfaceSource Win32(nint hinstance, nint hwnd) => new() { Kind = SurfaceKind.Win32, Display = hinstance, Window = hwnd };
}

/// <summary>Describes a swapchain to create.</summary>
/// <param name="Width">Initial width in pixels; used when the window system does not report one.</param>
/// <param name="Height">Initial height in pixels.</param>
public readonly record struct SwapchainDesc(uint Width, uint Height)
{
    /// <summary>Whether images are sRGB-encoded (BGRA8 sRGB) or linear (BGRA8). The actual format is in <see cref="GpuSwapchain.Format"/>.</summary>
    public bool Srgb { get; init; }

    /// <summary>Presentation pacing.</summary>
    public PresentMode PresentMode { get; init; }

    /// <summary>Extra usage flags for the backbuffers besides <see cref="TextureUsage.RenderTarget"/> and <see cref="TextureUsage.CopySource"/>.</summary>
    public TextureUsage ExtraUsage { get; init; }
}

/// <summary>Information about a graphics adapter.</summary>
/// <param name="Name">Driver-reported device name.</param>
/// <param name="Kind">Adapter kind.</param>
/// <param name="VendorId">PCI vendor id.</param>
/// <param name="DeviceId">PCI device id.</param>
/// <param name="ApiVersion">Highest graphics API version supported, as "major.minor.patch".</param>
public sealed record GpuAdapterInfo(string Name, AdapterKind Kind, uint VendorId, uint DeviceId, string ApiVersion);

/// <summary>Options for creating a device.</summary>
public sealed class GpuDeviceOptions
{
    /// <summary>Turns on the graphics API's validation layer when available. The environment variable ONEGAME_GPU_VALIDATION=1 also turns it on.</summary>
    public bool Validation { get; init; }

    /// <summary>When set with <see cref="Validation"/>, creation fails if the validation layer is missing instead of running without it.</summary>
    public bool RequireValidation { get; init; }

    /// <summary>Picks the adapter whose name contains this text (case-insensitive) or whose kind is named by it ("software", "discrete", "integrated"). The environment variable ONEGAME_GPU_ADAPTER overrides it.</summary>
    public string? Adapter { get; init; }

    /// <summary>Application name reported to the driver.</summary>
    public string ApplicationName { get; init; } = "onegame";
}
