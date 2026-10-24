using System.Text;

namespace OneGame.Platform;

/// <summary>An open /dev/input/event* node for a gamepad: probes capabilities, reads events, plays rumble.</summary>
internal sealed unsafe class EvdevDevice : IDisposable
{
    private const uint Read = 2, Write = 1;
    private const int EvKey = 1, EvAbs = 3, EvFf = 0x15, FfRumble = 0x50;
    private const int InputEventSize = 24; // struct input_event on 64-bit Linux: timeval (16) + type, code (4) + value (4).

    private int _fd;
    private short _effectId = -1;

    private EvdevDevice(int fd, string path, EvdevCapabilities caps)
    {
        _fd = fd;
        Path = path;
        Capabilities = caps;
    }

    public string Path { get; }

    public EvdevCapabilities Capabilities { get; }

    public EvdevGamepad? Pad { get; set; }

    /// <summary>Opens a device node and reads its capabilities; null when it cannot be opened or is not a gamepad.</summary>
    public static EvdevDevice? TryOpen(string path)
    {
        var fd = Libc.OpenNonBlocking(path, write: true);
        if (fd < 0)
        {
            // Force feedback needs write access; reading alone still gives input.
            fd = Libc.OpenNonBlocking(path);
        }

        if (fd < 0)
        {
            return null;
        }

        var caps = Probe(fd);
        if (caps is null || !EvdevGamepad.LooksLikeGamepad(caps))
        {
            _ = Libc.Close(fd);
            return null;
        }

        return new EvdevDevice(fd, path, caps);
    }

    private static EvdevCapabilities? Probe(int fd)
    {
        Span<byte> name = stackalloc byte[128];
        string deviceName;
        fixed (byte* p = name)
        {
            var n = Libc.Ioctl(fd, Libc.IoC(Read, 'E', 0x06, 128), p);
            if (n < 0)
            {
                return null;
            }

            deviceName = Encoding.UTF8.GetString(name[..Math.Max(0, Math.Min(n, 128) - 1)]);
        }

        Span<ushort> id = stackalloc ushort[4];
        fixed (ushort* p = id)
        {
            if (Libc.Ioctl(fd, Libc.IoC(Read, 'E', 0x02, 8), p) < 0)
            {
                return null;
            }
        }

        var keys = new HashSet<int>();
        Span<byte> keyBits = stackalloc byte[(EvdevCapabilities.KeyMax / 8) + 1];
        fixed (byte* p = keyBits)
        {
            if (Libc.Ioctl(fd, Libc.IoC(Read, 'E', 0x20 + EvKey, (uint)keyBits.Length), p) >= 0)
            {
                for (var code = 0; code <= EvdevCapabilities.KeyMax; code++)
                {
                    if ((keyBits[code >> 3] & (1 << (code & 7))) != 0)
                    {
                        keys.Add(code);
                    }
                }
            }
        }

        var abs = new Dictionary<int, (int, int, int)>();
        Span<byte> absBits = stackalloc byte[(EvdevCapabilities.AbsMax / 8) + 1];
        fixed (byte* p = absBits)
        {
            if (Libc.Ioctl(fd, Libc.IoC(Read, 'E', 0x20 + EvAbs, (uint)absBits.Length), p) >= 0)
            {
                Span<int> info = stackalloc int[6]; // struct input_absinfo: value, minimum, maximum, fuzz, flat, resolution.
                for (var code = 0; code <= EvdevCapabilities.AbsMax; code++)
                {
                    if ((absBits[code >> 3] & (1 << (code & 7))) == 0)
                    {
                        continue;
                    }

                    fixed (int* q = info)
                    {
                        if (Libc.Ioctl(fd, Libc.IoC(Read, 'E', (uint)(0x40 + code), 24), q) >= 0)
                        {
                            abs[code] = (info[1], info[2], info[0]);
                        }
                    }
                }
            }
        }

        var canRumble = false;
        Span<byte> ffBits = stackalloc byte[16];
        fixed (byte* p = ffBits)
        {
            if (Libc.Ioctl(fd, Libc.IoC(Read, 'E', 0x20 + EvFf, 16), p) >= 0)
            {
                canRumble = (ffBits[FfRumble >> 3] & (1 << (FfRumble & 7))) != 0;
            }
        }

        return new EvdevCapabilities
        {
            Name = deviceName,
            Bus = id[0],
            Vendor = id[1],
            Product = id[2],
            Version = id[3],
            Keys = keys,
            Abs = abs,
            CanRumble = canRumble,
        };
    }

    /// <summary>Reads and handles every pending event. Returns false when the device has been unplugged.</summary>
    public bool Drain()
    {
        Span<byte> buffer = stackalloc byte[InputEventSize * 64];
        while (_fd >= 0)
        {
            nint n;
            fixed (byte* p = buffer)
            {
                n = Libc.Read(_fd, p, (nuint)buffer.Length);
            }

            if (n < 0)
            {
                return System.Runtime.InteropServices.Marshal.GetLastPInvokeError() == Libc.EAGAIN;
            }

            if (n == 0)
            {
                return false;
            }

            for (var offset = 0; offset + InputEventSize <= n; offset += InputEventSize)
            {
                var type = BitConverter.ToUInt16(buffer[(offset + 16)..]);
                var code = BitConverter.ToUInt16(buffer[(offset + 18)..]);
                var value = BitConverter.ToInt32(buffer[(offset + 20)..]);
                Pad?.Handle(type, code, value);
            }
        }

        return false;
    }

    /// <summary>Plays a rumble effect. Magnitudes are 0 to 1. Returns false when the device cannot rumble.</summary>
    public bool Rumble(float strong, float weak, int durationMilliseconds)
    {
        if (!Capabilities.CanRumble || _fd < 0)
        {
            return false;
        }

        // struct ff_effect on 64-bit Linux is 48 bytes: type, id, direction, trigger, replay, then a union starting at offset 16
        // whose rumble variant is { u16 strong_magnitude; u16 weak_magnitude; }.
        Span<byte> effect = stackalloc byte[48];
        effect.Clear();
        BitConverter.TryWriteBytes(effect[0..], (ushort)FfRumble);
        BitConverter.TryWriteBytes(effect[2..], _effectId);
        BitConverter.TryWriteBytes(effect[10..], (ushort)Math.Clamp(durationMilliseconds, 0, 0x7fff));
        BitConverter.TryWriteBytes(effect[16..], (ushort)(Math.Clamp(strong, 0f, 1f) * 0xffff));
        BitConverter.TryWriteBytes(effect[18..], (ushort)(Math.Clamp(weak, 0f, 1f) * 0xffff));
        fixed (byte* p = effect)
        {
            if (Libc.Ioctl(_fd, Libc.IoC(Write, 'E', 0x80, 48), p) < 0)
            {
                return false;
            }
        }

        _effectId = BitConverter.ToInt16(effect[2..]);

        Span<byte> play = stackalloc byte[InputEventSize];
        play.Clear();
        BitConverter.TryWriteBytes(play[16..], (ushort)EvFf);
        BitConverter.TryWriteBytes(play[18..], (ushort)_effectId);
        BitConverter.TryWriteBytes(play[20..], 1);
        fixed (byte* p = play)
        {
            return Libc.Write(_fd, p, InputEventSize) == InputEventSize;
        }
    }

    public void Dispose()
    {
        if (_fd >= 0)
        {
            _ = Libc.Close(_fd);
            _fd = -1;
        }
    }
}
