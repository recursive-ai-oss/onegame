namespace OneGame.UI.Kit;

/// <summary>
/// The values the default theme is made of. Colors and the font size, corner radius and focus-ring width become theme tokens (<c>--kit-bg</c>, <c>--kit-accent</c>, <c>--kit-font-size</c> and so on)
/// on the root element, and every kit rule reads them with <c>var()</c>, so changing a token restyles the whole UI without rebuilding it. Make a theme with <c>with</c>:
/// <c>KitTheme.Create(KitTokens.Dark with { Accent = Color.Hex("#5ab0ff") })</c>.
/// </summary>
public sealed record KitTokens
{
    /// <summary>The dark theme, the default.</summary>
    public static KitTokens Dark { get; } = new();

    /// <summary>A light theme.</summary>
    public static KitTokens Light { get; } = new()
    {
        Background = Color.Hex("#eef0f4"),
        Panel = Color.Hex("#ffffff"),
        Raised = Color.Hex("#e1e5ec"),
        Field = Color.Hex("#f7f8fa"),
        Text = Color.Hex("#1b1e26"),
        TextDim = Color.Hex("#616a7d"),
        Accent = Color.Hex("#1f6fd1"),
        AccentHover = Color.Hex("#3b84e0"),
        AccentText = Color.Hex("#ffffff"),
        Border = Color.Hex("#b8bfcc"),
        Hover = Color.Hex("#d3d9e4"),
        Active = Color.Hex("#c2cadb"),
        Selection = Color.Hex("#bcd3f5"),
        SelectionText = Color.Hex("#1b1e26"),
        Scrim = Color.Hex("#101820a0"),
    };

    /// <summary>Screen background (<c>--kit-bg</c>).</summary>
    public Color Background { get; init; } = Color.Hex("#14161a");

    /// <summary>Panels, cards and dialogs (<c>--kit-panel</c>).</summary>
    public Color Panel { get; init; } = Color.Hex("#1e2128");

    /// <summary>Buttons and raised parts (<c>--kit-raised</c>).</summary>
    public Color Raised { get; init; } = Color.Hex("#2b303a");

    /// <summary>Text fields, tracks and list backgrounds (<c>--kit-field</c>).</summary>
    public Color Field { get; init; } = Color.Hex("#171a20");

    /// <summary>Text (<c>--kit-text</c>).</summary>
    public Color Text { get; init; } = Color.Hex("#e8eaf0");

    /// <summary>Secondary text and placeholders (<c>--kit-text-dim</c>).</summary>
    public Color TextDim { get; init; } = Color.Hex("#8a93a6");

    /// <summary>The accent: primary buttons, filled parts, the selected tab (<c>--kit-accent</c>).</summary>
    public Color Accent { get; init; } = Color.Hex("#d9a441");

    /// <summary>The accent while hovered (<c>--kit-accent-hover</c>).</summary>
    public Color AccentHover { get; init; } = Color.Hex("#e6b755");

    /// <summary>Text drawn on the accent (<c>--kit-accent-text</c>).</summary>
    public Color AccentText { get; init; } = Color.Hex("#14161a");

    /// <summary>Destructive actions (<c>--kit-danger</c>).</summary>
    public Color Danger { get; init; } = Color.Hex("#c8493f");

    /// <summary>Outlines of controls and panels (<c>--kit-border</c>).</summary>
    public Color Border { get; init; } = Color.Hex("#3a4050");

    /// <summary>The focus ring (<c>--kit-focus</c>).</summary>
    public Color Focus { get; init; } = Color.Hex("#6cb6ff");

    /// <summary>Hovered rows and buttons (<c>--kit-hover</c>).</summary>
    public Color Hover { get; init; } = Color.Hex("#343a47");

    /// <summary>Pressed buttons (<c>--kit-active</c>).</summary>
    public Color Active { get; init; } = Color.Hex("#232832");

    /// <summary>The selected row, option or text (<c>--kit-select</c>).</summary>
    public Color Selection { get; init; } = Color.Hex("#3d4a63");

    /// <summary>Text on the selection (<c>--kit-select-text</c>).</summary>
    public Color SelectionText { get; init; } = Color.Hex("#ffffff");

    /// <summary>The dimmed layer behind a dialog (<c>--kit-scrim</c>).</summary>
    public Color Scrim { get; init; } = Color.Hex("#000000a0");

    /// <summary>Scroll bar thumbs (<c>--kit-scrollbar</c>).</summary>
    public Color Scrollbar { get; init; } = Color.Hex("#59627a");

    /// <summary>The base font size (<c>--kit-font-size</c>).</summary>
    public Length FontSize { get; init; } = Length.Ui(14);

    /// <summary>The corner radius of controls (<c>--kit-radius</c>).</summary>
    public Length Radius { get; init; } = Length.Ui(4);

    /// <summary>The width of the focus ring (<c>--kit-focus-width</c>).</summary>
    public Length FocusWidth { get; init; } = Length.Ui(2);
}

/// <summary>Builds the style sheet that gives every kit component its look. A game adds the sheet (or lets <see cref="KitHost"/> do it) and then adds its own rules, which win ties.</summary>
public static class KitTheme
{
    /// <summary>Creates the kit's style sheet from a set of tokens.</summary>
    /// <param name="tokens">The colors and sizes; <see cref="KitTokens.Dark"/> by default.</param>
    public static StyleSheet Create(KitTokens? tokens = null)
    {
        var t = tokens ?? KitTokens.Dark;
        var sheet = new StyleSheet();
        sheet.Rule(":root", s => s
            .Token("--kit-bg", t.Background).Token("--kit-panel", t.Panel).Token("--kit-raised", t.Raised).Token("--kit-field", t.Field)
            .Token("--kit-text", t.Text).Token("--kit-text-dim", t.TextDim).Token("--kit-accent", t.Accent).Token("--kit-accent-hover", t.AccentHover)
            .Token("--kit-accent-text", t.AccentText).Token("--kit-danger", t.Danger).Token("--kit-border", t.Border).Token("--kit-focus", t.Focus)
            .Token("--kit-hover", t.Hover).Token("--kit-active", t.Active).Token("--kit-select", t.Selection).Token("--kit-select-text", t.SelectionText)
            .Token("--kit-scrim", t.Scrim).Token("--kit-scrollbar", t.Scrollbar)
            .Token("--kit-font-size", t.FontSize).Token("--kit-radius", t.Radius).Token("--kit-focus-width", t.FocusWidth)
            .Var("font-size", "--kit-font-size").Var("color", "--kit-text"));

        // Focus ring: an outline, so it takes no layout space.
        sheet.Rule(".kit-focusable:focus", s => s.Var("outline-width", "--kit-focus-width").Var("outline-color", "--kit-focus").Set("outline-offset", Length.Px(1)));

        // Button
        sheet.Rule(".kit-button", s => Rounded(s).Padding(Length.Ui(6), Length.Ui(14)).MinHeight(Length.Ui(32)).AlignItems(Align.Center).JustifyContent(Align.Center)
            .Var("background-color", "--kit-raised").BorderWidth(Length.Px(1)).Var("border-color", "--kit-border").Cursor(CursorShape.Hand));
        sheet.Rule(".kit-button:hover", s => s.Var("background-color", "--kit-hover"));
        sheet.Rule(".kit-button:active", s => s.Var("background-color", "--kit-active"));
        sheet.Rule(".kit-button.primary", s => s.Var("background-color", "--kit-accent").Var("color", "--kit-accent-text").Var("border-color", "--kit-accent"));
        sheet.Rule(".kit-button.primary:hover", s => s.Var("background-color", "--kit-accent-hover"));
        sheet.Rule(".kit-button.danger", s => s.Var("background-color", "--kit-danger").Var("border-color", "--kit-danger").TextColor(Color.White));
        sheet.Rule(".kit-button.flat", s => s.Background(Color.Transparent).BorderColor(Color.Transparent));
        sheet.Rule(".kit-button.flat:hover", s => s.Var("background-color", "--kit-hover"));
        sheet.Rule(".kit-button:disabled", s => s.Opacity(0.5f).Cursor(CursorShape.NotAllowed));

        // Checkbox and radio
        sheet.Rule(".kit-toggle, .kit-radio", s => s.AlignItems(Align.Center).Gap(Length.Ui(8)).Padding(Length.Ui(4), Length.Ui(6)).Cursor(CursorShape.Hand));
        sheet.Rule(".kit-toggle-box", s => Rounded(s).Size(Length.Ui(18), Length.Ui(18)).Shrink(0).AlignItems(Align.Center).JustifyContent(Align.Center)
            .Var("background-color", "--kit-field").BorderWidth(Length.Px(1)).Var("border-color", "--kit-border"));
        sheet.Rule(".kit-toggle-box.checked", s => s.Var("background-color", "--kit-accent").Var("border-color", "--kit-accent"));
        sheet.Rule(".kit-toggle-mark", s => s.Size(Length.Ui(8), Length.Ui(8)).Radius(Length.Ui(2)).Var("background-color", "--kit-accent-text").Opacity(0));
        sheet.Rule(".kit-toggle-mark.checked", s => s.Opacity(1));
        sheet.Rule(".kit-radiogroup", s => s.Direction(FlexDirection.Column).Gap(Length.Ui(2)));
        sheet.Rule(".kit-radiogroup.horizontal", s => s.Direction(FlexDirection.Row).Gap(Length.Ui(12)));
        sheet.Rule(".kit-radio-ring", s => s.Size(Length.Ui(18), Length.Ui(18)).Shrink(0).Radius(Length.Ui(9)).AlignItems(Align.Center).JustifyContent(Align.Center)
            .Var("background-color", "--kit-field").BorderWidth(Length.Px(1)).Var("border-color", "--kit-border"));
        sheet.Rule(".kit-radio-ring.checked", s => s.Var("border-color", "--kit-accent"));
        sheet.Rule(".kit-radio-dot", s => s.Size(Length.Ui(8), Length.Ui(8)).Radius(Length.Ui(4)).Var("background-color", "--kit-accent").Opacity(0));
        sheet.Rule(".kit-radio-dot.checked", s => s.Opacity(1));

        // Slider and stepper
        sheet.Rule(".kit-slider", s => s.AlignItems(Align.Center).Gap(Length.Ui(12)).Padding(Length.Ui(0), Length.Ui(10)).MinHeight(Length.Ui(28)).MinWidth(Length.Ui(160)).Cursor(CursorShape.Hand));
        sheet.Rule(".kit-slider-track", s => s.Grow(1).Height(Length.Ui(6)).Radius(Length.Ui(3)).Var("background-color", "--kit-field").BorderWidth(Length.Px(1)).Var("border-color", "--kit-border"));
        sheet.Rule(".kit-slider-fill", s => s.Position(PositionMode.Absolute).Left(0f).Top(0f).Bottom(0f).Radius(Length.Ui(3)).Var("background-color", "--kit-accent"));
        sheet.Rule(".kit-slider-thumb", s => s.Position(PositionMode.Absolute).Top(Length.Percent(50)).Translate(Length.Percent(-50), Length.Percent(-50))
            .Size(Length.Ui(16), Length.Ui(16)).Radius(Length.Ui(8)).Var("background-color", "--kit-text").BorderWidth(Length.Px(2)).Var("border-color", "--kit-accent"));
        sheet.Rule(".kit-slider-value", s => s.MinWidth(Length.Ui(40)).TextAlign(TextAlignment.End));
        sheet.Rule(".kit-stepper", s => s.AlignItems(Align.Center).Gap(Length.Ui(4)));
        sheet.Rule(".kit-stepper-button", s => Rounded(s).Size(Length.Ui(28), Length.Ui(28)).AlignItems(Align.Center).JustifyContent(Align.Center)
            .Var("background-color", "--kit-raised").BorderWidth(Length.Px(1)).Var("border-color", "--kit-border").Cursor(CursorShape.Hand));
        sheet.Rule(".kit-stepper-button:hover", s => s.Var("background-color", "--kit-hover"));
        sheet.Rule(".kit-stepper-button:disabled", s => s.Opacity(0.4f));
        sheet.Rule(".kit-stepper-value", s => s.MinWidth(Length.Ui(48)).TextAlign(TextAlignment.Center));

        // Progress, card, frame
        sheet.Rule(".kit-progress", s => s.Height(Length.Ui(10)).MinWidth(Length.Ui(120)).Radius(Length.Ui(5)).Overflow(OverflowMode.Hidden).Var("background-color", "--kit-field"));
        sheet.Rule(".kit-progress-fill", s => s.Var("background-color", "--kit-accent"));
        sheet.Rule(".kit-card", s => Rounded(s).Direction(FlexDirection.Column).Padding(Length.Ui(12)).Gap(Length.Ui(8)).Var("background-color", "--kit-panel")
            .BorderWidth(Length.Px(1)).Var("border-color", "--kit-border"));
        sheet.Rule(".kit-card-title", s => s.FontWeight(700));
        sheet.Rule(".kit-card-body", s => s.Direction(FlexDirection.Column).Gap(Length.Ui(6)));
        sheet.Rule(".kit-frame", s => s.Direction(FlexDirection.Column).Gap(Length.Ui(6)));

        // Tabs
        sheet.Rule(".kit-tabs", s => s.Direction(FlexDirection.Column).Grow(1).MinHeight(0f));
        sheet.Rule(".kit-tablist", s => s.Gap(Length.Ui(2)).BorderWidth(Length.Px(0), Length.Px(0), Length.Px(1), Length.Px(0)).Var("border-color", "--kit-border"));
        sheet.Rule(".kit-tab", s => s.Padding(Length.Ui(8), Length.Ui(16)).Cursor(CursorShape.Hand).BorderWidth(Length.Px(0), Length.Px(0), Length.Ui(2), Length.Px(0)).BorderColor(Color.Transparent)
            .Var("color", "--kit-text-dim"));
        sheet.Rule(".kit-tab:hover", s => s.Var("background-color", "--kit-hover"));
        sheet.Rule(".kit-tab.selected", s => s.Var("border-color", "--kit-accent").Var("color", "--kit-text"));
        sheet.Rule(".kit-tabpanels", s => s.Direction(FlexDirection.Column).Grow(1).MinHeight(0f));
        sheet.Rule(".kit-tabpanel", s => s.Direction(FlexDirection.Column).Grow(1).MinHeight(0f).Padding(Length.Ui(8), Length.Px(0)));

        // Scroll view
        sheet.Rule(".kit-scrollview", s => s.Direction(FlexDirection.Column).Grow(1).MinHeight(0f).MinWidth(0f));
        sheet.Rule(".kit-scrollbar.vertical", s => s.Position(PositionMode.Absolute).Right(0f).Top(0f).Bottom(0f).Width(Length.Ui(10)));
        sheet.Rule(".kit-scrollbar.horizontal", s => s.Position(PositionMode.Absolute).Left(0f).Right(0f).Bottom(0f).Height(Length.Ui(10)));
        sheet.Rule(".kit-scrollbar-thumb", s => s.Position(PositionMode.Absolute).Radius(Length.Ui(4)).Var("background-color", "--kit-scrollbar").Cursor(CursorShape.Grab));
        sheet.Rule(".kit-scrollbar-thumb.vertical", s => s.Left(Length.Ui(2)).Right(Length.Ui(2)));
        sheet.Rule(".kit-scrollbar-thumb.horizontal", s => s.Top(Length.Ui(2)).Bottom(Length.Ui(2)));
        sheet.Rule(".kit-scrollbar-thumb:hover", s => s.Var("background-color", "--kit-text-dim"));

        // List and table
        sheet.Rule(".kit-list", s => Rounded(s).Direction(FlexDirection.Column).Grow(1).MinHeight(0f).Var("background-color", "--kit-field").BorderWidth(Length.Px(1)).Var("border-color", "--kit-border"));
        sheet.Rule(".kit-list-view", s => s.Grow(1));
        sheet.Rule(".kit-list-rows", s => s.Direction(FlexDirection.Column));
        sheet.Rule(".kit-row", s => s.AlignItems(Align.Center));
        sheet.Rule(".kit-row:hover", s => s.Var("background-color", "--kit-hover"));
        sheet.Rule(".kit-row.selected", s => s.Var("background-color", "--kit-select").Var("color", "--kit-select-text"));
        sheet.Rule(".kit-cell", s => s.Padding(Length.Px(0), Length.Ui(8)).Overflow(OverflowMode.Hidden).WhiteSpace(WhiteSpace.NoWrap));
        sheet.Rule(".kit-cell.end", s => s.TextAlign(TextAlignment.End));
        sheet.Rule(".kit-table", s => s.Direction(FlexDirection.Column).Grow(1).MinHeight(0f));
        sheet.Rule(".kit-table-header", s => s.Var("background-color", "--kit-raised").BorderWidth(Length.Px(0), Length.Px(0), Length.Px(1), Length.Px(0)).Var("border-color", "--kit-border"));
        sheet.Rule(".kit-header-cell", s => s.AlignItems(Align.Center).Gap(Length.Ui(4)).Padding(Length.Ui(6), Length.Ui(8)).MinHeight(Length.Ui(30)).Overflow(OverflowMode.Hidden));
        sheet.Rule(".kit-header-cell:hover", s => s.Var("background-color", "--kit-hover"));
        sheet.Rule(".kit-header-cell.end", s => s.JustifyContent(Align.End));
        sheet.Rule(".kit-header-title", s => s.FontWeight(700).WhiteSpace(WhiteSpace.NoWrap));
        sheet.Rule(".kit-sort", s => s.Size(Length.Ui(10), Length.Ui(10)).Shrink(0).Opacity(0));
        sheet.Rule(".kit-sort.sorted", s => s.Opacity(1));
        sheet.Rule(".kit-sort.asc", s => s.BackgroundImage(KitArt.SortAscending));
        sheet.Rule(".kit-sort.desc", s => s.BackgroundImage(KitArt.SortDescending));

        // Drop-down
        sheet.Rule(".kit-dropdown", s => s.MinWidth(Length.Ui(140)));
        sheet.Rule(".kit-dropdown-head", s => Rounded(s).Grow(1).Padding(Length.Ui(6), Length.Ui(10)).MinHeight(Length.Ui(32)).AlignItems(Align.Center).Gap(Length.Ui(8))
            .Var("background-color", "--kit-field").BorderWidth(Length.Px(1)).Var("border-color", "--kit-border").Cursor(CursorShape.Hand));
        sheet.Rule(".kit-dropdown-head:hover", s => s.Var("background-color", "--kit-hover"));
        sheet.Rule(".kit-dropdown-label", s => s.Grow(1).WhiteSpace(WhiteSpace.NoWrap));
        sheet.Rule(".kit-dropdown-arrow", s => s.Size(Length.Ui(12), Length.Ui(12)).Shrink(0).BackgroundImage(KitArt.ChevronDown));
        sheet.Rule(".kit-dropdown-popup", s => Rounded(s).Position(PositionMode.Absolute).Left(0f).Top(Length.Percent(100)).MinWidth(Length.Percent(100)).Direction(FlexDirection.Column)
            .MaxHeight(Length.Ui(240)).Overflow(OverflowMode.Hidden, OverflowMode.Auto).Var("background-color", "--kit-raised").BorderWidth(Length.Px(1)).Var("border-color", "--kit-border")
            .Shadow(Length.Px(0), Length.Ui(4), Length.Ui(12), Length.Px(0), Color.Hex("#00000080")));
        sheet.Rule(".kit-dropdown-popup.up", s => s.Top(Length.Auto).Bottom(Length.Percent(100)));
        sheet.Rule(".kit-option", s => s.Padding(Length.Ui(6), Length.Ui(12)).Shrink(0).Cursor(CursorShape.Hand));
        sheet.Rule(".kit-option:hover, .kit-option:focus", s => s.Var("background-color", "--kit-hover"));
        sheet.Rule(".kit-option.selected", s => s.Var("background-color", "--kit-select").Var("color", "--kit-select-text"));

        // Dialog
        sheet.Rule(".kit-dialog", s => s.AlignItems(Align.Center).JustifyContent(Align.Center).Var("background-color", "--kit-scrim"));
        sheet.Rule(".kit-dialog-panel", s => s.Direction(FlexDirection.Column).Gap(Length.Ui(12)).Padding(Length.Ui(16)).MinWidth(Length.Ui(280)).MaxWidth(Length.Ui(480))
            .Radius(Length.Ui(8)).Var("background-color", "--kit-panel").BorderWidth(Length.Px(1)).Var("border-color", "--kit-border")
            .Shadow(Length.Px(0), Length.Ui(8), Length.Ui(24), Length.Px(0), Color.Hex("#000000a0")));
        sheet.Rule(".kit-dialog-title", s => s.FontWeight(700).FontSize(Length.Ui(18)));
        sheet.Rule(".kit-dialog-buttons", s => s.JustifyContent(Align.End).Gap(Length.Ui(8)));

        // Text input
        sheet.Rule(".kit-input", s => Rounded(s).AlignItems(Align.Center).Padding(Length.Ui(4), Length.Ui(8)).MinHeight(Length.Ui(32)).MinWidth(Length.Ui(160))
            .Var("background-color", "--kit-field").BorderWidth(Length.Px(1)).Var("border-color", "--kit-border"));
        sheet.Rule(".kit-input:focus", s => s.Var("border-color", "--kit-focus"));
        sheet.Rule(".kit-input-content", s => s.Shrink(0).Grow(1).Padding(Length.Px(0), Length.Ui(3), Length.Px(0), Length.Px(0)));
        sheet.Rule(".kit-input-text", s => s.WhiteSpace(WhiteSpace.Pre));
        sheet.Rule(".kit-placeholder", s => s.WhiteSpace(WhiteSpace.Pre).Var("color", "--kit-text-dim"));
        sheet.Rule(".kit-caret", s => s.Position(PositionMode.Absolute).Top(0f).Bottom(0f).Width(Length.Ui(1.5f)).Var("background-color", "--kit-text"));
        sheet.Rule(".kit-selection", s => s.Position(PositionMode.Absolute).Top(0f).Bottom(0f).Var("background-color", "--kit-select"));

        // Tooltip and input glyphs
        sheet.Rule(".kit-tooltip", s => Rounded(s).Padding(Length.Ui(6), Length.Ui(10)).MaxWidth(Length.Ui(280)).Var("background-color", "--kit-raised").BorderWidth(Length.Px(1)).Var("border-color", "--kit-border")
            .Shadow(Length.Px(0), Length.Ui(4), Length.Ui(10), Length.Px(0), Color.Hex("#00000080")));
        sheet.Rule(".kit-glyph", s => s.Size(Length.Ui(24), Length.Ui(24)).Shrink(0).AlignItems(Align.Center).JustifyContent(Align.Center));
        sheet.Rule(".kit-glyph.keyboard", s => s.Height(Length.Ui(24)).Width(Length.Auto).MinWidth(Length.Ui(24)).Padding(Length.Px(0), Length.Ui(6)).FontSize(Length.Ui(12)));
        sheet.Rule(".kit-glyph.unbound", s => s.Opacity(0));
        return sheet;
    }

    // Corner radius from the theme token, on all four corners.
    private static StyleBuilder Rounded(StyleBuilder s) => s
        .Var("border-top-left-radius", "--kit-radius").Var("border-top-right-radius", "--kit-radius")
        .Var("border-bottom-right-radius", "--kit-radius").Var("border-bottom-left-radius", "--kit-radius");
}
