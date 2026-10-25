using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace OneGame.Generators;

/// <summary>
/// Checks commands marked <c>[Command(Predicted = true)]</c>. A predicted handler runs on a replica, which holds only what its authority is
/// allowed to send, so it may not read members the replica never receives (OG0020), draw from a random stream unless it declares
/// <c>UsesRandom</c> (OG0021), or write members the replica never holds (OG0022). Calls into the same compilation are followed.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PredictionAnalyzer : DiagnosticAnalyzer
{
    private const int MaxDepth = 8;

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.PredictedReadsUnreceived, Rules.PredictedDrawsRandom, Rules.PredictedWritesHidden);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var scanner = new Scanner(start.Compilation);
            start.RegisterSymbolAction(scanner.Analyze, SymbolKind.Method);
        });
    }

    private readonly struct Hit
    {
        public Hit(DiagnosticDescriptor rule, string text, Location? location)
        {
            Rule = rule;
            Text = text;
            Location = location;
        }

        public DiagnosticDescriptor Rule { get; }

        public string Text { get; }

        public Location? Location { get; }
    }

    private sealed class Scanner
    {
        private readonly Compilation _compilation;

        public Scanner(Compilation compilation) => _compilation = compilation;

    public void Analyze(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;
        if (Codecs.FindAttribute(method, "CommandAttribute") is not { } command
            || !Codecs.HasAttribute(method.ContainingType, "EntityAttribute")
            || !HasFlag(command, "Predicted"))
        {
            return;
        }

        var usesRandom = HasFlag(command, "UsesRandom");
        var ownerOnly = HasFlag(command, "OwnerOnly");
        var hits = new List<Hit>();
        Scan(method, method.ContainingType, ownerOnly, usesRandom, new HashSet<ISymbol>(SymbolEqualityComparer.Default), 0, null, hits);
        var reported = new HashSet<(int, string)>();
        foreach (var hit in hits)
        {
            var location = hit.Location ?? method.Locations.FirstOrDefault() ?? Location.None;
            if (reported.Add((location.SourceSpan.Start, hit.Text)))
            {
                context.ReportDiagnostic(Diagnostic.Create(hit.Rule, location, method.Name, hit.Text));
            }
        }
    }

    public static bool HasFlag(AttributeData data, string name)
        => data.NamedArguments.Any(a => a.Key == name && a.Value.Value is true);

    public static int Level(AttributeData? data)
    {
        if (data is not null)
        {
            foreach (var arg in data.NamedArguments)
            {
                if (arg.Key == "Visibility" && arg.Value.Value is int v)
                {
                    return v;
                }
            }
        }

        return 0;
    }

    /// <summary>Visibility of a synced property: the stricter of its own and its entity's. 0 everyone, 1 owner, 2 team, 3 hidden.</summary>
    public static int EffectiveLevel(IPropertySymbol property)
    {
        var own = Level(Codecs.FindAttribute(property, "SyncedAttribute"));
        var entity = Level(Codecs.FindAttribute(property.ContainingType, "EntityAttribute"));
        return own == 3 || entity == 3 ? 3 : System.Math.Max(own, entity);
    }

    private void Scan(IMethodSymbol method, INamedTypeSymbol commandType, bool ownerOnly, bool usesRandom,
        HashSet<ISymbol> visited, int depth, Location? callSite, List<Hit> hits)
    {
        if (depth > MaxDepth || !visited.Add(method.OriginalDefinition))
        {
            return;
        }

        foreach (var reference in method.DeclaringSyntaxReferences)
        {
            var syntax = reference.GetSyntax();
            var model = _compilation.GetSemanticModel(reference.SyntaxTree);
            var root = model.GetOperation(syntax);
            if (root is null)
            {
                continue;
            }

            foreach (var op in root.DescendantsAndSelf())
            {
                var location = callSite ?? op.Syntax.GetLocation();
                var via = callSite is null ? string.Empty : " (through '" + method.Name + "')";
                switch (op)
                {
                    case IPropertyReferenceOperation pr when pr.Property.IsIndexer && pr.Property.ContainingType is { Name: "RngStreams" } rs && Codecs.IsInCore(rs):
                        if (!usesRandom)
                        {
                            hits.Add(new Hit(Rules.PredictedDrawsRandom, "it takes a random stream" + via, location));
                        }

                        break;
                    case IPropertyReferenceOperation pr when Codecs.HasAttribute(pr.Property, "SyncedAttribute"):
                        CheckSynced(pr, commandType, ownerOnly, via, location, hits);
                        break;
                    case IPropertyReferenceOperation pr when pr.Property.GetMethod is { } getter && !pr.Property.IsIndexer:
                        Scan(getter, commandType, ownerOnly, usesRandom, visited, depth + 1, callSite ?? op.Syntax.GetLocation(), hits);
                        break;
                    case IInvocationOperation call:
                        if (call.TargetMethod.ContainingType is { Name: "Rng" } rng && Codecs.IsInCore(rng))
                        {
                            if (!usesRandom)
                            {
                                hits.Add(new Hit(Rules.PredictedDrawsRandom, "it draws " + call.TargetMethod.Name + via, location));
                            }
                        }
                        else if (!call.TargetMethod.DeclaringSyntaxReferences.IsDefaultOrEmpty)
                        {
                            Scan(call.TargetMethod, commandType, ownerOnly, usesRandom, visited, depth + 1, callSite ?? op.Syntax.GetLocation(), hits);
                        }

                        break;
                }
            }
        }
    }

    }

    private static void CheckSynced(IPropertyReferenceOperation pr, INamedTypeSymbol commandType, bool ownerOnly, string via, Location location, List<Hit> hits)
    {
        var level = Scanner.EffectiveLevel(pr.Property);
        var name = pr.Property.ContainingType.Name + "." + pr.Property.Name;
        var (writes, reads) = Access(pr);
        if (level == 3)
        {
            if (writes)
            {
                hits.Add(new Hit(Rules.PredictedWritesHidden, "it writes '" + name + "'" + via, location));
            }
            else
            {
                hits.Add(new Hit(Rules.PredictedReadsUnreceived, "it reads '" + name + "', which is Hidden" + via, location));
            }

            return;
        }

        // Owner and team data reaches only that audience. An owner-only command on its own entity is run by the one replica that holds it.
        var ownData = ownerOnly && SymbolEqualityComparer.Default.Equals(pr.Property.ContainingType, commandType)
            && (pr.Instance is null || pr.Instance is IInstanceReferenceOperation);
        if (reads && level != 0 && !ownData)
        {
            hits.Add(new Hit(Rules.PredictedReadsUnreceived,
                "it reads '" + name + "', which only its " + (level == 1 ? "owner" : "team") + " receives, and the command is not OwnerOnly on its own entity" + via, location));
        }
    }

    private static (bool Writes, bool Reads) Access(IPropertyReferenceOperation pr)
    {
        switch (pr.Parent)
        {
            case ISimpleAssignmentOperation simple when simple.Target == pr:
                return (true, false);
            case ICompoundAssignmentOperation compound when compound.Target == pr:
                return (true, true);
            case IIncrementOrDecrementOperation inc when inc.Target == pr:
                return (true, true);
            default:
                return (false, true);
        }
    }
}
