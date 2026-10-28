using System.Diagnostics;
using System.Runtime.CompilerServices;
using OneGame.Audio;
using OneGame.Host;
using OneGame.Net;
using OneGame.Platform;
using OneGame.UI;

namespace OneGame.Control;

/// <summary>
/// The on-screen debug panel of development builds: frame time, entity count, network figures when the game is connected, and the audio
/// device's counters. It reads what the control layer already reports, draws over the finished frame (captures included) and never touches
/// game state, so the state hash is the same with it shown or hidden. A release build does not reference this assembly.
/// <para>
/// Toggle it with <see cref="ToggleKey"/> (F3) or the <see cref="ToggleChord"/> on a gamepad (Back and Start together). Both can be changed
/// with the environment variables <c>ONEGAME_OVERLAY_KEY</c> and <c>ONEGAME_OVERLAY_CHORD</c> (<c>none</c> turns one off), and
/// <c>ONEGAME_OVERLAY=on</c> shows it from the first frame. The <c>overlay</c> control operation and the scenario step <c>overlay:</c> do the same.
/// </para>
/// </summary>
public sealed class DebugOverlay : IFrameOverlay
{
    private const int History = 60;
    private const int RefreshFrames = 15;
    private const string Separator = "  ";
    private const string FrameTemplate = "frame 000.0 ms  min 000.0  avg 000.0  max 000.0";
    private const string NetTemplate = "net udp  rtt 000.0 ms  in 000.0 KB/s out 000.0 KB/s  drift none";
    private const string AudioTemplate = "audio wasapi  underruns 0000  peak 0.00 !";

    private static readonly ConditionalWeakTable<GameInstance, DebugOverlay> Attached = new();
    private readonly float[] _ms = new float[History];
    private readonly List<string> _lines = [];
    private readonly GameInstance _game;
    private int _count;
    private int _next;
    private long _lastStamp;
    private int _sinceRefresh = RefreshFrames;
    private float _width;
    private ulong _padDown;
    private long _netStamp;
    private long _netIn;
    private long _netOut;
    private double _rateIn;
    private double _rateOut;
    private bool _drawn;
    private bool _visible;
    private bool _hasNet;
    private bool _hasAudio;
    private int _shape = -1;
    private float _shapeSize;

    private DebugOverlay(GameInstance game)
    {
        _game = game;
        Visible = Env("ONEGAME_OVERLAY") is "on" or "1" or "true";
        if (Env("ONEGAME_OVERLAY_KEY") is { } key)
        {
            ToggleKey = key.Equals("none", StringComparison.OrdinalIgnoreCase) ? null : KeyNames.TryParse(key, out var k) ? k : throw new FormatException($"ONEGAME_OVERLAY_KEY '{key}' is not a key name (F4, Backquote, none).");
        }

        if (Env("ONEGAME_OVERLAY_CHORD") is { } chord)
        {
            ToggleChord = ParseChord(chord);
        }

        game.InputFilter = Filter;
        if (game.Stage is Render2DStage stage)
        {
            stage.Overlay = this;
        }
    }

    /// <summary>Whether the panel is shown. While it is, <see cref="Draw2D.DebugVisible"/> is true so views draw their debug geometry too.</summary>
    public bool Visible
    {
        get => _visible;
        set
        {
            _visible = value;
            if (_game.Stage is Render2DStage stage)
            {
                stage.Draw.DebugVisible = value;
            }
        }
    }

    /// <summary>The key that toggles the panel, or null for none. F3 by default.</summary>
    public Key? ToggleKey { get; set; } = Key.F3;

    /// <summary>The gamepad buttons that toggle the panel when all are held, or empty for none. Back and Start by default.</summary>
    public GamepadButton[] ToggleChord { get; set; } = [GamepadButton.Back, GamepadButton.Start];

    /// <summary>
    /// Shows <c>--</c> for every figure that depends on the machine or the moment (frame times, network rates, audio counters), so a capture
    /// is the same everywhere. Null, the default, masks in a headless run only.
    /// </summary>
    public bool? Masked { get; set; }

    /// <summary>The audio service whose device is reported; the newest one when null.</summary>
    public Func<AudioService?>? AudioSource { get; set; }

    /// <summary>Whether the figures are masked now.</summary>
    public bool IsMasked => Masked ?? _game.Options.Headless;

    /// <summary>The lines of the panel as last built.</summary>
    public IReadOnlyList<string> Lines => _lines;

    /// <summary>Makes the overlay for a game, or returns the one it already has. It is hooked into the game's 2D stage and input.</summary>
    /// <param name="game">The game.</param>
    /// <returns>The game's overlay.</returns>
    public static DebugOverlay For(GameInstance game)
    {
        ArgumentNullException.ThrowIfNull(game);
        return Attached.GetValue(game, static g => new DebugOverlay(g));
    }

    /// <summary>Reads a button chord such as <c>Back+Start</c>; <c>none</c> gives an empty chord.</summary>
    /// <param name="text">Button names joined by <c>+</c>.</param>
    /// <returns>The buttons.</returns>
    /// <exception cref="FormatException">A name is not a gamepad button.</exception>
    public static GamepadButton[] ParseChord(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Equals("none", StringComparison.OrdinalIgnoreCase) || text.Length == 0)
        {
            return [];
        }

        var parts = text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var buttons = new GamepadButton[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            buttons[i] = Enum.TryParse<GamepadButton>(parts[i], true, out var b) && Enum.IsDefined(b) ? b : throw new FormatException($"'{parts[i]}' is not a gamepad button (South, East, West, North, LeftShoulder, RightShoulder, Back, Start...).");
        }

        return buttons;
    }

    /// <summary>Builds the lines now (the panel itself rebuilds four times a second) and returns them.</summary>
    /// <returns>The panel's text.</returns>
    public IReadOnlyList<string> Refresh()
    {
        Build();
        return _lines;
    }

    private static string? Env(string name) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } v ? v : null;

    private bool Filter(in PlatformEvent e)
    {
        switch (e.Kind)
        {
            case PlatformEventKind.KeyDown when ToggleKey is { } key && e.Key == key:
                if (!e.IsRepeat)
                {
                    Visible = !Visible;
                }

                return true;
            case PlatformEventKind.GamepadButtonDown when Array.IndexOf(ToggleChord, e.GamepadButton) >= 0:
                _padDown |= 1UL << (int)e.GamepadButton;
                var all = true;
                foreach (var b in ToggleChord)
                {
                    all &= (_padDown & (1UL << (int)b)) != 0;
                }

                if (all)
                {
                    Visible = !Visible;
                    return true;
                }

                return false;
            case PlatformEventKind.GamepadButtonUp:
                _padDown &= ~(1UL << (int)e.GamepadButton);
                return false;
            default:
                return false;
        }
    }

    /// <inheritdoc />
    public void Draw(OverlayCanvas canvas)
    {
        if (!Visible)
        {
            _lastStamp = 0;
            return;
        }

        var now = Stopwatch.GetTimestamp();
        if (_lastStamp != 0)
        {
            var ms = (float)((now - _lastStamp) * 1000.0 / Stopwatch.Frequency);
            // A frame that took over a second was a pause (a hidden panel, a breakpoint), not a frame time.
            if (ms < 1000)
            {
                _ms[_next] = ms;
                _next = (_next + 1) % History;
                _count = Math.Min(_count + 1, History);
            }
        }

        _lastStamp = now;
        var s = canvas.Scale > 0 ? canvas.Scale : 1f;
        var size = 13 * s;
        if (!_drawn || ++_sinceRefresh >= RefreshFrames)
        {
            _sinceRefresh = 0;
            Build();
        }

        // The panel is as wide as the longest text each line can have, so it does not jitter as digits change; it is measured again only
        // when the size or the set of lines changes.
        var shape = (_hasNet ? 1 : 0) | (_hasAudio ? 2 : 0);
        if (shape != _shape || size != _shapeSize)
        {
            _shape = shape;
            _shapeSize = size;
            _width = Math.Max(canvas.Measure(FrameTemplate, size), Math.Max(canvas.Measure(_hasNet ? NetTemplate : string.Empty, size), canvas.Measure(_hasAudio ? AudioTemplate : string.Empty, size)));
        }

        _drawn = true;
        var pad = 6 * s;
        var lineHeight = size * 1.35f;
        var masked = IsMasked;
        var spark = !masked && _count > 1;
        var barW = 2 * s;
        var sparkH = 22 * s;
        var w = Math.Max(_width, History * barW) + (2 * pad);
        var h = (_lines.Count * lineHeight) + (2 * pad) + (spark ? sparkH + (4 * s) : 0);
        var x = canvas.Width - w - (8 * s);
        var y = 8 * s;
        canvas.Fill(x, y, w, h, new Color(8, 10, 14, 200));
        for (var i = 0; i < _lines.Count; i++)
        {
            var warn = _lines[i].EndsWith('!');
            canvas.Text(_lines[i], x + pad, y + pad + (i * lineHeight), size, warn ? new Color(255, 120, 100) : new Color(235, 240, 245));
        }

        if (spark)
        {
            // One bar per recent frame, 33 ms tall at most: green under 20 ms, amber under 34, red beyond.
            var baseY = y + h - pad;
            for (var i = 0; i < _count; i++)
            {
                var v = _ms[(_next - _count + i + History) % History];
                var bh = Math.Clamp(v / 33f, 0.05f, 1f) * sparkH;
                var c = v < 20 ? new Color(90, 200, 120) : v < 34 ? new Color(230, 190, 70) : new Color(240, 90, 80);
                canvas.Fill(x + pad + (i * barW), baseY - bh, barW - (0.5f * s), bh, c);
            }
        }
    }

    private void Build()
    {
        var masked = IsMasked;
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        _lines.Clear();
        _lines.Add("debug overlay");
        if (masked || _count == 0)
        {
            _lines.Add("frame -- ms");
        }
        else
        {
            var min = float.MaxValue;
            var max = 0f;
            var sum = 0f;
            for (var i = 0; i < _count; i++)
            {
                var v = _ms[i];
                min = Math.Min(min, v);
                max = Math.Max(max, v);
                sum += v;
            }

            var cur = _ms[(_next - 1 + History) % History];
            _lines.Add(string.Create(inv, $"frame {cur:0.0} ms{Separator}min {min:0.0}{Separator}avg {sum / _count:0.0}{Separator}max {max:0.0}"));
        }

        _lines.Add(string.Create(inv, $"tick {_game.World.Tick}{Separator}entities {_game.World.Entities.Count}"));
        if (_game.Remote is { } remote)
        {
            var s = remote.Connection.Stats;
            var now = Stopwatch.GetTimestamp();
            if (_netStamp != 0 && now > _netStamp)
            {
                var dt = (now - _netStamp) / (double)Stopwatch.Frequency;
                _rateIn = (s.BytesReceived - _netIn) / dt;
                _rateOut = (s.BytesSent - _netOut) / dt;
            }

            _netStamp = now;
            _netIn = s.BytesReceived;
            _netOut = s.BytesSent;
            var drift = _game.DriftReports.Count;
            _lines.Add(masked
                ? string.Create(inv, $"net {remote.Transport}{Separator}rtt -- ms{Separator}in --/s out --/s{Separator}drift {(drift == 0 ? "none" : drift.ToString(inv))}{(drift == 0 ? "" : " !")}")
                : string.Create(inv, $"net {remote.Transport}{Separator}rtt {s.RttMs:0.0} ms{Separator}in {Rate(_rateIn)} out {Rate(_rateOut)}{Separator}drift {(drift == 0 ? "none" : drift.ToString(inv))}{(drift == 0 ? "" : " !")}"));
        }

        var audio = AudioSource?.Invoke() ?? AudioService.Active;
        _hasNet = _game.Remote is not null;
        _hasAudio = audio is not null;
        if (audio is not null)
        {
            var d = audio.Output.DeviceStatus();
            if (masked)
            {
                _lines.Add($"audio {d.Backend}{Separator}underruns --{Separator}peak --");
                _lines.Add("written -- played --");
            }
            else
            {
                var bad = d.Underruns > 0 || d.DeviceUnderruns > 0;
                _lines.Add(string.Create(inv, $"audio {d.Backend}{Separator}underruns {d.Underruns + d.DeviceUnderruns}{Separator}peak {d.PeakPlayed:0.00}{(bad ? " !" : "")}"));
                _lines.Add(string.Create(inv, $"written {d.FramesWritten} played {d.FramesPlayed}"));
            }
        }
    }

    private static string Rate(double bytesPerSecond) =>
        bytesPerSecond >= 1024 * 1024 ? string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{bytesPerSecond / (1024 * 1024):0.0} MB/s")
        : bytesPerSecond >= 1024 ? string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{bytesPerSecond / 1024:0.0} KB/s")
        : string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{bytesPerSecond:0} B/s");
}
