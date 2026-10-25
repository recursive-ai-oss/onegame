using System.Linq;

namespace OneGame.UI.Kit;

/// <summary>One foreground and background pair of the theme and how it fares against WCAG 2.x AA.</summary>
/// <param name="Name">What is checked, such as <c>text on panel</c> or <c>focus ring on field</c>.</param>
/// <param name="Foreground">The foreground token, such as <c>--kit-text</c>.</param>
/// <param name="Background">The background token.</param>
/// <param name="ForegroundColor">The foreground as <c>#rrggbb</c>, or <c>missing</c> when the token is not defined.</param>
/// <param name="BackgroundColor">The background as <c>#rrggbb</c>, or <c>missing</c>.</param>
/// <param name="Ratio">The contrast ratio, 1 to 21 (0 when a token is missing).</param>
/// <param name="Required">The ratio the pair must reach: 4.5 for text, 3 for disabled text and indicators.</param>
/// <param name="Passed">True when <paramref name="Ratio"/> reaches <paramref name="Required"/>.</param>
/// <param name="Kind"><c>text</c>, <c>disabled-text</c> or <c>indicator</c> (focus ring, control outline, filled accent, scroll thumb).</param>
public sealed record ContrastCheck(string Name, string Foreground, string Background, string ForegroundColor, string BackgroundColor, double Ratio, double Required, bool Passed, string Kind);

/// <summary>The outcome of <see cref="ThemeLint"/>.</summary>
/// <param name="Passed">True when every pair passed.</param>
/// <param name="Checked">How many pairs were checked.</param>
/// <param name="Failures">The pairs that failed, worst first.</param>
/// <param name="Checks">Every pair, in a fixed order.</param>
public sealed record ThemeLintReport(bool Passed, int Checked, IReadOnlyList<ContrastCheck> Failures, IReadOnlyList<ContrastCheck> Checks);

/// <summary>
/// Checks that a theme's colors stay legible. The pairs are the ones the kit's components actually draw: text on every surface it sits on, text on
/// the filled variants in each state, disabled text (which WCAG exempts, but which must stay readable at 3:1), and the non-text indicators (the focus ring,
/// the outline of checkboxes, radios, sliders and inputs, the accent fill of checked and filled parts, scroll thumbs) at 3:1.
/// Use <see cref="Check(KitTokens)"/> on tokens in a test, or <see cref="Check(UiTree)"/> on a running UI to include a game's own token overrides.
/// </summary>
public static class ThemeLint
{
    private static readonly string[] Surfaces = ["--kit-bg", "--kit-panel", "--kit-raised", "--kit-field"];

    // name prefix, foreground token, background tokens, minimum, kind
    private static readonly (string Name, string Fg, string[] Bgs, double Min, string Kind)[] Pairs =
    [
        ("text", "--kit-text", ["--kit-bg", "--kit-panel", "--kit-raised", "--kit-field", "--kit-hover", "--kit-active", "--kit-select"], KitContrast.TextAa, "text"),
        ("dim text", "--kit-text-dim", ["--kit-bg", "--kit-panel", "--kit-raised", "--kit-field"], KitContrast.TextAa, "text"),
        ("primary button text", "--kit-accent-text", ["--kit-accent", "--kit-accent-hover", "--kit-accent-active"], KitContrast.TextAa, "text"),
        ("danger button text", "--kit-danger-text", ["--kit-danger", "--kit-danger-hover", "--kit-danger-active"], KitContrast.TextAa, "text"),
        ("selected text", "--kit-select-text", ["--kit-select"], KitContrast.TextAa, "text"),
        ("pressed button text", "--kit-text", ["--kit-pressed-fill", "--kit-pressed-hover", "--kit-pressed-active"], KitContrast.TextAa, "text"),
        ("disabled text", "--kit-disabled-text", ["--kit-disabled-fill", "--kit-disabled-pressed-fill", "--kit-bg", "--kit-panel", "--kit-field"], KitContrast.IndicatorAa, "disabled-text"),
        ("pressed button outline", "--kit-pressed-border", ["--kit-pressed-fill", "--kit-pressed-hover", "--kit-pressed-active", "--kit-bg", "--kit-panel", "--kit-field"], KitContrast.IndicatorAa, "indicator"),
        ("focus ring", "--kit-focus", Surfaces, KitContrast.IndicatorAa, "indicator"),
        ("control outline", "--kit-control-border", ["--kit-bg", "--kit-panel", "--kit-field"], KitContrast.IndicatorAa, "indicator"),
        ("accent fill", "--kit-accent", ["--kit-bg", "--kit-panel", "--kit-field"], KitContrast.IndicatorAa, "indicator"),
        ("scroll thumb", "--kit-scrollbar", ["--kit-bg", "--kit-panel", "--kit-field"], KitContrast.IndicatorAa, "indicator"),
    ];

    /// <summary>Checks the colors the tokens produce through <see cref="KitTheme.Create"/>, derived state colors included.</summary>
    public static ThemeLintReport Check(KitTokens tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        var colors = KitTheme.Palette(tokens);
        return Check(name =>
        {
            foreach (var (n, c) in colors)
            {
                if (string.Equals(n, name, StringComparison.Ordinal))
                {
                    return c;
                }
            }

            return null;
        });
    }

    /// <summary>Checks the tokens in effect on a UI tree's root: the kit's theme as the cascade resolved it, with any token a game sheet overrides. Resolve styles with one frame first.</summary>
    public static ThemeLintReport Check(UiTree tree)
    {
        ArgumentNullException.ThrowIfNull(tree);
        return Check(name => tree.TryGetColorToken(name, out var c) ? c : null);
    }

    /// <summary>Checks colors looked up by token name; null means the token is not defined.</summary>
    /// <param name="token">Returns the color of a token such as <c>--kit-text</c>, or null.</param>
    public static ThemeLintReport Check(Func<string, Color?> token)
    {
        ArgumentNullException.ThrowIfNull(token);
        var all = new List<ContrastCheck>();
        foreach (var (label, fg, bgs, min, kind) in Pairs)
        {
            var f = token(fg);
            foreach (var bg in bgs)
            {
                var b = token(bg);
                var name = label + " on " + bg["--kit-".Length..];
                if (f is not { } fc || b is not { } bc)
                {
                    all.Add(new ContrastCheck(name, fg, bg, f is null ? "missing" : KitContrast.ToHex(f.Value), b is null ? "missing" : KitContrast.ToHex(b.Value), 0, min, false, kind));
                    continue;
                }

                var ratio = Math.Round(KitContrast.Ratio(fc, KitContrast.Over(bc, Color.Black)), 2);
                // Compare the unrounded ratio so 4.496 never passes as 4.5.
                var passed = KitContrast.Ratio(fc, KitContrast.Over(bc, Color.Black)) >= min;
                all.Add(new ContrastCheck(name, fg, bg, KitContrast.ToHex(fc), KitContrast.ToHex(bc), ratio, min, passed, kind));
            }
        }

        var failures = new List<ContrastCheck>();
        foreach (var c in all)
        {
            if (!c.Passed)
            {
                failures.Add(c);
            }
        }

        // Worst first; the sort is stable on the fixed order of the checks, so reports are reproducible.
        failures = [.. failures.Select((c, i) => (c, i)).OrderBy(x => x.c.Ratio / x.c.Required).ThenBy(x => x.i).Select(x => x.c)];
        return new ThemeLintReport(failures.Count == 0, all.Count, failures, all);
    }
}
