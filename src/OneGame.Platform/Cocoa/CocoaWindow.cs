namespace OneGame.Platform.Cocoa;

/// <summary>An <c>NSWindow</c> whose content view owns a <c>CAMetalLayer</c> for the GPU layer to present to.</summary>
internal sealed unsafe class CocoaWindow : Window
{
    // Trackpads and smooth-scroll mice report points, wheels report lines; a notch is one wheel line or this many points.
    private const double PointsPerNotch = 20;

    // NSWindowStyleMask: titled, closable, miniaturizable, resizable.
    private const nuint StyleTitled = 1, StyleClosable = 2, StyleMiniaturizable = 4, StyleResizable = 8;

    private const nuint CollectionFullScreenPrimary = 1 << 7;

    private readonly CocoaBackend _backend;
    private nint _window;
    private nint _view;
    private nint _layer;
    private KeyModifiers _eventModifiers;
    private bool _inside = true;
    private bool _cursorHideApplied;
    private bool _marked;
    private int _markedLength;
    private nuint _selectionStart, _selectionLength;

    public CocoaWindow(CocoaBackend backend, PlatformApp app, int id, WindowOptions options)
        : base(app, id)
    {
        _backend = backend;
        var mainScale = Msg.Dbl(ObjC.Send(ObjC.Class("NSScreen"), "mainScreen"), ObjC.Sel("backingScaleFactor"));
        if (mainScale < 1)
        {
            mainScale = 1;
        }

        // Window sizes are pixels everywhere else in the platform layer; AppKit sizes windows in points.
        var rect = new CGRect { Size = new CGSize { Width = options.Width / mainScale, Height = options.Height / mainScale } };
        var style = StyleTitled | StyleClosable | StyleMiniaturizable | (options.Resizable ? StyleResizable : 0);

        // NSBackingStoreBuffered is 2.
        _window = Msg.Obj(ObjC.Send(ObjC.Class("NSWindow"), "alloc"), ObjC.Sel("initWithContentRect:styleMask:backing:defer:"), rect, style, (nuint)2, (byte)0);
        if (_window == 0)
        {
            throw new PlatformException("NSWindow could not be created.");
        }

        Msg.Void(_window, ObjC.Sel("setReleasedWhenClosed:"), (byte)0);
        _view = Msg.Obj(ObjC.Send(CocoaClasses.View, "alloc"), ObjC.Sel("initWithFrame:"), rect);

        // A layer-backed view asks makeBackingLayer for its layer, which is a CAMetalLayer; AppKit keeps its size and scale current.
        Msg.Void(_view, ObjC.Sel("setWantsLayer:"), (byte)1);
        Msg.Void(_view, ObjC.Sel("setLayerContentsRedrawPolicy:"), (nint)0);
        _layer = ObjC.objc_retain(ObjC.Send(_view, "layer"));

        // Enter and leave need a tracking area; mouseMoved needs the window to ask for it.
        // Options: mouseEnteredAndExited | mouseMoved | activeAlways | inVisibleRect.
        var area = Msg.Obj(ObjC.Send(ObjC.Class("NSTrackingArea"), "alloc"), ObjC.Sel("initWithRect:options:owner:userInfo:"), default(CGRect), (nuint)0x283, _view, (nint)0);
        Msg.Void(_view, ObjC.Sel("addTrackingArea:"), area);
        ObjC.objc_release(area);

        Msg.Void(_window, ObjC.Sel("setContentView:"), _view);
        Msg.Void(_window, ObjC.Sel("setDelegate:"), backend.DelegateObject);
        Msg.Void(_window, ObjC.Sel("setAcceptsMouseMovedEvents:"), (byte)1);
        Msg.Void(_window, ObjC.Sel("setCollectionBehavior:"), CollectionFullScreenPrimary);
        SetTitle(options.Title);
        ObjC.Send(_window, "center");

        CocoaBackend.ByView[_view] = this;
        CocoaBackend.ByWindow[_window] = this;
        ReadGeometry(silent: true);

        Msg.Void(_window, ObjC.Sel("makeFirstResponder:"), _view);
        Msg.Void(_window, ObjC.Sel("makeKeyAndOrderFront:"), (nint)0);
        IsFocused = Msg.Flag(_window, ObjC.Sel("isKeyWindow")) != 0;
        if (options.Fullscreen)
        {
            SetFullscreen(true);
        }
    }

    /// <summary>The <c>NSWindow</c>. Tests use it to inject events the way the system would.</summary>
    internal nint WindowObject => _window;

    /// <summary>The content view that receives key, mouse and text input events.</summary>
    internal nint ViewObject => _view;

    /// <inheritdoc />
    public override NativeWindowHandle NativeHandle => new(NativeWindowKind.Cocoa, _window, _layer);

    internal bool HasMarked => _marked;

    internal NSRange SelectedRange => _marked ? new NSRange { Location = _selectionStart, Length = _selectionLength } : default;

    internal NSRange MarkedRange => _marked ? new NSRange { Location = 0, Length = (nuint)_markedLength } : new NSRange { Location = NSRange.NotFound };

    /// <inheritdoc />
    public override void SetTitle(string title)
    {
        using var pool = new Pool();
        Title = title;
        Msg.Void(_window, ObjC.Sel("setTitle:"), ObjC.NSString(title));
    }

    /// <inheritdoc />
    public override void RequestSize(int width, int height)
    {
        if (IsFullscreen || width <= 0 || height <= 0)
        {
            return;
        }

        var scale = Scale > 0 ? Scale : 1;
        Msg.Void(_window, ObjC.Sel("setContentSize:"), new CGSize { Width = width / scale, Height = height / scale });
        Refresh();
    }

    /// <inheritdoc />
    public override void SetFullscreen(bool fullscreen)
    {
        // The change is animated; IsFullscreen follows when AppKit says it is done.
        if (fullscreen != IsFullscreen)
        {
            Msg.Void(_window, ObjC.Sel("toggleFullScreen:"), (nint)0);
        }
    }

    /// <inheritdoc />
    public override void Minimize() => Msg.Void(_window, ObjC.Sel("miniaturize:"), (nint)0);

    /// <inheritdoc />
    public override void SetCursorHidden(bool hidden)
    {
        CursorHidden = hidden;
        ApplyCursor();
    }

    /// <inheritdoc />
    public override void SetCursorLocked(bool locked)
    {
        if (locked == CursorLocked)
        {
            return;
        }

        // Detaching the pointer from the cursor leaves the cursor still and keeps the mouse deltas coming.
        if (ObjC.CGAssociateMouseAndMouseCursorPosition(!locked) != 0)
        {
            return;
        }

        CursorLocked = locked;
        ApplyCursor();
    }

    private protected override void ApplyTextInput()
    {
        var context = ObjC.Send(_view, "inputContext");
        if (!TextInputActive)
        {
            if (_marked)
            {
                _marked = false;
                _markedLength = 0;
                Post(new PlatformEvent { Kind = PlatformEventKind.TextComposition, WindowId = Id, Text = "", CursorStart = -1, CursorEnd = -1 });
            }

            if (context != 0)
            {
                ObjC.Send(context, "discardMarkedText");
            }
        }
        else if (context != 0)
        {
            ObjC.Send(context, "invalidateCharacterCoordinates");
        }
    }

    private protected override void DestroyNative()
    {
        if (CursorLocked)
        {
            ObjC.CGAssociateMouseAndMouseCursorPosition(true);
            CursorLocked = false;
        }

        CursorHidden = false;
        ApplyCursor();
        CocoaBackend.ByView.Remove(_view);
        CocoaBackend.ByWindow.Remove(_window);
        using var pool = new Pool();
        Msg.Void(_window, ObjC.Sel("setDelegate:"), (nint)0);
        ObjC.Send(_window, "close");
        ObjC.Release(ref _layer);
        ObjC.Release(ref _view);
        ObjC.Release(ref _window);
    }

    // ----- called from the Objective-C side -----

    /// <summary>Reads the window's size and scale from AppKit and reports changes.</summary>
    internal void Refresh() => ReadGeometry(silent: false);

    internal void CloseRequested() => Post(new PlatformEvent { Kind = PlatformEventKind.WindowCloseRequested, WindowId = Id });

    internal void KeyChanged(bool focused) => ApplyFocus(focused);

    internal void Miniaturized(bool minimized) => IsMinimized = minimized;

    internal void FullscreenChanged(bool fullscreen)
    {
        IsFullscreen = fullscreen;
        Refresh();
    }

    internal void KeyEvent(nint e, bool down)
    {
        var key = CocoaKeys.FromVirtualKey(Msg.Ret<ushort>(e, ObjC.Sel("keyCode")));
        var mods = CocoaKeys.ModifiersFromFlags(Msg.UL(e, ObjC.Sel("modifierFlags")));
        _eventModifiers = mods;
        try
        {
            if (!down)
            {
                if (key != Key.Unknown)
                {
                    Post(new PlatformEvent { Kind = PlatformEventKind.KeyUp, WindowId = Id, Key = key, Modifiers = mods });
                }

                return;
            }

            // While an input method is composing, its keys (Enter to confirm, arrows to pick) belong to it.
            if (key != Key.Unknown && !_marked)
            {
                Post(new PlatformEvent
                {
                    Kind = PlatformEventKind.KeyDown,
                    WindowId = Id,
                    Key = key,
                    Modifiers = mods,
                    IsRepeat = Msg.Flag(e, ObjC.Sel("isARepeat")) != 0,
                });
            }

            if ((mods & (KeyModifiers.Control | KeyModifiers.Meta)) == 0)
            {
                if (TextInputActive)
                {
                    // Dead keys and input methods: the text system turns the key into insertText / setMarkedText calls.
                    var keys = Msg.Obj(ObjC.Class("NSArray"), ObjC.Sel("arrayWithObject:"), e);
                    Msg.Void(_view, ObjC.Sel("interpretKeyEvents:"), keys);
                }
                else if (CocoaKeys.TypedText(ObjC.ToManaged(ObjC.Send(e, "characters")), mods) is { } text)
                {
                    Post(new PlatformEvent { Kind = PlatformEventKind.TextInput, WindowId = Id, Text = text, Modifiers = mods });
                }
            }

            if ((mods & KeyModifiers.Meta) != 0 && key != Key.Unknown && !CocoaKeys.IsModifier(key))
            {
                // AppKit sends no key-up for a key pressed while Command is held, so a Command chord is a tap.
                Post(new PlatformEvent { Kind = PlatformEventKind.KeyUp, WindowId = Id, Key = key, Modifiers = mods });
            }
        }
        finally
        {
            _eventModifiers = KeyModifiers.None;
        }
    }

    internal void FlagsEvent(nint e)
    {
        var key = CocoaKeys.FromVirtualKey(Msg.Ret<ushort>(e, ObjC.Sel("keyCode")));
        if (!CocoaKeys.IsModifier(key))
        {
            return;
        }

        var flags = Msg.UL(e, ObjC.Sel("modifierFlags"));
        var down = CocoaKeys.IsDown(key, flags);
        if (down == App.Input.IsKeyDown(key))
        {
            return;
        }

        Post(new PlatformEvent
        {
            Kind = down ? PlatformEventKind.KeyDown : PlatformEventKind.KeyUp,
            WindowId = Id,
            Key = key,
            Modifiers = CocoaKeys.ModifiersFromFlags(flags),
        });
    }

    internal void ButtonEvent(nint e, bool down)
    {
        // The event type names the left and right buttons; any other button is an "other" event that carries its number.
        var button = (long)Msg.UL(e, ObjC.Sel("type")) switch
        {
            1 or 2 => MouseButton.Left,
            3 or 4 => MouseButton.Right,
            _ => (long)Msg.UL(e, ObjC.Sel("buttonNumber")) switch
            {
                2 => MouseButton.Middle,
                3 => MouseButton.Back,
                4 => MouseButton.Forward,
                _ => (MouseButton?)null,
            },
        };
        if (button is null)
        {
            return;
        }

        var (x, y) = PointerPosition(e);
        Post(new PlatformEvent
        {
            Kind = down ? PlatformEventKind.PointerButtonDown : PlatformEventKind.PointerButtonUp,
            WindowId = Id,
            Button = button.Value,
            X = x,
            Y = y,
            Modifiers = CocoaKeys.ModifiersFromFlags(Msg.UL(e, ObjC.Sel("modifierFlags"))),
        });
    }

    internal void MoveEvent(nint e)
    {
        if (CursorLocked)
        {
            var dx = (float)(Msg.Dbl(e, ObjC.Sel("deltaX")) * Scale);
            var dy = (float)(Msg.Dbl(e, ObjC.Sel("deltaY")) * Scale);
            if (dx != 0 || dy != 0)
            {
                Post(new PlatformEvent { Kind = PlatformEventKind.PointerRelativeMotion, WindowId = Id, DeltaX = dx, DeltaY = dy });
            }

            return;
        }

        var (x, y) = PointerPosition(e);
        Post(new PlatformEvent { Kind = PlatformEventKind.PointerMoved, WindowId = Id, X = x, Y = y });
    }

    internal void Inside(bool entered)
    {
        _inside = entered;
        ApplyCursor();
        Post(new PlatformEvent { Kind = PlatformEventKind.PointerInside, WindowId = Id, Focused = entered });
    }

    internal void ScrollEvent(nint e)
    {
        var dx = Msg.Dbl(e, ObjC.Sel("scrollingDeltaX"));
        var dy = Msg.Dbl(e, ObjC.Sel("scrollingDeltaY"));
        if (Msg.Flag(e, ObjC.Sel("hasPreciseScrollingDeltas")) != 0)
        {
            dx /= PointsPerNotch;
            dy /= PointsPerNotch;
        }

        if (dx == 0 && dy == 0)
        {
            return;
        }

        // scrollingDeltaY is positive when the content moves down, which is the wheel turning away from the user.
        Post(new PlatformEvent
        {
            Kind = PlatformEventKind.PointerWheel,
            WindowId = Id,
            DeltaX = (float)dx,
            DeltaY = (float)dy,
            Modifiers = CocoaKeys.ModifiersFromFlags(Msg.UL(e, ObjC.Sel("modifierFlags"))),
        });
    }

    // NSTextInputClient.insertText:replacementRange:
    internal void Insert(string text)
    {
        EndComposition();
        if (CocoaKeys.TypedText(text, _eventModifiers) is { } typed)
        {
            Post(new PlatformEvent { Kind = PlatformEventKind.TextInput, WindowId = Id, Text = typed, Modifiers = _eventModifiers });
        }
    }

    // NSTextInputClient.setMarkedText:selectedRange:replacementRange:
    internal void Mark(string text, nuint selectionStart, nuint selectionLength)
    {
        if (text.Length == 0)
        {
            EndComposition();
            return;
        }

        _marked = true;
        _markedLength = text.Length;
        _selectionStart = Math.Min(selectionStart, (nuint)text.Length);
        _selectionLength = Math.Min(selectionLength, (nuint)text.Length - _selectionStart);
        Post(new PlatformEvent
        {
            Kind = PlatformEventKind.TextComposition,
            WindowId = Id,
            Text = text,
            CursorStart = (int)_selectionStart,
            CursorEnd = (int)(_selectionStart + _selectionLength),
        });
    }

    // NSTextInputClient.unmarkText
    internal void Unmark() => EndComposition();

    /// <summary>Where the caret is, in screen coordinates, for the input method's candidate window.</summary>
    internal CGRect CaretScreenRect()
    {
        var (x, y, w, h) = TextInputArea;
        var s = Scale > 0 ? Scale : 1;
        var local = new CGRect { Origin = new CGPoint { X = x / s, Y = y / s }, Size = new CGSize { Width = w / s, Height = h / s } };

        // The view is flipped, so (x, y) is already measured from the top left, as the text input area is.
        var inWindow = Msg.Ret<CGRect, CGRect, nint>(_view, ObjC.Sel("convertRect:toView:"), local, 0);
        return Msg.Ret<CGRect, CGRect>(_window, ObjC.Sel("convertRectToScreen:"), inWindow);
    }

    // ----- helpers -----

    private void Post(in PlatformEvent e) => App.Post(e);

    private void EndComposition()
    {
        if (!_marked)
        {
            return;
        }

        _marked = false;
        _markedLength = 0;
        Post(new PlatformEvent { Kind = PlatformEventKind.TextComposition, WindowId = Id, Text = "", CursorStart = -1, CursorEnd = -1 });
    }

    private (float X, float Y) PointerPosition(nint e)
    {
        var inWindow = Msg.Ret<CGPoint>(e, ObjC.Sel("locationInWindow"));

        // The view is flipped, so the converted point is measured from the top left, in points.
        var p = Msg.Ret<CGPoint, CGPoint, nint>(_view, ObjC.Sel("convertPoint:fromView:"), inWindow, 0);
        return ((float)(p.X * Scale), (float)(p.Y * Scale));
    }

    private void ReadGeometry(bool silent)
    {
        var scale = (float)Msg.Dbl(_window, ObjC.Sel("backingScaleFactor"));
        var bounds = Msg.Ret<CGRect>(_view, ObjC.Sel("bounds"));
        var w = Math.Max(1, (int)Math.Round(bounds.Size.Width * scale));
        var h = Math.Max(1, (int)Math.Round(bounds.Size.Height * scale));
        if (silent)
        {
            Scale = scale > 0 ? scale : 1;
            Width = w;
            Height = h;
            return;
        }

        ApplyScale(scale);
        ApplySize(w, h);
    }

    private void ApplyCursor()
    {
        // [NSCursor hide] and [NSCursor unhide] nest, so the window applies one hide at a time.
        var want = (CursorHidden && _inside) || CursorLocked;
        if (want == _cursorHideApplied)
        {
            return;
        }

        _cursorHideApplied = want;
        ObjC.Send(ObjC.Class("NSCursor"), want ? "hide" : "unhide");
    }
}
