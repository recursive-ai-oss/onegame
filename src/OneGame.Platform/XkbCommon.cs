using System.Runtime.InteropServices;
using System.Text;

namespace OneGame.Platform;

/// <summary>libxkbcommon entry points: keymaps turn key codes into characters for the active layout.</summary>
internal static unsafe partial class XkbNative
{
    private const string Lib = "libxkbcommon.so.0";
    private const string LibX11 = "libxkbcommon-x11.so.0";

    [LibraryImport(Lib, EntryPoint = "xkb_context_new")]
    public static partial nint ContextNew(int flags);

    [LibraryImport(Lib, EntryPoint = "xkb_context_unref")]
    public static partial void ContextUnref(nint context);

    [LibraryImport(Lib, EntryPoint = "xkb_keymap_new_from_names")]
    public static partial nint KeymapNewFromNames(nint context, void* names, int flags);

    [LibraryImport(Lib, EntryPoint = "xkb_keymap_new_from_string")]
    public static partial nint KeymapNewFromString(nint context, byte* text, int format, int flags);

    [LibraryImport(Lib, EntryPoint = "xkb_keymap_unref")]
    public static partial void KeymapUnref(nint keymap);

    [LibraryImport(Lib, EntryPoint = "xkb_state_new")]
    public static partial nint StateNew(nint keymap);

    [LibraryImport(Lib, EntryPoint = "xkb_state_unref")]
    public static partial void StateUnref(nint state);

    [LibraryImport(Lib, EntryPoint = "xkb_state_update_key")]
    public static partial void StateUpdateKey(nint state, uint key, int direction);

    [LibraryImport(Lib, EntryPoint = "xkb_state_update_mask")]
    public static partial void StateUpdateMask(nint state, uint depressed, uint latched, uint locked, uint depressedLayout, uint latchedLayout, uint lockedLayout);

    [LibraryImport(Lib, EntryPoint = "xkb_state_key_get_utf8")]
    public static partial int StateKeyGetUtf8(nint state, uint key, byte* buffer, nuint size);

    [LibraryImport(Lib, EntryPoint = "xkb_state_mod_name_is_active")]
    public static partial int StateModNameIsActive(nint state, byte* name, int type);

    [LibraryImport(LibX11, EntryPoint = "xkb_x11_setup_xkb_extension")]
    public static partial int X11Setup(nint connection, ushort major, ushort minor, int flags, ushort* majorOut, ushort* minorOut, byte* baseEvent, byte* baseError);

    [LibraryImport(LibX11, EntryPoint = "xkb_x11_get_core_keyboard_device_id")]
    public static partial int X11CoreKeyboardDeviceId(nint connection);

    [LibraryImport(LibX11, EntryPoint = "xkb_x11_keymap_new_from_device")]
    public static partial nint X11KeymapNewFromDevice(nint context, nint connection, int deviceId, int flags);

    [LibraryImport(LibX11, EntryPoint = "xkb_x11_state_new_from_device")]
    public static partial nint X11StateNewFromDevice(nint keymap, nint connection, int deviceId);
}

/// <summary>One keyboard layout and its modifier state; turns key codes into physical keys and typed text.</summary>
internal sealed unsafe class XkbKeyboard : IDisposable
{
    private const int StateModsEffective = 1 << 3;
    private nint _context;
    private nint _keymap;
    private nint _state;

    private XkbKeyboard(nint context, nint keymap, nint state)
    {
        _context = context;
        _keymap = keymap;
        _state = state;
    }

    /// <summary>The keyboard of the X server behind <paramref name="display"/>; falls back to the default layout when the X11 helper library is missing.</summary>
    public static XkbKeyboard FromX11(nint display)
    {
        var context = XkbNative.ContextNew(0);
        if (context == 0)
        {
            throw new PlatformException("xkbcommon could not create a context.");
        }

        try
        {
            var connection = Xlib.GetXcbConnection(display);
            ushort major, minor;
            byte baseEvent, baseError;
            if (connection != 0 && XkbNative.X11Setup(connection, 1, 0, 0, &major, &minor, &baseEvent, &baseError) != 0)
            {
                var device = XkbNative.X11CoreKeyboardDeviceId(connection);
                var keymap = device >= 0 ? XkbNative.X11KeymapNewFromDevice(context, connection, device, 0) : 0;
                if (keymap != 0)
                {
                    var state = XkbNative.X11StateNewFromDevice(keymap, connection, device);
                    if (state != 0)
                    {
                        return new XkbKeyboard(context, keymap, state);
                    }

                    XkbNative.KeymapUnref(keymap);
                }
            }
        }
        catch (DllNotFoundException)
        {
            // No xkbcommon-x11 on this machine; use the default layout.
        }
        catch (EntryPointNotFoundException)
        {
        }

        return FromDefault(context);
    }

    /// <summary>The system default layout (from XKB_DEFAULT_* variables or the built-in US layout).</summary>
    public static XkbKeyboard FromDefault(nint context = 0)
    {
        context = context != 0 ? context : XkbNative.ContextNew(0);
        var keymap = XkbNative.KeymapNewFromNames(context, null, 0);
        var state = keymap != 0 ? XkbNative.StateNew(keymap) : 0;
        if (state == 0)
        {
            throw new PlatformException("xkbcommon could not build a default keymap.");
        }

        return new XkbKeyboard(context, keymap, state);
    }

    /// <summary>A layout sent by a Wayland compositor as keymap text.</summary>
    public static XkbKeyboard FromText(string text)
    {
        var context = XkbNative.ContextNew(0);
        var bytes = Encoding.UTF8.GetBytes(text + "\0");
        nint keymap;
        fixed (byte* p = bytes)
        {
            keymap = XkbNative.KeymapNewFromString(context, p, 1, 0);
        }

        var state = keymap != 0 ? XkbNative.StateNew(keymap) : 0;
        if (state == 0)
        {
            throw new PlatformException("The compositor sent a keymap xkbcommon could not parse.");
        }

        return new XkbKeyboard(context, keymap, state);
    }

    /// <summary>Translates an xkb key code (evdev code plus 8) to a physical key.</summary>
    public static Key KeyFor(uint xkbKeycode) => KeyTables.FromEvdev((int)xkbKeycode - 8);

    /// <summary>The text this key would type with the current modifiers, or null for keys that type nothing (arrows, Ctrl combinations, Enter).</summary>
    public string? TextFor(uint xkbKeycode)
    {
        Span<byte> buffer = stackalloc byte[16];
        int n;
        fixed (byte* p = buffer)
        {
            n = XkbNative.StateKeyGetUtf8(_state, xkbKeycode, p, (nuint)buffer.Length);
        }

        if (n <= 0 || n >= buffer.Length)
        {
            return null;
        }

        var text = Encoding.UTF8.GetString(buffer[..n]);

        // Control characters (Enter, Tab, Backspace, Escape, Ctrl+letter) are keys, not text.
        return char.IsControl(text[0]) ? null : text;
    }

    /// <summary>Tracks a key press or release so shift and lock states follow.</summary>
    public void UpdateKey(uint xkbKeycode, bool down) => XkbNative.StateUpdateKey(_state, xkbKeycode, down ? 1 : 0);

    /// <summary>Applies the modifier masks a Wayland compositor sends.</summary>
    public void UpdateMask(uint depressed, uint latched, uint locked, uint group) =>
        XkbNative.StateUpdateMask(_state, depressed, latched, locked, 0, 0, group);

    /// <summary>The modifiers currently active.</summary>
    public KeyModifiers Modifiers
    {
        get
        {
            var m = KeyModifiers.None;
            if (Active("Shift"))
            {
                m |= KeyModifiers.Shift;
            }

            if (Active("Control"))
            {
                m |= KeyModifiers.Control;
            }

            if (Active("Mod1"))
            {
                m |= KeyModifiers.Alt;
            }

            if (Active("Mod4"))
            {
                m |= KeyModifiers.Meta;
            }

            if (Active("Lock"))
            {
                m |= KeyModifiers.CapsLock;
            }

            if (Active("Mod2"))
            {
                m |= KeyModifiers.NumLock;
            }

            return m;
        }
    }

    private bool Active(string name)
    {
        Span<byte> bytes = stackalloc byte[16];
        var n = Encoding.ASCII.GetBytes(name, bytes);
        bytes[n] = 0;
        fixed (byte* p = bytes)
        {
            return XkbNative.StateModNameIsActive(_state, p, StateModsEffective) > 0;
        }
    }

    public void Dispose()
    {
        if (_state != 0)
        {
            XkbNative.StateUnref(_state);
            _state = 0;
        }

        if (_keymap != 0)
        {
            XkbNative.KeymapUnref(_keymap);
            _keymap = 0;
        }

        if (_context != 0)
        {
            XkbNative.ContextUnref(_context);
            _context = 0;
        }
    }
}
