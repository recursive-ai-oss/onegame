using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace OneGame.Generators;

internal enum CodecKind
{
    Scalar,
    String,
    Enum,
    Value,
}

/// <summary>How one type is written, read, compared and dumped.</summary>
internal sealed class Codec
{
    public Codec(CodecKind kind, string typeName, string suffix, string underlyingCs, bool unsigned)
    {
        Kind = kind;
        TypeName = typeName;
        Suffix = suffix;
        UnderlyingCs = underlyingCs;
        Unsigned = unsigned;
    }

    public CodecKind Kind { get; }

    /// <summary>The type as C# text.</summary>
    public string TypeName { get; }

    /// <summary>The reader/writer method suffix for scalars and enums, such as Int32.</summary>
    public string Suffix { get; }

    /// <summary>The integer type behind an enum.</summary>
    public string UnderlyingCs { get; }

    public bool Unsigned { get; }

    public bool IsBool => Kind == CodecKind.Scalar && Suffix == "Bool";

    public string Write(string value, string w = "w") => Kind switch
    {
        CodecKind.Value => $"{value}.Write({w});",
        CodecKind.String => $"{w}.WriteString({value});",
        CodecKind.Enum => $"{w}.Write{Suffix}(({UnderlyingCs}){value});",
        _ => $"{w}.Write{Suffix}({value});",
    };

    public string Read(string r = "r") => Kind switch
    {
        CodecKind.Value => $"global::OneGame.Core.SyncRead.Value<{TypeName}>(ref {r})",
        CodecKind.String => $"{r}.ReadString()",
        CodecKind.Enum => $"({TypeName}){r}.Read{Suffix}()",
        _ => $"{r}.Read{Suffix}()",
    };

    public string Json(string value, string j = "j") => Kind switch
    {
        CodecKind.Value => $"{value}.WriteJson({j});",
        CodecKind.String => $"{j}.Value({value});",
        CodecKind.Enum => Unsigned ? $"{j}.Value((ulong){value});" : $"{j}.Value((long){value});",
        _ when IsBool => $"{j}.Value({value});",
        _ => Unsigned ? $"{j}.Value((ulong){value});" : $"{j}.Value((long){value});",
    };

    public string Equal(string a, string b) => $"global::System.Collections.Generic.EqualityComparer<{TypeName}>.Default.Equals({a}, {b})";
}

internal static class Codecs
{
    public const string CoreNamespace = "OneGame.Core";

    private static readonly SymbolDisplayFormat Qualified = SymbolDisplayFormat.FullyQualifiedFormat;

    public static string TypeText(ITypeSymbol t) => t.ToDisplayString(Qualified);

    public static string TypeTextNullable(ITypeSymbol t)
        => t.ToDisplayString(Qualified.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier));

    private static bool TryScalar(SpecialType special, out string suffix, out bool unsigned)
    {
        unsigned = false;
        switch (special)
        {
            case SpecialType.System_Boolean: suffix = "Bool"; return true;
            case SpecialType.System_Byte: suffix = "Byte"; unsigned = true; return true;
            case SpecialType.System_SByte: suffix = "SByte"; return true;
            case SpecialType.System_Int16: suffix = "Int16"; return true;
            case SpecialType.System_UInt16: suffix = "UInt16"; unsigned = true; return true;
            case SpecialType.System_Int32: suffix = "Int32"; return true;
            case SpecialType.System_UInt32: suffix = "UInt32"; unsigned = true; return true;
            case SpecialType.System_Int64: suffix = "Int64"; return true;
            case SpecialType.System_UInt64: suffix = "UInt64"; unsigned = true; return true;
            default: suffix = string.Empty; return false;
        }
    }

    /// <summary>The codec for a single value (not a list), or null when the type cannot be synced.</summary>
    public static Codec? TryGet(ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_String)
        {
            return new Codec(CodecKind.String, "string", "String", "", false);
        }

        if (TryScalar(type.SpecialType, out var suffix, out var unsigned))
        {
            return new Codec(CodecKind.Scalar, TypeText(type), suffix, "", unsigned);
        }

        if (type.TypeKind == TypeKind.Enum && type is INamedTypeSymbol e && e.EnumUnderlyingType is { } under
            && TryScalar(under.SpecialType, out var us, out var uu))
        {
            return new Codec(CodecKind.Enum, TypeText(type), us, TypeText(under), uu);
        }

        if (type.TypeKind == TypeKind.Struct && type is INamedTypeSymbol named && !IsNullableValue(named) && ImplementsSyncValue(named))
        {
            return new Codec(CodecKind.Value, TypeText(type), "", "", false);
        }

        return null;
    }

    private static bool IsNullableValue(INamedTypeSymbol t) => t.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

    private static bool ImplementsSyncValue(INamedTypeSymbol type)
    {
        foreach (var attribute in type.GetAttributes())
        {
            if (IsCoreAttribute(attribute, "SyncedStructAttribute"))
            {
                return true;
            }
        }

        foreach (var i in type.AllInterfaces)
        {
            if (i.Name == "ISyncValue" && i.TypeArguments.Length == 1 && IsInCore(i) && SymbolEqualityComparer.Default.Equals(i.TypeArguments[0], type))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsInCore(INamedTypeSymbol t)
        => t.ContainingNamespace is { Name: "Core", ContainingNamespace: { Name: "OneGame", ContainingNamespace.IsGlobalNamespace: true } };

    public static bool IsCoreAttribute(AttributeData a, string name)
        => a.AttributeClass is { } c && c.Name == name && IsInCore(c);

    public static AttributeData? FindAttribute(ISymbol symbol, string name)
        => symbol.GetAttributes().FirstOrDefault(a => IsCoreAttribute(a, name));

    public static bool HasAttribute(ISymbol symbol, string name) => FindAttribute(symbol, name) is not null;

    /// <summary>True for <c>SyncList&lt;T&gt;</c>; <paramref name="element"/> is T.</summary>
    public static bool IsSyncList(ITypeSymbol type, out ITypeSymbol element)
    {
        if (type is INamedTypeSymbol { Name: "SyncList", TypeArguments.Length: 1 } n && IsInCore(n))
        {
            element = n.TypeArguments[0];
            return true;
        }

        element = type;
        return false;
    }

    /// <summary>True if the type itself is float, double, decimal, Half, or one of the float vector and matrix types of System.Numerics (which hold floats: their math differs between machines).</summary>
    public static bool IsFloatScalar(ITypeSymbol type)
        => type.SpecialType is SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal
            || type is INamedTypeSymbol { Name: "Half" } half && half.ContainingNamespace?.ToDisplayString() == "System"
            || IsNumericsFloatType(type);

    private static bool IsNumericsFloatType(ITypeSymbol type)
        => type is INamedTypeSymbol { Name: "Vector2" or "Vector3" or "Vector4" or "Quaternion" or "Plane" or "Matrix3x2" or "Matrix4x4" } n
            && n.ContainingNamespace?.ToDisplayString() == "System.Numerics";

    /// <summary>True if the type is, or is built from, float, double, decimal or Half.</summary>
    public static bool IsFloating(ITypeSymbol type)
    {
        switch (type.SpecialType)
        {
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_Decimal:
                return true;
        }

        if (IsNumericsFloatType(type))
        {
            return true;
        }

        if (type is IArrayTypeSymbol array)
        {
            return IsFloating(array.ElementType);
        }

        if (type is INamedTypeSymbol named)
        {
            if (named.Name == "Half" && named.ContainingNamespace?.ToDisplayString() == "System")
            {
                return true;
            }

            foreach (var arg in named.TypeArguments)
            {
                if (IsFloating(arg))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>True when the type can be a synced property or command argument: a codec, or (when allowed) a list of one.</summary>
    public static bool IsSyncable(ITypeSymbol type, bool allowList)
    {
        if (allowList && IsSyncList(type, out var element))
        {
            return TryGet(element) is not null;
        }

        return TryGet(type) is not null;
    }
}
