namespace OneGame.UI;

/// <summary>Where <see cref="StyleBuilder.Anchor"/> pins an element.</summary>
public enum AnchorPoint
{
    /// <summary>Top-left corner.</summary>
    TopLeft,
    /// <summary>Top edge, centered horizontally.</summary>
    Top,
    /// <summary>Top-right corner.</summary>
    TopRight,
    /// <summary>Left edge, centered vertically.</summary>
    Left,
    /// <summary>Centered both ways.</summary>
    Center,
    /// <summary>Right edge, centered vertically.</summary>
    Right,
    /// <summary>Bottom-left corner.</summary>
    BottomLeft,
    /// <summary>Bottom edge, centered horizontally.</summary>
    Bottom,
    /// <summary>Bottom-right corner.</summary>
    BottomRight,
    /// <summary>Covers the whole containing box.</summary>
    Fill,
}

internal readonly record struct Declaration(StyleProp Prop, StyleValue Value, string? VarName = null, string? TokenName = null)
{
    public bool IsToken => TokenName is not null;
}

/// <summary>Collects style declarations for a rule or for one element's inline style. Every method returns the builder, so calls chain.</summary>
public sealed class StyleBuilder
{
    private readonly List<Declaration> _decls = [];
    private readonly Element? _owner;

    /// <summary>Creates an empty builder for use in a style sheet rule.</summary>
    public StyleBuilder()
    {
    }

    internal StyleBuilder(Element owner) => _owner = owner;

    internal List<Declaration> Declarations => _decls;

    internal int Count => _decls.Count;

    // ---- generic -------------------------------------------------------------------------------------------------

    /// <summary>Sets a property by its CSS name.</summary>
    /// <exception cref="UiStyleException">The name is unknown or the value has the wrong type for it.</exception>
    public StyleBuilder Set(string cssName, StyleValue value)
    {
        var p = Lookup(cssName);
        if (PropertyTable.Info(p).Kind != value.Kind)
        {
            throw new UiStyleException($"Property '{cssName}' takes a {PropertyTable.Info(p).Kind} value, not {value.Kind}.");
        }

        return Put(new Declaration(p, value));
    }

    /// <summary>Sets a property to the value of a theme token, such as <c>--accent</c>, falling back to <paramref name="fallback"/> when no ancestor defines it.</summary>
    /// <exception cref="UiStyleException">The property name is unknown or the token name does not start with <c>--</c>.</exception>
    public StyleBuilder Var(string cssName, string token, StyleValue fallback = default)
    {
        CheckToken(token);
        return Put(new Declaration(Lookup(cssName), fallback, token));
    }

    /// <summary>Defines a theme token on the matched elements; they and their descendants can read it with <see cref="Var"/>.</summary>
    /// <exception cref="UiStyleException">The token name does not start with <c>--</c>.</exception>
    public StyleBuilder Token(string name, StyleValue value)
    {
        CheckToken(name);
        return Put(new Declaration(StyleProp.Count, value, null, name));
    }

    /// <summary>Defines a theme token whose value is another token, looked up on the parent chain.</summary>
    /// <exception cref="UiStyleException">A token name does not start with <c>--</c>.</exception>
    public StyleBuilder TokenFrom(string name, string otherToken, StyleValue fallback = default)
    {
        CheckToken(name);
        CheckToken(otherToken);
        return Put(new Declaration(StyleProp.Count, fallback, otherToken, name));
    }

    // ---- box -----------------------------------------------------------------------------------------------------

    /// <summary>Sets <c>display</c>.</summary>
    public StyleBuilder Display(DisplayMode v) => E(StyleProp.Display, (int)v);

    /// <summary>Sets <c>position</c>.</summary>
    public StyleBuilder Position(PositionMode v) => E(StyleProp.Position, (int)v);

    /// <summary>Sets <c>left</c>.</summary>
    public StyleBuilder Left(Length v) => L(StyleProp.Left, v);

    /// <summary>Sets <c>top</c>.</summary>
    public StyleBuilder Top(Length v) => L(StyleProp.Top, v);

    /// <summary>Sets <c>right</c>.</summary>
    public StyleBuilder Right(Length v) => L(StyleProp.Right, v);

    /// <summary>Sets <c>bottom</c>.</summary>
    public StyleBuilder Bottom(Length v) => L(StyleProp.Bottom, v);

    /// <summary>Sets all four insets (<c>left/top/right/bottom</c>).</summary>
    public StyleBuilder Inset(Length v) => Left(v).Top(v).Right(v).Bottom(v);

    /// <summary>Sets <c>width</c>.</summary>
    public StyleBuilder Width(Length v) => L(StyleProp.Width, v);

    /// <summary>Sets <c>height</c>.</summary>
    public StyleBuilder Height(Length v) => L(StyleProp.Height, v);

    /// <summary>Sets <c>width</c> and <c>height</c>.</summary>
    public StyleBuilder Size(Length w, Length h) => Width(w).Height(h);

    /// <summary>Sets <c>min-width</c>.</summary>
    public StyleBuilder MinWidth(Length v) => L(StyleProp.MinWidth, v);

    /// <summary>Sets <c>min-height</c>.</summary>
    public StyleBuilder MinHeight(Length v) => L(StyleProp.MinHeight, v);

    /// <summary>Sets <c>max-width</c>; auto means none.</summary>
    public StyleBuilder MaxWidth(Length v) => L(StyleProp.MaxWidth, v);

    /// <summary>Sets <c>max-height</c>; auto means none.</summary>
    public StyleBuilder MaxHeight(Length v) => L(StyleProp.MaxHeight, v);

    /// <summary>Sets all four margins.</summary>
    public StyleBuilder Margin(Length all) => Margin(all, all, all, all);

    /// <summary>Sets vertical then horizontal margins.</summary>
    public StyleBuilder Margin(Length vertical, Length horizontal) => Margin(vertical, horizontal, vertical, horizontal);

    /// <summary>Sets the margins top, right, bottom, left (the CSS shorthand order).</summary>
    public StyleBuilder Margin(Length top, Length right, Length bottom, Length left) =>
        L(StyleProp.MarginTop, top).L(StyleProp.MarginRight, right).L(StyleProp.MarginBottom, bottom).L(StyleProp.MarginLeft, left);

    /// <summary>Sets <c>margin-left</c>.</summary>
    public StyleBuilder MarginLeft(Length v) => L(StyleProp.MarginLeft, v);

    /// <summary>Sets <c>margin-right</c>.</summary>
    public StyleBuilder MarginRight(Length v) => L(StyleProp.MarginRight, v);

    /// <summary>Sets <c>margin-top</c>.</summary>
    public StyleBuilder MarginTop(Length v) => L(StyleProp.MarginTop, v);

    /// <summary>Sets <c>margin-bottom</c>.</summary>
    public StyleBuilder MarginBottom(Length v) => L(StyleProp.MarginBottom, v);

    /// <summary>Sets all four paddings.</summary>
    public StyleBuilder Padding(Length all) => Padding(all, all, all, all);

    /// <summary>Sets vertical then horizontal paddings.</summary>
    public StyleBuilder Padding(Length vertical, Length horizontal) => Padding(vertical, horizontal, vertical, horizontal);

    /// <summary>Sets the paddings top, right, bottom, left.</summary>
    public StyleBuilder Padding(Length top, Length right, Length bottom, Length left) =>
        L(StyleProp.PaddingTop, top).L(StyleProp.PaddingRight, right).L(StyleProp.PaddingBottom, bottom).L(StyleProp.PaddingLeft, left);

    /// <summary>Sets all four border widths.</summary>
    public StyleBuilder BorderWidth(Length all) =>
        L(StyleProp.BorderTopWidth, all).L(StyleProp.BorderRightWidth, all).L(StyleProp.BorderBottomWidth, all).L(StyleProp.BorderLeftWidth, all);

    /// <summary>Sets the border widths top, right, bottom, left.</summary>
    public StyleBuilder BorderWidth(Length top, Length right, Length bottom, Length left) =>
        L(StyleProp.BorderTopWidth, top).L(StyleProp.BorderRightWidth, right).L(StyleProp.BorderBottomWidth, bottom).L(StyleProp.BorderLeftWidth, left);

    /// <summary>Sets all four border widths and the border color.</summary>
    public StyleBuilder Border(Length width, Color color) => BorderWidth(width).BorderColor(color);

    /// <summary>Sets the border color (one color for all sides).</summary>
    public StyleBuilder BorderColor(Color v) => C(StyleProp.BorderColor, v);

    /// <summary>Sets <c>box-sizing</c>.</summary>
    public StyleBuilder BoxSizing(BoxSizing v) => E(StyleProp.BoxSizing, (int)v);

    // ---- flex ----------------------------------------------------------------------------------------------------

    /// <summary>Sets <c>flex-direction</c>.</summary>
    public StyleBuilder Direction(FlexDirection v) => E(StyleProp.FlexDirection, (int)v);

    /// <summary>Sets <c>flex-wrap</c>.</summary>
    public StyleBuilder Wrap(FlexWrap v) => E(StyleProp.FlexWrap, (int)v);

    /// <summary>Sets <c>justify-content</c>.</summary>
    public StyleBuilder JustifyContent(Align v) => E(StyleProp.JustifyContent, (int)v);

    /// <summary>Sets <c>align-items</c>.</summary>
    public StyleBuilder AlignItems(Align v) => E(StyleProp.AlignItems, (int)v);

    /// <summary>Sets <c>align-self</c>.</summary>
    public StyleBuilder AlignSelf(Align v) => E(StyleProp.AlignSelf, (int)v);

    /// <summary>Sets <c>align-content</c>.</summary>
    public StyleBuilder AlignContent(Align v) => E(StyleProp.AlignContent, (int)v);

    /// <summary>Sets <c>flex-grow</c>.</summary>
    public StyleBuilder Grow(float v) => N(StyleProp.FlexGrow, v);

    /// <summary>Sets <c>flex-shrink</c>.</summary>
    public StyleBuilder Shrink(float v) => N(StyleProp.FlexShrink, v);

    /// <summary>Sets <c>flex-basis</c>.</summary>
    public StyleBuilder Basis(Length v) => L(StyleProp.FlexBasis, v);

    /// <summary>Sets <c>flex-grow</c>, <c>flex-shrink</c> and <c>flex-basis</c> (the CSS <c>flex</c> shorthand).</summary>
    public StyleBuilder Flex(float grow, float shrink, Length basis) => Grow(grow).Shrink(shrink).Basis(basis);

    /// <summary>Sets both gaps.</summary>
    public StyleBuilder Gap(Length v) => RowGap(v).ColumnGap(v);

    /// <summary>Sets the gap between lines (<c>row-gap</c>).</summary>
    public StyleBuilder RowGap(Length v) => L(StyleProp.RowGap, v);

    /// <summary>Sets the gap between items in a row (<c>column-gap</c>).</summary>
    public StyleBuilder ColumnGap(Length v) => L(StyleProp.ColumnGap, v);

    /// <summary>Sets <c>order</c>.</summary>
    public StyleBuilder Order(int v) => N(StyleProp.Order, v);

    /// <summary>Sets <c>overflow</c> on both axes.</summary>
    public StyleBuilder Overflow(OverflowMode v) => Overflow(v, v);

    /// <summary>Sets <c>overflow-x</c> and <c>overflow-y</c>.</summary>
    public StyleBuilder Overflow(OverflowMode x, OverflowMode y) => E(StyleProp.OverflowX, (int)x).E(StyleProp.OverflowY, (int)y);

    /// <summary>
    /// Pins an element at <paramref name="point"/> of its containing box (the parent, or the screen when <paramref name="toScreen"/>).
    /// <paramref name="x"/> and <paramref name="y"/> push the element inward from the anchored edges, or move it when centered.
    /// </summary>
    public StyleBuilder Anchor(AnchorPoint point, Length x = default, Length y = default, bool toScreen = false)
    {
        x = x.IsAuto ? 0f : x;
        y = y.IsAuto ? 0f : y;
        Position(toScreen ? PositionMode.Fixed : PositionMode.Absolute);
        Inset(Length.Auto).Margin(0f).Translate(0f, 0f);
        var half = Length.Percent(50);
        var back = Length.Percent(-50);
        switch (point)
        {
            case AnchorPoint.Fill:
                return Inset(x);
            case AnchorPoint.TopLeft: return Left(x).Top(y);
            case AnchorPoint.TopRight: return Right(x).Top(y);
            case AnchorPoint.BottomLeft: return Left(x).Bottom(y);
            case AnchorPoint.BottomRight: return Right(x).Bottom(y);
            case AnchorPoint.Top: return Left(half).MarginLeft(x).Top(y).Translate(back, 0f);
            case AnchorPoint.Bottom: return Left(half).MarginLeft(x).Bottom(y).Translate(back, 0f);
            case AnchorPoint.Left: return Left(x).Top(half).MarginTop(y).Translate(0f, back);
            case AnchorPoint.Right: return Right(x).Top(half).MarginTop(y).Translate(0f, back);
            default: return Left(half).Top(half).MarginLeft(x).MarginTop(y).Translate(back, back);
        }
    }

    // ---- paint ---------------------------------------------------------------------------------------------------

    /// <summary>Sets <c>background-color</c>.</summary>
    public StyleBuilder Background(Color v) => C(StyleProp.BackgroundColor, v);

    /// <summary>Sets the background image by asset id (raster or SVG); an empty string clears it.</summary>
    public StyleBuilder BackgroundImage(string id) => T(StyleProp.BackgroundImage, id);

    /// <summary>Sets the nine-slice inset of the background image, in pixels on every side; zero draws the image stretched.</summary>
    public StyleBuilder BackgroundSlice(Length v) => L(StyleProp.BackgroundSlice, v);

    /// <summary>Sets <c>background-repeat</c>: whether the background image tiles across, down, both or neither.</summary>
    public StyleBuilder BackgroundRepeat(BackgroundRepeat v) => E(StyleProp.BackgroundRepeat, (int)v);

    /// <summary>Sets <c>background-size</c> to a keyword (<see cref="BackgroundSizing.Fill"/> stretches to the box, <see cref="BackgroundSizing.Auto"/> uses the natural size times the UI scale) and clears <c>background-width</c>/<c>-height</c>.</summary>
    public StyleBuilder BackgroundSize(BackgroundSizing v) => E(StyleProp.BackgroundSize, (int)v).L(StyleProp.BackgroundWidth, Length.Auto).L(StyleProp.BackgroundHeight, Length.Auto);

    /// <summary>Sets an explicit tile or image size. Percentages are of the box; an <c>auto</c> side keeps the image's aspect ratio.</summary>
    public StyleBuilder BackgroundSize(Length width, Length height) => E(StyleProp.BackgroundSize, (int)BackgroundSizing.Auto).L(StyleProp.BackgroundWidth, width).L(StyleProp.BackgroundHeight, height);

    /// <summary>Sets <c>background-position-x</c> and <c>-y</c>. A percentage aligns that fraction of the image with the same fraction of the box, as in CSS; other lengths move the image from the box's top-left corner.</summary>
    public StyleBuilder BackgroundPosition(Length x, Length y) => L(StyleProp.BackgroundPositionX, x).L(StyleProp.BackgroundPositionY, y);

    /// <summary>Sets the color the background image is multiplied by (white by default, which leaves it as drawn). White single-color art tinted with a theme token follows the theme.</summary>
    public StyleBuilder BackgroundTint(Color v) => C(StyleProp.BackgroundTint, v);

    /// <summary>Sets all four corner radii.</summary>
    public StyleBuilder Radius(Length all) =>
        L(StyleProp.RadiusTopLeft, all).L(StyleProp.RadiusTopRight, all).L(StyleProp.RadiusBottomRight, all).L(StyleProp.RadiusBottomLeft, all);

    /// <summary>Sets the corner radii top-left, top-right, bottom-right, bottom-left.</summary>
    public StyleBuilder Radius(Length topLeft, Length topRight, Length bottomRight, Length bottomLeft) =>
        L(StyleProp.RadiusTopLeft, topLeft).L(StyleProp.RadiusTopRight, topRight).L(StyleProp.RadiusBottomRight, bottomRight).L(StyleProp.RadiusBottomLeft, bottomLeft);

    /// <summary>Sets <c>opacity</c> (0..1); the element and its children fade as a group.</summary>
    public StyleBuilder Opacity(float v) => N(StyleProp.Opacity, v);

    /// <summary>Sets <c>visibility</c>.</summary>
    public StyleBuilder Visibility(VisibilityMode v) => E(StyleProp.Visibility, (int)v);

    /// <summary>Sets <c>z-index</c>: it orders an element among its siblings; higher paints later and is hit first.</summary>
    public StyleBuilder ZIndex(int v) => N(StyleProp.ZIndex, v);

    /// <summary>Sets <c>pointer-events</c>.</summary>
    public StyleBuilder PointerEvents(PointerEvents v) => E(StyleProp.PointerEvents, (int)v);

    /// <summary>Sets <c>cursor</c>.</summary>
    public StyleBuilder Cursor(CursorShape v) => E(StyleProp.Cursor, (int)v);

    /// <summary>Sets an outline drawn outside the border box (a focus ring does not take layout space).</summary>
    public StyleBuilder Outline(Length width, Color color, Length offset = default) =>
        L(StyleProp.OutlineWidth, width).C(StyleProp.OutlineColor, color).L(StyleProp.OutlineOffset, offset.IsAuto ? 0f : offset);

    /// <summary>Sets a drop shadow under the box.</summary>
    public StyleBuilder Shadow(Length x, Length y, Length blur, Length spread, Color color) =>
        L(StyleProp.ShadowX, x).L(StyleProp.ShadowY, y).L(StyleProp.ShadowBlur, blur).L(StyleProp.ShadowSpread, spread).C(StyleProp.ShadowColor, color);

    /// <summary>Moves the painted box and its content without affecting layout; percentages are of the element's own size.</summary>
    public StyleBuilder Translate(Length x, Length y) => L(StyleProp.TranslateX, x).L(StyleProp.TranslateY, y);

    /// <summary>Scales the painted box about its center.</summary>
    public StyleBuilder Scale(float v) => N(StyleProp.Scale, v);

    /// <summary>Rotates the painted box about its center, in degrees clockwise.</summary>
    public StyleBuilder Rotate(float degrees) => N(StyleProp.Rotate, degrees);

    // ---- text ----------------------------------------------------------------------------------------------------

    /// <summary>Sets the inherited text <c>color</c>.</summary>
    public StyleBuilder TextColor(Color v) => C(StyleProp.Color, v);

    /// <summary>Sets <c>font-family</c> (a family name or generic family the text system understands).</summary>
    public StyleBuilder FontFamily(string v) => T(StyleProp.FontFamily, v);

    /// <summary>Sets <c>font-size</c>.</summary>
    public StyleBuilder FontSize(Length v) => L(StyleProp.FontSize, v);

    /// <summary>Sets <c>font-weight</c> (100..900).</summary>
    public StyleBuilder FontWeight(int v) => N(StyleProp.FontWeight, v);

    /// <summary>Sets <c>font-style</c>.</summary>
    public StyleBuilder FontStyle(FontStyle v) => E(StyleProp.FontStyle, (int)v);

    /// <summary>Sets <c>line-height</c>; a percentage is of the font size, auto uses the font's own.</summary>
    public StyleBuilder LineHeight(Length v) => L(StyleProp.LineHeight, v);

    /// <summary>Sets <c>letter-spacing</c>.</summary>
    public StyleBuilder LetterSpacing(Length v) => L(StyleProp.LetterSpacing, v);

    /// <summary>Sets <c>text-align</c>.</summary>
    public StyleBuilder TextAlign(TextAlignment v) => E(StyleProp.TextAlign, (int)v);

    /// <summary>Sets <c>white-space</c>.</summary>
    public StyleBuilder WhiteSpace(WhiteSpace v) => E(StyleProp.WhiteSpace, (int)v);

    /// <summary>Records <c>transition</c> data, such as <c>"opacity 150ms ease-out"</c>. Nothing animates yet; the value is kept for the tween system.</summary>
    public StyleBuilder Transition(string v) => T(StyleProp.Transition, v);

    // ---- plumbing ------------------------------------------------------------------------------------------------

    private StyleBuilder L(StyleProp p, Length v) => Put(new Declaration(p, v));

    private StyleBuilder N(StyleProp p, float v) => Put(new Declaration(p, StyleValue.Number(v)));

    private StyleBuilder C(StyleProp p, Color v) => Put(new Declaration(p, v));

    private StyleBuilder E(StyleProp p, int v) => Put(new Declaration(p, StyleValue.OfEnum(v)));

    private StyleBuilder T(StyleProp p, string v) => Put(new Declaration(p, v));

    private StyleBuilder Put(Declaration d)
    {
        for (var i = 0; i < _decls.Count; i++)
        {
            var e = _decls[i];
            if (e.Prop == d.Prop && string.Equals(e.TokenName, d.TokenName, StringComparison.Ordinal))
            {
                if (e != d)
                {
                    _decls[i] = d;
                    _owner?.InlineStyleChanged(d);
                }

                return this;
            }
        }

        _decls.Add(d);
        _owner?.InlineStyleChanged(d);
        return this;
    }

    private static StyleProp Lookup(string cssName)
    {
        if (!PropertyTable.TryGet(cssName, out var p))
        {
            throw new UiStyleException($"Unknown style property '{cssName}'.");
        }

        return p;
    }

    private static void CheckToken(string name)
    {
        if (name is null || !name.StartsWith("--", StringComparison.Ordinal) || name.Length < 3)
        {
            throw new UiStyleException($"Token names start with '--', got '{name}'.");
        }
    }
}

/// <summary>A style rule, selector or property was malformed.</summary>
public class UiStyleException : Exception
{
    /// <summary>Creates the exception with a message that says what to fix.</summary>
    public UiStyleException(string message) : base(message)
    {
    }
}
