namespace OneGame.Gpu;

/// <summary>
/// A graphics device: creates resources, records and submits work. Methods are not thread-safe; use one device from one thread.
/// Work runs in submission order. Resources must not be disposed while an unsubmitted encoder still uses them.
/// </summary>
/// <remarks>
/// Conventions that hold on every backend: clip space has +Y up and depth 0..1; framebuffer origin is the top-left;
/// resource states and barriers are handled by the backend.
/// </remarks>
public abstract class GpuDevice : IDisposable
{
    /// <summary>The adapter this device runs on.</summary>
    public abstract GpuAdapterInfo Adapter { get; }

    /// <summary>Number of submissions allowed in flight before <see cref="Submit"/> waits.</summary>
    public const int MaxFramesInFlight = 2;

    /// <summary>Creates a buffer. Contents are undefined until written.</summary>
    public abstract GpuBuffer CreateBuffer(in BufferDesc desc);

    /// <summary>Creates a texture. Contents are undefined until written or rendered.</summary>
    public abstract GpuTexture CreateTexture(in TextureDesc desc);

    /// <summary>Creates a sampler.</summary>
    public abstract GpuSampler CreateSampler(in SamplerDesc desc);

    /// <summary>Creates a shader from SPIR-V code (4-byte aligned words, little-endian).</summary>
    public abstract GpuShader CreateShader(ShaderStage stage, ReadOnlySpan<byte> spirv, string entryPoint);

    /// <summary>Creates a pipeline layout from the resource bindings of the pipeline's shaders. Entries from several shaders may repeat; identical ones merge and their stages combine.</summary>
    public abstract GpuPipelineLayout CreatePipelineLayout(ReadOnlySpan<ResourceBinding> bindings);

    /// <summary>Creates a bind group for one set of a pipeline layout. Entries must cover every binding of that set.</summary>
    public abstract GpuBindGroup CreateBindGroup(GpuPipelineLayout layout, uint setIndex, ReadOnlySpan<BindGroupEntry> entries);

    /// <summary>Creates a graphics pipeline.</summary>
    public abstract GpuGraphicsPipeline CreateGraphicsPipeline(GraphicsPipelineDesc desc);

    /// <summary>Creates a compute pipeline.</summary>
    public abstract GpuComputePipeline CreateComputePipeline(ComputePipelineDesc desc);

    /// <summary>Creates a swapchain on a native window.</summary>
    public abstract GpuSwapchain CreateSwapchain(in SurfaceSource surface, in SwapchainDesc desc);

    /// <summary>Starts recording commands.</summary>
    public abstract GpuCommandEncoder BeginCommands();

    /// <summary>Queues recorded commands. Pending uploads run first. Waits when too many submissions are still in flight.</summary>
    public abstract void Submit(GpuCommandEncoder commands);

    /// <summary>Queues a copy of bytes into a buffer; it runs before the next submitted commands.</summary>
    public abstract void WriteBuffer(GpuBuffer buffer, ulong offset, ReadOnlySpan<byte> data);

    /// <summary>Queues a copy of tightly packed rows into a texture mip level; it runs before the next submitted commands.</summary>
    public abstract void WriteTexture(GpuTexture texture, uint mipLevel, uint layer, ReadOnlySpan<byte> data);

    /// <summary>Waits for all submitted work, then copies buffer bytes to <paramref name="destination"/>.</summary>
    public abstract void ReadBuffer(GpuBuffer buffer, ulong offset, Span<byte> destination);

    /// <summary>Waits for all submitted work, then copies a texture mip level into <paramref name="destination"/> with tightly packed rows.</summary>
    public abstract void ReadTexture(GpuTexture texture, uint mipLevel, uint layer, Span<byte> destination);

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
    public static ulong LevelByteSize(GpuFormat format, uint width, uint height, uint mipLevel)
    {
        var w = Math.Max(width >> (int)mipLevel, 1u);
        var h = Math.Max(height >> (int)mipLevel, 1u);
        return (ulong)w * h * BytesPerTexel(format);
    }

    /// <summary>Bytes per texel of a format.</summary>
    public static uint BytesPerTexel(GpuFormat format) => format switch
    {
        GpuFormat.R8Unorm => 1,
        GpuFormat.Rg8Unorm or GpuFormat.R16Float => 2,
        GpuFormat.Rgba8Unorm or GpuFormat.Rgba8UnormSrgb or GpuFormat.Bgra8Unorm or GpuFormat.Bgra8UnormSrgb
            or GpuFormat.R32Float or GpuFormat.R32Uint or GpuFormat.Depth32Float => 4,
        GpuFormat.Rgba16Float or GpuFormat.Rg32Float or GpuFormat.Depth32FloatStencil8 => 8,
        GpuFormat.Rgba32Float => 16,
        _ => throw new ArgumentOutOfRangeException(nameof(format)),
    };
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
