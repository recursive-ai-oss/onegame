using OneRender;
using OneRender.Interaction;
using OneRender.Layout;
using OneRender.Painting;
using OneRender.Text;
using OneRender.Tree;
using RecursiveAI.OneHtml;

namespace OneGame.Pages;

/// <summary>What a page is made from.</summary>
public sealed class PageOptions
{
    /// <summary>Creates options.</summary>
    /// <param name="fonts">The fonts the page may use.</param>
    public PageOptions(PageFonts fonts)
    {
        ArgumentNullException.ThrowIfNull(fonts);
        Fonts = fonts;
    }

    /// <summary>The fonts the page may use.</summary>
    public PageFonts Fonts { get; }

    /// <summary>The images the page can name; none when <see langword="null"/>.</summary>
    public PageImages? Images { get; init; }

    /// <summary>
    /// Runs every time the page is (re)created, which happens when the window size or UI scale changes (a OneRender HTML page cannot resize).
    /// Register DOM listeners here so they survive: <c>s.Interactive!.Events.AddEventListener(...)</c>.
    /// </summary>
    public Action<PageScreen>? Setup { get; init; }
}

/// <summary>
/// One OneRender page shown as a screen: an HTML document (<see cref="Interactive"/>) or a builder tree (<see cref="Live"/>). It lays out at the size of the
/// window in CSS px, keeps its display list up to date, takes input and time, and offers lookups by name for the harness.
/// </summary>
public sealed class PageScreen
{
    private readonly string? _html;
    private readonly StyledTree? _tree;
    private readonly PageOptions _options;
    private readonly Painter _painter = new();
    private readonly RetainedDisplayList _list = new();
    private double _now;
    private int _width;
    private int _height;
    private float _scale;
    private PaintOptions _paint;

    private PageScreen(string? html, StyledTree? tree, PageOptions options)
    {
        _html = html;
        _tree = tree;
        _options = options;
    }

    /// <summary>A screen from HTML and CSS (inline <c>&lt;style&gt;</c> and style attributes).</summary>
    /// <param name="html">The document.</param>
    /// <param name="options">Fonts, images and setup.</param>
    public static PageScreen FromHtml(string html, PageOptions options)
    {
        ArgumentNullException.ThrowIfNull(html);
        ArgumentNullException.ThrowIfNull(options);
        return new PageScreen(html, null, options);
    }

    /// <summary>A screen from a builder tree. The tree stays the caller's to edit; changes show on the next frame.</summary>
    /// <param name="tree">The tree.</param>
    /// <param name="options">Fonts, images and setup.</param>
    public static PageScreen FromTree(StyledTree tree, PageOptions options)
    {
        ArgumentNullException.ThrowIfNull(tree);
        ArgumentNullException.ThrowIfNull(options);
        return new PageScreen(null, tree, options);
    }

    /// <summary>A screen that draws a display list built by hand (tests, tools). It lays out nothing and takes no input.</summary>
    internal static PageScreen FromList(DisplayList list, PageImages? images)
    {
        var screen = new PageScreen(null, null, new PageOptions(new PageFonts(new OneGame.Text.FontLibrary())) { Images = images });
        screen.Draw = new PageDraw(list, images);
        screen._width = int.MaxValue;
        screen._height = int.MaxValue;
        screen._scale = 1f;
        return screen;
    }

    /// <summary>The name the screen was shown under.</summary>
    public string Name { get; internal set; } = "";

    /// <summary>Drawn when true (the default) and takes input.</summary>
    public bool Visible { get; set; } = true;

    /// <summary>Screens draw in ascending order; later ones are on top.</summary>
    public int Order { get; set; }

    /// <summary>The interactive page of an HTML screen; null for a builder screen or before the first frame.</summary>
    public InteractivePage? Interactive { get; private set; }

    /// <summary>The live page of a builder screen; null for an HTML screen or before the first frame.</summary>
    public LivePage? Live { get; private set; }

    /// <summary>How many times the page was created (the first frame, then once per size or scale change).</summary>
    public int Creations { get; private set; }

    /// <summary>The viewport in CSS px.</summary>
    public (double Width, double Height) Viewport => _scale > 0 ? (_width / _scale, _height / _scale) : default;

    internal PageDraw? Draw { get; private set; }

    internal double Now => _now;

    /// <summary>Makes sure the page exists at this size, creating it again when the size or scale changed.</summary>
    internal void Prepare(int width, int height, float scale, double nowMs)
    {
        if (Draw is not null && (_html is null && _tree is null || width == _width && height == _height && scale == _scale))
        {
            return;
        }

        _width = width;
        _height = height;
        _scale = scale;
        var w = Math.Max(1.0, width / (double)scale);
        var h = Math.Max(1.0, height / (double)scale);
        _list.ClearDamage();
        if (_html is not null)
        {
            var page = InteractivePage.Create(_html, new HtmlPageOptions(_options.Fonts.Provider)
            {
                ViewportWidth = w,
                ViewportHeight = h,
                Images = _options.Images,
                DevicePixelRatio = scale,
            }, new InteractionOptions { AnimationStartTime = nowMs });
            Interactive = page;
            _now = nowMs;
            _paint = new PaintOptions(w, h) { Scroll = page.Page.Scroll, Images = _options.Images, Fonts = _options.Fonts.Provider, DevicePixelRatio = scale };
            _painter.Paint(page.Page.Fragments, _list, _paint);
        }
        else
        {
            if (Live is null)
            {
                Live = new LivePage(_tree!, new LivePageOptions(_options.Fonts.Provider) { ViewportWidth = w, ViewportHeight = h, Images = _options.Images, DevicePixelRatio = scale });
            }
            else
            {
                Live.Resize(w, h);
            }

            Live.Flush();
            _now = nowMs;
        }

        Creations++;
        Draw = new PageDraw(_html is not null ? _list.Current : Live!.Display.Current, _options.Images);
        if (_options.Setup is { } setup)
        {
            setup(this);
            if (Interactive is { } made)
            {
                // Setup may have set control values or focus; paint them now, so the first frame shows what it made.
                made.Repaint(_painter, _list, _paint);
                _list.ClearDamage();
                Draw = new PageDraw(_list.Current, _options.Images);
            }
        }
    }

    /// <summary>
    /// Makes the page lay out again on the next frame, for an image whose size changed through <see cref="PageImages.Replace"/>. An HTML page is
    /// created anew (its focus and field values are lost); a builder page is repainted. Same-size replacements need nothing: pixels are read every frame.
    /// </summary>
    public void Refresh()
    {
        if (_html is not null)
        {
            Draw = null;
            Interactive = null;
        }
        else
        {
            Live?.Repaint();
        }
    }

    /// <summary>Moves the page's clock to <paramref name="nowMs"/> (view time in milliseconds) and updates what animates.</summary>
    internal void Advance(double nowMs)
    {
        if (Draw is null)
        {
            return;
        }

        _now = nowMs;
        if (Interactive is { } page)
        {
            Apply(page.Advance(nowMs));
        }
        else
        {
            Live!.Flush();
            Draw = new PageDraw(Live.Display.Current, _options.Images);
        }
    }

    /// <summary>Brings the display list up to date after an input or a clock step.</summary>
    private void Apply(InputOutcome outcome)
    {
        var page = Interactive!;
        if ((outcome & (InputOutcome.Relaid | InputOutcome.EditChanged)) != 0)
        {
            page.Repaint(_painter, _list, _paint);
        }
        else if ((outcome & InputOutcome.Animated) != 0)
        {
            page.PatchAnimations(_painter, _list);
        }

        if ((outcome & InputOutcome.Scrolled) != 0)
        {
            _painter.PatchScroll(_list);
        }

        _list.ClearDamage();
        Draw = new PageDraw(_list.Current, _options.Images);
    }

    /// <summary>Whether an element with this name exists (an <c>id</c> or a form control's <c>name</c> in HTML; <c>SetName</c> in a builder tree).</summary>
    /// <param name="name">The name.</param>
    public bool Has(string name) => Interactive is { } p ? !p.FindElement(name).IsNull : Live is not null && !Live.Styled.FindByName(name).IsNull;

    /// <summary>The text of a named element, or null when there is none.</summary>
    /// <param name="name">The name.</param>
    public string? TextOf(string name)
    {
        if (Interactive is { } p)
        {
            var n = p.FindElement(name);
            if (n.IsNull)
            {
                return null;
            }

            // A text field's text is what was typed into it, not its (empty) content.
            return n.LocalName is "input" or "textarea" ? p.GetValue(n) : n.TextContent ?? "";
        }

        return null;
    }

    /// <summary>The box of a named element in device pixels, or null when there is none (builder pages report none).</summary>
    /// <param name="name">The name.</param>
    public (float X, float Y, float W, float H)? BoundsOf(string name)
    {
        if (Interactive is not { } p)
        {
            return null;
        }

        var n = p.FindElement(name);
        if (n.IsNull)
        {
            return null;
        }

        var r = p.Layout.GetBoundingClientRect(n);
        return ((float)(r.X * _scale), (float)(r.Y * _scale), (float)(r.Width * _scale), (float)(r.Height * _scale));
    }

    /// <summary>The state flags of a named element: <c>hover active focus disabled</c> joined by spaces.</summary>
    /// <param name="name">The name.</param>
    public string StatesOf(string name)
    {
        if (Interactive is not { } p)
        {
            return "";
        }

        var n = p.FindElement(name);
        if (n.IsNull)
        {
            return "";
        }

        var s = new List<string>(3);
        if (p.HoveredElement == n)
        {
            s.Add("hover");
        }

        if (p.ActiveElement == n)
        {
            s.Add("active");
        }

        if (p.FocusedElement == n)
        {
            s.Add("focus");
        }

        return string.Join(' ', s);
    }

    /// <summary>Clicks a named element the way <c>element.click()</c> does (events and activation, no hit test).</summary>
    /// <param name="name">The name.</param>
    /// <returns>False when there is no such element.</returns>
    public bool Click(string name)
    {
        if (Interactive is not { } p)
        {
            return false;
        }

        var n = p.FindElement(name);
        if (n.IsNull)
        {
            return false;
        }

        Apply(p.Click(n, _now));
        return true;
    }

    /// <summary>Whether a pointer at this device position is over something of the page other than the empty background.</summary>
    public bool Covers(float x, float y)
    {
        if (Interactive is not { } p)
        {
            return false;
        }

        var hit = p.ElementFromPoint(x / _scale, y / _scale);
        return !hit.IsNull && hit.LocalName is not ("html" or "body");
    }

    /// <summary>Sends a pointer event at a device position. True when the page took it.</summary>
    internal bool Pointer(PointerAction action, float x, float y, int button)
    {
        if (Interactive is not { } p)
        {
            return false;
        }

        var covers = Covers(x, y);
        var buttons = action == PointerAction.Down ? (button == 2 ? MouseButtons.Secondary : button == 1 ? MouseButtons.Auxiliary : MouseButtons.Primary) : MouseButtons.None;
        var outcome = p.HandlePointer(new PointerInput { Action = action, X = x / _scale, Y = y / _scale, Button = button, Buttons = buttons, Time = _now, PointerType = "mouse" });
        Apply(outcome);
        return covers || action == PointerAction.Up && (outcome & InputOutcome.DefaultPrevented) != 0;
    }

    /// <summary>Sends a wheel step at a device position. True when the page took it.</summary>
    internal bool Wheel(float x, float y, float dx, float dy)
    {
        if (Interactive is not { } p)
        {
            return false;
        }

        var covers = Covers(x, y);
        Apply(p.HandleWheel(new WheelInput { X = x / _scale, Y = y / _scale, DeltaX = dx, DeltaY = dy, Mode = WheelDeltaMode.Pixel, Time = _now }));
        return covers;
    }

    /// <summary>Sends a key. True when a page element has focus (so the key is the page's).</summary>
    internal bool Key(string key, string? code, bool down, KeyModifiers modifiers)
    {
        if (Interactive is not { } p)
        {
            return false;
        }

        var hadFocus = !p.FocusedElement.IsNull;
        var outcome = p.HandleKey(new KeyInput { Down = down, Key = key, Code = code, Modifiers = modifiers, Time = _now });
        Apply(outcome);
        return hadFocus || !p.FocusedElement.IsNull;
    }

    /// <summary>Inserts typed text into the focused editable element. True when one had focus.</summary>
    internal bool Text(string text)
    {
        if (Interactive is not { } p || p.FocusedElement.IsNull)
        {
            return false;
        }

        Apply(p.InsertText(text, _now));
        return true;
    }
}
