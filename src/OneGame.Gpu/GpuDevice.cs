using System.Threading.Tasks;

namespace OneGame.Gpu;

/// <summary>
/// A graphics device: creates resources, records and submits work. Methods are not thread-safe; use one device from one thread.
/// Work runs in submission order. Resources must not be disposed while an unsubmitted encoder still uses them.
/// </summary>
/// <remarks>
/// Conventions that hold on every backend: clip space has +Y up and depth 0..1; framebuffer origin is the top-left;
/// resource states and barriers are handled by the backend.
/// </remarks>
/// <example>
/// Clear an offscreen image and read it back. The device is the low-level layer; most games draw through the 2D and 3D renderers, which use it.
/// <code>
/// using var device = GpuBackends.CreateDevice();
/// using var target = device.CreateTexture(new TextureDesc(8, 8, GpuFormat.Rgba8Unorm, TextureUsage.RenderTarget | TextureUsage.CopySource));
///
/// var commands = device.BeginCommands();
/// commands.BeginRenderPass(ColorAttachment.Clear(target, 1f, 0.5f, 0f, 1f)).EndPass();
/// device.Submit(commands);
///
/// var pixels = new byte[8 * 8 * 4];
/// device.ReadTexture(target, mipLevel: 0, layer: 0, pixels);   // waits for the GPU; rows are tightly packed
/// </code>
/// </example>
public abstract class GpuDevice : IDisposable
{
    /// <summary>The adapter this device runs on.</summary>
    public abstract GpuAdapterInfo Adapter { get; }

    /// <summary>The graphics API this device runs on.</summary>
    public abstract GpuBackend Backend { get; }

    /// <summary>Number of submissions allowed in flight before <see cref="Submit"/> waits.</summary>
    public const int MaxFramesInFlight = 2;

    /// <summary>Dynamic buffer offsets must be multiples of this many bytes. 256 is valid on every backend.</summary>
    public const uint DynamicOffsetAlignment = 256;

    /// <summary>Most bytes of push constants a pipeline layout may declare. 128 is guaranteed on every backend.</summary>
    public const uint MaxPushConstantBytes = 128;

    /// <summary>Optional features and limits of this device.</summary>
    public abstract GpuCapabilities Capabilities { get; }

    /// <summary>Creates a buffer. Contents are undefined until written with <see cref="WriteBuffer"/>.</summary>
    /// <param name="desc">Size and allowed uses.</param>
    /// <exception cref="GpuOutOfMemoryException">The device has no memory left.</exception>
    public abstract GpuBuffer CreateBuffer(in BufferDesc desc);

    /// <summary>Creates a texture. Contents are undefined until written (<see cref="WriteTexture(GpuTexture, uint, uint, ReadOnlySpan{byte})"/>) or rendered into.</summary>
    /// <param name="desc">Size, format, mip levels and allowed uses.</param>
    /// <exception cref="GpuOutOfMemoryException">The device has no memory left.</exception>
    /// <exception cref="GpuUnsupportedException">The format or usage is not supported by this device.</exception>
    public abstract GpuTexture CreateTexture(in TextureDesc desc);

    /// <summary>Creates a sampler: how a shader filters and wraps texture reads.</summary>
    /// <param name="desc">Filters, addressing and level-of-detail range.</param>
    public abstract GpuSampler CreateSampler(in SamplerDesc desc);

    /// <summary>Creates a shader from SPIR-V code (4-byte aligned words, little-endian).</summary>
    /// <param name="stage">The shader stage.</param>
    /// <param name="spirv">The SPIR-V module.</param>
    /// <param name="entryPoint">The entry point's name.</param>
    public abstract GpuShader CreateShader(ShaderStage stage, ReadOnlySpan<byte> spirv, string entryPoint);

    /// <summary>
    /// Creates a shader from a compiled entry point, using the form this backend needs (SPIR-V for Vulkan, Metal Shading Language for Metal).
    /// Prefer this over the SPIR-V overload: it works on every backend.
    /// </summary>
    /// <exception cref="GpuUnsupportedException">The source has no code for this backend.</exception>
    public virtual GpuShader CreateShader(in ShaderSource source)
    {
        if (source.SpirV.IsEmpty)
        {
            throw new GpuUnsupportedException($"shader {source.EntryPoint} has no SPIR-V code, which the {Backend} backend needs");
        }

        return CreateShader(source.Stage, source.SpirV, source.EntryPoint);
    }

    /// <summary>Creates a pipeline layout from the resource bindings of the pipeline's shaders. Entries from several shaders may repeat; identical ones merge and their stages combine.</summary>
    public abstract GpuPipelineLayout CreatePipelineLayout(ReadOnlySpan<ResourceBinding> bindings);

    /// <summary>Creates a pipeline layout like <see cref="CreatePipelineLayout(ReadOnlySpan{ResourceBinding})"/> that also declares push constants.</summary>
    public abstract GpuPipelineLayout CreatePipelineLayout(ReadOnlySpan<ResourceBinding> bindings, in PushConstantRange pushConstants);

    /// <summary>Creates a set of timestamp queries for timing passes.</summary>
    /// <exception cref="GpuUnsupportedException">The device cannot time passes (<see cref="GpuCapabilities.Timestamps"/> is false).</exception>
    public abstract GpuTimestampSet CreateTimestampSet(uint count);

    /// <summary>
    /// Reads timestamps as nanoseconds since an arbitrary start, without waiting. Returns false when the passes that write them have not
    /// finished (nothing is written to <paramref name="nanoseconds"/> then).
    /// </summary>
    public abstract bool TryReadTimestamps(GpuTimestampSet timestamps, uint first, Span<ulong> nanoseconds);

    /// <summary>Creates a bind group for one set of a pipeline layout. Entries must cover every binding of that set.</summary>
    public abstract GpuBindGroup CreateBindGroup(GpuPipelineLayout layout, uint setIndex, ReadOnlySpan<BindGroupEntry> entries);

    /// <summary>Creates a graphics pipeline: shaders, vertex layout, blend, depth and target formats fixed together. Create pipelines once and reuse them.</summary>
    /// <param name="desc">The pipeline description.</param>
    /// <exception cref="GpuException">The shaders and layout do not agree, or the backend rejected the description.</exception>
    public abstract GpuGraphicsPipeline CreateGraphicsPipeline(GraphicsPipelineDesc desc);

    /// <summary>Creates a compute pipeline.</summary>
    /// <param name="desc">The compute shader and layout.</param>
    public abstract GpuComputePipeline CreateComputePipeline(ComputePipelineDesc desc);

    /// <summary>Creates a swapchain: the images presented to a native window.</summary>
    /// <param name="surface">The window, from the platform layer.</param>
    /// <param name="desc">Initial size, format and presentation pacing.</param>
    /// <exception cref="GpuUnsupportedException">The adapter cannot present to this window.</exception>
    public abstract GpuSwapchain CreateSwapchain(in SurfaceSource surface, in SwapchainDesc desc);

    /// <summary>Starts recording commands. Nothing runs until the encoder is passed to <see cref="Submit"/>.</summary>
    public abstract GpuCommandEncoder BeginCommands();

    /// <summary>Queues recorded commands. Pending uploads run first. Waits when too many submissions are still in flight.</summary>
    /// <param name="commands">The encoder from <see cref="BeginCommands"/>; it is finished by this call and cannot be reused.</param>
    /// <exception cref="GpuDeviceLostException">The driver reset or the GPU was removed.</exception>
    public abstract void Submit(GpuCommandEncoder commands);

    /// <summary>Queues a copy of bytes into a buffer; it runs before the next submitted commands.</summary>
    /// <param name="buffer">The destination; it needs <see cref="BufferUsage.CopyDestination"/>.</param>
    /// <param name="offset">Byte offset into the buffer.</param>
    /// <param name="data">The bytes.</param>
    public abstract void WriteBuffer(GpuBuffer buffer, ulong offset, ReadOnlySpan<byte> data);

    /// <summary>Queues a copy of tightly packed rows into a whole texture mip level (all slices of a volume); it runs before the next submitted commands.</summary>
    /// <param name="texture">The destination; it needs <see cref="TextureUsage.CopyDestination"/>.</param>
    /// <param name="mipLevel">The mip level.</param>
    /// <param name="layer">The array layer or cube face.</param>
    /// <param name="data">Exactly <see cref="LevelByteSize(GpuFormat, uint, uint, uint)"/> bytes for the level.</param>
    public abstract void WriteTexture(GpuTexture texture, uint mipLevel, uint layer, ReadOnlySpan<byte> data);

    /// <summary>
    /// Queues a copy of tightly packed texel rows into a box of a texture (a sub-rectangle of a mip level, a range of slices of a volume).
    /// It runs before the next submitted commands. <paramref name="data"/> holds exactly width × height × depth texels.
    /// </summary>
    public abstract void WriteTexture(GpuTexture texture, in TextureRegion region, ReadOnlySpan<byte> data);

    /// <summary>Waits for all submitted work, then copies buffer bytes to <paramref name="destination"/>. Blocks, so keep it for tools and tests.</summary>
    /// <param name="buffer">The source; it needs <see cref="BufferUsage.CopySource"/>.</param>
    /// <param name="offset">Byte offset into the buffer.</param>
    /// <param name="destination">Receives the bytes; its length is the amount read.</param>
    public abstract void ReadBuffer(GpuBuffer buffer, ulong offset, Span<byte> destination);

    /// <summary>Waits for all submitted work, then copies a texture mip level into <paramref name="destination"/> with tightly packed rows. Blocks, so keep it for tools and tests.</summary>
    /// <param name="texture">The source; it needs <see cref="TextureUsage.CopySource"/>.</param>
    /// <param name="mipLevel">The mip level.</param>
    /// <param name="layer">The array layer or cube face.</param>
    /// <param name="destination">Receives <see cref="LevelByteSize(GpuFormat, uint, uint, uint)"/> bytes.</param>
    public abstract void ReadTexture(GpuTexture texture, uint mipLevel, uint layer, Span<byte> destination);

    /// <summary>
    /// Like <see cref="ReadBuffer"/>, for backends that cannot block (the browser). Native backends complete before returning.
    /// </summary>
    public virtual ValueTask ReadBufferAsync(GpuBuffer buffer, ulong offset, Memory<byte> destination)
    {
        ReadBuffer(buffer, offset, destination.Span);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Like <see cref="ReadTexture"/>, for backends that cannot block (the browser). Native backends complete before returning.
    /// </summary>
    public virtual ValueTask ReadTextureAsync(GpuTexture texture, uint mipLevel, uint layer, Memory<byte> destination)
    {
        ReadTexture(texture, mipLevel, layer, destination.Span);
        return ValueTask.CompletedTask;
    }

    /// <summary>Blocks until the GPU has finished everything submitted.</summary>
    public abstract void WaitIdle();

    /// <summary>
    /// Returns and clears the warnings and errors the validation layer reported since the last call.
    /// Empty when validation is off.
    /// </summary>
    public abstract string[] TakeValidationMessages();

    /// <summary>Waits for the GPU and releases the device and everything created from it.</summary>
    public abstract void Dispose();

    /// <summary>Bytes needed for one tightly packed mip level of a texture.</summary>
    public static ulong LevelByteSize(GpuFormat format, uint width, uint height, uint mipLevel) => LevelByteSize(format, width, height, 1, mipLevel);

    /// <summary>Bytes needed for one tightly packed mip level of a volume texture.</summary>
    public static ulong LevelByteSize(GpuFormat format, uint width, uint height, uint depth, uint mipLevel)
    {
        var w = Math.Max(width >> (int)mipLevel, 1u);
        var h = Math.Max(height >> (int)mipLevel, 1u);
        var d = Math.Max(depth >> (int)mipLevel, 1u);
        return (ulong)w * h * d * BytesPerTexel(format);
    }

    /// <summary>Bytes per texel of a format.</summary>
    public static uint BytesPerTexel(GpuFormat format) => format switch
    {
        GpuFormat.R8Unorm => 1,
        GpuFormat.Rg8Unorm or GpuFormat.R16Float or GpuFormat.Depth16Unorm => 2,
        GpuFormat.Rgba8Unorm or GpuFormat.Rgba8UnormSrgb or GpuFormat.Bgra8Unorm or GpuFormat.Bgra8UnormSrgb
            or GpuFormat.R32Float or GpuFormat.R32Uint or GpuFormat.Depth32Float or GpuFormat.Rg16Float
            or GpuFormat.Rg11B10Float or GpuFormat.Rgb10A2Unorm => 4,
        GpuFormat.Rgba16Float or GpuFormat.Rg32Float or GpuFormat.Depth32FloatStencil8 => 8,
        GpuFormat.Rgba32Float => 16,
        _ => throw new ArgumentOutOfRangeException(nameof(format)),
    };
}

/// <summary>The window's drawing surface is gone, as when an app is sent to the background or the system rebuilds its window. Dispose the swapchain; make a new one once the window has a surface again.</summary>
public sealed class GpuSurfaceLostException : GpuException
{
    /// <summary>Creates the exception.</summary>
    public GpuSurfaceLostException()
        : base("the window surface was lost")
    {
    }
}

/// <summary>An error from the graphics layer.</summary>
public class GpuException : Exception
{
    /// <summary>Creates the exception.</summary>
    public GpuException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a cause.</summary>
    public GpuException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

/// <summary>The system has no usable graphics API, adapter or required feature.</summary>
public sealed class GpuUnsupportedException : GpuException
{
    /// <summary>Creates the exception.</summary>
    public GpuUnsupportedException(string message)
        : base(message)
    {
    }
}

/// <summary>The device ran out of memory.</summary>
public sealed class GpuOutOfMemoryException : GpuException
{
    /// <summary>Creates the exception.</summary>
    public GpuOutOfMemoryException(string message)
        : base(message)
    {
    }
}

/// <summary>The device was lost (driver reset, GPU removed).</summary>
public sealed class GpuDeviceLostException : GpuException
{
    /// <summary>Creates the exception.</summary>
    public GpuDeviceLostException(string message)
        : base(message)
    {
    }
}
