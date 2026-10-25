using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;

namespace OneGame.Platform;

/// <summary>The functions of <c>onegame-platform.js</c>; the page registers the script as the module "onegame-platform".</summary>
[SupportedOSPlatform("browser")]
internal static partial class PlatformJs
{
    private const string Module = "onegame-platform";

    [JSImport("init", Module)]
    public static partial string Init(string canvasId);

    [JSImport("size", Module)]
    public static partial string Size();

    [JSImport("drain", Module)]
    public static partial string Drain();

    [JSImport("setTitle", Module)]
    public static partial void SetTitle(string title);

    [JSImport("setCursorHidden", Module)]
    public static partial void SetCursorHidden(bool hidden);

    [JSImport("setCursorLocked", Module)]
    public static partial void SetCursorLocked(bool locked);

    [JSImport("setFullscreen", Module)]
    public static partial void SetFullscreen(bool fullscreen);

    [JSImport("setTextInput", Module)]
    public static partial void SetTextInput(bool active, int x, int y, int width, int height);

    [JSImport("clipboardWrite", Module)]
    public static partial void ClipboardWrite(string text);

    [JSImport("startFrames", Module)]
    public static partial void StartFrames([JSMarshalAs<JSType.Function<JSType.Number>>] Action<double> callback);

    [JSImport("stopFrames", Module)]
    public static partial void StopFrames();
}

/// <summary>
/// The web page as a platform: the window is a canvas, input arrives as DOM events queued by the script and read once per pump,
/// and game controllers come from the browser's Gamepad API.
/// </summary>
[SupportedOSPlatform("browser")]
internal sealed class BrowserBackend : PlatformBackend
{
    private BrowserWindow? _window;
    private string? _clipboard;

    public BrowserBackend(PlatformApp app)
        : base(app)
    {
    }

    public override PlatformBackendKind Kind => PlatformBackendKind.Browser;

    public override Window CreateWindow(WindowOptions options, int id)
    {
        if (_window is not null)
        {
            throw new PlatformException("A web page has one window: the canvas.");
        }

        _window = new BrowserWindow(App, id, options, App.Options.DisplayName ?? "onegame-canvas");
        return _window;
    }

    public override bool ClipboardAvailable => true;

    public override string? ClipboardGet(ClipboardSelection selection) => selection == ClipboardSelection.Clipboard ? _clipboard : null;

    public override bool ClipboardSet(ClipboardSelection selection, string text)
    {
        if (selection != ClipboardSelection.Clipboard)
        {
            return false;
        }

        _clipboard = text;
        PlatformJs.ClipboardWrite(text);
        return true;
    }

    public override void Pump(int timeoutMilliseconds)
    {
        var text = PlatformJs.Drain();
        if (text.Length == 0 || _window is null)
        {
            return;
        }

        using var doc = JsonDocument.Parse(text);
        foreach (var e in doc.RootElement.EnumerateArray())
        {
            Dispatch(_window, e);
        }
    }

    private void Dispatch(BrowserWindow w, JsonElement e)
    {
        var kind = e.GetProperty("t").GetString();
        var mods = e.TryGetProperty("m", out var m) ? (KeyModifiers)m.GetInt32() : KeyModifiers.None;
        switch (kind)
        {
            case "rs":
                w.OnResized(e.GetProperty("w").GetInt32(), e.GetProperty("h").GetInt32(), (float)e.GetProperty("s").GetDouble());
                break;
            case "fc":
                w.OnFocus(e.GetProperty("f").GetBoolean());
                break;
            case "vis":
                w.OnVisibility(e.GetProperty("hidden").GetBoolean());
                break;
            case "close":
                App.Post(new PlatformEvent { Kind = PlatformEventKind.WindowCloseRequested, WindowId = w.Id });
                break;
            case "kd" or "ku":
                var key = BrowserKeys.FromCode(e.GetProperty("code").GetString());
                if (key != Key.Unknown)
                {
                    App.Post(new PlatformEvent
                    {
                        Kind = kind == "kd" ? PlatformEventKind.KeyDown : PlatformEventKind.KeyUp,
                        WindowId = w.Id,
                        Key = key,
                        Modifiers = mods,
                        IsRepeat = e.TryGetProperty("rep", out var rep) && rep.GetBoolean(),
                    });
                }

                break;
            case "ti":
                App.Post(new PlatformEvent { Kind = PlatformEventKind.TextInput, WindowId = w.Id, Text = e.GetProperty("text").GetString() });
                break;
            case "tc":
                App.Post(new PlatformEvent
                {
                    Kind = PlatformEventKind.TextComposition,
                    WindowId = w.Id,
                    Text = e.GetProperty("text").GetString(),
                    CursorStart = e.GetProperty("cs").GetInt32(),
                    CursorEnd = e.GetProperty("ce").GetInt32(),
                });
                break;
            case "pm":
                var x = (float)e.GetProperty("x").GetDouble();
                var y = (float)e.GetProperty("y").GetDouble();
                App.Post(new PlatformEvent { Kind = PlatformEventKind.PointerMoved, WindowId = w.Id, X = x, Y = y, Modifiers = mods });
                if (w.CursorLocked && e.TryGetProperty("lock", out var locked) && locked.GetBoolean())
                {
                    App.Post(new PlatformEvent
                    {
                        Kind = PlatformEventKind.PointerRelativeMotion,
                        WindowId = w.Id,
                        DeltaX = (float)e.GetProperty("dx").GetDouble(),
                        DeltaY = (float)e.GetProperty("dy").GetDouble(),
                    });
                }

                break;
            case "pd" or "pu":
                App.Post(new PlatformEvent
                {
                    Kind = kind == "pd" ? PlatformEventKind.PointerButtonDown : PlatformEventKind.PointerButtonUp,
                    WindowId = w.Id,
                    Button = e.GetProperty("b").GetInt32() switch { 1 => MouseButton.Middle, 2 => MouseButton.Right, 3 => MouseButton.Back, 4 => MouseButton.Forward, _ => MouseButton.Left },
                    X = (float)e.GetProperty("x").GetDouble(),
                    Y = (float)e.GetProperty("y").GetDouble(),
                    Modifiers = mods,
                });
                break;
            case "wh":
                App.Post(new PlatformEvent
                {
                    Kind = PlatformEventKind.PointerWheel,
                    WindowId = w.Id,
                    X = (float)e.GetProperty("x").GetDouble(),
                    Y = (float)e.GetProperty("y").GetDouble(),
                    DeltaX = (float)e.GetProperty("dx").GetDouble(),
                    DeltaY = (float)e.GetProperty("dy").GetDouble(),
                    Modifiers = mods,
                });
                break;
            case "pi":
                App.Post(new PlatformEvent { Kind = PlatformEventKind.PointerInside, WindowId = w.Id, Focused = e.GetProperty("inside").GetBoolean() });
                break;
            case "lock":
                w.OnLockChanged(e.GetProperty("on").GetBoolean());
                break;
            case "cb":
                _clipboard = e.GetProperty("text").GetString();
                break;
            case "gc":
                App.Gamepads.RegisterSynthetic(PadId(e), e.GetProperty("name").GetString() ?? "gamepad");
                break;
            case "gd":
                App.Post(new PlatformEvent { Kind = PlatformEventKind.GamepadDisconnected, GamepadId = PadId(e) });
                break;
            case "gb":
                var down = e.GetProperty("down").GetBoolean();
                App.Post(new PlatformEvent
                {
                    Kind = down ? PlatformEventKind.GamepadButtonDown : PlatformEventKind.GamepadButtonUp,
                    GamepadId = PadId(e),
                    GamepadButton = (GamepadButton)e.GetProperty("b").GetInt32(),
                });
                break;
            case "ga":
                App.Post(new PlatformEvent
                {
                    Kind = PlatformEventKind.GamepadAxisMoved,
                    GamepadId = PadId(e),
                    GamepadAxis = (GamepadAxis)e.GetProperty("axis").GetInt32(),
                    Value = (float)ApplyDeadZone(e.GetProperty("axis").GetInt32(), e.GetProperty("v").GetDouble()),
                });
                break;
        }
    }

    // Browser pads get ids in their own range, apart from device-file ones.
    private static int PadId(JsonElement e) => 2000 + e.GetProperty("id").GetInt32();

    private double ApplyDeadZone(int axis, double v)
    {
        var zone = axis >= (int)GamepadAxis.LeftTrigger ? App.Options.TriggerDeadZone : App.Options.StickDeadZone;
        return Math.Abs(v) < zone ? 0 : v;
    }

    public override void Dispose()
    {
        PlatformJs.StopFrames();
        base.Dispose();
    }
}

[SupportedOSPlatform("browser")]
internal sealed class BrowserWindow : Window
{
    public BrowserWindow(PlatformApp app, int id, WindowOptions options, string canvasId)
        : base(app, id)
    {
        CanvasId = canvasId;
        Title = options.Title;
        using var doc = JsonDocument.Parse(PlatformJs.Init(canvasId));
        var r = doc.RootElement;
        Width = r.GetProperty("w").GetInt32();
        Height = r.GetProperty("h").GetInt32();
        Scale = (float)r.GetProperty("s").GetDouble();
        IsFocused = true;
        PlatformJs.SetTitle(options.Title);
    }

    public string CanvasId { get; }

    public override NativeWindowHandle NativeHandle => new(NativeWindowKind.Browser, 0, 0);

    public override void SetTitle(string title)
    {
        Title = title;
        PlatformJs.SetTitle(title);
    }

    // The page decides its size; a request resizes the drawing surface the game sees (harness runs use it to try other sizes).
    public override void RequestSize(int width, int height) => ApplySize(width, height);

    public override void SetFullscreen(bool fullscreen)
    {
        IsFullscreen = fullscreen;
        PlatformJs.SetFullscreen(fullscreen);
    }

    public override void Minimize()
    {
    }

    public override void SetCursorHidden(bool hidden)
    {
        CursorHidden = hidden;
        PlatformJs.SetCursorHidden(hidden);
    }

    public override void SetCursorLocked(bool locked)
    {
        CursorLocked = locked;
        if (locked)
        {
            CursorHidden = true;
        }

        PlatformJs.SetCursorLocked(locked);
    }

    internal void OnResized(int width, int height, float scale)
    {
        ApplyScale(scale);
        ApplySize(width, height);
    }

    internal void OnFocus(bool focused) => ApplyFocus(focused);

    internal void OnVisibility(bool hidden) => IsMinimized = hidden;

    internal void OnLockChanged(bool locked)
    {
        CursorLocked = locked;
        if (!locked)
        {
            CursorHidden = false;
        }
    }

    private protected override void ApplyTextInput() => PlatformJs.SetTextInput(TextInputActive, TextInputArea.X, TextInputArea.Y, TextInputArea.Width, TextInputArea.Height);

    private protected override void DestroyNative()
    {
    }
}

/// <summary>Translates DOM <c>KeyboardEvent.code</c> values (the physical key) to <see cref="Key"/>.</summary>
internal static class BrowserKeys
{
    private static readonly Dictionary<string, Key> Map = Build();

    public static Key FromCode(string? code) => code is not null && Map.TryGetValue(code, out var k) ? k : Key.Unknown;

    private static Dictionary<string, Key> Build()
    {
        var map = new Dictionary<string, Key>(StringComparer.Ordinal);
        foreach (var key in Enum.GetValues<Key>())
        {
            var name = key.ToString();
            if (key == Key.Unknown)
            {
                continue;
            }

            var code = name switch
            {
                _ when name.Length == 1 => "Key" + name,
                _ when name.StartsWith("Keypad", StringComparison.Ordinal) => "Numpad" + name["Keypad".Length..],
                "LeftShift" => "ShiftLeft",
                "RightShift" => "ShiftRight",
                "LeftControl" => "ControlLeft",
                "RightControl" => "ControlRight",
                "LeftAlt" => "AltLeft",
                "RightAlt" => "AltRight",
                "LeftMeta" => "MetaLeft",
                "RightMeta" => "MetaRight",
                "Menu" => "ContextMenu",
                _ => name,
            };
            map[code] = key;
        }

        map["OSLeft"] = Key.LeftMeta;
        map["OSRight"] = Key.RightMeta;
        return map;
    }
}
