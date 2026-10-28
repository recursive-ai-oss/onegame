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

    /// <summary>Client width in physical pixels: the width a swapchain should have. Divide by <see cref="Scale"/> for logical units.</summary>
    public int Width { get; private protected set; }

    /// <summary>Client height in physical pixels: the height a swapchain should have. Divide by <see cref="Scale"/> for logical units.</summary>
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

    /// <summary>
    /// The distances in pixels from each edge that content must keep clear of: display cut-outs, rounded corners, system bars and the
    /// gesture area. Zero on desktop. Changes arrive as <see cref="PlatformEventKind.SafeAreaChanged"/>.
    /// </summary>
    public EdgeInsets SafeArea { get; private protected set; }

    /// <summary>The window title as last set by <see cref="SetTitle"/>.</summary>
    public string Title { get; private protected set; } = "";

    /// <summary>Handles for creating a graphics surface.</summary>
    public abstract NativeWindowHandle NativeHandle { get; }

    /// <summary>Sets the title shown in the title bar and task switcher, where the system has them.</summary>
    /// <param name="title">The new title.</param>
    public abstract void SetTitle(string title);

    /// <summary>Asks for a new client size in pixels. The size takes effect when the system confirms it, which is reported as a <see cref="PlatformEventKind.WindowResized"/> event.</summary>
    /// <param name="width">Wanted client width in pixels.</param>
    /// <param name="height">Wanted client height in pixels.</param>
    public abstract void RequestSize(int width, int height);

    /// <summary>
    /// Enters or leaves fullscreen. Like <see cref="RequestSize"/> it takes effect when the system agrees; the new size arrives as a
    /// <see cref="PlatformEventKind.WindowResized"/> event.
    /// </summary>
    /// <param name="fullscreen">True for fullscreen, false for a normal window.</param>
    public abstract void SetFullscreen(bool fullscreen);

    /// <summary>Minimizes the window.</summary>
    public abstract void Minimize();

    /// <summary>Shows or hides the cursor over the window.</summary>
    /// <param name="hidden">True to hide the cursor while it is over the window.</param>
    public abstract void SetCursorHidden(bool hidden);

    /// <summary>Locks or unlocks the cursor for relative motion (mouse look). Locking also hides the cursor.</summary>
    /// <param name="locked">True to lock the cursor and receive <see cref="PlatformEventKind.PointerRelativeMotion"/> instead of positions.</param>
    public abstract void SetCursorLocked(bool locked);

    /// <summary>
    /// Fills a window made with <see cref="WindowOptions.SoftwareSurface"/> with one colour (0xAARRGGBB). Backends without a CPU surface ignore the call.
    /// </summary>
    /// <param name="argb">The colour as <c>0xAARRGGBB</c>.</param>
    public virtual void FillSolid(uint argb)
    {
    }

    /// <summary>Whether a text field has focus and the game asked for input-method (IME) support; see <see cref="SetTextInput"/>.</summary>
    public bool TextInputActive { get; private set; }

    /// <summary>
    /// Where the caret is, in window pixels, as last given to <see cref="SetTextInput"/>: left, top, width and height. Default (all
    /// zero) while no text field is active.
    /// </summary>
    public (int X, int Y, int Width, int Height) TextInputArea { get; private set; }

    /// <summary>
    /// Tells the system that a text field has (or no longer has) focus, and where its caret is, so an input method can show its candidate
    /// window beside it and send <see cref="PlatformEventKind.TextComposition"/> events. Call it again when the caret moves. Where the system
    /// has no input-method protocol this only records the state, and plain <see cref="PlatformEventKind.TextInput"/> keeps working.
    /// </summary>
    /// <param name="active">True while a text field has focus.</param>
    /// <param name="x">Left edge of the caret in window pixels.</param>
    /// <param name="y">Top edge of the caret in window pixels.</param>
    /// <param name="width">Caret width in pixels.</param>
    /// <param name="height">Caret height in pixels, which is the line height.</param>
    public void SetTextInput(bool active, int x = 0, int y = 0, int width = 0, int height = 0)
    {
        if (IsClosed)
        {
            return;
        }

        TextInputActive = active;
        TextInputArea = active ? (x, y, width, height) : default;
        ApplyTextInput();
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

    /// <summary>Passes <see cref="TextInputActive"/> and <see cref="TextInputArea"/> to the input method, where the system has one.</summary>
    private protected virtual void ApplyTextInput()
    {
    }

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

    private protected void ApplySafeArea(EdgeInsets insets)
    {
        if (insets == SafeArea)
        {
            return;
        }

        SafeArea = insets;
        App.Post(new PlatformEvent { Kind = PlatformEventKind.SafeAreaChanged, WindowId = Id, Insets = insets });
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
