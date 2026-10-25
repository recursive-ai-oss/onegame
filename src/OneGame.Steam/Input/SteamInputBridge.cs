using OneGame.Platform;
using OneGame.UI;
using OneGame.UI.Kit;

namespace OneGame.Steam.Input;

/// <summary>
/// Joins <see cref="SteamInputService"/> to the engine's input path so game code reads one path whether Steam is on or off. Each Steam controller becomes a virtual gamepad on the platform
/// layer: an action declared with a gamepad button target presses that button, a stick action moves that stick, a trigger action moves that trigger, and all of it arrives as ordinary
/// gamepad events (menu navigation, <c>GameDefinition.OnInput</c>, the harness's input state). The bridge also switches action sets with the open screen and opens Steam's
/// on-screen keyboard while a text input has focus and the player is on a controller.
/// Call <see cref="Update"/> once per frame, for example from <c>GameDefinition.OnFrame</c>.
/// </summary>
public sealed class SteamInputBridge : IDisposable
{
    private readonly SteamInputService _input;
    private readonly PlatformApp _platform;
    private readonly UiTree _ui;
    private readonly Func<float> _uiScale;
    private readonly InputGlyphs? _glyphs;
    private readonly List<(int Controller, int PadId)> _pads = [];
    private readonly List<(string Name, SteamActionKind Kind, GamepadButton? Button, SteamStick? Stick, GamepadAxis? Axis)> _targets;
    private TextInputView? _keyboardFor;
    private string? _lastScreen;
    private bool _screenSeen;
    private bool _disposed;

    /// <summary>Creates the bridge.</summary>
    /// <param name="input">The Steam Input service.</param>
    /// <param name="platform">The platform layer whose input queue receives the virtual gamepad's events.</param>
    /// <param name="ui">The UI tree, watched for a focused text input.</param>
    /// <param name="uiScale">The current UI scale, to turn a text input's box into window pixels; null for 1.</param>
    /// <param name="glyphs">The game's glyph table; when given the service keeps its gamepad glyphs on the player's real buttons and the keyboard also opens when the last used device was the gamepad.</param>
    public SteamInputBridge(SteamInputService input, PlatformApp platform, UiTree ui, Func<float>? uiScale = null, InputGlyphs? glyphs = null)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
        _platform = platform ?? throw new ArgumentNullException(nameof(platform));
        _ui = ui ?? throw new ArgumentNullException(nameof(ui));
        _uiScale = uiScale ?? (() => 1f);
        _glyphs = glyphs;
        _targets = [.. input.Targets()];
        input.ControllerConnected += OnConnected;
        input.ControllerDisconnected += OnDisconnected;
        input.ActionChanged += OnChanged;
        if (glyphs is not null)
        {
            input.BindGlyphs(glyphs);
        }
    }

    /// <summary>The platform gamepad id that stands for a Steam controller, or 0 when it has none.</summary>
    /// <param name="controllerIndex">The controller's index.</param>
    /// <returns>The gamepad id.</returns>
    public int PadFor(int controllerIndex)
    {
        foreach (var (c, p) in _pads)
        {
            if (c == controllerIndex)
            {
                return p;
            }
        }

        return 0;
    }

    /// <summary>Runs one frame: reads Steam Input, switches the action set for the open screen and shows or hides the on-screen keyboard.</summary>
    /// <param name="screen">The open screen's name (<c>GameInstance.CurrentScreen</c>).</param>
    public void Update(string? screen)
    {
        if (_disposed || !_input.IsEnabled)
        {
            return;
        }

        if (!_screenSeen || screen != _lastScreen)
        {
            _screenSeen = true;
            _lastScreen = screen;
            _input.ActivateForScreen(screen);
        }

        _input.Update();
        UpdateKeyboard();
    }

    private void OnConnected(SteamController c)
    {
        var id = _platform.Injector.GamepadConnect($"Steam Input {c.TypeName}");
        _pads.Add((c.Index, id));
    }

    private void OnDisconnected(SteamController c)
    {
        var i = _pads.FindIndex(p => p.Controller == c.Index);
        if (i < 0)
        {
            return;
        }

        _platform.Injector.GamepadDisconnect(_pads[i].PadId);
        _pads.RemoveAt(i);
    }

    private void OnChanged(SteamActionChange e)
    {
        var pad = PadFor(e.Controller.Index);
        if (pad == 0)
        {
            return;
        }

        var inj = _platform.Injector;
        foreach (var t in _targets)
        {
            if (t.Name != e.Action)
            {
                continue;
            }

            switch (e.Kind)
            {
                case SteamActionKind.Digital when t.Button is { } b:
                    inj.GamepadButton(pad, b, e.Down);
                    break;
                case SteamActionKind.Stick when t.Stick is { } s:
                    // Steam reports up as positive; the engine's gamepad axes run down-positive like screen coordinates.
                    inj.GamepadAxis(pad, s == SteamStick.Left ? GamepadAxis.LeftX : GamepadAxis.RightX, Math.Clamp(e.X, -1f, 1f));
                    inj.GamepadAxis(pad, s == SteamStick.Left ? GamepadAxis.LeftY : GamepadAxis.RightY, Math.Clamp(-e.Y, -1f, 1f));
                    break;
                case SteamActionKind.Trigger when t.Axis is { } a:
                    inj.GamepadAxis(pad, a, Math.Clamp(e.X, 0f, 1f));
                    break;
                default:
                    break;
            }
        }
    }

    // The keyboard is for a player without one: a handheld, or someone who last used the pad. A desktop player with a mouse and keyboard never sees it.
    private void UpdateKeyboard()
    {
        var focused = _ui.Focused as TextInputView;
        var wanted = focused is not null && _input.Controllers.Count > 0 && (_input.IsHandheld || _glyphs?.Device == InputDevice.Gamepad);
        if (!wanted)
        {
            if (_keyboardFor is not null)
            {
                _input.DismissKeyboard();
                _keyboardFor = null;
            }

            return;
        }

        if (ReferenceEquals(_keyboardFor, focused))
        {
            return;
        }

        var b = focused!.AbsoluteBounds;
        var s = _uiScale();
        _input.ShowKeyboard((int)(b.X * s), (int)(b.Y * s), (int)(b.Width * s), (int)(b.Height * s));
        _keyboardFor = focused;
    }

    /// <summary>Detaches from the service and removes the virtual gamepads.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _input.ControllerConnected -= OnConnected;
        _input.ControllerDisconnected -= OnDisconnected;
        _input.ActionChanged -= OnChanged;
        foreach (var (_, pad) in _pads)
        {
            _platform.Injector.GamepadDisconnect(pad);
        }

        _pads.Clear();
        if (_keyboardFor is not null)
        {
            _input.DismissKeyboard();
        }
    }
}
