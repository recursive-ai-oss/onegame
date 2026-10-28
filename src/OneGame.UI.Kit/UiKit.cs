namespace OneGame.UI.Kit;

/// <summary>
/// The component kit: factory methods that return ready-made <see cref="Element"/> trees for the controls a game screen needs.
/// Every component is "controlled": it reads its value from a lambda you give it and reports changes through another, so the game's own
/// state stays the single source of truth. Components are styled by classes (<c>kit-button</c>, <c>kit-slider</c> and so on) and theme tokens
/// (<c>--kit-accent</c> and so on) from <see cref="KitTheme"/>, which a game extends instead of forking. They set <see cref="Element.Role"/> and
/// <see cref="Element.Value"/>, which the JSON dump prints, and take an optional <c>id</c> that also names their parts (<c>id/thumb</c>).
/// Install the theme once with <see cref="KitHost"/> (from <c>GameDefinition.Style</c>), then build screens from these methods.
/// </summary>
/// <example>
/// A settings screen: the lambdas read and write the game's own settings object, and the kit holds no state of its own.
/// <code>
/// screens.Add("settings", game =&gt; UiKit.Card("Settings",
///     UiKit.Slider(() =&gt; options.Volume, v =&gt; options.Volume = v, min: 0, max: 10, id: "volume"),
///     UiKit.Toggle("Fullscreen", () =&gt; options.Fullscreen, v =&gt; options.Fullscreen = v, id: "fullscreen"),
///     UiKit.Button("Back", () =&gt; game.OpenScreen("main"), id: "back", kind: ButtonKind.Primary)));
/// </code>
/// </example>
public static partial class UiKit
{
    /// <summary>The class every focusable component carries; the theme draws the focus ring on <c>.kit-focusable:focus</c>.</summary>
    public const string FocusableClass = "kit-focusable";

    /// <summary>The class that marks the element a screen should focus first; see <see cref="FocusHelpers.FocusDefault"/>.</summary>
    public const string DefaultFocusClass = "kit-default-focus";

    internal static Element Make(string kind, string role, string classes, string? id, bool focusable = false)
    {
        var e = new Element(kind) { Role = role, Id = id };
        e.Class(focusable ? classes + " " + FocusableClass : classes);
        e.IsFocusable = focusable;
        return e;
    }

    internal static Element Part(string kind, string classes, string? parentId, string? name)
    {
        var e = new Element(kind) { Id = parentId is null || name is null ? null : parentId + "/" + name };
        e.Class(classes);
        return e;
    }

    // Converts a length that is in pixels or UI units to pixels with the scale of the tree the element is in (1 before it is added).
    internal static float ToPx(Element e, Length l)
    {
        var scale = e.Tree?.Scale ?? 1;
        return l.Unit switch
        {
            LengthUnit.Px => l.Value,
            LengthUnit.Ui => l.Value * scale,
            _ => throw new ArgumentException("Row heights and similar sizes take pixels or UI units, not percentages or auto.", nameof(l)),
        };
    }

    internal static string Invariant(float v) => v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

    internal static bool IsKey(in UiEvent e, string key) => string.Equals(e.Key, key, StringComparison.Ordinal);

    internal static string BoolText(bool v) => v ? "true" : "false";
}

internal static class ElementPlacement
{
    // Moves an absolutely positioned element; the style is only touched when the value differs, so calling this every frame costs nothing.
    public static Element Top(this Element e, Length v) => e.Style(s => s.Top(v));

    public static Element Left(this Element e, Length v) => e.Style(s => s.Left(v));
}
