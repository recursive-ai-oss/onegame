using OneGame.Gpu.Metal.Interop;

namespace OneGame.Gpu.Metal;

/// <summary>Presents to a <c>CAMetalLayer</c>. The window system layer creates the layer for a view and hands it over as a <see cref="SurfaceSource"/>.</summary>
internal sealed unsafe class MetalSwapchain : GpuSwapchain
{
    readonly MetalDevice _dev;
    nint _layer;
    nint _drawable;
    uint _width, _height;
    readonly GpuFormat _format;
    readonly TextureUsage _usage;
    bool _disposed;

    // Drawable textures repeat (the layer cycles through a few), so their wrappers are kept and a steady-state frame allocates nothing.
    readonly List<MetalTexture> _wrappers = [];

    public MetalSwapchain(MetalDevice dev, nint layer, in SwapchainDesc desc)
    {
        if (layer == 0)
        {
            throw new ArgumentException("the surface has no CAMetalLayer", nameof(layer));
        }

        _dev = dev;
        _layer = Objc.objc_retain(layer);
        _width = desc.Width;
        _height = desc.Height;
        _format = desc.Srgb ? GpuFormat.Bgra8UnormSrgb : GpuFormat.Bgra8Unorm;
        _usage = TextureUsage.RenderTarget | TextureUsage.CopySource | desc.ExtraUsage;
        Msg.Void(_layer, Sel.setDevice_, dev.Handle);
        Msg.Void(_layer, Sel.setPixelFormat_, MetalMap.Pixel(_format));
        // Readable, so a presented frame can be captured like any other target.
        Msg.Void(_layer, Sel.setFramebufferOnly_, (byte)0);
        Msg.Void(_layer, Sel.setMaximumDrawableCount_, (nuint)3);
        Msg.Void(_layer, Sel.setAllowsNextDrawableTimeout_, (byte)1);
        Msg.Void(_layer, Sel.setDisplaySyncEnabled_, desc.PresentMode == PresentMode.Immediate ? (byte)0 : (byte)1);
        Msg.Void(_layer, Sel.setDrawableSize_, new CGSize { Width = _width, Height = _height });
    }

    public override uint Width => _width;

    public override uint Height => _height;

    public override GpuFormat Format => _format;

    public override int ImageCount => 3;

    public override void Resize(uint width, uint height)
    {
        _width = width;
        _height = height;
        if (width > 0 && height > 0)
        {
            Msg.Void(_layer, Sel.setDrawableSize_, new CGSize { Width = width, Height = height });
        }
    }

    public override AcquireStatus TryAcquire(out GpuTexture backbuffer)
    {
        backbuffer = null!;
        if (_width == 0 || _height == 0)
        {
            return AcquireStatus.Skipped;
        }

        using var pool = new Pool();
        Objc.Release(ref _drawable);
        var drawable = Msg.Obj(_layer, Sel.nextDrawable);
        if (drawable == 0)
        {
            return AcquireStatus.Skipped;
        }

        _drawable = Objc.objc_retain(drawable);
        var texture = Msg.Obj(_drawable, Sel.texture);
        foreach (var w in _wrappers)
        {
            if (w.Handle == texture && w.Width == _width && w.Height == _height)
            {
                backbuffer = w;
                return AcquireStatus.Ready;
            }
        }

        if (_wrappers.Count >= 8)
        {
            _wrappers.RemoveAt(0);
        }

        var wrapper = new MetalTexture(texture, false, new TextureDesc(_width, _height, _format, _usage));
        _wrappers.Add(wrapper);
        backbuffer = wrapper;
        return AcquireStatus.Ready;
    }

    public override void Present()
    {
        if (_drawable == 0)
        {
            throw new InvalidOperationException("Present needs a successful TryAcquire first");
        }

        // The drawable stays referenced until the next acquire so the frame can still be read back.
        _dev.Present(_drawable);
    }

    public override void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Objc.Release(ref _drawable);
        Objc.Release(ref _layer);
    }
}
