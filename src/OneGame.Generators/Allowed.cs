using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace OneGame.Generators;

/// <summary>
/// What sim code may call in assemblies outside the compilation without a warning. The list is the framework surface that behaves the
/// same on every machine: integer math, collections with a defined order of operations, span and array helpers, string building
/// and ordinal comparison. Culture-dependent members are caught first, by <see cref="CultureRules"/>.
/// </summary>
internal static class SimAllow
{
    private static readonly HashSet<string> Types = new(StringComparer.Ordinal)
    {
        "System.Object", "System.ValueType", "System.String", "System.Char", "System.Boolean", "System.Byte", "System.SByte",
        "System.Int16", "System.UInt16", "System.Int32", "System.UInt32", "System.Int64", "System.UInt64", "System.Int128",
        "System.UInt128", "System.IntPtr", "System.UIntPtr", "System.Single", "System.Double", "System.Decimal", "System.Math",
        "System.MathF", "System.Array", "System.Span", "System.ReadOnlySpan", "System.Memory", "System.ReadOnlyMemory",
        "System.MemoryExtensions", "System.ArraySegment", "System.Buffer", "System.BitConverter", "System.Convert", "System.Enum",
        "System.Nullable", "System.Tuple", "System.ValueTuple", "System.Index", "System.Range", "System.StringComparer",
        "System.StringComparison", "System.IComparable", "System.IEquatable", "System.IFormattable", "System.IDisposable",
        "System.FormattableString", "System.DateTime", "System.DateTimeOffset", "System.TimeSpan", "System.DateOnly",
        "System.TimeOnly", "System.Guid", "System.Text.StringBuilder", "System.Text.Rune", "System.Text.Encoding",
        "System.Text.UTF8Encoding", "System.Globalization.CultureInfo", "System.Globalization.NumberStyles",
        "System.Globalization.CompareOptions", "System.Globalization.CharUnicodeInfo", "System.Globalization.UnicodeCategory",
        "System.Numerics.BitOperations", "System.Numerics.BigInteger", "System.Diagnostics.Debug",
        "System.Runtime.InteropServices.CollectionsMarshal", "System.Runtime.InteropServices.MemoryMarshal",
    };

    private static readonly string[] Namespaces =
    [
        "System.Collections", "System.Collections.Generic", "System.Collections.Immutable", "System.Collections.ObjectModel",
        "System.Linq", "System.Diagnostics.CodeAnalysis", "System.Buffers.Binary", "System.Runtime.CompilerServices",
    ];

    // Members of allowed types that still differ between runs or machines.
    private static readonly HashSet<string> DeniedMembers = new(StringComparer.Ordinal)
    {
        "System.Object.GetHashCode", "System.Object.GetType", "System.ValueType.GetHashCode", "System.String.GetHashCode",
        "System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode", "System.String.Intern", "System.String.IsInterned",
        "System.Text.Encoding.Default", "System.Text.Encoding.GetEncoding",
    };

    // Types where only the listed members are known to be machine independent.
    private static readonly Dictionary<string, HashSet<string>> OnlyMembers = new(StringComparer.Ordinal)
    {
        ["System.Globalization.CultureInfo"] = new(StringComparer.Ordinal) { "InvariantCulture", "Name" },
    };

    /// <summary>True if the framework API is on the built-in list.</summary>
    public static bool IsFrameworkSafe(ISymbol symbol)
    {
        var type = symbol as INamedTypeSymbol ?? symbol.ContainingType;
        if (type is null)
        {
            return false;
        }

        type = type.OriginalDefinition;
        var name = Banned.FullName(type);
        var member = symbol is INamedTypeSymbol ? null : symbol.Name;
        if (symbol is IMethodSymbol { MethodKind: MethodKind.Constructor })
        {
            member = ".ctor";
        }

        if (symbol is IMethodSymbol { AssociatedSymbol: { } associated })
        {
            member = associated.Name;
        }

        if (member is not null)
        {
            if (DeniedMembers.Contains(name + "." + member))
            {
                return false;
            }

            if (OnlyMembers.TryGetValue(name, out var only))
            {
                return only.Contains(member);
            }
        }

        if (type.TypeKind is TypeKind.Delegate or TypeKind.Enum || type.SpecialType != SpecialType.None)
        {
            return true;
        }

        if (Types.Contains(name))
        {
            return true;
        }

        var ns = type.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        foreach (var allowed in Namespaces)
        {
            if (ns == allowed)
            {
                return true;
            }
        }

        // Exception constructors and the static throw helpers (ThrowIfNull and friends) do not depend on the machine.
        for (var t = type; t is not null; t = t.BaseType)
        {
            if (t.Name == "Exception" && t.ContainingNamespace is { Name: "System", ContainingNamespace.IsGlobalNamespace: true })
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>True if the symbol, a type around it, or its assembly carries the engine's SimSafe attribute.</summary>
    public static bool IsMarkedSafe(ISymbol symbol)
    {
        if (symbol is IMethodSymbol { AssociatedSymbol: { } associated } && Codecs.HasAttribute(associated, "SimSafeAttribute"))
        {
            return true;
        }

        for (var s = symbol; s is not null; s = s.ContainingSymbol)
        {
            if (Codecs.HasAttribute(s, "SimSafeAttribute"))
            {
                return true;
            }

            if (s is IAssemblySymbol)
            {
                break;
            }
        }

        return false;
    }

    /// <summary>True if a trust entry (a namespace, a type name or an assembly name) covers the symbol.</summary>
    public static bool IsTrusted(ISymbol symbol, IReadOnlyCollection<string> trusted)
    {
        if (trusted.Count == 0)
        {
            return false;
        }

        var type = symbol as INamedTypeSymbol ?? symbol.ContainingType;
        var full = type is null ? string.Empty : Banned.FullName(type.OriginalDefinition);
        var assembly = symbol.ContainingAssembly?.Name ?? string.Empty;
        return trusted.Any(t => t == assembly || (full.Length > 0 && (full == t || full.StartsWith(t + ".", StringComparison.Ordinal))));
    }
}

/// <summary>Detects framework calls whose result depends on the machine's culture (OG0016).</summary>
internal static class CultureRules
{
    private static readonly HashSet<string> Numbers = new(StringComparer.Ordinal)
    {
        "System.Byte", "System.SByte", "System.Int16", "System.UInt16", "System.Int32", "System.UInt32", "System.Int64",
        "System.UInt64", "System.Int128", "System.UInt128", "System.Single", "System.Double", "System.Decimal", "System.Half",
        "System.DateTime", "System.DateTimeOffset", "System.Numerics.BigInteger", "System.IntPtr", "System.UIntPtr",
    };

    private static readonly HashSet<string> CurrentCultureMembers = new(StringComparer.Ordinal)
    {
        "System.Globalization.CultureInfo.CurrentCulture", "System.Globalization.CultureInfo.CurrentUICulture",
        "System.Globalization.CultureInfo.InstalledUICulture", "System.Globalization.CultureInfo.DefaultThreadCurrentCulture",
        "System.Globalization.CultureInfo.DefaultThreadCurrentUICulture", "System.StringComparer.CurrentCulture",
        "System.StringComparer.CurrentCultureIgnoreCase",
    };

    /// <summary>True for numeric and date types whose text form follows the current culture.</summary>
    public static bool IsCultureNumber(ITypeSymbol? type)
    {
        if (type is null)
        {
            return false;
        }

        if (type.SpecialType is SpecialType.System_Byte or SpecialType.System_SByte or SpecialType.System_Int16 or SpecialType.System_UInt16
            or SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64
            or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal or SpecialType.System_DateTime)
        {
            return true;
        }

        return type is INamedTypeSymbol n && Numbers.Contains(Banned.FullName(n.OriginalDefinition));
    }

    /// <summary>Why the symbol is culture-sensitive, or null. <paramref name="call"/> is the invocation when there is one.</summary>
    public static string? Match(ISymbol symbol, IInvocationOperation? call)
    {
        var type = symbol as INamedTypeSymbol ?? symbol.ContainingType;
        if (type is null)
        {
            return null;
        }

        var typeName = Banned.FullName(type.OriginalDefinition);
        var shown = type.Name + "." + (symbol is IMethodSymbol { AssociatedSymbol: { } a } ? a.Name : symbol.Name);
        if (symbol is not IMethodSymbol method)
        {
            return CurrentCultureMembers.Contains(typeName + "." + symbol.Name) ? shown : null;
        }

        if (method.AssociatedSymbol is { } property)
        {
            return CurrentCultureMembers.Contains(typeName + "." + property.Name) ? shown : null;
        }

        var name = method.Name;
        var hasProvider = method.Parameters.Any(p => p.Type.Name is "IFormatProvider" or "CultureInfo" or "NumberFormatInfo" or "DateTimeFormatInfo");

        // A StringComparison argument naming the current culture (0, 1) is the same hazard under another spelling.
        if (call is not null)
        {
            for (var i = 0; i < call.Arguments.Length; i++)
            {
                var p = call.Arguments[i].Parameter;
                if (p?.Type.Name == "StringComparison" && call.Arguments[i].Value.ConstantValue is { HasValue: true, Value: int v } && v is 0 or 1)
                {
                    return shown;
                }
            }
        }

        if (Numbers.Contains(typeName))
        {
            if (name is "ToString" or "Parse" or "TryParse" or "TryFormat" && !hasProvider)
            {
                return IsHexFormat(call) ? null : shown;
            }

            return null;
        }

        switch (typeName)
        {
            case "System.String":
                switch (name)
                {
                    case "Format" when !hasProvider:
                    case "ToUpper" or "ToLower" when !hasProvider:
                    case "Compare" or "CompareTo" when !hasProvider && !HasComparison(method):
                        return shown;
                    case "StartsWith" or "EndsWith" or "IndexOf" or "LastIndexOf"
                        when !hasProvider && !HasComparison(method) && method.Parameters.Length > 0 && method.Parameters[0].Type.SpecialType == SpecialType.System_String:
                        return shown;
                }

                return null;
            case "System.Char":
                return name is "ToUpper" or "ToLower" && !hasProvider ? shown : null;
            case "System.Convert":
                if (hasProvider || method.Parameters.Length == 0)
                {
                    return null;
                }

                var first = method.Parameters[0].Type;
                if (name == "ToString")
                {
                    return IsCultureNumber(first) ? shown : null;
                }

                return name.StartsWith("To", StringComparison.Ordinal) && first.SpecialType is SpecialType.System_String or SpecialType.System_Object ? shown : null;
            case "System.Text.StringBuilder":
                if (hasProvider)
                {
                    return null;
                }

                return name == "AppendFormat" || name is "Append" or "Insert" && method.Parameters.Any(p => IsCultureNumber(p.Type)) ? shown : null;
        }

        return null;
    }

    private static bool HasComparison(IMethodSymbol method) => method.Parameters.Any(p => p.Type.Name is "StringComparison");

    // Hex digits carry no sign or separators, so "x" and "X" formats read the same everywhere.
    private static bool IsHexFormat(IInvocationOperation? call)
        => call is { Arguments.Length: > 0 } && call.Arguments[0].Value.ConstantValue is { HasValue: true, Value: string { Length: > 0 } f } && f[0] is 'x' or 'X';
}
