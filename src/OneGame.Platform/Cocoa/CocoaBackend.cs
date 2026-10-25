using System.Runtime.InteropServices;

namespace OneGame.Platform.Cocoa;

/// <summary>
/// The macOS backend: <c>NSApplication</c> and <c>NSWindow</c> built without a nib, driven by a hand-pumped event loop so the game
/// keeps its own frame loop. AppKit classes the engine needs (the content view and the delegates) are created at run time with the
/// Objective-C runtime, whose method implementations are function pointers into this assembly.
/// </summary>
internal sealed unsafe class CocoaBackend : PlatformBackend
{
    // Cocoa delegates and views call back with only a pointer to themselves, so the windows are found by their object pointers.
    // The backend runs on the thread that owns AppKit, so plain dictionaries suffice.
    internal static readonly Dictionary<nint, CocoaWindow> ByView = [];
    internal static readonly Dictionary<nint, CocoaWindow> ByWindow = [];
    internal static CocoaBackend? Current;

    private readonly nint _nsApp;
    private nint _delegate;
    private readonly nint _runLoopMode;
    private readonly nint _pasteboardType;
    private bool _disposed;

    public CocoaBackend(PlatformApp app)
        : base(app)
    {
        if (!ObjC.IsSupported)
        {
            throw new PlatformException("The Cocoa backend needs macOS on Apple silicon.");
        }

        if (Current is not null)
        {
            throw new PlatformException("Only one Cocoa application can exist in a process.");
        }

        // AppKit refuses to create windows or set the menu bar anywhere but the process's main thread, so a game's frame loop runs there.
        if (ObjC.pthread_main_np() == 0)
        {
            throw new PlatformException("The Cocoa backend must be created, and used, on the process's main thread.");
        }

        ObjC.LoadFrameworks();
        using var pool = new Pool();
        _runLoopMode = ObjC.FoundationString("NSDefaultRunLoopMode");
        _pasteboardType = ObjC.FoundationString("NSPasteboardTypeString");
        if (_pasteboardType == 0)
        {
            // The constant lives in AppKit, not Foundation; its value is a fixed uniform type identifier.
            _pasteboardType = ObjC.objc_retain(ObjC.NSString("public.utf8-plain-text"));
        }

        _nsApp = ObjC.Send(ObjC.Class("NSApplication"), "sharedApplication");
        Current = this;
        var delegateClass = CocoaClasses.Delegate;
        _delegate = ObjC.Send(ObjC.Send(delegateClass, "alloc"), "init");
        Msg.Void(_nsApp, ObjC.Sel("setDelegate:"), _delegate);

        // NSApplicationActivationPolicyRegular: a dock icon and a menu bar, so a game started from a terminal gets keyboard focus.
        Msg.Void(_nsApp, ObjC.Sel("setActivationPolicy:"), (nint)0);
        BuildMenu();
        ObjC.Send(_nsApp, "finishLaunching");
        Activate();
    }

    public override PlatformBackendKind Kind => PlatformBackendKind.Cocoa;

    /// <summary>The first exception thrown inside a callback from AppKit; rethrown from <see cref="Pump"/> because it cannot cross the native frame.</summary>
    internal Exception? Failure { get; set; }

    internal PlatformApp Owner => App;

    // Asks to become the frontmost application, which key focus and fullscreen need. The system may decline for a process that was not
    // started from the Finder, the Dock or `open`; the window then simply is not focused.
    private void Activate()
    {
        Msg.Void(_nsApp, ObjC.Sel("activateIgnoringOtherApps:"), (byte)1);
        var me = ObjC.Send(ObjC.Class("NSRunningApplication"), "currentApplication");

        // NSApplicationActivateAllWindows | NSApplicationActivateIgnoringOtherApps.
        Msg.Flag(me, ObjC.Sel("activateWithOptions:"), (nuint)3);
    }

    internal nint NSApp => _nsApp;

    internal nint DelegateObject => _delegate;

    public override Window CreateWindow(WindowOptions options, int id)
    {
        using var pool = new Pool();
        var window = new CocoaWindow(this, App, id, options);
        Activate();
        return window;
    }

    public override void Pump(int timeoutMilliseconds)
    {
        using var pool = new Pool();
        var date = ObjC.Class("NSDate");
        var until = timeoutMilliseconds > 0
            ? Msg.Obj(date, ObjC.Sel("dateWithTimeIntervalSinceNow:"), timeoutMilliseconds / 1000.0)
            : ObjC.Send(date, "distantPast");
        var next = ObjC.Sel("nextEventMatchingMask:untilDate:inMode:dequeue:");
        var send = ObjC.Sel("sendEvent:");
        while (true)
        {
            // NSEventMaskAny is NSUIntegerMax.
            var e = Msg.Obj(_nsApp, next, nuint.MaxValue, until, _runLoopMode, (byte)1);
            if (e == 0)
            {
                break;
            }

            Msg.Void(_nsApp, send, e);
            until = ObjC.Send(date, "distantPast");
        }

        ObjC.Send(_nsApp, "updateWindows");
        if (Failure is { } failure)
        {
            Failure = null;
            throw new PlatformException("A Cocoa callback failed: " + failure.Message, failure);
        }
    }

    public override bool ClipboardAvailable => true;

    public override string? ClipboardGet(ClipboardSelection selection)
    {
        using var pool = new Pool();
        var board = ObjC.Send(ObjC.Class("NSPasteboard"), "generalPasteboard");
        var s = Msg.Obj(board, ObjC.Sel("stringForType:"), _pasteboardType);
        if (s == 0)
        {
            return null;
        }

        // NSUTF8StringEncoding is 4. The size is checked before the text is copied into managed memory.
        var bytes = Msg.UL(s, ObjC.Sel("lengthOfBytesUsingEncoding:"), (nuint)4);
        return bytes > PlatformClipboard.MaxTextBytes ? null : ObjC.ToManaged(s);
    }

    public override bool ClipboardSet(ClipboardSelection selection, string text)
    {
        using var pool = new Pool();
        var board = ObjC.Send(ObjC.Class("NSPasteboard"), "generalPasteboard");
        Msg.UL(board, ObjC.Sel("clearContents"));
        return Msg.Flag(board, ObjC.Sel("setString:forType:"), ObjC.NSString(text), _pasteboardType) != 0;
    }

    public override void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        using var pool = new Pool();
        Msg.Void(_nsApp, ObjC.Sel("setDelegate:"), (nint)0);
        ObjC.Release(ref _delegate);
        Current = null;
        base.Dispose();
    }

    private void BuildMenu()
    {
        // The menu bar has one menu, named by the system after the application. Copy and Paste are left out on purpose: a menu key
        // equivalent would swallow Command+C and Command+V before the game's text field sees them.
        var menuClass = ObjC.Class("NSMenu");
        var itemClass = ObjC.Class("NSMenuItem");
        var bar = ObjC.Send(ObjC.Send(menuClass, "alloc"), "init");
        var appItem = ObjC.Send(ObjC.Send(itemClass, "alloc"), "init");
        Msg.Void(bar, ObjC.Sel("addItem:"), appItem);
        var appMenu = ObjC.Send(ObjC.Send(menuClass, "alloc"), "init");

        var name = ObjC.ToManaged(ObjC.Send(ObjC.Send(ObjC.Class("NSProcessInfo"), "processInfo"), "processName"));
        AddItem(appMenu, "Hide " + name, "hide:", "h", 0, target: 0);
        AddItem(appMenu, "Enter Full Screen", "toggleFullScreen:", "f", (1UL << 18) | (1UL << 20), target: 0);
        Msg.Void(appMenu, ObjC.Sel("addItem:"), ObjC.Send(itemClass, "separatorItem"));
        AddItem(appMenu, "Quit " + name, "onegameQuit:", "q", 0, target: _delegate);

        Msg.Void(appItem, ObjC.Sel("setSubmenu:"), appMenu);
        Msg.Void(_nsApp, ObjC.Sel("setMainMenu:"), bar);
    }

    private static void AddItem(nint menu, string title, string action, string key, ulong modifiers, nint target)
    {
        var item = Msg.Obj(ObjC.Send(ObjC.Class("NSMenuItem"), "alloc"), ObjC.Sel("initWithTitle:action:keyEquivalent:"), ObjC.NSString(title), ObjC.Sel(action), ObjC.NSString(key));
        if (target != 0)
        {
            Msg.Void(item, ObjC.Sel("setTarget:"), target);
        }

        if (modifiers != 0)
        {
            Msg.Void(item, ObjC.Sel("setKeyEquivalentModifierMask:"), (nuint)modifiers);
        }

        Msg.Void(menu, ObjC.Sel("addItem:"), item);
    }

    /// <summary>Remembers a failure from a callback so the next pump can throw it.</summary>
    internal static void Fail(Exception e) => Current?.Failure ??= e;
}

/// <summary>The Objective-C classes created at run time: the content view and the delegate for the application and its windows.</summary>
internal static unsafe class CocoaClasses
{
    private static nint s_view;
    private static nint s_delegate;

    public static nint View => s_view != 0 ? s_view : s_view = CreateView();

    public static nint Delegate => s_delegate != 0 ? s_delegate : s_delegate = CreateDelegate();

    private static nint CreateView()
    {
        ReadOnlySpan<(string, nint, string)> methods =
        [
            ("acceptsFirstResponder", (nint)(delegate* unmanaged<nint, nint, byte>)&Yes, "B@:"),
            ("acceptsFirstMouse:", (nint)(delegate* unmanaged<nint, nint, nint, byte>)&YesWithArg, "B@:@"),
            ("isFlipped", (nint)(delegate* unmanaged<nint, nint, byte>)&Yes, "B@:"),
            ("wantsUpdateLayer", (nint)(delegate* unmanaged<nint, nint, byte>)&Yes, "B@:"),
            ("makeBackingLayer", (nint)(delegate* unmanaged<nint, nint, nint>)&MakeBackingLayer, "@@:"),
            ("keyDown:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&KeyDown, "v@:@"),
            ("keyUp:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&KeyUp, "v@:@"),
            ("flagsChanged:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&FlagsChanged, "v@:@"),
            ("mouseDown:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&ButtonDown, "v@:@"),
            ("rightMouseDown:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&ButtonDown, "v@:@"),
            ("otherMouseDown:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&ButtonDown, "v@:@"),
            ("mouseUp:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&ButtonUp, "v@:@"),
            ("rightMouseUp:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&ButtonUp, "v@:@"),
            ("otherMouseUp:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&ButtonUp, "v@:@"),
            ("mouseMoved:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&Moved, "v@:@"),
            ("mouseDragged:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&Moved, "v@:@"),
            ("rightMouseDragged:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&Moved, "v@:@"),
            ("otherMouseDragged:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&Moved, "v@:@"),
            ("mouseEntered:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&Entered, "v@:@"),
            ("mouseExited:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&Exited, "v@:@"),
            ("scrollWheel:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&Scroll, "v@:@"),

            // NSTextInputClient: how the system's input methods hand over typed and composed text.
            ("insertText:replacementRange:", (nint)(delegate* unmanaged<nint, nint, nint, nuint, nuint, void>)&InsertText, "v@:@{_NSRange=QQ}"),
            ("setMarkedText:selectedRange:replacementRange:", (nint)(delegate* unmanaged<nint, nint, nint, nuint, nuint, nuint, nuint, void>)&SetMarkedText, "v@:@{_NSRange=QQ}{_NSRange=QQ}"),
            ("unmarkText", (nint)(delegate* unmanaged<nint, nint, void>)&UnmarkText, "v@:"),
            ("hasMarkedText", (nint)(delegate* unmanaged<nint, nint, byte>)&HasMarkedText, "B@:"),
            ("selectedRange", (nint)(delegate* unmanaged<nint, nint, NSRange>)&SelectedRange, "{_NSRange=QQ}@:"),
            ("markedRange", (nint)(delegate* unmanaged<nint, nint, NSRange>)&MarkedRange, "{_NSRange=QQ}@:"),
            ("attributedSubstringForProposedRange:actualRange:", (nint)(delegate* unmanaged<nint, nint, nuint, nuint, nint, nint>)&NoSubstring, "@@:{_NSRange=QQ}^{_NSRange=QQ}"),
            ("validAttributesForMarkedText", (nint)(delegate* unmanaged<nint, nint, nint>)&NoAttributes, "@@:"),
            ("firstRectForCharacterRange:actualRange:", (nint)(delegate* unmanaged<nint, nint, nuint, nuint, nint, CGRect>)&CaretRect, "{CGRect={CGPoint=dd}{CGSize=dd}}@:{_NSRange=QQ}^{_NSRange=QQ}"),
            ("characterIndexForPoint:", (nint)(delegate* unmanaged<nint, nint, CGPoint, nuint>)&IndexForPoint, "Q@:{CGPoint=dd}"),
            ("doCommandBySelector:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&DoCommand, "v@::"),
        ];
        return ObjC.DefineClass("OneGameView", "NSView", methods, "NSTextInputClient");
    }

    private static nint CreateDelegate()
    {
        ReadOnlySpan<(string, nint, string)> methods =
        [
            ("applicationShouldTerminate:", (nint)(delegate* unmanaged<nint, nint, nint, nuint>)&ShouldTerminate, "Q@:@"),
            ("applicationShouldTerminateAfterLastWindowClosed:", (nint)(delegate* unmanaged<nint, nint, nint, byte>)&NoWithArg, "B@:@"),
            ("onegameQuit:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&QuitItem, "v@:@"),
            ("windowShouldClose:", (nint)(delegate* unmanaged<nint, nint, nint, byte>)&WindowShouldClose, "B@:@"),
            ("windowDidResize:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&WindowDidResize, "v@:@"),
            ("windowDidChangeBackingProperties:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&WindowDidResize, "v@:@"),
            ("windowDidChangeScreen:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&WindowDidResize, "v@:@"),
            ("windowDidBecomeKey:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&WindowDidBecomeKey, "v@:@"),
            ("windowDidResignKey:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&WindowDidResignKey, "v@:@"),
            ("windowDidMiniaturize:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&WindowDidMiniaturize, "v@:@"),
            ("windowDidDeminiaturize:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&WindowDidDeminiaturize, "v@:@"),
            ("windowDidEnterFullScreen:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&WindowDidEnterFullScreen, "v@:@"),
            ("windowDidExitFullScreen:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&WindowDidExitFullScreen, "v@:@"),
        ];
        return ObjC.DefineClass("OneGameDelegate", "NSObject", methods, "NSApplicationDelegate", "NSWindowDelegate");
    }

    private static CocoaWindow? OfView(nint view) => CocoaBackend.ByView.GetValueOrDefault(view);

    // The window an NSNotification is about is its object.
    private static CocoaWindow? OfNotification(nint note) => CocoaBackend.ByWindow.GetValueOrDefault(ObjC.Send(note, "object"));

    // ----- view: layer, keyboard, pointer -----

    [UnmanagedCallersOnly]
    private static byte Yes(nint self, nint cmd) => 1;

    [UnmanagedCallersOnly]
    private static byte YesWithArg(nint self, nint cmd, nint arg) => 1;

    [UnmanagedCallersOnly]
    private static byte NoWithArg(nint self, nint cmd, nint arg) => 0;

    [UnmanagedCallersOnly]
    private static nint MakeBackingLayer(nint self, nint cmd) => ObjC.Send(ObjC.Class("CAMetalLayer"), "layer");

    [UnmanagedCallersOnly]
    private static void KeyDown(nint self, nint cmd, nint e) => Run(() => OfView(self)?.KeyEvent(e, down: true));

    [UnmanagedCallersOnly]
    private static void KeyUp(nint self, nint cmd, nint e) => Run(() => OfView(self)?.KeyEvent(e, down: false));

    [UnmanagedCallersOnly]
    private static void FlagsChanged(nint self, nint cmd, nint e) => Run(() => OfView(self)?.FlagsEvent(e));

    [UnmanagedCallersOnly]
    private static void ButtonDown(nint self, nint cmd, nint e) => Run(() => OfView(self)?.ButtonEvent(e, down: true));

    [UnmanagedCallersOnly]
    private static void ButtonUp(nint self, nint cmd, nint e) => Run(() => OfView(self)?.ButtonEvent(e, down: false));

    [UnmanagedCallersOnly]
    private static void Moved(nint self, nint cmd, nint e) => Run(() => OfView(self)?.MoveEvent(e));

    [UnmanagedCallersOnly]
    private static void Entered(nint self, nint cmd, nint e) => Run(() => OfView(self)?.Inside(true));

    [UnmanagedCallersOnly]
    private static void Exited(nint self, nint cmd, nint e) => Run(() => OfView(self)?.Inside(false));

    [UnmanagedCallersOnly]
    private static void Scroll(nint self, nint cmd, nint e) => Run(() => OfView(self)?.ScrollEvent(e));

    // ----- view: text input -----

    [UnmanagedCallersOnly]
    private static void InsertText(nint self, nint cmd, nint text, nuint location, nuint length) => Run(() => OfView(self)?.Insert(ObjC.TextOf(text)));

    [UnmanagedCallersOnly]
    private static void SetMarkedText(nint self, nint cmd, nint text, nuint selLocation, nuint selLength, nuint replLocation, nuint replLength) =>
        Run(() => OfView(self)?.Mark(ObjC.TextOf(text), selLocation, selLength));

    [UnmanagedCallersOnly]
    private static void UnmarkText(nint self, nint cmd) => Run(() => OfView(self)?.Unmark());

    [UnmanagedCallersOnly]
    private static byte HasMarkedText(nint self, nint cmd) => OfView(self)?.HasMarked == true ? (byte)1 : (byte)0;

    [UnmanagedCallersOnly]
    private static NSRange SelectedRange(nint self, nint cmd) => OfView(self)?.SelectedRange ?? default;

    [UnmanagedCallersOnly]
    private static NSRange MarkedRange(nint self, nint cmd) => OfView(self)?.MarkedRange ?? new NSRange { Location = NSRange.NotFound };

    [UnmanagedCallersOnly]
    private static nint NoSubstring(nint self, nint cmd, nuint location, nuint length, nint actual) => 0;

    [UnmanagedCallersOnly]
    private static nint NoAttributes(nint self, nint cmd) => ObjC.Send(ObjC.Class("NSArray"), "array");

    [UnmanagedCallersOnly]
    private static CGRect CaretRect(nint self, nint cmd, nuint location, nuint length, nint actual) => OfView(self)?.CaretScreenRect() ?? default;

    [UnmanagedCallersOnly]
    private static nuint IndexForPoint(nint self, nint cmd, CGPoint point) => NSRange.NotFound;

    [UnmanagedCallersOnly]
    private static void DoCommand(nint self, nint cmd, nint selector)
    {
        // Editing commands (arrows, delete, return) reach the game as key events; nothing here needs the text system's reading of them.
    }

    // ----- delegate: application and window -----

    [UnmanagedCallersOnly]
    private static nuint ShouldTerminate(nint self, nint cmd, nint sender)
    {
        // The game decides when to leave; the system's quit becomes a QuitRequested event. NSTerminateCancel is 0.
        Run(() => CocoaBackend.Current?.Owner.RequestQuit());
        return 0;
    }

    [UnmanagedCallersOnly]
    private static void QuitItem(nint self, nint cmd, nint sender) => Run(() => CocoaBackend.Current?.Owner.RequestQuit());

    [UnmanagedCallersOnly]
    private static byte WindowShouldClose(nint self, nint cmd, nint window)
    {
        // The game closes the window itself when it has handled the request.
        Run(() => CocoaBackend.ByWindow.GetValueOrDefault(window)?.CloseRequested());
        return 0;
    }

    [UnmanagedCallersOnly]
    private static void WindowDidResize(nint self, nint cmd, nint note) => Run(() => OfNotification(note)?.Refresh());

    [UnmanagedCallersOnly]
    private static void WindowDidBecomeKey(nint self, nint cmd, nint note) => Run(() => OfNotification(note)?.KeyChanged(true));

    [UnmanagedCallersOnly]
    private static void WindowDidResignKey(nint self, nint cmd, nint note) => Run(() => OfNotification(note)?.KeyChanged(false));

    [UnmanagedCallersOnly]
    private static void WindowDidMiniaturize(nint self, nint cmd, nint note) => Run(() => OfNotification(note)?.Miniaturized(true));

    [UnmanagedCallersOnly]
    private static void WindowDidDeminiaturize(nint self, nint cmd, nint note) => Run(() => OfNotification(note)?.Miniaturized(false));

    [UnmanagedCallersOnly]
    private static void WindowDidEnterFullScreen(nint self, nint cmd, nint note) => Run(() => OfNotification(note)?.FullscreenChanged(true));

    [UnmanagedCallersOnly]
    private static void WindowDidExitFullScreen(nint self, nint cmd, nint note) => Run(() => OfNotification(note)?.FullscreenChanged(false));

    // An exception cannot unwind through the Objective-C frames above, so it is parked for the next pump to throw.
    private static void Run(Action action)
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            CocoaBackend.Fail(e);
        }
    }
}
