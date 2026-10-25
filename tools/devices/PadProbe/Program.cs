using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using OneGame.Platform;

// Watches the gamepads the platform layer finds and prints what it sees as JSON lines, then a one-line summary.
//
//   padprobe [--seconds N] [--db gamecontrollerdb.txt] [--rumble] [--expect NAME] [--quiet]
//
// Connect, name, mapping-database entry, idle values, every button and axis that moves, disconnects. Exit 0 when a pad (matching
// --expect, a case-insensitive part of its name) was seen; exit 1 when none was. Presses need a person (or a virtual pad,
// tools/devices/virtual-pad.py); with nobody pressing, the summary still tells what the device reported when idle.
internal static class Program
{
    private static int Main(string[] args)
    {
        var seconds = 8.0;
        string? db = null;
        string? expect = null;
        var rumble = false;
        var quiet = false;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--seconds": seconds = double.Parse(args[++i], CultureInfo.InvariantCulture); break;
                case "--db": db = args[++i]; break;
                case "--expect": expect = args[++i]; break;
                case "--rumble": rumble = true; break;
                case "--quiet": quiet = true; break;
                default:
                    Console.Error.WriteLine($"unknown option {args[i]}");
                    return 2;
            }
        }

        using var app = PlatformApp.Create(new PlatformOptions { Backend = PlatformBackendKind.Headless });
        var database = app.Gamepads.Mappings;
        var dbCount = 0;
        if (db is not null)
        {
            dbCount = database.AddText(File.ReadAllText(db), "Linux");
            app.Gamepads.Rescan();
        }

        var pads = new Dictionary<int, PadReport>();
        var clock = Stopwatch.StartNew();
        var rumbled = false;
        void Line(string kind, string body)
        {
            if (!quiet)
            {
                Console.WriteLine($"{{\"t\":{clock.Elapsed.TotalSeconds:F2},\"event\":\"{kind}\"{body}}}");
            }
        }

        while (clock.Elapsed.TotalSeconds < seconds)
        {
            app.PumpEvents(20);
            while (app.TryGetEvent(out var e))
            {
                switch (e.Kind)
                {
                    case PlatformEventKind.GamepadConnected:
                        {
                            var state = app.Input.GetGamepad(e.GamepadId);
                            var name = state?.Name ?? "";
                            var guid = EvdevGuid(name);
                            var entry = guid is null ? null : database.Find(guid);
                            var r = new PadReport(name, guid, entry?.Name, state?.IsMapped ?? false, state?.CanRumble ?? false);
                            pads[e.GamepadId] = r;
                            Line("connected", $",\"id\":{e.GamepadId},\"name\":{Q(name)},\"guid\":{Q(guid)},\"dbEntry\":{Q(entry?.Name)},\"mapped\":{B(r.Mapped)},\"canRumble\":{B(r.CanRumble)}");
                            if (state is not null)
                            {
                                r.Idle = Snapshot(state);
                                Line("idle", $",\"id\":{e.GamepadId},\"values\":{r.Idle}");
                            }

                            break;
                        }

                    case PlatformEventKind.GamepadDisconnected:
                        if (pads.TryGetValue(e.GamepadId, out var gone))
                        {
                            gone.Disconnected++;
                        }

                        Line("disconnected", $",\"id\":{e.GamepadId}");
                        break;
                    case PlatformEventKind.GamepadButtonDown:
                    case PlatformEventKind.GamepadButtonUp:
                        if (pads.TryGetValue(e.GamepadId, out var bp))
                        {
                            var down = e.Kind == PlatformEventKind.GamepadButtonDown;
                            if (down)
                            {
                                bp.Buttons.Add(e.GamepadButton.ToString());
                            }

                            Line("button", $",\"id\":{e.GamepadId},\"button\":\"{e.GamepadButton}\",\"down\":{B(down)}");
                        }

                        break;
                    case PlatformEventKind.GamepadAxisMoved:
                        if (pads.TryGetValue(e.GamepadId, out var ap))
                        {
                            var key = e.GamepadAxis.ToString();
                            var (lo, hi) = ap.Axes.GetValueOrDefault(key, (0f, 0f));
                            ap.Axes[key] = (Math.Min(lo, e.Value), Math.Max(hi, e.Value));
                            Line("axis", $",\"id\":{e.GamepadId},\"axis\":\"{key}\",\"value\":{e.Value.ToString("F3", CultureInfo.InvariantCulture)}");
                        }

                        break;
                    default:
                        break;
                }
            }

            if (rumble && !rumbled && pads.Count > 0 && clock.Elapsed.TotalSeconds > 1)
            {
                rumbled = true;
                foreach (var (id, r) in pads)
                {
                    r.RumbleStarted = app.Gamepads.Rumble(id, 0.6f, 0.6f, 400);
                    Line("rumble", $",\"id\":{id},\"started\":{B(r.RumbleStarted)}");
                }
            }
        }

        var matched = pads.Values.Where(p => expect is null || p.Name.Contains(expect, StringComparison.OrdinalIgnoreCase)).ToList();
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"{{\"summary\":true,\"database\":{dbCount},\"pads\":[");
        sb.Append(string.Join(',', pads.Values.Select(p =>
            $"{{\"name\":{Q(p.Name)},\"guid\":{Q(p.Guid)},\"dbEntry\":{Q(p.DbEntry)},\"mapped\":{B(p.Mapped)},\"canRumble\":{B(p.CanRumble)},\"rumbleStarted\":{B(p.RumbleStarted)},\"disconnects\":{p.Disconnected},\"buttonsPressed\":[{string.Join(',', p.Buttons.Order().Select(Q))}],\"axisRanges\":{{{string.Join(',', p.Axes.OrderBy(a => a.Key, StringComparer.Ordinal).Select(a => $"{Q(a.Key)}:[{a.Value.Item1.ToString("F2", CultureInfo.InvariantCulture)},{a.Value.Item2.ToString("F2", CultureInfo.InvariantCulture)}]"))}}}}}")));
        sb.Append(CultureInfo.InvariantCulture, $"],\"matched\":{matched.Count}}}");
        Console.WriteLine(sb.ToString());
        return matched.Count > 0 ? 0 : 1;
    }

    private static string B(bool v) => v ? "true" : "false";

    private static string Q(string? s) => s is null ? "null" : "\"" + s.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";

    private static string Snapshot(GamepadState s)
    {
        var sb = new StringBuilder("{");
        var first = true;
        foreach (var a in Enum.GetValues<GamepadAxis>())
        {
            sb.Append(CultureInfo.InvariantCulture, $"{(first ? "" : ",")}\"{a}\":{s.GetAxis(a).ToString("F3", CultureInfo.InvariantCulture)}");
            first = false;
        }

        var down = Enum.GetValues<GamepadButton>().Where(s.IsDown).Select(b => Q(b.ToString()));
        sb.Append(CultureInfo.InvariantCulture, $",\"buttonsDown\":[{string.Join(',', down)}]}}");
        return sb.ToString();
    }

    // The community database's device id for a Linux input device (bus, vendor, product, version little-endian; the name's checksum
    // stands in when the vendor is 0). Found by reading the name back from sysfs, because the platform reports the device name only.
    private static string? EvdevGuid(string name)
    {
        try
        {
            foreach (var dir in Directory.EnumerateDirectories("/sys/class/input", "event*"))
            {
                var d = Path.Combine(dir, "device");
                if (File.ReadAllText(Path.Combine(d, "name")).Trim() != name)
                {
                    continue;
                }

                ushort Id(string f) => Convert.ToUInt16(File.ReadAllText(Path.Combine(d, "id", f)).Trim(), 16);
                var bus = Id("bustype");
                var vendor = Id("vendor");
                var product = Id("product");
                var version = Id("version");
                if (vendor == 0 || product == 0)
                {
                    continue;
                }

                string Le(ushort v) => $"{v & 0xff:x2}{v >> 8:x2}";
                return $"{Le(bus)}0000{Le(vendor)}0000{Le(product)}0000{Le(version)}0000";
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return null;
    }

    private sealed class PadReport(string name, string? guid, string? dbEntry, bool mapped, bool canRumble)
    {
        public string Name { get; } = name;

        public string? Guid { get; } = guid;

        public string? DbEntry { get; } = dbEntry;

        public bool Mapped { get; } = mapped;

        public bool CanRumble { get; } = canRumble;

        public bool RumbleStarted { get; set; }

        public int Disconnected { get; set; }

        public string Idle { get; set; } = "{}";

        public SortedSet<string> Buttons { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, (float, float)> Axes { get; } = new(StringComparer.Ordinal);
    }
}
