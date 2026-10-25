namespace OneGame.UI;

// The order here is the order of the table below and of ComputedStyle's storage.
internal enum StyleProp : byte
{
    Display, Position, Left, Top, Right, Bottom,
    Width, Height, MinWidth, MinHeight, MaxWidth, MaxHeight,
    MarginTop, MarginRight, MarginBottom, MarginLeft,
    PaddingTop, PaddingRight, PaddingBottom, PaddingLeft,
    BorderTopWidth, BorderRightWidth, BorderBottomWidth, BorderLeftWidth,
    BoxSizing,
    FlexDirection, FlexWrap, JustifyContent, AlignItems, AlignSelf, AlignContent,
    FlexGrow, FlexShrink, FlexBasis, RowGap, ColumnGap, Order,
    OverflowX, OverflowY,
    BackgroundColor, BackgroundImage, BackgroundSlice, BackgroundTint, BorderColor,
    RadiusTopLeft, RadiusTopRight, RadiusBottomRight, RadiusBottomLeft,
    Opacity, Visibility, ZIndex, PointerEvents, Cursor,
    OutlineWidth, OutlineColor, OutlineOffset,
    ShadowX, ShadowY, ShadowBlur, ShadowSpread, ShadowColor,
    TranslateX, TranslateY, Scale, Rotate,
    Color, FontFamily, FontSize, FontWeight, FontStyle, LineHeight, LetterSpacing, TextAlign, WhiteSpace,
    Transition,
    Count,
}

internal sealed class PropInfo
{
    public required string Name { get; init; }

    public required ValueKind Kind { get; init; }

    public required bool Inherited { get; init; }

    /// <summary>True when a change cannot move or resize anything, so layout need not rerun.</summary>
    public required bool PaintOnly { get; init; }

    public required StyleValue Initial { get; init; }

    public string[]? EnumNames { get; init; }
}

internal static class PropertyTable
{
    public static readonly PropInfo[] All = new PropInfo[(int)StyleProp.Count];

    private static readonly Dictionary<string, StyleProp> ByName = new(StringComparer.Ordinal);

    static PropertyTable()
    {
        var auto = Length.Auto;
        Length zero = 0f;

        Enum(StyleProp.Display, "display", false, false, DisplayMode.Flex, "flex", "none");
        Enum(StyleProp.Position, "position", false, false, PositionMode.Relative, "relative", "absolute", "fixed");
        foreach (var (p, n) in new[] { (StyleProp.Left, "left"), (StyleProp.Top, "top"), (StyleProp.Right, "right"), (StyleProp.Bottom, "bottom") })
        {
            Len(p, n, false, auto);
        }

        foreach (var (p, n) in new[] { (StyleProp.Width, "width"), (StyleProp.Height, "height"), (StyleProp.MinWidth, "min-width"), (StyleProp.MinHeight, "min-height"), (StyleProp.MaxWidth, "max-width"), (StyleProp.MaxHeight, "max-height") })
        {
            Len(p, n, false, auto);
        }

        foreach (var (p, n) in new[] { (StyleProp.MarginTop, "margin-top"), (StyleProp.MarginRight, "margin-right"), (StyleProp.MarginBottom, "margin-bottom"), (StyleProp.MarginLeft, "margin-left"),
                                       (StyleProp.PaddingTop, "padding-top"), (StyleProp.PaddingRight, "padding-right"), (StyleProp.PaddingBottom, "padding-bottom"), (StyleProp.PaddingLeft, "padding-left"),
                                       (StyleProp.BorderTopWidth, "border-top-width"), (StyleProp.BorderRightWidth, "border-right-width"), (StyleProp.BorderBottomWidth, "border-bottom-width"), (StyleProp.BorderLeftWidth, "border-left-width") })
        {
            Len(p, n, false, zero);
        }

        Enum(StyleProp.BoxSizing, "box-sizing", false, false, BoxSizing.BorderBox, "border-box", "content-box");
        Enum(StyleProp.FlexDirection, "flex-direction", false, false, FlexDirection.Row, "row", "row-reverse", "column", "column-reverse");
        Enum(StyleProp.FlexWrap, "flex-wrap", false, false, FlexWrap.NoWrap, "nowrap", "wrap", "wrap-reverse");
        var align = new[] { "auto", "flex-start", "flex-end", "center", "stretch", "baseline", "space-between", "space-around", "space-evenly" };
        Enum(StyleProp.JustifyContent, "justify-content", false, false, Align.Start, align);
        Enum(StyleProp.AlignItems, "align-items", false, false, Align.Stretch, align);
        Enum(StyleProp.AlignSelf, "align-self", false, false, Align.Auto, align);
        Enum(StyleProp.AlignContent, "align-content", false, false, Align.Stretch, align);
        Num(StyleProp.FlexGrow, "flex-grow", false, false, 0);
        Num(StyleProp.FlexShrink, "flex-shrink", false, false, 1);
        Len(StyleProp.FlexBasis, "flex-basis", false, auto);
        Len(StyleProp.RowGap, "row-gap", false, zero);
        Len(StyleProp.ColumnGap, "column-gap", false, zero);
        Num(StyleProp.Order, "order", false, false, 0);
        var overflow = new[] { "visible", "hidden", "scroll", "auto" };
        Enum(StyleProp.OverflowX, "overflow-x", false, false, OverflowMode.Visible, overflow);
        Enum(StyleProp.OverflowY, "overflow-y", false, false, OverflowMode.Visible, overflow);

        Col(StyleProp.BackgroundColor, "background-color", false, Color.Transparent);
        Txt(StyleProp.BackgroundImage, "background-image", false, true, "");
        Len(StyleProp.BackgroundSlice, "background-slice", false, zero, paintOnly: true);
        Col(StyleProp.BackgroundTint, "background-tint", false, Color.White);
        Col(StyleProp.BorderColor, "border-color", false, Color.Black);
        Len(StyleProp.RadiusTopLeft, "border-top-left-radius", false, zero, paintOnly: true);
        Len(StyleProp.RadiusTopRight, "border-top-right-radius", false, zero, paintOnly: true);
        Len(StyleProp.RadiusBottomRight, "border-bottom-right-radius", false, zero, paintOnly: true);
        Len(StyleProp.RadiusBottomLeft, "border-bottom-left-radius", false, zero, paintOnly: true);
        Num(StyleProp.Opacity, "opacity", false, true, 1);
        Enum(StyleProp.Visibility, "visibility", true, true, VisibilityMode.Visible, "visible", "hidden");
        Num(StyleProp.ZIndex, "z-index", false, true, 0);
        Enum(StyleProp.PointerEvents, "pointer-events", true, true, UI.PointerEvents.Auto, "auto", "none");
        Enum(StyleProp.Cursor, "cursor", true, true, CursorShape.Default, "default", "hand", "text", "grab", "not-allowed");
        Len(StyleProp.OutlineWidth, "outline-width", false, zero, paintOnly: true);
        Col(StyleProp.OutlineColor, "outline-color", false, Color.Black);
        Len(StyleProp.OutlineOffset, "outline-offset", false, zero, paintOnly: true);
        Len(StyleProp.ShadowX, "shadow-offset-x", false, zero, paintOnly: true);
        Len(StyleProp.ShadowY, "shadow-offset-y", false, zero, paintOnly: true);
        Len(StyleProp.ShadowBlur, "shadow-blur", false, zero, paintOnly: true);
        Len(StyleProp.ShadowSpread, "shadow-spread", false, zero, paintOnly: true);
        Col(StyleProp.ShadowColor, "shadow-color", false, Color.Transparent);
        Len(StyleProp.TranslateX, "translate-x", false, zero, paintOnly: true);
        Len(StyleProp.TranslateY, "translate-y", false, zero, paintOnly: true);
        Num(StyleProp.Scale, "scale", false, true, 1);
        Num(StyleProp.Rotate, "rotate", false, true, 0);

        Col(StyleProp.Color, "color", true, Color.Black);
        Set(StyleProp.FontFamily, "font-family", ValueKind.Text, true, false, "sans-serif", null);
        Len(StyleProp.FontSize, "font-size", true, Length.Px(16));
        Num(StyleProp.FontWeight, "font-weight", true, false, 400);
        Enum(StyleProp.FontStyle, "font-style", true, false, UI.FontStyle.Normal, "normal", "italic");
        Len(StyleProp.LineHeight, "line-height", true, auto);
        Len(StyleProp.LetterSpacing, "letter-spacing", true, zero);
        Enum(StyleProp.TextAlign, "text-align", true, false, TextAlignment.Start, "start", "end", "center");
        Enum(StyleProp.WhiteSpace, "white-space", true, false, UI.WhiteSpace.Normal, "normal", "nowrap", "pre");
        Txt(StyleProp.Transition, "transition", false, true, "");

        for (var i = 0; i < All.Length; i++)
        {
            ByName[All[i].Name] = (StyleProp)i;
        }
    }

    public static bool TryGet(string cssName, out StyleProp prop) => ByName.TryGetValue(cssName, out prop);

    public static PropInfo Info(StyleProp p) => All[(int)p];

    /// <summary>Formats a computed value the way CSS would write it, using keyword names for enums.</summary>
    public static string Format(StyleProp p, in StyleValue v)
    {
        var names = All[(int)p].EnumNames;
        if (v.Kind == ValueKind.Enum && names is not null && (uint)v.AsEnum < names.Length)
        {
            return names[v.AsEnum];
        }

        return v.ToString();
    }

    private static void Set(StyleProp p, string name, ValueKind kind, bool inherited, bool paintOnly, StyleValue initial, string[]? names) =>
        All[(int)p] = new PropInfo { Name = name, Kind = kind, Inherited = inherited, PaintOnly = paintOnly, Initial = initial, EnumNames = names };

    private static void Len(StyleProp p, string name, bool inherited, Length initial, bool paintOnly = false) =>
        Set(p, name, ValueKind.Length, inherited, paintOnly, initial, null);

    private static void Num(StyleProp p, string name, bool inherited, bool paintOnly, float initial) =>
        Set(p, name, ValueKind.Number, inherited, paintOnly, StyleValue.Number(initial), null);

    private static void Col(StyleProp p, string name, bool inherited, Color initial) =>
        Set(p, name, ValueKind.Color, inherited, true, initial, null);

    private static void Txt(StyleProp p, string name, bool inherited, bool paintOnly, string initial) =>
        Set(p, name, ValueKind.Text, inherited, paintOnly, initial, null);

    private static void Enum<T>(StyleProp p, string name, bool inherited, bool paintOnly, T initial, params string[] names)
        where T : struct, Enum =>
        Set(p, name, ValueKind.Enum, inherited, paintOnly, StyleValue.OfEnum(System.Runtime.CompilerServices.Unsafe.As<T, int>(ref initial)), names);
}
