namespace OneGame.Platform;

/// <summary>Which windowing system a <see cref="PlatformApp"/> talks to.</summary>
public enum PlatformBackendKind : byte
{
    /// <summary>Pick the best one available: Android on a phone, otherwise Wayland, then X11, then headless.</summary>
    Auto,

    /// <summary>Windows are plain in-memory objects with no operating-system window. Events come only from injection.</summary>
    Headless,

    /// <summary>Wayland through libwayland-client.</summary>
    Wayland,

    /// <summary>X11 through Xlib.</summary>
    X11,

    /// <summary>Win32.</summary>
    Win32,

    /// <summary>Android through the NDK: a native activity with touch, lifecycle and a Vulkan surface.</summary>
    Android,

    /// <summary>iOS through UIKit: a scene with touch, lifecycle, a soft keyboard and a Metal layer.</summary>
    Ios,
}

/// <summary>The kind of native window system handles in a <see cref="NativeWindowHandle"/>.</summary>
public enum NativeWindowKind : byte
{
    /// <summary>No native window (headless).</summary>
    None,

    /// <summary>Wayland: <see cref="NativeWindowHandle.Display"/> is the wl_display*, <see cref="NativeWindowHandle.Window"/> the wl_surface*.</summary>
    Wayland,

    /// <summary>X11: <see cref="NativeWindowHandle.Display"/> is the Display*, <see cref="NativeWindowHandle.Window"/> the X Window id.</summary>
    X11,

    /// <summary>Win32: <see cref="NativeWindowHandle.Display"/> is the HINSTANCE, <see cref="NativeWindowHandle.Window"/> the HWND.</summary>
    Win32,

    /// <summary>Android: <see cref="NativeWindowHandle.Display"/> is 0, <see cref="NativeWindowHandle.Window"/> the ANativeWindow*. The pointer is valid until a <see cref="PlatformEventKind.SurfaceLost"/> event has been handled.</summary>
    Android,

    /// <summary>Metal (macOS and iOS): <see cref="NativeWindowHandle.Display"/> is 0, <see cref="NativeWindowHandle.Window"/> the <c>CAMetalLayer*</c> of the window's view. It lives as long as the window.</summary>
    MetalLayer,
}

/// <summary>The raw handles a graphics API needs to create a presentable surface for a window.</summary>
/// <param name="Kind">Which window system the handles belong to.</param>
/// <param name="Display">The display or instance handle; see <see cref="NativeWindowKind"/>.</param>
/// <param name="Window">The window or surface handle; see <see cref="NativeWindowKind"/>.</param>
public readonly record struct NativeWindowHandle(NativeWindowKind Kind, nint Display, nint Window);

/// <summary>Options for <see cref="PlatformApp.Create"/>.</summary>
public sealed record PlatformOptions
{
    /// <summary>The backend to use. <see cref="PlatformBackendKind.Auto"/> falls back to headless when no display is reachable.</summary>
    public PlatformBackendKind Backend { get; init; } = PlatformBackendKind.Auto;

    /// <summary>
    /// The display to connect to: an X display name such as <c>:1</c>, or a Wayland socket name such as <c>wayland-1</c>. When null the
    /// DISPLAY and WAYLAND_DISPLAY environment variables of the process decide.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>Radial dead zone applied to gamepad sticks, as a fraction of full travel.</summary>
    public float StickDeadZone { get; init; } = 0.1f;

    /// <summary>Dead zone applied to gamepad triggers, as a fraction of full travel.</summary>
    public float TriggerDeadZone { get; init; } = 0.03f;

    /// <summary>Whether to look for real gamepad devices. Tests that only want injected pads turn this off.</summary>
    public bool EnableGamepadDevices { get; init; } = true;

    /// <summary>
    /// Whether the first finger on the screen also drives the pointer (move, left button down and up), so games written for a mouse work
    /// with a finger. Touch events are posted either way. Turn it off for a game that handles touch itself.
    /// </summary>
    public bool TouchEmulatesPointer { get; init; } = true;
}

/// <summary>Options for <see cref="PlatformApp.CreateWindow"/>.</summary>
public sealed record WindowOptions
{
    /// <summary>The window title.</summary>
    public string Title { get; init; } = "onegame";

    /// <summary>Initial client width in pixels.</summary>
    public int Width { get; init; } = 1280;

    /// <summary>Initial client height in pixels.</summary>
    public int Height { get; init; } = 720;

    /// <summary>Whether the user can resize the window.</summary>
    public bool Resizable { get; init; } = true;

    /// <summary>Start in fullscreen.</summary>
    public bool Fullscreen { get; init; }

    /// <summary>
    /// Give the window a CPU-drawn surface (see <see cref="Window.FillSolid"/>). Wayland only shows a window once it has content,
    /// so tests and tools with no GPU surface set this. Leave it off when a GPU swapchain presents to the window.
    /// </summary>
    public bool SoftwareSurface { get; init; }
}
