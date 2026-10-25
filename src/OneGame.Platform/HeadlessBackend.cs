namespace OneGame.Platform;

/// <summary>A backend with no operating-system windows: windows are in-memory records, and events come only from injection.</summary>
internal sealed class HeadlessBackend : PlatformBackend
{
    public HeadlessBackend(PlatformApp app)
        : base(app)
    {
    }

    public override PlatformBackendKind Kind => PlatformBackendKind.Headless;

    public override Window CreateWindow(WindowOptions options, int id) => new HeadlessWindow(App, id, options);

    public override void Pump(int timeoutMilliseconds)
    {
    }

    // A private clipboard, so copy and paste work in headless runs and tests.
    private readonly string?[] _clipboard = new string?[2];

    public override bool ClipboardAvailable => true;

    public override bool ClipboardSupports(ClipboardSelection selection) => true;

    public override string? ClipboardGet(ClipboardSelection selection) => _clipboard[(int)selection];

    public override bool ClipboardSet(ClipboardSelection selection, string text)
    {
        _clipboard[(int)selection] = text;
        return true;
    }

    private sealed class HeadlessWindow : Window
    {
        public HeadlessWindow(PlatformApp app, int id, WindowOptions options)
            : base(app, id)
        {
            Width = options.Width;
            Height = options.Height;
            Title = options.Title;
            IsFullscreen = options.Fullscreen;
            IsFocused = true;
        }

        public override NativeWindowHandle NativeHandle => default;

        public override void SetTitle(string title) => Title = title;

        public override void RequestSize(int width, int height) => ApplySize(width, height);

        public override void SetFullscreen(bool fullscreen) => IsFullscreen = fullscreen;

        public override void Minimize() => IsMinimized = true;

        public override void SetCursorHidden(bool hidden) => CursorHidden = hidden;

        public override void SetCursorLocked(bool locked)
        {
            CursorLocked = locked;
            if (locked)
            {
                CursorHidden = true;
            }
        }

        private protected override void DestroyNative()
        {
        }
    }
}
