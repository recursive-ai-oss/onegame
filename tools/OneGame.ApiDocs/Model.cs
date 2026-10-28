using System.Reflection.Metadata;
using System.Xml.Linq;

namespace OneGame.ApiDocs;

enum TypeKind { Class, Struct, Interface, Enum, Delegate }

enum MemberKind { Constructor, Field, Property, Event, Method, Operator }

sealed class ParamInfo
{
    public required string Name { get; init; }
    public required TypeUse Type { get; init; }
    /// <summary>"", "ref", "out", "in", "params" or "this".</summary>
    public string Modifier { get; init; } = "";
    public string? Default { get; init; }
}

sealed class MemberInfo
{
    public required MemberKind Kind { get; init; }
    public required string Name { get; init; }
    public required string DocId { get; init; }
    public required string Declaration { get; init; }
    public string Anchor { get; set; } = "";
    /// <summary>Field, property and event type, or a method's return type; null for constructors and void.</summary>
    public TypeUse? Type { get; init; }
    public List<ParamInfo> Params { get; init; } = [];
    public string[] TypeParams { get; init; } = [];
    public string? ConstValue { get; init; }
    public string? Obsolete { get; init; }
    public string SortKey { get; init; } = "";
}

sealed class TypeInfo
{
    public required AssemblyInfo Assembly { get; init; }
    public required string Namespace { get; init; }
    public required string[] Names { get; init; }
    public required TypeKind Kind { get; init; }
    public required string DocId { get; init; }
    public required string Declaration { get; init; }
    public required string DisplayName { get; init; }
    public string[] OwnTypeParams { get; init; } = [];
    public TypeInfo? Outer { get; init; }
    public List<TypeInfo> Nested { get; } = [];
    public List<MemberInfo> Members { get; } = [];
    public TypeSig? Base { get; init; }
    public List<TypeSig> Interfaces { get; init; } = [];
    public string? Obsolete { get; init; }
    public string? AttributeTargets { get; init; }
    public bool IsRecord { get; init; }
    /// <summary>The metadata key (namespace plus names with arity), the same text <see cref="NamedSig.Key"/> gives.</summary>
    public string Key => Namespace.Length == 0 ? string.Join('.', Names) : Namespace + "." + string.Join('.', Names);
    /// <summary>The file this type's page is written to, relative to the output folder.</summary>
    public string Path { get; set; } = "";
}

sealed class AssemblyInfo
{
    public required string Name { get; init; }
    public required string Package { get; init; }
    public required string Description { get; init; }
    /// <summary>The package is published only as a prerelease (its project appends a suffix to the engine version).</summary>
    public bool Prerelease { get; init; }
    public List<TypeInfo> Types { get; } = [];
    public Dictionary<string, XElement> Docs { get; } = new(StringComparer.Ordinal);
}
