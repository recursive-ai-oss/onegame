using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace OneGame.ApiDocs;

/// <summary>Where every documented type and member lives, and how to link to it from another page.</summary>
sealed class Links
{
    readonly Dictionary<string, (string Path, string? Anchor)> _targets = new(StringComparer.Ordinal);
    readonly Dictionary<string, TypeInfo> _typeByKey;

    // Sibling libraries are shown as plain names: they have their own documentation.
    static readonly string[] SiblingPrefixes = ["OneAudio", "OneFont", "OneImage", "OneYaml", "OneShader", "OneRender", "OneHtml"];

    public SortedSet<string> Unresolved { get; } = new(StringComparer.Ordinal);
    public int ToSibling;
    public int Rescued;

    public Links(Dictionary<string, TypeInfo> typeByKey)
    {
        _typeByKey = typeByKey;
        foreach (TypeInfo t in typeByKey.Values)
        {
            _targets[t.DocId] = (t.Path, null);
            foreach (MemberInfo m in t.Members) _targets[m.DocId] = (t.Path, m.Anchor);
        }
    }

    public static bool IsSibling(string key) => SiblingPrefixes.Any(p => key.StartsWith(p, StringComparison.Ordinal) && (key.Length == p.Length || key[p.Length] is '.' or '`'));

    public static bool IsOurs(string key) => key.StartsWith("OneGame.", StringComparison.Ordinal) || key == "OneGame";

    public static string? ExternalUrl(string key)
    {
        if (!(key.StartsWith("System.", StringComparison.Ordinal) || key.StartsWith("Microsoft.", StringComparison.Ordinal) || key == "System")) return null;
        return "https://learn.microsoft.com/dotnet/api/" + key.ToLowerInvariant().Replace('`', '-');
    }

    /// <summary>A link target for a type key from a signature, relative to the page at <paramref name="from"/>; null when it is only a name.</summary>
    public string? ForType(string key, string from)
    {
        if (_targets.TryGetValue("T:" + key, out var t)) return Rel(from, t.Path);
        return ExternalUrl(key);
    }

    public static string Rel(string from, string to)
    {
        string[] f = from.Split('/');
        string[] t = to.Split('/');
        int i = 0;
        while (i < f.Length - 1 && i < t.Length - 1 && f[i] == t[i]) i++;
        var parts = new List<string>();
        for (int k = i; k < f.Length - 1; k++) parts.Add("..");
        for (int k = i; k < t.Length; k++) parts.Add(Uri.EscapeDataString(t[k]).Replace("%60", "`"));
        return string.Join('/', parts);
    }

    /// <summary>Resolves a documentation comment cref. Returns a link (possibly null) and the text to show.</summary>
    public (string? Url, string Display) Cref(string cref, string from, string owner)
    {
        string display = CrefDisplay(cref);
        if (cref.Length < 3 || cref[1] != ':') return Fail(cref, owner, display);
        string id = cref;
        if (_targets.TryGetValue(id, out var hit)) return (Rel(from, hit.Path) + (hit.Anchor is null ? "" : "#" + hit.Anchor), display);

        string body = cref[2..];
        int paren = body.IndexOf('(');
        string nameOnly = paren < 0 ? body : body[..paren];
        if (cref[0] != 'T')
        {
            // Same member with a different parameter spelling (or an overload): the first overload by name.
            string prefix = cref[..2] + nameOnly;
            foreach (var kv in _targets)
            {
                if (kv.Key == prefix || kv.Key.StartsWith(prefix + "(", StringComparison.Ordinal) || kv.Key.StartsWith(prefix + "``", StringComparison.Ordinal))
                {
                    Rescued++;
                    return (Rel(from, kv.Value.Path) + (kv.Value.Anchor is null ? "" : "#" + kv.Value.Anchor), display);
                }
            }
            string owned = nameOnly[..Math.Max(0, nameOnly.LastIndexOf('.'))];
            if (_targets.ContainsKey("T:" + owned) || IsOurs(owned)) return Fail(cref, owner, display);
        }
        string typeKey = cref[0] == 'T' ? body : body[..Math.Max(0, body.LastIndexOf('.'))];
        if (cref[0] != 'T' && ExternalUrl(typeKey.TrimEnd('.')) is { } ext) return (ext, display);
        if (cref[0] == 'T' && ExternalUrl(body) is { } ext2) return (ext2, display);
        if (IsSibling(typeKey))
        {
            ToSibling++;
            return (null, display);
        }
        return Fail(cref, owner, display);
    }

    (string?, string) Fail(string cref, string owner, string display)
    {
        Unresolved.Add(owner + " -> " + cref);
        return (null, display);
    }

    string CrefDisplay(string cref)
    {
        if (cref.Length < 3 || cref[1] != ':') return cref.TrimStart('!', ':');
        string body = cref[2..];
        int paren = body.IndexOf('(');
        if (paren >= 0) body = body[..paren];
        if (cref[0] == 'T') return TypeShort(body);
        int dot = body.LastIndexOf('.');
        if (dot < 0) return body;
        string owner = TypeShort(body[..dot]);
        string member = body[(dot + 1)..];
        int gen = member.IndexOf("``", StringComparison.Ordinal);
        if (gen >= 0) member = member[..gen];
        if (member == "#ctor") return owner + "(…)";
        return owner + "." + member + (cref[0] == 'M' ? "()" : "");
    }

    string TypeShort(string key)
    {
        if (_typeByKey.TryGetValue(key, out TypeInfo? t)) return t.DisplayName;
        int dot = key.LastIndexOf('.');
        string last = dot < 0 ? key : key[(dot + 1)..];
        int tick = last.IndexOf('`');
        return tick < 0 ? last : last[..tick];
    }
}

/// <summary>Renders documentation comment XML as GitHub-flavoured Markdown.</summary>
sealed class Md(Links links, string from, string owner)
{
    static readonly Regex Spaces = new(@"\s+", RegexOptions.Compiled);
    static readonly Regex SentenceEnd = new(@"\.(?=\s|$)", RegexOptions.Compiled);

    public static string Escape(string s)
    {
        var sb = new StringBuilder(s.Length + 8);
        foreach (char c in s)
        {
            switch (c)
            {
                case '\\': case '`': case '*': case '_': case '[': case ']': case '#': case '~': case '|':
                    sb.Append('\\').Append(c);
                    break;
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                case '&': sb.Append("&amp;"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }

    public static string Code(string s)
    {
        s = Spaces.Replace(s, " ").Trim();
        if (s.Length == 0) return "";
        int longest = 0, run = 0;
        foreach (char c in s)
        {
            run = c == '`' ? run + 1 : 0;
            longest = Math.Max(longest, run);
        }
        string fence = new('`', longest + 1);
        string pad = s.StartsWith('`') || s.EndsWith('`') ? " " : "";
        return fence + pad + s + pad + fence;
    }

    sealed class Inline(bool firstSentence)
    {
        public readonly StringBuilder Sb = new();
        public bool Done;
        public bool FirstSentence = firstSentence;
    }

    /// <summary>The first sentence of the first paragraph, as inline Markdown.</summary>
    public string FirstSentence(XElement? el)
    {
        if (el is null) return "";
        var w = new Inline(true);
        Nodes(el.Nodes(), w);
        return w.Sb.ToString().Trim();
    }

    /// <summary>Block-level Markdown paragraphs for an element's content.</summary>
    public List<string> Blocks(XElement? el)
    {
        var blocks = new List<string>();
        if (el is not null) BlocksInto(el, blocks);
        return blocks;
    }

    /// <summary>One line of inline Markdown (paragraphs joined by a space).</summary>
    public string Line(XElement? el) => string.Join(" ", Blocks(el).Select(b => b.Replace("\n", " ")));

    void BlocksInto(XElement el, List<string> blocks)
    {
        var cur = new Inline(false);
        void Flush()
        {
            string s = cur.Sb.ToString().Trim();
            cur.Sb.Clear();
            if (s.Length == 0) return;
            if (Regex.IsMatch(s, @"^(\d+[.)]|[-+])(\s|$)")) s = "\\" + s;
            blocks.Add(s);
        }
        foreach (XNode n in el.Nodes())
        {
            if (n is XElement e)
            {
                switch (e.Name.LocalName)
                {
                    case "para":
                    case "remarks":
                    case "summary":
                    case "example":
                    case "returns":
                    case "value":
                    case "description":
                        Flush();
                        BlocksInto(e, blocks);
                        continue;
                    case "code":
                        Flush();
                        blocks.Add(Fence(e.Value, (string?)e.Attribute("language") ?? "csharp"));
                        continue;
                    case "list":
                        Flush();
                        blocks.Add(ListMd(e));
                        continue;
                }
            }
            Nodes([n], cur);
        }
        Flush();
    }

    public static string Fence(string code, string lang)
    {
        string[] lines = code.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        int first = 0, last = lines.Length - 1;
        while (first <= last && lines[first].Trim().Length == 0) first++;
        while (last >= first && lines[last].Trim().Length == 0) last--;
        int indent = int.MaxValue;
        for (int i = first; i <= last; i++)
        {
            if (lines[i].Trim().Length == 0) continue;
            int k = 0;
            while (k < lines[i].Length && lines[i][k] is ' ' or '\t') k++;
            indent = Math.Min(indent, k);
        }
        if (indent == int.MaxValue) indent = 0;
        var sb = new StringBuilder();
        int longest = 0;
        for (int i = first; i <= last; i++)
        {
            string l = (lines[i].Length >= indent ? lines[i][indent..] : lines[i].TrimStart()).TrimEnd();
            int run = 0;
            foreach (char c in l)
            {
                run = c == '`' ? run + 1 : 0;
                longest = Math.Max(longest, run);
            }
            sb.Append(l).Append('\n');
        }
        string fence = new('`', Math.Max(3, longest + 1));
        return fence + lang + "\n" + sb + fence;
    }

    string ListMd(XElement list)
    {
        string type = (string?)list.Attribute("type") ?? "bullet";
        var sb = new StringBuilder();
        if (type == "table")
        {
            XElement? header = list.Element("listheader");
            var rows = list.Elements("item").ToList();
            int cols = Math.Max(header is null ? 0 : Cells(header).Count, rows.Count == 0 ? 0 : rows.Max(r => Cells(r).Count));
            if (cols == 0) return "";
            List<string> h = header is null ? [] : Cells(header);
            while (h.Count < cols) h.Add("");
            sb.Append("| ").Append(string.Join(" | ", h)).Append(" |\n");
            sb.Append('|').Append(string.Concat(Enumerable.Repeat(" --- |", cols)));
            foreach (XElement r in rows)
            {
                List<string> c = Cells(r);
                while (c.Count < cols) c.Add("");
                sb.Append("\n| ").Append(string.Join(" | ", c)).Append(" |");
            }
            return sb.ToString();
        }
        int number = 1;
        foreach (XElement item in list.Elements("item"))
        {
            string marker = type == "number" ? number++ + ". " : "- ";
            List<string> cells = Cells(item);
            string text = cells.Count == 2 && cells[0].Length > 0 && cells[1].Length > 0
                ? "**" + cells[0] + "**: " + cells[1]
                : string.Join(" ", cells.Where(c => c.Length > 0));
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(marker).Append(text);
        }
        return sb.ToString();
    }

    // The term and description of a list item, each as one line; an item with plain text is one cell.
    List<string> Cells(XElement item)
    {
        var cells = new List<string>();
        XElement? term = item.Element("term");
        XElement? desc = item.Element("description");
        if (term is null && desc is null) cells.Add(Line(item).Replace("|", "\\|"));
        else
        {
            if (term is not null) cells.Add(Line(term).Replace("|", "\\|"));
            if (desc is not null) cells.Add(Line(desc).Replace("|", "\\|"));
        }
        return cells;
    }

    void Nodes(IEnumerable<XNode> nodes, Inline w)
    {
        XNode[] arr = nodes.ToArray();
        for (int i = 0; i < arr.Length && !w.Done; i++)
        {
            switch (arr[i])
            {
                case XText t:
                    Text(t.Value, w, i < arr.Length - 1);
                    break;
                case XElement e:
                    Element(e, w);
                    break;
            }
        }
    }

    static void Text(string raw, Inline w, bool moreFollows)
    {
        string s = Spaces.Replace(raw, " ");
        if (w.FirstSentence)
        {
            foreach (Match m in SentenceEnd.Matches(s))
            {
                string before = s[..m.Index];
                if (before.EndsWith("e.g", StringComparison.Ordinal) || before.EndsWith("i.e", StringComparison.Ordinal) || before.EndsWith(" vs", StringComparison.Ordinal)) continue;
                string after = s[(m.Index + 1)..].TrimStart();
                bool boundary = after.Length == 0 ? moreFollows || true : char.IsUpper(after[0]) || char.IsDigit(after[0]) || after[0] is '<' or '(' or '"';
                if (!boundary) continue;
                w.Sb.Append(Escape(s[..(m.Index + 1)]));
                w.Done = true;
                return;
            }
        }
        w.Sb.Append(Escape(s));
    }

    void Element(XElement e, Inline w)
    {
        switch (e.Name.LocalName)
        {
            case "c":
                w.Sb.Append(Code(e.Value));
                break;
            case "paramref":
            case "typeparamref":
                w.Sb.Append(Code((string?)e.Attribute("name") ?? ""));
                break;
            case "see":
            case "seealso":
                See(e, w);
                break;
            case "b":
            case "strong":
                w.Sb.Append("**");
                Nodes(e.Nodes(), w);
                w.Sb.Append("**");
                break;
            case "i":
            case "em":
                w.Sb.Append('*');
                Nodes(e.Nodes(), w);
                w.Sb.Append('*');
                break;
            case "a":
                {
                    string href = (string?)e.Attribute("href") ?? "";
                    string text = Spaces.Replace(e.Value, " ").Trim();
                    w.Sb.Append(href.Length == 0 ? Escape(text) : "[" + Escape(text.Length == 0 ? href : text) + "](" + href + ")");
                    break;
                }
            case "br":
                w.Sb.Append("  \n");
                break;
            case "para":
            case "code":
            case "list":
                // Block elements met while writing a sentence: the sentence is over.
                if (w.FirstSentence) w.Done = true;
                else if (e.Name.LocalName == "code") w.Sb.Append(Code(e.Value));
                else Nodes(e.Nodes(), w);
                break;
            default:
                Nodes(e.Nodes(), w);
                break;
        }
    }

    void See(XElement e, Inline w)
    {
        string? cref = (string?)e.Attribute("cref");
        string? href = (string?)e.Attribute("href");
        string? lang = (string?)e.Attribute("langword");
        string text = Spaces.Replace(e.Value, " ").Trim();
        if (lang is not null)
        {
            w.Sb.Append(Code(lang));
            return;
        }
        if (href is not null)
        {
            w.Sb.Append('[').Append(Escape(text.Length == 0 ? href : text)).Append("](").Append(href).Append(')');
            return;
        }
        if (cref is null) return;
        (string? url, string display) = links.Cref(cref, from, owner);
        string shown = text.Length > 0 ? Escape(text) : Code(display);
        w.Sb.Append(url is null ? (text.Length > 0 ? Code(text) : shown) : "[" + shown + "](" + url + ")");
    }

    /// <summary>Signature tokens as Markdown: names are code, linked names are links.</summary>
    public static string Tokens(List<Tok> toks, string from, Links links)
    {
        var parts = new List<(string Text, string? Url)>();
        foreach (Tok t in toks)
        {
            if (parts.Count > 0 && t.Url is null && parts[^1].Url is null) parts[^1] = (parts[^1].Text + t.Text, null);
            else parts.Add((t.Text, t.Url));
        }
        var sb = new StringBuilder();
        foreach ((string text, string? url) in parts)
        {
            // Spaces stay outside the code spans so "A, B" keeps its gap between two links.
            string core = text.Trim();
            if (core.Length == 0) { sb.Append(text); continue; }
            sb.Append(text[..(text.Length - text.TrimStart().Length)]);
            if (core.Length > 0)
            {
                string c = core.Any(char.IsLetterOrDigit) ? Code(core) : Escape(core);
                sb.Append(url is null ? c : "[" + c + "](" + url + ")");
            }
            sb.Append(text[(text.TrimEnd().Length)..]);
        }
        return sb.ToString();
    }
}
