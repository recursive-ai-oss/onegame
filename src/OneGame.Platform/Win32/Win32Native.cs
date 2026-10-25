using System.Runtime.InteropServices;

namespace OneGame.Platform;

/// <summary>user32, kernel32 and shcore entry points the Win32 backend uses. Strings are UTF-16, as the wide-character forms take.</summary>
internal static unsafe partial class Win32Native
{
    public const uint WsOverlappedWindow = 0x00CF0000, WsPopup = 0x80000000, WsVisible = 0x10000000, WsThickFrame = 0x00040000, WsMaximizeBox = 0x00010000;
    public const int SwShow = 5, SwMinimize = 6;
    public const uint SwpNoZOrder = 0x0004, SwpNoActivate = 0x0010, SwpFrameChanged = 0x0020, SwpNoMove = 0x0002, SwpNoSize = 0x0001;
    public const uint PmRemove = 1, QsAllInput = 0x04FF;
    public const int GwlStyle = -16, GwlpUserData = -21;
    public const nint DpiAwarenessPerMonitorV2 = -4;
    public const uint TmeLeave = 2, RidInput = 0x10000003, RidevRemove = 1;
    public const uint WmInput = 0x00FF, WmSetCursor = 0x0020;
    public const nint HtClient = 1;

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Point
    {
        public int X, Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Msg
    {
        public nint Hwnd;
        public uint Message;
        public nuint WParam;
        public nint LParam;
        public uint Time;
        public Point Pt;
        public uint Private;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WndClassEx
    {
        public uint Size;
        public uint Style;
        public delegate* unmanaged<nint, uint, nuint, nint, nint> WndProc;
        public int ClsExtra, WndExtra;
        public nint Instance, Icon, Cursor, Background, MenuName, ClassName, IconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TrackMouseEvent
    {
        public uint Size, Flags;
        public nint Hwnd;
        public uint HoverTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RawInputDevice
    {
        public ushort UsagePage, Usage;
        public uint Flags;
        public nint Target;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MonitorInfo
    {
        public uint Size;
        public Rect Monitor, Work;
        public uint Flags;
    }

    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW")]
    public static partial nint GetModuleHandle(nint name);

    [LibraryImport("user32.dll", EntryPoint = "RegisterClassExW", SetLastError = true)]
    public static partial ushort RegisterClassEx(WndClassEx* wc);

    [LibraryImport("user32.dll", EntryPoint = "CreateWindowExW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint CreateWindowEx(uint exStyle, string className, string title, uint style, int x, int y, int width, int height, nint parent, nint menu, nint instance, nint param);

    [LibraryImport("user32.dll", EntryPoint = "DefWindowProcW")]
    public static partial nint DefWindowProc(nint hwnd, uint message, nuint wParam, nint lParam);

    [LibraryImport("user32.dll", EntryPoint = "DestroyWindow")]
    public static partial void DestroyWindow(nint hwnd);

    [LibraryImport("user32.dll", EntryPoint = "ShowWindow")]
    public static partial void ShowWindow(nint hwnd, int command);

    [LibraryImport("user32.dll", EntryPoint = "PeekMessageW")]
    public static partial int PeekMessage(Msg* msg, nint hwnd, uint min, uint max, uint remove);

    [LibraryImport("user32.dll", EntryPoint = "TranslateMessage")]
    public static partial void TranslateMessage(Msg* msg);

    [LibraryImport("user32.dll", EntryPoint = "DispatchMessageW")]
    public static partial void DispatchMessage(Msg* msg);

    [LibraryImport("user32.dll", EntryPoint = "MsgWaitForMultipleObjects")]
    public static partial void MsgWaitForMultipleObjects(uint count, nint* handles, int waitAll, uint milliseconds, uint wakeMask);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowTextW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial void SetWindowText(nint hwnd, string text);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowPos")]
    public static partial void SetWindowPos(nint hwnd, nint after, int x, int y, int width, int height, uint flags);

    [LibraryImport("user32.dll", EntryPoint = "AdjustWindowRectEx")]
    public static partial void AdjustWindowRectEx(Rect* rect, uint style, int menu, uint exStyle);

    [LibraryImport("user32.dll", EntryPoint = "GetClientRect")]
    public static partial void GetClientRect(nint hwnd, Rect* rect);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowRect")]
    public static partial void GetWindowRect(nint hwnd, Rect* rect);

    [LibraryImport("user32.dll", EntryPoint = "ClientToScreen")]
    public static partial void ClientToScreen(nint hwnd, Point* point);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    public static partial nint GetWindowLongPtr(nint hwnd, int index);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    public static partial void SetWindowLongPtr(nint hwnd, int index, nint value);

    [LibraryImport("user32.dll", EntryPoint = "GetDpiForWindow")]
    public static partial uint GetDpiForWindow(nint hwnd);

    [LibraryImport("user32.dll", EntryPoint = "SetProcessDpiAwarenessContext")]
    public static partial void SetProcessDpiAwarenessContext(nint context);

    [LibraryImport("user32.dll", EntryPoint = "SetCursor")]
    public static partial void SetCursor(nint cursor);

    [LibraryImport("user32.dll", EntryPoint = "LoadCursorW")]
    public static partial nint LoadCursor(nint instance, nint name);

    [LibraryImport("user32.dll", EntryPoint = "ClipCursor")]
    public static partial void ClipCursor(Rect* rect);

    [LibraryImport("user32.dll", EntryPoint = "SetCursorPos")]
    public static partial void SetCursorPos(int x, int y);

    [LibraryImport("user32.dll", EntryPoint = "TrackMouseEvent")]
    public static partial void TrackMouse(TrackMouseEvent* e);

    [LibraryImport("user32.dll", EntryPoint = "GetKeyState")]
    public static partial short GetKeyState(int virtualKey);

    [LibraryImport("user32.dll", EntryPoint = "RegisterRawInputDevices")]
    public static partial void RegisterRawInputDevices(RawInputDevice* devices, uint count, uint size);

    [LibraryImport("user32.dll", EntryPoint = "GetRawInputData")]
    public static partial uint GetRawInputData(nint rawInput, uint command, void* data, uint* size, uint headerSize);

    [LibraryImport("user32.dll", EntryPoint = "MonitorFromWindow")]
    public static partial nint MonitorFromWindow(nint hwnd, uint flags);

    [LibraryImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
    public static partial void GetMonitorInfo(nint monitor, MonitorInfo* info);

    [LibraryImport("user32.dll", EntryPoint = "SetFocus")]
    public static partial void SetFocus(nint hwnd);

    public const uint CfUnicodeText = 13, GmemMoveable = 0x0002;

    [LibraryImport("user32.dll", EntryPoint = "OpenClipboard")]
    public static partial int OpenClipboard(nint owner);

    [LibraryImport("user32.dll", EntryPoint = "CloseClipboard")]
    public static partial int CloseClipboard();

    [LibraryImport("user32.dll", EntryPoint = "EmptyClipboard")]
    public static partial int EmptyClipboard();

    [LibraryImport("user32.dll", EntryPoint = "IsClipboardFormatAvailable")]
    public static partial int IsClipboardFormatAvailable(uint format);

    [LibraryImport("user32.dll", EntryPoint = "GetClipboardData")]
    public static partial nint GetClipboardData(uint format);

    [LibraryImport("user32.dll", EntryPoint = "SetClipboardData")]
    public static partial nint SetClipboardData(uint format, nint memory);

    [LibraryImport("kernel32.dll", EntryPoint = "GlobalAlloc")]
    public static partial nint GlobalAlloc(uint flags, nuint bytes);

    [LibraryImport("kernel32.dll", EntryPoint = "GlobalFree")]
    public static partial nint GlobalFree(nint memory);

    [LibraryImport("kernel32.dll", EntryPoint = "GlobalLock")]
    public static partial void* GlobalLock(nint memory);

    [LibraryImport("kernel32.dll", EntryPoint = "GlobalUnlock")]
    public static partial int GlobalUnlock(nint memory);

    [LibraryImport("kernel32.dll", EntryPoint = "GlobalSize")]
    public static partial nuint GlobalSize(nint memory);
}
