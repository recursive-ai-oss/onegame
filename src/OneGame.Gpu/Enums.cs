namespace OneGame.Gpu;

/// <summary>Pixel formats of textures and render targets. Chosen to exist on Vulkan, Metal and WebGPU alike.</summary>
public enum GpuFormat
{
    /// <summary>No format.</summary>
    Undefined = 0,
    /// <summary>8-bit single channel, normalized.</summary>
    R8Unorm,
    /// <summary>8-bit two channels, normalized.</summary>
    Rg8Unorm,
    /// <summary>8-bit RGBA, normalized, linear.</summary>
    Rgba8Unorm,
    /// <summary>8-bit RGBA with sRGB encoding on color channels.</summary>
    Rgba8UnormSrgb,
    /// <summary>8-bit BGRA, normalized, linear.</summary>
    Bgra8Unorm,
    /// <summary>8-bit BGRA with sRGB encoding on color channels.</summary>
    Bgra8UnormSrgb,
    /// <summary>16-bit float single channel.</summary>
    R16Float,
    /// <summary>16-bit float RGBA.</summary>
    Rgba16Float,
    /// <summary>32-bit float single channel.</summary>
    R32Float,
    /// <summary>32-bit float two channels.</summary>
    Rg32Float,
    /// <summary>32-bit float RGBA.</summary>
    Rgba32Float,
    /// <summary>32-bit unsigned integer single channel.</summary>
    R32Uint,
    /// <summary>32-bit float depth.</summary>
    Depth32Float,
    /// <summary>32-bit float depth with 8-bit stencil.</summary>
    Depth32FloatStencil8,
    /// <summary>16-bit float, two channels.</summary>
    Rg16Float,
    /// <summary>Packed 11/11/10-bit unsigned float RGB: a compact HDR color format. Not usable as a storage texture.</summary>
    Rg11B10Float,
    /// <summary>Packed 10/10/10-bit RGB with 2-bit alpha, normalized.</summary>
    Rgb10A2Unorm,
    /// <summary>16-bit normalized depth.</summary>
    Depth16Unorm,
}

/// <summary>The shape of a texture as shaders see it.</summary>
public enum TextureDimension
{
    /// <summary>A 2D image, or a 2D array when it has more than one layer.</summary>
    D2,
    /// <summary>A cube map (6 layers) or cube array (a multiple of 6 layers). Layer order is +X, -X, +Y, -Y, +Z, -Z.</summary>
    Cube,
    /// <summary>A volume texture.</summary>
    D3,
}

/// <summary>How a view of a texture presents it to a shader.</summary>
public enum TextureViewKind
{
    /// <summary>Derived from the texture: 2D, 2D array, cube, cube array or 3D; a view of exactly one layer of a 2D or cube texture is 2D.</summary>
    Auto,
    /// <summary>One 2D image.</summary>
    D2,
    /// <summary>A 2D array.</summary>
    D2Array,
    /// <summary>One cube map (6 layers).</summary>
    Cube,
    /// <summary>A cube array.</summary>
    CubeArray,
    /// <summary>A volume.</summary>
    D3,
}

/// <summary>How a texture may be used. Combine flags; a texture can only be used in the ways it was created for.</summary>
[Flags]
public enum TextureUsage
{
    /// <summary>No usage.</summary>
    None = 0,
    /// <summary>Sampled in shaders.</summary>
    Sampled = 1,
    /// <summary>Written by shaders as a storage image.</summary>
    Storage = 2,
    /// <summary>Rendered into as a color or depth attachment.</summary>
    RenderTarget = 4,
    /// <summary>Source of copies and readback.</summary>
    CopySource = 8,
    /// <summary>Destination of copies and uploads.</summary>
    CopyDestination = 16,
}

/// <summary>How a buffer may be used.</summary>
[Flags]
public enum BufferUsage
{
    /// <summary>No usage.</summary>
    None = 0,
    /// <summary>Vertex data.</summary>
    Vertex = 1,
    /// <summary>Index data.</summary>
    Index = 2,
    /// <summary>Uniform (constant) buffer.</summary>
    Uniform = 4,
    /// <summary>Storage buffer, readable and writable by shaders.</summary>
    Storage = 8,
    /// <summary>Source of copies and readback.</summary>
    CopySource = 16,
    /// <summary>Destination of copies and uploads.</summary>
    CopyDestination = 32,
    /// <summary>Holds indirect draw or dispatch arguments.</summary>
    Indirect = 64,
}

/// <summary>Shader stage of a module or a resource's visibility.</summary>
[Flags]
public enum ShaderStage
{
    /// <summary>No stage.</summary>
    None = 0,
    /// <summary>Vertex stage.</summary>
    Vertex = 1,
    /// <summary>Pixel (fragment) stage.</summary>
    Pixel = 2,
    /// <summary>Compute stage.</summary>
    Compute = 4,
    /// <summary>Every stage.</summary>
    All = Vertex | Pixel | Compute,
}

/// <summary>Kind of a shader resource binding.</summary>
public enum ResourceKind
{
    /// <summary>Constant (uniform) buffer.</summary>
    ConstantBuffer,
    /// <summary>Read-write storage buffer.</summary>
    StorageBuffer,
    /// <summary>Storage buffer the shader only reads.</summary>
    ReadOnlyStorageBuffer,
    /// <summary>Sampled texture (separate from its sampler).</summary>
    SampledTexture,
    /// <summary>Sampler state.</summary>
    Sampler,
    /// <summary>Read-write storage texture.</summary>
    StorageTexture,
}

/// <summary>Format of one vertex attribute in memory.</summary>
public enum VertexFormat
{
    /// <summary>One 32-bit float.</summary>
    Float1,
    /// <summary>Two 32-bit floats.</summary>
    Float2,
    /// <summary>Three 32-bit floats.</summary>
    Float3,
    /// <summary>Four 32-bit floats.</summary>
    Float4,
    /// <summary>Two 16-bit floats.</summary>
    Half2,
    /// <summary>Four 16-bit floats.</summary>
    Half4,
    /// <summary>One 32-bit unsigned integer.</summary>
    UInt1,
    /// <summary>Two 32-bit unsigned integers.</summary>
    UInt2,
    /// <summary>Four 32-bit unsigned integers.</summary>
    UInt4,
    /// <summary>One 32-bit signed integer.</summary>
    SInt1,
    /// <summary>Two 32-bit signed integers.</summary>
    SInt2,
    /// <summary>Four 8-bit unsigned values, normalized to 0..1.</summary>
    UNorm8x4,
    /// <summary>Four 8-bit unsigned integers.</summary>
    UInt8x4,
    /// <summary>Two 16-bit unsigned values, normalized to 0..1.</summary>
    UNorm16x2,
    /// <summary>Four 16-bit unsigned values, normalized to 0..1.</summary>
    UNorm16x4,
}

/// <summary>How vertices are assembled into primitives.</summary>
public enum PrimitiveTopology
{
    /// <summary>Every three vertices form a triangle.</summary>
    TriangleList,
    /// <summary>Each vertex after the second forms a triangle with the previous two.</summary>
    TriangleStrip,
    /// <summary>Every two vertices form a line.</summary>
    LineList,
    /// <summary>Each vertex is a point.</summary>
    PointList,
}

/// <summary>Which triangle faces are discarded.</summary>
public enum CullMode
{
    /// <summary>Nothing is culled.</summary>
    None,
    /// <summary>Front faces are culled.</summary>
    Front,
    /// <summary>Back faces are culled.</summary>
    Back,
}

/// <summary>Winding order that makes a triangle front-facing, as seen with clip-space +Y up.</summary>
public enum FrontFace
{
    /// <summary>Counter-clockwise is front-facing.</summary>
    CounterClockwise,
    /// <summary>Clockwise is front-facing.</summary>
    Clockwise,
}

/// <summary>Comparison used by depth tests.</summary>
public enum CompareFunction
{
    /// <summary>Never passes.</summary>
    Never,
    /// <summary>Passes when less.</summary>
    Less,
    /// <summary>Passes when equal.</summary>
    Equal,
    /// <summary>Passes when less or equal.</summary>
    LessEqual,
    /// <summary>Passes when greater.</summary>
    Greater,
    /// <summary>Passes when not equal.</summary>
    NotEqual,
    /// <summary>Passes when greater or equal.</summary>
    GreaterEqual,
    /// <summary>Always passes.</summary>
    Always,
}

/// <summary>Factor applied to a blend operand.</summary>
public enum BlendFactor
{
    /// <summary>0.</summary>
    Zero,
    /// <summary>1.</summary>
    One,
    /// <summary>Source color.</summary>
    Source,
    /// <summary>1 - source color.</summary>
    OneMinusSource,
    /// <summary>Source alpha.</summary>
    SourceAlpha,
    /// <summary>1 - source alpha.</summary>
    OneMinusSourceAlpha,
    /// <summary>Destination color.</summary>
    Destination,
    /// <summary>1 - destination color.</summary>
    OneMinusDestination,
    /// <summary>Destination alpha.</summary>
    DestinationAlpha,
    /// <summary>1 - destination alpha.</summary>
    OneMinusDestinationAlpha,
}

/// <summary>Operation combining the blended source and destination.</summary>
public enum BlendOperation
{
    /// <summary>Source + destination.</summary>
    Add,
    /// <summary>Source - destination.</summary>
    Subtract,
    /// <summary>Destination - source.</summary>
    ReverseSubtract,
    /// <summary>Per-channel minimum.</summary>
    Min,
    /// <summary>Per-channel maximum.</summary>
    Max,
}

/// <summary>Color channels a pipeline writes.</summary>
[Flags]
public enum ColorWriteMask
{
    /// <summary>No channel.</summary>
    None = 0,
    /// <summary>Red.</summary>
    Red = 1,
    /// <summary>Green.</summary>
    Green = 2,
    /// <summary>Blue.</summary>
    Blue = 4,
    /// <summary>Alpha.</summary>
    Alpha = 8,
    /// <summary>All channels.</summary>
    All = 15,
}

/// <summary>Texture filtering.</summary>
public enum FilterMode
{
    /// <summary>Nearest texel.</summary>
    Nearest,
    /// <summary>Linear interpolation.</summary>
    Linear,
}

/// <summary>Texture coordinate handling outside 0..1.</summary>
public enum AddressMode
{
    /// <summary>Clamp to the edge texel.</summary>
    ClampToEdge,
    /// <summary>Repeat.</summary>
    Repeat,
    /// <summary>Repeat, mirroring every other tile.</summary>
    MirrorRepeat,
}

/// <summary>What happens to an attachment's contents when a pass begins.</summary>
public enum LoadOp
{
    /// <summary>Keep the existing contents.</summary>
    Load,
    /// <summary>Clear to the given value.</summary>
    Clear,
    /// <summary>Contents are undefined; the pass overwrites everything.</summary>
    DontCare,
}

/// <summary>What happens to an attachment's contents when a pass ends.</summary>
public enum StoreOp
{
    /// <summary>Keep the results.</summary>
    Store,
    /// <summary>Results are not needed afterwards.</summary>
    Discard,
}

/// <summary>Size of index values.</summary>
public enum IndexFormat
{
    /// <summary>16-bit indices.</summary>
    Index16,
    /// <summary>32-bit indices.</summary>
    Index32,
}

/// <summary>How often a vertex buffer advances.</summary>
public enum VertexStepMode
{
    /// <summary>Once per vertex.</summary>
    Vertex,
    /// <summary>Once per instance.</summary>
    Instance,
}

/// <summary>Kind of graphics adapter.</summary>
public enum AdapterKind
{
    /// <summary>Unknown.</summary>
    Other,
    /// <summary>Discrete GPU with its own memory.</summary>
    Discrete,
    /// <summary>GPU integrated with the CPU.</summary>
    Integrated,
    /// <summary>Virtualized GPU.</summary>
    Virtual,
    /// <summary>Software rasterizer running on the CPU.</summary>
    Software,
}

/// <summary>How a swapchain paces presentation.</summary>
public enum PresentMode
{
    /// <summary>Wait for vertical blank; always available.</summary>
    Vsync,
    /// <summary>Wait for vertical blank but replace the queued image with the newest, when supported.</summary>
    LowLatency,
    /// <summary>Present immediately, which may tear, when supported.</summary>
    Immediate,
}

/// <summary>Result of acquiring the next swapchain image.</summary>
public enum AcquireStatus
{
    /// <summary>An image was acquired.</summary>
    Ready,
    /// <summary>The surface has no drawable area right now (minimized), or no image became free within <see cref="GpuSwapchain.AcquireTimeout"/>. Skip the frame.</summary>
    Skipped,
}

/// <summary>Native window system a <see cref="SurfaceSource"/> belongs to.</summary>
public enum SurfaceKind
{
    /// <summary>X11: a Display pointer and a Window id.</summary>
    X11,
    /// <summary>Wayland: a wl_display pointer and a wl_surface pointer.</summary>
    Wayland,
    /// <summary>Win32: an HINSTANCE and an HWND.</summary>
    Win32,
    /// <summary>Android: an ANativeWindow pointer.</summary>
    Android,
    /// <summary>Metal: a CAMetalLayer pointer (macOS and iOS).</summary>
    Metal,
}

/// <summary>The graphics API a <see cref="GpuDevice"/> runs on.</summary>
public enum GpuBackend
{
    /// <summary>Vulkan 1.3.</summary>
    Vulkan,
    /// <summary>Metal.</summary>
    Metal,
}
