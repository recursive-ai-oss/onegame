using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace OneGame.ApiDocs;

/// <summary>Writes the Markdown pages. All text is built in memory, so a check run and a write run produce the same bytes.</summary>
sealed class Pages
{
    readonly List<AssemblyInfo> _assemblies;
    readonly Links _links;
    readonly DocLookup _docs;
    readonly Dictionary<string, TypeInfo> _typeByKey;
    readonly SortedDictionary<string, string> _files = new(StringComparer.Ordinal);
    readonly bool _hasSdkPage;
    readonly List<string> _noSdk;

    public Pages(List<AssemblyInfo> assemblies, Dictionary<string, TypeInfo> typeByKey, Links links, DocLookup docs, bool hasSdkPage, List<string> noAssembly)
    {
        _assemblies = assemblies;
        _typeByKey = typeByKey;
        _links = links;
        _docs = docs;
        _hasSdkPage = hasSdkPage;
        _noSdk = noAssembly;
    }

    public SortedDictionary<string, string> Build()
    {
        foreach (AssemblyInfo a in _assemblies)
        {
            foreach (TypeInfo t in All(a.Types))
            {
                _files[t.Path] = TypePage(t);
            }
        }
        foreach (AssemblyInfo a in _assemblies) _files[a.Name + "/index.md"] = PackagePage(a);
        _files["types.md"] = TypesPage();
        _files["index.md"] = IndexPage();
        return _files;
    }

    public static IEnumerable<TypeInfo> All(IEnumerable<TypeInfo> roots)
    {
        foreach (TypeInfo t in roots)
        {
            yield return t;
            foreach (TypeInfo n in All(t.Nested.OrderBy(x => x.Key, StringComparer.Ordinal))) yield return n;
        }
    }

    static string KindLabel(TypeInfo t, Dictionary<string, TypeInfo> byKey)
    {
        switch (t.Kind)
        {
            case TypeKind.Interface: return "interface";
            case TypeKind.Enum: return "enum";
            case TypeKind.Delegate: return "delegate";
            case TypeKind.Struct: return t.IsRecord ? "record struct" : "struct";
        }
        // Walk the base chain inside the engine; an attribute derives from System.Attribute somewhere up it.
        TypeSig? b = t.Base;
        for (int i = 0; i < 16 && b is not null; i++)
        {
            string? key = b switch { NamedSig n => n.Key, GenericSig g => g.Def.Key, _ => null };
            if (key == "System.Attribute") return "attribute";
            if (key is null || !byKey.TryGetValue(key, out TypeInfo? up)) break;
            b = up.Base;
        }
        if (t.Declaration.Contains("static class ", StringComparison.Ordinal)) return "static class";
        return t.IsRecord ? "record" : "class";
    }

    static string PackageLine(AssemblyInfo a, string from) =>
        "[`" + a.Package + "`](" + Links.Rel(from, a.Name + "/index.md") + ")";

    // ---------------------------------------------------------------- type pages

    string TypePage(TypeInfo t)
    {
        string path = t.Path;
        var md = new Md(_links, path, t.DocId);
        var sb = new StringBuilder();
        string root = Links.Rel(path, "index.md");
        string nsLabel = t.Namespace.Length == 0 ? "(global namespace)" : t.Namespace;
        sb.Append("[API reference](").Append(root).Append(") › [").Append(t.Assembly.Name).Append("](").Append(Links.Rel(path, t.Assembly.Name + "/index.md")).Append(") › ")
          .Append(Escape(nsLabel)).Append("\n\n");
        sb.Append("# ").Append(Md.Escape(t.DisplayName)).Append(' ').Append(KindLabel(t, _typeByKey)).Append("\n\n");
        sb.Append("**Namespace:** ").Append(Md.Code(nsLabel)).Append("  \n");
        sb.Append("**Package:** ").Append(PackageLine(t.Assembly, path));
        if (t.Outer is not null) sb.Append("  \n**Declared in:** ").Append(TypeLink(t.Outer, path));
        if (t.Kind == TypeKind.Class && t.Base is not null && !(t.Base is NamedSig { Key: "System.Object" }))
            sb.Append("  \n**Inherits:** ").Append(SigMd(t.Base, path));
        if (t.Kind != TypeKind.Enum && t.Interfaces.Count > 0)
            sb.Append("  \n**Implements:** ").Append(string.Join(", ", t.Interfaces.Select(i => SigMd(i, path))));
        if (t.AttributeTargets is not null) sb.Append("  \n**Applies to:** ").Append(Md.Code(t.AttributeTargets));
        sb.Append("\n\n");
        sb.Append("```csharp\n").Append(t.Declaration).Append("\n```\n\n");
        if (t.Obsolete is not null) sb.Append(ObsoleteNote(t.Obsolete)).Append("\n\n");

        Doc doc = _docs.Get(t, null);
        DocBody(sb, md, doc, null, path, t);

        if (t.Nested.Count > 0)
        {
            sb.Append("## Nested types\n\n| Type | Description |\n| --- | --- |\n");
            foreach (TypeInfo n in t.Nested.OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                string sum = new Md(_links, path, n.DocId).FirstSentence(_docs.Get(n, null).Summary);
                sb.Append("| ").Append(TypeLink(n, path)).Append(" | ").Append(sum.Replace("|", "\\|")).Append(" |\n");
            }
            sb.Append('\n');
        }

        var groups = new (MemberKind Kind, string Title)[]
        {
            (MemberKind.Constructor, "Constructors"), (MemberKind.Field, t.Kind == TypeKind.Enum ? "Members" : "Fields"),
            (MemberKind.Property, "Properties"), (MemberKind.Event, "Events"), (MemberKind.Method, "Methods"), (MemberKind.Operator, "Operators"),
        };
        foreach ((MemberKind kind, string title) in groups)
        {
            var members = t.Members.Where(m => m.Kind == kind).ToList();
            if (members.Count == 0) continue;
            if (t.Kind != TypeKind.Enum) members.Sort((a, b) => string.CompareOrdinal(a.SortKey, b.SortKey));
            sb.Append("## ").Append(title).Append("\n\n");
            if (t.Kind == TypeKind.Enum)
            {
                sb.Append("| Name | Value | Description |\n| --- | --- | --- |\n");
                foreach (MemberInfo m in members)
                {
                    Doc d = _docs.Get(t, m);
                    string desc = md.Line(d.Summary).Replace("|", "\\|");
                    string name = "<a id=\"" + m.Anchor + "\"></a>" + Md.Code(m.Name) + (m.Obsolete is null ? "" : " (obsolete)");
                    sb.Append("| ").Append(name).Append(" | ").Append(Md.Code(m.ConstValue ?? "")).Append(" | ").Append(desc).Append(" |\n");
                }
                sb.Append('\n');
                continue;
            }
            sb.Append("| Member | Description |\n| --- | --- |\n");
            foreach (MemberInfo m in members)
            {
                string sum = md.FirstSentence(_docs.Get(t, m).Summary).Replace("|", "\\|");
                sb.Append("| [").Append(Md.Code(ShortName(m))).Append("](#").Append(m.Anchor).Append(") | ").Append(sum).Append(" |\n");
            }
            sb.Append('\n');
            foreach (MemberInfo m in members)
            {
                sb.Append("### <a id=\"").Append(m.Anchor).Append("\"></a>").Append(Md.Escape(ShortName(m))).Append("\n\n");
                sb.Append("```csharp\n").Append(m.Declaration).Append("\n```\n\n");
                if (m.Obsolete is not null) sb.Append(ObsoleteNote(m.Obsolete)).Append("\n\n");
                DocBody(sb, md, _docs.Get(t, m), m, path, t);
            }
        }
        return Finish(sb);
    }

    static string Escape(string s) => Md.Escape(s);

    // The first sentence of a type's summary, with its links written relative to the page it appears on.
    string Sum(TypeInfo t, string from) => new Md(_links, from, t.DocId).FirstSentence(_docs.Get(t, null).Summary);

    static string ObsoleteNote(string message) =>
        "> **Obsolete.**" + (message.Length > 0 ? " " + Md.Escape(message) : "");

    static string ShortName(MemberInfo m)
    {
        if (m.Kind is MemberKind.Field or MemberKind.Event) return m.Name;
        if (m.Kind == MemberKind.Property && m.Params.Count == 0) return m.Name;
        string types = string.Join(", ", m.Params.Select(p => (p.Modifier is "ref" or "out" or "in" ? p.Modifier + " " : "") + TypeText.Plain(p.Type)));
        string tp = m.TypeParams.Length > 0 ? "<" + string.Join(", ", m.TypeParams) + ">" : "";
        if (m.Kind == MemberKind.Property) return "this[" + types + "]";
        return m.Name + tp + "(" + types + ")";
    }

    static string TypeLink(TypeInfo t, string from) => "[" + Md.Code(t.DisplayName) + "](" + Links.Rel(from, t.Path) + ")";

    string SigMd(TypeSig sig, string from) =>
        Md.Tokens(TypeText.Tokens(new TypeUse(sig, null, 0, null), k => _links.ForType(k, from)), from, _links);

    string UseMd(TypeUse use, string from) =>
        Md.Tokens(TypeText.Tokens(use, k => _links.ForType(k, from)), from, _links);

    void DocBody(StringBuilder sb, Md md, Doc doc, MemberInfo? m, string path, TypeInfo owner)
    {
        foreach (string b in md.Blocks(doc.Summary)) sb.Append(b).Append("\n\n");
        foreach (string b in md.Blocks(doc.Remarks)) sb.Append(b).Append("\n\n");

        if (doc.TypeParams.Count > 0)
        {
            sb.Append("**Type parameters**\n\n");
            foreach ((string n, XElement e) in doc.TypeParams) sb.Append("- ").Append(Md.Code(n)).Append(": ").Append(md.Line(e)).Append('\n');
            sb.Append('\n');
        }
        bool isDelegate = m is null && owner.Kind == TypeKind.Delegate;
        if (m is not null && m.Params.Count > 0 || isDelegate && doc.Params.Count > 0)
        {
            sb.Append("**Parameters**\n\n");
            if (m is not null)
            {
                foreach (ParamInfo p in m.Params)
                {
                    string d = doc.Params.FirstOrDefault(x => x.Name == p.Name).El is { } el ? md.Line(el) : "";
                    sb.Append("- ").Append(Md.Code(p.Name)).Append(" (").Append(UseMd(p.Type, path)).Append(')');
                    if (d.Length > 0) sb.Append(": ").Append(d);
                    sb.Append('\n');
                }
            }
            else
            {
                foreach ((string n, XElement e) in doc.Params) sb.Append("- ").Append(Md.Code(n)).Append(": ").Append(md.Line(e)).Append('\n');
            }
            sb.Append('\n');
        }
        if (m is not null && m.Type is not null)
        {
            string label = m.Kind is MemberKind.Field or MemberKind.Property or MemberKind.Event ? "Type" : "Returns";
            string text = label == "Type" ? md.Line(doc.Value) : md.Line(doc.Returns);
            sb.Append("**").Append(label).Append(":** ").Append(UseMd(m.Type, path));
            if (text.Length > 0) sb.Append(" — ").Append(text);
            sb.Append("\n\n");
        }
        else if (isDelegate && doc.Returns is not null) sb.Append("**Returns:** ").Append(md.Line(doc.Returns)).Append("\n\n");

        if (doc.Exceptions.Count > 0)
        {
            sb.Append("**Exceptions**\n\n");
            foreach (XElement e in doc.Exceptions)
            {
                string cref = (string?)e.Attribute("cref") ?? "";
                (string? url, string display) = _links.Cref(cref, path, m?.DocId ?? owner.DocId);
                string name = Md.Code(display);
                sb.Append("- ").Append(url is null ? name : "[" + name + "](" + url + ")");
                string cond = md.Line(e);
                if (cond.Length > 0) sb.Append(": ").Append(cond);
                sb.Append('\n');
            }
            sb.Append('\n');
        }
        foreach (XElement ex in doc.Examples)
        {
            sb.Append("**Example**\n\n");
            foreach (string b in md.Blocks(ex)) sb.Append(b).Append("\n\n");
        }
        if (doc.SeeAlso.Count > 0)
        {
            var items = new List<string>();
            foreach (XElement e in doc.SeeAlso)
            {
                var holder = new XElement("x", new XElement("see", e.Attributes(), e.Nodes()));
                items.Add(md.Line(holder));
            }
            sb.Append("**See also:** ").Append(string.Join(", ", items.Where(i => i.Length > 0))).Append("\n\n");
        }
    }

    static string Finish(StringBuilder sb)
    {
        string s = sb.ToString().TrimEnd('\n', ' ') + "\n";
        return s;
    }

    // ---------------------------------------------------------------- index pages

    string PackagePage(AssemblyInfo a)
    {
        string path = a.Name + "/index.md";
        var sb = new StringBuilder();
        sb.Append("[API reference](../index.md) › ").Append(a.Name).Append("\n\n# ").Append(a.Name).Append("\n\n");
        sb.Append(Md.Escape(a.Description)).Append("\n\n");
        sb.Append("**Package:** ").Append(Md.Code(a.Package)).Append("\n\n```sh\ndotnet add package ").Append(a.Package).Append("\n```\n\n");
        TypeInfo[] types = [.. All(a.Types)];
        if (types.Length == 0) sb.Append("This package has no public .NET API: it ships a command or build files, which the guides describe.\n");
        foreach (var g in types.GroupBy(t => t.Namespace).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            sb.Append("## ").Append(g.Key.Length == 0 ? "(global namespace)" : Md.Escape(g.Key)).Append("\n\n| Type | Kind | Description |\n| --- | --- | --- |\n");
            foreach (TypeInfo t in g.OrderBy(x => x.Key, StringComparer.Ordinal))
                sb.Append("| ").Append(TypeLink(t, path)).Append(" | ").Append(KindLabel(t, _typeByKey)).Append(" | ").Append(Sum(t, path).Replace("|", "\\|")).Append(" |\n");
            sb.Append('\n');
        }
        return Finish(sb);
    }

    string TypesPage()
    {
        var sb = new StringBuilder();
        sb.Append("[API reference](index.md)\n\n# All types\n\nEvery public type in every package, in alphabetical order.\n\n| Type | Kind | Namespace | Package | Description |\n| --- | --- | --- | --- | --- |\n");
        var all = _assemblies.SelectMany(a => All(a.Types)).OrderBy(t => t.DisplayName, StringComparer.OrdinalIgnoreCase).ThenBy(t => t.Key, StringComparer.Ordinal);
        foreach (TypeInfo t in all)
            sb.Append("| ").Append(TypeLink(t, "types.md")).Append(" | ").Append(KindLabel(t, _typeByKey)).Append(" | ").Append(Md.Code(t.Namespace.Length == 0 ? "(global)" : t.Namespace))
              .Append(" | [").Append(t.Assembly.Name).Append("](").Append(t.Assembly.Name).Append("/index.md) | ").Append(Sum(t, "types.md").Replace("|", "\\|")).Append(" |\n");
        return Finish(sb);
    }

    string IndexPage()
    {
        var sb = new StringBuilder();
        sb.Append("# onegame API reference\n\n");
        sb.Append("The public API of every onegame package, generated from the built assemblies and their documentation comments. ")
          .Append("Look a type up by package below, or by name in [all types](types.md). The guides in [docs/](../) explain how the pieces fit together; this is the detail behind them.\n\n");
        sb.Append("How the pages are laid out, which documentation tags they support and how to regenerate them: [README](README.md).\n\n");
        sb.Append("## Packages\n\n| Package | Namespaces | Types | Members | What it is |\n| --- | --- | --- | --- | --- |\n");
        foreach (AssemblyInfo a in _assemblies)
        {
            TypeInfo[] types = [.. All(a.Types)];
            sb.Append("| [").Append(a.Package).Append("](").Append(a.Name).Append("/index.md) | ").Append(types.Select(t => t.Namespace).Distinct().Count())
              .Append(" | ").Append(types.Length).Append(" | ").Append(types.Sum(t => t.Members.Count)).Append(" | ").Append(Md.Escape(a.Description).Replace("|", "\\|")).Append(" |\n");
        }
        foreach (string n in _noSdk)
            sb.Append("| ").Append(_hasSdkPage ? "[RecursiveAI." + n + "](sdk.md)" : "RecursiveAI." + n).Append(" | – | – | – | MSBuild properties and items, not .NET types. |\n");
        sb.Append('\n');
        if (_hasSdkPage) sb.Append("The MSBuild SDK's properties and items (`OneGameKind` and the rest) are on the [SDK page](sdk.md).\n");
        else sb.Append("The MSBuild SDK's properties and items are described on the SDK page, which is not written yet.\n");
        return Finish(sb);
    }
}
