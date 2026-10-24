using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace OneGame.UI.Tests;

internal static class Paths
{
    public static string TestData([CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "testdata", "ui-layout"));
}

/// <summary>Reads the small HTML subset of the layout cases (nested divs with id and inline style) into elements.</summary>
internal static partial class MiniHtml
{
    [GeneratedRegex("""<div([^>]*)>|</div>""")]
    private static partial Regex Tag();

    [GeneratedRegex("""id=(\w+)""")]
    private static partial Regex IdAttr();

    [GeneratedRegex(""""style="([^"]*)"""")]
    private static partial Regex StyleAttr();

    public static List<Element> Parse(string html)
    {
        var roots = new List<Element>();
        var stack = new Stack<Element>();
        foreach (Match m in Tag().Matches(html))
        {
            if (m.Value == "</div>")
            {
                stack.Pop();
                continue;
            }

            var el = new Element("Box");
            var attrs = m.Groups[1].Value;
            if (IdAttr().Match(attrs) is { Success: true } id)
            {
                el.Id = id.Groups[1].Value;
            }

            if (StyleAttr().Match(attrs) is { Success: true } st)
            {
                ApplyStyle(el, st.Groups[1].Value);
            }

            if (stack.Count > 0)
            {
                stack.Peek().Add(el);
            }
            else
            {
                roots.Add(el);
            }

            stack.Push(el);
        }

        return roots;
    }

    private static void ApplyStyle(Element el, string css)
    {
        foreach (var decl in css.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var i = decl.IndexOf(':', StringComparison.Ordinal);
            Declare(el.Inline, decl[..i].Trim(), decl[(i + 1)..].Trim());
        }
    }

    private static void Declare(StyleBuilder b, string name, string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        switch (name)
        {
            case "margin":
            case "padding":
                var sides = Four(parts);
                string[] order = ["top", "right", "bottom", "left"];
                for (var k = 0; k < 4; k++)
                {
                    b.Set($"{name}-{order[k]}", Len(sides[k]));
                }

                return;
            case "border-width":
                var bs = Four(parts);
                string[] bo = ["top", "right", "bottom", "left"];
                for (var k = 0; k < 4; k++)
                {
                    b.Set($"border-{bo[k]}-width", Len(bs[k]));
                }

                return;
            case "gap":
                b.Set("row-gap", Len(parts[0]));
                b.Set("column-gap", Len(parts.Length > 1 ? parts[1] : parts[0]));
                return;
            case "flex":
                FlexShorthand(b, parts);
                return;
            default:
                break;
        }

        Assert.True(PropertyTable.TryGet(name, out var prop), $"unknown property {name}");
        var info = PropertyTable.Info(prop);
        switch (info.Kind)
        {
            case ValueKind.Length:
                b.Set(name, Len(value));
                break;
            case ValueKind.Number:
                b.Set(name, StyleValue.Number(float.Parse(value, CultureInfo.InvariantCulture)));
                break;
            case ValueKind.Enum:
                var idx = Array.IndexOf(info.EnumNames!, value);
                Assert.True(idx >= 0, $"{value} is not a value of {name}");
                b.Declarations.Add(new Declaration(prop, StyleValue.OfEnum(idx)));
                break;
            default:
                throw new InvalidOperationException(name);
        }
    }

    private static void FlexShorthand(StyleBuilder b, string[] p)
    {
        float grow = 1;
        float shrink = 1;
        Length basis = Length.Auto;
        if (p.Length == 1 && p[0] == "none")
        {
            (grow, shrink) = (0, 0);
        }
        else if (p.Length == 1 && p[0] == "auto")
        {
        }
        else if (p.Length == 1)
        {
            grow = float.Parse(p[0], CultureInfo.InvariantCulture);
            basis = Length.Percent(0);
        }
        else
        {
            grow = float.Parse(p[0], CultureInfo.InvariantCulture);
            shrink = float.Parse(p[1], CultureInfo.InvariantCulture);
            basis = p.Length > 2 ? Len(p[2]) : Length.Percent(0);
            if (p.Length == 2 && !float.TryParse(p[1], CultureInfo.InvariantCulture, out _))
            {
                shrink = 1;
                basis = Len(p[1]);
            }
        }

        b.Flex(grow, shrink, basis);
    }

    private static string[] Four(string[] p) => p.Length switch
    {
        1 => [p[0], p[0], p[0], p[0]],
        2 => [p[0], p[1], p[0], p[1]],
        3 => [p[0], p[1], p[2], p[1]],
        _ => p,
    };

    public static Length Len(string v)
    {
        if (v == "auto")
        {
            return Length.Auto;
        }

        if (v.EndsWith("px", StringComparison.Ordinal))
        {
            return Length.Px(float.Parse(v[..^2], CultureInfo.InvariantCulture));
        }

        if (v.EndsWith('%'))
        {
            return Length.Percent(float.Parse(v[..^1], CultureInfo.InvariantCulture));
        }

        return Length.Px(float.Parse(v, CultureInfo.InvariantCulture));
    }
}
