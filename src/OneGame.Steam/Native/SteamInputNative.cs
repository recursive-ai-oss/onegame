using System.Runtime.InteropServices;

namespace OneGame.Steam.Native;

/// <summary>The real Steam Input boundary: forwards each call to the flat API with the interface pointers fetched after Steam started.</summary>
internal sealed unsafe class SteamInputNative(nint input, nint utils) : ISteamInputNative
{
    // The keyboard mode the docs call single line: Enter dismisses it.
    private const int SingleLine = 0;

    public bool Init(string? manifestPath)
    {
        // We drive RunFrame ourselves from Update, so the data is read at a known point of the game loop.
        var ok = SteamApi.InputInit(input, 1) != 0;
        if (ok && manifestPath is not null)
        {
            return SteamApi.InputSetManifestPath(input, manifestPath) != 0;
        }

        return ok;
    }

    public void Shutdown() => SteamApi.InputShutdown(input);

    public void RunFrame() => SteamApi.InputRunFrame(input, 1);

    public int GetConnectedControllers(Span<ulong> handles)
    {
        // The call writes up to the SDK's maximum (16); a larger buffer is never overrun.
        Span<ulong> scratch = stackalloc ulong[64];
        int n;
        fixed (ulong* p = scratch)
        {
            n = SteamApi.InputGetConnectedControllers(input, p);
        }

        n = Math.Clamp(n, 0, Math.Min(scratch.Length, handles.Length));
        scratch[..n].CopyTo(handles);
        return n;
    }

    public int GetInputType(ulong controller) => SteamApi.InputGetInputTypeForHandle(input, controller);

    public ulong GetActionSet(string name) => SteamApi.InputGetActionSetHandle(input, name);

    public ulong GetDigitalAction(string name) => SteamApi.InputGetDigitalActionHandle(input, name);

    public ulong GetAnalogAction(string name) => SteamApi.InputGetAnalogActionHandle(input, name);

    public void ActivateActionSet(ulong controller, ulong set) => SteamApi.InputActivateActionSet(input, controller, set);

    public ulong GetCurrentActionSet(ulong controller) => SteamApi.InputGetCurrentActionSet(input, controller);

    public DigitalReading ReadDigital(ulong controller, ulong action)
    {
        var d = SteamApi.InputGetDigitalActionData(input, controller, action);
        return new DigitalReading(d.State != 0, d.Active != 0);
    }

    public AnalogReading ReadAnalog(ulong controller, ulong action)
    {
        var d = SteamApi.InputGetAnalogActionData(input, controller, action);
        return new AnalogReading(d.X, d.Y, d.Active != 0);
    }

    public int GetDigitalOrigins(ulong controller, ulong set, ulong action, Span<int> origins) => Origins(origins, (p) => SteamApi.InputGetDigitalActionOrigins(input, controller, set, action, p));

    public int GetAnalogOrigins(ulong controller, ulong set, ulong action, Span<int> origins) => Origins(origins, (p) => SteamApi.InputGetAnalogActionOrigins(input, controller, set, action, p));

    public int TranslateOrigin(int destinationInputType, int origin) => SteamApi.InputTranslateActionOrigin(input, destinationInputType, origin);

    public string GlyphPath(int origin, int size, uint flags) => Text(SteamApi.InputGetGlyphPng(input, origin, size, flags));

    public string OriginName(int origin) => Text(SteamApi.InputGetStringForActionOrigin(input, origin));

    public bool ShowKeyboard(int x, int y, int width, int height) => SteamApi.UtilsShowFloatingGamepadTextInput(utils, SingleLine, x, y, width, height) != 0;

    public bool DismissKeyboard() => SteamApi.UtilsDismissFloatingGamepadTextInput(utils) != 0;

    private delegate int OriginFill(int* origins);

    private static int Origins(Span<int> into, OriginFill fill)
    {
        // The SDK documents a maximum of 8 origins per action; leave room for growth.
        Span<int> scratch = stackalloc int[32];
        int n;
        fixed (int* p = scratch)
        {
            n = fill(p);
        }

        n = Math.Clamp(n, 0, Math.Min(scratch.Length, into.Length));
        scratch[..n].CopyTo(into);
        return n;
    }

    private static string Text(byte* utf8) => utf8 == null ? string.Empty : Marshal.PtrToStringUTF8((nint)utf8) ?? string.Empty;
}
