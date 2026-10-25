namespace OneGame.UI;

public partial class Element
{
    /// <summary>
    /// True for elements that take typed text (a text field). While one has focus the host tells the platform that text input is wanted, so an
    /// on-screen keyboard or input method can appear, and it sends <see cref="UiEventKind.Composition"/> events to the element.
    /// </summary>
    public bool AcceptsText { get; set; }

    /// <summary>
    /// Where an input method should place its candidate window: the caret's rectangle in screen pixels. When null, the element's whole bounds are used.
    /// A text field updates it as the caret moves.
    /// </summary>
    public Rect? CaretArea { get; set; }
}

public sealed partial class UiTree
{
    /// <summary>The focused element when it takes typed text (<see cref="Element.AcceptsText"/>), otherwise null.</summary>
    public Element? TextInputTarget => _focused is { AcceptsText: true } f ? f : null;

    /// <summary>The rectangle (screen pixels) an input method should avoid covering for <see cref="TextInputTarget"/>: its caret, or its bounds.</summary>
    public Rect? TextInputArea => TextInputTarget is { } t ? t.CaretArea ?? t.AbsoluteBounds : null;
}
