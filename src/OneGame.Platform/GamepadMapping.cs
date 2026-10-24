using System.Globalization;

namespace OneGame.Platform;

/// <summary>
/// One entry of a gamepad mapping database in the community text format: a device GUID, a name, and comma-separated
/// <c>target:source</c> pairs such as <c>a:b0,leftx:a0,dpup:h0.1</c>. Sources are buttons (<c>bN</c>), axes (<c>aN</c>, with an
/// optional <c>+</c>/<c>-</c> half-axis prefix and <c>~</c> inversion suffix) and hats (<c>hN.mask</c>).
/// </summary>
public sealed class GamepadMapping
{
    internal GamepadMapping(string deviceId, string name, string platform, Binding[] bindings)
    {
        DeviceId = deviceId;
        Name = name;
        Platform = platform;
        Bindings = bindings;
    }

    /// <summary>The device GUID as 32 lower-case hex digits.</summary>
    public string DeviceId { get; }

    /// <summary>The device name from the database.</summary>
    public string Name { get; }

    /// <summary>The platform field (for example <c>Linux</c>), or empty when the entry has none.</summary>
    public string Platform { get; }

    internal Binding[] Bindings { get; }

    internal enum SourceKind : byte
    {
        Button,
        Axis,
        Hat,
    }

    internal readonly record struct Binding(
        bool TargetIsAxis,
        GamepadButton Button,
        GamepadAxis Axis,
        int TargetHalf,
        SourceKind Source,
        int Index,
        int HatMask,
        int SourceHalf,
        bool Invert);

    /// <summary>Parses one database line. Returns false for comments, blank lines and lines that are not valid entries.</summary>
    public static bool TryParse(string line, out GamepadMapping? mapping)
    {
        mapping = null;
        var text = line.AsSpan().Trim();
        if (text.IsEmpty || text[0] == '#')
        {
            return false;
        }

        var parts = text.ToString().Split(',');
        if (parts.Length < 3 || parts[0].Length != 32)
        {
            return false;
        }

        foreach (var c in parts[0])
        {
            if (!char.IsAsciiHexDigit(c))
            {
                return false;
            }
        }

        var bindings = new List<Binding>();
        var platform = "";
        for (var i = 2; i < parts.Length; i++)
        {
            var colon = parts[i].IndexOf(':', StringComparison.Ordinal);
            if (colon <= 0)
            {
                continue;
            }

            var key = parts[i][..colon];
            var value = parts[i][(colon + 1)..];
            if (key == "platform")
            {
                platform = value;
                continue;
            }

            if (TryParseBinding(key, value, out var binding))
            {
                bindings.Add(binding);
            }
        }

        mapping = new GamepadMapping(parts[0].ToLowerInvariant(), parts[1], platform, [.. bindings]);
        return true;
    }

    private static bool TryParseBinding(string key, string value, out Binding binding)
    {
        binding = default;
        var targetHalf = 0;
        if (key.Length > 1 && (key[0] == '+' || key[0] == '-'))
        {
            targetHalf = key[0] == '+' ? 1 : -1;
            key = key[1..];
        }

        var isAxis = TryAxis(key, out var axis);
        if (!isAxis && !TryButton(key, out _))
        {
            return false;
        }

        _ = TryButton(key, out var button);

        var sourceHalf = 0;
        var invert = false;
        if (value.Length > 0 && (value[0] == '+' || value[0] == '-'))
        {
            sourceHalf = value[0] == '+' ? 1 : -1;
            value = value[1..];
        }

        if (value.EndsWith('~'))
        {
            invert = true;
            value = value[..^1];
        }

        if (value.Length < 2)
        {
            return false;
        }

        var kind = value[0] switch
        {
            'b' => SourceKind.Button,
            'a' => SourceKind.Axis,
            'h' => SourceKind.Hat,
            _ => (SourceKind)255,
        };
        if (kind == (SourceKind)255)
        {
            return false;
        }

        var index = 0;
        var hatMask = 0;
        if (kind == SourceKind.Hat)
        {
            var dot = value.IndexOf('.', StringComparison.Ordinal);
            if (dot < 0
                || !int.TryParse(value.AsSpan(1, dot - 1), NumberStyles.None, CultureInfo.InvariantCulture, out index)
                || !int.TryParse(value.AsSpan(dot + 1), NumberStyles.None, CultureInfo.InvariantCulture, out hatMask))
            {
                return false;
            }
        }
        else if (!int.TryParse(value.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out index))
        {
            return false;
        }

        binding = new Binding(isAxis, button, axis, targetHalf, kind, index, hatMask, sourceHalf, invert);
        return true;
    }

    private static bool TryAxis(string name, out GamepadAxis axis)
    {
        (var ok, axis) = name switch
        {
            "leftx" => (true, GamepadAxis.LeftX),
            "lefty" => (true, GamepadAxis.LeftY),
            "rightx" => (true, GamepadAxis.RightX),
            "righty" => (true, GamepadAxis.RightY),
            "lefttrigger" => (true, GamepadAxis.LeftTrigger),
            "righttrigger" => (true, GamepadAxis.RightTrigger),
            _ => (false, default),
        };
        return ok;
    }

    private static bool TryButton(string name, out GamepadButton button)
    {
        (var ok, button) = name switch
        {
            "a" => (true, GamepadButton.South),
            "b" => (true, GamepadButton.East),
            "x" => (true, GamepadButton.West),
            "y" => (true, GamepadButton.North),
            "back" => (true, GamepadButton.Back),
            "start" => (true, GamepadButton.Start),
            "guide" => (true, GamepadButton.Guide),
            "leftstick" => (true, GamepadButton.LeftStick),
            "rightstick" => (true, GamepadButton.RightStick),
            "leftshoulder" => (true, GamepadButton.LeftShoulder),
            "rightshoulder" => (true, GamepadButton.RightShoulder),
            "dpup" => (true, GamepadButton.DPadUp),
            "dpdown" => (true, GamepadButton.DPadDown),
            "dpleft" => (true, GamepadButton.DPadLeft),
            "dpright" => (true, GamepadButton.DPadRight),
            "misc1" => (true, GamepadButton.Misc1),
            "touchpad" => (true, GamepadButton.Touchpad),
            "paddle1" => (true, GamepadButton.Paddle1),
            "paddle2" => (true, GamepadButton.Paddle2),
            "paddle3" => (true, GamepadButton.Paddle3),
            "paddle4" => (true, GamepadButton.Paddle4),
            _ => (false, default),
        };
        return ok;
    }
}

/// <summary>A set of <see cref="GamepadMapping"/> entries looked up by device GUID.</summary>
public sealed class GamepadMappingDatabase
{
    private readonly Dictionary<string, GamepadMapping> _exact = [];
    private readonly Dictionary<string, GamepadMapping> _loose = [];

    /// <summary>The number of entries.</summary>
    public int Count => _exact.Count;

    /// <summary>Adds every valid entry in <paramref name="text"/> (one per line). Entries for other platforms are skipped. Later entries win.</summary>
    /// <returns>How many entries were added.</returns>
    public int AddText(string text, string? platform = null)
    {
        platform ??= CurrentPlatformName();
        var added = 0;
        foreach (var line in text.Split('\n'))
        {
            if (!GamepadMapping.TryParse(line, out var m) || m is null)
            {
                continue;
            }

            if (m.Platform.Length != 0 && platform.Length != 0 && !string.Equals(m.Platform, platform, StringComparison.Ordinal))
            {
                continue;
            }

            Add(m);
            added++;
        }

        return added;
    }

    /// <summary>Adds one entry.</summary>
    public void Add(GamepadMapping mapping)
    {
        _exact[mapping.DeviceId] = mapping;
        _loose[LooseKey(mapping.DeviceId)] = mapping;
    }

    /// <summary>Finds the entry for a device: an exact GUID match first, then one that differs only in the name checksum and version fields.</summary>
    public GamepadMapping? Find(string deviceId)
    {
        deviceId = deviceId.ToLowerInvariant();
        if (_exact.TryGetValue(deviceId, out var m))
        {
            return m;
        }

        return deviceId.Length == 32 && _loose.TryGetValue(LooseKey(deviceId), out m) ? m : null;
    }

    internal static string CurrentPlatformName() =>
        OperatingSystem.IsWindows() ? "Windows" : OperatingSystem.IsLinux() ? "Linux" : OperatingSystem.IsMacOS() ? "Mac OS X" : "";

    // GUID bytes 2-3 are a checksum of the name and bytes 12-15 hold the version and driver data; neither identifies the model.
    private static string LooseKey(string id) => id.Length == 32 ? string.Concat(id.AsSpan(0, 4), id.AsSpan(8, 16)) : id;
}
