using System.Text;
using OneGame.Platform;

namespace OneGame.Steam.Input;

/// <summary>How Steam interprets a two-axis action. Both values are the names the in-game actions file uses.</summary>
public enum SteamStickMode
{
    /// <summary>A joystick: constant deflection from a centre point. Use for movement.</summary>
    JoystickMove,

    /// <summary>Mouse-like deltas that also work on pads, gyro and sticks. Valve recommends it for cameras and cursors.</summary>
    AbsoluteMouse,
}

/// <summary>Which stick of the engine's virtual gamepad a two-axis action drives.</summary>
public enum SteamStick
{
    /// <summary>The left stick: <see cref="GamepadAxis.LeftX"/> and <see cref="GamepadAxis.LeftY"/>.</summary>
    Left,

    /// <summary>The right stick: <see cref="GamepadAxis.RightX"/> and <see cref="GamepadAxis.RightY"/>.</summary>
    Right,
}

/// <summary>One digital action: a button that is down or up.</summary>
/// <param name="Name">The action's name; unique across all sets.</param>
/// <param name="Title">The text shown in Steam's controller configurator.</param>
/// <param name="Button">The gamepad button the action also drives in the engine's input path, or null for none.</param>
public sealed record SteamDigitalAction(string Name, string Title, GamepadButton? Button);

/// <summary>One analog action with two axes.</summary>
/// <param name="Name">The action's name; unique across all sets.</param>
/// <param name="Title">The text shown in Steam's controller configurator.</param>
/// <param name="Mode">How Steam interprets the axes.</param>
/// <param name="Stick">The stick of the virtual gamepad the action also drives, or null for none.</param>
public sealed record SteamStickAction(string Name, string Title, SteamStickMode Mode, SteamStick? Stick);

/// <summary>One analog action with a single axis, such as a trigger.</summary>
/// <param name="Name">The action's name; unique across all sets.</param>
/// <param name="Title">The text shown in Steam's controller configurator.</param>
/// <param name="Axis">The trigger axis of the virtual gamepad the action also drives, or null for none.</param>
public sealed record SteamTriggerAction(string Name, string Title, GamepadAxis? Axis);

/// <summary>An action set: the actions that are live together on one kind of screen (a map, a trade window, a menu).</summary>
public sealed class SteamInputSet
{
    private readonly List<SteamDigitalAction> _digital = [];
    private readonly List<SteamStickAction> _sticks = [];
    private readonly List<SteamTriggerAction> _triggers = [];
    private readonly List<string> _screens = [];

    internal SteamInputSet(string name, string title)
    {
        Name = name;
        Title = title;
    }

    /// <summary>The set's name, as Steam knows it.</summary>
    public string Name { get; }

    /// <summary>The set's title in Steam's configurator.</summary>
    public string Title { get; }

    /// <summary>The digital actions in declaration order.</summary>
    public IReadOnlyList<SteamDigitalAction> Digital => _digital;

    /// <summary>The two-axis actions in declaration order.</summary>
    public IReadOnlyList<SteamStickAction> Sticks => _sticks;

    /// <summary>The one-axis actions in declaration order.</summary>
    public IReadOnlyList<SteamTriggerAction> Triggers => _triggers;

    /// <summary>The game screens this set is active on (see <see cref="SteamInputActions.SetForScreen"/>).</summary>
    public IReadOnlyList<string> Screens => _screens;

    /// <summary>Adds a button action.</summary>
    /// <param name="name">Letters, digits and underscores, starting with a letter.</param>
    /// <param name="title">Text for the configurator.</param>
    /// <param name="button">The gamepad button it also presses in the engine's input path (menu navigation, <c>OnInput</c>), or null.</param>
    /// <returns>This set.</returns>
    public SteamInputSet Button(string name, string title, GamepadButton? button = null)
    {
        _digital.Add(new SteamDigitalAction(SteamInputActions.CheckName(name), SteamInputActions.CheckTitle(title), button));
        return this;
    }

    /// <summary>Adds a two-axis action (a stick, pad or gyro).</summary>
    /// <param name="name">Letters, digits and underscores, starting with a letter.</param>
    /// <param name="title">Text for the configurator.</param>
    /// <param name="mode">How Steam interprets it.</param>
    /// <param name="stick">The virtual gamepad stick it also drives, or null.</param>
    /// <returns>This set.</returns>
    public SteamInputSet Stick(string name, string title, SteamStickMode mode = SteamStickMode.JoystickMove, SteamStick? stick = null)
    {
        _sticks.Add(new SteamStickAction(SteamInputActions.CheckName(name), SteamInputActions.CheckTitle(title), mode, stick));
        return this;
    }

    /// <summary>Adds a one-axis action (an analog trigger). Valve suggests a button action when only a threshold matters.</summary>
    /// <param name="name">Letters, digits and underscores, starting with a letter.</param>
    /// <param name="title">Text for the configurator.</param>
    /// <param name="axis">The trigger axis it also drives (<see cref="GamepadAxis.LeftTrigger"/> or <see cref="GamepadAxis.RightTrigger"/>), or null.</param>
    /// <returns>This set.</returns>
    public SteamInputSet Trigger(string name, string title, GamepadAxis? axis = null)
    {
        if (axis is { } a && a is not (GamepadAxis.LeftTrigger or GamepadAxis.RightTrigger))
        {
            throw new ArgumentException("A trigger action can only drive LeftTrigger or RightTrigger.", nameof(axis));
        }

        _triggers.Add(new SteamTriggerAction(SteamInputActions.CheckName(name), SteamInputActions.CheckTitle(title), axis));
        return this;
    }

    /// <summary>Makes this set the one active while the named game screens are open.</summary>
    /// <param name="screens">Screen names as the game's <c>DefineScreens</c> registers them.</param>
    /// <returns>This set.</returns>
    public SteamInputSet OnScreens(params string[] screens)
    {
        foreach (var s in screens)
        {
            ArgumentException.ThrowIfNullOrEmpty(s);
            _screens.Add(s);
        }

        return this;
    }
}

/// <summary>
/// What a game declares to Steam Input: action sets with their actions. The same declaration drives the code (action state, glyphs, the virtual gamepad) and
/// writes the in-game actions file (<c>game_actions_&lt;appid&gt;.vdf</c>) Steam needs, so the file and the code cannot disagree.
/// A game keeps the generated file checked in and asserts <see cref="Matches"/> in a test.
/// </summary>
public sealed class SteamInputActions
{
    private readonly List<SteamInputSet> _sets = [];

    /// <summary>The sets in declaration order. The first is the default set.</summary>
    public IReadOnlyList<SteamInputSet> Sets => _sets;

    /// <summary>The file's major revision; bump it when action sets change so Steam replaces players' older bindings. Emitted when above zero.</summary>
    public int MajorRevision { get; init; }

    /// <summary>The file's minor revision, for added optional actions or text. Emitted when above zero.</summary>
    public int MinorRevision { get; init; }

    /// <summary>English texts for Steam's configurator are the titles; other languages can be added here (Steam's API language names, e.g. <c>german</c>), token to text.</summary>
    public Dictionary<string, Dictionary<string, string>> Localization { get; } = new(StringComparer.Ordinal);

    /// <summary>Declares an action set.</summary>
    /// <param name="name">Letters, digits and underscores, starting with a letter; unique.</param>
    /// <param name="title">Text for the configurator.</param>
    /// <param name="build">Adds the set's actions.</param>
    /// <returns>This declaration.</returns>
    /// <exception cref="ArgumentException">The name is malformed or already used.</exception>
    public SteamInputActions Set(string name, string title, Action<SteamInputSet> build)
    {
        CheckName(name);
        ArgumentNullException.ThrowIfNull(build);
        if (_sets.Exists(s => s.Name == name))
        {
            throw new ArgumentException($"Action set '{name}' is declared twice.", nameof(name));
        }

        var set = new SteamInputSet(name, CheckTitle(title));
        build(set);
        _sets.Add(set);
        Validate();
        return this;
    }

    /// <summary>Finds a set by name.</summary>
    /// <param name="name">The set's name.</param>
    /// <returns>The set, or null.</returns>
    public SteamInputSet? FindSet(string name) => _sets.Find(s => s.Name == name);

    /// <summary>The set to activate while a game screen is open: the set that lists the screen, else the first (default) set.</summary>
    /// <param name="screen">The screen name, or null.</param>
    /// <returns>The set, or null when nothing is declared.</returns>
    public SteamInputSet? SetForScreen(string? screen)
    {
        if (screen is not null)
        {
            var hit = _sets.Find(s => s.Screens.Contains(screen));
            if (hit is not null)
            {
                return hit;
            }
        }

        return _sets.Count > 0 ? _sets[0] : null;
    }

    /// <summary>Every declared action name, in declaration order, with its kind.</summary>
    /// <returns>Name and kind pairs.</returns>
    public IEnumerable<(string Name, SteamActionKind Kind, string SetName)> AllActions()
    {
        foreach (var s in _sets)
        {
            foreach (var d in s.Digital)
            {
                yield return (d.Name, SteamActionKind.Digital, s.Name);
            }

            foreach (var d in s.Sticks)
            {
                yield return (d.Name, SteamActionKind.Stick, s.Name);
            }

            foreach (var d in s.Triggers)
            {
                yield return (d.Name, SteamActionKind.Trigger, s.Name);
            }
        }
    }

    internal static string CheckName(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        if (name.Length > 64 || !char.IsAsciiLetter(name[0]) || !name.All(c => char.IsAsciiLetterOrDigit(c) || c == '_'))
        {
            throw new ArgumentException($"'{name}' is not a valid Steam Input name: use up to 64 letters, digits and underscores, starting with a letter.", nameof(name));
        }

        return name;
    }

    internal static string CheckTitle(string title)
    {
        ArgumentException.ThrowIfNullOrEmpty(title);
        if (title.Any(char.IsControl))
        {
            throw new ArgumentException("A title cannot contain control characters.", nameof(title));
        }

        return title;
    }

    // Steam looks actions up by name alone, so a name used in two sets would silently resolve to one of them.
    private void Validate()
    {
        var seen = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (name, _, set) in AllActions())
        {
            if (!seen.TryAdd(name, set))
            {
                throw new ArgumentException($"Action '{name}' is declared in sets '{seen[name]}' and '{set}'. Steam finds actions by name alone, so every action name must be unique across sets.");
            }
        }

        var screens = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var s in _sets)
        {
            foreach (var screen in s.Screens)
            {
                if (!screens.TryAdd(screen, s.Name) && screens[screen] != s.Name)
                {
                    throw new ArgumentException($"Screen '{screen}' is assigned to sets '{screens[screen]}' and '{s.Name}'.");
                }
            }
        }
    }

    /// <summary>Writes the in-game actions file Steam reads (Valve's KeyValues text format) for these declarations. The output is deterministic.</summary>
    /// <returns>The file's text, with <c>\n</c> line ends.</returns>
    public string ToVdf()
    {
        var sb = new StringBuilder();
        sb.Append("\"In Game Actions\"\n{\n");
        if (MajorRevision > 0)
        {
            Pair(sb, 1, "major_revision", MajorRevision.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        if (MinorRevision > 0)
        {
            Pair(sb, 1, "minor_revision", MinorRevision.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        Open(sb, 1, "actions");
        foreach (var s in _sets)
        {
            Open(sb, 2, s.Name);
            Pair(sb, 3, "title", "#Set_" + s.Name);
            if (s.Sticks.Count > 0)
            {
                Open(sb, 3, "StickPadGyro");
                foreach (var a in s.Sticks)
                {
                    Open(sb, 4, a.Name);
                    Pair(sb, 5, "title", "#Action_" + a.Name);
                    Pair(sb, 5, "input_mode", a.Mode == SteamStickMode.JoystickMove ? "joystick_move" : "absolute_mouse");
                    Close(sb, 4);
                }

                Close(sb, 3);
            }

            if (s.Triggers.Count > 0)
            {
                Open(sb, 3, "AnalogTrigger");
                foreach (var a in s.Triggers)
                {
                    Pair(sb, 4, a.Name, "#Action_" + a.Name);
                }

                Close(sb, 3);
            }

            if (s.Digital.Count > 0)
            {
                Open(sb, 3, "Button");
                foreach (var a in s.Digital)
                {
                    Pair(sb, 4, a.Name, "#Action_" + a.Name);
                }

                Close(sb, 3);
            }

            Close(sb, 2);
        }

        Close(sb, 1);

        Open(sb, 1, "localization");
        Open(sb, 2, "english");
        foreach (var s in _sets)
        {
            Pair(sb, 3, "Set_" + s.Name, s.Title);
            foreach (var d in s.Sticks)
            {
                Pair(sb, 3, "Action_" + d.Name, d.Title);
            }

            foreach (var d in s.Triggers)
            {
                Pair(sb, 3, "Action_" + d.Name, d.Title);
            }

            foreach (var d in s.Digital)
            {
                Pair(sb, 3, "Action_" + d.Name, d.Title);
            }
        }

        Close(sb, 2);
        foreach (var lang in Localization.Keys.Order(StringComparer.Ordinal))
        {
            Open(sb, 2, lang);
            foreach (var token in Localization[lang].Keys.Order(StringComparer.Ordinal))
            {
                Pair(sb, 3, token, Localization[lang][token]);
            }

            Close(sb, 2);
        }

        Close(sb, 1);
        sb.Append("}\n");
        return sb.ToString();
    }

    /// <summary>Writes <see cref="ToVdf"/> to a file.</summary>
    /// <param name="path">The file, usually <c>game_actions_&lt;appid&gt;.vdf</c>.</param>
    public void WriteVdf(string path) => File.WriteAllText(path, ToVdf(), new UTF8Encoding(false));

    /// <summary>Checks that a checked-in actions file is what these declarations generate (line ends are ignored).</summary>
    /// <param name="existing">The file's text.</param>
    /// <param name="difference">The first differing line, as "line N: expected ..., found ...", or null.</param>
    /// <returns>True when they agree.</returns>
    public bool Matches(string existing, out string? difference)
    {
        ArgumentNullException.ThrowIfNull(existing);
        var want = ToVdf().Split('\n');
        var have = existing.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        for (var i = 0; i < Math.Max(want.Length, have.Length); i++)
        {
            var w = i < want.Length ? want[i] : "<end of file>";
            var h = i < have.Length ? have[i] : "<end of file>";
            if (w != h)
            {
                difference = $"line {i + 1}: expected `{w.Trim()}`, found `{h.Trim()}`";
                return false;
            }
        }

        difference = null;
        return true;
    }

    private static void Open(StringBuilder sb, int depth, string key)
    {
        Indent(sb, depth);
        sb.Append('"').Append(Escape(key)).Append("\"\n");
        Indent(sb, depth);
        sb.Append("{\n");
    }

    private static void Close(StringBuilder sb, int depth)
    {
        Indent(sb, depth);
        sb.Append("}\n");
    }

    private static void Pair(StringBuilder sb, int depth, string key, string value)
    {
        Indent(sb, depth);
        sb.Append('"').Append(Escape(key)).Append("\"\t\"").Append(Escape(value)).Append("\"\n");
    }

    private static void Indent(StringBuilder sb, int depth) => sb.Append('\t', depth);

    private static string Escape(string s) => s.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
}

/// <summary>The kind of a declared action.</summary>
public enum SteamActionKind
{
    /// <summary>A button.</summary>
    Digital,

    /// <summary>A two-axis action.</summary>
    Stick,

    /// <summary>A one-axis action.</summary>
    Trigger,
}
