namespace OneGame.Gpu.WebGpu;

// The objects of the backend. Each is a handle number that the browser-side script resolves to the real WebGPU object.

internal interface IWebGpuObject
{
    int Id { get; }
}

internal sealed class WebBuffer : GpuBuffer, IWebGpuObject
{
    private readonly WebGpuDevice _device;
    private bool _disposed;

    public WebBuffer(WebGpuDevice device, int id, in BufferDesc desc)
    {
        _device = device;
        Id = id;
        Size = desc.Size;
        Usage = desc.Usage;
        Label = desc.Label;
    }

    public int Id { get; }

    public override void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _device.Destroy(Id, 1);
        }
    }
}

internal sealed class WebTexture : GpuTexture, IWebGpuObject
{
    private readonly WebGpuDevice? _device;
    private bool _disposed;

    public WebTexture(WebGpuDevice device, int id, in TextureDesc desc)
    {
        _device = device;
        Id = id;
        Width = desc.Width;
        Height = desc.Height;
        Format = desc.Format;
        Usage = desc.Usage;
        MipLevels = desc.MipLevels;
        ArrayLayers = desc.Dimension == TextureDimension.D3 ? 1 : desc.ArrayLayers;
        Dimension = desc.Dimension;
        Depth = desc.Dimension == TextureDimension.D3 ? desc.Depth : 1;
        SampleCount = desc.SampleCount;
        Label = desc.Label;
    }

    // A backbuffer: the browser owns the image.
    public WebTexture(int id, uint width, uint height, GpuFormat format)
    {
        Id = id;
        Width = width;
        Height = height;
        Format = format;
        Usage = TextureUsage.RenderTarget | TextureUsage.CopySource;
        MipLevels = 1;
        ArrayLayers = 1;
        SampleCount = 1;
        Label = "backbuffer";
    }

    public int Id { get; }

    public override void Dispose()
    {
        if (!_disposed && _device is not null)
        {
            _disposed = true;
            _device.Destroy(Id, 2);
        }
    }
}

internal sealed class WebSampler : GpuSampler, IWebGpuObject
{
    private readonly WebGpuDevice _device;
    private bool _disposed;

    public WebSampler(WebGpuDevice device, int id)
    {
        _device = device;
        Id = id;
    }

    public int Id { get; }

    public override void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _device.Destroy(Id, 0);
        }
    }
}

internal sealed class WebShader : GpuShader, IWebGpuObject
{
    private readonly WebGpuDevice _device;
    private bool _disposed;

    public WebShader(WebGpuDevice device, int id, ShaderStage stage, string entry)
    {
        _device = device;
        Id = id;
        Stage = stage;
        EntryPoint = entry;
    }

    public int Id { get; }

    // Pipelines keep the compiled module alive on the browser side, so releasing the shader early is fine there.
    public override void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _device.Destroy(Id, 0);
        }
    }
}

internal sealed class WebPipelineLayout : GpuPipelineLayout, IWebGpuObject
{
    private readonly WebGpuDevice _device;
    private bool _disposed;

    public WebPipelineLayout(WebGpuDevice device, int id, int setCount, ResourceBinding[] bindings, uint pushSize)
    {
        PushSize = pushSize;
        _device = device;
        Id = id;
        SetCount = setCount;
        Bindings = bindings;
    }

    public ResourceBinding[] Bindings { get; }

    /// <summary>Bytes of push constants the layout declared.</summary>
    public uint PushSize { get; }

    public int Id { get; }

    public override void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _device.Destroy(Id, 0);
        }
    }
}

internal sealed class WebBindGroup : GpuBindGroup, IWebGpuObject
{
    private readonly WebGpuDevice _device;
    private bool _disposed;

    public WebBindGroup(WebGpuDevice device, int id, uint set, int dynamicCount)
    {
        _device = device;
        Id = id;
        Set = set;
        DynamicCount = dynamicCount;
    }

    public uint Set { get; }

    public int DynamicCount { get; }

    public int Id { get; }

    public override void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _device.Destroy(Id, 0);
        }
    }
}

internal sealed class WebGraphicsPipeline : GpuGraphicsPipeline, IWebGpuObject
{
    private readonly WebGpuDevice _device;
    private bool _disposed;

    public WebGraphicsPipeline(WebGpuDevice device, int id, WebPipelineLayout layout)
    {
        Layout = layout;
        _device = device;
        Id = id;
    }

    public WebPipelineLayout Layout { get; }

    public int Id { get; }

    public override void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _device.Destroy(Id, 0);
        }
    }
}

internal sealed class WebComputePipeline : GpuComputePipeline, IWebGpuObject
{
    private readonly WebGpuDevice _device;
    private bool _disposed;

    public WebComputePipeline(WebGpuDevice device, int id, WebPipelineLayout layout)
    {
        Layout = layout;
        _device = device;
        Id = id;
    }

    public WebPipelineLayout Layout { get; }

    public int Id { get; }

    public override void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _device.Destroy(Id, 0);
        }
    }
}

internal sealed class WebTimestampSet : GpuTimestampSet
{
    public WebTimestampSet(uint count) => Count = count;

    public override void Dispose()
    {
    }
}
