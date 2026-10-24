using System.Runtime.InteropServices;

namespace OneGame.Platform;

/// <summary>One XInput controller: feeds its state through the same mapper as every other pad, using an identity mapping onto the standard layout.</summary>
internal sealed class XInputPad
{
    // Raw layout: buttons 0-13 in the order below, axes 0-5 are left x/y, right x/y, left trigger, right trigger.
    private const string IdentityMapping =
        "00000000000000000000000000000000,XInput,a:b0,b:b1,x:b2,y:b3,leftshoulder:b4,rightshoulder:b5,back:b6,start:b7,leftstick:b8,rightstick:b9,"
        + "dpup:b10,dpdown:b11,dpleft:b12,dpright:b13,leftx:a0,lefty:a1,rightx:a2,righty:a3,lefttrigger:a4,righttrigger:a5,";

    // XINPUT_GAMEPAD_* bit masks, in the raw button order above.
    private static readonly ushort[] s_buttonMasks = [0x1000, 0x2000, 0x4000, 0x8000, 0x0100, 0x0200, 0x0020, 0x0010, 0x0040, 0x0080, 0x0001, 0x0002, 0x0004, 0x0008];

    private readonly GamepadMapper _mapper;

    public XInputPad(int id, float stickDeadZone, float triggerDeadZone, Action<PlatformEvent> sink)
    {
        if (!GamepadMapping.TryParse(IdentityMapping, out var mapping))
        {
            throw new InvalidOperationException("built-in XInput mapping is malformed");
        }

        _mapper = new GamepadMapper(mapping!, id, s_buttonMasks.Length, 6, 0, stickDeadZone, triggerDeadZone, sink);
    }

    /// <summary>Applies a new XINPUT_GAMEPAD state. XInput's y axes point up and its triggers run 0-255; the standard layout wants y down and triggers as -1 to 1 raw.</summary>
    public void Update(ushort buttons, byte leftTrigger, byte rightTrigger, short thumbLX, short thumbLY, short thumbRX, short thumbRY)
    {
        for (var i = 0; i < s_buttonMasks.Length; i++)
        {
            _mapper.SetButton(i, (buttons & s_buttonMasks[i]) != 0);
        }

        _mapper.SetAxis(0, thumbLX / 32768f);
        _mapper.SetAxis(1, -thumbLY / 32768f);
        _mapper.SetAxis(2, thumbRX / 32768f);
        _mapper.SetAxis(3, -thumbRY / 32768f);
        _mapper.SetAxis(4, (leftTrigger / 255f * 2f) - 1f);
        _mapper.SetAxis(5, (rightTrigger / 255f * 2f) - 1f);
        _mapper.Flush();
    }
}

/// <summary>The four XInput slots on Windows.</summary>
internal sealed unsafe partial class XInputGamepads
{
    private const int Slots = 4;
    private const uint ErrorSuccess = 0;
    private const long RecheckMilliseconds = 1000;

    private readonly PlatformApp _app;
    private readonly Func<int> _nextId;
    private readonly int[] _ids = new int[Slots];
    private readonly XInputPad?[] _pads = new XInputPad?[Slots];
    private readonly uint[] _packets = new uint[Slots];
    private readonly long[] _rumbleStop = new long[Slots];
    private long _nextProbe;
    private bool _unavailable;

    public XInputGamepads(PlatformApp app, Func<int> nextId)
    {
        _app = app;
        _nextId = nextId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputState
    {
        public uint PacketNumber;
        public ushort Buttons;
        public byte LeftTrigger, RightTrigger;
        public short ThumbLX, ThumbLY, ThumbRX, ThumbRY;
    }

    [LibraryImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
    private static partial uint GetState(uint user, XInputState* state);

    [LibraryImport("xinput1_4.dll", EntryPoint = "XInputSetState")]
    private static partial uint SetState(uint user, ushort* vibration);

    public void Poll()
    {
        if (_unavailable)
        {
            return;
        }

        var probe = false;
        var now = Environment.TickCount64;
        ushort* off = stackalloc ushort[2];
        off[0] = 0;
        off[1] = 0;
        for (var slot = 0; slot < Slots; slot++)
        {
            // XInput vibration runs until it is told to stop, so the requested duration is enforced here.
            if (_rumbleStop[slot] != 0 && now >= _rumbleStop[slot])
            {
                _rumbleStop[slot] = 0;
                _ = SetState((uint)slot, off);
            }
        }

        if (now >= _nextProbe)
        {
            probe = true;
            _nextProbe = now + RecheckMilliseconds;
        }

        for (var slot = 0; slot < Slots; slot++)
        {
            // Asking an empty slot is slow on Windows, so empty slots are only checked once a second.
            if (_pads[slot] is null && !probe)
            {
                continue;
            }

            XInputState state;
            uint result;
            try
            {
                result = GetState((uint)slot, &state);
            }
            catch (DllNotFoundException)
            {
                _unavailable = true;
                return;
            }

            if (result != ErrorSuccess)
            {
                if (_pads[slot] is not null)
                {
                    _pads[slot] = null;
                    _app.Post(new PlatformEvent { Kind = PlatformEventKind.GamepadDisconnected, GamepadId = _ids[slot] });
                }

                continue;
            }

            if (_pads[slot] is null)
            {
                var id = _nextId();
                _ids[slot] = id;
                _app.Input.AddGamepad(id, "XInput controller " + (slot + 1), isMapped: true, canRumble: true);
                _app.Post(new PlatformEvent { Kind = PlatformEventKind.GamepadConnected, GamepadId = id });
                _pads[slot] = new XInputPad(id, _app.Options.StickDeadZone, _app.Options.TriggerDeadZone, e => _app.Post(e));
                _packets[slot] = state.PacketNumber - 1;
            }

            if (state.PacketNumber != _packets[slot])
            {
                _packets[slot] = state.PacketNumber;
                _pads[slot]!.Update(state.Buttons, state.LeftTrigger, state.RightTrigger, state.ThumbLX, state.ThumbLY, state.ThumbRX, state.ThumbRY);
            }
        }
    }

    public bool Rumble(int gamepadId, float low, float high, int durationMilliseconds)
    {
        for (var slot = 0; slot < Slots; slot++)
        {
            if (_pads[slot] is not null && _ids[slot] == gamepadId)
            {
                // XINPUT_VIBRATION: the left motor is the heavy one.
                ushort* vibration = stackalloc ushort[2];
                vibration[0] = (ushort)(Math.Clamp(low, 0f, 1f) * 0xffff);
                vibration[1] = (ushort)(Math.Clamp(high, 0f, 1f) * 0xffff);
                var ok = SetState((uint)slot, vibration) == ErrorSuccess;
                _rumbleStop[slot] = ok && (low > 0f || high > 0f) ? Environment.TickCount64 + Math.Max(1, durationMilliseconds) : 0;
                return ok;
            }
        }

        return false;
    }
}
