using OneFont;

namespace OneGame.UI.Kit;

// Where the grapheme clusters of one line of text sit on screen, and how a caret moves through them.
//
// The text is split into extended grapheme clusters (Unicode UAX #29), each cluster gets a bidirectional level (UAX #9), and the clusters are
// put in visual order with the reordering rule L2. A "boundary" is a place between clusters: logical boundary g lies before cluster g
// (0..n), visual boundary v lies before the cluster in visual slot v. Arrow keys move through visual boundaries, so the caret always
// goes the way the key points even in mixed left-to-right and right-to-left text, and never lands inside a cluster.
internal sealed class CaretMap
{
    private CaretMap(int[] starts, byte[] levels, int[] visualToGrapheme, int[] graphemeToVisual, float[] slotX, bool paragraphRtl)
    {
        Starts = starts;
        Levels = levels;
        VisualToGrapheme = visualToGrapheme;
        GraphemeToVisual = graphemeToVisual;
        SlotX = slotX;
        ParagraphRtl = paragraphRtl;
    }

    /// <summary>UTF-16 index where each cluster starts, followed by the text length.</summary>
    public int[] Starts { get; }

    public byte[] Levels { get; }

    public int[] VisualToGrapheme { get; }

    public int[] GraphemeToVisual { get; }

    /// <summary>X of each visual boundary, 0..n.</summary>
    public float[] SlotX { get; }

    public bool ParagraphRtl { get; }

    public int Count => Levels.Length;

    public float Width => SlotX[Count];

    public bool IsRtl(int grapheme) => (Levels[grapheme] & 1) == 1;

    public static CaretMap Build(string text, Func<string, float> widthOf)
    {
        var starts = TextSegmentation.GetGraphemeBoundaries(text);
        var n = starts.Length - 1;
        var levels = new byte[n];
        var paragraphRtl = false;
        if (n > 0)
        {
            var unitLevels = TextShaping.GetBidiLevels(text, TextDirection.Auto, out var para);
            paragraphRtl = para == 1;
            for (var g = 0; g < n; g++)
            {
                levels[g] = unitLevels[starts[g]];
            }
        }

        // Rule L2: from the highest level down to the lowest odd level, reverse every run of clusters at that level or higher.
        var order = new int[n];
        byte highest = 0;
        byte lowestOdd = byte.MaxValue;
        for (var g = 0; g < n; g++)
        {
            order[g] = g;
            highest = Math.Max(highest, levels[g]);
            if ((levels[g] & 1) == 1)
            {
                lowestOdd = Math.Min(lowestOdd, levels[g]);
            }
        }

        for (var level = highest; level >= lowestOdd && level >= 1; level--)
        {
            var p = 0;
            while (p < n)
            {
                if (levels[order[p]] < level)
                {
                    p++;
                    continue;
                }

                var q = p;
                while (q + 1 < n && levels[order[q + 1]] >= level)
                {
                    q++;
                }

                Array.Reverse(order, p, q - p + 1);
                p = q + 1;
            }
        }

        var toVisual = new int[n];
        var slotX = new float[n + 1];
        for (var v = 0; v < n; v++)
        {
            var g = order[v];
            toVisual[g] = v;
            slotX[v + 1] = slotX[v] + widthOf(text.Substring(starts[g], starts[g + 1] - starts[g]));
        }

        return new CaretMap(starts, levels, order, toVisual, slotX, paragraphRtl);
    }

    /// <summary>The visual boundary where a caret at logical boundary <paramref name="g"/> is drawn: after the cluster before it, or at the start of the line.</summary>
    public int VisualOf(int g)
    {
        if (Count == 0)
        {
            return 0;
        }

        if (g <= 0)
        {
            return ParagraphRtl ? Count : 0;
        }

        var prev = g - 1;
        var slot = GraphemeToVisual[prev];
        return IsRtl(prev) ? slot : slot + 1;
    }

    /// <summary>The logical boundary a caret at visual boundary <paramref name="v"/> stands for, read from the cluster to its left (or to its right at the left end).</summary>
    public int LogicalOf(int v)
    {
        if (Count == 0)
        {
            return 0;
        }

        if (v <= 0)
        {
            var first = VisualToGrapheme[0];
            return IsRtl(first) ? first + 1 : first;
        }

        var left = VisualToGrapheme[Math.Min(v, Count) - 1];
        return IsRtl(left) ? left : left + 1;
    }

    /// <summary>The visual boundary closest to <paramref name="x"/>.</summary>
    public int NearestBoundary(float x)
    {
        var best = 0;
        var bestDistance = float.PositiveInfinity;
        for (var v = 0; v <= Count; v++)
        {
            var d = MathF.Abs(SlotX[v] - x);
            if (d < bestDistance)
            {
                bestDistance = d;
                best = v;
            }
        }

        return best;
    }

    /// <summary>Appends the x ranges that cover clusters <paramref name="from"/> up to (not including) <paramref name="to"/>, merged where they touch on screen.</summary>
    public void Runs(int from, int to, List<(float X0, float X1)> into)
    {
        var v = 0;
        while (v < Count)
        {
            var g = VisualToGrapheme[v];
            if (g < from || g >= to)
            {
                v++;
                continue;
            }

            var end = v;
            while (end + 1 < Count && VisualToGrapheme[end + 1] >= from && VisualToGrapheme[end + 1] < to)
            {
                end++;
            }

            into.Add((SlotX[v], SlotX[end + 1]));
            v = end + 1;
        }
    }
}

// The editing model of a single-line text field: the text, a caret, a selection, and the operations a keyboard performs on them.
// Positions are cluster indexes (0..n) unless the name says UTF-16.
internal sealed class TextEditor
{
    private readonly Func<string, float> _widthOf;
    private int[] _starts = [0];
    private string _display = "";

    public TextEditor(Func<string, float> widthOf)
    {
        _widthOf = widthOf;
        Map = CaretMap.Build("", widthOf);
    }

    public string Text { get; private set; } = "";

    public string DisplayText => _display;

    public bool Password { get; set; }

    public char MaskChar { get; set; } = '\u2022';

    public int MaxLength { get; set; } = int.MaxValue;

    public Func<string, bool>? Validate { get; set; }

    public CaretMap Map { get; private set; }

    public int Caret { get; private set; }

    public int Anchor { get; private set; }

    public int CaretVisual { get; private set; }

    public int Count => _starts.Length - 1;

    public int[] RealStarts => _starts;

    public bool HasSelection => Caret != Anchor;

    public int SelectionStart => Math.Min(Caret, Anchor);

    public int SelectionEnd => Math.Max(Caret, Anchor);

    public int CaretUtf16 => _starts[Caret];

    public int SelectionStartUtf16 => _starts[SelectionStart];

    public int SelectionEndUtf16 => _starts[SelectionEnd];

    public string SelectedText => Text[SelectionStartUtf16..SelectionEndUtf16];

    // Rebuilds the clusters and the geometry; call it after the text, the mask or the font changed.
    public void Rebuild()
    {
        _starts = TextSegmentation.GetGraphemeBoundaries(Text);
        _display = Password ? new string(MaskChar, Count) : Text;
        Map = CaretMap.Build(_display, _widthOf);
        Caret = Math.Min(Caret, Count);
        Anchor = Math.Min(Anchor, Count);
        CaretVisual = Map.VisualOf(Caret);
    }

    // Replaces the whole text (the game changed it); the caret goes to the end unless it still fits where it was.
    public void SetText(string text, bool caretToEnd)
    {
        Text = text;
        var keep = Caret;
        Rebuild();
        Place(caretToEnd ? Count : Math.Min(keep, Count), extend: false);
    }

    public void Place(int cluster, bool extend)
    {
        Caret = Math.Clamp(cluster, 0, Count);
        CaretVisual = Map.VisualOf(Caret);
        if (!extend)
        {
            Anchor = Caret;
        }
    }

    public void PlaceAtX(float x, bool extend)
    {
        var v = Map.NearestBoundary(x);
        CaretVisual = v;
        Caret = Map.LogicalOf(v);
        if (!extend)
        {
            Anchor = Caret;
        }
    }

    public void SelectAll()
    {
        Anchor = 0;
        Caret = Count;
        CaretVisual = Map.VisualOf(Caret);
    }

    // Arrow keys: one visual boundary left (-1) or right (+1).
    public void MoveVisual(int direction, bool extend)
    {
        if (!extend && HasSelection)
        {
            Place(direction < 0 ? SelectionStart : SelectionEnd, extend: false);
            return;
        }

        var v = Math.Clamp(CaretVisual + direction, 0, Count);
        CaretVisual = v;
        Caret = Map.LogicalOf(v);
        if (!extend)
        {
            Anchor = Caret;
        }
    }

    public bool Insert(string input)
    {
        var s = Sanitize(input);
        return (s.Length > 0 || HasSelection) && Replace(SelectionStart, SelectionEnd, s);
    }

    public bool Backspace()
    {
        if (HasSelection)
        {
            return Replace(SelectionStart, SelectionEnd, "");
        }

        return Caret > 0 && Replace(Caret - 1, Caret, "");
    }

    public bool Delete()
    {
        if (HasSelection)
        {
            return Replace(SelectionStart, SelectionEnd, "");
        }

        return Caret < Count && Replace(Caret, Caret + 1, "");
    }

    // Single-line text: line breaks and tabs become spaces and other control characters are dropped.
    internal static string Sanitize(string s)
    {
        if (s.Length == 0)
        {
            return s;
        }

        var sb = new System.Text.StringBuilder(s.Length);
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (c == '\r' && i + 1 < s.Length && s[i + 1] == '\n')
            {
                continue;
            }

            if (c is '\r' or '\n' or '\t')
            {
                sb.Append(' ');
            }
            else if (!char.IsControl(c))
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    private bool Replace(int from, int to, string insert)
    {
        var kept = Count - (to - from);
        var room = MaxLength - kept;
        if (insert.Length > 0)
        {
            if (room <= 0)
            {
                return false;
            }

            var bounds = TextSegmentation.GetGraphemeBoundaries(insert);
            if (bounds.Length - 1 > room)
            {
                insert = insert[..bounds[room]];
            }
        }

        var prefix = Text[.._starts[from]];
        var candidate = string.Concat(prefix, insert, Text.AsSpan(_starts[to]));
        if (candidate == Text || (Validate is not null && !Validate(candidate)))
        {
            return false;
        }

        Text = candidate;
        _starts = TextSegmentation.GetGraphemeBoundaries(Text);
        // The inserted text may merge with its neighbors into one cluster (a combining mark), so the caret goes to the next boundary at or after its end.
        var at = Array.BinarySearch(_starts, prefix.Length + insert.Length);
        var cluster = at >= 0 ? at : ~at;
        Rebuild();
        Place(cluster, extend: false);
        return true;
    }
}
