using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace OneGame.Generators;

/// <summary>Emits the codec, equality and JSON dump for a [SyncedStruct], and the column table for a [HotData] struct.</summary>
internal static class ValueEmitters
{
    private sealed class Member
    {
        public Member(ISymbol symbol, ITypeSymbol type, Codec codec)
        {
            Symbol = symbol;
            Type = type;
            Codec = codec;
        }

        public ISymbol Symbol { get; }

        public ITypeSymbol Type { get; }

        public Codec Codec { get; }

        public string Name => Symbol.Name;
    }

    private static List<Member>? Collect(INamedTypeSymbol type, List<DiagData> diags)
    {
        var members = new List<Member>();
        var ok = true;
        var symbols = type.GetMembers().Where(m => !m.IsStatic && !m.IsImplicitlyDeclared && m.DeclaredAccessibility == Accessibility.Public
            && (m is IFieldSymbol { IsConst: false } || m is IPropertySymbol { IsIndexer: false })).ToList();
        symbols.Sort(EmitUtil.SourceOrder);
        foreach (var m in symbols)
        {
            var t = m is IFieldSymbol f ? f.Type : ((IPropertySymbol)m).Type;
            if (m is IFieldSymbol { IsReadOnly: true } || (m is IPropertySymbol p && (p.GetMethod is null || p.SetMethod is null || p.SetMethod.IsInitOnly)))
            {
                diags.Add(DiagData.Create(Rules.PropertyShape, EmitUtil.LocationOf(m),
                    $"Member '{m.Name}' of '{type.Name}' must be a writable public field or a property with get and set, so the decoder can fill it."));
                ok = false;
                continue;
            }

            var codec = Codecs.TryGet(t);
            if (codec is null)
            {
                // Floats are reported by OG0001 and other types by OG0009 from the analyzer; the generator leaves them out.
                ok = false;
                continue;
            }

            members.Add(new Member(m, t, codec));
        }

        return ok ? members : null;
    }

    public static GenResult BuildStruct(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        var diags = new List<DiagData>();
        if (context.TargetSymbol is not INamedTypeSymbol type)
        {
            return new GenResult(null, null, default, null);
        }

        if (!EmitUtil.IsPartialTopLevel(type, out var problem))
        {
            diags.Add(DiagData.Create(Rules.EntityShape, EmitUtil.LocationOf(type), problem!));
            return new GenResult(null, null, new EquatableArray<DiagData>(diags.ToArray()), null);
        }

        var members = Collect(type, diags);
        if (members is null)
        {
            return new GenResult(null, null, new EquatableArray<DiagData>(diags.ToArray()), null);
        }

        var name = type.Name;
        var s = new Src();
        var ns = EmitUtil.Header(s, type);
        if (ns.Length > 0)
        {
            s.Open("namespace " + ns);
        }

        s.Open($"{EmitUtil.Access(type.DeclaredAccessibility == Accessibility.NotApplicable ? Accessibility.Internal : type.DeclaredAccessibility)} partial struct {name} : global::OneGame.Core.ISyncValue<{name}>");
        s.Line("/// <inheritdoc />");
        s.Open("public void Write(global::OneGame.Core.SyncWriter w)");
        foreach (var m in members)
        {
            s.Line(m.Codec.Write(m.Name));
        }

        s.Close();
        s.Line("/// <inheritdoc />");
        s.Open($"public static {name} Read(ref global::OneGame.Core.SyncReader r)");
        s.Line($"{name} v = default;");
        foreach (var m in members)
        {
            s.Line($"v.{m.Name} = {m.Codec.Read()};");
        }

        s.Line("return v;");
        s.Close();
        s.Line("/// <inheritdoc />");
        s.Open("public void WriteJson(global::OneGame.Core.JsonOut j)");
        s.Line("j.BeginObject();");
        foreach (var m in members)
        {
            s.Line($"j.Name({EmitUtil.Literal(m.Name)});");
            s.Line(m.Codec.Json(m.Name));
        }

        s.Line("j.EndObject();");
        s.Close();
        s.Line("/// <inheritdoc />");
        var equal = members.Count == 0 ? "true" : string.Join(" && ", members.Select(m => m.Codec.Equal(m.Name, "other." + m.Name)));
        s.Line($"public bool Equals({name} other) => {equal};");
        s.Line("/// <inheritdoc />");
        s.Line($"public override bool Equals(object? obj) => obj is {name} other && Equals(other);");
        s.Line("/// <inheritdoc />");
        s.Open("public override int GetHashCode()");
        s.Line("var h = new global::System.HashCode();");
        foreach (var m in members)
        {
            s.Line($"h.Add({m.Name});");
        }

        s.Line("return h.ToHashCode();");
        s.Close();
        s.Line("/// <summary>True if every member is equal.</summary>");
        s.Line($"public static bool operator ==({name} a, {name} b) => a.Equals(b);");
        s.Line("/// <summary>True if any member differs.</summary>");
        s.Line($"public static bool operator !=({name} a, {name} b) => !a.Equals(b);");
        s.Close();
        if (ns.Length > 0)
        {
            s.Close();
        }

        return new GenResult(EmitUtil.HintName(type, "Struct"), s.ToString(), new EquatableArray<DiagData>(diags.ToArray()), null);
    }

    public static GenResult BuildTable(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        var diags = new List<DiagData>();
        if (context.TargetSymbol is not INamedTypeSymbol type)
        {
            return new GenResult(null, null, default, null);
        }

        if (type.ContainingType is not null || type.IsGenericType)
        {
            diags.Add(DiagData.Create(Rules.EntityShape, EmitUtil.LocationOf(type),
                $"Hot data struct '{type.Name}' must be a non-generic top-level type."));
            return new GenResult(null, null, new EquatableArray<DiagData>(diags.ToArray()), null);
        }

        var members = Collect(type, diags);
        if (members is null)
        {
            return new GenResult(null, null, new EquatableArray<DiagData>(diags.ToArray()), null);
        }

        var tableName = type.Name;
        var hidden = false;
        foreach (var arg in context.Attributes[0].NamedArguments)
        {
            if (arg.Key == "Name" && arg.Value.Value is string n && n.Length > 0)
            {
                tableName = n;
            }
            else if (arg.Key == "Visibility" && arg.Value.Value is int v)
            {
                if (v == 3)
                {
                    hidden = true;
                }
                else if (v != 0)
                {
                    diags.Add(DiagData.Create(Rules.VisibilityShape, EmitUtil.LocationOf(type),
                        $"Hot data '{type.Name}' can be Everyone or Hidden: its rows have no owner or team to match. Use Hidden to keep the table on the authority, or split owner data into entities."));
                }
            }
        }

        var row = Codecs.TypeText(type);
        var cls = type.Name + "Table";
        var s = new Src();
        var ns = EmitUtil.Header(s, type);
        if (ns.Length > 0)
        {
            s.Open("namespace " + ns);
        }

        var access = EmitUtil.Access(type.DeclaredAccessibility == Accessibility.NotApplicable ? Accessibility.Internal : type.DeclaredAccessibility);
        s.Line($"/// <summary>Struct-of-arrays storage for <see cref=\"{type.Name}\"/> rows: one array per field, so loops touch only the columns they use.</summary>");
        s.Open($"{access} sealed class {cls} : global::OneGame.Core.IHotTable");
        foreach (var m in members)
        {
            s.Line($"private {m.Codec.TypeName}[] _{m.Name};");
        }

        s.Line("private int _count;");
        s.Line($"/// <summary>Creates a table with room for <paramref name=\"capacity\"/> rows.</summary>");
        s.Open($"public {cls}(int capacity = 16)");
        s.Line("if (capacity < 1) { capacity = 1; }");
        foreach (var m in members)
        {
            s.Line($"_{m.Name} = new {m.Codec.TypeName}[capacity];");
        }

        s.Close();
        s.Line("/// <inheritdoc />");
        s.Line($"public string Name => {EmitUtil.Literal(tableName)};");
        if (hidden)
        {
            s.Line("/// <inheritdoc />");
            s.Line("public global::OneGame.Core.Visibility Visibility => global::OneGame.Core.Visibility.Hidden;");
        }

        s.Line("/// <inheritdoc />");
        s.Line("public int Count => _count;");
        s.Line("/// <summary>The number of rows the arrays hold before they grow.</summary>");
        s.Line(members.Count == 0 ? "public int Capacity => 0;" : $"public int Capacity => _{members[0].Name}.Length;");
        foreach (var m in members)
        {
            s.Line($"/// <summary>The <c>{m.Name}</c> column, one element per row. Valid until the table grows.</summary>");
            s.Line($"public global::System.Span<{m.Codec.TypeName}> {m.Name} => new(_{m.Name}, 0, _count);");
        }

        s.Line("/// <inheritdoc />");
        s.Open($"void global::OneGame.Core.IHotTable.CopyFrom(global::OneGame.Core.IHotTable source)");
        s.Line($"if (source is not {cls} other) {{ throw new global::System.ArgumentException(\"The source is not a {cls}.\", nameof(source)); }}");
        s.Line("Reserve(other._count);");
        foreach (var m in members)
        {
            s.Line($"global::System.Array.Copy(other._{m.Name}, _{m.Name}, other._count);");
        }

        s.Line("_count = other._count;");
        s.Close();
        s.Line("/// <summary>Makes room for at least <paramref name=\"capacity\"/> rows, so later adds do not allocate.</summary>");
        s.Open("public void Reserve(int capacity)");
        s.Open("if (capacity > Capacity)");
        foreach (var m in members)
        {
            s.Line($"global::System.Array.Resize(ref _{m.Name}, capacity);");
        }

        s.Close();
        s.Close();
        s.Line("/// <summary>Adds a row and returns its index. Grows the arrays when full.</summary>");
        s.Open($"public int Add(in {row} row)");
        s.Line("if (_count == Capacity) { Reserve(checked(Capacity * 2)); }");
        foreach (var m in members)
        {
            s.Line($"_{m.Name}[_count] = row.{m.Name};");
        }

        s.Line("return _count++;");
        s.Close();
        s.Line("/// <summary>Reads a row.</summary>");
        s.Open($"public {row} Get(int index)");
        s.Line("if ((uint)index >= (uint)_count) { throw new global::System.ArgumentOutOfRangeException(nameof(index)); }");
        s.Line($"{row} row = default;");
        foreach (var m in members)
        {
            s.Line($"row.{m.Name} = _{m.Name}[index];");
        }

        s.Line("return row;");
        s.Close();
        s.Line("/// <summary>Overwrites a row.</summary>");
        s.Open($"public void Set(int index, in {row} row)");
        s.Line("if ((uint)index >= (uint)_count) { throw new global::System.ArgumentOutOfRangeException(nameof(index)); }");
        foreach (var m in members)
        {
            s.Line($"_{m.Name}[index] = row.{m.Name};");
        }

        s.Close();
        s.Line("/// <summary>Removes a row by moving the last row into its place. Row order changes deterministically.</summary>");
        s.Open("public void RemoveAtSwap(int index)");
        s.Line("if ((uint)index >= (uint)_count) { throw new global::System.ArgumentOutOfRangeException(nameof(index)); }");
        s.Line("_count--;");
        foreach (var m in members)
        {
            s.Line($"_{m.Name}[index] = _{m.Name}[_count];");
            s.Line($"_{m.Name}[_count] = default!;");
        }

        s.Close();
        s.Line("/// <summary>Removes every row and keeps the arrays.</summary>");
        s.Open("public void Clear()");
        foreach (var m in members)
        {
            s.Line($"global::System.Array.Clear(_{m.Name}, 0, _count);");
        }

        s.Line("_count = 0;");
        s.Close();
        s.Line("/// <inheritdoc />");
        s.Open("public void Write(global::OneGame.Core.SyncWriter w)");
        s.Line("w.WriteVarUInt((ulong)_count);");
        foreach (var m in members)
        {
            s.Line($"for (var i = 0; i < _count; i++) {{ {m.Codec.Write($"_{m.Name}[i]")} }}");
        }

        s.Close();
        // Read keeps the table's arrays: a dry run over a copy of the reader checks every row first (so an error leaves the table
        // untouched), then the rows are decoded in place. Replicas apply a table every tick, so this must not allocate.
        s.Line("/// <inheritdoc />");
        s.Open("public void Read(ref global::OneGame.Core.SyncReader r)");
        s.Line("var n = r.ReadCount(global::OneGame.Core.SyncLimits.MaxTableRows);");
        s.Line("if (!r.Ok) { return; }");
        s.Line("var check = Validate(r, n);");
        s.Line("if (check != global::OneGame.Core.SyncError.None) { r.Fail(check); return; }");
        s.Line("if (n > Capacity) { Reserve(n); }");
        foreach (var m in members)
        {
            s.Line($"for (var i = 0; i < n; i++) {{ _{m.Name}[i] = {m.Codec.Read()}; }}");
            s.Line($"if (_count > n) {{ global::System.Array.Clear(_{m.Name}, n, _count - n); }}");
        }

        s.Line("_count = n;");
        s.Close();
        s.Open("private static global::OneGame.Core.SyncError Validate(global::OneGame.Core.SyncReader r, int n)");
        foreach (var m in members)
        {
            s.Line($"for (var i = 0; i < n && r.Ok; i++) {{ _ = {m.Codec.Read()}; }}");
        }

        s.Line("return r.Error;");
        s.Close();
        s.Line("/// <inheritdoc />");
        s.Open("public void WriteJson(global::OneGame.Core.JsonOut j)");
        s.Line("j.BeginArray();");
        s.Open("for (var i = 0; i < _count; i++)");
        s.Line("j.BeginObject();");
        foreach (var m in members)
        {
            s.Line($"j.Name({EmitUtil.Literal(m.Name)});");
            s.Line(m.Codec.Json($"_{m.Name}[i]"));
        }

        s.Line("j.EndObject();");
        s.Close();
        s.Line("j.EndArray();");
        s.Close();
        // One scalar cell from text, for the harness. Only numbers, bool, string, enum and Fixed columns have a text form.
        s.Line("/// <inheritdoc />");
        s.Open("public bool TrySetFromText(int row, string column, string text)");
        s.Line("if ((uint)row >= (uint)_count) { return false; }");
        s.Open("switch (column)");
        foreach (var m in members)
        {
            var c = m.Codec;
            if (c.Kind == CodecKind.Value && c.TypeName != "global::OneGame.Core.Fixed")
            {
                continue;
            }

            var cell = $"_{m.Name}[row]";
            s.Open($"case {EmitUtil.Literal(m.Name)}:");
            switch (c.Kind)
            {
                case CodecKind.String:
                    s.Line($"{cell} = text; return true;");
                    break;
                case CodecKind.Value:
                    s.Line($"if (!global::OneGame.Core.Fixed.TryParse(text, out var f)) {{ return false; }} {cell} = f; return true;");
                    break;
                default:
                    var typeName = c.Kind == CodecKind.Enum ? c.UnderlyingCs : c.TypeName;
                    if (c.IsBool)
                    {
                        s.Line($"if (!global::OneGame.Core.TextParse.TryBool(text, out var b)) {{ return false; }} {cell} = b; return true;");
                    }
                    else if (c.Unsigned)
                    {
                        s.Line($"if (!global::OneGame.Core.TextParse.TryUnsigned(text, {typeName}.MaxValue, out var u)) {{ return false; }} {cell} = ({c.TypeName})({typeName})u; return true;");
                    }
                    else
                    {
                        s.Line($"if (!global::OneGame.Core.TextParse.TryInteger(text, {typeName}.MinValue, {typeName}.MaxValue, out var n)) {{ return false; }} {cell} = ({c.TypeName})({typeName})n; return true;");
                    }

                    break;
            }

            s.Close();
        }

        s.Line("default: return false;");
        s.Close();
        s.Close();
        s.Close();
        if (ns.Length > 0)
        {
            s.Close();
        }

        return new GenResult(EmitUtil.HintName(type, "Table"), s.ToString(), new EquatableArray<DiagData>(diags.ToArray()), null);
    }
}
