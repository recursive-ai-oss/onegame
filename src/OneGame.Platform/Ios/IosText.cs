using System.Runtime.InteropServices;

namespace OneGame.Platform;

/// <summary>
/// The text input object: a hidden view that becomes first responder while a game text field has focus, so iOS shows its keyboard and
/// talks to it through UIKeyInput (committed text, delete) and UITextInput (marked text, which is how Chinese, Japanese and Korean input
/// methods, predictive text and dictation show text that is not final). The "document" it exposes holds only the marked text: whatever
/// is committed leaves at once as a <see cref="PlatformEventKind.TextInput"/> event, and the game's own field keeps the real text.
/// Positions and ranges are small Objective-C objects (UITextPosition and UITextRange subclasses) that carry UTF-16 indexes.
/// </summary>
internal static unsafe class IosText
{
    private static string _marked = "";
    private static int _selStart;
    private static int _selEnd;
    private static (int X, int Y, int W, int H) _area;
    private static nint _delegate;
    private static nint _tokenizer;
    private static nint _textClass;
    private static nint _positionClass;
    private static nint _rangeClass;
    private static nint _positionIvar;
    private static nint _rangeStartIvar;
    private static nint _rangeEndIvar;

    internal static void SetCaretArea(int x, int y, int w, int h) => _area = (x, y, w, h);

    internal static nint Define()
    {
        _positionClass = IosObjc.DefineClass("OneGameTextPosition", "UITextPosition", [], [], null, [("_index", 8, 3, "q")]);
        _positionIvar = IosObjc.class_getInstanceVariable(_positionClass, "_index");
        _rangeClass = IosObjc.DefineClass("OneGameTextRange", "UITextRange", [],
        [
            new("start", (nint)(delegate* unmanaged<nint, nint, nint>)&RangeStart, "@@:"),
            new("end", (nint)(delegate* unmanaged<nint, nint, nint>)&RangeEnd, "@@:"),
            new("isEmpty", (nint)(delegate* unmanaged<nint, nint, byte>)&RangeIsEmpty, "B@:"),
        ], null, [("_start", 8, 3, "q"), ("_end", 8, 3, "q")]);
        _rangeStartIvar = IosObjc.class_getInstanceVariable(_rangeClass, "_start");
        _rangeEndIvar = IosObjc.class_getInstanceVariable(_rangeClass, "_end");

        _textClass = IosObjc.DefineClass("OneGameTextView", "UIView", ["UIKeyInput", "UITextInput"],
        [
            new("canBecomeFirstResponder", (nint)(delegate* unmanaged<nint, nint, byte>)&Yes, "B@:"),
            new("hasText", (nint)(delegate* unmanaged<nint, nint, byte>)&Yes, "B@:"),
            new("insertText:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&InsertText, "v@:@"),
            new("deleteBackward", (nint)(delegate* unmanaged<nint, nint, void>)&DeleteBackward, "v@:"),
            new("pressesBegan:withEvent:", (nint)(delegate* unmanaged<nint, nint, nint, nint, void>)&PressesBegan, "v@:@@"),
            new("pressesEnded:withEvent:", (nint)(delegate* unmanaged<nint, nint, nint, nint, void>)&PressesEnded, "v@:@@"),
            new("pressesCancelled:withEvent:", (nint)(delegate* unmanaged<nint, nint, nint, nint, void>)&PressesEnded, "v@:@@"),

            // Traits: a game field wants exactly what was typed.
            new("autocorrectionType", (nint)(delegate* unmanaged<nint, nint, nint>)&TraitNo, "q@:"),
            new("spellCheckingType", (nint)(delegate* unmanaged<nint, nint, nint>)&TraitNo, "q@:"),
            new("smartQuotesType", (nint)(delegate* unmanaged<nint, nint, nint>)&TraitNo, "q@:"),
            new("smartDashesType", (nint)(delegate* unmanaged<nint, nint, nint>)&TraitNo, "q@:"),
            new("smartInsertDeleteType", (nint)(delegate* unmanaged<nint, nint, nint>)&TraitNo, "q@:"),
            new("autocapitalizationType", (nint)(delegate* unmanaged<nint, nint, nint>)&Zero, "q@:"),

            // UITextInput
            new("selectedTextRange", (nint)(delegate* unmanaged<nint, nint, nint>)&SelectedTextRange, "@@:"),
            new("setSelectedTextRange:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&SetSelectedTextRange, "v@:@"),
            new("markedTextRange", (nint)(delegate* unmanaged<nint, nint, nint>)&MarkedTextRange, "@@:"),
            new("markedTextStyle", (nint)(delegate* unmanaged<nint, nint, nint>)&Zero, "@@:"),
            new("setMarkedTextStyle:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&Ignore1, "v@:@"),
            new("setMarkedText:selectedRange:", (nint)(delegate* unmanaged<nint, nint, nint, NSRange, void>)&SetMarkedText, "v@:@{_NSRange=QQ}"),
            new("unmarkText", (nint)(delegate* unmanaged<nint, nint, void>)&UnmarkText, "v@:"),
            new("beginningOfDocument", (nint)(delegate* unmanaged<nint, nint, nint>)&BeginningOfDocument, "@@:"),
            new("endOfDocument", (nint)(delegate* unmanaged<nint, nint, nint>)&EndOfDocument, "@@:"),
            new("textInRange:", (nint)(delegate* unmanaged<nint, nint, nint, nint>)&TextInRange, "@@:@"),
            new("replaceRange:withText:", (nint)(delegate* unmanaged<nint, nint, nint, nint, void>)&ReplaceRange, "v@:@@"),
            new("selectionAffinity", (nint)(delegate* unmanaged<nint, nint, nint>)&Zero, "q@:"),
            new("setSelectionAffinity:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&Ignore1, "v@:q"),
            new("comparePosition:toPosition:", (nint)(delegate* unmanaged<nint, nint, nint, nint, nint>)&ComparePosition, "q@:@@"),
            new("offsetFromPosition:toPosition:", (nint)(delegate* unmanaged<nint, nint, nint, nint, nint>)&OffsetFromPosition, "q@:@@"),
            new("positionFromPosition:offset:", (nint)(delegate* unmanaged<nint, nint, nint, nint, nint>)&PositionOffset, "@@:@q"),
            new("positionFromPosition:inDirection:offset:", (nint)(delegate* unmanaged<nint, nint, nint, nint, nint, nint>)&PositionDirection, "@@:@qq"),
            new("textRangeFromPosition:toPosition:", (nint)(delegate* unmanaged<nint, nint, nint, nint, nint>)&TextRangeFromPositions, "@@:@@"),
            new("positionWithinRange:farthestInDirection:", (nint)(delegate* unmanaged<nint, nint, nint, nint, nint>)&FarthestPosition, "@@:@q"),
            new("characterRangeByExtendingPosition:inDirection:", (nint)(delegate* unmanaged<nint, nint, nint, nint, nint>)&CharacterRangeByExtending, "@@:@q"),
            new("baseWritingDirectionForPosition:inDirection:", (nint)(delegate* unmanaged<nint, nint, nint, nint, nint>)&Zero4, "q@:@q"),
            new("setBaseWritingDirection:forRange:", (nint)(delegate* unmanaged<nint, nint, nint, nint, void>)&Ignore2, "v@:q@"),
            new("firstRectForRange:", (nint)(delegate* unmanaged<nint, nint, nint, CGRect>)&CaretRect, "{CGRect={CGPoint=dd}{CGSize=dd}}@:@"),
            new("caretRectForPosition:", (nint)(delegate* unmanaged<nint, nint, nint, CGRect>)&CaretRect, "{CGRect={CGPoint=dd}{CGSize=dd}}@:@"),
            new("selectionRectsForRange:", (nint)(delegate* unmanaged<nint, nint, nint, nint>)&SelectionRects, "@@:@"),
            new("closestPositionToPoint:", (nint)(delegate* unmanaged<nint, nint, CGPoint, nint>)&ClosestPosition, "@@:{CGPoint=dd}"),
            new("closestPositionToPoint:withinRange:", (nint)(delegate* unmanaged<nint, nint, CGPoint, nint, nint>)&ClosestPositionInRange, "@@:{CGPoint=dd}@"),
            new("characterRangeAtPoint:", (nint)(delegate* unmanaged<nint, nint, CGPoint, nint>)&CharacterRangeAtPoint, "@@:{CGPoint=dd}"),
            new("inputDelegate", (nint)(delegate* unmanaged<nint, nint, nint>)&GetInputDelegate, "@@:"),
            new("setInputDelegate:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&SetInputDelegate, "v@:@"),
            new("tokenizer", (nint)(delegate* unmanaged<nint, nint, nint>)&Tokenizer, "@@:"),
            new("textInputView", (nint)(delegate* unmanaged<nint, nint, nint>)&Self, "@@:"),
        ]);
        return _textClass;
    }

    // ---- state for the control surface ----

    /// <summary>Calls the object's own text-input methods on the main thread, the way the keyboard would.</summary>
    internal static string Drive(string action, string argument)
    {
        var result = "ok";
        IosObjc.OnMainWait(() =>
        {
            var view = IosUi.TextView;
            if (view == 0 || !IosObjc.Flag(view, IosObjc.Sel("isFirstResponder")))
            {
                result = "no_text_input";
                return;
            }

            switch (action)
            {
                case "text_insert":
                    IosObjc.Void(view, IosObjc.Sel("insertText:"), IosObjc.NSString(argument));
                    break;
                case "text_mark":
                    IosObjc.Void(view, IosObjc.Sel("setMarkedText:selectedRange:"), IosObjc.NSString(argument), new NSRange { Location = (nuint)argument.Length, Length = 0 });
                    break;
                case "text_unmark":
                    IosObjc.Void(view, IosObjc.Sel("unmarkText"));
                    break;
                case "text_delete":
                    IosObjc.Void(view, IosObjc.Sel("deleteBackward"));
                    break;
                case "text_probe":
                    result = Probe(view);
                    break;
                default:
                    result = "unknown_action";
                    break;
            }
        });
        return result;
    }

    // Asks the text object the questions the system's text machinery asks, through the Objective-C runtime, so the argument and result
    // conventions of every answer (positions, ranges, rectangles, points) are exercised and not just compiled.
    private static string Probe(nint view)
    {
        var begin = IosObjc.Obj(view, IosObjc.Sel("beginningOfDocument"));
        var end = IosObjc.Obj(view, IosObjc.Sel("endOfDocument"));
        var range = IosObjc.Obj(view, IosObjc.Sel("textRangeFromPosition:toPosition:"), begin, end);
        var text = IosObjc.ToManaged(IosObjc.Obj(view, IosObjc.Sel("textInRange:"), range));
        var order = IosObjc.Int(view, IosObjc.Sel("comparePosition:toPosition:"), begin, end);
        var distance = IosObjc.Int(view, IosObjc.Sel("offsetFromPosition:toPosition:"), begin, end);
        var rect = IosObjc.Ret<CGRect, nint>(view, IosObjc.Sel("caretRectForPosition:"), end);
        var farthest = IosObjc.Obj(view, IosObjc.Sel("positionWithinRange:farthestInDirection:"), range, (nint)2);
        var closest = IosObjc.Obj(view, IosObjc.Sel("closestPositionToPoint:"), new CGPoint { X = 3, Y = 4 });
        var inRange = IosObjc.Obj(view, IosObjc.Sel("positionFromPosition:offset:"), begin, (nint)1);
        var marked = IosObjc.Obj(view, IosObjc.Sel("markedTextRange")) != 0;
        var selected = IosObjc.Obj(view, IosObjc.Sel("selectedTextRange"));
        var selEmpty = IosObjc.Flag(selected, IosObjc.Sel("isEmpty"));
        return string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"text={text};order={order};distance={distance};rect={rect.Origin.X},{rect.Origin.Y},{rect.Size.Width},{rect.Size.Height};farthest={IndexOf(farthest)};closest={IndexOf(closest)};next={(inRange == 0 ? "nil" : IndexOf(inRange).ToString(System.Globalization.CultureInfo.InvariantCulture))};marked={marked};selectionEmpty={selEmpty}");
    }

    /// <summary>Whether the text view is first responder (the keyboard is up).</summary>
    internal static bool IsActive()
    {
        var active = false;
        IosObjc.OnMainWait(() => active = IosUi.TextView != 0 && IosObjc.Flag(IosUi.TextView, IosObjc.Sel("isFirstResponder")));
        return active;
    }

    // ---- positions and ranges ----

    private static nint MakePosition(int index)
    {
        var p = IosObjc.Autorelease(IosObjc.Obj(IosObjc.Alloc(_positionClass), IosObjc.Sel("init")));
        *(nint*)((byte*)p + IosObjc.ivar_getOffset(_positionIvar)) = index;
        return p;
    }

    private static int IndexOf(nint position) =>
        position == 0 ? 0 : (int)*(nint*)((byte*)position + IosObjc.ivar_getOffset(_positionIvar));

    private static nint MakeRange(int start, int end)
    {
        var r = IosObjc.Autorelease(IosObjc.Obj(IosObjc.Alloc(_rangeClass), IosObjc.Sel("init")));
        *(nint*)((byte*)r + IosObjc.ivar_getOffset(_rangeStartIvar)) = start;
        *(nint*)((byte*)r + IosObjc.ivar_getOffset(_rangeEndIvar)) = end;
        return r;
    }

    private static (int Start, int End) Bounds(nint range) =>
        range == 0 ? (0, 0) : ((int)*(nint*)((byte*)range + IosObjc.ivar_getOffset(_rangeStartIvar)), (int)*(nint*)((byte*)range + IosObjc.ivar_getOffset(_rangeEndIvar)));

    [UnmanagedCallersOnly]
    private static nint RangeStart(nint self, nint cmd) => MakePosition(Bounds(self).Start);

    [UnmanagedCallersOnly]
    private static nint RangeEnd(nint self, nint cmd) => MakePosition(Bounds(self).End);

    [UnmanagedCallersOnly]
    private static byte RangeIsEmpty(nint self, nint cmd) => Bounds(self) is var (s, e) && s == e ? (byte)1 : (byte)0;

    // ---- small answers ----

    [UnmanagedCallersOnly]
    private static byte Yes(nint self, nint cmd) => 1;

    [UnmanagedCallersOnly]
    private static nint Zero(nint self, nint cmd) => 0;

    [UnmanagedCallersOnly]
    private static nint Zero4(nint self, nint cmd, nint a, nint b) => 0;

    // UITextAutocorrectionTypeNo, UITextSpellCheckingTypeNo, UITextSmartQuotesTypeNo and the like are all 1.
    [UnmanagedCallersOnly]
    private static nint TraitNo(nint self, nint cmd) => 1;

    [UnmanagedCallersOnly]
    private static nint Self(nint self, nint cmd) => self;

    [UnmanagedCallersOnly]
    private static void Ignore1(nint self, nint cmd, nint a)
    {
    }

    [UnmanagedCallersOnly]
    private static void Ignore2(nint self, nint cmd, nint a, nint b)
    {
    }

    [UnmanagedCallersOnly]
    private static nint GetInputDelegate(nint self, nint cmd) => _delegate;

    [UnmanagedCallersOnly]
    private static void SetInputDelegate(nint self, nint cmd, nint value) => _delegate = value;

    [UnmanagedCallersOnly]
    private static nint Tokenizer(nint self, nint cmd)
    {
        if (_tokenizer == 0)
        {
            _tokenizer = IosObjc.objc_retain(IosObjc.Obj(IosObjc.Alloc(IosObjc.Cls("UITextInputStringTokenizer")), IosObjc.Sel("initWithTextInput:"), self));
        }

        return _tokenizer;
    }

    [UnmanagedCallersOnly]
    private static nint SelectionRects(nint self, nint cmd, nint range) => IosObjc.Obj(IosObjc.Cls("NSArray"), IosObjc.Sel("array"));

    // ---- key presses ----

    [UnmanagedCallersOnly]
    private static void PressesBegan(nint self, nint cmd, nint presses, nint ev) => IosUi.Presses(self, cmd, presses, ev, true, _textClass, textMode: true);

    [UnmanagedCallersOnly]
    private static void PressesEnded(nint self, nint cmd, nint presses, nint ev) => IosUi.Presses(self, cmd, presses, ev, false, _textClass, textMode: true);

    // ---- committed text ----

    private static void PostKey(Key key)
    {
        var host = IosAppHost.Current;
        host?.Post(new IosEvent { Kind = IosEventKind.KeyDown, Key = key });
        host?.Post(new IosEvent { Kind = IosEventKind.KeyUp, Key = key });
    }

    private static void EndComposition()
    {
        if (_marked.Length > 0)
        {
            _marked = "";
            _selStart = _selEnd = 0;
            IosAppHost.Current?.Post(new IosEvent { Kind = IosEventKind.Composition, Text = "", CursorStart = -1, CursorEnd = -1 });
        }
    }

    private static void Commit(string text)
    {
        EndComposition();
        if (text == "\n")
        {
            PostKey(Key.Enter);
        }
        else if (text.Length > 0)
        {
            IosAppHost.Current?.Post(new IosEvent { Kind = IosEventKind.Text, Text = text });
        }
    }

    [UnmanagedCallersOnly]
    private static void InsertText(nint self, nint cmd, nint text)
    {
        try
        {
            Commit(IosObjc.ToManaged(text));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("onegame: insertText failed: " + ex);
        }
    }

    [UnmanagedCallersOnly]
    private static void DeleteBackward(nint self, nint cmd)
    {
        try
        {
            if (_marked.Length == 0)
            {
                PostKey(Key.Backspace);
                return;
            }

            var cut = _marked.Length >= 2 && char.IsLowSurrogate(_marked[^1]) ? 2 : 1;
            _marked = _marked[..^cut];
            _selStart = _selEnd = _marked.Length;
            IosAppHost.Current?.Post(new IosEvent { Kind = IosEventKind.Composition, Text = _marked, CursorStart = _marked.Length == 0 ? -1 : _selStart, CursorEnd = _marked.Length == 0 ? -1 : _selEnd });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("onegame: deleteBackward failed: " + ex);
        }
    }

    // ---- marked text ----

    [UnmanagedCallersOnly]
    private static void SetMarkedText(nint self, nint cmd, nint text, NSRange selected)
    {
        try
        {
            var s = IosObjc.ToManaged(text);
            if (s.Length == 0)
            {
                EndComposition();
                return;
            }

            _marked = s;
            _selStart = (int)Math.Min(selected.Location, (nuint)s.Length);
            _selEnd = (int)Math.Min(selected.Location + selected.Length, (nuint)s.Length);
            IosAppHost.Current?.Post(new IosEvent { Kind = IosEventKind.Composition, Text = s, CursorStart = _selStart, CursorEnd = _selEnd });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("onegame: setMarkedText failed: " + ex);
        }
    }

    [UnmanagedCallersOnly]
    private static void UnmarkText(nint self, nint cmd)
    {
        try
        {
            Commit(_marked);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("onegame: unmarkText failed: " + ex);
        }
    }

    [UnmanagedCallersOnly]
    private static nint SelectedTextRange(nint self, nint cmd) => MakeRange(_selStart, _selEnd);

    [UnmanagedCallersOnly]
    private static void SetSelectedTextRange(nint self, nint cmd, nint range)
    {
        if (_marked.Length > 0 && range != 0)
        {
            var (s, e) = Bounds(range);
            _selStart = Math.Clamp(s, 0, _marked.Length);
            _selEnd = Math.Clamp(e, 0, _marked.Length);
        }
    }

    [UnmanagedCallersOnly]
    private static nint MarkedTextRange(nint self, nint cmd) => _marked.Length == 0 ? 0 : MakeRange(0, _marked.Length);

    [UnmanagedCallersOnly]
    private static nint BeginningOfDocument(nint self, nint cmd) => MakePosition(0);

    [UnmanagedCallersOnly]
    private static nint EndOfDocument(nint self, nint cmd) => MakePosition(_marked.Length);

    [UnmanagedCallersOnly]
    private static nint TextInRange(nint self, nint cmd, nint range)
    {
        var (s, e) = Bounds(range);
        s = Math.Clamp(s, 0, _marked.Length);
        e = Math.Clamp(e, s, _marked.Length);
        return IosObjc.NSString(_marked[s..e]);
    }

    [UnmanagedCallersOnly]
    private static void ReplaceRange(nint self, nint cmd, nint range, nint text)
    {
        try
        {
            Commit(IosObjc.ToManaged(text));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("onegame: replaceRange failed: " + ex);
        }
    }

    [UnmanagedCallersOnly]
    private static nint ComparePosition(nint self, nint cmd, nint a, nint b) => IndexOf(a).CompareTo(IndexOf(b));

    [UnmanagedCallersOnly]
    private static nint OffsetFromPosition(nint self, nint cmd, nint from, nint to) => IndexOf(to) - IndexOf(from);

    private static nint Moved(nint position, long offset)
    {
        var i = IndexOf(position) + offset;
        return i < 0 || i > _marked.Length ? 0 : MakePosition((int)i);
    }

    [UnmanagedCallersOnly]
    private static nint PositionOffset(nint self, nint cmd, nint position, nint offset) => Moved(position, offset);

    // UITextLayoutDirection: right 2, left 3, up 4, down 5.
    [UnmanagedCallersOnly]
    private static nint PositionDirection(nint self, nint cmd, nint position, nint direction, nint offset) =>
        direction switch
        {
            3 => Moved(position, -offset),
            2 => Moved(position, offset),
            _ => position == 0 ? 0 : MakePosition(IndexOf(position)),
        };

    [UnmanagedCallersOnly]
    private static nint TextRangeFromPositions(nint self, nint cmd, nint a, nint b)
    {
        int s = IndexOf(a), e = IndexOf(b);
        return MakeRange(Math.Min(s, e), Math.Max(s, e));
    }

    [UnmanagedCallersOnly]
    private static nint FarthestPosition(nint self, nint cmd, nint range, nint direction)
    {
        var (s, e) = Bounds(range);
        return MakePosition(direction is 2 or 5 ? e : s);
    }

    [UnmanagedCallersOnly]
    private static nint CharacterRangeByExtending(nint self, nint cmd, nint position, nint direction)
    {
        var i = IndexOf(position);
        return MakeRange(i, i);
    }

    [UnmanagedCallersOnly]
    private static CGRect CaretRect(nint self, nint cmd, nint positionOrRange)
    {
        // Where a candidate list should appear: the caret the game gave us, in points.
        var scale = IosUi.View == 0 ? 1.0 : IosObjc.Dbl(IosUi.View, IosObjc.Sel("contentScaleFactor"));
        if (scale <= 0)
        {
            scale = 1;
        }

        return new CGRect
        {
            Origin = new CGPoint { X = _area.X / scale, Y = _area.Y / scale },
            Size = new CGSize { Width = Math.Max(1, _area.W) / scale, Height = Math.Max(1, _area.H) / scale },
        };
    }

    [UnmanagedCallersOnly]
    private static nint ClosestPosition(nint self, nint cmd, CGPoint point) => MakePosition(_marked.Length);

    [UnmanagedCallersOnly]
    private static nint ClosestPositionInRange(nint self, nint cmd, CGPoint point, nint range) => MakePosition(Bounds(range).End);

    [UnmanagedCallersOnly]
    private static nint CharacterRangeAtPoint(nint self, nint cmd, CGPoint point) => 0;
}
