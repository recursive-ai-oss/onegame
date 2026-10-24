namespace OneGame.Gpu;

/// <summary>Base class of every GPU object. Disposing releases the object once the GPU has finished with it.</summary>
public abstract class GpuObject : IDisposable
{
    /// <summary>Releases the object. The GPU may still be using it; the backend defers the actual release until it is done.</summary>
    public abstract void Dispose();

    /// <summary>Name shown in debugging tools, if one was given.</summary>
    public string? Label { get; protected init; }
}

/// <summary>A block of GPU memory.</summary>
public abstract class GpuBuffer : GpuObject
{
    /// <summary>Size in bytes.</summary>
    public ulong Size { get; protected init; }

    /// <summary>Allowed uses.</summary>
    public BufferUsage Usage { get; protected init; }
}

/// <summary>A 2D image or 2D image array.</summary>
public abstract class GpuTexture : GpuObject
{
    /// <summary>Width in texels.</summary>
    public uint Width { get; protected init; }

    /// <summary>Height in texels.</summary>
    public uint Height { get; protected init; }

    /// <summary>Pixel format.</summary>
    public GpuFormat Format { get; protected init; }

    /// <summary>Allowed uses.</summary>
    public TextureUsage Usage { get; protected init; }

    /// <summary>Number of mip levels.</summary>
    public uint MipLevels { get; protected init; }

    /// <summary>Number of array layers.</summary>
    public uint ArrayLayers { get; protected init; }

    /// <summary>Samples per texel.</summary>
    public uint SampleCount { get; protected init; }

    /// <summary>Number of levels in a full mip chain for the given size.</summary>
    public static uint FullMipCount(uint width, uint height) => (uint)(32 - int.LeadingZeroCount((int)Math.Max(Math.Max(width, height), 1u)));
}

/// <summary>Sampler state.</summary>
public abstract class GpuSampler : GpuObject
{
}

/// <summary>A compiled shader entry point.</summary>
public abstract class GpuShader : GpuObject
{
    /// <summary>Stage the shader runs in.</summary>
    public ShaderStage Stage { get; protected init; }

    /// <summary>Entry point name.</summary>
    public string EntryPoint { get; protected init; } = "";
}

/// <summary>The resource interface shared by pipelines and bind groups.</summary>
public abstract class GpuPipelineLayout : GpuObject
{
    /// <summary>Number of bind group sets (highest set index + 1).</summary>
    public int SetCount { get; protected init; }
}

/// <summary>A set of resources bound together to a pipeline.</summary>
public abstract class GpuBindGroup : GpuObject
{
}

/// <summary>A compiled graphics pipeline.</summary>
public abstract class GpuGraphicsPipeline : GpuObject
{
}

/// <summary>A compiled compute pipeline.</summary>
public abstract class GpuComputePipeline : GpuObject
{
}

/// <summary>
/// Presents rendered images to a window. Per frame: <see cref="TryAcquire"/>, record and submit commands that render into the
/// returned texture, then <see cref="Present"/>.
/// </summary>
public abstract class GpuSwapchain : GpuObject
{
    /// <summary>Current width in pixels.</summary>
    public abstract uint Width { get; }

    /// <summary>Current height in pixels.</summary>
    public abstract uint Height { get; }

    /// <summary>Format of the backbuffers.</summary>
    public abstract GpuFormat Format { get; }

    /// <summary>Number of backbuffers.</summary>
    public abstract int ImageCount { get; }

    /// <summary>Tells the swapchain the window's new size in pixels. It is rebuilt at the next acquire.</summary>
    public abstract void Resize(uint width, uint height);

    /// <summary>
    /// Gets the next backbuffer to render into. Rebuilds the swapchain first when the window changed. The texture is valid until
    /// <see cref="Present"/>, and the next command submission must render into it.
    /// </summary>
    public abstract AcquireStatus TryAcquire(out GpuTexture backbuffer);

    /// <summary>Queues the acquired backbuffer for display. The commands that rendered into it must already be submitted.</summary>
    public abstract void Present();
}

/// <summary>Records a sequence of copy, render and compute work, then is handed to <see cref="GpuDevice.Submit"/>. Encoders are reused after submission; do not touch one afterwards.</summary>
public abstract class GpuCommandEncoder
{
    /// <summary>Begins a render pass into one color attachment, with an optional depth attachment. End it before recording anything else.</summary>
    public GpuRenderPass BeginRenderPass(in ColorAttachment color, DepthAttachment? depth = null)
    {
        ReadOnlySpan<ColorAttachment> one = [color];
        return BeginRenderPass(one, depth);
    }

    /// <summary>Begins a render pass into several color attachments, with an optional depth attachment.</summary>
    public abstract GpuRenderPass BeginRenderPass(ReadOnlySpan<ColorAttachment> colors, DepthAttachment? depth = null);

    /// <summary>Begins a compute pass.</summary>
    public abstract GpuComputePass BeginComputePass();

    /// <summary>Copies bytes between buffers.</summary>
    public abstract void CopyBuffer(GpuBuffer source, ulong sourceOffset, GpuBuffer destination, ulong destinationOffset, ulong size);

    /// <summary>Copies a texture mip level into a buffer with tightly packed rows.</summary>
    public abstract void CopyTextureToBuffer(GpuTexture source, uint mipLevel, uint layer, GpuBuffer destination, ulong destinationOffset);

    /// <summary>Copies tightly packed rows from a buffer into a texture mip level.</summary>
    public abstract void CopyBufferToTexture(GpuBuffer source, ulong sourceOffset, GpuTexture destination, uint mipLevel, uint layer);

    /// <summary>Fills mip levels 1 and up of every layer by repeatedly halving level 0 with linear filtering.</summary>
    public abstract void GenerateMips(GpuTexture texture);
}

/// <summary>Records draws inside a render pass.</summary>
public abstract class GpuRenderPass
{
    /// <summary>Sets the pipeline for the following draws.</summary>
    public abstract void SetPipeline(GpuGraphicsPipeline pipeline);

    /// <summary>Binds a bind group at a set index of the current pipeline layout.</summary>
    public abstract void SetBindGroup(uint setIndex, GpuBindGroup group);

    /// <summary>Binds a vertex buffer to a vertex buffer slot.</summary>
    public abstract void SetVertexBuffer(uint slot, GpuBuffer buffer, ulong offset = 0);

    /// <summary>Binds the index buffer.</summary>
    public abstract void SetIndexBuffer(GpuBuffer buffer, IndexFormat format, ulong offset = 0);

    /// <summary>Sets the viewport in pixels. Clip space has +Y up and depth 0..1 on every backend.</summary>
    public abstract void SetViewport(float x, float y, float width, float height, float minDepth = 0, float maxDepth = 1);

    /// <summary>Sets the scissor rectangle in pixels.</summary>
    public abstract void SetScissor(int x, int y, uint width, uint height);

    /// <summary>Draws vertices.</summary>
    public abstract void Draw(uint vertexCount, uint instanceCount = 1, uint firstVertex = 0, uint firstInstance = 0);

    /// <summary>Draws indexed vertices.</summary>
    public abstract void DrawIndexed(uint indexCount, uint instanceCount = 1, uint firstIndex = 0, int baseVertex = 0, uint firstInstance = 0);

    /// <summary>Ends the pass.</summary>
    public abstract void EndPass();
}

/// <summary>Records compute dispatches.</summary>
public abstract class GpuComputePass
{
    /// <summary>Sets the pipeline for the following dispatches.</summary>
    public abstract void SetPipeline(GpuComputePipeline pipeline);

    /// <summary>Binds a bind group at a set index of the current pipeline layout.</summary>
    public abstract void SetBindGroup(uint setIndex, GpuBindGroup group);

    /// <summary>Runs the compute shader over a grid of workgroups.</summary>
    public abstract void Dispatch(uint x, uint y = 1, uint z = 1);

    /// <summary>Ends the pass.</summary>
    public abstract void EndPass();
}
