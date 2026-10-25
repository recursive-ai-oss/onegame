using System.Runtime.InteropServices;

namespace OneGame.Platform;

/// <summary>Xlib entry points. Only the calls the X11 backend needs. Layouts below assume a 64-bit Linux (long is 8 bytes).</summary>
internal static unsafe partial class Xlib
{
    private const string Lib = "libX11.so.6";

    public const int KeyPress = 2, KeyRelease = 3, ButtonPress = 4, ButtonRelease = 5, MotionNotify = 6, EnterNotify = 7, LeaveNotify = 8,
        FocusIn = 9, FocusOut = 10, DestroyNotify = 17, UnmapNotify = 18, MapNotify = 19, ConfigureNotify = 22, PropertyNotify = 28, ClientMessage = 33;

    public const long KeyPressMask = 1, KeyReleaseMask = 2, ButtonPressMask = 4, ButtonReleaseMask = 8, EnterWindowMask = 16, LeaveWindowMask = 32,
        PointerMotionMask = 64, StructureNotifyMask = 1 << 17, FocusChangeMask = 1 << 21, PropertyChangeMask = 1 << 22,
        SubstructureNotifyMask = 1 << 19, SubstructureRedirectMask = 1 << 20;

    public const int PropModeReplace = 0;
    public const int GrabModeAsync = 1;

    /// <summary>XEvent is a 192-byte union; fields are read at fixed offsets.</summary>
    [StructLayout(LayoutKind.Sequential, Size = 192)]
    public struct XEvent
    {
        public int Type;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XColor
    {
        public nuint Pixel;
        public ushort Red, Green, Blue;
        public byte Flags, Pad;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XSizeHints
    {
        public nint Flags;
        public int X, Y, Width, Height, MinWidth, MinHeight, MaxWidth, MaxHeight, WidthInc, HeightInc;
        public int MinAspectX, MinAspectY, MaxAspectX, MaxAspectY, BaseWidth, BaseHeight, WinGravity;
    }

    // Xlib shares global state between connections (extension tables, locks) that is only protected once XInitThreads has run, and
    // it must run before any other call. The engine uses one connection per thread (a second app, a clipboard owner, the tests), so it is
    // called from the type initializer, which the runtime runs before the first Xlib call of any kind.
    static Xlib() => _ = InitThreads();

    [LibraryImport(Lib, EntryPoint = "XInitThreads")]
    private static partial int InitThreads();

    public static T At<T>(XEvent* e, int offset)
        where T : unmanaged => *(T*)((byte*)e + offset);

    [LibraryImport(Lib, EntryPoint = "XOpenDisplay")]
    public static partial nint OpenDisplay(byte* name);

    [LibraryImport(Lib, EntryPoint = "XCloseDisplay")]
    public static partial void CloseDisplay(nint display);

    [LibraryImport(Lib, EntryPoint = "XDefaultScreen")]
    public static partial int DefaultScreen(nint display);

    [LibraryImport(Lib, EntryPoint = "XRootWindow")]
    public static partial nuint RootWindow(nint display, int screen);

    [LibraryImport(Lib, EntryPoint = "XCreateSimpleWindow")]
    public static partial nuint CreateSimpleWindow(nint display, nuint parent, int x, int y, uint width, uint height, uint borderWidth, nuint border, nuint background);

    [LibraryImport(Lib, EntryPoint = "XSelectInput")]
    public static partial void SelectInput(nint display, nuint window, nint mask);

    [LibraryImport(Lib, EntryPoint = "XMapWindow")]
    public static partial void MapWindow(nint display, nuint window);

    [LibraryImport(Lib, EntryPoint = "XUnmapWindow")]
    public static partial void UnmapWindow(nint display, nuint window);

    [LibraryImport(Lib, EntryPoint = "XDestroyWindow")]
    public static partial void DestroyWindow(nint display, nuint window);

    [LibraryImport(Lib, EntryPoint = "XFlush")]
    public static partial void Flush(nint display);

    [LibraryImport(Lib, EntryPoint = "XSync")]
    public static partial void Sync(nint display, int discard);

    [LibraryImport(Lib, EntryPoint = "XPending")]
    public static partial int Pending(nint display);

    [LibraryImport(Lib, EntryPoint = "XNextEvent")]
    public static partial void NextEvent(nint display, XEvent* e);

    [LibraryImport(Lib, EntryPoint = "XInternAtom")]
    public static partial nuint InternAtom(nint display, byte* name, int onlyIfExists);

    [LibraryImport(Lib, EntryPoint = "XSetWMProtocols")]
    public static partial void SetWMProtocols(nint display, nuint window, nuint* protocols, int count);

    [LibraryImport(Lib, EntryPoint = "XChangeProperty")]
    public static partial void ChangeProperty(nint display, nuint window, nuint property, nuint type, int format, int mode, byte* data, int elements);

    [LibraryImport(Lib, EntryPoint = "XResizeWindow")]
    public static partial void ResizeWindow(nint display, nuint window, uint width, uint height);

    [LibraryImport(Lib, EntryPoint = "XSendEvent")]
    public static partial void SendEvent(nint display, nuint window, int propagate, nint mask, XEvent* e);

    [LibraryImport(Lib, EntryPoint = "XSetInputFocus")]
    public static partial void SetInputFocus(nint display, nuint window, int revertTo, nuint time);

    [LibraryImport(Lib, EntryPoint = "XIconifyWindow")]
    public static partial void IconifyWindow(nint display, nuint window, int screen);

    [LibraryImport(Lib, EntryPoint = "XGrabPointer")]
    public static partial void GrabPointer(nint display, nuint window, int ownerEvents, uint eventMask, int pointerMode, int keyboardMode, nuint confineTo, nuint cursor, nuint time);

    [LibraryImport(Lib, EntryPoint = "XUngrabPointer")]
    public static partial void UngrabPointer(nint display, nuint time);

    [LibraryImport(Lib, EntryPoint = "XWarpPointer")]
    public static partial void WarpPointer(nint display, nuint src, nuint dest, int srcX, int srcY, uint srcWidth, uint srcHeight, int destX, int destY);

    [LibraryImport(Lib, EntryPoint = "XCreatePixmap")]
    public static partial nuint CreatePixmap(nint display, nuint drawable, uint width, uint height, uint depth);

    [LibraryImport(Lib, EntryPoint = "XFreePixmap")]
    public static partial void FreePixmap(nint display, nuint pixmap);

    [LibraryImport(Lib, EntryPoint = "XCreatePixmapCursor")]
    public static partial nuint CreatePixmapCursor(nint display, nuint source, nuint mask, XColor* foreground, XColor* background, uint x, uint y);

    [LibraryImport(Lib, EntryPoint = "XDefineCursor")]
    public static partial void DefineCursor(nint display, nuint window, nuint cursor);

    [LibraryImport(Lib, EntryPoint = "XFreeCursor")]
    public static partial void FreeCursor(nint display, nuint cursor);

    public const int SelectionClear = 29, SelectionRequest = 30, SelectionNotify = 31;
    public const int PropertyNewValue = 0;

    [LibraryImport(Lib, EntryPoint = "XSetSelectionOwner")]
    public static partial void SetSelectionOwner(nint display, nuint selection, nuint owner, nuint time);

    [LibraryImport(Lib, EntryPoint = "XGetSelectionOwner")]
    public static partial nuint GetSelectionOwner(nint display, nuint selection);

    [LibraryImport(Lib, EntryPoint = "XConvertSelection")]
    public static partial void ConvertSelection(nint display, nuint selection, nuint target, nuint property, nuint requestor, nuint time);

    [LibraryImport(Lib, EntryPoint = "XGetWindowProperty")]
    public static partial int GetWindowProperty(nint display, nuint window, nuint property, nint offset, nint length, int delete, nuint requestType, nuint* actualType, int* actualFormat, nuint* itemCount, nuint* bytesAfter, byte** data);

    [LibraryImport(Lib, EntryPoint = "XDeleteProperty")]
    public static partial void DeleteProperty(nint display, nuint window, nuint property);

    [LibraryImport(Lib, EntryPoint = "XFree")]
    public static partial void Free(void* data);

    [LibraryImport(Lib, EntryPoint = "XExtendedMaxRequestSize")]
    public static partial nint ExtendedMaxRequestSize(nint display);

    [LibraryImport(Lib, EntryPoint = "XMaxRequestSize")]
    public static partial nint MaxRequestSize(nint display);

    [LibraryImport(Lib, EntryPoint = "XResourceManagerString")]
    public static partial byte* ResourceManagerString(nint display);

    [LibraryImport(Lib, EntryPoint = "XConnectionNumber")]
    public static partial int ConnectionNumber(nint display);

    [LibraryImport(Lib, EntryPoint = "XkbSetDetectableAutoRepeat")]
    public static partial void XkbSetDetectableAutoRepeat(nint display, int detectable, int* supported);

    [LibraryImport(Lib, EntryPoint = "XSetWMNormalHints")]
    public static partial void SetWMNormalHints(nint display, nuint window, XSizeHints* hints);

    [LibraryImport(Lib, EntryPoint = "XSetErrorHandler")]
    public static partial nint SetErrorHandler(delegate* unmanaged<nint, void*, int> handler);

    [LibraryImport("libX11-xcb.so.1", EntryPoint = "XGetXCBConnection")]
    public static partial nint GetXcbConnection(nint display);
}
