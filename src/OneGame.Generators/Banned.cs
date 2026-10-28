using System;
using Microsoft.CodeAnalysis;

namespace OneGame.Generators;

/// <summary>The tables that say which framework members sim code may not use.</summary>
internal static class Banned
{
    /// <summary>Returns the rule that forbids using <paramref name="symbol"/> in sim code, or null.</summary>
    public static DiagnosticDescriptor? Match(ISymbol symbol)
    {
        var type = symbol as INamedTypeSymbol ?? symbol.ContainingType;
        if (type is null)
        {
            return null;
        }

        type = type.OriginalDefinition;
        var typeName = FullName(type);
        var member = symbol is INamedTypeSymbol ? null : symbol.Name;
        if (symbol is IMethodSymbol { MethodKind: MethodKind.Constructor })
        {
            member = ".ctor";
        }

        switch (typeName)
        {
            // Clock
            case "System.Diagnostics.Stopwatch":
            case "System.TimeProvider":
                return Rules.Clock;
            case "System.DateTime":
                return member is "Now" or "UtcNow" or "Today" ? Rules.Clock : null;
            case "System.DateTimeOffset":
                return member is "Now" or "UtcNow" ? Rules.Clock : null;
            case "System.Environment":
                if (member is "TickCount" or "TickCount64")
                {
                    return Rules.Clock;
                }

                return member == "NewLine" ? null : Rules.InputOutput;

            // Threads and tasks
            case "System.Threading.Thread":
            case "System.Threading.ThreadPool":
            case "System.Threading.Tasks.Task":
            case "System.Threading.Tasks.ValueTask":
            case "System.Threading.Tasks.Parallel":
            case "System.Threading.Timer":
            case "System.Timers.Timer":
            case "System.Threading.Monitor":
            case "System.Threading.Mutex":
            case "System.Threading.SemaphoreSlim":
            case "System.Linq.ParallelEnumerable":
                return Rules.Threads;

            // I/O
            case "System.IO.File":
            case "System.IO.Directory":
            case "System.IO.FileStream":
            case "System.IO.FileInfo":
            case "System.IO.DirectoryInfo":
            case "System.IO.StreamReader":
            case "System.IO.StreamWriter":
            case "System.IO.FileSystemWatcher":
            case "System.Console":
            case "System.Diagnostics.Process":
                return Rules.InputOutput;

            // Randomness
            case "System.Random":
            case "System.Security.Cryptography.RandomNumberGenerator":
            case "OneGame.Core.Entropy":
                return Rules.UnnamedRandom;
            case "System.Guid":
                return member == "NewGuid" ? Rules.UnnamedRandom : null;
        }

        if (typeName.StartsWith("OneGame.Gltf.", StringComparison.Ordinal))
        {
            return Rules.AnimationInSim;
        }

        return typeName.StartsWith("System.Net.", StringComparison.Ordinal) ? Rules.InputOutput : null;
    }

    public static string FullName(INamedTypeSymbol type)
    {
        var name = type.Name;
        for (var c = type.ContainingType; c is not null; c = c.ContainingType)
        {
            name = c.Name + "." + name;
        }

        var ns = type.ContainingNamespace;
        return ns is null || ns.IsGlobalNamespace ? name : ns.ToDisplayString() + "." + name;
    }

    /// <summary>True for collections whose enumeration order the framework does not define.</summary>
    public static bool IsUnordered(ITypeSymbol? type)
    {
        for (var t = type as INamedTypeSymbol; t is not null; t = t.ContainingType)
        {
            switch (FullName(t.OriginalDefinition))
            {
                case "System.Collections.Generic.Dictionary":
                case "System.Collections.Generic.HashSet":
                case "System.Collections.Generic.IDictionary":
                case "System.Collections.Generic.IReadOnlyDictionary":
                case "System.Collections.Generic.ISet":
                case "System.Collections.Generic.IReadOnlySet":
                case "System.Collections.Concurrent.ConcurrentDictionary":
                case "System.Collections.Concurrent.ConcurrentBag":
                case "System.Collections.Hashtable":
                case "System.Collections.Immutable.ImmutableDictionary":
                case "System.Collections.Immutable.ImmutableHashSet":
                    return true;
            }
        }

        return false;
    }
}
