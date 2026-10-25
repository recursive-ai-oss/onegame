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
    public Color TextDim { get; init; } = Color.Hex("#8e97a9");

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
        sheet.Rule(":root", s =>
        {
            foreach (var (name, color) in Palette(t))
            {
                s.Token(name, color);
            }

            s.Token("--kit-font-size", t.FontSize).Token("--kit-radius", t.Radius).Token("--kit-focus-width", t.FocusWidth)
                .Var("font-size", "--kit-font-size").Var("color", "--kit-text");
        });

        // Focus ring: an outline, so it takes no layout space.
        // The 2 px gap shows the surface behind the control, so the ring is read against that surface and not against a primary or danger fill it surrounds.
        sheet.Rule(".kit-focusable:focus", s => s.Var("outline-width", "--kit-focus-width").Var("outline-color", "--kit-focus").Set("outline-offset", Length.Px(2)));

        // Button
        sheet.Rule(".kit-button", s => Rounded(s).Padding(Length.Ui(6), Length.Ui(14)).MinHeight(Length.Ui(32)).AlignItems(Align.Center).JustifyContent(Align.Center)
            .Var("background-color", "--kit-raised").BorderWidth(Length.Px(1)).Var("border-color", "--kit-border").Cursor(CursorShape.Hand));
        sheet.Rule(".kit-button:hover", s => s.Var("background-color", "--kit-hover"));
        sheet.Rule(".kit-button:active", s => s.Var("background-color", "--kit-active"));
        sheet.Rule(".kit-button.primary", s => s.Var("background-color", "--kit-accent").Var("color", "--kit-accent-text").Var("border-color", "--kit-accent"));
        sheet.Rule(".kit-button.primary:hover", s => s.Var("background-color", "--kit-accent-hover").Var("border-color", "--kit-accent-hover"));
        sheet.Rule(".kit-button.primary:active", s => s.Var("background-color", "--kit-accent-active").Var("border-color", "--kit-accent-active"));
        sheet.Rule(".kit-button.danger", s => s.Var("background-color", "--kit-danger").Var("border-color", "--kit-danger").Var("color", "--kit-danger-text"));
        sheet.Rule(".kit-button.danger:hover", s => s.Var("background-color", "--kit-danger-hover").Var("border-color", "--kit-danger-hover"));
        sheet.Rule(".kit-button.danger:active", s => s.Var("background-color", "--kit-danger-active").Var("border-color", "--kit-danger-active"));
        sheet.Rule(".kit-button.flat", s => s.Background(Color.Transparent).BorderColor(Color.Transparent));
        sheet.Rule(".kit-button.flat:hover", s => s.Var("background-color", "--kit-hover"));
        sheet.Rule(".kit-button.flat:active", s => s.Var("background-color", "--kit-active"));
        // Disabled is a flat, dim control for every variant: no accent or danger fill, no hover or pressed change, muted text. It is listed last so it wins every state rule above.
        sheet.Rule(".kit-button:disabled, .kit-button.primary:disabled, .kit-button.danger:disabled", s => s.Var("background-color", "--kit-disabled-fill").Var("border-color", "--kit-disabled-border")
            .Var("color", "--kit-disabled-text").Cursor(CursorShape.NotAllowed));
        sheet.Rule(".kit-button.flat:disabled", s => s.Background(Color.Transparent).BorderColor(Color.Transparent).Var("color", "--kit-disabled-text").Cursor(CursorShape.NotAllowed));

        // Checkbox and radio
        sheet.Rule(".kit-toggle, .kit-radio", s => s.AlignItems(Align.Center).Gap(Length.Ui(8)).Padding(Length.Ui(4), Length.Ui(6)).Cursor(CursorShape.Hand));
        sheet.Rule(".kit-toggle-box", s => Rounded(s).Size(Length.Ui(18), Length.Ui(18)).Shrink(0).AlignItems(Align.Center).JustifyContent(Align.Center)
            .Var("background-color", "--kit-field").BorderWidth(Length.Px(1)).Var("border-color", "--kit-control-border"));
        sheet.Rule(".kit-toggle-box.checked", s => s.Var("background-color", "--kit-accent").Var("border-color", "--kit-accent"));
        sheet.Rule(".kit-toggle:hover .kit-toggle-box", s => s.Var("background-color", "--kit-hover"));
        sheet.Rule(".kit-toggle:hover .kit-toggle-box.checked", s => s.Var("background-color", "--kit-accent-hover").Var("border-color", "--kit-accent-hover"));
        sheet.Rule(".kit-toggle:active .kit-toggle-box", s => s.Var("background-color", "--kit-active"));
        sheet.Rule(".kit-toggle:active .kit-toggle-box.checked", s => s.Var("background-color", "--kit-accent-active").Var("border-color", "--kit-accent-active"));
        sheet.Rule(".kit-toggle-mark", s => s.Size(Length.Ui(8), Length.Ui(8)).Radius(Length.Ui(2)).Var("background-color", "--kit-accent-text").Opacity(0));
        sheet.Rule(".kit-toggle-mark.checked", s => s.Opacity(1));
        sheet.Rule(".kit-radiogroup", s => s.Direction(FlexDirection.Column).Gap(Length.Ui(2)));
        sheet.Rule(".kit-radiogroup.horizontal", s => s.Direction(FlexDirection.Row).Gap(Length.Ui(12)));
        sheet.Rule(".kit-radio-ring", s => s.Size(Length.Ui(18), Length.Ui(18)).Shrink(0).Radius(Length.Ui(9)).AlignItems(Align.Center).JustifyContent(Align.Center)
            .Var("background-color", "--kit-field").BorderWidth(Length.Px(1)).Var("border-color", "--kit-control-border"));
        sheet.Rule(".kit-radio-ring.checked", s => s.Var("border-color", "--kit-accent"));
        sheet.Rule(".kit-radio:hover .kit-radio-ring", s => s.Var("background-color", "--kit-hover"));
        sheet.Rule(".kit-radio:active .kit-radio-ring", s => s.Var("background-color", "--kit-active"));
        sheet.Rule(".kit-radio-dot", s => s.Size(Length.Ui(8), Length.Ui(8)).Radius(Length.Ui(4)).Var("background-color", "--kit-accent").Opacity(0));
        sheet.Rule(".kit-radio-dot.checked", s => s.Opacity(1));
        // Disabled toggles keep their checked shape in the muted colors: a gray box with a dark mark, a gray ring with a gray dot.
        sheet.Rule(".kit-toggle:disabled, .kit-radio:disabled", s => s.Var("color", "--kit-disabled-text").Cursor(CursorShape.NotAllowed));
        sheet.Rule(".kit-toggle:disabled .kit-toggle-box, .kit-radio:disabled .kit-radio-ring", s => s.Var("background-color", "--kit-disabled-fill").Var("border-color", "--kit-disabled-border"));
        sheet.Rule(".kit-toggle:disabled .kit-toggle-box.checked", s => s.Var("background-color", "--kit-disabled-text").Var("border-color", "--kit-disabled-text"));
        sheet.Rule(".kit-toggle:disabled .kit-toggle-mark", s => s.Var("background-color", "--kit-disabled-fill"));
        sheet.Rule(".kit-radio:disabled .kit-radio-ring.checked", s => s.Var("border-color", "--kit-disabled-text"));
        sheet.Rule(".kit-radio:disabled .kit-radio-dot", s => s.Var("background-color", "--kit-disabled-text"));

        // Slider and stepper
        sheet.Rule(".kit-slider", s => s.AlignItems(Align.Center).Gap(Length.Ui(12)).Padding(Length.Ui(0), Length.Ui(10)).MinHeight(Length.Ui(28)).MinWidth(Length.Ui(160)).Cursor(CursorShape.Hand));
        sheet.Rule(".kit-slider-track", s => s.Grow(1).Height(Length.Ui(6)).Radius(Length.Ui(3)).Var("background-color", "--kit-field").BorderWidth(Length.Px(1)).Var("border-color", "--kit-control-border"));
        sheet.Rule(".kit-slider-fill", s => s.Position(PositionMode.Absolute).Left(0f).Top(0f).Bottom(0f).Radius(Length.Ui(3)).Var("background-color", "--kit-accent"));
        sheet.Rule(".kit-slider-thumb", s => s.Position(PositionMode.Absolute).Top(Length.Percent(50)).Translate(Length.Percent(-50), Length.Percent(-50))
            .Size(Length.Ui(16), Length.Ui(16)).Radius(Length.Ui(8)).Var("background-color", "--kit-text").BorderWidth(Length.Px(2)).Var("border-color", "--kit-accent"));
        sheet.Rule(".kit-slider:hover .kit-slider-thumb", s => s.Var("border-color", "--kit-accent-hover"));
        sheet.Rule(".kit-slider:active .kit-slider-thumb", s => s.Var("background-color", "--kit-accent"));
        sheet.Rule(".kit-slider:disabled", s => s.Var("color", "--kit-disabled-text").Cursor(CursorShape.NotAllowed));
        sheet.Rule(".kit-slider:disabled .kit-slider-track", s => s.Var("background-color", "--kit-disabled-fill").Var("border-color", "--kit-disabled-border"));
        sheet.Rule(".kit-slider:disabled .kit-slider-fill", s => s.Var("background-color", "--kit-disabled-border"));
        sheet.Rule(".kit-slider:disabled .kit-slider-thumb", s => s.Var("background-color", "--kit-disabled-text").Var("border-color", "--kit-disabled-fill"));
        sheet.Rule(".kit-slider-value", s => s.MinWidth(Length.Ui(40)).TextAlign(TextAlignment.End));
        sheet.Rule(".kit-stepper", s => s.AlignItems(Align.Center).AlignSelf(Align.Start).Gap(Length.Ui(4)));
        sheet.Rule(".kit-stepper-button", s => Rounded(s).Size(Length.Ui(28), Length.Ui(28)).AlignItems(Align.Center).JustifyContent(Align.Center)
            .Var("background-color", "--kit-raised").BorderWidth(Length.Px(1)).Var("border-color", "--kit-border").Cursor(CursorShape.Hand));
        sheet.Rule(".kit-stepper-button:hover", s => s.Var("background-color", "--kit-hover"));
        sheet.Rule(".kit-stepper-button:active", s => s.Var("background-color", "--kit-active"));
        sheet.Rule(".kit-stepper-button:disabled", s => s.Var("background-color", "--kit-disabled-fill").Var("border-color", "--kit-disabled-border").Var("color", "--kit-disabled-text").Cursor(CursorShape.NotAllowed));
        sheet.Rule(".kit-stepper:disabled .kit-stepper-value", s => s.Var("color", "--kit-disabled-text"));
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
        sheet.Rule(".kit-tab:hover", s => s.Var("background-color", "--kit-hover").Var("color", "--kit-text"));
        sheet.Rule(".kit-tab:active", s => s.Var("background-color", "--kit-active").Var("color", "--kit-text"));
        sheet.Rule(".kit-tab.selected", s => s.Var("border-color", "--kit-accent").Var("color", "--kit-text"));
        sheet.Rule(".kit-tab:disabled, .kit-tab.selected:disabled", s => s.Background(Color.Transparent).Var("color", "--kit-disabled-text").Cursor(CursorShape.NotAllowed));
        sheet.Rule(".kit-tab.selected:disabled", s => s.Var("border-color", "--kit-disabled-border"));
        sheet.Rule(".kit-tab:focus", s => s.Set("outline-offset", Length.Px(-2)));
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
        // A selected row is shown by its fill, its text color and an accent frame inside the row (an outline, so rows keep their size), which stays 3:1 against the list.
        sheet.Rule(".kit-row.selected", s => s.Var("background-color", "--kit-select").Var("color", "--kit-select-text").Var("outline-color", "--kit-accent").Set("outline-width", Length.Px(2)).Set("outline-offset", Length.Px(-2)));
        sheet.Rule(".kit-list:disabled", s => s.Var("background-color", "--kit-disabled-fill").Var("border-color", "--kit-disabled-border").Var("color", "--kit-disabled-text"));
        sheet.Rule(".kit-list:disabled .kit-row, .kit-list:disabled .kit-row.selected", s => s.Background(Color.Transparent).Var("color", "--kit-disabled-text").Var("outline-color", "--kit-disabled-border"));
        sheet.Rule(".kit-list:disabled .kit-row.selected", s => s.Var("background-color", "--kit-disabled-border"));
        sheet.Rule(".kit-cell", s => s.Padding(Length.Px(0), Length.Ui(8)).Overflow(OverflowMode.Hidden).WhiteSpace(WhiteSpace.NoWrap));
        sheet.Rule(".kit-cell.end", s => s.TextAlign(TextAlignment.End));
        sheet.Rule(".kit-table", s => Rounded(s).Direction(FlexDirection.Column).Grow(1).MinHeight(0f).Overflow(OverflowMode.Hidden).Var("background-color", "--kit-field")
            .BorderWidth(Length.Px(1)).Var("border-color", "--kit-border"));
        // The table draws the one border around header and body, so their columns line up.
        sheet.Rule(".kit-table-body", s => s.BorderWidth(Length.Px(0)).Radius(Length.Px(0)));
        sheet.Rule(".kit-table-header", s => s.Var("background-color", "--kit-raised").BorderWidth(Length.Px(0), Length.Px(0), Length.Px(1), Length.Px(0)).Var("border-color", "--kit-border"));
        sheet.Rule(".kit-header-cell", s => s.AlignItems(Align.Center).Gap(Length.Ui(4)).Padding(Length.Ui(6), Length.Ui(8)).MinHeight(Length.Ui(30)).Overflow(OverflowMode.Hidden));
        sheet.Rule(".kit-header-cell:hover", s => s.Var("background-color", "--kit-hover"));
        sheet.Rule(".kit-header-cell:active", s => s.Var("background-color", "--kit-active"));
        sheet.Rule(".kit-header-cell:focus", s => s.Set("outline-offset", Length.Px(-2)));
        sheet.Rule(".kit-header-cell.end", s => s.JustifyContent(Align.End));
        sheet.Rule(".kit-header-title", s => s.FontWeight(700).WhiteSpace(WhiteSpace.NoWrap));
        sheet.Rule(".kit-sort", s => s.Size(Length.Ui(10), Length.Ui(10)).Shrink(0).Opacity(0).Var("background-tint", "--kit-text"));
        sheet.Rule(".kit-sort.sorted", s => s.Opacity(1));
        sheet.Rule(".kit-sort.asc", s => s.BackgroundImage(KitArt.SortAscending));
        sheet.Rule(".kit-sort.desc", s => s.BackgroundImage(KitArt.SortDescending));

        // Drop-down
        sheet.Rule(".kit-dropdown", s => s.MinWidth(Length.Ui(140)));
        sheet.Rule(".kit-dropdown-head", s => Rounded(s).Grow(1).Padding(Length.Ui(6), Length.Ui(10)).MinHeight(Length.Ui(32)).AlignItems(Align.Center).Gap(Length.Ui(8))
            .Var("background-color", "--kit-field").BorderWidth(Length.Px(1)).Var("border-color", "--kit-control-border").Cursor(CursorShape.Hand));
        sheet.Rule(".kit-dropdown-head:hover", s => s.Var("background-color", "--kit-hover"));
        sheet.Rule(".kit-dropdown-head:active", s => s.Var("background-color", "--kit-active"));
        sheet.Rule(".kit-dropdown.open .kit-dropdown-head", s => s.Var("border-color", "--kit-accent"));
        sheet.Rule(".kit-dropdown-head:disabled", s => s.Var("background-color", "--kit-disabled-fill").Var("border-color", "--kit-disabled-border").Var("color", "--kit-disabled-text").Cursor(CursorShape.NotAllowed));
        sheet.Rule(".kit-dropdown-label", s => s.Grow(1).WhiteSpace(WhiteSpace.NoWrap));
        sheet.Rule(".kit-dropdown-arrow", s => s.Size(Length.Ui(12), Length.Ui(12)).Shrink(0).BackgroundImage(KitArt.ChevronDown).Var("background-tint", "--kit-text"));
        sheet.Rule(".kit-dropdown-head:disabled .kit-dropdown-arrow", s => s.Var("background-tint", "--kit-disabled-text"));
        sheet.Rule(".kit-dropdown-popup", s => Rounded(s).Position(PositionMode.Absolute).Left(0f).Top(Length.Percent(100)).MinWidth(Length.Percent(100)).Direction(FlexDirection.Column)
            .MaxHeight(Length.Ui(240)).Overflow(OverflowMode.Hidden, OverflowMode.Auto).Var("background-color", "--kit-raised").BorderWidth(Length.Px(1)).Var("border-color", "--kit-border")
            .Shadow(Length.Px(0), Length.Ui(4), Length.Ui(12), Length.Px(0), Color.Hex("#00000080")));
        sheet.Rule(".kit-dropdown-popup.up", s => s.Top(Length.Auto).Bottom(Length.Percent(100)));
        sheet.Rule(".kit-option", s => s.Padding(Length.Ui(6), Length.Ui(12)).Shrink(0).Cursor(CursorShape.Hand));
        sheet.Rule(".kit-option:hover, .kit-option:focus", s => s.Var("background-color", "--kit-hover"));
        sheet.Rule(".kit-option:active", s => s.Var("background-color", "--kit-active"));
        sheet.Rule(".kit-option.selected", s => s.Var("background-color", "--kit-select").Var("color", "--kit-select-text"));
        // Options sit inside a clipped popup, so the ring is drawn inside the option.
        sheet.Rule(".kit-option:focus", s => s.Set("outline-offset", Length.Px(-2)));

        // Dialog
        sheet.Rule(".kit-dialog", s => s.AlignItems(Align.Center).JustifyContent(Align.Center).Var("background-color", "--kit-scrim"));
        sheet.Rule(".kit-dialog-panel", s => s.Direction(FlexDirection.Column).Gap(Length.Ui(12)).Padding(Length.Ui(16)).MinWidth(Length.Ui(280)).MaxWidth(Length.Ui(480))
            .Radius(Length.Ui(8)).Var("background-color", "--kit-panel").BorderWidth(Length.Px(1)).Var("border-color", "--kit-border")
            .Shadow(Length.Px(0), Length.Ui(8), Length.Ui(24), Length.Px(0), Color.Hex("#000000a0")));
        sheet.Rule(".kit-dialog-title", s => s.FontWeight(700).FontSize(Length.Ui(18)));
        sheet.Rule(".kit-dialog-buttons", s => s.JustifyContent(Align.End).Gap(Length.Ui(8)));

        // Text input
        sheet.Rule(".kit-input", s => Rounded(s).AlignItems(Align.Center).Padding(Length.Ui(4), Length.Ui(8)).MinHeight(Length.Ui(32)).MinWidth(Length.Ui(160))
            .Var("background-color", "--kit-field").BorderWidth(Length.Px(1)).Var("border-color", "--kit-control-border"));
        sheet.Rule(".kit-input:hover", s => s.Var("border-color", "--kit-text-dim"));
        sheet.Rule(".kit-input:focus", s => s.Var("border-color", "--kit-focus"));
        sheet.Rule(".kit-input:disabled", s => s.Var("background-color", "--kit-disabled-fill").Var("border-color", "--kit-disabled-border").Var("color", "--kit-disabled-text").Cursor(CursorShape.NotAllowed));
        sheet.Rule(".kit-input:disabled .kit-placeholder", s => s.Var("color", "--kit-disabled-text"));
        sheet.Rule(".kit-input-content", s => s.Shrink(0).Grow(1).Padding(Length.Px(0), Length.Ui(3), Length.Px(0), Length.Px(0)));
        sheet.Rule(".kit-input-text", s => s.WhiteSpace(WhiteSpace.Pre));
        sheet.Rule(".kit-placeholder", s => s.WhiteSpace(WhiteSpace.Pre).Var("color", "--kit-text-dim"));
        sheet.Rule(".kit-caret", s => s.Position(PositionMode.Absolute).Top(0f).Bottom(0f).Width(Length.Ui(1.5f)).Var("background-color", "--kit-text"));
        sheet.Rule(".kit-selection", s => s.Position(PositionMode.Absolute).Top(0f).Bottom(0f).Var("background-color", "--kit-select"));

        // Tooltip and input glyphs
        sheet.Rule(".kit-tooltip", s => Rounded(s).Padding(Length.Ui(6), Length.Ui(10)).MaxWidth(Length.Ui(280)).Var("background-color", "--kit-raised").BorderWidth(Length.Px(1)).Var("border-color", "--kit-border")
            .Shadow(Length.Px(0), Length.Ui(4), Length.Ui(10), Length.Px(0), Color.Hex("#00000080")));
        sheet.Rule(".kit-glyph", s => s.Size(Length.Ui(24), Length.Ui(24)).Shrink(0).AlignItems(Align.Center).JustifyContent(Align.Center));
        sheet.Rule(".kit-glyph.keyboard", s => s.Height(Length.Ui(24)).Width(Length.Auto).MinWidth(Length.Ui(24)).Padding(Length.Px(0), Length.Ui(6)).FontSize(Length.Ui(12)).TextColor(Color.Hex("#e6e8ee")));  // the key cap art is always dark
        sheet.Rule(".kit-glyph.unbound", s => s.Opacity(0));
        return sheet;
    }

    /// <summary>
    /// The color tokens the theme defines, in a fixed order. The ones that carry text or an indicator are made legible: a token that would fall under the WCAG AA ratio against what it is
    /// drawn on is moved toward white or black just far enough (<see cref="KitContrast.Ensure"/>), and the state colors (hover, pressed and disabled fills, the text on them) are derived from
    /// the base tokens, so a game theme made of kit tokens cannot end up unreadable. <see cref="ThemeLint"/> reports what is still wrong when a game overrides tokens by hand.
    /// </summary>
    internal static (string Name, Color Color)[] Palette(KitTokens t)
    {
        var bg = t.Background;
        var panel = t.Panel;
        var raised = t.Raised;
        var field = t.Field;
        var hover = t.Hover;
        var active = t.Active;
        var accentActive = KitContrast.Mix(t.Accent, Color.Black, 0.2);
        var dangerHover = KitContrast.Mix(t.Danger, Color.Black, 0.12);
        var dangerActive = KitContrast.Mix(t.Danger, Color.Black, 0.28);

        // Text: AA 4.5:1 on every surface it sits on. Primary text must hold on the accent in all of its states.
        var text = KitContrast.Ensure(t.Text, [bg, panel, raised, field, hover, active, t.Selection], KitContrast.TextAa, 7);
        var dim = KitContrast.Ensure(t.TextDim, [bg, panel, raised, field], KitContrast.TextAa, 5.5);
        // The hover fill is pulled toward the accent when no text color can be read on it as well as on the accent itself.
        var accentHover = t.AccentHover;
        var accentText = KitContrast.Ensure(t.AccentText, [t.Accent, accentHover, accentActive], KitContrast.TextAa, 7);
        for (var step = 1; step <= 10 && KitContrast.Worst(accentText, [t.Accent, accentHover, accentActive]) < KitContrast.TextAa; step++)
        {
            accentHover = KitContrast.Mix(t.AccentHover, t.Accent, step / 10.0);
            accentText = KitContrast.Ensure(t.AccentText, [t.Accent, accentHover, accentActive], KitContrast.TextAa, 7);
        }
        var dangerText = KitContrast.Ensure(Color.White, [t.Danger, dangerHover, dangerActive], KitContrast.TextAa, 7);
        var selectText = KitContrast.Ensure(t.SelectionText, [t.Selection], KitContrast.TextAa, 7);

        // Indicators: AA 3:1 against the surfaces they are drawn on. The focus ring is also checked against raised parts, because a ring sits on whatever is behind its button.
        var focus = KitContrast.Ensure(t.Focus, [bg, panel, raised, field], KitContrast.IndicatorAa);
        var controlBorder = KitContrast.Ensure(t.Border, [bg, panel, field], KitContrast.IndicatorAa);
        var scrollbar = KitContrast.Ensure(t.Scrollbar, [bg, panel, field], KitContrast.IndicatorAa);

        // Disabled: flatter than any enabled control (a fill between raised and panel, a dimmer outline) with text that is dimmer than the dim text but still 3:1.
        var disabledFill = KitContrast.Mix(raised, panel, 0.6);
        var disabledBorder = KitContrast.Mix(t.Border, panel, 0.5);
        var disabledText = KitContrast.Ensure(KitContrast.Mix(dim, disabledFill, 0.4), [disabledFill, bg, panel, field], KitContrast.IndicatorAa);

        return
        [
            ("--kit-bg", bg), ("--kit-panel", panel), ("--kit-raised", raised), ("--kit-field", field),
            ("--kit-text", text), ("--kit-text-dim", dim),
            ("--kit-accent", t.Accent), ("--kit-accent-hover", accentHover), ("--kit-accent-active", accentActive), ("--kit-accent-text", accentText),
            ("--kit-danger", t.Danger), ("--kit-danger-hover", dangerHover), ("--kit-danger-active", dangerActive), ("--kit-danger-text", dangerText),
            ("--kit-border", t.Border), ("--kit-control-border", controlBorder), ("--kit-focus", focus),
            ("--kit-hover", hover), ("--kit-active", active), ("--kit-select", t.Selection), ("--kit-select-text", selectText),
            ("--kit-scrim", t.Scrim), ("--kit-scrollbar", scrollbar),
            ("--kit-disabled-fill", disabledFill), ("--kit-disabled-border", disabledBorder), ("--kit-disabled-text", disabledText),
        ];
    }

    // Corner radius from the theme token, on all four corners.
    private static StyleBuilder Rounded(StyleBuilder s) => s
        .Var("border-top-left-radius", "--kit-radius").Var("border-top-right-radius", "--kit-radius")
        .Var("border-bottom-right-radius", "--kit-radius").Var("border-bottom-left-radius", "--kit-radius");
}
