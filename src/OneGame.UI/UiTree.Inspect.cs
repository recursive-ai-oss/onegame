using System.Buffers;
using System.Text;
using System.Text.Json;
using P = OneGame.UI.StyleProp;

namespace OneGame.UI;

public sealed partial class UiTree
{
    /// <summary>Finds the first element (in document order) with this id, or null.</summary>
    public Element? Find(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        _walk.Clear();
        _walk.Push(Root);
        while (_walk.Count > 0)
        {
            var e = _walk.Pop();
            if (string.Equals(e.Id, id, StringComparison.Ordinal))
            {
                _walk.Clear();
                return e;
            }

            for (var i = e.ChildCount - 1; i >= 0; i--)
            {
                _walk.Push(e.ChildAt(i));
            }
        }

        return null;
    }

    /// <summary>Builds the selector that matches exactly the elements with this id, escaping characters a selector cannot hold bare. Ids containing a comma cannot be selected this way; use <see cref="Find"/>.</summary>
    public static string IdSelector(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return "#" + Selector.EscapeId(id);
    }

    /// <summary>Finds every element matching a selector (the same syntax as style rules), in document order.</summary>
    /// <exception cref="UiStyleException">The selector does not parse.</exception>
    public List<Element> Query(string selector)
    {
        var selectors = Selector.ParseList(selector);
        var result = new List<Element>();
        _walk.Clear();
        _walk.Push(Root);
        while (_walk.Count > 0)
        {
            var e = _walk.Pop();
            foreach (var s in selectors)
            {
                if (s.Matches(e))
                {
                    result.Add(e);
                    break;
                }
            }

            for (var i = e.ChildCount - 1; i >= 0; i--)
            {
                _walk.Push(e.ChildAt(i));
            }
        }

        return result;
    }

    /// <summary>
    /// The tree as JSON for the harness: for each element its kind, id, classes, states, text, local and screen boxes, scroll extents and computed style.
    /// Styles that equal their initial value are left out unless <paramref name="allStyles"/> is set.
    /// </summary>
    /// <param name="from">The element to start at; the root by default.</param>
    /// <param name="allStyles">Include every computed property.</param>
    /// <param name="children">Include descendants.</param>
    public string DumpJson(Element? from = null, bool allStyles = false, bool children = true)
    {
        Update();
        var buffer = new ArrayBufferWriter<byte>(4096);
        using (var w = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            if (from is null)
            {
                w.WriteStartObject();
                w.WriteNumber("width", _width);
                w.WriteNumber("height", _height);
                w.WriteNumber("scale", _scale);
                if (_focused is not null)
                {
                    w.WriteString("focused", _focused.Describe());
                }

                w.WritePropertyName("root");
                WriteElement(w, Root, allStyles, children);
                w.WriteEndObject();
            }
            else
            {
                WriteElement(w, from, allStyles, children);
            }
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static void WriteRect(Utf8JsonWriter w, string name, Rect r)
    {
        w.WriteStartObject(name);
        w.WriteNumber("x", Math.Round(r.X, 3));
        w.WriteNumber("y", Math.Round(r.Y, 3));
        w.WriteNumber("w", Math.Round(r.Width, 3));
        w.WriteNumber("h", Math.Round(r.Height, 3));
        w.WriteEndObject();
    }

    // The visible text of an element's descendants, in tree order, joined by single spaces; null when there is none. Walks with an
    // explicit stack so a deep tree cannot overflow the call stack.
    private static string? ComposedText(Element root)
    {
        StringBuilder? sb = null;
        var stack = new Stack<Element>();
        stack.Push(root);
        while (stack.Count > 0 && (sb?.Length ?? 0) < 4096)
        {
            var el = stack.Pop();
            if (el != root && el.Text is { Length: > 0 } t && el.IsVisible)
            {
                sb = (sb ?? new StringBuilder()).Append(sb is { Length: > 0 } ? " " : "").Append(t);
            }

            for (var i = el.ChildCount - 1; i >= 0; i--)
            {
                stack.Push(el.ChildAt(i));
            }
        }

        return sb?.ToString();
    }

    private static void WriteElement(Utf8JsonWriter w, Element e, bool allStyles, bool children)
    {
        w.WriteStartObject();
        w.WriteString("kind", e.Kind);
        if (e.Id is not null)
        {
            w.WriteString("id", e.Id);
        }

        if (e.Role is not null)
        {
            w.WriteString("role", e.Role);
        }

        if (e.Value is not null)
        {
            w.WriteString("value", e.Value);
        }

        if (e.Classes.Count > 0)
        {
            w.WriteStartArray("classes");
            foreach (var c in e.Classes)
            {
                w.WriteStringValue(c);
            }

            w.WriteEndArray();
        }

        var states = new List<string>(4);
        if (e.HoverChain)
        {
            states.Add("hover");
        }

        if (e.ActiveChain)
        {
            states.Add("active");
        }

        if (e.FocusFlag)
        {
            states.Add("focus");
        }

        if (!e.IsEffectivelyEnabled)
        {
            states.Add("disabled");
        }

        if (!e.IsVisible)
        {
            states.Add("hidden");
        }

        if (e.IsFocusable)
        {
            states.Add("focusable");
        }

        if (states.Count > 0)
        {
            w.WriteStartArray("states");
            foreach (var st in states)
            {
                w.WriteStringValue(st);
            }

            w.WriteEndArray();
        }

        // A control built from parts (a button, a toggle, a tab: by role) keeps its label in a child; report it so "what does this say" has one answer.
        var text = e.Text ?? (e.Role is "button" or "checkbox" or "radio" or "tab" or "option" or "columnheader" or "combobox" or "menuitem" ? ComposedText(e) : null);
        if (text is not null)
        {
            w.WriteString("text", text);
        }

        WriteRect(w, "bounds", e.Bounds);
        WriteRect(w, "screen", e.AbsoluteBounds);
        if (e.Scrolls(true) || e.Scrolls(false))
        {
            w.WriteStartObject("scroll");
            w.WriteNumber("x", Math.Round(e.ScrollX, 3));
            w.WriteNumber("y", Math.Round(e.ScrollY, 3));
            w.WriteNumber("contentW", Math.Round(e.SW, 3));
            w.WriteNumber("contentH", Math.Round(e.SH, 3));
            w.WriteEndObject();
        }

        w.WriteStartObject("style");
        for (var i = 0; i < PropertyTable.All.Length; i++)
        {
            var info = PropertyTable.All[i];
            if (allStyles || !e.Computed.V[i].Equals(info.Initial))
            {
                w.WriteString(info.Name, PropertyTable.Format((StyleProp)i, e.Computed.V[i]));
            }
        }

        w.WriteEndObject();
        if (children && e.ChildCount > 0)
        {
            w.WriteStartArray("children");
            for (var i = 0; i < e.ChildCount; i++)
            {
                WriteElement(w, e.ChildAt(i), allStyles, true);
            }

            w.WriteEndArray();
        }

        w.WriteEndObject();
    }
}
