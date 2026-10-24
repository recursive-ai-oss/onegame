using System.Globalization;
using System.Text;

namespace OneGame.Platform;

/// <summary>What an evdev device reports about itself (the answers to the EVIOCG* queries).</summary>
internal sealed class EvdevCapabilities
{
    public const int KeyMax = 0x2ff;
    public const int AbsMax = 0x3f;

    public string Name { get; init; } = "";

    public ushort Bus { get; init; }

    public ushort Vendor { get; init; }

    public ushort Product { get; init; }

    public ushort Version { get; init; }

    /// <summary>Key and button codes the device can produce.</summary>
    public HashSet<int> Keys { get; init; } = [];

    /// <summary>Absolute axes by code, with their ranges.</summary>
    public Dictionary<int, (int Min, int Max, int Value)> Abs { get; init; } = [];

    /// <summary>Whether the device supports rumble force feedback.</summary>
    public bool CanRumble { get; init; }
}

/// <summary>
/// Interprets the events of one evdev gamepad: numbers its buttons, axes and hats the way community mapping databases do,
/// finds its mapping, and drives a <see cref="GamepadMapper"/>.
/// </summary>
internal sealed class EvdevGamepad
{
    public const int EvSyn = 0, EvKey = 1, EvAbs = 3;
    public const int AbsHat0X = 0x10, AbsHat3Y = 0x17;
    public const int BtnJoystick = 0x120, BtnGamepad = 0x130, BtnMisc = 0x100;

    private readonly Dictionary<int, int> _buttonIndex = [];
    private readonly Dictionary<int, int> _axisIndex = [];
    private readonly Dictionary<int, (int Min, int Max)> _axisRange = [];
    private readonly int[] _hatMask = new int[4];
    private readonly int[] _hatX = new int[4];
    private readonly int[] _hatY = new int[4];
    private readonly GamepadMapper _mapper;

    public EvdevGamepad(int id, EvdevCapabilities caps, GamepadMappingDatabase database, float stickDeadZone, float triggerDeadZone, Action<PlatformEvent> sink)
    {
        Id = id;
        Capabilities = caps;
        DeviceId = MakeDeviceId(caps);

        // Buttons are numbered in code order starting from BTN_MISC, then the low key codes (keyboard-style keys) last.
        var index = 0;
        for (var code = BtnMisc; code <= EvdevCapabilities.KeyMax; code++)
        {
            if (caps.Keys.Contains(code))
            {
                _buttonIndex[code] = index++;
            }
        }

        for (var code = 0; code < BtnMisc; code++)
        {
            if (caps.Keys.Contains(code))
            {
                _buttonIndex[code] = index++;
            }
        }

        // Axes are numbered in code order, skipping the hat axes, which are reported as hats instead.
        var axes = 0;
        var hats = 0;
        for (var code = 0; code <= EvdevCapabilities.AbsMax; code++)
        {
            if (!caps.Abs.TryGetValue(code, out var info))
            {
                continue;
            }

            if (code >= AbsHat0X && code <= AbsHat3Y)
            {
                hats = Math.Max(hats, ((code - AbsHat0X) / 2) + 1);
                continue;
            }

            _axisIndex[code] = axes++;
            _axisRange[code] = (info.Min, info.Max);
        }

        var found = database.Find(DeviceId);
        Mapped = found is not null;
        Mapping = found ?? BuildFallback(caps);
        _mapper = new GamepadMapper(Mapping, id, index, axes, hats, stickDeadZone, triggerDeadZone, sink);

        foreach (var (code, info) in caps.Abs)
        {
            if (code is >= AbsHat0X and <= AbsHat3Y || info.Max == info.Min)
            {
                if (code is >= AbsHat0X and <= AbsHat3Y)
                {
                    SetHat(code, info.Value);
                }

                continue;
            }

            SetAxis(code, info.Value);
        }

        _mapper.Flush();
    }

    public int Id { get; }

    public EvdevCapabilities Capabilities { get; }

    public string DeviceId { get; }

    public GamepadMapping Mapping { get; }

    /// <summary>True when the mapping came from the database rather than the built-in standard layout.</summary>
    public bool Mapped { get; }

    /// <summary>Whether the device looks like a gamepad or joystick rather than a keyboard, mouse or touchpad.</summary>
    public static bool LooksLikeGamepad(EvdevCapabilities caps)
    {
        var hasPadButton = false;
        for (var code = BtnJoystick; code < 0x140; code++)
        {
            if (caps.Keys.Contains(code))
            {
                hasPadButton = true;
                break;
            }
        }

        return hasPadButton && caps.Abs.Count >= 2;
    }

    /// <summary>
    /// The community GUID layout: bus, a name checksum (zero when the vendor and product are known), vendor, product and
    /// version as little-endian 16-bit fields, each padded as the database format expects.
    /// </summary>
    public static string MakeDeviceId(EvdevCapabilities c)
    {
        Span<byte> b = stackalloc byte[16];
        b.Clear();
        WriteU16(b, 0, c.Bus);
        WriteU16(b, 4, c.Vendor);
        WriteU16(b, 8, c.Product);
        WriteU16(b, 12, c.Version);
        return Convert.ToHexStringLower(b);
    }

    public void SetSink(Action<PlatformEvent> sink) => _mapper.SetSink(sink);

    public void Handle(int type, int code, int value)
    {
        switch (type)
        {
            case EvKey:
                if (_buttonIndex.TryGetValue(code, out var button))
                {
                    _mapper.SetButton(button, value != 0);
                }

                break;
            case EvAbs:
                if (code is >= AbsHat0X and <= AbsHat3Y)
                {
                    SetHat(code, value);
                }
                else
                {
                    SetAxis(code, value);
                }

                break;
            case EvSyn when code == 0:
                _mapper.Flush();
                break;
        }
    }

    private void SetAxis(int code, int value)
    {
        if (_axisIndex.TryGetValue(code, out var index))
        {
            var (min, max) = _axisRange[code];
            _mapper.SetAxis(index, max == min ? 0f : (((float)(value - min) / (max - min)) * 2f) - 1f);
        }
    }

    private void SetHat(int code, int value)
    {
        var hat = (code - AbsHat0X) / 2;
        if (((code - AbsHat0X) & 1) == 0)
        {
            _hatX[hat] = Math.Sign(value);
        }
        else
        {
            _hatY[hat] = Math.Sign(value);
        }

        // Hat mask bits: up 1, right 2, down 4, left 8.
        _hatMask[hat] = (_hatY[hat] < 0 ? 1 : 0) | (_hatX[hat] > 0 ? 2 : 0) | (_hatY[hat] > 0 ? 4 : 0) | (_hatX[hat] < 0 ? 8 : 0);
        _mapper.SetHat(hat, _hatMask[hat]);
    }

    private static void WriteU16(Span<byte> destination, int offset, ushort value)
    {
        destination[offset] = (byte)(value & 0xff);
        destination[offset + 1] = (byte)(value >> 8);
    }

    // The kernel's gamepad API fixes which code is which button, so a device with no database entry can still be mapped by code.
    private GamepadMapping BuildFallback(EvdevCapabilities caps)
    {
        var sb = new StringBuilder();
        sb.Append(DeviceId).Append(",Standard layout,");

        void Button(string target, int code)
        {
            if (_buttonIndex.TryGetValue(code, out var i))
            {
                sb.Append(target).Append(":b").Append(i.ToString(CultureInfo.InvariantCulture)).Append(',');
            }
        }

        void Axis(string target, int code)
        {
            if (_axisIndex.TryGetValue(code, out var i))
            {
                sb.Append(target).Append(":a").Append(i.ToString(CultureInfo.InvariantCulture)).Append(',');
            }
        }

        Button("a", 0x130);
        Button("b", 0x131);
        // The kernel names X and Y after the Xbox labels: X is BTN_NORTH (0x133) and Y is BTN_WEST (0x134).
        Button("x", 0x133);
        Button("y", 0x134);
        Button("leftshoulder", 0x136);
        Button("rightshoulder", 0x137);
        Button("back", 0x13a);
        Button("start", 0x13b);
        Button("guide", 0x13c);
        Button("leftstick", 0x13d);
        Button("rightstick", 0x13e);
        Axis("leftx", 0);
        Axis("lefty", 1);
        Axis("rightx", 3);
        Axis("righty", 4);
        Axis("lefttrigger", 2);
        Axis("righttrigger", 5);
        if (caps.Abs.ContainsKey(AbsHat0X))
        {
            sb.Append("dpup:h0.1,dpright:h0.2,dpdown:h0.4,dpleft:h0.8,");
        }
        else
        {
            Button("dpup", 0x220);
            Button("dpdown", 0x221);
            Button("dpleft", 0x222);
            Button("dpright", 0x223);
        }

        return GamepadMapping.TryParse(sb.ToString(), out var mapping) ? mapping! : throw new InvalidOperationException("built-in mapping is malformed");
    }
}
