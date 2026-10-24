namespace OneGame.UI;

/// <summary>
/// A UI: a root element, the style sheets that apply to it, the screen size and scale, and the per-frame pipeline
/// (bindings, then style, then layout, then the display list). Nothing runs until <see cref="Frame"/> or <see cref="Update"/> is called,
/// and a frame in which nothing changed does no work and allocates nothing.
/// </summary>
public sealed partial class UiTree
{
    private readonly List<StyleSheet> _sheets = [];
    private readonly StyleValue[] _scratch = new StyleValue[(int)StyleProp.Count];
    private readonly List<(Rule Rule, int Order)> _matched = [];
    private readonly List<(string Name, StyleValue Value)> _tokens = [];
    private readonly FlexEngine _engine;
    private readonly Painter _painter;
    private readonly DisplayList _list = new();
    private ITextMeasurer _measurer;
    private float _scale = 1;
    private float _width;
    private float _height;
    private bool _dirty = true;
    private bool _needsPaint = true;

    /// <summary>Creates a UI with a 1280×720 screen at scale 1.</summary>
    /// <param name="measurer">Measures text for layout; defaults to <see cref="MonospaceTextMeasurer"/> for headless use.</param>
    public UiTree(ITextMeasurer? measurer = null)
    {
        _measurer = measurer ?? new MonospaceTextMeasurer();
        _engine = new FlexEngine(this);
        _painter = new Painter(this);
        Root = new Element("Root");
        Root.SetOwner(this);
        _width = 1280;
        _height = 720;
    }

    /// <summary>The root element. Its size is the screen size; add the screens as its children.</summary>
    public Element Root { get; }

    /// <summary>Screen width in pixels.</summary>
    public float Width => _width;

    /// <summary>Screen height in pixels.</summary>
    public float Height => _height;

    /// <summary>Pixels per UI unit (the <c>ui</c> length unit), for example 2 on a 4K display with a 1920×1080 design.</summary>
    public float Scale => _scale;

    /// <summary>The text measurer. Replace it when the text system becomes available; all layout is redone.</summary>
    public ITextMeasurer Measurer
    {
        get => _measurer;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _measurer = value;
            InvalidateAllLayout();
        }
    }

    /// <summary>The display list produced by the last <see cref="Frame"/>.</summary>
    public DisplayList DisplayList => _list;

    /// <summary>True when any selector in any sheet uses a combinator, so a class or state change can restyle descendants.</summary>
    internal bool AncestorSelectors { get; private set; }

    /// <summary>True when any selector uses <c>:first-child</c>, <c>:last-child</c> or <c>:nth-child</c>.</summary>
    internal bool StructuralSelectors { get; private set; }

    internal FlexEngine Engine => _engine;

    /// <summary>Sets the screen size in pixels and the UI scale.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A size or the scale is not positive.</exception>
    public void SetViewport(float width, float height, float scale = 1)
    {
        if (width <= 0 || height <= 0 || scale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "The screen size and scale must be positive.");
        }

        var scaleChanged = scale != _scale;
        if (width == _width && height == _height && !scaleChanged)
        {
            return;
        }

        _width = width;
        _height = height;
        _scale = scale;
        if (scaleChanged)
        {
            InvalidateAllLayout();
        }

        Root.MarkLayout();
    }

    /// <summary>Adds a style sheet after the existing ones; its rules win ties in specificity.</summary>
    public void AddStyleSheet(StyleSheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        _sheets.Add(sheet);
        SheetsChanged();
    }

    /// <summary>Removes a style sheet; returns false when it was not added.</summary>
    public bool RemoveStyleSheet(StyleSheet sheet)
    {
        var removed = _sheets.Remove(sheet);
        if (removed)
        {
            SheetsChanged();
        }

        return removed;
    }

    private void SheetsChanged()
    {
        AncestorSelectors = false;
        StructuralSelectors = false;
        foreach (var sheet in _sheets)
        {
            foreach (var r in sheet.Rules)
            {
                AncestorSelectors |= r.Selector.HasCombinator;
                StructuralSelectors |= r.Selector.HasStructural;
            }
        }

        Root.MarkStyle(true);
    }

    internal void Dirty() => _dirty = true;

    private void InvalidateAllLayout()
    {
        var stack = new Stack<Element>();
        stack.Push(Root);
        while (stack.Count > 0)
        {
            var e = stack.Pop();
            e.LayoutDirty = true;
            e.HasCommit = false;
            e.ClearCache();
            for (var i = 0; i < e.ChildCount; i++)
            {
                stack.Push(e.ChildAt(i));
            }
        }

        _dirty = true;
    }

    internal float Px(Length l) => l.Unit switch
    {
        LengthUnit.Px => l.Value,
        LengthUnit.Ui => l.Value * _scale,
        _ => 0,
    };

    internal float Resolve(Length l, float basis) => l.Unit == LengthUnit.Percent ? basis * l.Value / 100f : Px(l);

    // ---- frame ---------------------------------------------------------------------------------------------------

    /// <summary>
    /// Evaluates bindings, resolves styles and lays out whatever changed, without producing a display list.
    /// Input dispatch calls this first so hit tests see current boxes.
    /// </summary>
    public void Update()
    {
        RunBindings(Root);
        DropStaleReferences();
        if (!_dirty)
        {
            return;
        }

        ResolveStyles(Root, false, false);
        _engine.Commit(Root, _width, _height, _width);
        Root.LX = 0;
        Root.LY = 0;
        _engine.LayoutFixed(Root);
        _dirty = false;
        _needsPaint = true;
    }

    /// <summary>Runs <see cref="Update"/> and returns the display list, rebuilt only if something changed since the last call.</summary>
    public DisplayList Frame()
    {
        Update();
        if (_needsPaint)
        {
            _painter.Paint(Root, _list);
            _list.Width = _width;
            _list.Height = _height;
            _list.Scale = _scale;
            _list.Version++;
            _needsPaint = false;
        }

        return _list;
    }

    private static void RunBindings(Element e)
    {
        var b = e.Bindings;
        if (b is not null)
        {
            for (var i = 0; i < b.Count; i++)
            {
                b[i].Update(e);
            }
        }

        // The count is re-read because a binding on this element may have changed the children.
        for (var i = 0; i < e.ChildCount; i++)
        {
            RunBindings(e.ChildAt(i));
        }
    }

    // ---- style resolution ----------------------------------------------------------------------------------------

    private void ResolveStyles(Element e, bool force, bool all)
    {
        var recompute = force || all || e.StyleDirty;
        var inheritedChanged = false;
        var tokensChanged = false;
        if (recompute)
        {
            inheritedChanged = Recompute(e, out tokensChanged);
        }

        var childAll = all || tokensChanged || e.StyleSubtree;
        var childForce = inheritedChanged;
        var visit = childAll || childForce || e.DescendantStyleDirty;
        e.StyleDirty = false;
        e.StyleSubtree = false;
        e.DescendantStyleDirty = false;
        if (!visit)
        {
            return;
        }

        for (var i = 0; i < e.ChildCount; i++)
        {
            var c = e.ChildAt(i);
            if (childAll || childForce || c.StyleDirty || c.DescendantStyleDirty)
            {
                ResolveStyles(c, childForce, childAll);
            }
        }
    }

    // Recomputes one element's style: initial and inherited values, then matching rules by (specificity, order), then inline style.
    // Returns true when an inherited property changed. CSS Cascade 4 §6.
    private bool Recompute(Element e, out bool tokensChanged)
    {
        var parent = e.Parent;
        var scratch = _scratch;
        var all = PropertyTable.All;
        for (var i = 0; i < scratch.Length; i++)
        {
            scratch[i] = all[i].Inherited && parent is not null ? parent.Computed.V[i] : all[i].Initial;
        }

        _matched.Clear();
        var order = 0;
        foreach (var sheet in _sheets)
        {
            foreach (var rule in sheet.Rules)
            {
                if (rule.Selector.Matches(e))
                {
                    // Stable insertion by specificity keeps later rules after earlier ones of equal weight.
                    var spec = rule.Selector.Specificity;
                    var j = _matched.Count;
                    while (j > 0 && Specificity(_matched[j - 1]) > spec)
                    {
                        j--;
                    }

                    _matched.Insert(j, (rule, order++));
                }
            }
        }

        _tokens.Clear();
        foreach (var (rule, _) in _matched)
        {
            foreach (var d in rule.Declarations)
            {
                if (d.IsToken)
                {
                    ApplyToken(e, d);
                }
            }
        }

        var inline = e.InlineOrNull;
        if (inline is not null)
        {
            foreach (var d in inline.Declarations)
            {
                if (d.IsToken)
                {
                    ApplyToken(e, d);
                }
            }
        }

        foreach (var (rule, _) in _matched)
        {
            foreach (var d in rule.Declarations)
            {
                if (!d.IsToken)
                {
                    ApplyProperty(e, d);
                }
            }
        }

        if (inline is not null)
        {
            foreach (var d in inline.Declarations)
            {
                if (!d.IsToken)
                {
                    ApplyProperty(e, d);
                }
            }
        }

        var cur = e.Computed.V;
        var inherited = false;
        var layout = false;
        var paint = false;
        for (var i = 0; i < scratch.Length; i++)
        {
            if (!scratch[i].Equals(cur[i]))
            {
                cur[i] = scratch[i];
                paint = true;
                inherited |= all[i].Inherited;
                layout |= !all[i].PaintOnly;
            }
        }

        tokensChanged = !SameTokens(e.Tokens);
        if (tokensChanged)
        {
            e.Tokens = _tokens.Count == 0 ? null : [.. _tokens];
        }

        if (layout)
        {
            e.MarkLayout();
        }
        else if (paint)
        {
            e.MarkPaint();
        }

        return inherited;
    }

    private static int Specificity((Rule Rule, int Order) m) => m.Rule.Selector.Specificity;

    private bool SameTokens((string Name, StyleValue Value)[]? old)
    {
        if ((old?.Length ?? 0) != _tokens.Count)
        {
            return false;
        }

        for (var i = 0; i < _tokens.Count; i++)
        {
            if (!string.Equals(old![i].Name, _tokens[i].Name, StringComparison.Ordinal) || !old[i].Value.Equals(_tokens[i].Value))
            {
                return false;
            }
        }

        return true;
    }

    private void ApplyToken(Element e, in Declaration d)
    {
        var value = d.Value;
        if (d.VarName is not null && !TryToken(e, d.VarName, true, out value))
        {
            value = d.Value;
        }

        if (value.Kind == ValueKind.None)
        {
            return;
        }

        for (var i = 0; i < _tokens.Count; i++)
        {
            if (string.Equals(_tokens[i].Name, d.TokenName, StringComparison.Ordinal))
            {
                _tokens[i] = (d.TokenName!, value);
                return;
            }
        }

        _tokens.Add((d.TokenName!, value));
    }

    private void ApplyProperty(Element e, in Declaration d)
    {
        var value = d.Value;
        if (d.VarName is not null)
        {
            // A token that no ancestor defines uses the fallback; with no fallback the property keeps its inherited or initial value.
            if (!TryToken(e, d.VarName, true, out value))
            {
                value = d.Value;
            }

            if (value.Kind == ValueKind.None)
            {
                return;
            }
        }

        // A token of the wrong type for the property is ignored (CSS: invalid at computed-value time).
        if (value.Kind == PropertyTable.All[(int)d.Prop].Kind)
        {
            _scratch[(int)d.Prop] = value;
        }
    }

    // The nearest definition wins: the element's own tokens (when allowed), then each ancestor's.
    private bool TryToken(Element e, string name, bool includeOwn, out StyleValue value)
    {
        if (includeOwn)
        {
            for (var i = _tokens.Count - 1; i >= 0; i--)
            {
                if (string.Equals(_tokens[i].Name, name, StringComparison.Ordinal))
                {
                    value = _tokens[i].Value;
                    return true;
                }
            }
        }

        for (var p = e.Parent; p is not null; p = p.Parent)
        {
            if (p.Tokens is { } t)
            {
                foreach (var (n, v) in t)
                {
                    if (string.Equals(n, name, StringComparison.Ordinal))
                    {
                        value = v;
                        return true;
                    }
                }
            }
        }

        value = default;
        return false;
    }
}
