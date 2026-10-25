namespace OneGame.UI.Kit;

/// <summary>The look of a <see cref="UiKit.Button(string, Action, string?, ButtonKind)"/>.</summary>
public enum ButtonKind
{
    /// <summary>The ordinary raised button.</summary>
    Normal,
    /// <summary>The accent-colored call to action (adds the class <c>primary</c>).</summary>
    Primary,
    /// <summary>A destructive action such as delete or sell everything (adds the class <c>danger</c>).</summary>
    Danger,
    /// <summary>No background until hovered or focused (adds the class <c>flat</c>).</summary>
    Flat,
}

/// <summary>One option of a radio group or drop-down: the value it stands for and the text shown.</summary>
/// <typeparam name="T">The value type.</typeparam>
/// <param name="Value">The value the option stands for.</param>
/// <param name="Label">The text shown for it.</param>
public sealed record Choice<T>(T Value, string Label);

/// <summary>A page of <see cref="UiKit.Tabs"/>: the tab's title and the content shown while it is selected.</summary>
/// <param name="Title">The text on the tab.</param>
/// <param name="Content">The content, built once; it stays in the tree (hidden) while another tab is selected, so its state survives.</param>
/// <param name="Id">The id of the tab (the content keeps its own id); null for none.</param>
public sealed record TabPage(string Title, Element Content, string? Id = null);

public static partial class UiKit
{
    // ---- button ----------------------------------------------------------------------------------------------------

    /// <summary>A button (kind <c>Button</c>, role <c>button</c>) that runs <paramref name="onClick"/> when clicked, or activated with Enter, Space or the confirm button.</summary>
    /// <param name="text">The label.</param>
    /// <param name="onClick">Called on activation. A disabled button (<see cref="Element.IsEnabled"/> false) never calls it.</param>
    /// <param name="id">The id, or null.</param>
    /// <param name="kind">The look.</param>
    public static Element Button(string text, Action onClick, string? id = null, ButtonKind kind = ButtonKind.Normal)
    {
        ArgumentNullException.ThrowIfNull(text);
        var b = ButtonShell(onClick, id, kind);
        b.Value = text;
        b.Add(Ui.Label(text).Class("kit-button-label"));
        return b;
    }

    /// <summary>A button whose label follows a lambda, such as a price that changes. See <see cref="Button(string, Action, string?, ButtonKind)"/>.</summary>
    public static Element Button(Func<string> text, Action onClick, string? id = null, ButtonKind kind = ButtonKind.Normal)
    {
        ArgumentNullException.ThrowIfNull(text);
        var b = ButtonShell(onClick, id, kind);
        var label = Ui.Label(text).Class("kit-button-label");
        b.Add(label);
        b.OnUpdate(e => e.Value = label.Text);
        return b;
    }

    /// <summary>
    /// A toggle button (kind <c>Button</c>, role <c>button</c>) that is switched on or off, like a button with <c>aria-pressed</c>: a "FAST" speed switch, a bold-text toggle, a filter chip.
    /// While on it has the class <c>pressed</c> (the kit draws an accent-tinted fill and outline), <see cref="Element.Pressed"/> is <see langword="true"/>, and the JSON dump shows <c>"pressed": true</c>.
    /// </summary>
    /// <param name="text">The label.</param>
    /// <param name="get">Reads the state, every frame.</param>
    /// <param name="set">Called with the new state when the player activates the button.</param>
    /// <param name="id">The id, or null.</param>
    /// <param name="kind">The look; the switched-on tint replaces the variant's fill.</param>
    public static Element ToggleButton(string text, Func<bool> get, Action<bool> set, string? id = null, ButtonKind kind = ButtonKind.Normal)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(get);
        ArgumentNullException.ThrowIfNull(set);
        var b = ButtonShell(() => set(!get()), id, kind);
        b.Value = text;
        b.Add(Ui.Label(text).Class("kit-button-label"));
        b.OnUpdate(e =>
        {
            var on = get();
            e.Pressed = on;
            e.SetClass("pressed", on);
        });
        b.Pressed = get();
        b.SetClass("pressed", b.Pressed.Value);
        return b;
    }

    private static Element ButtonShell(Action onClick, string? id, ButtonKind kind)
    {
        ArgumentNullException.ThrowIfNull(onClick);
        var b = Make("Button", "button", "kit-button", id, focusable: true);
        switch (kind)
        {
            case ButtonKind.Primary: b.SetClass("primary", true); break;
            case ButtonKind.Danger: b.SetClass("danger", true); break;
            case ButtonKind.Flat: b.SetClass("flat", true); break;
            default: break;
        }

        return b.OnClick(onClick);
    }

    // ---- checkbox ----------------------------------------------------------------------------------------------------

    /// <summary>
    /// A checkbox with a label (kind <c>Toggle</c>, role <c>checkbox</c>). While checked it has the class <c>checked</c> and <see cref="Element.Value"/> is <c>true</c>.
    /// </summary>
    /// <param name="label">The text beside the box.</param>
    /// <param name="get">Reads the state, every frame.</param>
    /// <param name="set">Called with the new state when the player toggles it.</param>
    /// <param name="id">The id, or null.</param>
    public static Element Toggle(string label, Func<bool> get, Action<bool> set, string? id = null)
    {
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(get);
        ArgumentNullException.ThrowIfNull(set);
        var root = Make("Toggle", "checkbox", "kit-toggle", id, focusable: true);
        var box = Part("Box", "kit-toggle-box", id, "box");
        var mark = Part("Box", "kit-toggle-mark", id, "mark");
        box.Add(mark);
        root.With(box, Ui.Label(label).Class("kit-toggle-label"));
        root.OnClick(() => set(!get()));
        root.OnUpdate(e =>
        {
            var on = get();
            e.SetClass("checked", on);
            box.SetClass("checked", on);
            mark.SetClass("checked", on);
            e.Value = BoolText(on);
        });
        return root;
    }

    // ---- radio group -------------------------------------------------------------------------------------------------

    /// <summary>
    /// A group of mutually exclusive options (kind <c>RadioGroup</c>, role <c>radiogroup</c>; each option is kind <c>Radio</c>, role <c>radio</c>, ids <c>id/0</c>, <c>id/1</c>...).
    /// Only the selected option takes Tab focus; the arrow keys and the d-pad move to the neighbor and select it, and the group is left with the other directions.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="choices">The options, in order.</param>
    /// <param name="get">Reads the selected value, every frame.</param>
    /// <param name="set">Called with the value of the option the player picks.</param>
    /// <param name="id">The id, or null.</param>
    /// <param name="horizontal">Lay the options out in a row; the default is a column.</param>
    public static Element RadioGroup<T>(IReadOnlyList<Choice<T>> choices, Func<T> get, Action<T> set, string? id = null, bool horizontal = false)
    {
        ArgumentNullException.ThrowIfNull(choices);
        ArgumentNullException.ThrowIfNull(get);
        ArgumentNullException.ThrowIfNull(set);
        var group = Make("RadioGroup", "radiogroup", horizontal ? "kit-radiogroup horizontal" : "kit-radiogroup", id);
        var radios = new Element[choices.Count];
        for (var i = 0; i < radios.Length; i++)
        {
            var index = i;
            var radio = Make("Radio", "radio", "kit-radio", id is null ? null : id + "/" + Invariant(i), focusable: true);
            radio.Value = choices[i].Label;
            var dot = Part("Box", "kit-radio-dot", radio.Id, "dot");
            var ring = Part("Box", "kit-radio-ring", radio.Id, "ring");
            ring.Add(dot);
            radio.With(ring, Ui.Label(choices[i].Label).Class("kit-radio-label"));
            radio.OnClick(() =>
            {
                set(choices[index].Value);
                // Only the selected option is focusable, so a click on another one would otherwise drop focus.
                radio.IsFocusable = true;
                radio.Tree?.Focus(radio);
            });
            radios[i] = radio;
            group.Add(radio);
        }

        group.OnUpdate(g =>
        {
            var current = get();
            var sel = -1;
            for (var i = 0; i < radios.Length; i++)
            {
                if (EqualityComparer<T>.Default.Equals(current, choices[i].Value))
                {
                    sel = i;
                    break;
                }
            }

            for (var i = 0; i < radios.Length; i++)
            {
                var on = i == sel;
                radios[i].SetClass("checked", on);
                radios[i].Children[0].SetClass("checked", on);
                radios[i].Children[0].Children[0].SetClass("checked", on);
                radios[i].IsFocusable = on || (sel < 0 && i == 0);
            }

            g.Value = sel < 0 ? null : choices[sel].Label;
        });
        NavigationKeys.OnStep(group, horizontal, delta =>
        {
            var focused = group.Tree?.Focused;
            var at = focused is null ? -1 : Array.IndexOf(radios, focused);
            var to = at + delta;
            if (at < 0 || to < 0 || to >= radios.Length)
            {
                return false;
            }

            radios[to].IsFocusable = true;
            set(choices[to].Value);
            group.Tree!.Focus(radios[to]);
            return true;
        });
        return group;
    }

    // ---- progress ----------------------------------------------------------------------------------------------------

    /// <summary>A progress bar (kind <c>Progress</c>, role <c>progressbar</c>). <see cref="Element.Value"/> is the fraction filled, such as <c>0.45</c>.</summary>
    /// <param name="value">Reads the current amount, every frame.</param>
    /// <param name="max">The amount that means full.</param>
    /// <param name="id">The id, or null.</param>
    public static Element Progress(Func<float> value, float max = 1, string? id = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!(max > 0))
        {
            throw new ArgumentOutOfRangeException(nameof(max), "The full amount must be greater than zero.");
        }

        var root = Make("Progress", "progressbar", "kit-progress", id);
        var fill = Part("Box", "kit-progress-fill", id, "fill");
        root.Add(fill);
        var last = float.NaN;
        root.OnUpdate(e =>
        {
            var f = Math.Clamp(value() / max, 0f, 1f);
            if (f != last)
            {
                last = f;
                fill.Width(Length.Percent(f * 100));
                e.Value = Invariant(f);
            }
        });
        return root;
    }

    // ---- card and frame ----------------------------------------------------------------------------------------------

    /// <summary>A card (kind <c>Card</c>, role <c>card</c>): a raised panel with an optional title above its content.</summary>
    /// <param name="title">The heading, or null for none.</param>
    /// <param name="content">The content, laid out top to bottom.</param>
    public static Element Card(string? title, params Element[] content)
    {
        var card = Make("Card", "card", "kit-card", null);
        card.Value = title;
        if (title is not null)
        {
            card.Add(Ui.Label(title).Class("kit-card-title"));
        }

        var body = new Element("Box").Class("kit-card-body");
        body.With(content);
        return card.Add(body);
    }

    /// <summary>
    /// A panel framed with nine-slice art (kind <c>Frame</c>): the image <paramref name="artId"/> is stretched so its corners keep their size, and
    /// the padding is the slice so content stays inside the border. Register the art with the atlas (<see cref="KitArt.Frame"/> is one to start with).
    /// </summary>
    /// <param name="artId">The asset id registered in the art atlas.</param>
    /// <param name="slice">The width of the unstretched border, in UI units (the insets the art was registered with, at scale 1).</param>
    /// <param name="content">The content, laid out top to bottom.</param>
    public static Element Frame(string artId, float slice, params Element[] content)
    {
        ArgumentException.ThrowIfNullOrEmpty(artId);
        var f = Make("Frame", "group", "kit-frame", null);
        f.Style(s => s.BackgroundImage(artId).BackgroundSlice(Length.Ui(slice)).Padding(Length.Ui(slice)));
        return f.With(content);
    }
}
