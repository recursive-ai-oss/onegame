using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;

namespace OneGame.ApiDocs;

/// <summary>The custom attributes of one metadata entity, read by attribute type name.</summary>
readonly struct AttrSet(MetadataReader reader, CustomAttributeHandleCollection handles)
{
    string? NameOf(CustomAttribute a)
    {
        EntityHandle parent;
        if (a.Constructor.Kind == HandleKind.MemberReference) parent = reader.GetMemberReference((MemberReferenceHandle)a.Constructor).Parent;
        else if (a.Constructor.Kind == HandleKind.MethodDefinition) parent = reader.GetMethodDefinition((MethodDefinitionHandle)a.Constructor).GetDeclaringType();
        else return null;
        if (parent.Kind == HandleKind.TypeReference)
        {
            TypeReference tr = reader.GetTypeReference((TypeReferenceHandle)parent);
            return reader.GetString(tr.Namespace) + "." + reader.GetString(tr.Name);
        }
        if (parent.Kind == HandleKind.TypeDefinition)
        {
            TypeDefinition td = reader.GetTypeDefinition((TypeDefinitionHandle)parent);
            return reader.GetString(td.Namespace) + "." + reader.GetString(td.Name);
        }
        return null;
    }

    public IEnumerable<(string Name, CustomAttribute Attribute)> All()
    {
        if (reader is null) yield break;
        foreach (CustomAttributeHandle h in handles)
        {
            CustomAttribute a = reader.GetCustomAttribute(h);
            string? n = NameOf(a);
            if (n is not null) yield return (n, a);
        }
    }

    public bool Has(string name) => All().Any(a => a.Name == name);

    BlobReader? Blob(string name)
    {
        foreach ((string n, CustomAttribute a) in All())
        {
            if (n != name) continue;
            BlobReader b = reader.GetBlobReader(a.Value);
            if (b.Length >= 2 && b.ReadUInt16() == 1) return b;
        }
        return null;
    }

    public byte? NullableContext => Blob("System.Runtime.CompilerServices.NullableContextAttribute") is { } b ? b.ReadByte() : null;

    public byte[]? Nullable
    {
        get
        {
            if (Blob("System.Runtime.CompilerServices.NullableAttribute") is not { } b) return null;
            if (b.RemainingBytes <= 3) return [b.ReadByte()];
            int n = b.ReadInt32();
            if (n < 0 || n > 4096) return null;
            var r = new byte[n];
            for (int i = 0; i < n; i++) r[i] = b.ReadByte();
            return r;
        }
    }

    public string?[]? TupleNames
    {
        get
        {
            if (Blob("System.Runtime.CompilerServices.TupleElementNamesAttribute") is not { } b) return null;
            int n = b.ReadInt32();
            if (n < 0 || n > 4096) return null;
            var r = new string?[n];
            for (int i = 0; i < n; i++) r[i] = b.ReadSerializedString();
            return r;
        }
    }

    /// <summary>The obsolete message (empty when there is none), or null when the member is not obsolete.</summary>
    public string? Obsolete
    {
        get
        {
            if (Blob("System.ObsoleteAttribute") is not { } b) return null;
            return b.RemainingBytes > 2 ? b.ReadSerializedString() ?? "" : "";
        }
    }

    public int? AttributeUsageTargets => Blob("System.AttributeUsageAttribute") is { } b ? b.ReadInt32() : null;

    /// <summary>Attributes defined by onegame that were applied without arguments, such as a marker on an entity.</summary>
    public IEnumerable<string> OwnMarkers()
    {
        foreach ((string n, CustomAttribute a) in All())
        {
            if (!n.StartsWith("OneGame.", StringComparison.Ordinal)) continue;
            if (reader.GetBlobReader(a.Value).Length != 4) continue;
            string s = n[(n.LastIndexOf('.') + 1)..];
            yield return s.EndsWith("Attribute", StringComparison.Ordinal) ? s[..^9] : s;
        }
    }
}

/// <summary>Reads the public surface of one assembly with System.Reflection.Metadata. Nothing is loaded or run.</summary>
sealed class AssemblyReader
{
    readonly MetadataReader _r;
    readonly AssemblyInfo _asm;

    AssemblyReader(MetadataReader reader, AssemblyInfo asm)
    {
        _r = reader;
        _asm = asm;
    }

    public static void Read(string dllPath, AssemblyInfo asm)
    {
        using FileStream fs = File.OpenRead(dllPath);
        using var pe = new PEReader(fs);
        MetadataReader r = pe.GetMetadataReader();
        var me = new AssemblyReader(r, asm);
        foreach (TypeDefinitionHandle h in r.TypeDefinitions)
        {
            TypeDefinition td = r.GetTypeDefinition(h);
            if (!td.GetDeclaringType().IsNil) continue;
            if ((td.Attributes & TypeAttributes.VisibilityMask) != TypeAttributes.Public) continue;
            TypeInfo? t = me.ReadType(h, null, 0);
            if (t is not null) asm.Types.Add(t);
        }
    }

    static bool Visible(MethodAttributes a) => (a & MethodAttributes.MemberAccessMask) is MethodAttributes.Public or MethodAttributes.Family or MethodAttributes.FamORAssem;

    static int Rank(MethodAttributes a) => (a & MethodAttributes.MemberAccessMask) switch
    {
        MethodAttributes.Public => 3,
        MethodAttributes.FamORAssem => 2,
        MethodAttributes.Family => 1,
        _ => 0,
    };

    static string Access(MethodAttributes a) => (a & MethodAttributes.MemberAccessMask) switch
    {
        MethodAttributes.Public => "public",
        MethodAttributes.FamORAssem => "protected internal",
        _ => "protected",
    };

    AttrSet Attrs(CustomAttributeHandleCollection c) => new(_r, c);

    string Str(StringHandle h) => _r.GetString(h);

    static string[] GenericNames(MetadataReader r, GenericParameterHandleCollection c) =>
        [.. c.Select(r.GetGenericParameter).OrderBy(g => g.Index).Select(g => r.GetString(g.Name))];

    TypeInfo? ReadType(TypeDefinitionHandle handle, TypeInfo? outer, byte outerContext)
    {
        TypeDefinition td = _r.GetTypeDefinition(handle);
        TypeAttributes ta = td.Attributes;
        string name = Str(td.Name);
        if (name.Contains('<') || Attrs(td.GetCustomAttributes()).Has("System.Runtime.CompilerServices.CompilerGeneratedAttribute")) return null;
        if (outer is not null && (ta & TypeAttributes.VisibilityMask) is not (TypeAttributes.NestedPublic or TypeAttributes.NestedFamily or TypeAttributes.NestedFamORAssem)) return null;
        AttrSet attrs = Attrs(td.GetCustomAttributes());
        if (attrs.Has("System.ComponentModel.EditorBrowsableAttribute")) return null;

        string[] allParams = GenericNames(_r, td.GetGenericParameters());
        int outerCount = outer is null ? 0 : outer.OwnTypeParams.Length + OuterParamCount(outer);
        string[] own = allParams[Math.Min(outerCount, allParams.Length)..];
        var ctx = new GenCtx(allParams, []);
        byte context = attrs.NullableContext ?? outerContext;

        string[] names = outer is null ? [name] : [.. outer.Names, name];
        string ns = outer is null ? Str(td.Namespace) : outer.Namespace;
        string key = ns.Length == 0 ? string.Join('.', names) : ns + "." + string.Join('.', names);

        TypeSig? baseSig = td.BaseType.IsNil ? null : SigProvider.FromHandle(_r, td.BaseType, ctx);
        string? baseKey = (baseSig as NamedSig)?.Key;
        TypeKind kind;
        if ((ta & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface) kind = TypeKind.Interface;
        else if (baseKey == "System.Enum") kind = TypeKind.Enum;
        else if (baseKey == "System.ValueType") kind = TypeKind.Struct;
        else if (baseKey == "System.MulticastDelegate") kind = TypeKind.Delegate;
        else kind = TypeKind.Class;

        var interfaces = new List<TypeSig>();
        foreach (InterfaceImplementationHandle ih in td.GetInterfaceImplementations())
            interfaces.Add(SigProvider.FromHandle(_r, _r.GetInterfaceImplementation(ih).Interface, ctx));

        bool isRecord = kind is TypeKind.Class or TypeKind.Struct && td.GetMethods().Any(m => Str(_r.GetMethodDefinition(m).Name) == "<Clone>$");

        int? usage = attrs.AttributeUsageTargets;
        var info = new TypeInfo
        {
            Assembly = _asm,
            Namespace = ns,
            Names = names,
            Kind = kind,
            DocId = "T:" + key,
            Declaration = "",
            DisplayName = TypeText.DeclName(names, own),
            OwnTypeParams = own,
            Outer = outer,
            Base = baseSig,
            Interfaces = interfaces,
            Obsolete = attrs.Obsolete,
            AttributeTargets = usage is { } u ? TargetsText(u) : null,
            IsRecord = isRecord,
        };
        info = WithDeclaration(info, td, attrs, ctx, allParams, outerCount);

        ReadMembers(info, td, ctx, context);

        foreach (TypeDefinitionHandle nh in td.GetNestedTypes())
        {
            TypeInfo? n = ReadType(nh, info, context);
            if (n is not null) info.Nested.Add(n);
        }
        return info;
    }

    static int OuterParamCount(TypeInfo t) => t.Outer is null ? 0 : t.Outer.OwnTypeParams.Length + OuterParamCount(t.Outer);

    static string TargetsText(int v)
    {
        if (v == 32767) return "All";
        string[] names = ["Assembly", "Module", "Class", "Struct", "Enum", "Constructor", "Method", "Property", "Field", "Event", "Interface", "Parameter", "Delegate", "ReturnValue", "GenericParameter"];
        var l = new List<string>();
        for (int i = 0; i < names.Length; i++) if ((v & (1 << i)) != 0) l.Add(names[i]);
        return string.Join(", ", l);
    }

    TypeInfo WithDeclaration(TypeInfo t, TypeDefinition td, AttrSet attrs, GenCtx ctx, string[] allParams, int outerCount)
    {
        var sb = new StringBuilder();
        foreach (string m in attrs.OwnMarkers()) sb.Append('[').Append(m).Append("]\n");
        if (attrs.Has("System.FlagsAttribute")) sb.Append("[Flags]\n");
        TypeAttributes ta = td.Attributes;
        bool nested = t.Outer is not null;
        sb.Append((ta & TypeAttributes.VisibilityMask) switch
        {
            TypeAttributes.Public or TypeAttributes.NestedPublic => "public ",
            TypeAttributes.NestedFamORAssem => "protected internal ",
            _ => nested ? "protected " : "public ",
        });
        switch (t.Kind)
        {
            case TypeKind.Interface:
                sb.Append("interface ");
                break;
            case TypeKind.Enum:
                sb.Append("enum ");
                break;
            case TypeKind.Delegate:
                sb.Append("delegate ");
                break;
            case TypeKind.Struct:
                if (attrs.Has("System.Runtime.CompilerServices.IsReadOnlyAttribute")) sb.Append("readonly ");
                if (attrs.Has("System.Runtime.CompilerServices.IsByRefLikeAttribute")) sb.Append("ref ");
                sb.Append(t.IsRecord ? "record struct " : "struct ");
                break;
            default:
                if ((ta & TypeAttributes.Abstract) != 0 && (ta & TypeAttributes.Sealed) != 0) sb.Append("static ");
                else if ((ta & TypeAttributes.Abstract) != 0) sb.Append("abstract ");
                else if ((ta & TypeAttributes.Sealed) != 0) sb.Append("sealed ");
                sb.Append(t.IsRecord ? "record " : "class ");
                break;
        }

        if (t.Kind == TypeKind.Delegate)
        {
            MethodDefinition invoke = td.GetMethods().Select(_r.GetMethodDefinition).First(m => Str(m.Name) == "Invoke");
            byte ctxByte = Attrs(td.GetCustomAttributes()).NullableContext ?? 0;
            MemberBuild b = BuildMethod(invoke, t, ctx, ctxByte, "Invoke");
            sb.Append(b.Return).Append(' ').Append(t.DisplayName).Append('(').Append(string.Join(", ", b.Params.Select(ParamText))).Append(')');
            sb.Append(Constraints(td.GetGenericParameters(), ctx, outerCount));
            sb.Append(';');
            return Replace(t, sb.ToString());
        }

        sb.Append(t.DisplayName);
        var bases = new List<string>();
        if (t.Kind == TypeKind.Enum)
        {
            FieldDefinition value = td.GetFields().Select(_r.GetFieldDefinition).First(f => Str(f.Name) == "value__");
            TypeSig under = value.DecodeSignature(SigProvider.Instance, ctx);
            bases.Add(TypeText.Plain(new TypeUse(under, null, 0, null)));
        }
        else
        {
            if (t.Kind == TypeKind.Class && t.Base is NamedSig bn && bn.Key != "System.Object") bases.Add(TypeText.Plain(new TypeUse(t.Base, null, 0, null)));
            else if (t.Kind == TypeKind.Class && t.Base is GenericSig) bases.Add(TypeText.Plain(new TypeUse(t.Base!, null, 0, null)));
            foreach (TypeSig i in t.Interfaces) bases.Add(TypeText.Plain(new TypeUse(i, null, 0, null)));
        }
        if (bases.Count > 0) sb.Append(" : ").Append(string.Join(", ", bases));
        sb.Append(Constraints(td.GetGenericParameters(), ctx, outerCount));
        return Replace(t, sb.ToString());
    }

    // TypeInfo is built once with a placeholder declaration; the declaration needs the names, so copy it over.
    static TypeInfo Replace(TypeInfo t, string declaration)
    {
        var n = new TypeInfo
        {
            Assembly = t.Assembly,
            Namespace = t.Namespace,
            Names = t.Names,
            Kind = t.Kind,
            DocId = t.DocId,
            Declaration = declaration,
            DisplayName = t.DisplayName,
            OwnTypeParams = t.OwnTypeParams,
            Outer = t.Outer,
            Base = t.Base,
            Interfaces = t.Interfaces,
            Obsolete = t.Obsolete,
            AttributeTargets = t.AttributeTargets,
            IsRecord = t.IsRecord,
        };
        return n;
    }

    string Constraints(GenericParameterHandleCollection handles, GenCtx ctx, int skip)
    {
        var sb = new StringBuilder();
        GenericParameter[] ps = [.. handles.Select(_r.GetGenericParameter).OrderBy(g => g.Index)];
        for (int i = skip; i < ps.Length; i++)
        {
            GenericParameter g = ps[i];
            var parts = new List<string>();
            GenericParameterAttributes a = g.Attributes;
            if ((a & GenericParameterAttributes.ReferenceTypeConstraint) != 0) parts.Add("class");
            if ((a & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
                parts.Add(Attrs(g.GetCustomAttributes()).Has("System.Runtime.CompilerServices.IsUnmanagedAttribute") ? "unmanaged" : "struct");
            foreach (GenericParameterConstraintHandle ch in g.GetConstraints())
            {
                TypeSig c = SigProvider.FromHandle(_r, _r.GetGenericParameterConstraint(ch).Type, ctx);
                if (c is NamedSig { Key: "System.ValueType" }) continue;
                parts.Add(TypeText.Plain(new TypeUse(c, null, 0, null)));
            }
            if ((a & GenericParameterAttributes.DefaultConstructorConstraint) != 0 && (a & GenericParameterAttributes.NotNullableValueTypeConstraint) == 0) parts.Add("new()");
            if (parts.Count > 0) sb.Append(" where ").Append(Str(g.Name)).Append(" : ").Append(string.Join(", ", parts));
        }
        return sb.ToString();
    }

    sealed class MemberBuild
    {
        public string Return = "";
        public TypeUse? ReturnUse;
        public List<ParamInfo> Params = [];
        public string[] TypeParams = [];
        public string Constraints = "";
        public MethodSignature<TypeSig> Sig;
    }

    static string ParamText(ParamInfo p)
    {
        string m = p.Modifier.Length > 0 ? p.Modifier + " " : "";
        string d = p.Default is null ? "" : " = " + p.Default;
        return m + TypeText.Plain(p.Type) + " " + p.Name + d;
    }

    static (bool ByRef, bool IsIn, TypeSig Inner) Peel(TypeSig s)
    {
        bool byRef = false, isIn = false;
        while (true)
        {
            if (s is ModSig m)
            {
                if (m.Modifier.EndsWith("InAttribute", StringComparison.Ordinal)) isIn = true;
                s = m.Inner;
            }
            else if (s is ByRefSig b)
            {
                byRef = true;
                s = b.Elem;
            }
            else break;
        }
        return (byRef, isIn, s);
    }

    MemberBuild BuildMethod(MethodDefinition md, TypeInfo owner, GenCtx typeCtx, byte typeContext, string name)
    {
        AttrSet mattrs = Attrs(md.GetCustomAttributes());
        string[] mparams = GenericNames(_r, md.GetGenericParameters());
        var ctx = new GenCtx(typeCtx.TypeParams, mparams);
        MethodSignature<TypeSig> sig = md.DecodeSignature(SigProvider.Instance, ctx);
        byte context = mattrs.NullableContext ?? typeContext;
        var b = new MemberBuild { TypeParams = mparams, Sig = sig };

        ParameterHandle? retHandle = null;
        var byNumber = new Dictionary<int, ParameterHandle>();
        foreach (ParameterHandle ph in md.GetParameters())
        {
            int seq = _r.GetParameter(ph).SequenceNumber;
            if (seq == 0) retHandle = ph;
            else byNumber[seq] = ph;
        }

        bool isExtension = mattrs.Has("System.Runtime.CompilerServices.ExtensionAttribute") && (md.Attributes & MethodAttributes.Static) != 0;
        for (int i = 0; i < sig.ParameterTypes.Length; i++)
        {
            bool has = byNumber.TryGetValue(i + 1, out ParameterHandle handle);
            Parameter p = has ? _r.GetParameter(handle) : default;
            AttrSet pa = has ? Attrs(p.GetCustomAttributes()) : default;
            (bool byRef, bool isIn, TypeSig inner) = Peel(sig.ParameterTypes[i]);
            string mod = "";
            if (byRef)
            {
                if (has && (p.Attributes & ParameterAttributes.Out) != 0 && !isIn) mod = "out";
                else if (isIn || (has && pa.Has("System.Runtime.CompilerServices.IsReadOnlyAttribute"))) mod = "in";
                else mod = "ref";
            }
            else if (has && pa.Has("System.ParamArrayAttribute")) mod = "params";
            else if (i == 0 && isExtension) mod = "this";

            string? def = null;
            if (has && (p.Attributes & ParameterAttributes.HasDefault) != 0)
            {
                Constant c = _r.GetConstant(p.GetDefaultValue());
                def = c.TypeCode == ConstantTypeCode.NullReference ? (inner is NamedSig { IsValueType: true } or GenericSig { Def.IsValueType: true } ? "default" : "null") : FormatConstant(c);
            }
            else if (has && (p.Attributes & ParameterAttributes.Optional) != 0) def = "default";

            b.Params.Add(new ParamInfo
            {
                Name = has ? Str(p.Name) : "arg" + (i + 1),
                Type = new TypeUse(inner, pa.Nullable, pa.NullableContext ?? context, pa.TupleNames),
                Modifier = mod,
                Default = def,
            });
        }

        AttrSet ra = retHandle is { } rh ? Attrs(_r.GetParameter(rh).GetCustomAttributes()) : default;
        (bool rByRef, _, TypeSig rInner) = Peel(sig.ReturnType);
        b.ReturnUse = new TypeUse(rInner, ra.Nullable, ra.NullableContext ?? context, ra.TupleNames);
        string rt = TypeText.Plain(b.ReturnUse);
        if (rByRef) rt = (ra.Has("System.Runtime.CompilerServices.IsReadOnlyAttribute") ? "ref readonly " : "ref ") + rt;
        b.Return = rt;
        b.Constraints = Constraints(md.GetGenericParameters(), ctx, 0);
        return b;
    }

    string FormatConstant(Constant c)
    {
        BlobReader b = _r.GetBlobReader(c.Value);
        var inv = CultureInfo.InvariantCulture;
        switch (c.TypeCode)
        {
            case ConstantTypeCode.Boolean: return b.ReadBoolean() ? "true" : "false";
            case ConstantTypeCode.Char: return "'" + Escape(b.ReadChar().ToString(), '\'') + "'";
            case ConstantTypeCode.SByte: return b.ReadSByte().ToString(inv);
            case ConstantTypeCode.Byte: return b.ReadByte().ToString(inv);
            case ConstantTypeCode.Int16: return b.ReadInt16().ToString(inv);
            case ConstantTypeCode.UInt16: return b.ReadUInt16().ToString(inv);
            case ConstantTypeCode.Int32: return b.ReadInt32().ToString(inv);
            case ConstantTypeCode.UInt32: return b.ReadUInt32().ToString(inv);
            case ConstantTypeCode.Int64: return b.ReadInt64().ToString(inv);
            case ConstantTypeCode.UInt64: return b.ReadUInt64().ToString(inv);
            case ConstantTypeCode.Single: return b.ReadSingle().ToString("R", inv) + "f";
            case ConstantTypeCode.Double: return b.ReadDouble().ToString("R", inv);
            case ConstantTypeCode.String: return "\"" + Escape(b.ReadUTF16(b.RemainingBytes), '"') + "\"";
            default: return "null";
        }
    }

    static string Escape(string s, char quote)
    {
        var sb = new StringBuilder();
        foreach (char ch in s)
        {
            if (ch == quote || ch == '\\') sb.Append('\\').Append(ch);
            else if (ch == '\n') sb.Append("\\n");
            else if (ch == '\r') sb.Append("\\r");
            else if (ch == '\t') sb.Append("\\t");
            else if (ch < ' ') sb.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
            else sb.Append(ch);
        }
        return sb.ToString();
    }

    static readonly Dictionary<string, string> Operators = new(StringComparer.Ordinal)
    {
        ["op_Addition"] = "+", ["op_Subtraction"] = "-", ["op_Multiply"] = "*", ["op_Division"] = "/", ["op_Modulus"] = "%",
        ["op_BitwiseAnd"] = "&", ["op_BitwiseOr"] = "|", ["op_ExclusiveOr"] = "^", ["op_LeftShift"] = "<<", ["op_RightShift"] = ">>",
        ["op_UnsignedRightShift"] = ">>>", ["op_Equality"] = "==", ["op_Inequality"] = "!=", ["op_LessThan"] = "<", ["op_GreaterThan"] = ">",
        ["op_LessThanOrEqual"] = "<=", ["op_GreaterThanOrEqual"] = ">=", ["op_LogicalNot"] = "!", ["op_OnesComplement"] = "~",
        ["op_UnaryPlus"] = "+", ["op_UnaryNegation"] = "-", ["op_Increment"] = "++", ["op_Decrement"] = "--",
        ["op_True"] = "true", ["op_False"] = "false",
    };

    static string ParamIds(IEnumerable<TypeSig> sigs)
    {
        TypeSig[] a = [.. sigs];
        return a.Length == 0 ? "" : "(" + string.Join(",", a.Select(DocId.Of)) + ")";
    }

    static string MethodModifiers(MethodAttributes a, bool inInterface, bool inSealedType)
    {
        var sb = new StringBuilder();
        bool isStatic = (a & MethodAttributes.Static) != 0;
        bool isVirtual = (a & MethodAttributes.Virtual) != 0;
        bool isAbstract = (a & MethodAttributes.Abstract) != 0;
        bool isFinal = (a & MethodAttributes.Final) != 0;
        bool newSlot = (a & MethodAttributes.NewSlot) != 0;
        if (isStatic) sb.Append("static ");
        if (inInterface)
        {
            if (isStatic && isAbstract) sb.Append("abstract ");
            else if (isStatic && isVirtual) sb.Append("virtual ");
            return sb.ToString();
        }
        if (isAbstract) sb.Append("abstract ");
        else if (isVirtual && !newSlot) sb.Append(isFinal ? "sealed override " : "override ");
        else if (isVirtual && !isFinal && !inSealedType) sb.Append("virtual ");
        return sb.ToString();
    }

    void ReadMembers(TypeInfo t, TypeDefinition td, GenCtx ctx, byte typeContext)
    {
        bool inInterface = t.Kind == TypeKind.Interface;
        bool sealedType = (td.Attributes & TypeAttributes.Sealed) != 0;
        var accessors = new HashSet<MethodDefinitionHandle>();
        foreach (PropertyDefinitionHandle ph in td.GetProperties())
        {
            PropertyAccessors pa = _r.GetPropertyDefinition(ph).GetAccessors();
            if (!pa.Getter.IsNil) accessors.Add(pa.Getter);
            if (!pa.Setter.IsNil) accessors.Add(pa.Setter);
        }
        foreach (EventDefinitionHandle eh in td.GetEvents())
        {
            EventAccessors ea = _r.GetEventDefinition(eh).GetAccessors();
            if (!ea.Adder.IsNil) accessors.Add(ea.Adder);
            if (!ea.Remover.IsNil) accessors.Add(ea.Remover);
        }

        string tid = t.DocId[2..];
        bool Synthesized(string docId, string name) =>
            t.IsRecord && !_asm.Docs.ContainsKey(docId) &&
            name is "EqualityContract" or "PrintMembers" or "Equals" or "GetHashCode" or "ToString" or "Deconstruct" or "op_Equality" or "op_Inequality";

        // Fields and enum members.
        foreach (FieldDefinitionHandle fh in td.GetFields())
        {
            FieldDefinition fd = _r.GetFieldDefinition(fh);
            FieldAttributes fa = fd.Attributes;
            if ((fa & FieldAttributes.FieldAccessMask) is not (FieldAttributes.Public or FieldAttributes.Family or FieldAttributes.FamORAssem)) continue;
            if ((fa & (FieldAttributes.SpecialName | FieldAttributes.RTSpecialName)) != 0) continue;
            string fname = Str(fd.Name);
            if (fname.Contains('<')) continue;
            AttrSet at = Attrs(fd.GetCustomAttributes());
            if (at.Has("System.ComponentModel.EditorBrowsableAttribute") || at.Has("System.Runtime.CompilerServices.CompilerGeneratedAttribute")) continue;
            TypeSig sig = fd.DecodeSignature(SigProvider.Instance, ctx);
            var use = new TypeUse(Peel(sig).Inner, at.Nullable, at.NullableContext ?? typeContext, at.TupleNames);
            string? constant = (fa & FieldAttributes.Literal) != 0 ? FormatConstant(_r.GetConstant(fd.GetDefaultValue())) : null;
            string access = (fa & FieldAttributes.FieldAccessMask) switch
            {
                FieldAttributes.Public => "public",
                FieldAttributes.FamORAssem => "protected internal",
                _ => "protected",
            };
            string decl;
            if (t.Kind == TypeKind.Enum) decl = fname + " = " + constant;
            else
            {
                string mods = constant is not null ? "const " : (fa & FieldAttributes.Static) != 0 ? ((fa & FieldAttributes.InitOnly) != 0 ? "static readonly " : "static ") : (fa & FieldAttributes.InitOnly) != 0 ? "readonly " : "";
                decl = access + " " + mods + TypeText.Plain(use) + " " + fname + (constant is not null ? " = " + constant : "") + ";";
            }
            t.Members.Add(new MemberInfo
            {
                Kind = MemberKind.Field,
                Name = fname,
                DocId = "F:" + tid + "." + fname,
                Declaration = decl,
                Type = use,
                ConstValue = constant,
                Obsolete = at.Obsolete,
                SortKey = t.Kind == TypeKind.Enum ? "" : fname,
            });
        }

        // Methods, constructors and operators.
        foreach (MethodDefinitionHandle mh in td.GetMethods())
        {
            MethodDefinition md = _r.GetMethodDefinition(mh);
            if (accessors.Contains(mh) || !Visible(md.Attributes)) continue;
            string name = Str(md.Name);
            if (name == ".cctor" || name.Contains('<')) continue;
            if (t.Kind is TypeKind.Delegate or TypeKind.Enum) continue;
            AttrSet ma = Attrs(md.GetCustomAttributes());
            if (ma.Has("System.ComponentModel.EditorBrowsableAttribute") || ma.Has("System.Runtime.CompilerServices.CompilerGeneratedAttribute")) continue;
            bool ctor = name == ".ctor";
            if (name == "Finalize" && (md.Attributes & MethodAttributes.Virtual) != 0) continue;
            if (ctor && (md.Attributes & MethodAttributes.Static) != 0) continue;
            bool isOp = name.StartsWith("op_", StringComparison.Ordinal) && (md.Attributes & MethodAttributes.SpecialName) != 0;
            if (!isOp && (md.Attributes & MethodAttributes.SpecialName) != 0 && !ctor) continue;

            MemberBuild b = BuildMethod(md, t, ctx, typeContext, name);
            string idName = ctor ? "#ctor" : name;
            string id = "M:" + tid + "." + idName + (b.TypeParams.Length > 0 ? "``" + b.TypeParams.Length : "") + ParamIds(b.Sig.ParameterTypes);
            if (name is "op_Implicit" or "op_Explicit") id += "~" + DocId.Of(b.Sig.ReturnType);
            if (Synthesized(id, name)) continue;

            string access = Access(md.Attributes);
            if (inInterface) access = "";
            string mods = MethodModifiers(md.Attributes, inInterface, sealedType);
            string pars = string.Join(", ", b.Params.Select(ParamText));
            string decl;
            string display = name;
            MemberKind kind = MemberKind.Method;
            if (ctor)
            {
                kind = MemberKind.Constructor;
                display = t.Names[^1] is var last ? DocId.Split(last).Name : name;
                decl = Join(access, display) + "(" + pars + ");";
            }
            else if (isOp)
            {
                kind = MemberKind.Operator;
                if (name is "op_Implicit" or "op_Explicit")
                {
                    display = b.Return;
                    decl = Join(access, "static", name == "op_Implicit" ? "implicit" : "explicit") + " operator " + b.Return + "(" + pars + ");";
                    display = (name == "op_Implicit" ? "implicit" : "explicit") + " operator " + b.Return;
                }
                else
                {
                    string sym = Operators.TryGetValue(name, out string? s) ? s : name;
                    display = "operator " + sym;
                    decl = Join(access, "static", b.Return, "operator " + sym) + "(" + pars + ");";
                }
            }
            else
            {
                string tp = b.TypeParams.Length > 0 ? "<" + string.Join(", ", b.TypeParams) + ">" : "";
                decl = Join(access, mods.TrimEnd(), b.Return, name + tp) + "(" + pars + ")" + b.Constraints + ";";
            }

            t.Members.Add(new MemberInfo
            {
                Kind = kind,
                Name = display,
                DocId = id,
                Declaration = decl,
                Type = b.Sig.ReturnType is NamedSig { Key: "System.Void" } ? null : b.ReturnUse,
                Params = b.Params,
                TypeParams = b.TypeParams,
                Obsolete = ma.Obsolete,
                SortKey = (ctor ? "" : display) + "\u0001" + b.Params.Count.ToString("D3", CultureInfo.InvariantCulture) + id,
            });
        }

        // Properties and indexers.
        foreach (PropertyDefinitionHandle ph in td.GetProperties())
        {
            PropertyDefinition pd = _r.GetPropertyDefinition(ph);
            PropertyAccessors pa = pd.GetAccessors();
            MethodDefinition? get = pa.Getter.IsNil ? null : _r.GetMethodDefinition(pa.Getter);
            MethodDefinition? set = pa.Setter.IsNil ? null : _r.GetMethodDefinition(pa.Setter);
            bool getVisible = get is { } g && Visible(g.Attributes);
            bool setVisible = set is { } s && Visible(s.Attributes);
            if (!getVisible && !setVisible) continue;
            string pname = Str(pd.Name);
            if (pname.Contains('.')) continue;
            AttrSet at = Attrs(pd.GetCustomAttributes());
            if (at.Has("System.ComponentModel.EditorBrowsableAttribute")) continue;
            MethodSignature<TypeSig> sig = pd.DecodeSignature(SigProvider.Instance, ctx);
            MethodDefinition primary = getVisible ? get!.Value : set!.Value;
            byte context = at.NullableContext ?? Attrs(primary.GetCustomAttributes()).NullableContext ?? typeContext;
            (bool rByRef, _, TypeSig inner) = Peel(sig.ReturnType);
            var use = new TypeUse(inner, at.Nullable, context, at.TupleNames);

            // Parameter names come from an accessor, the property itself has none.
            var plist = new List<ParamInfo>();
            if (sig.ParameterTypes.Length > 0)
            {
                MethodDefinition src = get ?? set!.Value;
                var names = new Dictionary<int, string>();
                foreach (ParameterHandle prh in src.GetParameters())
                {
                    Parameter prm = _r.GetParameter(prh);
                    if (prm.SequenceNumber > 0) names[prm.SequenceNumber] = Str(prm.Name);
                }
                for (int i = 0; i < sig.ParameterTypes.Length; i++)
                {
                    plist.Add(new ParamInfo
                    {
                        Name = names.TryGetValue(i + 1, out string? n) ? n : "arg" + (i + 1),
                        Type = new TypeUse(Peel(sig.ParameterTypes[i]).Inner, null, context, null),
                    });
                }
            }

            int best = Math.Max(getVisible ? Rank(get!.Value.Attributes) : 0, setVisible ? Rank(set!.Value.Attributes) : 0);
            string access = best == 3 ? "public" : best == 2 ? "protected internal" : "protected";
            if (inInterface) access = "";
            string mods = MethodModifiers(primary.Attributes, inInterface, sealedType);
            var acc = new List<string>();
            if (getVisible) acc.Add((Rank(get!.Value.Attributes) < best ? Access(get.Value.Attributes) + " " : "") + "get;");
            if (setVisible)
            {
                MethodSignature<TypeSig> ss = set!.Value.DecodeSignature(SigProvider.Instance, ctx);
                bool init = ss.ReturnType is ModSig { Modifier: "System.Runtime.CompilerServices.IsExternalInit" };
                acc.Add((Rank(set.Value.Attributes) < best ? Access(set.Value.Attributes) + " " : "") + (init ? "init;" : "set;"));
            }
            bool indexer = plist.Count > 0;
            string display = indexer ? "this[" + string.Join(", ", plist.Select(p => TypeText.Plain(p.Type) + " " + p.Name)) + "]" : pname;
            string decl = Join(access, mods.TrimEnd(), rByRef ? "ref" : "", TypeText.Plain(use), display) + " { " + string.Join(" ", acc) + " }";
            string id = "P:" + tid + "." + pname + ParamIds(sig.ParameterTypes);
            if (Synthesized(id, pname)) continue;
            t.Members.Add(new MemberInfo
            {
                Kind = MemberKind.Property,
                Name = display,
                DocId = id,
                Declaration = decl,
                Type = use,
                Params = plist,
                Obsolete = at.Obsolete,
                SortKey = (indexer ? "~" : "") + pname + "\u0001" + plist.Count.ToString("D3", CultureInfo.InvariantCulture) + id,
            });
        }

        // Events.
        foreach (EventDefinitionHandle eh in td.GetEvents())
        {
            EventDefinition ed = _r.GetEventDefinition(eh);
            EventAccessors ea = ed.GetAccessors();
            if (ea.Adder.IsNil) continue;
            MethodDefinition add = _r.GetMethodDefinition(ea.Adder);
            if (!Visible(add.Attributes)) continue;
            string ename = Str(ed.Name);
            AttrSet at = Attrs(ed.GetCustomAttributes());
            if (at.Has("System.ComponentModel.EditorBrowsableAttribute")) continue;
            byte context = at.NullableContext ?? Attrs(add.GetCustomAttributes()).NullableContext ?? typeContext;
            var use = new TypeUse(SigProvider.FromHandle(_r, ed.Type, ctx), at.Nullable, context, null);
            string access = inInterface ? "" : Access(add.Attributes);
            string mods = MethodModifiers(add.Attributes, inInterface, sealedType);
            t.Members.Add(new MemberInfo
            {
                Kind = MemberKind.Event,
                Name = ename,
                DocId = "E:" + tid + "." + ename,
                Declaration = Join(access, mods.TrimEnd(), "event", TypeText.Plain(use), ename) + ";",
                Type = use,
                Obsolete = at.Obsolete,
                SortKey = ename,
            });
        }
    }

    static string Join(params string[] parts) => string.Join(' ', parts.Where(p => p.Length > 0));
}
