namespace OneGame.UI.Kit.Tests;

public class TextInputTests
{
    private const float Cw = 14 * 0.6f; // the monospace stand-in measures 0.6 em per character at the theme's 14 ui-unit font

    private static (Screen Screen, TextInputView Input, Func<string> Text) Make(string initial = "", bool password = false, int max = int.MaxValue, Func<string, bool>? validate = null, IClipboard? clipboard = null, string? reject = null)
    {
        var s = new Screen();
        var text = initial;
        var input = UiKit.TextInput(() => text, v => text = v == reject ? text : v, "Filter", password, max, validate, clipboard: clipboard, id: "t");
        s.Root.Add(input.Width(300));
        s.Settle();
        s.Focus(input);
        return (s, input, () => text);
    }

    [Fact]
    public void TypingInsertsAtTheCaretAndBackspaceAndDeleteRemove()
    {
        var (s, input, text) = Make("ac");
        s.Key("Home");
        s.Key("ArrowRight");
        s.Type("b");
        Assert.Equal("abc", text());
        Assert.Equal(2, input.CaretIndex);
        s.Key("Backspace");
        Assert.Equal("ac", text());
        s.Key("Delete");
        Assert.Equal("a", text());
        s.Key("End");
        s.Type("\r\nx\ty");
        Assert.Equal("a x y", text());
        Assert.Equal("a x y", input.Value);
        Assert.Equal("a x y", s.Get("t/text").Text);
    }

    [Fact]
    public void CaretAndDeletionStepByGraphemeCluster()
    {
        // e + combining acute, a ZWJ family emoji (7 code units... one cluster), a flag, then z.
        const string family = "\U0001F468\u200D\U0001F469\u200D\U0001F467";
        const string flag = "\U0001F1EF\U0001F1F5";
        var (s, input, text) = Make("e\u0301" + family + flag + "z");
        Assert.Equal(4, input.GraphemeCount);
        s.Key("Home");
        s.Key("ArrowRight");
        Assert.Equal(2, input.CaretIndex);
        s.Key("ArrowRight");
        Assert.Equal(2 + family.Length, input.CaretIndex);
        s.Key("ArrowRight");
        Assert.Equal(2 + family.Length + flag.Length, input.CaretIndex);
        s.Key("Backspace");
        Assert.Equal("e\u0301" + family + "z", text());
        s.Key("ArrowLeft");
        s.Key("Delete");
        Assert.Equal("e\u0301z", text());
        Assert.Equal(2, input.GraphemeCount);
        // A combining mark typed after a letter joins its cluster: one step, and the caret lands after the whole cluster.
        s.Key("Home");
        s.Key("ArrowRight");
        s.Key("ArrowRight");
        s.Type("o");
        s.Type("\u0308");
        Assert.Equal("e\u0301zo\u0308", text());
        Assert.Equal(3, input.GraphemeCount);
        Assert.Equal(text().Length, input.CaretIndex);
        s.Key("Backspace");
        Assert.Equal("e\u0301z", text());
    }

    [Fact]
    public void ShiftSelectionReplacementAndSelectAll()
    {
        var (s, input, text) = Make("hello world");
        s.Key("Home");
        for (var i = 0; i < 5; i++)
        {
            s.Key("ArrowRight", KeyModifiers.Shift);
        }

        Assert.Equal("hello", input.SelectedText);
        Assert.Equal((0, 5), (input.SelectionStart, input.SelectionEnd));
        var rect = s.Get("t/selection");
        Assert.True(rect.IsVisible);
        Assert.Equal(5 * Cw, rect.Bounds.Width, 0.01f);
        s.Type("bye");
        Assert.Equal("bye world", text());
        Assert.Equal(3, input.CaretIndex);
        Assert.False(s.Get("t/selection").IsVisible);
        s.Key("a", KeyModifiers.Ctrl);
        Assert.Equal("bye world", input.SelectedText);
        s.Key("ArrowLeft");
        Assert.Equal(0, input.CaretIndex);
        Assert.Equal("", input.SelectedText);
        s.Key("End", KeyModifiers.Shift);
        s.Key("Backspace");
        Assert.Equal("", text());
        Assert.True(s.Get("t/placeholder").IsVisible);
    }

    [Fact]
    public void ClipboardCopyCutAndPasteGoThroughTheInterface()
    {
        var clip = new MemoryClipboard();
        var (s, input, text) = Make("one two", clipboard: clip);
        s.Key("a", KeyModifiers.Ctrl);
        s.Key("c", KeyModifiers.Ctrl);
        Assert.Equal("one two", clip.GetText());
        s.Key("Home");
        s.Key("ArrowRight", KeyModifiers.Shift);
        s.Key("ArrowRight", KeyModifiers.Shift);
        s.Key("ArrowRight", KeyModifiers.Shift);
        s.Key("x", KeyModifiers.Meta);
        Assert.Equal("one", clip.GetText());
        Assert.Equal(" two", text());
        s.Key("End");
        clip.SetText("line1\nline2");
        s.Key("v", KeyModifiers.Ctrl);
        Assert.Equal(" twoline1 line2", text());
        Assert.Equal(text().Length, input.CaretIndex);
    }

    [Fact]
    public void PasswordModeMasksAndKeepsTheTextOutOfTheDump()
    {
        var clip = new MemoryClipboard();
        clip.SetText("keep");
        var (s, input, text) = Make(password: true, clipboard: clip);
        s.Type("hunter2");
        Assert.Equal("hunter2", text());
        Assert.Equal(new string('\u2022', 7), s.Get("t/text").Text);
        Assert.Null(input.Value);
        Assert.DoesNotContain("hunter2", s.Ui.DumpJson(), StringComparison.Ordinal);
        s.Key("a", KeyModifiers.Ctrl);
        s.Key("c", KeyModifiers.Ctrl);
        s.Key("x", KeyModifiers.Ctrl);
        Assert.Equal("keep", clip.GetText());
        Assert.Equal("hunter2", text());
        s.Key("Backspace");
        Assert.Equal("", text());
        s.Type("a\u0301b");
        Assert.Equal(new string('\u2022', 2), s.Get("t/text").Text);
        s.Key("ArrowLeft");
        s.Key("Backspace");
        Assert.Equal("b", text());
    }

    [Fact]
    public void MixedDirectionTextMovesTheCaretOneStepAtATimeAlongTheScreen()
    {
        // Logical order a b א ב c d; on screen: a b ב א c d.
        var (s, input, _) = Make("ab\u05D0\u05D1cd");
        Assert.Equal(6, input.GraphemeCount);
        s.Key("End");
        Assert.Equal(6 * Cw, input.CaretX, 0.01f);
        Assert.Equal(6, input.CaretIndex);
        var logicalWhenGoingLeft = new List<int>();
        for (var step = 5; step >= 0; step--)
        {
            s.Key("ArrowLeft");
            Assert.Equal(step * Cw, input.CaretX, 0.01f);
            logicalWhenGoingLeft.Add(input.CaretIndex);
        }

        Assert.Equal([5, 2, 3, 2, 1, 0], logicalWhenGoingLeft);
        s.Key("ArrowLeft");
        Assert.Equal(0, input.CaretX);
        for (var step = 1; step <= 6; step++)
        {
            s.Key("ArrowRight");
            Assert.Equal(step * Cw, input.CaretX, 0.01f);
        }

        // The visual right end is reachable even though the right-to-left run's start has the same logical index as another spot.
        Assert.Equal(6 * Cw, input.CaretX, 0.01f);
        // Shift+arrow selects the characters under the sweep, as a logical range.
        s.Key("Home");
        for (var i = 0; i < 3; i++)
        {
            s.Key("ArrowRight", KeyModifiers.Shift);
        }

        Assert.Equal(3 * Cw, input.CaretX, 0.01f);
        Assert.True(input.SelectionEnd > input.SelectionStart);
        // Clicking between the two right-to-left letters puts the caret there and between them in logical order.
        var label = s.Get("t/text").AbsoluteBounds;
        s.Host.Dispatch(UiEvent.PointerDown(label.X + (3 * Cw) + 0.5f, label.Y + 3));
        s.Host.Dispatch(UiEvent.PointerUp(label.X + (3 * Cw) + 0.5f, label.Y + 3));
        Assert.Equal(3 * Cw, input.CaretX, 0.01f);
    }

    [Fact]
    public void RightToLeftParagraphStartsAtTheRightEdge()
    {
        var (s, input, _) = Make("\u05D0\u05D1\u05D2");
        s.Key("Home");
        Assert.Equal(0, input.CaretIndex);
        Assert.Equal(3 * Cw, input.CaretX, 0.01f);
        s.Key("ArrowLeft");
        Assert.Equal(1, input.CaretIndex);
        Assert.Equal(2 * Cw, input.CaretX, 0.01f);
        s.Key("ArrowRight");
        Assert.Equal(0, input.CaretIndex);
        s.Key("End");
        Assert.Equal(3, input.CaretIndex);
        Assert.Equal(0, input.CaretX, 0.01f);
    }

    [Fact]
    public void PointerPlacesTheCaretAndDragSelects()
    {
        var (s, input, _) = Make("abcdefgh");
        var label = s.Get("t/text").AbsoluteBounds;
        var y = label.Y + 3;
        s.Host.Dispatch(UiEvent.PointerMove(label.X + (2 * Cw) + 1, y));
        s.Host.Dispatch(UiEvent.PointerDown(label.X + (2 * Cw) + 1, y));
        Assert.Equal(2, input.CaretIndex);
        s.Host.Dispatch(UiEvent.PointerMove(label.X + (6 * Cw) - 1, y + 200));
        s.Host.Dispatch(UiEvent.PointerUp(label.X + (6 * Cw) - 1, y + 200));
        Assert.Equal("cdef", input.SelectedText);
    }

    [Fact]
    public void MaxLengthAndValidationRejectEdits()
    {
        var (s, input, text) = Make("12", max: 4, validate: t => t.All(char.IsAsciiDigit));
        s.Type("abc");
        Assert.Equal("12", text());
        s.Type("3456");
        Assert.Equal("1234", text());
        Assert.Equal(4, input.GraphemeCount);
        s.Type("9");
        Assert.Equal("1234", text());
    }

    [Fact]
    public void AGameThatRefusesAnEditSeesItUndone()
    {
        var (s, _, text) = Make("ok", reject: "ok!");
        s.Type("!");
        Assert.Equal("ok", text());
        Assert.Equal("ok", s.Get("t/text").Text);
    }

    [Fact]
    public void ExternalChangesShowUpAndLongTextScrollsToTheCaret()
    {
        var s = new Screen();
        var text = "short";
        var input = UiKit.TextInput(() => text, v => text = v, id: "t");
        s.Root.Add(input.Width(120));
        s.Settle();
        s.Focus(input);
        text = "a much longer filter text that cannot fit";
        s.Settle();
        Assert.Equal(text, s.Get("t/text").Text);
        var viewport = s.Get("t/viewport");
        Assert.True(viewport.ScrollX > 0, "scrolled to keep the caret in view");
        var caret = s.Get("t/caret").AbsoluteBounds;
        var box = viewport.AbsoluteBounds;
        Assert.True(caret.X >= box.X - 0.5f && caret.X <= box.Right + 0.5f);
        s.Key("Home");
        s.Settle();
        Assert.Equal(0, viewport.ScrollX);
        Assert.True(input.CaretScreenRect.Height > 0);
    }

    [Fact]
    public void CaretOnlyShowsWhileFocused()
    {
        var (s, input, _) = Make("x");
        Assert.True(s.Get("t/caret").IsVisible);
        s.Ui.Focus(null);
        s.Settle();
        Assert.False(s.Get("t/caret").IsVisible);
        Assert.False(input.IsFocused);
    }
}
