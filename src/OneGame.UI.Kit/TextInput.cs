namespace OneGame.UI.Kit;

/// <summary>Where copied text goes. A windowed game's fields use the system clipboard; <see cref="MemoryClipboard"/> keeps text inside the process (headless runs and tests).</summary>
public interface IClipboard
{
    /// <summary>The text on the clipboard, or null when there is none.</summary>
    string? GetText();

    /// <summary>Replaces the clipboard's content.</summary>
    void SetText(string text);
}

/// <summary>A clipboard that lives in memory, shared by everything that was given the same instance.</summary>
public sealed class MemoryClipboard : IClipboard
{
    private string? _text;

    /// <inheritdoc/>
    public string? GetText() => _text;

    /// <inheritdoc/>
    public void SetText(string text) => _text = text ?? throw new ArgumentNullException(nameof(text));
}

/// <summary>
/// A single-line text field (kind <c>TextInput</c>, role <c>textbox</c>). The caret and the selection are drawn as elements (<c>id/caret</c>, <c>id/selection</c> rectangles).
/// Editing works on grapheme clusters: a letter with its accents, or an emoji sequence, is one step for the arrow keys, Backspace and Delete.
/// Arrow keys move the caret through the text as it looks on screen, also where left-to-right and right-to-left text mix. Shift extends the selection; Ctrl (or Cmd) with A, C, X, V selects all, copies, cuts and pastes.
/// Home and End go to the start and end of the text. Enter calls the submit callback. Up, Down, Tab and the gamepad d-pad leave the field.
/// The text is the game's: the field reads it through <c>get</c> every frame and reports edits through <c>set</c>, so a game that refuses an edit by not storing it sees it undone.
/// In password mode the field shows bullets, never copies, and keeps the text out of <see cref="Element.Value"/>.
/// </summary>
public sealed class TextInputView : Element
{
    private readonly TextEditor _editor;
    private readonly Func<string>? _get;
    private readonly Action<string>? _set;
    private readonly Action<string>? _onSubmit;
    private readonly Element _viewport;
    private readonly Element _label;
    private readonly Element _placeholder;
    private readonly Element _caret;
    private readonly Element _content;
    private readonly List<Element> _selectionRects = [];
    private readonly List<(float X0, float X1)> _runs = [];
    private TextStyle _style;
    private ITextMeasurer? _measurer;
    private float _baseline;
    private float _lineHeight = -1;
    private bool _visualsDirty = true;
    private bool _rebuild = true;
    private int _reveal;
    private bool _caretShown;
    private float _lastCaretX = float.NaN;
    private int _usedRects;
    private string _preedit = "";
    private float _preeditWidth;

    internal TextInputView(Func<string>? get, Action<string>? set, string placeholder, bool password, int maxLength, Func<string, bool>? validate, Action<string>? onSubmit, IClipboard? clipboard, string? id)
        : base("TextInput")
    {
        _get = get;
        _set = set;
        _onSubmit = onSubmit;
        Clipboard = clipboard ?? UiKit.DefaultClipboard;
        _editor = new TextEditor(GraphemeWidth)
        {
            Password = password,
            MaxLength = maxLength,
            Validate = validate,
        };
        Id = id;
        Role = "textbox";
        AcceptsText = true;   // tells the host to switch the platform's input method on while this field has focus
        Class("kit-input " + UiKit.FocusableClass);
        IsFocusable = true;
        Style(s => s.Cursor(CursorShape.Text));
        _viewport = UiKit.Part("Box", "kit-input-viewport", id, "viewport");
        _viewport.Style(s => s.Overflow(OverflowMode.Hidden).Grow(1).MinWidth(0f));
        _content = UiKit.Part("Box", "kit-input-content", id, "content");
        _content.Style(s => s.Shrink(0).Grow(1));
        _label = UiKit.Part("Label", "kit-input-text", id, "text");
        _placeholder = UiKit.Part("Label", "kit-placeholder", id, "placeholder");
        _placeholder.Text = placeholder;
        _placeholder.Style(s => s.Position(PositionMode.Absolute).Left(0f).Top(0f));
        _placeholder.IsVisible = false;
        _caret = UiKit.Part("Box", "kit-caret", id, "caret");
        _caret.IsVisible = false;
        _content.With(_label, _placeholder, _caret);
        _viewport.Add(_content);
        Add(_viewport);

        On(UiEventKind.PointerDown, (t, e) =>
        {
            if (e.Button != 0)
            {
                return false;
            }

            _editor.PlaceAtX(e.X - _label.AbsoluteBounds.X, extend: false);
            t.Tree?.CapturePointer(t);
            Moved();
            return true;
        });
        On(UiEventKind.PointerMove, (t, e) =>
        {
            if (t.Tree?.PointerCaptor != t)
            {
                return false;
            }

            _editor.PlaceAtX(e.X - _label.AbsoluteBounds.X, extend: true);
            Moved();
            return true;
        });
        On(UiEventKind.Composition, (_, e) =>
        {
            SetPreedit(e.Text ?? "");
            return true;
        });
        On(UiEventKind.TextInput, (_, e) =>
        {
            SetPreedit("");
            Commit(_editor.Insert(e.Text ?? ""));
            return true;
        });
        On(UiEventKind.KeyDown, (_, e) => HandleKey(e));
        OnUpdate(_ => Sync());
    }

    /// <summary>Where cut and copied text goes, and where pasted text comes from. Defaults to <see cref="UiKit.DefaultClipboard"/>.</summary>
    public IClipboard Clipboard { get; set; }

    /// <summary>The text in the field.</summary>
    public string FieldText => _editor.Text;

    /// <summary>The caret as a UTF-16 index into <see cref="FieldText"/>; it is always at the edge of a grapheme cluster.</summary>
    public int CaretIndex => _editor.CaretUtf16;

    /// <summary>Where the selection starts, as a UTF-16 index; equal to <see cref="SelectionEnd"/> when nothing is selected.</summary>
    public int SelectionStart => _editor.SelectionStartUtf16;

    /// <summary>Where the selection ends, as a UTF-16 index.</summary>
    public int SelectionEnd => _editor.SelectionEndUtf16;

    /// <summary>The selected text, or an empty string.</summary>
    public string SelectedText => _editor.SelectedText;

    /// <summary>The horizontal position of the caret in pixels from the start of the text, as of the last update.</summary>
    public float CaretX => _editor.Map.SlotX[_editor.CaretVisual];

    /// <summary>The number of grapheme clusters in <see cref="FieldText"/>.</summary>
    public int GraphemeCount => _editor.Count;

    /// <summary>The input method's uncommitted text, shown at the caret until the method commits it (as ordinary text input) or clears it; empty when none.</summary>
    public string Preedit => _preedit;

    /// <summary>
    /// The caret's box on screen, which the host hands to the platform layer to place an input method's candidate window.
    /// While an input method has uncommitted text the box sits after it.
    /// </summary>
    public Rect CaretScreenRect
    {
        get
        {
            var b = _label.AbsoluteBounds;
            return new Rect(b.X + CaretX + _preeditWidth, b.Y, 1, b.Height);
        }
    }

    /// <summary>Selects the text between two UTF-16 indexes (moved outward to cluster edges) and puts the caret at <paramref name="end"/>.</summary>
    public void Select(int start, int end)
    {
        _editor.Place(ClusterAt(start), extend: false);
        _editor.Place(ClusterAt(end), extend: true);
        Moved();
    }

    /// <summary>Replaces the text without calling the <c>set</c> callback, for a field that keeps its own text; the caret goes to the end.</summary>
    public void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        _editor.SetText(text, caretToEnd: true);
        Moved();
    }

    /// <summary>Selects all the text.</summary>
    public void SelectAll()
    {
        _editor.SelectAll();
        Moved();
    }

    /// <summary>Copies the selection to the clipboard; does nothing in password mode or without a selection.</summary>
    public void Copy()
    {
        if (!_editor.Password && _editor.HasSelection)
        {
            Clipboard.SetText(_editor.SelectedText);
        }
    }

    /// <summary>Copies the selection to the clipboard and deletes it.</summary>
    public void Cut()
    {
        if (!_editor.Password && _editor.HasSelection)
        {
            Clipboard.SetText(_editor.SelectedText);
            Commit(_editor.Backspace());
        }
    }

    /// <summary>Inserts the clipboard's text at the caret, replacing the selection.</summary>
    public void Paste()
    {
        if (Clipboard.GetText() is { Length: > 0 } text)
        {
            Commit(_editor.Insert(text));
        }
    }

    private void SetPreedit(string text)
    {
        if (_editor.Password && text.Length > 0)
        {
            text = new string('\u2022', text.Length);   // what a password field shows for typed characters
        }

        if (string.Equals(text, _preedit, StringComparison.Ordinal))
        {
            return;
        }

        _preedit = text;
        _preeditWidth = text.Length == 0 ? 0 : Measure(text);
        Moved();
    }

    private int ClusterAt(int utf16)
    {
        var at = Array.BinarySearch(_editor.RealStarts, Math.Clamp(utf16, 0, _editor.Text.Length));
        return at >= 0 ? at : ~at;
    }

    private bool HandleKey(in UiEvent e)
    {
        var shift = (e.Modifiers & KeyModifiers.Shift) != 0;
        var command = (e.Modifiers & (KeyModifiers.Ctrl | KeyModifiers.Meta)) != 0;
        if (command)
        {
            switch (e.Key?.ToLowerInvariant())
            {
                case "a": SelectAll(); return true;
                case "c": Copy(); return true;
                case "x": Cut(); return true;
                case "v": Paste(); return true;
                default: break;
            }
        }

        switch (e.Key)
        {
            case "ArrowLeft": _editor.MoveVisual(-1, shift); Moved(); return true;
            case "ArrowRight": _editor.MoveVisual(1, shift); Moved(); return true;
            case "Home": _editor.Place(0, shift); Moved(); return true;
            case "End": _editor.Place(_editor.Count, shift); Moved(); return true;
            case "Backspace": Commit(_editor.Backspace()); return true;
            case "Delete": Commit(_editor.Delete()); return true;
            case "Enter":
                _onSubmit?.Invoke(_editor.Text);
                return true;
            default: return false;
        }
    }

    private void Moved()
    {
        _visualsDirty = true;
        _reveal = 2;
    }

    private void Commit(bool changed)
    {
        Moved();
        if (changed)
        {
            _set?.Invoke(_editor.Text);
        }
    }

    private float Measure(string s) => (Tree?.Measurer ?? _measurer)?.Measure(s, _style, float.PositiveInfinity).Width ?? s.Length * 8f;

    // A cluster's advance, measured between two sentinels so that spaces and marks that the measurer might trim still count.
    private float GraphemeWidth(string cluster) => Math.Max(0, Measure("x" + cluster + "x") - _baseline);

    private void Sync()
    {
        var tree = Tree;
        if (_get is not null)
        {
            var wanted = _get();
            if (!string.Equals(wanted, _editor.Text, StringComparison.Ordinal))
            {
                _editor.SetText(wanted ?? "", caretToEnd: _editor.Caret >= _editor.Count);
                _visualsDirty = true;
                _reveal = 2;
            }
        }

        if (tree is not null)
        {
            var style = tree.TextStyleOf(_label);
            if (_rebuild || !style.Equals(_style) || !ReferenceEquals(tree.Measurer, _measurer))
            {
                _style = style;
                _measurer = tree.Measurer;
                _baseline = Measure("xx");
                var lineHeight = tree.Measurer.Measure("x", style, float.PositiveInfinity).Height;
                if (lineHeight != _lineHeight)
                {
                    _lineHeight = lineHeight;
                    _content.Style(s => s.MinHeight(Length.Px(lineHeight)));
                }

                _editor.Rebuild();
                _rebuild = false;
                _visualsDirty = true;
                _reveal = 2;
            }
        }

        var focused = IsFocused;
        if (focused != _caretShown)
        {
            _caretShown = focused;
            _caret.IsVisible = focused;
        }

        if (!_visualsDirty)
        {
            RevealCaret();
            CaretArea = CaretScreenRect;
            return;
        }

        _visualsDirty = false;
        var shown = _editor.DisplayText;
        if (_preedit.Length > 0)
        {
            // The pre-edit text goes in at the caret; the text after it moves right by its width.
            shown = shown.Insert(Math.Min(_editor.CaretUtf16, shown.Length), _preedit);
        }

        _label.Text = shown;
        _placeholder.IsVisible = _editor.Count == 0 && _preedit.Length == 0;
        Value = _editor.Password ? null : _editor.Text;
        var x = CaretX + _preeditWidth;
        if (x != _lastCaretX)
        {
            _lastCaretX = x;
            _caret.Left(Length.Px(x));
        }

        _runs.Clear();
        if (_editor.HasSelection)
        {
            _editor.Map.Runs(_editor.SelectionStart, _editor.SelectionEnd, _runs);
        }

        for (var i = 0; i < _runs.Count; i++)
        {
            if (i >= _selectionRects.Count)
            {
                var rect = UiKit.Part("Box", "kit-selection", Id, "selection");
                _selectionRects.Add(rect);
                _content.Insert(0, rect);
            }

            var r = _selectionRects[i];
            r.IsVisible = true;
            var (x0, x1) = _runs[i];
            r.Left(Length.Px(x0)).Width(Length.Px(x1 - x0));
        }

        for (var i = _runs.Count; i < _usedRects; i++)
        {
            _selectionRects[i].IsVisible = false;
        }

        _usedRects = _runs.Count;
        RevealCaret();
        CaretArea = CaretScreenRect;
    }

    // Scrolls the field sideways so the caret stays inside it. Layout lags an update behind, so a change is applied for two updates.
    private void RevealCaret()
    {
        if (_reveal <= 0)
        {
            return;
        }

        _reveal--;
        var width = _viewport.Bounds.Width;
        if (width <= 0)
        {
            return;
        }

        var x = CaretX + _preeditWidth;
        var scroll = _viewport.ScrollX;
        if (x - scroll > width - 2)
        {
            _viewport.ScrollTo(x - width + 2, 0);
        }
        else if (x < scroll)
        {
            _viewport.ScrollTo(x, 0);
        }
    }
}

public static partial class UiKit
{
    /// <summary>The clipboard text fields use unless they are given one. It keeps text inside the process until a windowed game instance replaces it with the system clipboard; a game can set its own.</summary>
    public static IClipboard DefaultClipboard { get; set; } = new MemoryClipboard();

    /// <summary>A single-line text field. See <see cref="TextInputView"/>.</summary>
    /// <param name="get">Reads the text, every frame. Null makes the field keep its own text (read it from <see cref="TextInputView.FieldText"/>).</param>
    /// <param name="set">Called with the whole new text after each edit.</param>
    /// <param name="placeholder">Dimmed text shown while the field is empty.</param>
    /// <param name="password">Show bullets instead of the text and never copy it.</param>
    /// <param name="maxLength">The most grapheme clusters the field takes; longer input is cut.</param>
    /// <param name="validate">Called with the text an edit would produce; return false to reject the edit (digits only, for example).</param>
    /// <param name="onSubmit">Called with the text when Enter is pressed.</param>
    /// <param name="clipboard">Where copy and paste go; the default is <see cref="DefaultClipboard"/>.</param>
    /// <param name="id">The id, or null.</param>
    public static TextInputView TextInput(
        Func<string>? get = null,
        Action<string>? set = null,
        string placeholder = "",
        bool password = false,
        int maxLength = int.MaxValue,
        Func<string, bool>? validate = null,
        Action<string>? onSubmit = null,
        IClipboard? clipboard = null,
        string? id = null) =>
        new(get, set, placeholder, password, maxLength, validate, onSubmit, clipboard, id);
}
