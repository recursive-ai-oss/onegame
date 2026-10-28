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

    /// <summary>
    /// Sets <c>width</c>. A bare number is pixels; <see cref="Length.Percent"/> is of the parent, <see cref="Length.Ui"/> follows
    /// the UI scale.
    /// </summary>
    public Element Width(Length v) => Style(s => s.Width(v));

    /// <summary>Sets <c>height</c>; units as for <see cref="Width"/>.</summary>
    public Element Height(Length v) => Style(s => s.Height(v));

    /// <summary>Sets <c>width</c> and <c>height</c>.</summary>
    public Element Size(Length w, Length h) => Style(s => s.Size(w, h));

    /// <summary>Sets <c>min-width</c>.</summary>
    public Element MinWidth(Length v) => Style(s => s.MinWidth(v));

    /// <summary>Sets <c>max-width</c>.</summary>
    public Element MaxWidth(Length v) => Style(s => s.MaxWidth(v));

    /// <summary>Sets <c>padding</c> on all four sides: space inside the border, around the content.</summary>
    public Element Padding(Length v) => Style(s => s.Padding(v));

    /// <summary>Sets vertical and horizontal <c>padding</c>.</summary>
    public Element Padding(Length vertical, Length horizontal) => Style(s => s.Padding(vertical, horizontal));

    /// <summary>Sets <c>margin</c> on all four sides: space outside the border, around the element.</summary>
    public Element Margin(Length v) => Style(s => s.Margin(v));

    /// <summary>Sets vertical and horizontal <c>margin</c>.</summary>
    public Element Margin(Length vertical, Length horizontal) => Style(s => s.Margin(vertical, horizontal));

    /// <summary>Sets the space between children, on both axes (CSS <c>gap</c>), so spacing needs no margins on the children.</summary>
    /// <param name="v">The gap. Use <see cref="Length.Ui"/> to follow the UI scale.</param>
    public Element Gap(Length v) => Style(s => s.Gap(v));

    /// <summary>Lays children out left to right.</summary>
    public Element Row() => Style(s => s.Direction(FlexDirection.Row));

    /// <summary>Lays children out top to bottom.</summary>
    public Element Column() => Style(s => s.Direction(FlexDirection.Column));

    /// <summary>Lets children wrap onto further lines.</summary>
    public Element Wrap() => Style(s => s.Wrap(FlexWrap.Wrap));

    /// <summary>
    /// Sets <c>flex-grow</c>: the share of the parent's free space this element takes along the main axis. 0 keeps its natural size;
    /// two siblings with 1 and 2 split the free space one to two.
    /// </summary>
    /// <param name="v">The weight.</param>
    public Element Grow(float v = 1) => Style(s => s.Grow(v));

    /// <summary>
    /// Sets <c>flex-basis</c>: the size along the main axis before free space is shared out. <see cref="Length.Auto"/> uses the
    /// width or height.
    /// </summary>
    public Element Basis(Length v) => Style(s => s.Basis(v));

    /// <summary>
    /// Sets <c>flex-shrink</c>: how much this element gives up, relative to its siblings, when the children do not fit. 0 never
    /// shrinks it.
    /// </summary>
    public Element Shrink(float v) => Style(s => s.Shrink(v));

    /// <summary>
    /// Sets <c>justify-content</c>: where children sit along the main axis (the row's width, the column's height) when they do not
    /// fill it.
    /// </summary>
    public Element Justify(Align v) => Style(s => s.JustifyContent(v));

    /// <summary>
    /// Sets <c>align-items</c>: where children sit across the main axis (a row's children vertically). A child can override it with
    /// <see cref="AlignSelf"/>.
    /// </summary>
    public Element Align(Align v) => Style(s => s.AlignItems(v));

    /// <summary>
    /// Sets <c>align-self</c>: this element's own position across its parent's main axis, overriding the parent's
    /// <see cref="Align"/>.
    /// </summary>
    public Element AlignSelf(Align v) => Style(s => s.AlignSelf(v));

    /// <summary>Sets <c>background-color</c>, painted inside the border box under the children.</summary>
    public Element Background(Color v) => Style(s => s.Background(v));

    /// <summary>Sets <c>color</c>, the text color. It is inherited, so setting it on a screen's root colors every label in it.</summary>
    public Element TextColor(Color v) => Style(s => s.TextColor(v));

    /// <summary>Sets <c>font-size</c>, inherited by descendants. Pixels are device pixels; <see cref="Length.Ui"/> follows the UI scale.</summary>
    public Element FontSize(Length v) => Style(s => s.FontSize(v));

    /// <summary>Rounds all four corners of the background and border.</summary>
    public Element Radius(Length v) => Style(s => s.Radius(v));

    /// <summary>Draws a solid border on all four sides.</summary>
    /// <param name="width">The line width.</param>
    /// <param name="color">The line color.</param>
    public Element Border(Length width, Color color) => Style(s => s.Border(width, color));

    /// <summary>Sets <c>overflow</c> on both axes: whether content past the box is shown, clipped or scrolled.</summary>
    public Element Overflow(OverflowMode v) => Style(s => s.Overflow(v));

    /// <summary>Pins the element at a point of its parent (or the screen), out of the normal flow; see <see cref="StyleBuilder.Anchor"/>.</summary>
    /// <param name="point">Which corner, edge middle or centre of the parent the element attaches to.</param>
    /// <param name="x">Horizontal offset from that point, away from the edge.</param>
    /// <param name="y">Vertical offset from that point, away from the edge.</param>
    /// <param name="toScreen">True to anchor to the whole screen instead of the parent.</param>
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
    /// <param name="name">The class to switch.</param>
    /// <param name="on">Returns true to have the class.</param>
    public Element BindClass(string name, Func<bool> on) => Bind(new ClassBinding(name, on ?? throw new ArgumentNullException(nameof(on))));

    /// <summary>Shows or hides the element from a lambda that is re-read every frame (a hidden element takes no space).</summary>
    /// <param name="visible">Returns true to show. Keep it cheap and free of side effects.</param>
    public Element BindVisible(Func<bool> visible) => Bind(new VisibleBinding(visible ?? throw new ArgumentNullException(nameof(visible))));

    /// <summary>Enables or disables the element from a lambda re-read every frame; a disabled element and its descendants ignore pointer and focus input and match <c>:disabled</c> in style rules (the kit themes dim them).</summary>
    /// <param name="enabled">Returns true when the element should react.</param>
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
    /// <param name="action">Runs on a pointer click, a touch tap, or Enter / the gamepad's confirm button while the element has focus. A view handler: send a command here instead of writing synced state.</param>
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
