namespace OneGame.UI;

/// <summary>A tree operation was invalid (adding an element that already has a parent, or creating a cycle).</summary>
public sealed class UiTreeException : InvalidOperationException
{
    /// <summary>Creates the exception with a message that says what to fix.</summary>
    public UiTreeException(string message) : base(message)
    {
    }
}

/// <summary>
/// One node of the retained UI tree. An element has a kind (its type selector), an optional id, classes, children, inline style, event handlers and bindings.
/// Changing anything marks only what it affects as dirty; <see cref="UiTree.Frame"/> restyles, lays out and repaints what changed.
/// </summary>
public partial class Element
{
    private Element? _parent;
    private List<Element>? _children;
    private List<string>? _classes;
    private StyleBuilder? _inline;
    private string? _id;
    private string? _text;
    private bool _enabled = true;
    private bool _visible = true;
    private UiTree? _ownerTree;

    /// <summary>Creates an element. <paramref name="kind"/> is what type selectors match; layout and painting do not depend on it.</summary>
    public Element(string kind = "Box")
    {
        ArgumentNullException.ThrowIfNull(kind);
        Kind = kind;
        Computed = new ComputedStyle();
        StyleDirty = true;
        StyleSubtree = true;
        LayoutDirty = true;
    }

    /// <summary>The element's type name, such as <c>Box</c>, <c>Label</c> or <c>Button</c>.</summary>
    public string Kind { get; }

    /// <summary>The id selectors and the harness find the element by; null for none.</summary>
    public string? Id
    {
        get => _id;
        set
        {
            if (!string.Equals(_id, value, StringComparison.Ordinal))
            {
                _id = value;
                MarkStyle(true);
            }
        }
    }

    /// <summary>Free-form data for the game's own use; the UI never reads it.</summary>
    public object? Tag { get; set; }

    /// <summary>What the element is for, such as <c>button</c>, <c>slider</c> or <c>row</c>. Components set it; the JSON dump prints it, so scenarios can find controls by role. It has no effect on layout or painting.</summary>
    public string? Role { get; set; }

    /// <summary>The current value a control shows or holds, as text (a slider's number, a tab's title, a toggle's <c>true</c>). Components keep it current; the JSON dump prints it.</summary>
    public string? Value { get; set; }

    /// <summary>
    /// Whether a toggle control is switched on, like WAI-ARIA <c>aria-pressed</c>: <see langword="null"/> for an element that is not a toggle, otherwise <see langword="true"/> or <see langword="false"/>.
    /// Components keep it current; the JSON dump prints it as <c>pressed</c> and scenarios check it with <c>pressed:</c>. It has no effect on painting by itself: the kit also sets the class <c>pressed</c>.
    /// </summary>
    public bool? Pressed { get; set; }

    /// <summary>The parent, or null for the root and for elements not yet added.</summary>
    public Element? Parent => _parent;

    /// <summary>The tree this element belongs to, or null when it is not attached.</summary>
    public UiTree? Tree
    {
        get
        {
            var e = this;
            while (e._parent is not null)
            {
                e = e._parent;
            }

            return e._ownerTree;
        }
    }

    /// <summary>The children in document order.</summary>
    public IReadOnlyList<Element> Children => _children ?? (IReadOnlyList<Element>)Array.Empty<Element>();

    /// <summary>The number of children.</summary>
    public int ChildCount => _children?.Count ?? 0;

    /// <summary>The child at <paramref name="index"/>.</summary>
    public Element ChildAt(int index) => (_children ?? throw new ArgumentOutOfRangeException(nameof(index)))[index];

    internal int IndexInParent => _parent?._children?.IndexOf(this) ?? 0;

    /// <summary>The element's own computed style after the last <see cref="UiTree.Frame"/>.</summary>
    public ComputedStyle Computed { get; }

    /// <summary>The text of a text element; null for a container. A text element is a leaf sized by the <see cref="ITextMeasurer"/>.</summary>
    public string? Text
    {
        get => _text;
        set
        {
            if (!string.Equals(_text, value, StringComparison.Ordinal))
            {
                _text = value;
                TextLines?.Clear();
                MarkLayout();
            }
        }
    }

    /// <summary>False disables the element and its descendants: no pointer or focus input, and <c>:disabled</c> matches the element and its descendants, as it matches the controls inside a disabled fieldset in CSS.</summary>
    public bool IsEnabled
    {
        get => _enabled;
        set
        {
            if (_enabled != value)
            {
                _enabled = value;
                MarkStyle(true);
            }
        }
    }

    /// <summary>False hides the element and its subtree like <c>display: none</c>, without touching its style.</summary>
    public bool IsVisible
    {
        get => _visible;
        set
        {
            if (_visible != value)
            {
                _visible = value;
                MarkLayout();
            }
        }
    }

    /// <summary>True when the element can take keyboard or gamepad focus (while enabled and shown).</summary>
    public bool IsFocusable { get; set; }

    /// <summary>When true, directional and Tab navigation stays inside this element while it is shown (a modal dialog).</summary>
    public bool TrapsFocus { get; set; }

    /// <summary>Lower values come first in Tab order among focusable elements; zero (the default) follows document order after all positive values, as in HTML.</summary>
    public int TabIndex { get; set; }

    /// <summary>True when the element has keyboard focus.</summary>
    public bool IsFocused => FocusFlag;

    /// <summary>True when the pointer is over the element or one of its descendants.</summary>
    public bool IsHovered => HoverChain;

    /// <summary>True while a pointer button pressed on the element or one of its descendants is still down.</summary>
    public bool IsPressed => ActiveChain;

    internal bool HoverChain;
    internal bool ActiveChain;
    internal bool FocusFlag;

    /// <summary>True when the element and all its ancestors are enabled.</summary>
    public bool IsEffectivelyEnabled
    {
        get
        {
            for (var e = this; e is not null; e = e._parent)
            {
                if (!e._enabled)
                {
                    return false;
                }
            }

            return true;
        }
    }

    // ---- structure -----------------------------------------------------------------------------------------------

    /// <summary>Appends a child.</summary>
    /// <exception cref="UiTreeException">The child already has a parent, or is this element or one of its ancestors.</exception>
    public Element Add(Element child) => Insert(ChildCount, child);

    /// <summary>Inserts a child at <paramref name="index"/>.</summary>
    /// <exception cref="UiTreeException">The child already has a parent, or is this element or one of its ancestors.</exception>
    public Element Insert(int index, Element child)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (child._parent is not null)
        {
            throw new UiTreeException($"Element '{child.Describe()}' already has a parent; remove it first.");
        }

        for (var a = this; a is not null; a = a._parent)
        {
            if (a == child)
            {
                throw new UiTreeException($"Cannot add '{child.Describe()}' below itself.");
            }
        }

        if (child._ownerTree is not null)
        {
            throw new UiTreeException("The tree root cannot be added to another element.");
        }

        _children ??= [];
        _children.Insert(index, child);
        child._parent = this;
        child.MarkStyle(true);
        child.MarkLayout();
        StructureChanged();
        return this;
    }

    /// <summary>Removes a child; returns false when it is not a child of this element.</summary>
    public bool Remove(Element child)
    {
        if (child is null || child._parent != this || _children is null)
        {
            return false;
        }

        _children.Remove(child);
        child._parent = null;
        MarkLayout();
        StructureChanged();
        return true;
    }

    /// <summary>Removes every child.</summary>
    public void Clear()
    {
        if (_children is null || _children.Count == 0)
        {
            return;
        }

        foreach (var c in _children)
        {
            c._parent = null;
        }

        _children.Clear();
        MarkLayout();
        StructureChanged();
    }

    /// <summary>Removes this element from its parent, if it has one.</summary>
    public void RemoveFromParent() => _parent?.Remove(this);

    /// <summary>
    /// Makes the children exactly <paramref name="wanted"/>, in order, reusing the ones already present and touching only the difference.
    /// Elements that are no longer wanted are removed.
    /// </summary>
    public void SetChildren(IReadOnlyList<Element> wanted)
    {
        ArgumentNullException.ThrowIfNull(wanted);
        var have = _children ??= [];
        // Drop children that are not wanted.
        for (var i = have.Count - 1; i >= 0; i--)
        {
            var found = false;
            for (var j = 0; j < wanted.Count; j++)
            {
                if (ReferenceEquals(wanted[j], have[i]))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                have[i]._parent = null;
                have.RemoveAt(i);
            }
        }

        // Put the rest in order.
        for (var i = 0; i < wanted.Count; i++)
        {
            var w = wanted[i];
            if (i < have.Count && ReferenceEquals(have[i], w))
            {
                continue;
            }

            if (w._parent == this)
            {
                have.Remove(w);
                have.Insert(i, w);
            }
            else
            {
                if (w._parent is not null)
                {
                    throw new UiTreeException($"Element '{w.Describe()}' already has a parent; remove it first.");
                }

                have.Insert(i, w);
                w._parent = this;
                w.MarkStyle(true);
                w.MarkLayout();
            }
        }

        MarkLayout();
        StructureChanged();
    }

    private void StructureChanged()
    {
        var tree = Tree;
        if (tree is not null && tree.StructuralSelectors && _children is not null)
        {
            foreach (var c in _children)
            {
                c.MarkStyle(false);
            }
        }

        MarkPaint();
    }

    // ---- classes -------------------------------------------------------------------------------------------------

    /// <summary>The classes on the element, in the order added.</summary>
    public IReadOnlyList<string> Classes => _classes ?? (IReadOnlyList<string>)Array.Empty<string>();

    /// <summary>True when the element has the class.</summary>
    public bool HasClass(string name)
    {
        if (_classes is null)
        {
            return false;
        }

        foreach (var c in _classes)
        {
            if (string.Equals(c, name, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Adds or removes a class. Does nothing when the element already is in that state.</summary>
    public Element SetClass(string name, bool on)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        if (HasClass(name) == on)
        {
            return this;
        }

        if (on)
        {
            (_classes ??= []).Add(name);
        }
        else
        {
            _classes!.Remove(name);
        }

        MarkStyle(Tree?.AncestorSelectors ?? true);
        return this;
    }

    // ---- style ---------------------------------------------------------------------------------------------------

    internal StyleBuilder Inline => _inline ??= new StyleBuilder(this);

    internal StyleBuilder? InlineOrNull => _inline;

    internal void InlineStyleChanged(Declaration d) => MarkStyle(d.IsToken);

    internal string Describe() => Id is null ? Kind : $"{Kind}#{Id}";

    /// <inheritdoc/>
    public override string ToString() => Describe();

    // ---- dirty tracking ------------------------------------------------------------------------------------------

    internal (string Name, StyleValue Value)[]? Tokens;
    internal bool StyleDirty;
    internal bool StyleSubtree;
    internal bool DescendantStyleDirty;
    internal bool LayoutDirty;

    internal void MarkStyle(bool subtree)
    {
        StyleDirty = true;
        if (subtree)
        {
            StyleSubtree = true;
        }

        var e = this;
        while (e._parent is { } p)
        {
            p.DescendantStyleDirty = true;
            e = p;
        }

        e._ownerTree?.Dirty();
    }

    internal void MarkLayout()
    {
        var e = this;
        e.LayoutDirty = true;
        e.ClearCache();
        while (e._parent is { } p)
        {
            p.LayoutDirty = true;
            p.ClearCache();
            e = p;
        }

        e._ownerTree?.Dirty();
    }

    internal void MarkPaint()
    {
        var e = this;
        while (e._parent is { } p)
        {
            e = p;
        }

        e._ownerTree?.Dirty();
    }

    internal void SetOwner(UiTree tree) => _ownerTree = tree;

    // ---- walking -------------------------------------------------------------------------------------------------

    /// <summary>True when <paramref name="other"/> is this element or lies below it.</summary>
    public bool Contains(Element? other)
    {
        for (var e = other; e is not null; e = e._parent)
        {
            if (e == this)
            {
                return true;
            }
        }

        return false;
    }
}
