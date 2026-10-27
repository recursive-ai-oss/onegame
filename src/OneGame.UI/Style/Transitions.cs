using System.Globalization;
using OneGame.Tweens;

namespace OneGame.UI;

// One entry of a parsed `transition` value: which property, how long, after what delay, with which easing.
internal readonly record struct TransitionSpec(int Prop, float Duration, float Delay, Easing Ease);

// Parses the `transition` property (CSS Transitions Level 1 §2): a comma-separated list of "<property> <duration> <easing> <delay>" in any order
// after the property, where the first time is the duration and the second the delay. Items it cannot read are skipped, as CSS drops an invalid list item's property.
internal static class TransitionParser
{
    private static readonly Dictionary<string, StyleProp[]> Shorthands = new(StringComparer.Ordinal)
    {
        ["margin"] = [StyleProp.MarginTop, StyleProp.MarginRight, StyleProp.MarginBottom, StyleProp.MarginLeft],
        ["padding"] = [StyleProp.PaddingTop, StyleProp.PaddingRight, StyleProp.PaddingBottom, StyleProp.PaddingLeft],
        ["border-width"] = [StyleProp.BorderTopWidth, StyleProp.BorderRightWidth, StyleProp.BorderBottomWidth, StyleProp.BorderLeftWidth],
        ["border-radius"] = [StyleProp.RadiusTopLeft, StyleProp.RadiusTopRight, StyleProp.RadiusBottomRight, StyleProp.RadiusBottomLeft],
        ["gap"] = [StyleProp.RowGap, StyleProp.ColumnGap],
        ["translate"] = [StyleProp.TranslateX, StyleProp.TranslateY],
    };

    /// <summary>The entries of a <c>transition</c> value, or null for none, empty or unreadable text.</summary>
    public static TransitionSpec[]? Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Trim().Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var result = new List<TransitionSpec>();
        var span = text.AsSpan();
        var start = 0;
        var depth = 0;
        for (var i = 0; i <= span.Length; i++)
        {
            var end = i == span.Length;
            if (!end)
            {
                if (span[i] == '(')
                {
                    depth++;
                }
                else if (span[i] == ')')
                {
                    depth = Math.Max(0, depth - 1);
                }
            }

            if (end || (span[i] == ',' && depth == 0))
            {
                ParseItem(span[start..i], result);
                start = i + 1;
            }
        }

        return result.Count == 0 ? null : [.. result];
    }

    private static void ParseItem(ReadOnlySpan<char> item, List<TransitionSpec> into)
    {
        var props = new List<int>();
        var duration = -1f;
        var delay = 0f;
        var gotDelay = false;
        var ease = Easing.Ease;
        var depth = 0;
        var tokenStart = -1;
        for (var i = 0; i <= item.Length; i++)
        {
            var end = i == item.Length;
            var space = !end && char.IsWhiteSpace(item[i]) && depth == 0;
            if (!end)
            {
                if (item[i] == '(')
                {
                    depth++;
                }
                else if (item[i] == ')')
                {
                    depth = Math.Max(0, depth - 1);
                }
            }

            if (end || space)
            {
                if (tokenStart >= 0)
                {
                    if (!Token(item[tokenStart..i], props, ref duration, ref delay, ref gotDelay, ref ease))
                    {
                        return;
                    }

                    tokenStart = -1;
                }
            }
            else if (tokenStart < 0)
            {
                tokenStart = i;
            }
        }

        if (props.Count == 0 && duration < 0)
        {
            return;
        }

        if (props.Count == 0)
        {
            props.Add(-1);
        }

        foreach (var p in props)
        {
            into.Add(new TransitionSpec(p, Math.Max(0, duration), delay, ease));
        }
    }

    private static bool Token(ReadOnlySpan<char> token, List<int> props, ref float duration, ref float delay, ref bool gotDelay, ref Easing ease)
    {
        if (TryTime(token, out var seconds))
        {
            if (duration < 0)
            {
                if (seconds < 0)
                {
                    return false;
                }

                duration = seconds;
            }
            else if (!gotDelay)
            {
                delay = Math.Max(0, seconds);
                gotDelay = true;
            }
            else
            {
                return false;
            }

            return true;
        }

        if (token.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            props.Add(-1);
            return true;
        }

        var name = token.ToString().ToLowerInvariant();
        if (PropertyTable.TryGet(name, out var prop))
        {
            if (prop != StyleProp.Transition)
            {
                props.Add((int)prop);
            }

            return true;
        }

        if (Shorthands.TryGetValue(name, out var group))
        {
            foreach (var p in group)
            {
                props.Add((int)p);
            }

            return true;
        }

        if (Easing.TryParse(token, out var e))
        {
            ease = e;
            return true;
        }

        return false;
    }

    private static bool TryTime(ReadOnlySpan<char> token, out float seconds)
    {
        seconds = 0;
        var scale = 1f;
        ReadOnlySpan<char> number;
        if (token.EndsWith("ms", StringComparison.OrdinalIgnoreCase))
        {
            number = token[..^2];
            scale = 0.001f;
        }
        else if (token.EndsWith("s", StringComparison.OrdinalIgnoreCase))
        {
            number = token[..^1];
        }
        else
        {
            return false;
        }

        if (!float.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) || !float.IsFinite(v))
        {
            return false;
        }

        seconds = v * scale;
        return true;
    }
}

// A running or finished style transition of one property of one element. Its tween lives in the tree's TweenSet, so style transitions and game tweens
// share one clock and one listing.
internal sealed class UiTransition(Element element, StyleProp prop) : ITweenTarget
{
    public readonly StyleProp Prop = prop;
    public StyleValue From;
    public StyleValue To;
    public Tween Handle;

    public bool Active => Handle.IsActive;

    public string? Label => $"{element.Describe()} {PropertyTable.Info(Prop).Name}";

    public void Apply(float progress)
    {
        StyleValue v;
        switch (From.Kind)
        {
            case ValueKind.Number:
                var n = TweenSet.Mix(From.F, To.F, progress);
                n = Prop == StyleProp.Opacity ? Math.Clamp(n, 0, 1) : Prop is StyleProp.FlexGrow or StyleProp.FlexShrink ? Math.Max(0, n) : n;
                v = StyleValue.Number(n);
                break;
            case ValueKind.Length:
                var l = TweenSet.Mix(From.F, To.F, progress);
                v = new Length(AllowsNegative(Prop) ? l : Math.Max(0, l), From.Unit);
                break;
            default:
                v = Interp.Mix(From.AsColor, To.AsColor, progress);
                break;
        }

        element.SetAnimated(Prop, v);
    }

    // Sizes, spacing inside a box and radii cannot go below zero when an easing overshoots; offsets, margins and positions can.
    private static bool AllowsNegative(StyleProp p) => p is
        StyleProp.Left or StyleProp.Top or StyleProp.Right or StyleProp.Bottom
        or StyleProp.MarginTop or StyleProp.MarginRight or StyleProp.MarginBottom or StyleProp.MarginLeft
        or StyleProp.OutlineOffset or StyleProp.ShadowX or StyleProp.ShadowY or StyleProp.ShadowSpread
        or StyleProp.TranslateX or StyleProp.TranslateY
        or StyleProp.BackgroundPositionX or StyleProp.BackgroundPositionY or StyleProp.LetterSpacing;

    // Only numbers, colors and lengths in one unit have a value between two values; everything else switches at once.
    public static bool CanBlend(StyleProp p, in StyleValue a, in StyleValue b) =>
        p is not (StyleProp.Order or StyleProp.Transition)
        && a.Kind == b.Kind
        && a.Kind switch
        {
            ValueKind.Number or ValueKind.Color => true,
            ValueKind.Length => a.Unit == b.Unit && a.Unit != LengthUnit.Auto,
            _ => false,
        };
}
