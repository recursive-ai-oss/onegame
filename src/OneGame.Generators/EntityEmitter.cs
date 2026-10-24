using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OneGame.Generators;

/// <summary>What one generator step produced: source text, diagnostics and the entity's registration line. Compared by value so unchanged inputs skip re-emission.</summary>
internal sealed class GenResult : IEquatable<GenResult>
{
    public GenResult(string? hintName, string? source, EquatableArray<DiagData> diagnostics, Registration? registration)
    {
        HintName = hintName;
        Source = source;
        Diagnostics = diagnostics;
        Registration = registration;
    }

    public string? HintName { get; }

    public string? Source { get; }

    public EquatableArray<DiagData> Diagnostics { get; }

    public Registration? Registration { get; }

    public bool Equals(GenResult? other)
        => other is not null && HintName == other.HintName && Source == other.Source && Diagnostics.Equals(other.Diagnostics)
            && Equals(Registration, other.Registration);

    public override bool Equals(object? obj) => Equals(obj as GenResult);

    public override int GetHashCode() => (HintName?.GetHashCode() ?? 0) ^ (Source?.GetHashCode() ?? 0);
}

internal sealed class Registration : IEquatable<Registration>
{
    public Registration(string typeName, string displayName, uint typeId)
    {
        TypeName = typeName;
        DisplayName = displayName;
        TypeId = typeId;
    }

    public string TypeName { get; }

    public string DisplayName { get; }

    public uint TypeId { get; }

    public bool Equals(Registration? other) => other is not null && TypeName == other.TypeName && DisplayName == other.DisplayName && TypeId == other.TypeId;

    public override bool Equals(object? obj) => Equals(obj as Registration);

    public override int GetHashCode() => (int)TypeId;
}

/// <summary>Tiny indenting text builder for emitted C#.</summary>
internal sealed class Src
{
    private readonly StringBuilder _sb = new();
    private int _indent;

    public void Line(string text = "")
    {
        if (text.Length > 0)
        {
            _sb.Append(' ', _indent * 4).Append(text);
        }

        _sb.Append('\n');
    }

    public void Open(string text)
    {
        Line(text);
        Line("{");
        _indent++;
    }

    public void Close(string suffix = "")
    {
        _indent--;
        Line("}" + suffix);
    }

    public override string ToString() => _sb.ToString();
}

internal static class EmitUtil
{
    public static string Access(Accessibility a) => a switch
    {
        Accessibility.Public => "public",
        Accessibility.Private => "private",
        Accessibility.Protected => "protected",
        Accessibility.Internal => "internal",
        Accessibility.ProtectedOrInternal => "protected internal",
        Accessibility.ProtectedAndInternal => "private protected",
        _ => "private",
    };

    public static string HintName(INamedTypeSymbol t, string kind)
    {
        var name = t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", string.Empty);
        var sb = new StringBuilder();
        foreach (var c in name)
        {
            sb.Append(char.IsLetterOrDigit(c) ? c : '_');
        }

        return sb + "." + kind + ".g.cs";
    }

    public static string Namespace(INamedTypeSymbol t) => t.ContainingNamespace.IsGlobalNamespace ? string.Empty : t.ContainingNamespace.ToDisplayString();

    public static string Literal(string s)
    {
        var sb = new StringBuilder("\"");
        foreach (var c in s)
        {
            if (c == '"' || c == '\\')
            {
                sb.Append('\\');
            }

            sb.Append(c);
        }

        return sb.Append('"').ToString();
    }

    /// <summary>The partial declaration keyword for a type: "class", "struct" or "readonly struct" are not needed; partial parts need only the kind.</summary>
    public static string Kind(INamedTypeSymbol t) => t.TypeKind == TypeKind.Struct ? "struct" : "class";

    public static bool IsPartialTopLevel(INamedTypeSymbol t, out string? problem)
    {
        problem = null;
        if (t.ContainingType is not null)
        {
            problem = $"'{t.Name}' is nested inside '{t.ContainingType.Name}'. Move it to the top level of a namespace.";
            return false;
        }

        if (t.IsGenericType)
        {
            problem = $"'{t.Name}' is generic. Generated types cannot be generic.";
            return false;
        }

        foreach (var reference in t.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is TypeDeclarationSyntax decl && !decl.Modifiers.Any(m => m.Text == "partial"))
            {
                problem = $"'{t.Name}' must be declared 'partial' so the generator can add its members.";
                return false;
            }
        }

        return true;
    }

    public static Location LocationOf(ISymbol s) => s.Locations.FirstOrDefault() ?? Location.None;

    public static uint Fnv32(string text)
    {
        var h = 2166136261u;
        foreach (var b in Encoding.UTF8.GetBytes(text))
        {
            h = unchecked((h ^ b) * 16777619u);
        }

        return h;
    }

    public static int SourceOrder(ISymbol a, ISymbol b)
    {
        var la = a.Locations.FirstOrDefault();
        var lb = b.Locations.FirstOrDefault();
        var pa = la?.SourceTree?.FilePath ?? string.Empty;
        var pb = lb?.SourceTree?.FilePath ?? string.Empty;
        var c = string.CompareOrdinal(pa, pb);
        return c != 0 ? c : (la?.SourceSpan.Start ?? 0).CompareTo(lb?.SourceSpan.Start ?? 0);
    }

    public static string Header(Src s, INamedTypeSymbol t)
    {
        s.Line("// <auto-generated/>");
        s.Line("#nullable enable");
        return EmitUtil.Namespace(t);
    }

    public static string Hex(uint v) => "0x" + v.ToString("X8", CultureInfo.InvariantCulture) + "u";
}

internal static class EntityEmitter
{
    private sealed class Prop
    {
        public Prop(IPropertySymbol symbol, int index, Codec? codec, Codec? element, bool isList)
        {
            Symbol = symbol;
            Index = index;
            Codec = codec;
            Element = element;
            IsList = isList;
        }

        public IPropertySymbol Symbol { get; }

        public int Index { get; }

        public Codec? Codec { get; }

        public Codec? Element { get; }

        public bool IsList { get; }

        public string Name => Symbol.Name;

        public string Field => "__p_" + Symbol.Name;

        public string Bit => "(1UL << " + Index.ToString(CultureInfo.InvariantCulture) + ")";

        public bool Serialized => IsList ? Element is not null : Codec is not null;

        public string TypeText => Codecs.TypeTextNullable(Symbol.Type);
    }

    private sealed class Command
    {
        public Command(IMethodSymbol method, List<Codec> codecs)
        {
            Method = method;
            Codecs = codecs;
        }

        public IMethodSymbol Method { get; }

        public List<Codec> Codecs { get; }

        public uint Id => EmitUtil.Fnv32(Method.Name);

        public bool ReturnsBool => Method.ReturnType.SpecialType == SpecialType.System_Boolean;
    }

    public static GenResult Build(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        var diags = new List<DiagData>();
        if (context.TargetSymbol is not INamedTypeSymbol type)
        {
            return new GenResult(null, null, default, null);
        }

        var attribute = context.Attributes[0];
        uint schema = 1;
        uint typeIdOverride = 0;
        foreach (var arg in attribute.NamedArguments)
        {
            if (arg.Key == "Schema" && arg.Value.Value is uint sv)
            {
                schema = sv;
            }
            else if (arg.Key == "Id" && arg.Value.Value is uint tv)
            {
                typeIdOverride = tv;
            }
        }

        var typeLocation = EmitUtil.LocationOf(type);
        var shapeOk = true;
        if (!EmitUtil.IsPartialTopLevel(type, out var problem))
        {
            diags.Add(DiagData.Create(Rules.EntityShape, typeLocation, problem!));
            shapeOk = false;
        }

        if (type.TypeKind != TypeKind.Class || type.BaseType is not { Name: "Entity" } baseType || !Codecs.IsInCore(baseType))
        {
            diags.Add(DiagData.Create(Rules.EntityShape, typeLocation,
                $"Entity '{type.Name}' must derive directly from OneGame.Core.Entity."));
            shapeOk = false;
        }

        if (type.Constructors.Any(c => !c.IsImplicitlyDeclared && c.Parameters.Length > 0) && !type.Constructors.Any(c => c.Parameters.Length == 0 && c.DeclaredAccessibility != Accessibility.Private))
        {
            diags.Add(DiagData.Create(Rules.EntityShape, typeLocation,
                $"Entity '{type.Name}' needs a parameterless constructor (not private) so snapshots can create it."));
            shapeOk = false;
        }

        if (!shapeOk)
        {
            return new GenResult(null, null, new EquatableArray<DiagData>(diags.ToArray()), null);
        }

        // Synced properties, in source order.
        var syncedSymbols = type.GetMembers().OfType<IPropertySymbol>().Where(p => Codecs.HasAttribute(p, "SyncedAttribute")).ToList();
        syncedSymbols.Sort(EmitUtil.SourceOrder);
        var props = new List<Prop>();
        foreach (var p in syncedSymbols)
        {
            ct.ThrowIfCancellationRequested();
            var location = EmitUtil.LocationOf(p);
            var isList = Codecs.IsSyncList(p.Type, out var elementType);
            if (p.IsStatic || p.IsIndexer)
            {
                diags.Add(DiagData.Create(Rules.PropertyShape, location, $"Synced property '{p.Name}' must be an instance property."));
                continue;
            }

            if (isList)
            {
                if (p.GetMethod is null || p.SetMethod is not null)
                {
                    diags.Add(DiagData.Create(Rules.PropertyShape, location,
                        $"Synced list '{p.Name}' must be get-only with an initializer: public SyncList<T> {p.Name} {{ get; }} = new();"));
                    continue;
                }

                props.Add(new Prop(p, props.Count, null, Codecs.TryGet(elementType), true));
                continue;
            }

            if (!p.IsPartialDefinition || p.GetMethod is null || p.SetMethod is null || p.SetMethod.IsInitOnly)
            {
                diags.Add(DiagData.Create(Rules.PropertyShape, location,
                    $"Synced property '{p.Name}' must be declared partial with get and set: public partial {Codecs.TypeText(p.Type)} {p.Name} {{ get; set; }}"));
                continue;
            }

            props.Add(new Prop(p, props.Count, Codecs.TryGet(p.Type), null, false));
        }

        var serialized = props.Where(p => p.Serialized).ToList();
        // Bit positions follow the serialized order.
        for (var i = 0; i < serialized.Count; i++)
        {
            serialized[i] = new Prop(serialized[i].Symbol, i, serialized[i].Codec, serialized[i].Element, serialized[i].IsList);
        }

        var unserialized = props.Where(p => !p.Serialized && !p.IsList).ToList();
        if (serialized.Count > 64)
        {
            diags.Add(DiagData.Create(Rules.TooManyProperties, typeLocation, type.Name, serialized.Count.ToString(CultureInfo.InvariantCulture)));
            return new GenResult(null, null, new EquatableArray<DiagData>(diags.ToArray()), null);
        }

        // Commands.
        var commands = new List<Command>();
        var commandIds = new Dictionary<uint, string>();
        foreach (var m in type.GetMembers().OfType<IMethodSymbol>().Where(m => Codecs.HasAttribute(m, "CommandAttribute")))
        {
            var location = EmitUtil.LocationOf(m);
            var codecs = new List<Codec>();
            var ok = true;
            if (m.IsStatic || m.IsGenericMethod || (m.ReturnType.SpecialType != SpecialType.System_Void && m.ReturnType.SpecialType != SpecialType.System_Boolean))
            {
                diags.Add(DiagData.Create(Rules.CommandShape, location,
                    $"Command '{m.Name}' must be a non-static, non-generic instance method returning void or bool (false rejects the command)."));
                ok = false;
            }

            foreach (var parameter in m.Parameters)
            {
                var codec = parameter.RefKind == RefKind.None ? Codecs.TryGet(parameter.Type) : null;
                if (codec is null)
                {
                    if (!Codecs.IsFloating(parameter.Type))
                    {
                        diags.Add(DiagData.Create(Rules.CommandShape, EmitUtil.LocationOf(parameter),
                            $"Command parameter '{parameter.Name}' of '{m.Name}' has type '{parameter.Type.ToDisplayString()}', which has no codec. Use a number, bool, string, enum, Fixed, FVec2, EntityId or a [SyncedStruct], passed by value."));
                    }

                    ok = false;
                    continue;
                }

                codecs.Add(codec);
            }

            if (commandIds.TryGetValue(EmitUtil.Fnv32(m.Name), out var other))
            {
                diags.Add(DiagData.Create(Rules.CommandShape, location,
                    other == m.Name
                        ? $"Command '{m.Name}' is overloaded. Command names must be unique so their ids are stable; rename one."
                        : $"Commands '{other}' and '{m.Name}' hash to the same id. Rename one."));
                ok = false;
            }

            commandIds[EmitUtil.Fnv32(m.Name)] = m.Name;
            if (ok)
            {
                commands.Add(new Command(m, codecs));
            }
        }

        // Migrations.
        var migrations = new List<(uint From, IMethodSymbol Method)>();
        foreach (var m in type.GetMembers().OfType<IMethodSymbol>())
        {
            if (Codecs.FindAttribute(m, "MigrateAttribute") is not { } migrate)
            {
                continue;
            }

            var from = migrate.ConstructorArguments.Length == 1 && migrate.ConstructorArguments[0].Value is uint v ? v : 0;
            var location = EmitUtil.LocationOf(m);
            if (!m.IsStatic || !m.ReturnsVoid || m.Parameters.Length != 2 || m.Parameters[0].RefKind != RefKind.Ref
                || m.Parameters[0].Type.Name != "SyncReader" || !SymbolEqualityComparer.Default.Equals(m.Parameters[1].Type, type))
            {
                diags.Add(DiagData.Create(Rules.MigrationShape, location,
                    $"Migration '{m.Name}' must be: static void {m.Name}(ref SyncReader old, {type.Name} entity)."));
                continue;
            }

            if (from == 0 || from >= schema)
            {
                diags.Add(DiagData.Create(Rules.MigrationShape, location,
                    $"Migration '{m.Name}' reads version {from}, but the entity's schema is {schema}. A migration must read an older version, and versions start at 1."));
                continue;
            }

            if (migrations.Any(x => x.From == from))
            {
                diags.Add(DiagData.Create(Rules.MigrationShape, location, $"Two migrations read version {from}. Keep one."));
                continue;
            }

            migrations.Add((from, m));
        }

        var typeId = typeIdOverride != 0
            ? typeIdOverride
            : EmitUtil.Fnv32(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", string.Empty));

        var s = new Src();
        var ns = EmitUtil.Header(s, type);
        if (ns.Length > 0)
        {
            s.Open("namespace " + ns);
        }

        s.Open($"{EmitUtil.Access(type.DeclaredAccessibility == Accessibility.NotApplicable ? Accessibility.Internal : type.DeclaredAccessibility)} partial class {type.Name}");
        EmitMembers(s, type, serialized, unserialized, props, commands, migrations, schema, typeId);
        s.Close();
        if (ns.Length > 0)
        {
            s.Close();
        }

        var registration = new Registration(Codecs.TypeText(type), type.Name, typeId);
        return new GenResult(EmitUtil.HintName(type, "Entity"), s.ToString(), new EquatableArray<DiagData>(diags.ToArray()), registration);
    }

    private static string ReadLocal(Prop p, string r = "r")
    {
        var name = "v" + p.Index.ToString(CultureInfo.InvariantCulture);
        var idx = p.Index.ToString(CultureInfo.InvariantCulture);
        if (!p.IsList)
        {
            return $"var {name} = {p.Codec!.Read(r)};";
        }

        return $"var n{idx} = {r}.ReadCount(global::OneGame.Core.SyncLimits.MaxListCount); var {name} = new {p.Element!.TypeName}[n{idx}]; "
            + $"for (var i{idx} = 0; i{idx} < n{idx} && {r}.Ok; i{idx}++) {{ {name}[i{idx}] = {p.Element.Read(r)}; }}";
    }

    private static void EmitWrite(Src s, Prop p)
    {
        var idx = p.Index.ToString(CultureInfo.InvariantCulture);
        if (!p.IsList)
        {
            s.Line(p.Codec!.Write(p.Field));
            return;
        }

        s.Open("");
        s.Line($"var l{idx} = {p.Name};");
        s.Line($"w.WriteCount(l{idx}.Count, global::OneGame.Core.SyncLimits.MaxListCount);");
        s.Line($"for (var i{idx} = 0; i{idx} < l{idx}.Count; i{idx}++) {{ {p.Element!.Write($"l{idx}[i{idx}]")} }}");
        s.Close();
    }

    private static void EmitMembers(Src s, INamedTypeSymbol type, List<Prop> serialized, List<Prop> unserialized, List<Prop> allProps,
        List<Command> commands, List<(uint From, IMethodSymbol Method)> migrations, uint schema, uint typeId)
    {
        var all = allProps.Where(p => !p.Serialized && !p.IsList).Concat(serialized).ToList();

        s.Line("private ulong __dirty;");

        // Property implementations (and backing fields).
        foreach (var p in serialized.Concat(unserialized))
        {
            if (p.IsList)
            {
                continue;
            }

            var sym = p.Symbol;
            var init = sym.Type.SpecialType == SpecialType.System_String ? " = string.Empty" : string.Empty;
            s.Line($"private {p.TypeText} {p.Field}{init};");
            var getAcc = sym.GetMethod!.DeclaredAccessibility != sym.DeclaredAccessibility ? EmitUtil.Access(sym.GetMethod.DeclaredAccessibility) + " " : string.Empty;
            var setAcc = sym.SetMethod!.DeclaredAccessibility != sym.DeclaredAccessibility ? EmitUtil.Access(sym.SetMethod.DeclaredAccessibility) + " " : string.Empty;
            s.Open($"{EmitUtil.Access(sym.DeclaredAccessibility)} partial {p.TypeText} {sym.Name}");
            s.Line($"{getAcc}get => {p.Field};");
            s.Open($"{setAcc}set");
            s.Line("AssertWritable();");
            if (sym.Type.SpecialType == SpecialType.System_String)
            {
                s.Line("value ??= string.Empty;");
            }

            s.Open($"if (!global::System.Collections.Generic.EqualityComparer<{p.TypeText}>.Default.Equals({p.Field}, value))");
            s.Line($"{p.Field} = value;");
            if (p.Serialized)
            {
                s.Line($"__dirty |= {p.Bit};");
            }

            s.Close();
            s.Close();
            s.Close();
        }

        s.Line();
        s.Line($"/// <inheritdoc />");
        s.Line($"public override uint TypeId => {EmitUtil.Hex(typeId)};");
        s.Line($"/// <inheritdoc />");
        s.Line($"public override string TypeName => {EmitUtil.Literal(type.Name)};");
        s.Line($"/// <inheritdoc />");
        s.Line($"public override uint SchemaVersion => {schema.ToString(CultureInfo.InvariantCulture)}u;");
        s.Line($"/// <inheritdoc />");
        s.Line($"public override int PropertyCount => {serialized.Count.ToString(CultureInfo.InvariantCulture)};");
        s.Line("/// <inheritdoc />");
        s.Open("public override string PropertyName(int index)");
        s.Open("switch (index)");
        foreach (var p in serialized)
        {
            s.Line($"case {p.Index.ToString(CultureInfo.InvariantCulture)}: return {EmitUtil.Literal(p.Name)};");
        }

        s.Line("default: throw new global::System.ArgumentOutOfRangeException(nameof(index));");
        s.Close();
        s.Close();

        var dirtyExpr = new StringBuilder("__dirty");
        foreach (var p in serialized.Where(p => p.IsList))
        {
            dirtyExpr.Append($" | ({p.Name}.IsDirty ? {p.Bit} : 0UL)");
        }

        s.Line("/// <inheritdoc />");
        s.Line($"public override ulong DirtyMask => {dirtyExpr};");
        s.Line("/// <inheritdoc />");
        s.Open("public override void ClearDirty()");
        s.Line("__dirty = 0;");
        foreach (var p in serialized.Where(p => p.IsList))
        {
            s.Line($"{p.Name}.ClearDirty();");
        }

        s.Close();

        var valid = serialized.Count == 0 ? "0UL" : serialized.Count == 64 ? "ulong.MaxValue" : $"((1UL << {serialized.Count.ToString(CultureInfo.InvariantCulture)}) - 1)";

        // Full state.
        s.Line("/// <inheritdoc />");
        s.Open("public override void WriteState(global::OneGame.Core.SyncWriter w)");
        foreach (var p in serialized)
        {
            EmitWrite(s, p);
        }

        s.Close();

        s.Line("/// <inheritdoc />");
        s.Open("public override void ReadState(ref global::OneGame.Core.SyncReader r)");
        foreach (var p in serialized)
        {
            s.Line(ReadLocal(p));
        }

        s.Line("if (!r.Ok) { return; }");
        foreach (var p in serialized)
        {
            s.Line(p.IsList ? $"{p.Name}.ReplaceWith(v{p.Index.ToString(CultureInfo.InvariantCulture)});" : $"{p.Field} = v{p.Index.ToString(CultureInfo.InvariantCulture)};");
        }

        s.Line("ClearDirty();");
        s.Close();

        s.Line("/// <inheritdoc />");
        s.Open("public override void ReadState(uint schemaVersion, ref global::OneGame.Core.SyncReader r)");
        s.Open($"if (schemaVersion == {schema.ToString(CultureInfo.InvariantCulture)}u)");
        s.Line("ReadState(ref r);");
        s.Line("return;");
        s.Close();
        s.Open("switch (schemaVersion)");
        foreach (var (from, method) in migrations.OrderBy(m => m.From))
        {
            s.Open($"case {from.ToString(CultureInfo.InvariantCulture)}u:");
            s.Line("BeginRestore();");
            s.Open("try");
            s.Line($"{method.Name}(ref r, this);");
            s.Close();
            s.Open("finally");
            s.Line("EndRestore();");
            s.Close();
            s.Line("ClearDirty();");
            s.Line("return;");
            s.Close();
        }

        s.Line("default: r.Fail(global::OneGame.Core.SyncError.UnsupportedVersion); return;");
        s.Close();
        s.Close();

        // Deltas.
        s.Open("private void __WriteDelta(global::OneGame.Core.SyncWriter w, ulong mask)");
        s.Line("w.WriteVarUInt(mask);");
        foreach (var p in serialized)
        {
            s.Open($"if ((mask & {p.Bit}) != 0)");
            EmitWrite(s, p);
            s.Close();
        }

        s.Close();

        s.Line("/// <inheritdoc />");
        s.Line("public override void WriteDirtyDelta(global::OneGame.Core.SyncWriter w) => __WriteDelta(w, DirtyMask);");

        s.Line("/// <inheritdoc />");
        s.Open("public override bool WriteDeltaFrom(global::OneGame.Core.Entity baseline, global::OneGame.Core.SyncWriter w)");
        s.Line($"if (baseline is not {type.Name} b) {{ return false; }}");
        s.Line("ulong mask = 0;");
        foreach (var p in serialized)
        {
            var differs = p.IsList ? $"!{p.Name}.ContentEquals(b.{p.Name})" : $"!{p.Codec!.Equal(p.Field, "b." + p.Field)}";
            s.Line($"if ({differs}) {{ mask |= {p.Bit}; }}");
        }

        s.Line("__WriteDelta(w, mask);");
        s.Line("return true;");
        s.Close();

        s.Line("/// <inheritdoc />");
        s.Open("public override void ApplyDelta(ref global::OneGame.Core.SyncReader r)");
        s.Line("var mask = r.ReadVarUInt();");
        s.Line("if (!r.Ok) { return; }");
        s.Line($"if ((mask & ~{valid}) != 0) {{ r.Fail(global::OneGame.Core.SyncError.InvalidValue); return; }}");
        foreach (var p in serialized)
        {
            var idx = p.Index.ToString(CultureInfo.InvariantCulture);
            var elementOrValue = p.IsList ? p.Element!.TypeName + "[]" : p.Codec!.TypeName;
            s.Line($"{elementOrValue} v{idx} = default!;");
            s.Open($"if ((mask & {p.Bit}) != 0)");
            s.Line(ReadLocal(p).Replace($"var v{idx} =", $"v{idx} =").Replace($"var v{idx} = new", $"v{idx} = new"));
            s.Close();
        }

        s.Line("if (!r.Ok) { return; }");
        foreach (var p in serialized)
        {
            var idx = p.Index.ToString(CultureInfo.InvariantCulture);
            s.Open($"if ((mask & {p.Bit}) != 0)");
            s.Line(p.IsList ? $"{p.Name}.ReplaceWith(v{idx});" : $"{p.Field} = v{idx};");
            s.Line($"__dirty |= {p.Bit};");
            s.Close();
        }

        s.Close();

        // JSON and text patch.
        s.Line("/// <inheritdoc />");
        s.Open("public override void WriteJson(global::OneGame.Core.JsonOut j)");
        s.Line("j.BeginObject();");
        s.Line("j.Name(\"id\"); j.Value(Id.Value);");
        s.Line($"j.Name(\"type\"); j.Value({EmitUtil.Literal(type.Name)});");
        foreach (var p in serialized)
        {
            var idx = p.Index.ToString(CultureInfo.InvariantCulture);
            s.Line($"j.Name({EmitUtil.Literal(p.Name)});");
            if (p.IsList)
            {
                s.Open("");
                s.Line($"var l{idx} = {p.Name};");
                s.Line("j.BeginArray();");
                s.Line($"for (var i{idx} = 0; i{idx} < l{idx}.Count; i{idx}++) {{ {p.Element!.Json($"l{idx}[i{idx}]")} }}");
                s.Line("j.EndArray();");
                s.Close();
            }
            else
            {
                s.Line(p.Codec!.Json(p.Field));
            }
        }

        s.Line("j.EndObject();");
        s.Close();

        s.Line("/// <inheritdoc />");
        s.Open("public override bool TrySetFromText(string propertyName, string text)");
        s.Open("switch (propertyName)");
        foreach (var p in serialized.Where(p => !p.IsList))
        {
            var c = p.Codec!;
            if (c.Kind == CodecKind.Value && c.TypeName != "global::OneGame.Core.Fixed")
            {
                continue;
            }

            s.Open($"case {EmitUtil.Literal(p.Name)}:");
            switch (c.Kind)
            {
                case CodecKind.String:
                    s.Line($"{p.Name} = text; return true;");
                    break;
                case CodecKind.Value:
                    s.Line($"if (!global::OneGame.Core.Fixed.TryParse(text, out var f)) {{ return false; }} {p.Name} = f; return true;");
                    break;
                default:
                    var typeName = c.Kind == CodecKind.Enum ? c.UnderlyingCs : c.TypeName;
                    if (c.IsBool)
                    {
                        s.Line($"if (!global::OneGame.Core.TextParse.TryBool(text, out var b)) {{ return false; }} {p.Name} = b; return true;");
                    }
                    else if (c.Unsigned)
                    {
                        s.Line($"if (!global::OneGame.Core.TextParse.TryUnsigned(text, {typeName}.MaxValue, out var u)) {{ return false; }} {p.Name} = ({p.TypeText})({typeName})u; return true;");
                    }
                    else
                    {
                        s.Line($"if (!global::OneGame.Core.TextParse.TryInteger(text, {typeName}.MinValue, {typeName}.MaxValue, out var n)) {{ return false; }} {p.Name} = ({p.TypeText})({typeName})n; return true;");
                    }

                    break;
            }

            s.Close();
        }

        s.Line("default: return false;");
        s.Close();
        s.Close();

        // Commands.
        s.Line("/// <inheritdoc />");
        s.Open("protected override global::OneGame.Core.CommandStatus ExecuteCommand(uint commandId, ref global::OneGame.Core.SyncReader r)");
        s.Open("switch (commandId)");
        foreach (var c in commands)
        {
            s.Open($"case {EmitUtil.Hex(c.Id)}:");
            var names = new List<string>();
            for (var i = 0; i < c.Codecs.Count; i++)
            {
                s.Line($"var a{i.ToString(CultureInfo.InvariantCulture)} = {c.Codecs[i].Read()};");
                names.Add("a" + i.ToString(CultureInfo.InvariantCulture));
            }

            s.Line("r.ExpectEnd();");
            s.Line("if (!r.Ok) { return global::OneGame.Core.CommandStatus.Malformed; }");
            var call = $"{c.Method.Name}({string.Join(", ", names)})";
            if (c.ReturnsBool)
            {
                s.Line($"return {call} ? global::OneGame.Core.CommandStatus.Executed : global::OneGame.Core.CommandStatus.Rejected;");
            }
            else
            {
                s.Line($"{call};");
                s.Line("return global::OneGame.Core.CommandStatus.Executed;");
            }

            s.Close();
        }

        s.Line("default: return global::OneGame.Core.CommandStatus.UnknownCommand;");
        s.Close();
        s.Close();

        s.Line("/// <summary>Sends commands to this entity. Each call encodes the arguments and queues the command on the authority.</summary>");
        s.Open("public readonly struct Commands");
        s.Line($"private readonly {type.Name} _e;");
        s.Line($"internal Commands({type.Name} e) => _e = e;");
        foreach (var c in commands)
        {
            var parameters = string.Join(", ", c.Method.Parameters.Select(p => $"{Codecs.TypeText(p.Type)} {p.Name}"));
            s.Line($"/// <summary>Sends <c>{c.Method.Name}</c> to the authority; the handler runs there on the next tick.</summary>");
            s.Open($"public global::OneGame.Core.CommandStatus {c.Method.Name}({parameters})");
            s.Line("var w = _e.BeginCommand();");
            for (var i = 0; i < c.Codecs.Count; i++)
            {
                s.Line(c.Codecs[i].Write(c.Method.Parameters[i].Name));
            }

            s.Line($"return _e.SendCommand({EmitUtil.Hex(c.Id)}, w);");
            s.Close();
        }

        s.Close();
        s.Line("/// <summary>Sends commands to this entity; views and tests use this instead of calling the handlers.</summary>");
        s.Line("public Commands Send => new(this);");

        // Read-only surface.
        s.Line("/// <summary>A read-only window onto the synced state, for views.</summary>");
        s.Open("public readonly struct ReadOnlyView");
        s.Line($"private readonly {type.Name} _e;");
        s.Line($"internal ReadOnlyView({type.Name} e) => _e = e;");
        s.Line("/// <summary>The entity's id.</summary>");
        s.Line("public global::OneGame.Core.EntityId Id => _e.Id;");
        foreach (var p in serialized.Concat(unserialized))
        {
            var t = p.IsList ? $"global::System.Collections.Generic.IReadOnlyList<{p.Element!.TypeName}>" : p.TypeText;
            s.Line($"/// <summary>The synced value of <c>{p.Name}</c>.</summary>");
            s.Line($"public {t} {p.Name} => _e.{p.Name};");
        }

        s.Close();
        s.Line("/// <summary>The read-only view of this entity's synced state.</summary>");
        s.Line("public ReadOnlyView AsReadOnly => new(this);");
    }
}
