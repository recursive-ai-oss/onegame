namespace OneGame.Platform;

/// <summary>
/// An application window. Sizes are physical pixels (the size of the framebuffer a GPU swapchain should have);
/// <see cref="Scale"/> converts to logical units: logical = pixels / scale.
/// </summary>
public abstract class Window : IDisposable
{
    private protected Window(PlatformApp app, int id)
    {
        App = app;
        Id = id;
    }

    /// <summary>The owning application.</summary>
    public PlatformApp App { get; }

    /// <summary>The id carried by this window's events. Ids start at 1 and are not reused.</summary>
    public int Id { get; }

    /// <summary>Client width in pixels.</summary>
    public int Width { get; private protected set; }

    /// <summary>Client height in pixels.</summary>
    public int Height { get; private protected set; }

    /// <summary>Content scale: 1.0 at 96 dpi, 2.0 on a typical HiDPI screen, fractional where the system offers it.</summary>
    public float Scale { get; private protected set; } = 1f;

    /// <summary>Whether the window has input focus.</summary>
    public bool IsFocused { get; private protected set; }

    /// <summary>Whether the window is fullscreen.</summary>
    public bool IsFullscreen { get; private protected set; }

    /// <summary>Whether the window is minimized.</summary>
    public bool IsMinimized { get; private protected set; }

    /// <summary>Whether the window has been closed.</summary>
    public bool IsClosed { get; private protected set; }

    /// <summary>Whether the cursor is hidden while over the window.</summary>
    public bool CursorHidden { get; private protected set; }

    /// <summary>Whether the cursor is locked: it stays put and <see cref="PlatformEventKind.PointerRelativeMotion"/> events report movement.</summary>
    public bool CursorLocked { get; private protected set; }

    /// <summary>The window title.</summary>
    public string Title { get; private protected set; } = "";

    /// <summary>Handles for creating a graphics surface.</summary>
    public abstract NativeWindowHandle NativeHandle { get; }

    /// <summary>Sets the title.</summary>
    public abstract void SetTitle(string title);

    /// <summary>Asks for a new client size in pixels. The size takes effect when the system confirms it, which is reported as a <see cref="PlatformEventKind.WindowResized"/> event.</summary>
    public abstract void RequestSize(int width, int height);

    /// <summary>Enters or leaves fullscreen.</summary>
    public abstract void SetFullscreen(bool fullscreen);

    /// <summary>Minimizes the window.</summary>
    public abstract void Minimize();

    /// <summary>Shows or hides the cursor over the window.</summary>
    public abstract void SetCursorHidden(bool hidden);

    /// <summary>Locks or unlocks the cursor for relative motion (mouse look). Locking also hides the cursor.</summary>
    public abstract void SetCursorLocked(bool locked);

    /// <summary>
    /// Fills a window made with <see cref="WindowOptions.SoftwareSurface"/> with one colour (0xAARRGGBB). Backends without a CPU surface ignore the call.
    /// </summary>
    public virtual void FillSolid(uint argb)
    {
    }

    /// <summary>Closes the window. Further calls do nothing.</summary>
    public void Close()
    {
        if (IsClosed)
        {
            return;
        }

        IsClosed = true;
        DestroyNative();
        App.OnWindowClosed(this);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }

    private protected abstract void DestroyNative();

    // Backends call these when the system reports a change; they post the matching event.
    private protected void ApplySize(int width, int height)
    {
        if (width == Width && height == Height)
        {
            return;
        }

        Width = width;
        Height = height;
        App.Post(new PlatformEvent { Kind = PlatformEventKind.WindowResized, WindowId = Id, Width = width, Height = height });
    }

    private protected void ApplyScale(float scale)
    {
        if (scale <= 0f || scale == Scale)
        {
            return;
        }

        Scale = scale;
        App.Post(new PlatformEvent { Kind = PlatformEventKind.WindowScaleChanged, WindowId = Id, Scale = scale });
    }

    private protected void ApplyFocus(bool focused)
    {
        if (focused == IsFocused)
        {
            return;
        }

        IsFocused = focused;
        App.Post(new PlatformEvent { Kind = PlatformEventKind.WindowFocusChanged, WindowId = Id, Focused = focused });
    }
}
