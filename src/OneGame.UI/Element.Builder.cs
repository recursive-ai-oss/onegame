namespace OneGame.UI;

public partial class Element
{
    private List<Binding>? _bindings;
    private List<(UiEventKind Kind, UiHandler Handler)>? _handlers;

    internal List<Binding>? Bindings => _bindings;

    // ---- identity and structure ----------------------------------------------------------------------------------

    /// <summary>Sets the id and returns the element, for chaining.</summary>
    public Element Named(string id)
    {
        Id = id;
        return this;
    }

    /// <summary>Adds one or more classes, separated by spaces.</summary>
    public Element Class(string names)
    {
        ArgumentNullException.ThrowIfNull(names);
        foreach (var n in names.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            SetClass(n, true);
        }

        return this;
    }

    /// <summary>Appends children and returns this element.</summary>
    public Element With(params Element[] children)
    {
        ArgumentNullException.ThrowIfNull(children);
        foreach (var c in children)
        {
            Add(c);
        }

        return this;
    }

    /// <summary>Makes the element take focus from Tab, gamepad and pointer input.</summary>
    public Element Focusable(bool on = true)
    {
        IsFocusable = on;
        return this;
    }

    /// <summary>Enables or disables the element (and its descendants).</summary>
    public Element Enabled(bool on)
    {
        IsEnabled = on;
        return this;
    }

    /// <summary>Sets the tab order.</summary>
    public Element Tab(int tabIndex)
    {
        TabIndex = tabIndex;
        return this;
    }

    /// <summary>Makes focus navigation stay within this element while it is shown.</summary>
    public Element TrapFocus(bool on = true)
    {
        TrapsFocus = on;
        return this;
    }

    // ---- inline style --------------------------------------------------------------------------------------------

    /// <summary>Applies inline style declarations. Inline style beats every style sheet rule.</summary>
    public Element Style(Action<StyleBuilder> declare)
    {
        ArgumentNullException.ThrowIfNull(declare);
        declare(Inline);
        return this;
    }

    /// <summary>Sets <c>width</c>.</summary>
    public Element Width(Length v) => Style(s => s.Width(v));

    /// <summary>Sets <c>height</c>.</summary>
    public Element Height(Length v) => Style(s => s.Height(v));

    /// <summary>Sets <c>width</c> and <c>height</c>.</summary>
    public Element Size(Length w, Length h) => Style(s => s.Size(w, h));

    /// <summary>Sets <c>min-width</c>.</summary>
    public Element MinWidth(Length v) => Style(s => s.MinWidth(v));

    /// <summary>Sets <c>max-width</c>.</summary>
    public Element MaxWidth(Length v) => Style(s => s.MaxWidth(v));

    /// <summary>Sets <c>padding</c> on all sides.</summary>
    public Element Padding(Length v) => Style(s => s.Padding(v));

    /// <summary>Sets vertical and horizontal <c>padding</c>.</summary>
    public Element Padding(Length vertical, Length horizontal) => Style(s => s.Padding(vertical, horizontal));

    /// <summary>Sets <c>margin</c> on all sides.</summary>
    public Element Margin(Length v) => Style(s => s.Margin(v));

    /// <summary>Sets vertical and horizontal <c>margin</c>.</summary>
    public Element Margin(Length vertical, Length horizontal) => Style(s => s.Margin(vertical, horizontal));

    /// <summary>Sets both gaps.</summary>
    public Element Gap(Length v) => Style(s => s.Gap(v));

    /// <summary>Lays children out left to right.</summary>
    public Element Row() => Style(s => s.Direction(FlexDirection.Row));

    /// <summary>Lays children out top to bottom.</summary>
    public Element Column() => Style(s => s.Direction(FlexDirection.Column));

    /// <summary>Lets children wrap onto further lines.</summary>
    public Element Wrap() => Style(s => s.Wrap(FlexWrap.Wrap));

    /// <summary>Sets <c>flex-grow</c>.</summary>
    public Element Grow(float v = 1) => Style(s => s.Grow(v));

    /// <summary>Sets <c>flex-basis</c>.</summary>
    public Element Basis(Length v) => Style(s => s.Basis(v));

    /// <summary>Sets <c>flex-shrink</c>.</summary>
    public Element Shrink(float v) => Style(s => s.Shrink(v));

    /// <summary>Sets <c>justify-content</c>.</summary>
    public Element Justify(Align v) => Style(s => s.JustifyContent(v));

    /// <summary>Sets <c>align-items</c>.</summary>
    public Element Align(Align v) => Style(s => s.AlignItems(v));

    /// <summary>Sets <c>align-self</c>.</summary>
    public Element AlignSelf(Align v) => Style(s => s.AlignSelf(v));

    /// <summary>Sets <c>background-color</c>.</summary>
    public Element Background(Color v) => Style(s => s.Background(v));

    /// <summary>Sets the inherited text color.</summary>
    public Element TextColor(Color v) => Style(s => s.TextColor(v));

    /// <summary>Sets <c>font-size</c>.</summary>
    public Element FontSize(Length v) => Style(s => s.FontSize(v));

    /// <summary>Rounds all corners.</summary>
    public Element Radius(Length v) => Style(s => s.Radius(v));

    /// <summary>Draws a border on all sides.</summary>
    public Element Border(Length width, Color color) => Style(s => s.Border(width, color));

    /// <summary>Sets <c>overflow</c> on both axes.</summary>
    public Element Overflow(OverflowMode v) => Style(s => s.Overflow(v));

    /// <summary>Pins the element at a point of its parent (or the screen); see <see cref="StyleBuilder.Anchor"/>.</summary>
    public Element Anchor(AnchorPoint point, Length x = default, Length y = default, bool toScreen = false) => Style(s => s.Anchor(point, x, y, toScreen));

    // ---- bindings ------------------------------------------------------------------------------------------------

    private Element Bind(Binding b)
    {
        (_bindings ??= []).Add(b);
        return this;
    }

    /// <summary>Sets <see cref="Text"/> from a lambda, re-evaluated every frame. Returning a cached string keeps frames allocation-free.</summary>
    public Element BindText(Func<string> source) => Bind(new TextBinding(source ?? throw new ArgumentNullException(nameof(source))));

    /// <summary>Sets <see cref="Text"/> from <paramref name="format"/> applied to a value that is re-read every frame; the text is rebuilt only when the value changes.</summary>
    public Element BindText<T>(Func<T> source, Func<T, string> format)
        where T : IEquatable<T> =>
        Bind(new TextBinding<T>(source ?? throw new ArgumentNullException(nameof(source)), format ?? throw new ArgumentNullException(nameof(format))));

    /// <summary>Turns a class on or off from a lambda.</summary>
    public Element BindClass(string name, Func<bool> on) => Bind(new ClassBinding(name, on ?? throw new ArgumentNullException(nameof(on))));

    /// <summary>Shows or hides the element from a lambda.</summary>
    public Element BindVisible(Func<bool> visible) => Bind(new VisibleBinding(visible ?? throw new ArgumentNullException(nameof(visible))));

    /// <summary>Enables or disables the element from a lambda.</summary>
    public Element BindEnabled(Func<bool> enabled) => Bind(new EnabledBinding(enabled ?? throw new ArgumentNullException(nameof(enabled))));

    /// <summary>Re-applies inline style every frame, for values that follow game state; unchanged values cost nothing.</summary>
    public Element BindStyle(Action<StyleBuilder> apply) => Bind(new StyleBinding(apply ?? throw new ArgumentNullException(nameof(apply))));

    /// <summary>Runs a lambda against the element every frame, before styles are resolved.</summary>
    public Element OnUpdate(Action<Element> update) => Bind(new UpdateBinding(update ?? throw new ArgumentNullException(nameof(update))));

    /// <summary>Keeps the children in step with a list: <paramref name="build"/> runs once per distinct item, and an item keeps its element while it stays in the list.</summary>
    public Element BindChildren<T>(Func<IReadOnlyList<T>> items, Func<T, Element> build) =>
        Bind(new RepeatBinding<T>(items ?? throw new ArgumentNullException(nameof(items)), build ?? throw new ArgumentNullException(nameof(build))));

    // ---- events --------------------------------------------------------------------------------------------------

    /// <summary>Attaches a handler for an event kind. Returning true marks the event handled.</summary>
    public Element On(UiEventKind kind, UiHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        (_handlers ??= []).Add((kind, handler));
        return this;
    }

    /// <summary>Runs <paramref name="action"/> on click or activation.</summary>
    public Element OnClick(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return On(UiEventKind.Click, (_, _) =>
        {
            action();
            return true;
        });
    }

    /// <summary>Runs <paramref name="action"/> with this element on click or activation.</summary>
    public Element OnClick(Action<Element> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return On(UiEventKind.Click, (t, _) =>
        {
            action(t);
            return true;
        });
    }

    /// <summary>Runs <paramref name="action"/> when the pointer enters the element.</summary>
    public Element OnPointerEnter(Action action) => OnSimple(UiEventKind.PointerEnter, action);

    /// <summary>Runs <paramref name="action"/> when the pointer leaves the element.</summary>
    public Element OnPointerLeave(Action action) => OnSimple(UiEventKind.PointerLeave, action);

    /// <summary>Runs <paramref name="action"/> when the element gains focus.</summary>
    public Element OnFocus(Action action) => OnSimple(UiEventKind.Focus, action);

    /// <summary>Runs <paramref name="action"/> when the element loses focus.</summary>
    public Element OnBlur(Action action) => OnSimple(UiEventKind.Blur, action);

    /// <summary>Runs <paramref name="action"/> when the back button is pressed while focus is in this element.</summary>
    public Element OnCancel(Action action) => OnSimple(UiEventKind.Cancel, action);

    private Element OnSimple(UiEventKind kind, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return On(kind, (_, _) =>
        {
            action();
            return true;
        });
    }

    internal bool HasHandlers => _handlers is { Count: > 0 };

    // Runs the handlers for one event on this element; true when one of them handled it.
    internal bool Raise(in UiEvent e)
    {
        if (_handlers is null)
        {
            return false;
        }

        var handled = false;
        for (var i = 0; i < _handlers.Count; i++)
        {
            var (kind, handler) = _handlers[i];
            if (kind == e.Kind)
            {
                handled |= handler(this, e);
            }
        }

        return handled;
    }
}
