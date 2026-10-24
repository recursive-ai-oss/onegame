namespace OneGame.UI.Kit;

/// <summary>
/// Installs the kit into a <see cref="UiTree"/>: adds the theme, owns the tooltip layer and the input glyph bindings, and keeps the kit's notion of time.
/// Create one per tree, call <see cref="Advance"/> once per frame with the frame time (tooltips wait for a delay, and the UI never reads a clock itself),
/// and send input through <see cref="Dispatch"/> so glyphs learn which device the player is using.
/// </summary>
public sealed class KitHost
{
    private readonly Element _layer;
    private readonly Element _label;
    private Element? _target;
    private Func<string>? _text;
    private float _elapsed;
    private bool _shown;
    private float _x = float.NaN;
    private float _y = float.NaN;

    /// <summary>Adds the theme and the tooltip layer to <paramref name="tree"/>.</summary>
    /// <param name="tree">The UI to install into.</param>
    /// <param name="theme">The style sheet to add; the default theme (<see cref="KitTheme.Create"/>) when null. Add game styles after it so they win ties.</param>
    public KitHost(UiTree tree, StyleSheet? theme = null)
    {
        Tree = tree ?? throw new ArgumentNullException(nameof(tree));
        tree.AddStyleSheet(theme ?? KitTheme.Create());
        _label = Ui.Label("").Class("kit-tooltip-label");
        _layer = UiKit.Make("Tooltip", "tooltip", "kit-tooltip", "kit/tooltip");
        _layer.Style(s => s.Position(PositionMode.Fixed).ZIndex(1000).PointerEvents(PointerEvents.None));
        _layer.IsVisible = false;
        _layer.Add(_label);
        _layer.OnUpdate(_ => Place());
        tree.Root.Add(_layer);
    }

    /// <summary>The tree this host serves.</summary>
    public UiTree Tree { get; }

    /// <summary>Which device the player used last, and what each action looks like on it.</summary>
    public InputGlyphs Glyphs { get; } = new();

    /// <summary>Seconds a pointer or focus must rest on a target before its tooltip appears.</summary>
    public float TooltipDelay { get; set; } = 0.5f;

    /// <summary>The tooltip element (id <c>kit/tooltip</c>), visible while a tooltip shows.</summary>
    public Element TooltipLayer => _layer;

    /// <summary>Moves the kit's clock on. Call once per frame, before the frame is built.</summary>
    /// <param name="seconds">The time since the last call.</param>
    public void Advance(float seconds)
    {
        if (_target is not null && !_shown)
        {
            _elapsed += seconds;
            if (_elapsed >= TooltipDelay)
            {
                _shown = true;
                _layer.IsVisible = true;
            }
        }
    }

    /// <summary>Notes the event's device for the glyphs, then passes the event to the tree. Returns what <see cref="UiTree.Dispatch"/> returns.</summary>
    public bool Dispatch(in UiEvent e)
    {
        Glyphs.Observe(e);
        return Tree.Dispatch(e);
    }

    /// <summary>
    /// Gives an element a tooltip: after <see cref="TooltipDelay"/> of pointer hover or keyboard focus, a small box with the text appears under it
    /// (above it when there is no room below), clamped to the screen, and follows the element if it moves.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="target">The element that gets the tooltip.</param>
    /// <param name="text">The tooltip text.</param>
    /// <returns><paramref name="target"/>, so the call fits into a builder chain.</returns>
    public T Tooltip<T>(T target, string text)
        where T : Element
    {
        ArgumentNullException.ThrowIfNull(text);
        return Tooltip(target, () => text);
    }

    /// <summary>Gives an element a tooltip whose text follows a lambda (re-read every frame while it shows). See <see cref="Tooltip{T}(T, string)"/>.</summary>
    public T Tooltip<T>(T target, Func<string> text)
        where T : Element
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(text);
        target.OnPointerEnter(() => Arm(target, text));
        target.OnFocus(() => Arm(target, text));
        target.OnPointerLeave(() => Disarm(target));
        target.OnBlur(() => Disarm(target));
        return target;
    }

    private void Arm(Element target, Func<string> text)
    {
        if (_target == target)
        {
            return;
        }

        Hide();
        _target = target;
        _text = text;
        _elapsed = 0;
    }

    private void Disarm(Element target)
    {
        if (_target == target)
        {
            Hide();
        }
    }

    private void Hide()
    {
        _target = null;
        _text = null;
        _shown = false;
        _x = float.NaN;
        _y = float.NaN;
        if (_layer.IsVisible)
        {
            _layer.IsVisible = false;
        }
    }

    private void Place()
    {
        if (!_shown || _target is null)
        {
            return;
        }

        if (_target.Tree != Tree || !_target.IsVisible)
        {
            Hide();
            return;
        }

        var text = _text!();
        if (!ReferenceEquals(_label.Text, text))
        {
            _label.Text = text;
        }

        var t = _target.AbsoluteBounds;
        var size = _layer.Bounds;
        var gap = 6 * Tree.Scale;
        var x = t.X;
        var y = t.Bottom + gap;
        if (y + size.Height > Tree.Height)
        {
            y = t.Y - gap - size.Height;
        }

        x = Math.Max(0, Math.Min(x, Tree.Width - size.Width));
        y = Math.Max(0, Math.Min(y, Tree.Height - size.Height));
        if (x != _x || y != _y)
        {
            _x = x;
            _y = y;
            _layer.Style(s => s.Left(Length.Px(x)).Top(Length.Px(y)));
        }
    }
}
