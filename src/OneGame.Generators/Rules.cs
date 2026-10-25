using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace OneGame.Generators;

/// <summary>Every diagnostic the generator and analyzers report.</summary>
internal static class Rules
{
    private const string Category = "OneGame";

    private static DiagnosticDescriptor Error(string id, string title, string message)
        => new(id, title, message, Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor FloatInState = Error("OG0001", "Floating-point type in synced state",
        "'{0}' uses a floating-point type, which differs between platforms. Use OneGame.Core.Fixed (or FVec2) for synced state.");

    public static readonly DiagnosticDescriptor FloatInSim = Error("OG0002", "Floating-point arithmetic in sim code",
        "Floating-point value in sim code ({0}). Sim code must use OneGame.Core.Fixed; floats belong in [View] code.");

    public static readonly DiagnosticDescriptor UnorderedIteration = Error("OG0003", "Iteration order is not defined",
        "Sim code iterates in no defined order: {0}. Use a List<T>, an array, a SortedDictionary or SortedSet, or sort the keys first.");

    public static readonly DiagnosticDescriptor Clock = Error("OG0004", "Clock in sim code",
        "{0} reads the clock, but the sim advances only by ticks. Use World.Tick, or a Timed<T> value.");

    public static readonly DiagnosticDescriptor Threads = Error("OG0005", "Threads or tasks in sim code",
        "{0} introduces threads or parallelism. Sim code runs on one thread in a fixed order.");

    public static readonly DiagnosticDescriptor InputOutput = Error("OG0006", "I/O in sim code",
        "{0} reads or writes outside the world. Sim code does no I/O; load data before the sim starts and pass it in as state.");

    public static readonly DiagnosticDescriptor UnnamedRandom = Error("OG0007", "Random source is not a named stream",
        "{0} is not a deterministic named stream. Use world.Rng[\"name\"] so draws are part of the world state.");

    public static readonly DiagnosticDescriptor ViewWrites = Error("OG0008", "View code changes simulation state",
        "View code cannot change simulation state: {0}. Views read state and send commands; use entity.Send.<Command>(...) instead.");

    public static readonly DiagnosticDescriptor NoCodec = Error("OG0009", "Synced type has no codec",
        "Type '{0}' cannot be synced. Use a number, bool, string, enum, Fixed, FVec2, EntityId, Timed<T>, SyncList<T> (as a get-only property) or a [SyncedStruct].");

    public static readonly DiagnosticDescriptor EntityShape = Error("OG0010", "Entity declaration is not supported",
        "{0}");

    public static readonly DiagnosticDescriptor TooManyProperties = Error("OG0011", "Too many synced properties",
        "Entity '{0}' has {1} synced properties; the limit is 64. Group related values into a [SyncedStruct].");

    public static readonly DiagnosticDescriptor PropertyShape = Error("OG0012", "Synced member is declared wrongly",
        "{0}");

    public static readonly DiagnosticDescriptor CommandShape = Error("OG0013", "Command is declared wrongly",
        "{0}");

    public static readonly DiagnosticDescriptor ExternalApi = new("OG0015", "API not known to be deterministic",
        "Sim code calls '{0}', which is outside this project and not known to be deterministic. Use a sim-safe API, or vouch for the library with [assembly: SimTrust(\"Namespace\")] or onegame_sim_trusted in .editorconfig.",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Culture = Error("OG0016", "Culture-sensitive text in sim code",
        "{0} follows the machine's culture, so it gives different results on different machines. Pass CultureInfo.InvariantCulture, or compare with StringComparison.Ordinal.");

    public static readonly DiagnosticDescriptor KeyShape = Error("OG0017", "Owner or team key is declared wrongly",
        "{0}");

    public static readonly DiagnosticDescriptor VisibilityShape = Error("OG0018", "Visibility has nothing to match against",
        "{0}");

    public static readonly DiagnosticDescriptor ViewReadsUnreceived = Error("OG0019", "View reads a member that replicas never receive",
        "View code reads '{0}', which is Hidden: the authority never sends it, so a replica always holds the default. Make the member visible to the audience that needs it, or compute what the view shows on the authority and sync that.");

    public static readonly DiagnosticDescriptor MigrationShape = Error("OG0014", "Migration is declared wrongly",
        "{0}");
}

/// <summary>A diagnostic captured as plain data so the incremental pipeline can compare it.</summary>
internal sealed class DiagData : IEquatable<DiagData>
{
    public DiagData(string id, string path, int start, int length, int sl, int sc, int el, int ec, string[] args)
    {
        Id = id;
        Path = path;
        Start = start;
        Length = length;
        Sl = sl;
        Sc = sc;
        El = el;
        Ec = ec;
        Args = args;
    }

    public string Id { get; }

    public string Path { get; }

    public int Start { get; }

    public int Length { get; }

    public int Sl { get; }

    public int Sc { get; }

    public int El { get; }

    public int Ec { get; }

    public string[] Args { get; }

    public static DiagData Create(DiagnosticDescriptor rule, Location? location, params string[] args)
    {
        if (location is null || !location.IsInSource)
        {
            return new DiagData(rule.Id, string.Empty, 0, 0, 0, 0, 0, 0, args);
        }

        var span = location.GetLineSpan();
        return new DiagData(rule.Id, location.SourceTree?.FilePath ?? string.Empty, location.SourceSpan.Start, location.SourceSpan.Length,
            span.StartLinePosition.Line, span.StartLinePosition.Character, span.EndLinePosition.Line, span.EndLinePosition.Character, args);
    }

    public Diagnostic ToDiagnostic(DiagnosticDescriptor rule)
    {
        Location location = Path.Length == 0
            ? Location.None
            : Location.Create(Path, new Microsoft.CodeAnalysis.Text.TextSpan(Start, Length),
                new Microsoft.CodeAnalysis.Text.LinePositionSpan(
                    new Microsoft.CodeAnalysis.Text.LinePosition(Sl, Sc), new Microsoft.CodeAnalysis.Text.LinePosition(El, Ec)));
        return Diagnostic.Create(rule, location, Array.ConvertAll(Args, a => (object)a));
    }

    public bool Equals(DiagData? other)
    {
        if (other is null || Id != other.Id || Path != other.Path || Start != other.Start || Length != other.Length
            || Sl != other.Sl || Sc != other.Sc || El != other.El || Ec != other.Ec || Args.Length != other.Args.Length)
        {
            return false;
        }

        for (var i = 0; i < Args.Length; i++)
        {
            if (Args[i] != other.Args[i])
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => Equals(obj as DiagData);

    public override int GetHashCode() => (Id.GetHashCode() * 31) + Start;
}

/// <summary>Structural equality for an array, so pipeline outputs compare by value.</summary>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
{
    private readonly T[]? _items;

    public EquatableArray(T[] items) => _items = items;

    public int Length => _items?.Length ?? 0;

    public bool Equals(EquatableArray<T> other)
    {
        if (Length != other.Length)
        {
            return false;
        }

        var comparer = EqualityComparer<T>.Default;
        for (var i = 0; i < Length; i++)
        {
            if (!comparer.Equals(_items![i], other._items![i]))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        var h = 17;
        for (var i = 0; i < Length; i++)
        {
            h = (h * 31) + (_items![i]?.GetHashCode() ?? 0);
        }

        return h;
    }

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)(_items ?? Array.Empty<T>())).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
