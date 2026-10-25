using OneGame.Steam.Native;
using OneGame.Stores;

namespace OneGame.Steam.Input;

/// <summary>A controller Steam Input reports. <see cref="Index"/> is stable while the controller stays connected and is what the harness and scenarios name it by.</summary>
/// <param name="Index">1-based number, the lowest free one when the controller connected.</param>
/// <param name="Handle">Steam's handle.</param>
/// <param name="InputType">Steam's input type number (3 is an Xbox One pad, 13 a PS5 pad, 14 the Steam Deck).</param>
public sealed record SteamController(int Index, ulong Handle, int InputType)
{
    /// <summary>A readable name for <see cref="InputType"/>.</summary>
    public string TypeName => InputType switch
    {
        1 => "steam-controller",
        2 => "xbox-360",
        3 => "xbox-one",
        4 => "generic-gamepad",
        5 => "ps4",
        6 => "apple-mfi",
        7 => "android",
        8 => "switch-joycon-pair",
        9 => "switch-joycon-single",
        10 => "switch-pro",
        11 => "mobile-touch",
        12 => "ps3",
        13 => "ps5",
        14 => "steam-deck",
        15 => "steamos-handheld",
        16 => "switch-2-pro",
        17 => "steam-controller-2026",
        18 => "steam-frame",
        _ => "unknown",
    };
}

/// <summary>One change of an action's state, as <see cref="SteamInputService.ActionChanged"/> reports it.</summary>
/// <param name="Controller">The controller it came from.</param>
/// <param name="Action">The action's name.</param>
/// <param name="Kind">The action's kind.</param>
/// <param name="Down">For a digital action, whether it is down.</param>
/// <param name="X">For analog actions, the horizontal (or only) axis. Positive is right.</param>
/// <param name="Y">For two-axis actions, the vertical axis. Positive is up, as Steam reports it.</param>
public readonly record struct SteamActionChange(SteamController Controller, string Action, SteamActionKind Kind, bool Down, float X, float Y);

/// <summary>Glyph picture sizes Steam offers.</summary>
public enum SteamGlyphSize
{
    /// <summary>32 px.</summary>
    Small = 0,

    /// <summary>128 px.</summary>
    Medium = 1,

    /// <summary>256 px.</summary>
    Large = 2,
}

/// <summary>What the harness can read about Steam Input.</summary>
/// <param name="Enabled">True when Steam Input is running.</param>
/// <param name="Reason">Why it is not, when it is not.</param>
/// <param name="Fake">True for the harness stand-in that has no Steam behind it.</param>
/// <param name="ActiveSet">The set the game last activated.</param>
/// <param name="Sets">The declared sets.</param>
/// <param name="Controllers">The connected controllers.</param>
/// <param name="Values">The latest value of every declared action, per controller.</param>
/// <param name="Keyboard">The on-screen keyboard's state.</param>
/// <param name="Missing">Declared actions Steam does not know (not in the actions file it loaded).</param>
public sealed record SteamInputReport(
    bool Enabled,
    string? Reason,
    bool Fake,
    string? ActiveSet,
    IReadOnlyList<SteamInputSetInfo> Sets,
    IReadOnlyList<SteamControllerInfo> Controllers,
    IReadOnlyList<SteamActionValue> Values,
    SteamKeyboardInfo Keyboard,
    IReadOnlyList<string> Missing);

/// <summary>A declared set, for the report.</summary>
/// <param name="Name">The set's name.</param>
/// <param name="Screens">The screens it is active on.</param>
/// <param name="Actions">Its action names.</param>
public sealed record SteamInputSetInfo(string Name, IReadOnlyList<string> Screens, IReadOnlyList<string> Actions);

/// <summary>A controller, for the report.</summary>
/// <param name="Index">Its index.</param>
/// <param name="Type">Its type name.</param>
/// <param name="ActiveSet">The set Steam says is active on it.</param>
public sealed record SteamControllerInfo(int Index, string Type, string? ActiveSet);

/// <summary>An action value, for the report.</summary>
/// <param name="Controller">The controller's index.</param>
/// <param name="Action">The action's name.</param>
/// <param name="Kind">digital, stick or trigger.</param>
/// <param name="Down">A digital action's state.</param>
/// <param name="X">The first axis.</param>
/// <param name="Y">The second axis.</param>
public sealed record SteamActionValue(int Controller, string Action, string Kind, bool Down, float X, float Y);

/// <summary>The on-screen keyboard, for the report.</summary>
/// <param name="Visible">True while the game has asked for it and not dismissed it.</param>
/// <param name="X">Left of the text field it avoids, in window pixels.</param>
/// <param name="Y">Top of the text field.</param>
/// <param name="Width">Width of the text field.</param>
/// <param name="Height">Height of the text field.</param>
/// <param name="Shown">How many times it has been shown.</param>
public sealed record SteamKeyboardInfo(bool Visible, int X, int Y, int Width, int Height, int Shown);

/// <summary>How the harness plays the part of a pad when no Steam is behind Steam Input.</summary>
public interface ISteamInputDriver
{
    /// <summary>Plugs in a controller of a Steam input type (3 = Xbox One).</summary>
    /// <param name="inputType">The input type number.</param>
    /// <returns>Its index.</returns>
    int Connect(int inputType);

    /// <summary>Unplugs a controller.</summary>
    /// <param name="index">Its index.</param>
    void Disconnect(int index);

    /// <summary>Sets a digital action's state, as the action sets bound on the pad would.</summary>
    /// <param name="index">The controller.</param>
    /// <param name="action">The action's name.</param>
    /// <param name="down">Down or up.</param>
    /// <returns>False when the action is not in the active set (a real pad would report it inactive).</returns>
    bool SetDigital(int index, string action, bool down);

    /// <summary>Sets an analog action's value.</summary>
    /// <param name="index">The controller.</param>
    /// <param name="action">The action's name.</param>
    /// <param name="x">First axis.</param>
    /// <param name="y">Second axis (up positive).</param>
    /// <returns>False when the action is not in the active set.</returns>
    bool SetAnalog(int index, string action, float x, float y);
}

/// <summary>
/// Steam Input for a game: the declared action sets, the active set per screen, digital and analog action state, controller connect and disconnect, and the glyphs
/// of the buttons the player actually has. Everything is polled from <see cref="Update"/>, on the game thread, like the rest of the store binding.
/// When Steam is not running the service is disabled and does nothing, so game code never branches on it; the platform gamepad path is untouched.
/// </summary>
public sealed class SteamInputService : IDisposable
{
    // Steam's origin numbers for an Xbox One controller start at 114 (A). Everything is translated to that family to pick a kit glyph.
    private const int XboxOneType = 3;

    // How often (in frames) unresolved handles are asked for again: about twice a second at 60 Hz.
    private const int ResolveRetryFrames = 30;

    private readonly ISteamInputNative? _native;
    private readonly SteamInputActions _actions;
    private readonly List<ActionSlot> _slots = [];
    private readonly List<SteamController> _controllers = [];
    private readonly Dictionary<ulong, SteamController> _byHandle = [];
    private readonly Dictionary<string, ulong> _setHandles = new(StringComparer.Ordinal);
    private readonly List<string> _missing = [];
    private readonly List<(OneGame.UI.Kit.InputGlyphs Glyphs, SteamInputService Owner)> _glyphTargets = [];
    private ulong[] _scratchHandles = new ulong[64];
    private string? _activeSet;
    private bool _setDirty;
    private int _frame;
    private bool _disposed;
    private bool _keyboardVisible;
    private int _kx, _ky, _kw, _kh, _kshown;
    private int _glyphCheckAt;
    private string _glyphSignature = "";

    private SteamInputService(ISteamInputNative? native, SteamInputActions actions, string? reason, bool isHandheld)
    {
        _native = native;
        _actions = actions;
        UnavailableReason = reason;
        IsHandheld = isHandheld;
    }

    /// <summary>The service most recently started in this process, which the harness reads. Set by <c>Start</c>; cleared when that service is disposed.</summary>
    public static SteamInputService? Active { get; private set; }

    /// <summary>True when Steam Input is running.</summary>
    public bool IsEnabled => _native is not null && !_disposed;

    /// <summary>Why Steam Input is not running, or null.</summary>
    public string? UnavailableReason { get; }

    /// <summary>True when the game runs on a Steam Deck or another handheld, where the on-screen keyboard is the player's only keyboard.</summary>
    public bool IsHandheld { get; }

    /// <summary>The declarations the service was started with.</summary>
    public SteamInputActions Actions => _actions;

    /// <summary>The connected controllers, in index order.</summary>
    public IReadOnlyList<SteamController> Controllers => _controllers;

    /// <summary>The set the game last activated (all controllers share it), or null before the first activation.</summary>
    public string? ActiveSet => _activeSet;

    /// <summary>The driver for the harness's stand-in, or null when real Steam is behind the service.</summary>
    public ISteamInputDriver? Driver => _native as ISteamInputDriver;

    /// <summary>Raised from <see cref="Update"/> when a controller appears.</summary>
    public event Action<SteamController>? ControllerConnected;

    /// <summary>Raised from <see cref="Update"/> when a controller goes away.</summary>
    public event Action<SteamController>? ControllerDisconnected;

    /// <summary>Raised from <see cref="Update"/> when an action's value changes (a button goes down or up, an axis moves).</summary>
    public event Action<SteamActionChange>? ActionChanged;

    /// <summary>Starts Steam Input on a connected Steam store. Never throws for a missing Steam: the result is a disabled service with <see cref="UnavailableReason"/>.</summary>
    /// <param name="store">The platform from <see cref="SteamPlatform.Start(SteamOptions?)"/>.</param>
    /// <param name="actions">The game's action sets.</param>
    /// <param name="manifestPath">Absolute path to an action manifest file shipped with the game, for games that bundle Steam Input configurations; or null to use the configuration uploaded to Steam.</param>
    /// <returns>The service; always non-null.</returns>
    public static SteamInputService Start(IStorePlatform store, SteamInputActions actions, string? manifestPath = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(actions);
        var handheld = store.Environment.IsHandheld;
        if (store is SteamStorePlatform steam && steam.Native.CreateInput() is { } native)
        {
            return Start(native, actions, manifestPath, handheld);
        }

        if (Environment.GetEnvironmentVariable(FakeSteamInputNative.EnableVariable) == "fake")
        {
            return Start(new FakeSteamInputNative(actions), actions, null, handheld);
        }

        return Disabled(actions, store.IsAvailable ? "This Steam library does not provide Steam Input." : (store.UnavailableReason ?? "Steam is not running."), handheld);
    }

    /// <summary>Starts the harness stand-in, which has no Steam behind it and is driven through <see cref="Driver"/>.</summary>
    /// <param name="actions">The game's action sets.</param>
    /// <returns>The service.</returns>
    public static SteamInputService StartFake(SteamInputActions actions)
    {
        ArgumentNullException.ThrowIfNull(actions);
        return Start(new FakeSteamInputNative(actions), actions, null, false);
    }

    internal static SteamInputService Start(ISteamInputNative native, SteamInputActions actions, string? manifestPath, bool isHandheld = false)
    {
        if (!native.Init(manifestPath))
        {
            return Disabled(actions, manifestPath is null ? "Steam Input did not start." : $"Steam Input did not accept the action manifest '{manifestPath}'.", isHandheld);
        }

        var svc = new SteamInputService(native, actions, null, isHandheld);
        svc.Resolve();
        Active = svc;
        return svc;
    }

    private static SteamInputService Disabled(SteamInputActions actions, string reason, bool isHandheld)
    {
        var svc = new SteamInputService(null, actions, reason, isHandheld);
        Active = svc;
        return svc;
    }

    // Steam answers 0 for every handle until it has adopted the app's configuration (about a second after Init), and again after
    // the player changes configuration, so unresolved names are asked for again instead of being given up on.
    private void Resolve()
    {
        var n = _native!;
        if (_slots.Count == 0)
        {
            foreach (var set in _actions.Sets)
            {
                foreach (var d in set.Digital)
                {
                    _slots.Add(new ActionSlot(d.Name, SteamActionKind.Digital, set.Name, 0, d.Button, null, null));
                }

                foreach (var d in set.Sticks)
                {
                    _slots.Add(new ActionSlot(d.Name, SteamActionKind.Stick, set.Name, 0, null, d.Stick, null));
                }

                foreach (var d in set.Triggers)
                {
                    _slots.Add(new ActionSlot(d.Name, SteamActionKind.Trigger, set.Name, 0, null, null, d.Axis));
                }
            }
        }

        _missing.Clear();
        foreach (var set in _actions.Sets)
        {
            if (_setHandles.GetValueOrDefault(set.Name) == 0)
            {
                _setHandles[set.Name] = n.GetActionSet(set.Name);
            }

            if (_setHandles[set.Name] == 0)
            {
                _missing.Add("set " + set.Name);
            }
        }

        for (var i = 0; i < _slots.Count; i++)
        {
            var s = _slots[i];
            if (s.Handle == 0)
            {
                var h = s.Kind == SteamActionKind.Digital ? n.GetDigitalAction(s.Name) : n.GetAnalogAction(s.Name);
                s = s with { Handle = h };
                _slots[i] = s;
            }

            if (s.Handle == 0)
            {
                _missing.Add(s.Name);
            }
        }

        _glyphSignature = "";
    }

    /// <summary>Makes <paramref name="name"/> the active set on every controller (and on controllers that connect later). Cheap; safe to call every frame.</summary>
    /// <param name="name">A declared set's name.</param>
    /// <returns><see cref="StoreResult.NotFound"/> for an undeclared set, <see cref="StoreResult.Unavailable"/> when Steam Input is not running, else Ok.</returns>
    public StoreResult ActivateSet(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        if (_actions.FindSet(name) is null)
        {
            return StoreResult.NotFound;
        }

        if (!IsEnabled)
        {
            return StoreResult.Unavailable;
        }

        if (_activeSet != name)
        {
            _activeSet = name;
            _setDirty = true;
            // The new set's actions read as inactive until Steam switches; a stale pressed button must not stay pressed.
            ReleaseStaleActions(name);
        }

        return StoreResult.Ok;
    }

    /// <summary>Activates the set declared for a screen (<see cref="SteamInputSet.OnScreens"/>), or the default set.</summary>
    /// <param name="screen">The open screen's name, or null.</param>
    /// <returns>As <see cref="ActivateSet"/>.</returns>
    public StoreResult ActivateForScreen(string? screen) =>
        _actions.SetForScreen(screen) is { } set ? ActivateSet(set.Name) : StoreResult.NotFound;

    /// <summary>Reads Steam's latest data and raises connect, disconnect and change events. Call once per frame after <c>IStorePlatform.Pump</c>.</summary>
    public void Update()
    {
        if (!IsEnabled)
        {
            return;
        }

        var n = _native!;
        _frame++;
        n.RunFrame();
        var before = _controllers.Count;
        SyncControllers(n);
        if (_missing.Count > 0 && (_frame % ResolveRetryFrames == 0 || _controllers.Count > before))
        {
            Resolve();
        }

        if (_activeSet is not null)
        {
            var handle = _setHandles[_activeSet];
            foreach (var c in _controllers)
            {
                if (handle == 0)
                {
                    break;
                }

                // Steam may reset a controller's set (focus changes, reconfiguration); re-assert it when it differs.
                if (_setDirty || n.GetCurrentActionSet(c.Handle) != handle)
                {
                    n.ActivateActionSet(c.Handle, handle);
                }
            }

            _setDirty = false;
        }

        foreach (var c in _controllers)
        {
            foreach (var s in _slots)
            {
                if (s.Handle == 0)
                {
                    continue;
                }

                Read(n, c, s);
            }
        }

        RefreshGlyphs(false);
    }

    private void SyncControllers(ISteamInputNative n)
    {
        var count = n.GetConnectedControllers(_scratchHandles);
        for (var i = 0; i < _controllers.Count; i++)
        {
            var c = _controllers[i];
            var still = false;
            for (var j = 0; j < count; j++)
            {
                if (_scratchHandles[j] == c.Handle)
                {
                    still = true;
                    break;
                }
            }

            if (!still)
            {
                _controllers.RemoveAt(i--);
                _byHandle.Remove(c.Handle);
                _values.RemoveAll(v => v.Controller == c.Index);
                ControllerDisconnected?.Invoke(c);
            }
        }

        for (var j = 0; j < count; j++)
        {
            var h = _scratchHandles[j];
            if (_byHandle.ContainsKey(h))
            {
                continue;
            }

            var index = 1;
            while (_controllers.Exists(c => c.Index == index))
            {
                index++;
            }

            var ctl = new SteamController(index, h, n.GetInputType(h));
            _controllers.Add(ctl);
            _controllers.Sort((a, b) => a.Index.CompareTo(b.Index));
            _byHandle[h] = ctl;
            _setDirty = true;
            _glyphSignature = "";
            ControllerConnected?.Invoke(ctl);
        }
    }

    private readonly List<Value> _values = [];

    private void Read(ISteamInputNative n, SteamController c, ActionSlot s)
    {
        var vi = _values.FindIndex(v => v.Controller == c.Index && v.Name == s.Name);
        var old = vi >= 0 ? _values[vi] : default;
        Value now;
        if (s.Kind == SteamActionKind.Digital)
        {
            var d = n.ReadDigital(c.Handle, s.Handle);
            now = new Value(c.Index, s.Name, d.Active && d.Down, 0, 0, s.Kind);
        }
        else
        {
            var a = n.ReadAnalog(c.Handle, s.Handle);
            now = a.Active ? new Value(c.Index, s.Name, false, a.X, a.Y, s.Kind) : new Value(c.Index, s.Name, false, 0, 0, s.Kind);
        }

        if (vi < 0)
        {
            _values.Add(now);
        }
        else
        {
            _values[vi] = now;
        }

        var pressedNow = now.Down && !(vi >= 0 && old.Down);
        if (pressedNow)
        {
            _pressedFrame[(c.Index, s.Name)] = _frame;
        }

        var changed = vi < 0 ? (now.Down || now.X != 0 || now.Y != 0) : (now.Down != old.Down || now.X != old.X || now.Y != old.Y);
        if (changed)
        {
            ActionChanged?.Invoke(new SteamActionChange(c, s.Name, s.Kind, now.Down, now.X, now.Y));
        }
    }

    private readonly Dictionary<(int, string), int> _pressedFrame = [];

    private void ReleaseStaleActions(string newSet)
    {
        // Actions outside the new set stop being delivered by Steam, so report them released rather than leaving them held.
        for (var i = 0; i < _values.Count; i++)
        {
            var v = _values[i];
            var slot = _slots.Find(s => s.Name == v.Name);
            if (slot is null || slot.SetName == newSet)
            {
                continue;
            }

            if (v.Down || v.X != 0 || v.Y != 0)
            {
                var c = _controllers.Find(x => x.Index == v.Controller);
                _values[i] = v with { Down = false, X = 0, Y = 0 };
                if (c is not null)
                {
                    ActionChanged?.Invoke(new SteamActionChange(c, v.Name, v.Kind, false, 0, 0));
                }
            }
        }
    }

    /// <summary>Whether a digital action is down now on any controller, or on the numbered one.</summary>
    /// <param name="action">The action's name.</param>
    /// <param name="controller">A controller index, or 0 for any.</param>
    /// <returns>True while down.</returns>
    public bool IsDown(string action, int controller = 0)
    {
        foreach (var v in _values)
        {
            if (v.Name == action && v.Down && (controller == 0 || v.Controller == controller))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether a digital action went down during the latest <see cref="Update"/>.</summary>
    /// <param name="action">The action's name.</param>
    /// <param name="controller">A controller index, or 0 for any.</param>
    /// <returns>True on the frame it went down.</returns>
    public bool WasPressed(string action, int controller = 0)
    {
        foreach (var c in _controllers)
        {
            if ((controller == 0 || c.Index == controller) && _pressedFrame.TryGetValue((c.Index, action), out var f) && f == _frame)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The value of an analog action: the controller with the largest deflection when none is named.</summary>
    /// <param name="action">The action's name.</param>
    /// <param name="x">The first axis (positive right).</param>
    /// <param name="y">The second axis (positive up); 0 for a one-axis action.</param>
    /// <param name="controller">A controller index, or 0 for any.</param>
    public void GetAnalog(string action, out float x, out float y, int controller = 0)
    {
        x = 0;
        y = 0;
        var best = -1f;
        foreach (var v in _values)
        {
            if (v.Name != action || (controller != 0 && v.Controller != controller))
            {
                continue;
            }

            var size = (v.X * v.X) + (v.Y * v.Y);
            if (size > best)
            {
                best = size;
                x = v.X;
                y = v.Y;
            }
        }
    }

    /// <summary>Steam's origin numbers for the buttons an action is bound to on a controller, in the action's own set.</summary>
    /// <param name="action">The action's name.</param>
    /// <param name="controller">A controller index, or 0 for the first.</param>
    /// <returns>Origin numbers (empty when unbound, unknown or Steam Input is off).</returns>
    public IReadOnlyList<int> GetOrigins(string action, int controller = 0)
    {
        var slot = _slots.Find(s => s.Name == action);
        var c = controller == 0 ? (_controllers.Count > 0 ? _controllers[0] : null) : _controllers.Find(x => x.Index == controller);
        if (!IsEnabled || slot is null || slot.Handle == 0 || c is null || _setHandles.GetValueOrDefault(slot.SetName) == 0)
        {
            return [];
        }

        Span<int> buf = stackalloc int[32];
        var set = _setHandles[slot.SetName];
        var count = slot.Kind == SteamActionKind.Digital ? _native!.GetDigitalOrigins(c.Handle, set, slot.Handle, buf) : _native!.GetAnalogOrigins(c.Handle, set, slot.Handle, buf);
        var result = new int[count];
        buf[..count].CopyTo(result);
        return result;
    }

    /// <summary>The path of Steam's own PNG for the first button the action is bound to, in the player's controller style, the way <c>GetGlyphForActionOrigin</c> hands one out.</summary>
    /// <param name="action">The action's name.</param>
    /// <param name="size">The picture size.</param>
    /// <param name="controller">A controller index, or 0 for the first.</param>
    /// <returns>A local file path, or null when the action has no button.</returns>
    public string? GetGlyphPath(string action, SteamGlyphSize size = SteamGlyphSize.Medium, int controller = 0)
    {
        var origins = GetOrigins(action, controller);
        if (origins.Count == 0)
        {
            return null;
        }

        var path = _native!.GlyphPath(origins[0], (int)size, 0);
        return path.Length == 0 ? null : path;
    }

    /// <summary>The kit's gamepad glyph name (<c>a</c>, <c>lb</c>, <c>dpad-up</c>...) for the first button an action is bound to, whatever pad the player has: the origin is translated to the Xbox One layout first.</summary>
    /// <param name="action">The action's name.</param>
    /// <param name="controller">A controller index, or 0 for the first.</param>
    /// <returns>The glyph name, or null when unbound or when no kit glyph stands for it.</returns>
    public string? GetGlyphName(string action, int controller = 0)
    {
        var origins = GetOrigins(action, controller);
        if (origins.Count == 0)
        {
            return null;
        }

        var xbox = _native!.TranslateOrigin(XboxOneType, origins[0]);
        return SteamOrigins.KitGlyphName(xbox);
    }

    /// <summary>Keeps an <see cref="OneGame.UI.Kit.InputGlyphs"/> up to date: each declared action's gamepad glyph follows the buttons the player's controller and bindings really use, so <c>UiKit.InputGlyph</c> hints show real buttons. Keyboard and mouse bindings are untouched.</summary>
    /// <param name="glyphs">The game's glyph table.</param>
    public void BindGlyphs(OneGame.UI.Kit.InputGlyphs glyphs)
    {
        ArgumentNullException.ThrowIfNull(glyphs);
        _glyphTargets.Add((glyphs, this));
        RefreshGlyphs(true);
    }

    private void RefreshGlyphs(bool force)
    {
        if (_glyphTargets.Count == 0 || !IsEnabled)
        {
            return;
        }

        // Players rebind at any time; poll about twice a second rather than every frame.
        if (!force && _frame < _glyphCheckAt)
        {
            return;
        }

        _glyphCheckAt = _frame + 30;
        var sig = new System.Text.StringBuilder();
        var names = new List<(string Action, string? Glyph)>();
        foreach (var s in _slots)
        {
            var g = GetGlyphName(s.Name);
            names.Add((s.Name, g));
            sig.Append(s.Name).Append('=').Append(g).Append(';');
        }

        var signature = sig.ToString();
        if (!force && signature == _glyphSignature)
        {
            return;
        }

        _glyphSignature = signature;
        foreach (var (glyphs, _) in _glyphTargets)
        {
            foreach (var (action, glyph) in names)
            {
                glyphs.SetGamepad(action, glyph);
            }
        }
    }

    /// <summary>Opens Steam's on-screen keyboard over the game, positioned so it does not cover a text field. Steam sends the typed keys to the game as ordinary keyboard input, so they arrive through the normal text-input path.</summary>
    /// <param name="x">Left of the field in window pixels.</param>
    /// <param name="y">Top of the field.</param>
    /// <param name="width">Width of the field.</param>
    /// <param name="height">Height of the field.</param>
    /// <returns>Ok when shown; Unavailable without Steam Input; Failed when Steam refused (no Big Picture or Deck keyboard available).</returns>
    public StoreResult ShowKeyboard(int x, int y, int width, int height)
    {
        if (!IsEnabled)
        {
            return StoreResult.Unavailable;
        }

        if (!_native!.ShowKeyboard(x, y, Math.Max(width, 0), Math.Max(height, 0)))
        {
            return StoreResult.Failed;
        }

        _keyboardVisible = true;
        (_kx, _ky, _kw, _kh) = (x, y, width, height);
        _kshown++;
        return StoreResult.Ok;
    }

    /// <summary>Closes the on-screen keyboard.</summary>
    public void DismissKeyboard()
    {
        if (IsEnabled && _keyboardVisible)
        {
            _native!.DismissKeyboard();
        }

        _keyboardVisible = false;
    }

    /// <summary>True while the keyboard has been shown and not dismissed by the game.</summary>
    public bool IsKeyboardVisible => _keyboardVisible;

    /// <summary>Everything the harness shows about Steam Input.</summary>
    /// <returns>A snapshot.</returns>
    public SteamInputReport Describe()
    {
        var sets = new List<SteamInputSetInfo>();
        foreach (var s in _actions.Sets)
        {
            sets.Add(new SteamInputSetInfo(s.Name, s.Screens, [.. s.Digital.Select(a => a.Name), .. s.Sticks.Select(a => a.Name), .. s.Triggers.Select(a => a.Name)]));
        }

        var ctls = new List<SteamControllerInfo>();
        foreach (var c in _controllers)
        {
            string? set = null;
            if (IsEnabled)
            {
                var h = _native!.GetCurrentActionSet(c.Handle);
                foreach (var (name, handle) in _setHandles)
                {
                    if (handle == h && h != 0)
                    {
                        set = name;
                    }
                }
            }

            ctls.Add(new SteamControllerInfo(c.Index, c.TypeName, set));
        }

        var values = new List<SteamActionValue>();
        foreach (var v in _values.OrderBy(v => v.Controller).ThenBy(v => _slots.FindIndex(s => s.Name == v.Name)))
        {
            values.Add(new SteamActionValue(v.Controller, v.Name, v.Kind.ToString().ToLowerInvariant(), v.Down, v.X, v.Y));
        }

        return new SteamInputReport(IsEnabled, UnavailableReason, _native?.IsFake ?? false, _activeSet, sets, ctls, values,
            new SteamKeyboardInfo(_keyboardVisible, _kx, _ky, _kw, _kh, _kshown), _missing);
    }

    /// <summary>The virtual-gamepad targets of the declared actions, for the engine bridge.</summary>
    internal IEnumerable<(string Name, SteamActionKind Kind, OneGame.Platform.GamepadButton? Button, SteamStick? Stick, OneGame.Platform.GamepadAxis? Axis)> Targets()
    {
        foreach (var s in _slots)
        {
            yield return (s.Name, s.Kind, s.Button, s.Stick, s.Axis);
        }
    }

    /// <summary>Stops Steam Input.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_native is not null)
        {
            DismissKeyboard();
            _native.Shutdown();
        }

        _disposed = true;
        if (ReferenceEquals(Active, this))
        {
            Active = null;
        }
    }

    private sealed record ActionSlot(string Name, SteamActionKind Kind, string SetName, ulong Handle, OneGame.Platform.GamepadButton? Button, SteamStick? Stick, OneGame.Platform.GamepadAxis? Axis);

    private readonly record struct Value(int Controller, string Name, bool Down, float X, float Y, SteamActionKind Kind);
}
