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
            IsMaximized = options.Placement?.State == WindowState.Maximized;
            IsFocused = true;
            var at = options.Placement is { HasPosition: true } p ? p : default;
            InitNormalRect(at.X, at.Y, options.Width, options.Height);
        }

        // A headless window pretends to be placeable so scenarios and tests can exercise saved placements; there is no screen to fill,
        // so maximizing changes the state only.
        public override bool CanPlace => true;

        public override void Maximize()
        {
            IsFullscreen = false;
            IsMaximized = true;
        }

        public override void Restore()
        {
            IsFullscreen = false;
            IsMaximized = false;
        }

        public override void MoveTo(int x, int y) => SetPosition(x, y);

        private protected override void PlaceNormal(WindowPlacement placement)
        {
            ApplySize(placement.Width, placement.Height);
            if (placement.HasPosition)
            {
                SetPosition(placement.X, placement.Y);
            }
        }

        public override NativeWindowHandle NativeHandle => default;

        public override void SetTitle(string title) => Title = title;

        public override void RequestSize(int width, int height) => ApplySize(width, height);

        public override void SetFullscreen(bool fullscreen)
        {
            IsFullscreen = fullscreen;
            if (fullscreen)
            {
                IsMaximized = false;
            }
        }

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
