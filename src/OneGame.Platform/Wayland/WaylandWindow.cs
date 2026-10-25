using System.Text;

namespace OneGame.Platform;

/// <summary>A toplevel window on Wayland: a wl_surface given the xdg_toplevel role.</summary>
internal sealed unsafe class WaylandWindow : Window
{
    private const uint Argb8888 = 0;

    private readonly WaylandBackend _b;
    private readonly bool _software;
    private readonly bool _scaleFromFractional;
    private readonly WlObject _xdgSurface;
    private readonly WlObject _toplevel;
    private readonly WlObject? _decoration;
    private readonly WlObject? _fractionalScale;
    private readonly WlObject? _viewport;
    private WlObject? _buffer;
    private WaylandPointerLock? _lock;
    private int _logicalWidth, _logicalHeight;
    private int _pendingWidth, _pendingHeight;
    private bool _pendingFullscreen, _pendingActivated, _hiddenBeforeLock;
    private uint _color = 0xFF1E1E1E;

    public WaylandWindow(WaylandBackend backend, PlatformApp app, int id, WindowOptions o)
        : base(app, id)
    {
        _b = backend;
        _software = o.SoftwareSurface;
        Width = o.Width;
        Height = o.Height;
        _logicalWidth = o.Width;
        _logicalHeight = o.Height;
        Title = o.Title;

        Surface = backend.Compositor.Create(WlCompositor.Request.CreateSurface, WlSurface.Info, backend.Compositor.Version, [0]);
        Surface.Handler = OnSurface;
        _xdgSurface = backend.WmBase.Create(XdgWmBase.Request.GetXdgSurface, XdgSurface.Info, backend.WmBase.Version, [0, WlObject.Arg(Surface)]);
        _xdgSurface.Handler = OnXdgSurface;
        _toplevel = _xdgSurface.Create(XdgSurface.Request.GetToplevel, XdgToplevel.Info, _xdgSurface.Version, [0]);
        _toplevel.Handler = OnToplevel;

        SendString(_toplevel, XdgToplevel.Request.SetTitle, o.Title);
        SendString(_toplevel, XdgToplevel.Request.SetAppId, "onegame");
        if (!o.Resizable)
        {
            _toplevel.Send(XdgToplevel.Request.SetMinSize, [WlObject.Arg(o.Width), WlObject.Arg(o.Height)]);
            _toplevel.Send(XdgToplevel.Request.SetMaxSize, [WlObject.Arg(o.Width), WlObject.Arg(o.Height)]);
        }

        if (backend.DecorationManager is { } decorations)
        {
            _decoration = decorations.Create(
                ZxdgDecorationManagerV1.Request.GetToplevelDecoration, ZxdgToplevelDecorationV1.Info, decorations.Version, [0, WlObject.Arg(_toplevel)]);
            _decoration.Send(ZxdgToplevelDecorationV1.Request.SetMode, [ZxdgToplevelDecorationV1.ModeEnum.ServerSide]);
        }

        if (backend.FractionalScaleManager is { } fractional && backend.Viewporter is { } viewporter)
        {
            _viewport = viewporter.Create(WpViewporter.Request.GetViewport, WpViewport.Info, viewporter.Version, [0, WlObject.Arg(Surface)]);
            _fractionalScale = fractional.Create(
                WpFractionalScaleManagerV1.Request.GetFractionalScale, WpFractionalScaleV1.Info, fractional.Version, [0, WlObject.Arg(Surface)]);
            _fractionalScale.Handler = OnFractionalScale;
            _scaleFromFractional = true;
        }

        if (o.Fullscreen)
        {
            _toplevel.Send(XdgToplevel.Request.SetFullscreen, [0]);
        }
    }

    public WlObject Surface { get; }

    /// <summary>True once the compositor has sent the first configure and it was acknowledged.</summary>
    public bool Configured { get; private set; }

    public override NativeWindowHandle NativeHandle => new(NativeWindowKind.Wayland, _b.DisplayHandle, Surface.Proxy);

    /// <summary>The initial empty commit that asks the compositor for the first configure.</summary>
    public void CommitInitial() => Surface.Send(WlSurface.Request.Commit);

    public float ToPixels(double logical) => (float)(logical * Scale);

    public void OnFocus(bool focused) => ApplyFocus(focused);

    public void OnPointerMove(double surfaceX, double surfaceY) =>
        App.Post(new PlatformEvent
        {
            Kind = PlatformEventKind.PointerMoved,
            WindowId = Id,
            X = ToPixels(surfaceX),
            Y = ToPixels(surfaceY),
        });

    private protected override void ApplyTextInput() => _b.SetTextInput(this);

    public override void SetTitle(string title)
    {
        Title = title;
        SendString(_toplevel, XdgToplevel.Request.SetTitle, title);
    }

    public override void RequestSize(int width, int height)
    {
        // Wayland clients decide their own size by the buffer they attach; the new size becomes real when the next frame is committed.
        _logicalWidth = Math.Max(1, (int)Math.Round(width / Scale));
        _logicalHeight = Math.Max(1, (int)Math.Round(height / Scale));
        ApplySize(Math.Max(1, width), Math.Max(1, height));
        Present();
    }

    public override void SetFullscreen(bool fullscreen)
    {
        if (fullscreen)
        {
            _toplevel.Send(XdgToplevel.Request.SetFullscreen, [0]);
        }
        else
        {
            _toplevel.Send(XdgToplevel.Request.UnsetFullscreen);
        }

        IsFullscreen = fullscreen;
    }

    public override void Minimize()
    {
        _toplevel.Send(XdgToplevel.Request.SetMinimized);
        IsMinimized = true;
    }

    public override void SetCursorHidden(bool hidden)
    {
        CursorHidden = hidden;
        _b.ApplyPointerCursor(hidden || CursorLocked);
    }

    public override void SetCursorLocked(bool locked)
    {
        if (locked == CursorLocked)
        {
            return;
        }

        if (locked)
        {
            _lock = _b.StartLock(Surface, (dx, dy) =>
                App.Post(new PlatformEvent { Kind = PlatformEventKind.PointerRelativeMotion, WindowId = Id, DeltaX = (float)(dx * Scale), DeltaY = (float)(dy * Scale) }));
            if (_lock is null)
            {
                return; // The compositor offers no pointer constraints; the lock is refused.
            }

            CursorLocked = true;
            _hiddenBeforeLock = CursorHidden;
            CursorHidden = true;
        }
        else
        {
            _lock?.Release();
            _lock = null;
            CursorLocked = false;
            CursorHidden = _hiddenBeforeLock;
        }

        _b.ApplyPointerCursor(CursorHidden);
    }

    public override void FillSolid(uint argb)
    {
        _color = argb;
        Present();
    }

    /// <summary>Attaches a buffer of the current size when the window has a CPU surface; otherwise a GPU swapchain provides the content.</summary>
    public void Present()
    {
        if (!_software || _b.Shm is not { } shm || IsClosed || !Configured)
        {
            return;
        }

        if (_viewport is not null)
        {
            _viewport.Send(WpViewport.Request.SetDestination, [WlObject.Arg(_logicalWidth), WlObject.Arg(_logicalHeight)]);
        }
        else
        {
            Surface.Send(WlSurface.Request.SetBufferScale, [WlObject.Arg(Math.Max(1, (int)Math.Round(Scale)))]);
        }

        int w = Width, h = Height, stride = Width * 4;
        var size = checked(stride * h);
        int fd;
        fixed (byte* n = "onegame-shm\0"u8)
        {
            fd = Libc.MemfdCreate(n, 1 /* MFD_CLOEXEC */);
        }

        if (fd < 0 || Libc.Ftruncate(fd, size) < 0)
        {
            throw new PlatformException("Could not allocate shared memory for the window surface.");
        }

        var map = Libc.Mmap(null, (nuint)size, Libc.PROT_READ | Libc.PROT_WRITE, Libc.MAP_SHARED, fd, 0);
        if (map == (void*)-1)
        {
            _ = Libc.Close(fd);
            throw new PlatformException("Could not map shared memory for the window surface.");
        }

        new Span<uint>(map, size / 4).Fill(_color);
        _ = Libc.Munmap(map, (nuint)size);

        var pool = shm.Create(WlShm.Request.CreatePool, WlShmPool.Info, shm.Version, [0, (ulong)fd, (ulong)size]);
        var buffer = pool.Create(
            WlShmPool.Request.CreateBuffer, WlBuffer.Info, pool.Version, [0, 0, WlObject.Arg(w), WlObject.Arg(h), WlObject.Arg(stride), Argb8888]);
        pool.Destroy(WlShmPool.Request.Destroy);
        _ = Libc.Close(fd);

        Surface.Send(WlSurface.Request.Attach, [WlObject.Arg(buffer), 0, 0]);
        Surface.Send(
            Surface.Version >= 4 ? WlSurface.Request.DamageBuffer : WlSurface.Request.Damage,
            [0, 0, WlObject.Arg(w), WlObject.Arg(h)]);
        Surface.Send(WlSurface.Request.Commit);
        _buffer?.Destroy(WlBuffer.Request.Destroy);
        _buffer = buffer;
    }

    private protected override void DestroyNative()
    {
        _lock?.Release();
        _lock = null;
        _decoration?.Destroy(ZxdgToplevelDecorationV1.Request.Destroy);
        _fractionalScale?.Destroy(WpFractionalScaleV1.Request.Destroy);
        _viewport?.Destroy(WpViewport.Request.Destroy);
        _buffer?.Destroy(WlBuffer.Request.Destroy);
        _toplevel.Destroy(XdgToplevel.Request.Destroy);
        _xdgSurface.Destroy(XdgSurface.Request.Destroy);
        Surface.Destroy(WlSurface.Request.Destroy);
        _b.Forget(this);
        _ = WlClient.DisplayFlush(_b.DisplayHandle);
    }

    private static void SendString(WlObject target, int opcode, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text + "\0");
        fixed (byte* p = bytes)
        {
            target.Send(opcode, [(ulong)p]);
        }
    }

    private void OnSurface(WlObject sender, int opcode, WlArgs a)
    {
        // Without fractional scale the compositor's whole-number preference decides the scale.
        if (opcode == WlSurface.Event.PreferredBufferScale && !_scaleFromFractional)
        {
            ApplyScaleAndResize(a.Int(0));
        }
    }

    private void OnFractionalScale(WlObject sender, int opcode, WlArgs a)
    {
        if (opcode == WpFractionalScaleV1.Event.PreferredScale)
        {
            // The scale arrives in 120ths.
            ApplyScaleAndResize(a.Uint(0) / 120f);
        }
    }

    private void ApplyScaleAndResize(float scale)
    {
        if (scale <= 0f || scale == Scale)
        {
            return;
        }

        ApplyScale(scale);
        ApplySize(Math.Max(1, (int)Math.Round(_logicalWidth * scale)), Math.Max(1, (int)Math.Round(_logicalHeight * scale)));
        Present();
    }

    private void OnXdgSurface(WlObject sender, int opcode, WlArgs a)
    {
        if (opcode != XdgSurface.Event.Configure)
        {
            return;
        }

        sender.Send(XdgSurface.Request.AckConfigure, [a.Uint(0)]);
        if (_pendingWidth > 0 && _pendingHeight > 0)
        {
            _logicalWidth = _pendingWidth;
            _logicalHeight = _pendingHeight;
            ApplySize(Math.Max(1, (int)Math.Round(_logicalWidth * Scale)), Math.Max(1, (int)Math.Round(_logicalHeight * Scale)));
        }

        IsFullscreen = _pendingFullscreen;
        Configured = true;
        ApplyFocus(_pendingActivated);
        Present();
    }

    private void OnToplevel(WlObject sender, int opcode, WlArgs a)
    {
        switch (opcode)
        {
            case XdgToplevel.Event.Configure:
                _pendingWidth = a.Int(0);
                _pendingHeight = a.Int(1);
                _pendingFullscreen = false;
                _pendingActivated = false;
                var states = a.Array(2);
                for (var i = 0; i + 4 <= states.Length; i += 4)
                {
                    var state = BitConverter.ToUInt32(states[i..]);
                    _pendingFullscreen |= state == XdgToplevel.StateEnum.Fullscreen;
                    _pendingActivated |= state == XdgToplevel.StateEnum.Activated;
                }

                break;
            case XdgToplevel.Event.Close:
                App.Post(new PlatformEvent { Kind = PlatformEventKind.WindowCloseRequested, WindowId = Id });
                break;
        }
    }
}
