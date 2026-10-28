using System.Xml.Linq;

namespace OneGame.ApiDocs;

/// <summary>The documentation of one type or member, with <c>inheritdoc</c> already filled in from the member it inherits.</summary>
sealed class Doc
{
    public XElement? Summary;
    public XElement? Remarks;
    public XElement? Returns;
    public XElement? Value;
    public List<(string Name, XElement El)> Params = [];
    public List<(string Name, XElement El)> TypeParams = [];
    public List<XElement> Exceptions = [];
    public List<XElement> Examples = [];
    public List<XElement> SeeAlso = [];

    /// <summary>For an <c>inheritdoc</c> whose source is outside the engine: the member it implements or overrides.</summary>
    public Inherited? Source;

    public bool IsEmpty => Summary is null && Remarks is null && Returns is null && Value is null && Params.Count == 0 && Exceptions.Count == 0 && Examples.Count == 0;

    public static Doc From(XElement member)
    {
        var d = new Doc();
        foreach (XElement e in member.Elements())
        {
            switch (e.Name.LocalName)
            {
                case "summary": d.Summary ??= e; break;
                case "remarks": d.Remarks ??= e; break;
                case "returns": d.Returns ??= e; break;
                case "value": d.Value ??= e; break;
                case "param": d.Params.Add(((string?)e.Attribute("name") ?? "", e)); break;
                case "typeparam": d.TypeParams.Add(((string?)e.Attribute("name") ?? "", e)); break;
                case "exception": d.Exceptions.Add(e); break;
                case "example": d.Examples.Add(e); break;
                case "seealso": d.SeeAlso.Add(e); break;
            }
        }
        return d;
    }

    /// <summary>Fills what this doc lacks from <paramref name="from"/>; what the member says itself wins.</summary>
    public void FillFrom(Doc from)
    {
        Summary ??= from.Summary;
        Remarks ??= from.Remarks;
        Returns ??= from.Returns;
        Value ??= from.Value;
        foreach ((string n, XElement e) in from.Params) if (!Params.Any(p => p.Name == n)) Params.Add((n, e));
        foreach ((string n, XElement e) in from.TypeParams) if (!TypeParams.Any(p => p.Name == n)) TypeParams.Add((n, e));
        foreach (XElement e in from.Exceptions)
            if (!Exceptions.Any(x => (string?)x.Attribute("cref") == (string?)e.Attribute("cref"))) Exceptions.Add(e);
        if (Examples.Count == 0) Examples.AddRange(from.Examples);
        if (SeeAlso.Count == 0) SeeAlso.AddRange(from.SeeAlso);
    }
}

/// <summary>A member of a .NET or sibling-library type that an engine member implements or overrides.</summary>
sealed record Inherited(string TypeKey, string TypeDisplay, string Member, bool IsInterface, bool IsConstructor);

/// <summary>Finds a documented member's doc, following <c>inheritdoc</c> to the base type or interface it comes from.</summary>
sealed class DocLookup(Dictionary<string, XElement> xml, Dictionary<string, TypeInfo> typesByKey)
{
    readonly Dictionary<string, Doc?> _cache = new(StringComparer.Ordinal);
    public int UnresolvedInheritDoc;
    public List<string> UnresolvedInheritDocIds { get; } = [];

    public Doc Get(TypeInfo type, MemberInfo? member) => Resolve(type, member, 0) ?? new Doc();

    Doc? Resolve(TypeInfo type, MemberInfo? member, int depth)
    {
        string id = member?.DocId ?? type.DocId;
        if (_cache.TryGetValue(id, out Doc? hit)) return hit;
        _cache[id] = null; // breaks cycles
        Doc? result = null;
        if (xml.TryGetValue(id, out XElement? el))
        {
            result = Doc.From(el);
            XElement? inherit = el.Elements("inheritdoc").FirstOrDefault();
            if (inherit is not null && depth < 16)
            {
                Doc? src = null;
                string? cref = (string?)inherit.Attribute("cref");
                if (cref is not null)
                {
                    if (xml.TryGetValue(cref, out XElement? target))
                    {
                        src = Doc.From(target);
                        if (target.Element("inheritdoc") is not null && FindOwner(cref) is { } o) src = Resolve(o.Type, o.Member, depth + 1) ?? src;
                    }
                }
                else src = FromAncestors(type, member, depth);
                if (src is not null) result.FillFrom(src);
                else
                {
                    result.Source = member is null ? null : External(type, member);
                    if (result.Source is null)
                    {
                        UnresolvedInheritDoc++;
                        UnresolvedInheritDocIds.Add(id);
                    }
                }
            }
        }
        else if (member is null)
        {
            // An undocumented type with no comment at all has nothing to inherit either.
            result = null;
        }
        _cache[id] = result;
        return result;
    }

    (TypeInfo Type, MemberInfo? Member)? FindOwner(string docId)
    {
        foreach (TypeInfo t in typesByKey.Values)
        {
            if (t.DocId == docId) return (t, null);
            MemberInfo? m = t.Members.FirstOrDefault(x => x.DocId == docId);
            if (m is not null) return (t, m);
        }
        return null;
    }

    IEnumerable<TypeInfo> Ancestors(TypeInfo t)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal) { t.Key };
        var queue = new Queue<TypeSig>();
        void Enqueue(TypeInfo x)
        {
            if (x.Base is not null) queue.Enqueue(x.Base);
            foreach (TypeSig i in x.Interfaces) queue.Enqueue(i);
        }
        Enqueue(t);
        while (queue.Count > 0)
        {
            TypeSig s = queue.Dequeue();
            string? key = s switch { NamedSig n => n.Key, GenericSig g => g.Def.Key, _ => null };
            if (key is null || !seen.Add(key) || !typesByKey.TryGetValue(key, out TypeInfo? a)) continue;
            yield return a;
            Enqueue(a);
        }
    }

    Doc? FromAncestors(TypeInfo type, MemberInfo? member, int depth)
    {
        foreach (TypeInfo a in Ancestors(type))
        {
            if (member is null)
            {
                Doc? d = Resolve(a, null, depth + 1);
                if (d is not null) return d;
                continue;
            }
            MemberInfo? match = a.Members.FirstOrDefault(m => m.Kind == member.Kind && m.Name == member.Name && SameParams(m, member))
                ?? a.Members.FirstOrDefault(m => m.Kind == member.Kind && m.Name == member.Name && m.Params.Count == member.Params.Count);
            if (match is null) continue;
            Doc? d2 = Resolve(a, match, depth + 1);
            if (d2 is not null) return d2;
        }
        return null;
    }

    static bool SameParams(MemberInfo a, MemberInfo b) =>
        a.Params.Count == b.Params.Count && a.Params.Zip(b.Params).All(p => DocId.Of(p.First.Type.Sig) == DocId.Of(p.Second.Type.Sig));

    // The first ancestor outside the engine (base class, then interfaces, then object) that has a member of this name and arity.
    // Names are looked up in the runtime's own libraries; that only reads member names, nothing from the engine is loaded.
    Inherited? External(TypeInfo type, MemberInfo member)
    {
        if (member.Kind == MemberKind.Constructor) return new Inherited(type.Key, type.DisplayName, type.Names[^1], false, true);
        string name = member.Kind == MemberKind.Property && member.Name.StartsWith("this[", StringComparison.Ordinal) ? "Item" : member.Name;
        var seen = new HashSet<string>(StringComparer.Ordinal) { type.Key };
        var queue = new Queue<TypeSig>();
        void Enqueue(TypeInfo x)
        {
            if (x.Base is not null) queue.Enqueue(x.Base);
            foreach (TypeSig i in x.Interfaces) queue.Enqueue(i);
        }
        Enqueue(type);
        var external = new List<(TypeSig Sig, string Key)>();
        while (queue.Count > 0)
        {
            TypeSig s = queue.Dequeue();
            string? key = s switch { NamedSig n => n.Key, GenericSig g => g.Def.Key, _ => null };
            if (key is null || !seen.Add(key)) continue;
            if (typesByKey.TryGetValue(key, out TypeInfo? ours)) Enqueue(ours);
            else external.Add((s, key));
        }
        if (!seen.Contains("System.Object")) external.Add((new NamedSig("System", ["Object"], false), "System.Object"));
        foreach ((TypeSig sig, string key) in external)
        {
            Type? t = FindRuntimeType(key);
            if (t is null || !HasMember(t, name, member.Params, member.Kind)) continue;
            return new Inherited(key, DisplayOf(sig, key), name, t.IsInterface, false);
        }
        // A sibling library's type cannot be inspected here; when it is the only candidate it is the source.
        var siblings = external.Where(e => e.Key.StartsWith("One", StringComparison.Ordinal) && !e.Key.StartsWith("OneGame.", StringComparison.Ordinal)).ToList();
        if (siblings.Count == 1) return new Inherited(siblings[0].Key, DisplayOf(siblings[0].Sig, siblings[0].Key), name, true, false);
        return null;
    }

    static string DisplayOf(TypeSig sig, string key)
    {
        string plain = TypeText.Plain(new TypeUse(sig, null, 0, null));
        return key == "System.Object" ? "Object" : plain;
    }

    static readonly string[] RuntimeAssemblies =
    [
        "", ", System.Private.CoreLib", ", System.Collections", ", System.Linq", ", System.Text.Json", ", System.Runtime",
        ", System.Memory", ", System.ObjectModel", ", System.Collections.Concurrent", ", System.Collections.Immutable",
    ];

    static Type? FindRuntimeType(string key)
    {
        foreach (string a in RuntimeAssemblies)
        {
            Type? t = Type.GetType(key + a, false);
            if (t is not null) return t;
        }
        return null;
    }

    static bool HasMember(Type t, string name, List<ParamInfo> ps, MemberKind kind)
    {
        const System.Reflection.BindingFlags all = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
        if (kind == MemberKind.Property)
            return t.GetProperties(all).Any(p => p.Name == name && p.GetIndexParameters().Length == ps.Count);
        if (kind == MemberKind.Event) return t.GetEvents(all).Any(e => e.Name == name);
        return t.GetMethods(all).Any(m => m.Name == name && SameShape(m.GetParameters(), ps));
    }

    // Same number of parameters, and a parameter of type object only matches object: Equals(Fixed) is IEquatable's, Equals(object) is the base class's.
    static bool SameShape(System.Reflection.ParameterInfo[] actual, List<ParamInfo> ps)
    {
        if (actual.Length != ps.Count) return false;
        for (int i = 0; i < actual.Length; i++)
        {
            bool wantObject = ps[i].Type.Sig is NamedSig { Key: "System.Object" };
            if (wantObject != (actual[i].ParameterType == typeof(object))) return false;
        }
        return true;
    }
}
