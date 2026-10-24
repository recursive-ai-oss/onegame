using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace OneGame.Generators;

/// <summary>
/// Enforces the sim and view rules. Sim code is every member of a type marked [Entity], [Sim] or implementing ISimSystem, except
/// members marked [View], plus any member marked [Sim]. View code is every member marked [View] and every member of a type marked [View].
/// Calls into other methods are checked only for the view rule (a view may not call anything that changes state); the sim rules
/// look at the code written in the sim member itself.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SimRulesAnalyzer : DiagnosticAnalyzer
{
    private enum Scope
    {
        None,
        Sim,
        View,
    }

    private static readonly OperationKind[] FloatKinds =
    [
        OperationKind.Literal, OperationKind.Binary, OperationKind.Unary, OperationKind.Conversion, OperationKind.Invocation,
        OperationKind.PropertyReference, OperationKind.FieldReference, OperationKind.LocalReference, OperationKind.ParameterReference,
        OperationKind.SimpleAssignment, OperationKind.CompoundAssignment, OperationKind.Increment, OperationKind.Decrement,
        OperationKind.ObjectCreation, OperationKind.DefaultValue,
    ];

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        Rules.FloatInState, Rules.FloatInSim, Rules.UnorderedIteration, Rules.Clock, Rules.Threads, Rules.InputOutput,
        Rules.UnnamedRandom, Rules.ViewWrites, Rules.NoCodec);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var state = new State(start.Compilation);
            start.RegisterSymbolAction(State.AnalyzeType, SymbolKind.NamedType);
            start.RegisterSymbolAction(State.AnalyzeMemberDeclaration, SymbolKind.Field, SymbolKind.Property, SymbolKind.Method);
            start.RegisterOperationAction(State.AnalyzeFloat, FloatKinds);
            start.RegisterOperationAction(State.AnalyzeMember, OperationKind.PropertyReference, OperationKind.FieldReference,
                OperationKind.MethodReference, OperationKind.Invocation, OperationKind.ObjectCreation);
            start.RegisterOperationAction(State.AnalyzeForEach, OperationKind.Loop);
            start.RegisterOperationAction(State.AnalyzeLockAndAwait, OperationKind.Lock, OperationKind.Await);
            start.RegisterOperationAction(State.AnalyzeLinq, OperationKind.Invocation);
            start.RegisterOperationAction(state.AnalyzeViewWrite, OperationKind.SimpleAssignment, OperationKind.CompoundAssignment,
                OperationKind.Increment, OperationKind.Decrement);
            start.RegisterOperationAction(state.AnalyzeViewCall, OperationKind.Invocation);
        });
    }

    private sealed class State
    {
        private readonly Compilation _compilation;
        private readonly ConcurrentDictionary<ISymbol, bool> _writes = new(SymbolEqualityComparer.Default);

        public State(Compilation compilation) => _compilation = compilation;

        // ---- scope -------------------------------------------------------------------------------------------------

        private static Scope ScopeOf(ISymbol? containing)
        {
            var member = containing;
            while (member is IMethodSymbol { MethodKind: MethodKind.AnonymousFunction or MethodKind.LocalFunction })
            {
                member = member.ContainingSymbol;
            }

            if (member is null)
            {
                return Scope.None;
            }

            var primary = member is IMethodSymbol { AssociatedSymbol: { } associated } ? associated : member;
            if (Codecs.HasAttribute(primary, "ViewAttribute") || Codecs.HasAttribute(member, "ViewAttribute"))
            {
                return Scope.View;
            }

            if (Codecs.HasAttribute(primary, "SimAttribute") || Codecs.HasAttribute(member, "SimAttribute"))
            {
                return Scope.Sim;
            }

            for (var t = member as INamedTypeSymbol ?? member.ContainingType; t is not null; t = t.ContainingType)
            {
                if (Codecs.HasAttribute(t, "ViewAttribute"))
                {
                    return Scope.View;
                }

                if (Codecs.HasAttribute(t, "EntityAttribute") || Codecs.HasAttribute(t, "SimAttribute") || ImplementsSimSystem(t))
                {
                    return Scope.Sim;
                }
            }

            return Scope.None;
        }

        private static bool ImplementsSimSystem(INamedTypeSymbol t)
            => t.AllInterfaces.Any(i => i.Name == "ISimSystem" && Codecs.IsInCore(i));

        // ---- synced state types (OG0001, OG0009) -----------------------------------------------------------------

        public static void AnalyzeType(SymbolAnalysisContext context)
        {
            var type = (INamedTypeSymbol)context.Symbol;
            if (Codecs.HasAttribute(type, "EntityAttribute"))
            {
                foreach (var member in type.GetMembers())
                {
                    if (member is IPropertySymbol p && Codecs.HasAttribute(p, "SyncedAttribute"))
                    {
                        CheckStateType(context, p, p.Type, allowList: true);
                    }
                    else if (member is IMethodSymbol m && Codecs.HasAttribute(m, "CommandAttribute"))
                    {
                        foreach (var parameter in m.Parameters)
                        {
                            if (Codecs.IsFloating(parameter.Type))
                            {
                                context.ReportDiagnostic(Diagnostic.Create(Rules.FloatInState, Where(parameter), $"{m.Name}({parameter.Name})"));
                            }
                        }
                    }
                }
            }
            else if (Codecs.HasAttribute(type, "SyncedStructAttribute") || Codecs.HasAttribute(type, "HotDataAttribute"))
            {
                foreach (var member in type.GetMembers())
                {
                    if (member.IsStatic || member.IsImplicitlyDeclared || member.DeclaredAccessibility != Accessibility.Public)
                    {
                        continue;
                    }

                    if (member is IFieldSymbol { IsConst: false } f)
                    {
                        CheckStateType(context, f, f.Type, allowList: false);
                    }
                    else if (member is IPropertySymbol { IsIndexer: false } p)
                    {
                        CheckStateType(context, p, p.Type, allowList: false);
                    }
                }
            }
        }

        private static void CheckStateType(SymbolAnalysisContext context, ISymbol symbol, ITypeSymbol type, bool allowList)
        {
            if (Codecs.IsFloating(type))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.FloatInState, Where(symbol), symbol.Name));
            }
            else if (!Codecs.IsSyncable(type, allowList))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.NoCodec, Where(symbol), type.ToDisplayString()));
            }
        }

        private static Location Where(ISymbol symbol)
        {
            foreach (var l in symbol.Locations)
            {
                if (l.IsInSource && !(l.SourceTree?.FilePath ?? string.Empty).EndsWith(".g.cs", StringComparison.Ordinal))
                {
                    return l;
                }
            }

            return Location.None;
        }

        // ---- declarations in sim types ----------------------------------------------------------------------------

        public static void AnalyzeMemberDeclaration(SymbolAnalysisContext context)
        {
            var symbol = context.Symbol;
            if (ScopeOf(symbol) != Scope.Sim || Codecs.HasAttribute(symbol, "SyncedAttribute"))
            {
                return;
            }

            switch (symbol)
            {
                case IFieldSymbol { IsImplicitlyDeclared: false } f:
                    CheckDeclaredType(context, f, f.Type);
                    break;
                case IPropertySymbol { IsImplicitlyDeclared: false } p:
                    CheckDeclaredType(context, p, p.Type);
                    break;
                case IMethodSymbol { MethodKind: MethodKind.Ordinary or MethodKind.Constructor } m:
                    CheckDeclaredType(context, m, m.ReturnType);
                    if (!Codecs.HasAttribute(m, "CommandAttribute"))
                    {
                        // A command's parameters are synced input, reported as OG0001 instead.
                        foreach (var parameter in m.Parameters)
                        {
                            CheckDeclaredType(context, parameter, parameter.Type);
                        }
                    }

                    if (m.IsAsync)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Rules.Threads, Where(m), $"async method '{m.Name}'"));
                    }

                    break;
            }
        }

        private static void CheckDeclaredType(SymbolAnalysisContext context, ISymbol owner, ITypeSymbol type)
        {
            if (Codecs.IsFloatScalar(type))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.FloatInSim, Where(owner), $"'{owner.Name}' is declared {type.ToDisplayString()}"));
            }
            else if (type is INamedTypeSymbol named && Banned.Match(named) is { } rule)
            {
                context.ReportDiagnostic(Diagnostic.Create(rule, Where(owner), type.ToDisplayString()));
            }
        }

        // ---- floats in sim code (OG0002) --------------------------------------------------------------------------

        public static void AnalyzeFloat(OperationAnalysisContext context)
        {
            var op = context.Operation;
            if (op.Type is null || !Codecs.IsFloatScalar(op.Type) || op.IsImplicit && op.Kind != OperationKind.Conversion
                || ScopeOf(context.ContainingSymbol) != Scope.Sim)
            {
                return;
            }

            // Report the outermost floating-point expression once.
            for (var p = op.Parent; p is not null && p.Type is not null && Codecs.IsFloatScalar(p.Type); p = p.Parent)
            {
                if (Array.IndexOf(FloatKinds, p.Kind) >= 0 && !p.IsImplicit)
                {
                    return;
                }
            }

            context.ReportDiagnostic(Diagnostic.Create(Rules.FloatInSim, op.Syntax.GetLocation(), Snippet(op)));
        }

        private static string Snippet(IOperation op)
        {
            var text = op.Syntax.ToString().Replace('\n', ' ').Replace('\r', ' ');
            return text.Length > 40 ? text.Substring(0, 40) + "..." : text;
        }

        // ---- banned members (OG0004-OG0007) ------------------------------------------------------------------------

        public static void AnalyzeMember(OperationAnalysisContext context)
        {
            ISymbol? symbol = context.Operation switch
            {
                IPropertyReferenceOperation p => p.Property,
                IFieldReferenceOperation f => f.Field,
                IMethodReferenceOperation m => m.Method,
                IInvocationOperation i => i.TargetMethod,
                IObjectCreationOperation c => c.Constructor,
                _ => null,
            };
            if (symbol is null || ScopeOf(context.ContainingSymbol) != Scope.Sim)
            {
                return;
            }

            if (Banned.Match(symbol) is { } rule)
            {
                var type = (symbol.ContainingType ?? (symbol as INamedTypeSymbol))?.Name;
                var shown = symbol is IMethodSymbol { MethodKind: MethodKind.Constructor } ? "new " + type : type + "." + symbol.Name;
                context.ReportDiagnostic(Diagnostic.Create(rule, context.Operation.Syntax.GetLocation(), shown));
            }
        }

        public static void AnalyzeLockAndAwait(OperationAnalysisContext context)
        {
            if (ScopeOf(context.ContainingSymbol) == Scope.Sim)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.Threads, context.Operation.Syntax.GetLocation(),
                    context.Operation.Kind == OperationKind.Lock ? "lock" : "await"));
            }
        }

        // ---- unordered iteration (OG0003) --------------------------------------------------------------------------

        public static void AnalyzeForEach(OperationAnalysisContext context)
        {
            if (context.Operation is IForEachLoopOperation loop && ScopeOf(context.ContainingSymbol) == Scope.Sim
                && Banned.IsUnordered(Unwrap(loop.Collection).Type))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.UnorderedIteration, loop.Collection.Syntax.GetLocation(),
                    loop.Collection.Type!.ToDisplayString()));
            }
        }

        public static void AnalyzeLinq(OperationAnalysisContext context)
        {
            var invocation = (IInvocationOperation)context.Operation;
            var method = invocation.TargetMethod;
            if (ScopeOf(context.ContainingSymbol) != Scope.Sim)
            {
                return;
            }

            ITypeSymbol? receiver = null;
            if (method.IsExtensionMethod && method.ContainingType is { Name: "Enumerable" or "ImmutableArray" or "CollectionExtensions" } && invocation.Arguments.Length > 0)
            {
                receiver = Unwrap(invocation.Arguments[0].Value).Type;
            }
            else if (method.Name == "GetEnumerator" && invocation.Instance is not null)
            {
                receiver = invocation.Instance.Type;
            }

            if (Banned.IsUnordered(receiver))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.UnorderedIteration, invocation.Syntax.GetLocation(), receiver!.ToDisplayString()));
            }
        }

        private static IOperation Unwrap(IOperation op)
        {
            while (op is IConversionOperation { IsImplicit: true } c)
            {
                op = c.Operand;
            }

            return op;
        }

        // ---- views writing state (OG0008) --------------------------------------------------------------------------

        public void AnalyzeViewWrite(OperationAnalysisContext context)
        {
            if (ScopeOf(context.ContainingSymbol) != Scope.View)
            {
                return;
            }

            if (WriteTarget(context.Operation, new HashSet<ISymbol>(SymbolEqualityComparer.Default)) is { } description)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.ViewWrites, context.Operation.Syntax.GetLocation(), description));
            }
        }

        public void AnalyzeViewCall(OperationAnalysisContext context)
        {
            if (ScopeOf(context.ContainingSymbol) != Scope.View)
            {
                return;
            }

            var method = ((IInvocationOperation)context.Operation).TargetMethod;
            if (WritesState(method))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.ViewWrites, context.Operation.Syntax.GetLocation(),
                    $"'{method.ContainingType?.Name}.{method.Name}' changes state"));
            }
        }

        /// <summary>If the assignment or increment changes sim state, says what it changes.</summary>
        private string? WriteTarget(IOperation op, HashSet<ISymbol> inProgress)
        {
            var target = op switch
            {
                IAssignmentOperation a => a.Target,
                IIncrementOrDecrementOperation i => i.Target,
                _ => null,
            };
            if (target is IPropertyReferenceOperation pr)
            {
                if (Codecs.HasAttribute(pr.Property, "SyncedAttribute"))
                {
                    return $"it assigns the synced property '{pr.Property.Name}'";
                }

                if (pr.Property.SetMethod is { } setter && WritesState(setter, inProgress))
                {
                    return $"assigning '{pr.Property.Name}' changes state";
                }
            }

            return null;
        }

        private bool WritesState(IMethodSymbol method) => WritesState(method, new HashSet<ISymbol>(SymbolEqualityComparer.Default));

        private bool WritesState(IMethodSymbol method, HashSet<ISymbol> inProgress)
        {
            method = method.OriginalDefinition;
            if (Codecs.HasAttribute(method, "CommandAttribute") || Codecs.HasAttribute(method, "MutatesStateAttribute"))
            {
                return true;
            }

            if (method.AssociatedSymbol is { } associated && Codecs.HasAttribute(associated, "SyncedAttribute") && method.MethodKind == MethodKind.PropertySet)
            {
                return true;
            }

            if (ScopeOf(method) == Scope.View || method.DeclaringSyntaxReferences.IsDefaultOrEmpty)
            {
                return false;
            }

            if (_writes.TryGetValue(method, out var known))
            {
                return known;
            }

            // A method already being analyzed counts as "no" so recursion ends; results cut short that way are not cached.
            if (!inProgress.Add(method))
            {
                return false;
            }

            var topLevel = inProgress.Count == 1;
            var result = false;
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
                    if (op is IAssignmentOperation or IIncrementOrDecrementOperation && WriteTarget(op, inProgress) is not null
                        || op is IInvocationOperation call && WritesState(call.TargetMethod, inProgress))
                    {
                        result = true;
                        break;
                    }
                }

                if (result)
                {
                    break;
                }
            }

            inProgress.Remove(method);
            if (result || topLevel)
            {
                _writes[method] = result;
            }

            return result;
        }
    }
}
