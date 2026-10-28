using System.Text;

namespace OneGame.ApiDocs;

readonly record struct Tok(string Text, string? Url = null);

/// <summary>Turns a <see cref="TypeUse"/> into C# source text, optionally with links on the type names.</summary>
static class TypeText
{
    static readonly Dictionary<string, string> Keywords = new(StringComparer.Ordinal)
    {
        ["System.Void"] = "void", ["System.Boolean"] = "bool", ["System.Char"] = "char", ["System.SByte"] = "sbyte",
        ["System.Byte"] = "byte", ["System.Int16"] = "short", ["System.UInt16"] = "ushort", ["System.Int32"] = "int",
        ["System.UInt32"] = "uint", ["System.Int64"] = "long", ["System.UInt64"] = "ulong", ["System.Single"] = "float",
        ["System.Double"] = "double", ["System.String"] = "string", ["System.Object"] = "object",
        ["System.IntPtr"] = "nint", ["System.UIntPtr"] = "nuint", ["System.Decimal"] = "decimal",
    };

    sealed class Walker(TypeUse use, Func<string, string?>? link)
    {
        readonly List<Tok> _out = [];
        int _flag;
        int _tuple;

        public List<Tok> Run()
        {
            Walk(use.Sig);
            return _out;
        }

        byte Next()
        {
            if (use.Flags is null) return use.Context;
            if (use.Flags.Length == 1) return use.Flags[0];
            return _flag < use.Flags.Length ? use.Flags[_flag++] : (byte)0;
        }

        void Add(string text, string? url = null) => _out.Add(new Tok(text, url));

        void Walk(TypeSig s)
        {
            switch (s)
            {
                case ModSig m: Walk(m.Inner); break;
                case ByRefSig b: Walk(b.Elem); break;
                case PointerSig p: Walk(p.Elem); Add("*"); break;
                case FnPtrSig: Add("delegate*"); break;
                case ArraySig a:
                {
                    byte f = Next();
                    Walk(a.Elem);
                    Add("[" + new string(',', a.Rank - 1) + "]");
                    if (f == 2) Add("?");
                    break;
                }
                case ParamSig p:
                {
                    byte f = Next();
                    Add(p.Name);
                    if (f == 2) Add("?");
                    break;
                }
                case NamedSig n:
                {
                    byte f = Next();
                    Named(n, []);
                    if (!n.IsValueType && f == 2) Add("?");
                    break;
                }
                case GenericSig g:
                    Generic(g);
                    break;
            }
        }

        void Generic(GenericSig g)
        {
            byte f = Next();
            string key = g.Def.Key;
            if (key == "System.Nullable`1")
            {
                Walk(g.Args[0]);
                Add("?");
                return;
            }
            if (key.StartsWith("System.ValueTuple`", StringComparison.Ordinal) && g.Args.Length is >= 2 and <= 7)
            {
                string?[]? names = use.TupleNames;
                int start = _tuple;
                _tuple += g.Args.Length;
                Add("(");
                for (int i = 0; i < g.Args.Length; i++)
                {
                    if (i > 0) Add(", ");
                    Walk(g.Args[i]);
                    string? nm = names is not null && start + i < names.Length ? names[start + i] : null;
                    if (nm is not null) Add(" " + nm);
                }
                Add(")");
                if (f == 2) Add("?");
                return;
            }
            Named(g.Def, g.Args);
            if (!g.Def.IsValueType && f == 2) Add("?");
        }

        // Writes Outer<A>.Inner<B>: each segment takes as many of the arguments as its own arity says.
        void Named(NamedSig n, TypeSig[] args)
        {
            string? url = link?.Invoke(n.Key);
            if (args.Length == 0 && Keywords.TryGetValue(n.Key, out string? kw))
            {
                Add(kw);
                return;
            }
            int next = 0;
            for (int i = 0; i < n.Names.Length; i++)
            {
                (string name, int arity) = DocId.Split(n.Names[i]);
                if (i > 0) Add(".");
                bool last = i == n.Names.Length - 1;
                // Link the whole chain once, on the innermost segment's text, so "Outer.Inner" is one link.
                Add(name, last ? url : null);
                if (arity > 0 && args.Length >= next + arity)
                {
                    Add("<");
                    for (int k = 0; k < arity; k++)
                    {
                        if (k > 0) Add(", ");
                        Walk(args[next++]);
                    }
                    Add(">");
                }
            }
        }
    }

    public static List<Tok> Tokens(TypeUse use, Func<string, string?>? link) => new Walker(use, link).Run();

    public static string Plain(TypeUse use)
    {
        var sb = new StringBuilder();
        foreach (Tok t in Tokens(use, null)) sb.Append(t.Text);
        return sb.ToString();
    }

    /// <summary>The name of a type definition as declared, with its own type parameters: <c>Outer.Inner&lt;T&gt;</c>.</summary>
    public static string DeclName(string[] names, string[] ownParams)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < names.Length; i++)
        {
            (string n, int arity) = DocId.Split(names[i]);
            if (i > 0) sb.Append('.');
            sb.Append(n);
            if (i == names.Length - 1 && ownParams.Length > 0) sb.Append('<').Append(string.Join(", ", ownParams)).Append('>');
        }
        return sb.ToString();
    }
}
