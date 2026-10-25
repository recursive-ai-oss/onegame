namespace OneGame.Gpu.WebGpu;

internal sealed class WebSwapchain : GpuSwapchain
{
    private readonly WebGpuDevice _device;
    private readonly string _canvas;
    private readonly bool _srgb;
    private readonly int _id;
    private uint _width;
    private uint _height;
    private WebTexture? _current;

    public WebSwapchain(WebGpuDevice device, string canvasId, in SwapchainDesc desc)
    {
        _device = device;
        _canvas = canvasId;
        _srgb = desc.Srgb;
        _id = device.NextId();
        var size = Js.ConfigureCanvas(canvasId, (int)Math.Max(desc.Width, 1), (int)Math.Max(desc.Height, 1)).Split('x');
        _width = uint.Parse(size[0], System.Globalization.CultureInfo.InvariantCulture);
        _height = uint.Parse(size[1], System.Globalization.CultureInfo.InvariantCulture);
    }

    public override uint Width => _width;

    public override uint Height => _height;

    public override GpuFormat Format => _srgb ? GpuFormat.Bgra8UnormSrgb : GpuFormat.Bgra8Unorm;

    public override int ImageCount => 2;

    public override void Resize(uint width, uint height)
    {
        width = Math.Max(width, 1);
        height = Math.Max(height, 1);
        if (width == _width && height == _height)
        {
            return;
        }

        _device.Flush();
        Js.SetCanvasSize(_canvas, (int)width, (int)height);
        _width = width;
        _height = height;
    }

    public override AcquireStatus TryAcquire(out GpuTexture backbuffer)
    {
        // The browser hands out the canvas image for the current frame; ask the script to fetch it.
        _device.Stream.U(Op.SetBackbuffer).U(_id).Str(_canvas).U(_srgb ? 1u : 0u);
        _current ??= new WebTexture(_id, _width, _height, Format);
        if (_current.Width != _width || _current.Height != _height)
        {
            _current = new WebTexture(_id, _width, _height, Format);
        }

        backbuffer = _current;
        return AcquireStatus.Ready;
    }

    // The browser shows the canvas when the current task ends.
    public override void Present() => _device.Flush();

    public override void Dispose()
    {
    }
}
