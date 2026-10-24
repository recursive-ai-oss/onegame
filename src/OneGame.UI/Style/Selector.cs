namespace OneGame.UI;

[Flags]
internal enum Pseudo
{
    None = 0,
    Hover = 1,
    Focus = 2,
    Active = 4,
    Disabled = 8,
    Root = 16,
    FirstChild = 32,
    LastChild = 64,
    NthChild = 128,
}

internal sealed class Compound
{
    public string? Type;
    public string? Id;
    public string[] Classes = [];
    public Pseudo Pseudo;
    public int NthA;
    public int NthB;

    public bool IsStateful => (Pseudo & (Pseudo.Hover | Pseudo.Focus | Pseudo.Active | Pseudo.Disabled)) != 0;

    public bool Structural => (Pseudo & (Pseudo.FirstChild | Pseudo.LastChild | Pseudo.NthChild)) != 0;

    public bool Matches(Element el)
    {
        if (Type is not null && !string.Equals(Type, el.Kind, StringComparison.Ordinal))
        {
            return false;
        }

        if (Id is not null && !string.Equals(Id, el.Id, StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var c in Classes)
        {
            if (!el.HasClass(c))
            {
                return false;
            }
        }

        var p = Pseudo;
        if (p == Pseudo.None)
        {
            return true;
        }

        // Hover and active also apply to ancestors of the pointer target, as in CSS.
        if ((p & Pseudo.Hover) != 0 && !el.HoverChain)
        {
            return false;
        }

        if ((p & Pseudo.Active) != 0 && !el.ActiveChain)
        {
            return false;
        }

        if ((p & Pseudo.Focus) != 0 && !el.IsFocused)
        {
            return false;
        }

        if ((p & Pseudo.Disabled) != 0 && el.IsEnabled)
        {
            return false;
        }

        if ((p & Pseudo.Root) != 0 && el.Parent is not null)
        {
            return false;
        }

        if ((p & Pseudo.FirstChild) != 0 && el.IndexInParent != 0)
        {
            return false;
        }

        if ((p & Pseudo.LastChild) != 0 && (el.Parent is null || el.IndexInParent != el.Parent.ChildCount - 1))
        {
            return false;
        }

        if ((p & Pseudo.NthChild) != 0)
        {
            // An+B with 1-based position n; A = 0 means "exactly B".
            var pos = el.IndexInParent + 1;
            if (NthA == 0 ? pos != NthB : (pos - NthB) % NthA != 0 || (pos - NthB) / NthA < 0)
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>A parsed selector: compound selectors joined by descendant (space) or child (<c>&gt;</c>) combinators.</summary>
internal sealed class Selector
{
    private readonly Compound[] _parts;
    private readonly bool[] _child;

    private Selector(Compound[] parts, bool[] child)
    {
        _parts = parts;
        _child = child;
        var a = 0;
        var b = 0;
        var c = 0;
        foreach (var part in parts)
        {
            a += part.Id is null ? 0 : 1;
            b += part.Classes.Length + BitCount(part.Pseudo & ~Pseudo.Root);
            c += part.Type is null ? 0 : 1;
            HasStructural |= part.Structural;
            if (part.Pseudo == Pseudo.Root)
            {
                b++;
            }
        }

        // CSS Selectors 4 §17: specificity is compared as (ids, classes+pseudo-classes, types).
        Specificity = (Math.Min(a, 255) << 16) | (Math.Min(b, 255) << 8) | Math.Min(c, 255);
        HasCombinator = parts.Length > 1;
    }

    public int Specificity { get; }

    public bool HasCombinator { get; }

    public bool HasStructural { get; }

    public bool Matches(Element el) => Match(el, _parts.Length - 1);

    private bool Match(Element el, int i)
    {
        if (!_parts[i].Matches(el))
        {
            return false;
        }

        if (i == 0)
        {
            return true;
        }

        if (_child[i])
        {
            return el.Parent is { } p && Match(p, i - 1);
        }

        for (var a = el.Parent; a is not null; a = a.Parent)
        {
            if (Match(a, i - 1))
            {
                return true;
            }
        }

        return false;
    }

    private static int BitCount(Pseudo p) => System.Numerics.BitOperations.PopCount((uint)p);

    /// <summary>Parses a comma-separated selector list.</summary>
    public static List<Selector> ParseList(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var result = new List<Selector>();
        foreach (var part in text.Split(','))
        {
            result.Add(Parse(part.Trim(), text));
        }

        return result;
    }

    private static Selector Parse(string s, string whole)
    {
        if (s.Length == 0)
        {
            throw new UiStyleException($"Empty selector in '{whole}'.");
        }

        var parts = new List<Compound>();
        var child = new List<bool> { false };
        var i = 0;
        while (i < s.Length)
        {
            var c = new Compound();
            var start = i;
            var classes = new List<string>();
            while (i < s.Length && !char.IsWhiteSpace(s[i]) && s[i] != '>')
            {
                var ch = s[i];
                if (ch == '*')
                {
                    i++;
                }
                else if (ch == '#' || ch == '.')
                {
                    i++;
                    var name = Ident(s, ref i, whole);
                    if (ch == '#')
                    {
                        c.Id = name;
                    }
                    else
                    {
                        classes.Add(name);
                    }
                }
                else if (ch == ':')
                {
                    i++;
                    ParsePseudo(s, ref i, c, whole);
                }
                else if (i == start)
                {
                    c.Type = Ident(s, ref i, whole);
                }
                else
                {
                    throw new UiStyleException($"Unexpected '{ch}' in selector '{whole}'.");
                }
            }

            if (i == start)
            {
                throw new UiStyleException($"Missing selector before '>' in '{whole}'.");
            }

            c.Classes = [.. classes];
            parts.Add(c);

            while (i < s.Length && char.IsWhiteSpace(s[i]))
            {
                i++;
            }

            var isChild = false;
            if (i < s.Length && s[i] == '>')
            {
                isChild = true;
                i++;
                while (i < s.Length && char.IsWhiteSpace(s[i]))
                {
                    i++;
                }

                if (i >= s.Length)
                {
                    throw new UiStyleException($"Selector '{whole}' ends with '>'.");
                }
            }

            if (i < s.Length)
            {
                child.Add(isChild);
            }
        }

        return new Selector([.. parts], [.. child]);
    }

    private static string Ident(string s, ref int i, string whole)
    {
        var start = i;
        while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] is '-' or '_'))
        {
            i++;
        }

        if (i == start)
        {
            throw new UiStyleException($"Expected a name in selector '{whole}' at position {start}.");
        }

        return s[start..i];
    }

    private static void ParsePseudo(string s, ref int i, Compound c, string whole)
    {
        var name = Ident(s, ref i, whole);
        switch (name)
        {
            case "hover": c.Pseudo |= Pseudo.Hover; break;
            case "focus": c.Pseudo |= Pseudo.Focus; break;
            case "active": c.Pseudo |= Pseudo.Active; break;
            case "disabled": c.Pseudo |= Pseudo.Disabled; break;
            case "root": c.Pseudo |= Pseudo.Root; break;
            case "first-child": c.Pseudo |= Pseudo.FirstChild; break;
            case "last-child": c.Pseudo |= Pseudo.LastChild; break;
            case "nth-child":
                if (i >= s.Length || s[i] != '(')
                {
                    throw new UiStyleException($":nth-child needs an argument in '{whole}'.");
                }

                var close = s.IndexOf(')', i);
                if (close < 0)
                {
                    throw new UiStyleException($"Unclosed ':nth-child(' in '{whole}'.");
                }

                var arg = s[(i + 1)..close].Trim();
                i = close + 1;
                c.Pseudo |= Pseudo.NthChild;
                if (arg == "odd")
                {
                    c.NthA = 2;
                    c.NthB = 1;
                }
                else if (arg == "even")
                {
                    c.NthA = 2;
                    c.NthB = 0;
                }
                else if (int.TryParse(arg, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var n) && n > 0)
                {
                    c.NthA = 0;
                    c.NthB = n;
                }
                else
                {
                    throw new UiStyleException($":nth-child accepts odd, even or a positive number, got '{arg}' in '{whole}'.");
                }

                break;
            default:
                throw new UiStyleException($"Unsupported pseudo-class ':{name}' in '{whole}'. Supported: hover, focus, active, disabled, root, first-child, last-child, nth-child.");
        }
    }
}
