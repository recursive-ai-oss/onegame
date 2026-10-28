using System.Linq;
using System.Text;

namespace OneGame.Platform;

/// <summary>What shape of value an action produces.</summary>
public enum ActionKind : byte
{
    /// <summary>Down or up, with edges: <see cref="InputAction.Pressed"/>, <see cref="InputAction.Held"/>, <see cref="InputAction.Released"/>. <see cref="InputAction.Value"/> is the analog strength 0..1.</summary>
    Button,

    /// <summary>One axis, -1..1 (the mouse wheel reports notches).</summary>
    Axis,

    /// <summary>Two axes. x grows right and y grows down, like the screen and the pad's sticks; a diagonal from keys has length 1.</summary>
    Vector,
}

/// <summary>What to do with other actions that already use a source being assigned.</summary>
public enum ConflictPolicy : byte
{
    /// <summary>Leave them; both actions fire. Conflicts are still reported.</summary>
    Allow,

    /// <summary>Take the source away from the other actions in the same context.</summary>
    Replace,
}

/// <summary>A 2D action value.</summary>
/// <param name="X">Right is positive.</param>
/// <param name="Y">Down is positive.</param>
public readonly record struct ActionVector(float X, float Y)
{
    /// <summary>The vector's length.</summary>
    public float Length => MathF.Sqrt((X * X) + (Y * Y));
}

/// <summary>The outcome of a rebinding capture.</summary>
/// <param name="Action">The action name.</param>
/// <param name="Slot">The binding slot that changed.</param>
/// <param name="Source">The captured source, or none when cancelled.</param>
/// <param name="Cancelled">True when the player cancelled (Escape) and nothing changed.</param>
/// <param name="Conflicts">Other actions in the same context that already used the source (before the policy was applied).</param>
public sealed record RebindResult(string Action, int Slot, InputSource Source, bool Cancelled, IReadOnlyList<string> Conflicts);

/// <summary>A rebinding waiting for the player's next input.</summary>
/// <param name="Action">The action name.</param>
/// <param name="Slot">The slot being replaced (the binding count to add one).</param>
/// <param name="Part">Which source of a composite binding is being replaced.</param>
public sealed record RebindRequest(string Action, int Slot, int Part);

/// <summary>What <see cref="ActionMap.LoadText"/> did.</summary>
/// <param name="Applied">How many action lines were applied.</param>
/// <param name="Problems">One message per rejected line; empty when the whole file was understood.</param>
public sealed record BindingsLoadResult(int Applied, IReadOnlyList<string> Problems);

/// <summary>One declared action and its state for the current frame.</summary>
public sealed class InputAction
{
    internal readonly List<ActionBinding> Current;
    internal readonly ActionBinding[] DefaultBindings;
    internal bool LatchPress, LatchRelease;

    internal InputAction(string name, ActionKind kind, ActionContext context, ActionBinding[] defaults)
    {
        Name = name;
        Title = name;
        Kind = kind;
        ContextInfo = context;
        DefaultBindings = defaults;
        Current = [.. defaults];
        DeadZone = kind == ActionKind.Button ? 0f : 0.15f;
        Threshold = 0.5f;
    }

    /// <summary>The action's name, unique in the map.</summary>
    public string Name { get; }

    /// <summary>The text shown in a rebinding list; the name until <see cref="ActionContext.Tune"/> changes it.</summary>
    public string Title { get; internal set; }

    /// <summary>Button, axis or vector.</summary>
    public ActionKind Kind { get; }

    /// <summary>The name of the context that declares the action.</summary>
    public string Context => ContextInfo.Name;

    internal ActionContext ContextInfo { get; }

    /// <summary>Stick and axis values closer to the centre than this read as zero; the rest is rescaled to start from zero. Default 0.15; buttons ignore it.</summary>
    public float DeadZone { get; internal set; }

    /// <summary>How far an analog source has to go for <see cref="Held"/> (default 0.5).</summary>
    public float Threshold { get; internal set; }

    /// <summary>The current bindings, in slot order.</summary>
    public IReadOnlyList<ActionBinding> Bindings => Current;

    /// <summary>The bindings the game declared.</summary>
    public IReadOnlyList<ActionBinding> Defaults => DefaultBindings;

    /// <summary>True when the bindings are the declared ones.</summary>
    public bool IsDefault => Current.SequenceEqual(DefaultBindings);

    /// <summary>Whether the action's context is active this frame.</summary>
    public bool IsActive { get; internal set; }

    /// <summary>Down this frame: a button is down, or an analog value is past <see cref="Threshold"/>.</summary>
    public bool Held { get; internal set; }

    /// <summary>Went down since the last frame (a press shorter than a frame still counts).</summary>
    public bool Pressed { get; internal set; }

    /// <summary>Went up since the last frame.</summary>
    public bool Released { get; internal set; }

    /// <summary>A button's strength 0..1, an axis's value, or a vector's x.</summary>
    public float Value => X;

    /// <summary>The value, or a vector's x.</summary>
    public float X { get; internal set; }

    /// <summary>A vector's y (down is positive); 0 for other kinds.</summary>
    public float Y { get; internal set; }

    /// <summary>The vector value.</summary>
    public ActionVector Vector => new(X, Y);

    internal bool PrevHeld { get; set; }
}

/// <summary>Declares the actions of one context. Get one from <see cref="ActionMap.Context"/>.</summary>
public sealed class ActionContext
{
    private readonly ActionMap _map;
    private readonly List<string> _screens = [];

    internal ActionContext(ActionMap map, string name)
    {
        _map = map;
        Name = name;
    }

    /// <summary>The context's name.</summary>
    public string Name { get; }

    /// <summary>The screens that switch this context on (see <see cref="ActionMap.ActivateForScreen"/>); empty means it is on whenever screens are followed.</summary>
    public IReadOnlyList<string> Screens => _screens;

    internal List<InputAction> Actions { get; } = [];

    internal int Index { get; set; }

    internal bool Active { get; set; } = true;

    /// <summary>Declares a button. Several bindings can drive it; any of them presses it.</summary>
    /// <param name="name">Letters, digits and underscores, starting with a letter (Steam Input uses the same names).</param>
    /// <param name="bindings">Single sources: <c>Bind.Key(Key.Space)</c>, <c>Bind.Pad(GamepadButton.South)</c>, <c>Bind.Mouse(MouseButton.Left)</c>…</param>
    /// <returns>This context.</returns>
    public ActionContext Button(string name, params ActionBinding[] bindings) => Add(name, ActionKind.Button, bindings);

    /// <summary>Declares a 1D axis from single sources (a pad axis, the wheel) or pairs (<c>Bind.Pair(Key.A, Key.D)</c>).</summary>
    /// <param name="name">The action's name.</param>
    /// <param name="bindings">The bindings.</param>
    /// <returns>This context.</returns>
    public ActionContext Axis(string name, params ActionBinding[] bindings) => Add(name, ActionKind.Axis, bindings);

    /// <summary>Declares a 2D vector from quads (<see cref="Bind.Wasd"/>) or sticks (<see cref="Bind.LeftStick"/>).</summary>
    /// <param name="name">The action's name.</param>
    /// <param name="bindings">The bindings.</param>
    /// <returns>This context.</returns>
    public ActionContext Vector(string name, params ActionBinding[] bindings) => Add(name, ActionKind.Vector, bindings);

    /// <summary>Makes this context the active one while the named game screens are open.</summary>
    /// <param name="screens">Screen names as <c>DefineScreens</c> registers them.</param>
    /// <returns>This context.</returns>
    public ActionContext OnScreens(params string[] screens)
    {
        foreach (var s in screens)
        {
            ArgumentException.ThrowIfNullOrEmpty(s);
            _screens.Add(s);
        }

        return this;
    }

    /// <summary>Changes an action's tuning or its list title.</summary>
    /// <param name="name">An action declared in this context.</param>
    /// <param name="deadZone">New dead zone 0..0.95, or null to keep.</param>
    /// <param name="threshold">New hold threshold 0.05..1, or null to keep.</param>
    /// <param name="title">The text a rebinding list shows, or null to keep.</param>
    /// <returns>This context.</returns>
    public ActionContext Tune(string name, float? deadZone = null, float? threshold = null, string? title = null)
    {
        var a = Actions.Find(x => x.Name == name) ?? throw new ArgumentException($"'{name}' is not declared in context '{Name}'.", nameof(name));
        if (deadZone is { } dz)
        {
            a.DeadZone = Math.Clamp(dz, 0f, 0.95f);
        }

        if (threshold is { } t)
        {
            a.Threshold = Math.Clamp(t, 0.05f, 1f);
        }

        if (title is not null)
        {
            a.Title = title;
        }

        return this;
    }

    private ActionContext Add(string name, ActionKind kind, ActionBinding[] bindings)
    {
        ActionMap.CheckName(name, "action");
        ArgumentNullException.ThrowIfNull(bindings);
        foreach (var b in bindings)
        {
            ActionMap.CheckShape(name, kind, b);
        }

        _map.AddAction(this, new InputAction(name, kind, this, bindings));
        return this;
    }
}

/// <summary>
/// A game's actions: named buttons, axes and 2D vectors, with default bindings on keyboard, mouse, gamepad and touch, grouped in contexts a screen switches on and off.
/// View code asks <see cref="Pressed"/>, <see cref="Held"/>, <see cref="Released"/>, <see cref="Value"/> and <see cref="Vector"/> and sends commands; actions never touch synced state.
/// A hosted game gets one as <c>GameInstance.Actions</c>, declared in <c>GameDefinition.DefineActions</c>, and it is updated once a frame. Outside the host, call <see cref="Observe"/> for every
/// event and <see cref="Update"/> once per frame.
/// </summary>
/// <example>
/// Declare in <c>GameDefinition.DefineActions</c>, read in view code:
/// <code>
/// public override void DefineActions(ActionMap actions) =&gt;
///     actions.Context("play", c =&gt; c
///         .Vector("move", Bind.Wasd, Bind.Arrows, Bind.LeftStick)
///         .Button("jump", Bind.Key(Key.Space), Bind.Pad(GamepadButton.South)));
///
/// // In OnEachFrame (view code):
/// var move = game.Actions.Vector("move");       // x and y from -1 to 1
/// if (game.Actions.Pressed("jump")) { player.Send.Jump(); }   // true on the frame the button went down
/// </code>
/// </example>
public sealed class ActionMap
{
    private const int MaxBindingsFileChars = 64 * 1024;
    private const int MaxBindingsPerAction = 16;

    private readonly List<ActionContext> _contexts = [];
    private readonly List<InputAction> _actions = [];
    private readonly Dictionary<string, InputAction> _byName = new(StringComparer.Ordinal);
    private readonly List<ActionSource> _sources = [];
    private float _wheelX, _wheelY;
    private RebindState? _rebind;

    /// <summary>Creates an empty map. It has one extra source, <see cref="Harness"/>, for scenarios and live control.</summary>
    public ActionMap() => Harness = CreateSource("harness");

    /// <summary>Raised when a rebinding capture finishes or is cancelled.</summary>
    public event Action<RebindResult>? RebindFinished;

    /// <summary>Raised at the start of <see cref="Update"/>, so a source that polls a device (Steam Input) can set its values for this frame.</summary>
    public event Action? Updating;

    /// <summary>The source the harness drives (the <c>action:</c> scenario step and the <c>actions</c> control operation).</summary>
    public ActionSource Harness { get; }

    /// <summary>Increases whenever a binding changes, so a list or glyph table knows to refresh.</summary>
    public int Version { get; private set; }

    /// <summary>True while key sources are ignored because a text field has the keyboard (the host sets this).</summary>
    public bool KeyboardCaptured { get; set; }

    /// <summary>The declared actions in declaration order.</summary>
    public IReadOnlyList<InputAction> Actions => _actions;

    /// <summary>The contexts in declaration order.</summary>
    public IReadOnlyList<ActionContext> Contexts => _contexts;

    /// <summary>True when some context names screens, so the host switches contexts as screens open.</summary>
    public bool FollowsScreens => _contexts.Exists(c => c.Screens.Count > 0);

    /// <summary>The pending rebinding capture, or null.</summary>
    public RebindRequest? Rebinding => _rebind is { } r ? new RebindRequest(r.Action.Name, r.Slot, r.Part) : null;

    /// <summary>Looks an action up. A typo throws instead of silently reading zero.</summary>
    /// <param name="name">The action's name.</param>
    /// <exception cref="ArgumentException">No such action.</exception>
    public InputAction this[string name] => _byName.TryGetValue(name, out var a) ? a : throw new ArgumentException($"No action named '{name}'. Declared: {string.Join(", ", _byName.Keys)}.", nameof(name));

    /// <summary>Looks an action up without throwing.</summary>
    /// <param name="name">The action's name.</param>
    /// <param name="action">The action.</param>
    /// <returns>True when it exists.</returns>
    public bool TryGet(string name, out InputAction action) => _byName.TryGetValue(name, out action!);

    /// <summary>Declares a context and its actions.</summary>
    /// <param name="name">Letters, digits and underscores, starting with a letter.</param>
    /// <param name="build">Declares the context's actions.</param>
    /// <returns>This map.</returns>
    /// <exception cref="ArgumentException">The name is taken or invalid, or an action is declared twice.</exception>
    public ActionMap Context(string name, Action<ActionContext> build)
    {
        CheckName(name, "context");
        ArgumentNullException.ThrowIfNull(build);
        if (_contexts.Exists(c => c.Name == name))
        {
            throw new ArgumentException($"A context named '{name}' already exists.", nameof(name));
        }

        var c = new ActionContext(this, name) { Index = _contexts.Count };
        _contexts.Add(c);
        build(c);
        Version++;
        return this;
    }

    /// <summary>Whether a context is switched on.</summary>
    /// <param name="context">The context's name.</param>
    public bool IsContextActive(string context) => FindContext(context).Active;

    /// <summary>Switches exactly the named contexts on. Actions of the others read as not held, with no edges.</summary>
    /// <param name="contexts">Context names.</param>
    public void SetActive(params string[] contexts)
    {
        var list = new List<ActionContext>();
        foreach (var n in contexts)
        {
            list.Add(FindContext(n));
        }

        foreach (var c in _contexts)
        {
            c.Active = list.Contains(c);
        }
    }

    /// <summary>Switches one context on.</summary>
    /// <param name="context">The context's name.</param>
    public void Activate(string context) => FindContext(context).Active = true;

    /// <summary>Switches one context off.</summary>
    /// <param name="context">The context's name.</param>
    public void Deactivate(string context) => FindContext(context).Active = false;

    /// <summary>Follows the open screen: contexts that name screens are on only while one of them is open; contexts that name none stay on.</summary>
    /// <param name="screen">The open screen's name.</param>
    public void ActivateForScreen(string? screen)
    {
        foreach (var c in _contexts)
        {
            c.Active = c.Screens.Count == 0 || (screen is not null && c.Screens.Contains(screen));
        }
    }

    /// <summary>True when the button went down this frame.</summary>
    /// <param name="action">The action's name.</param>
    public bool Pressed(string action) => this[action].Pressed;

    /// <summary>True when the button went up this frame.</summary>
    /// <param name="action">The action's name.</param>
    public bool Released(string action) => this[action].Released;

    /// <summary>True while the button is down.</summary>
    /// <param name="action">The action's name.</param>
    public bool Held(string action) => this[action].Held;

    /// <summary>A button's strength, an axis's value, or a vector's x.</summary>
    /// <param name="action">The action's name.</param>
    public float Value(string action) => this[action].Value;

    /// <summary>A vector action's value.</summary>
    /// <param name="action">The action's name.</param>
    public ActionVector Vector(string action) => this[action].Vector;

    /// <summary>Makes an extra source other code can drive: Steam Input, a replay, an on-screen stick.</summary>
    /// <param name="name">A label for reports.</param>
    /// <returns>The source.</returns>
    public ActionSource CreateSource(string name)
    {
        var s = new ActionSource(this, name);
        _sources.Add(s);
        return s;
    }

    /// <summary>Takes one platform event in: latches button presses shorter than a frame, sums wheel notches and feeds a pending rebinding capture.</summary>
    /// <param name="e">The event.</param>
    /// <returns>True when a pending rebinding capture swallowed the event, so nothing else should handle it.</returns>
    public bool Observe(in PlatformEvent e)
    {
        if (_rebind is not null && Capture(e))
        {
            return true;
        }

        switch (e.Kind)
        {
            case PlatformEventKind.PointerWheel:
                _wheelX += e.DeltaX;
                _wheelY += e.DeltaY;
                break;
            case PlatformEventKind.KeyDown when !e.IsRepeat:
                Latch(new InputSource(InputSourceKind.Key, (int)e.Key), true);
                break;
            case PlatformEventKind.KeyUp:
                Latch(new InputSource(InputSourceKind.Key, (int)e.Key), false);
                break;
            case PlatformEventKind.PointerButtonDown:
                Latch(new InputSource(InputSourceKind.Mouse, (int)e.Button), true);
                break;
            case PlatformEventKind.PointerButtonUp:
                Latch(new InputSource(InputSourceKind.Mouse, (int)e.Button), false);
                break;
            case PlatformEventKind.GamepadButtonDown:
                Latch(new InputSource(InputSourceKind.PadButton, (int)e.GamepadButton), true);
                break;
            case PlatformEventKind.GamepadButtonUp:
                Latch(new InputSource(InputSourceKind.PadButton, (int)e.GamepadButton), false);
                break;
            default:
                break;
        }

        return false;
    }

    private void Latch(InputSource source, bool down)
    {
        if (source.Kind == InputSourceKind.Key && KeyboardCaptured)
        {
            return;
        }

        foreach (var a in _actions)
        {
            if (a.Kind != ActionKind.Button || !a.ContextInfo.Active)
            {
                continue;
            }

            foreach (var b in a.Current)
            {
                if (b.Shape == BindingShape.One && b.Part(0) == source)
                {
                    if (down)
                    {
                        a.LatchPress = true;
                    }
                    else
                    {
                        a.LatchRelease = true;
                    }
                }
            }
        }
    }

    /// <summary>Computes every action's state for this frame from the devices and the extra sources. Call once per frame, after the frame's events were observed.</summary>
    /// <param name="input">The platform's input state.</param>
    /// <param name="width">The window width in pixels, for touch regions.</param>
    /// <param name="height">The window height in pixels, for touch regions.</param>
    public void Update(InputState input, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(input);
        Updating?.Invoke();
        var ctx = new Eval(input, width, height, _wheelX, _wheelY, KeyboardCaptured);
        _wheelX = _wheelY = 0;
        foreach (var a in _actions)
        {
            a.PrevHeld = a.Held;
            a.IsActive = a.ContextInfo.Active;
            float x = 0, y = 0;
            if (a.IsActive)
            {
                Evaluate(a, in ctx, out x, out y);
                foreach (var s in _sources)
                {
                    if (s.Values.TryGetValue(a, out var v))
                    {
                        Merge(a.Kind, v.X, v.Y, ref x, ref y);
                    }
                }
            }
            else
            {
                a.LatchPress = a.LatchRelease = false;
            }

            a.X = x;
            a.Y = y;
            var mag = a.Kind == ActionKind.Vector ? MathF.Sqrt((x * x) + (y * y)) : MathF.Abs(x);
            a.Held = a.IsActive && mag >= a.Threshold;
            a.Pressed = (a.Held && !a.PrevHeld) || a.LatchPress;
            a.Released = (!a.Held && a.PrevHeld) || a.LatchRelease;
            a.LatchPress = a.LatchRelease = false;
        }

        foreach (var s in _sources)
        {
            s.EndFrame();
        }
    }

    private static void Merge(ActionKind kind, float bx, float by, ref float x, ref float y)
    {
        if (kind == ActionKind.Button)
        {
            bx = Math.Clamp(MathF.Max(bx, MathF.Abs(by)), 0f, 1f);
            x = MathF.Max(x, bx);
        }
        else if ((bx * bx) + (by * by) > (x * x) + (y * y))
        {
            x = bx;
            y = kind == ActionKind.Vector ? by : 0;
        }
    }

    private readonly struct Eval(InputState input, int width, int height, float wheelX, float wheelY, bool keysOff)
    {
        public InputState Input { get; } = input;

        public int Width { get; } = width;

        public int Height { get; } = height;

        public float WheelX { get; } = wheelX;

        public float WheelY { get; } = wheelY;

        public bool KeysOff { get; } = keysOff;
    }

    private static void Evaluate(InputAction a, in Eval e, out float x, out float y)
    {
        x = y = 0;
        var dz = a.Kind == ActionKind.Button ? 0f : a.DeadZone;
        foreach (var b in a.Current)
        {
            float bx = 0, by = 0;
            switch (a.Kind)
            {
                case ActionKind.Button:
                    bx = Strength(b.Part(0), in e, 0f);
                    break;
                case ActionKind.Axis when b.Shape == BindingShape.Pair:
                    bx = Strength(b.Part(1), in e, dz) - Strength(b.Part(0), in e, dz);
                    break;
                case ActionKind.Axis:
                    bx = Signed(b.Part(0), in e, dz);
                    break;
                case ActionKind.Vector when b.Shape == BindingShape.Stick:
                    Stick(in e, b.Part(0).Code == (int)GamepadAxis.RightX, dz, out bx, out by);
                    break;
                case ActionKind.Vector:
                    bx = Strength(b.Part(1), in e, dz) - Strength(b.Part(0), in e, dz);
                    by = Strength(b.Part(3), in e, dz) - Strength(b.Part(2), in e, dz);
                    var len = MathF.Sqrt((bx * bx) + (by * by));
                    if (len > 1f)
                    {
                        bx /= len;
                        by /= len;
                    }

                    break;
                default:
                    break;
            }

            if (a.Kind == ActionKind.Button)
            {
                x = MathF.Max(x, bx);
            }
            else if ((bx * bx) + (by * by) > (x * x) + (y * y))
            {
                x = bx;
                y = by;
            }
        }

        if (a.Kind == ActionKind.Axis && a.Current.Count > 0 && !HasWheel(a))
        {
            x = Math.Clamp(x, -1f, 1f);
        }
    }

    private static bool HasWheel(InputAction a)
    {
        foreach (var b in a.Current)
        {
            if (b.Part(0).Kind == InputSourceKind.Wheel)
            {
                return true;
            }
        }

        return false;
    }

    // 0..1 for "how pressed is this source in its own direction".
    private static float Strength(InputSource s, in Eval e, float dz)
    {
        switch (s.Kind)
        {
            case InputSourceKind.Key:
                return !e.KeysOff && e.Input.IsKeyDown((Key)s.Code) ? 1f : 0f;
            case InputSourceKind.Mouse:
                return e.Input.IsButtonDown((MouseButton)s.Code) ? 1f : 0f;
            case InputSourceKind.PadButton:
                foreach (var pad in e.Input.GamepadStates.Values)
                {
                    if (pad.IsDown((GamepadButton)s.Code))
                    {
                        return 1f;
                    }
                }

                return 0f;
            case InputSourceKind.PadAxis:
                return MathF.Max(0f, Rescale(PadAxisValue(in e, (GamepadAxis)s.Code) * s.Sign, dz));
            case InputSourceKind.Wheel:
                return MathF.Max(0f, (s.Code == 1 ? e.WheelX : e.WheelY) * s.Sign);
            case InputSourceKind.Touch:
                return TouchIn(in e, (TouchRegion)s.Code) ? 1f : 0f;
            default:
                return 0f;
        }
    }

    // The value of a lone source on a 1D axis: pad axes and the wheel keep their sign.
    private static float Signed(InputSource s, in Eval e, float dz) => s.Kind switch
    {
        InputSourceKind.PadAxis => Rescale(PadAxisValue(in e, (GamepadAxis)s.Code) * s.Sign, dz),
        InputSourceKind.Wheel => (s.Code == 1 ? e.WheelX : e.WheelY) * s.Sign,
        _ => Strength(s, in e, dz),
    };

    private static float PadAxisValue(in Eval e, GamepadAxis axis)
    {
        var best = 0f;
        foreach (var pad in e.Input.GamepadStates.Values)
        {
            var v = pad.GetAxis(axis);
            if (MathF.Abs(v) > MathF.Abs(best))
            {
                best = v;
            }
        }

        return best;
    }

    private static float Rescale(float v, float dz)
    {
        var m = MathF.Abs(v);
        return m <= dz ? 0f : MathF.Sign(v) * MathF.Min(1f, (m - dz) / (1f - dz));
    }

    private static void Stick(in Eval e, bool right, float dz, out float x, out float y)
    {
        var ax = right ? GamepadAxis.RightX : GamepadAxis.LeftX;
        var ay = right ? GamepadAxis.RightY : GamepadAxis.LeftY;
        var bestSq = 0f;
        x = y = 0;
        foreach (var pad in e.Input.GamepadStates.Values)
        {
            float px = pad.GetAxis(ax), py = pad.GetAxis(ay);
            var sq = (px * px) + (py * py);
            if (sq > bestSq)
            {
                bestSq = sq;
                x = px;
                y = py;
            }
        }

        var len = MathF.Sqrt(bestSq);
        if (len <= dz)
        {
            x = y = 0;
            return;
        }

        var scaled = MathF.Min(1f, (len - dz) / (1f - dz));
        x = x / len * scaled;
        y = y / len * scaled;
    }

    private static bool TouchIn(in Eval e, TouchRegion region)
    {
        foreach (var t in e.Input.TouchPoints.Values)
        {
            var inside = region switch
            {
                TouchRegion.Left => t.X < e.Width / 2f,
                TouchRegion.Right => t.X >= e.Width / 2f,
                TouchRegion.Top => t.Y < e.Height / 2f,
                TouchRegion.Bottom => t.Y >= e.Height / 2f,
                _ => true,
            };
            if (inside)
            {
                return true;
            }
        }

        return false;
    }

    // ---- declaration helpers ----

    internal static void CheckName(string name, string what)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        var ok = name.Length <= 64 && char.IsAsciiLetter(name[0]);
        foreach (var c in name)
        {
            ok &= char.IsAsciiLetterOrDigit(c) || c == '_';
        }

        if (!ok)
        {
            throw new ArgumentException($"'{name}' is not a valid {what} name: use letters, digits and underscores, starting with a letter (at most 64).", nameof(name));
        }
    }

    internal static bool Accepts(ActionKind kind, BindingShape shape) => kind switch
    {
        ActionKind.Button => shape == BindingShape.One,
        ActionKind.Axis => shape is BindingShape.One or BindingShape.Pair,
        _ => shape is BindingShape.Quad or BindingShape.Stick,
    };

    internal static void CheckShape(string name, ActionKind kind, ActionBinding b)
    {
        if (!Accepts(kind, b.Shape))
        {
            throw new ArgumentException($"Action '{name}' is a {kind.ToString().ToLowerInvariant()}, which takes {AcceptedShapes(kind)}, not a {b.Shape.ToString().ToLowerInvariant()} binding.", nameof(b));
        }
    }

    private static string AcceptedShapes(ActionKind kind) => kind switch
    {
        ActionKind.Button => "single sources",
        ActionKind.Axis => "single sources or pairs",
        _ => "quads (Bind.Wasd, Bind.Keys) or sticks (Bind.LeftStick)",
    };

    internal void AddAction(ActionContext context, InputAction action)
    {
        if (!_byName.TryAdd(action.Name, action))
        {
            throw new ArgumentException($"An action named '{action.Name}' already exists (in context '{_byName[action.Name].Context}'); action names are unique across the map.");
        }

        _actions.Add(action);
        context.Actions.Add(action);
    }

    private ActionContext FindContext(string name) =>
        _contexts.Find(c => c.Name == name) ?? throw new ArgumentException($"No context named '{name}'. Declared: {string.Join(", ", _contexts.Select(c => c.Name))}.", nameof(name));

    // ---- rebinding ----

    /// <summary>Other actions in the same context that use <paramref name="source"/>.</summary>
    /// <param name="action">The action being bound (excluded).</param>
    /// <param name="source">The source.</param>
    /// <returns>The names of the conflicting actions.</returns>
    public IReadOnlyList<string> FindConflicts(string action, InputSource source)
    {
        var self = this[action];
        var list = new List<string>();
        if (source.IsNone)
        {
            return list;
        }

        foreach (var other in self.ContextInfo.Actions)
        {
            if (other != self && other.Current.Exists(b => b.Uses(source)))
            {
                list.Add(other.Name);
            }
        }

        return list;
    }

    /// <summary>Replaces one binding slot.</summary>
    /// <param name="action">The action's name.</param>
    /// <param name="slot">The slot, or the binding count to add one.</param>
    /// <param name="binding">The new binding; its shape must suit the action's kind.</param>
    /// <param name="policy">What to do with other actions using the same sources.</param>
    /// <returns>The conflicting actions found, before the policy was applied.</returns>
    /// <exception cref="ArgumentException">Wrong shape for the kind, or an unknown action.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The slot does not exist.</exception>
    public IReadOnlyList<string> SetBinding(string action, int slot, ActionBinding binding, ConflictPolicy policy = ConflictPolicy.Allow)
    {
        var a = this[action];
        CheckShape(a.Name, a.Kind, binding);
        if ((uint)slot > (uint)a.Current.Count || slot >= MaxBindingsPerAction)
        {
            throw new ArgumentOutOfRangeException(nameof(slot), $"Action '{action}' has {a.Current.Count} bindings; slot must be 0..{a.Current.Count}.");
        }

        var conflicts = new List<string>();
        for (var i = 0; i < binding.PartCount; i++)
        {
            foreach (var n in FindConflicts(action, binding.Part(i)))
            {
                if (!conflicts.Contains(n))
                {
                    conflicts.Add(n);
                }
            }
        }

        if (policy == ConflictPolicy.Replace)
        {
            foreach (var n in conflicts)
            {
                for (var i = 0; i < binding.PartCount; i++)
                {
                    StripSource(this[n], binding.Part(i));
                }
            }
        }

        if (slot == a.Current.Count)
        {
            a.Current.Add(binding);
        }
        else
        {
            a.Current[slot] = binding;
        }

        Version++;
        return conflicts;
    }

    private static void StripSource(InputAction a, InputSource s)
    {
        if (s.IsNone)
        {
            return;
        }

        for (var i = a.Current.Count - 1; i >= 0; i--)
        {
            var b = a.Current[i];
            for (var p = 0; p < b.PartCount && b.Shape != BindingShape.Stick; p++)
            {
                if (b.Part(p) == s)
                {
                    b = b.With(p, default);
                }
            }

            if (b.IsEmpty)
            {
                a.Current.RemoveAt(i);
            }
            else
            {
                a.Current[i] = b;
            }
        }
    }

    /// <summary>Removes one binding slot.</summary>
    /// <param name="action">The action's name.</param>
    /// <param name="slot">The slot.</param>
    public void RemoveBinding(string action, int slot)
    {
        var a = this[action];
        if ((uint)slot >= (uint)a.Current.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(slot));
        }

        a.Current.RemoveAt(slot);
        Version++;
    }

    /// <summary>Puts one action, or every action, back to the declared bindings.</summary>
    /// <param name="action">The action's name, or null for all.</param>
    public void ResetBindings(string? action = null)
    {
        IEnumerable<InputAction> set = action is null ? _actions : new[] { this[action] };
        foreach (var a in set)
        {
            a.Current.Clear();
            a.Current.AddRange(a.DefaultBindings);
        }

        Version++;
    }

    /// <summary>
    /// Waits for the player's next key, mouse button, pad button, pad axis push or wheel turn and puts it into a binding slot. Escape cancels. While waiting, <see cref="Observe"/> swallows input events so the UI does not act on them.
    /// Progress and the result come through <see cref="Rebinding"/> and <see cref="RebindFinished"/>.
    /// </summary>
    /// <param name="action">The action's name.</param>
    /// <param name="slot">The slot to replace, or the binding count to add one.</param>
    /// <param name="part">For a pair or quad binding, which source: 0..1 or 0..3 (left, right, up, down).</param>
    /// <param name="policy">What to do with other actions using the captured source.</param>
    public void BeginRebind(string action, int slot = 0, int part = 0, ConflictPolicy policy = ConflictPolicy.Allow)
    {
        var a = this[action];
        if ((uint)slot > (uint)a.Current.Count || slot >= MaxBindingsPerAction)
        {
            throw new ArgumentOutOfRangeException(nameof(slot));
        }

        var shape = slot < a.Current.Count ? a.Current[slot].Shape : a.Kind == ActionKind.Vector ? BindingShape.Quad : BindingShape.One;
        if ((uint)part >= (uint)(shape == BindingShape.Stick ? 1 : new ActionBinding(shape, default).PartCount))
        {
            throw new ArgumentOutOfRangeException(nameof(part));
        }

        _rebind = new RebindState(a, slot, part, policy);
    }

    /// <summary>Stops a pending capture without changing anything.</summary>
    public void CancelRebind()
    {
        if (_rebind is { } r)
        {
            _rebind = null;
            RebindFinished?.Invoke(new RebindResult(r.Action.Name, r.Slot, default, true, []));
        }
    }

    private bool Capture(in PlatformEvent e)
    {
        var r = _rebind!;
        InputSource got = default;
        switch (e.Kind)
        {
            case PlatformEventKind.KeyDown when !e.IsRepeat:
                if (e.Key == Key.Escape)
                {
                    CancelRebind();
                    return true;
                }

                got = new InputSource(InputSourceKind.Key, (int)e.Key);
                break;
            case PlatformEventKind.PointerButtonDown:
                got = new InputSource(InputSourceKind.Mouse, (int)e.Button);
                break;
            case PlatformEventKind.GamepadButtonDown:
                got = new InputSource(InputSourceKind.PadButton, (int)e.GamepadButton);
                break;
            case PlatformEventKind.GamepadAxisMoved when MathF.Abs(e.Value) >= 0.6f:
                got = new InputSource(InputSourceKind.PadAxis, (int)e.GamepadAxis, e.Value < 0 ? -1 : 1);
                break;
            case PlatformEventKind.PointerWheel when e.DeltaY != 0 || e.DeltaX != 0:
                got = e.DeltaY != 0 ? new InputSource(InputSourceKind.Wheel, 0, e.DeltaY < 0 ? -1 : 1) : new InputSource(InputSourceKind.Wheel, 1, e.DeltaX < 0 ? -1 : 1);
                break;
            case PlatformEventKind.KeyDown or PlatformEventKind.KeyUp or PlatformEventKind.PointerButtonUp or PlatformEventKind.GamepadButtonUp or PlatformEventKind.GamepadAxisMoved or PlatformEventKind.PointerWheel:
                return true;
            default:
                return false;
        }

        var a = r.Action;
        var existing = r.Slot < a.Current.Count ? a.Current[r.Slot] : default;
        ActionBinding binding;
        if (r.Slot < a.Current.Count && existing.Shape == BindingShape.Stick)
        {
            if (got.Kind != InputSourceKind.PadAxis || got.Code > (int)GamepadAxis.RightY)
            {
                return true;
            }

            binding = Bind.Stick(got.Code >= (int)GamepadAxis.RightX);
        }
        else if (r.Slot < a.Current.Count)
        {
            binding = existing.Shape == BindingShape.One ? got : existing.With(r.Part, got);
        }
        else
        {
            binding = a.Kind == ActionKind.Vector ? new ActionBinding(BindingShape.Quad, default).With(r.Part, got) : (ActionBinding)got;
        }

        if (a.Kind is ActionKind.Button or ActionKind.Axis && binding.Shape == BindingShape.One && got.Kind == InputSourceKind.PadAxis && got.Code >= (int)GamepadAxis.LeftTrigger)
        {
            binding = new InputSource(InputSourceKind.PadAxis, got.Code);
        }

        _rebind = null;
        var conflicts = SetBinding(a.Name, r.Slot, binding, r.Policy);
        RebindFinished?.Invoke(new RebindResult(a.Name, r.Slot, got, false, conflicts));
        return true;
    }

    private sealed record RebindState(InputAction Action, int Slot, int Part, ConflictPolicy Policy);

    // ---- saving ----

    /// <summary>The first line of a bindings file.</summary>
    public const string FileHeader = "onegame-bindings 1";

    /// <summary>A per-user bindings file for a game: <c>bindings.txt</c> under the user's local application data folder in a folder named after the game.</summary>
    /// <param name="gameName">The game's name.</param>
    public static string DefaultPath(string gameName)
    {
        ArgumentException.ThrowIfNullOrEmpty(gameName);
        var safe = new string([.. gameName.Select(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' ? c : '_')]);
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), safe, "bindings.txt");
    }

    /// <summary>Writes the bindings that differ from the declared ones as text (one line per changed action). Same state gives the same text.</summary>
    public string ToText()
    {
        var sb = new StringBuilder(FileHeader).Append('\n');
        foreach (var a in _actions)
        {
            if (!a.IsDefault)
            {
                sb.Append(a.Name).Append(" =");
                for (var i = 0; i < a.Current.Count; i++)
                {
                    sb.Append(i == 0 ? " " : " | ").Append(a.Current[i]);
                }

                sb.Append('\n');
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Resets every action to its declared bindings, then applies a bindings text. Lines for unknown actions or with bindings the action cannot take are skipped and reported; nothing throws on bad input.
    /// Text over 64 KiB is rejected whole.
    /// </summary>
    /// <param name="text">The text <see cref="ToText"/> wrote.</param>
    public BindingsLoadResult LoadText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length > MaxBindingsFileChars)
        {
            return new BindingsLoadResult(0, [$"bindings text is {text.Length} characters; the limit is {MaxBindingsFileChars}."]);
        }

        var problems = new List<string>();
        var parsed = new List<(InputAction Action, List<ActionBinding> Bindings)>();
        var lineNo = 0;
        foreach (var raw in text.Split('\n'))
        {
            lineNo++;
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            if (lineNo == 1 && line.StartsWith("onegame-bindings", StringComparison.Ordinal))
            {
                if (line != FileHeader)
                {
                    problems.Add($"line 1: unsupported bindings file version '{line}'.");
                    return new BindingsLoadResult(0, problems);
                }

                continue;
            }

            if (line.Length > 1024)
            {
                problems.Add($"line {lineNo}: too long.");
                continue;
            }

            var eq = line.IndexOf('=', StringComparison.Ordinal);
            if (eq <= 0 || !_byName.TryGetValue(line[..eq].Trim(), out var action))
            {
                problems.Add($"line {lineNo}: {(eq <= 0 ? "expected 'action = binding | binding'" : "unknown action '" + line[..eq].Trim() + "'")}.");
                continue;
            }

            var list = new List<ActionBinding>();
            var ok = true;
            var rest = line[(eq + 1)..].Trim();
            if (rest.Length > 0)
            {
                foreach (var piece in rest.Split('|'))
                {
                    if (!ActionBinding.TryParse(piece, out var b) || !Accepts(action.Kind, b.Shape) || list.Count >= MaxBindingsPerAction)
                    {
                        problems.Add($"line {lineNo}: '{piece.Trim()}' is not a binding {action.Name} can take.");
                        ok = false;
                        break;
                    }

                    list.Add(b);
                }
            }

            if (ok)
            {
                parsed.Add((action, list));
            }
        }

        ResetBindings();
        foreach (var (a, list) in parsed)
        {
            a.Current.Clear();
            a.Current.AddRange(list);
        }

        Version++;
        return new BindingsLoadResult(parsed.Count, problems);
    }

    /// <summary>Writes <see cref="ToText"/> to a file, creating the folder, through a temporary file so a crash never leaves half a file.</summary>
    /// <param name="path">The file; <see cref="DefaultPath"/> gives the usual place.</param>
    public void Save(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var tmp = path + ".tmp";
        File.WriteAllText(tmp, ToText(), new UTF8Encoding(false));
        File.Move(tmp, path, true);
    }

    /// <summary>Reads a bindings file. A missing file is not an error: the declared bindings stay.</summary>
    /// <param name="path">The file.</param>
    public BindingsLoadResult Load(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (!File.Exists(path))
        {
            return new BindingsLoadResult(0, []);
        }

        var length = new FileInfo(path).Length;
        if (length > MaxBindingsFileChars * 4L)
        {
            return new BindingsLoadResult(0, [$"{path} is {length} bytes; the limit is {MaxBindingsFileChars * 4}."]);
        }

        return LoadText(File.ReadAllText(path, Encoding.UTF8));
    }
}

/// <summary>
/// Another producer of action values next to the devices: Steam Input, the harness, a replay. Values stay until changed, and the strongest of all sources wins.
/// </summary>
public sealed class ActionSource
{
    private readonly ActionMap _map;
    private readonly List<InputAction> _pulses = [];

    internal ActionSource(ActionMap map, string name)
    {
        _map = map;
        Name = name;
    }

    /// <summary>The source's label.</summary>
    public string Name { get; }

    internal Dictionary<InputAction, (float X, float Y)> Values { get; } = [];

    /// <summary>Holds a button down (or an analog action at full) until released.</summary>
    /// <param name="action">The action's name.</param>
    /// <param name="down">Down or up.</param>
    public void Set(string action, bool down) => Put(_map[action], down ? 1f : 0f, 0f);

    /// <summary>Holds an axis or vector at a value until changed. Vectors: down is positive y.</summary>
    /// <param name="action">The action's name.</param>
    /// <param name="x">The value, or x.</param>
    /// <param name="y">y for a vector.</param>
    public void Set(string action, float x, float y = 0f) => Put(_map[action], x, y);

    /// <summary>Presses a button for exactly one frame: it reads held and pressed next <see cref="ActionMap.Update"/>, then released.</summary>
    /// <param name="action">The action's name.</param>
    public void Pulse(string action)
    {
        var a = _map[action];
        Put(a, 1f, 0f);
        _pulses.Add(a);
    }

    /// <summary>Lets go of one action.</summary>
    /// <param name="action">The action's name.</param>
    public void Release(string action) => Values.Remove(_map[action]);

    /// <summary>Lets go of everything this source holds.</summary>
    public void Clear()
    {
        Values.Clear();
        _pulses.Clear();
    }

    internal void EndFrame()
    {
        foreach (var a in _pulses)
        {
            Values.Remove(a);
        }

        _pulses.Clear();
    }

    private void Put(InputAction a, float x, float y)
    {
        if (x == 0f && y == 0f)
        {
            Values.Remove(a);
        }
        else
        {
            Values[a] = (x, y);
        }
    }
}
