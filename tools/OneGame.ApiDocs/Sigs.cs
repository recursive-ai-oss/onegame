using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace OneGame.ApiDocs;

// A type as written in a signature, decoded from metadata without loading any assembly.
abstract class TypeSig;

sealed class NamedSig(string ns, string[] names, bool isValueType) : TypeSig
{
    public string Namespace { get; } = ns;
    /// <summary>The outermost type first; every name keeps its backtick arity suffix.</summary>
    public string[] Names { get; } = names;
    public bool IsValueType { get; } = isValueType;
    public string Key => Namespace.Length == 0 ? string.Join('.', Names) : Namespace + "." + string.Join('.', Names);
}

sealed class GenericSig(NamedSig def, TypeSig[] args) : TypeSig
{
    public NamedSig Def { get; } = def;
    public TypeSig[] Args { get; } = args;
}

sealed class ArraySig(TypeSig elem, int rank) : TypeSig
{
    public TypeSig Elem { get; } = elem;
    public int Rank { get; } = rank;
}

sealed class PointerSig(TypeSig elem) : TypeSig { public TypeSig Elem { get; } = elem; }

sealed class ByRefSig(TypeSig elem) : TypeSig { public TypeSig Elem { get; } = elem; }

sealed class ParamSig(int index, bool ofMethod, string name) : TypeSig
{
    public int Index { get; } = index;
    public bool OfMethod { get; } = ofMethod;
    public string Name { get; } = name;
}

sealed class FnPtrSig : TypeSig;

/// <summary>A type with a custom modifier (<c>modreq</c> or <c>modopt</c>), such as <c>IsExternalInit</c> on an init setter.</summary>
sealed class ModSig(TypeSig inner, string modifier) : TypeSig
{
    public TypeSig Inner { get; } = inner;
    public string Modifier { get; } = modifier;
}

/// <summary>Names of the generic parameters in scope: the declaring type's (outer types first) and the method's.</summary>
sealed class GenCtx(string[] typeParams, string[] methodParams)
{
    public string[] TypeParams { get; } = typeParams;
    public string[] MethodParams { get; } = methodParams;
    public static GenCtx Empty { get; } = new([], []);
}

sealed class SigProvider : ISignatureTypeProvider<TypeSig, GenCtx>
{
    public static readonly SigProvider Instance = new();

    public TypeSig GetPrimitiveType(PrimitiveTypeCode typeCode)
    {
        (string name, bool value) = typeCode switch
        {
            PrimitiveTypeCode.Void => ("Void", true),
            PrimitiveTypeCode.Boolean => ("Boolean", true),
            PrimitiveTypeCode.Char => ("Char", true),
            PrimitiveTypeCode.SByte => ("SByte", true),
            PrimitiveTypeCode.Byte => ("Byte", true),
            PrimitiveTypeCode.Int16 => ("Int16", true),
            PrimitiveTypeCode.UInt16 => ("UInt16", true),
            PrimitiveTypeCode.Int32 => ("Int32", true),
            PrimitiveTypeCode.UInt32 => ("UInt32", true),
            PrimitiveTypeCode.Int64 => ("Int64", true),
            PrimitiveTypeCode.UInt64 => ("UInt64", true),
            PrimitiveTypeCode.Single => ("Single", true),
            PrimitiveTypeCode.Double => ("Double", true),
            PrimitiveTypeCode.String => ("String", false),
            PrimitiveTypeCode.Object => ("Object", false),
            PrimitiveTypeCode.IntPtr => ("IntPtr", true),
            PrimitiveTypeCode.UIntPtr => ("UIntPtr", true),
            PrimitiveTypeCode.TypedReference => ("TypedReference", true),
            _ => ("Object", false),
        };
        return new NamedSig("System", [name], value);
    }

    public TypeSig GetSZArrayType(TypeSig elementType) => new ArraySig(elementType, 1);

    public TypeSig GetArrayType(TypeSig elementType, ArrayShape shape) => new ArraySig(elementType, shape.Rank);

    public TypeSig GetByReferenceType(TypeSig elementType) => new ByRefSig(elementType);

    public TypeSig GetPointerType(TypeSig elementType) => new PointerSig(elementType);

    public TypeSig GetFunctionPointerType(MethodSignature<TypeSig> signature) => new FnPtrSig();

    public TypeSig GetGenericInstantiation(TypeSig genericType, ImmutableArray<TypeSig> typeArguments) =>
        new GenericSig((NamedSig)genericType, [.. typeArguments]);

    public TypeSig GetGenericMethodParameter(GenCtx genericContext, int index) =>
        new ParamSig(index, true, index < genericContext.MethodParams.Length ? genericContext.MethodParams[index] : "T" + index);

    public TypeSig GetGenericTypeParameter(GenCtx genericContext, int index) =>
        new ParamSig(index, false, index < genericContext.TypeParams.Length ? genericContext.TypeParams[index] : "T" + index);

    public TypeSig GetModifiedType(TypeSig modifier, TypeSig unmodifiedType, bool isRequired) =>
        new ModSig(unmodifiedType, modifier is NamedSig n ? n.Key : "");

    public TypeSig GetPinnedType(TypeSig elementType) => elementType;

    public TypeSig GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
    {
        var names = new List<string>();
        string ns = "";
        for (TypeDefinitionHandle h = handle; !h.IsNil;)
        {
            TypeDefinition td = reader.GetTypeDefinition(h);
            names.Insert(0, reader.GetString(td.Name));
            TypeDefinitionHandle outer = td.GetDeclaringType();
            if (outer.IsNil) ns = reader.GetString(td.Namespace);
            h = outer;
        }
        return new NamedSig(ns, [.. names], rawTypeKind == 0x11);
    }

    public TypeSig GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
    {
        var names = new List<string>();
        string ns = "";
        for (TypeReferenceHandle h = handle; !h.IsNil;)
        {
            TypeReference tr = reader.GetTypeReference(h);
            names.Insert(0, reader.GetString(tr.Name));
            if (tr.ResolutionScope.Kind == HandleKind.TypeReference)
            {
                h = (TypeReferenceHandle)tr.ResolutionScope;
            }
            else
            {
                ns = reader.GetString(tr.Namespace);
                break;
            }
        }
        return new NamedSig(ns, [.. names], rawTypeKind == 0x11);
    }

    public TypeSig GetTypeFromSpecification(MetadataReader reader, GenCtx genericContext, TypeSpecificationHandle handle, byte rawTypeKind) =>
        reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);

    public static TypeSig FromHandle(MetadataReader reader, EntityHandle handle, GenCtx ctx) => handle.Kind switch
    {
        HandleKind.TypeDefinition => Instance.GetTypeFromDefinition(reader, (TypeDefinitionHandle)handle, 0x12),
        HandleKind.TypeReference => Instance.GetTypeFromReference(reader, (TypeReferenceHandle)handle, 0x12),
        HandleKind.TypeSpecification => Instance.GetTypeFromSpecification(reader, ctx, (TypeSpecificationHandle)handle, 0x12),
        _ => new NamedSig("System", ["Object"], false),
    };
}

/// <summary>A signature type plus the annotations that live in attributes: nullability flags and tuple element names.</summary>
sealed class TypeUse(TypeSig sig, byte[]? flags, byte context, string?[]? tupleNames)
{
    public TypeSig Sig { get; } = sig;
    public byte[]? Flags { get; } = flags;
    public byte Context { get; } = context;
    public string?[]? TupleNames { get; } = tupleNames;
}

/// <summary>Documentation comment IDs for types in signatures (ECMA-334 annex D).</summary>
static class DocId
{
    public static string Of(TypeSig sig) => sig switch
    {
        ModSig m => Of(m.Inner),
        ByRefSig b => Of(b.Elem) + "@",
        PointerSig p => Of(p.Elem) + "*",
        ArraySig a => Of(a.Elem) + (a.Rank == 1 ? "[]" : "[" + string.Join(",", Enumerable.Repeat("0:", a.Rank)) + "]"),
        ParamSig p => (p.OfMethod ? "``" : "`") + p.Index,
        GenericSig g => OfGeneric(g),
        NamedSig n => n.Key,
        _ => "System.IntPtr",
    };

    static string OfGeneric(GenericSig g)
    {
        var sb = new System.Text.StringBuilder();
        if (g.Def.Namespace.Length > 0) sb.Append(g.Def.Namespace).Append('.');
        int next = 0;
        for (int i = 0; i < g.Def.Names.Length; i++)
        {
            (string name, int arity) = Split(g.Def.Names[i]);
            if (i > 0) sb.Append('.');
            sb.Append(name);
            if (arity > 0)
            {
                sb.Append('{');
                for (int k = 0; k < arity; k++)
                {
                    if (k > 0) sb.Append(',');
                    sb.Append(Of(g.Args[next++]));
                }
                sb.Append('}');
            }
        }
        return sb.ToString();
    }

    public static (string Name, int Arity) Split(string metadataName)
    {
        int tick = metadataName.IndexOf('`');
        return tick < 0 ? (metadataName, 0) : (metadataName[..tick], int.Parse(metadataName[(tick + 1)..], System.Globalization.CultureInfo.InvariantCulture));
    }
}
