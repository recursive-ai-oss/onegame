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
/// Sim code is checked through calls: a call into a method of the same compilation is followed to the offending line, and a call
/// into another assembly must be on the framework allow list, marked [SimSafe] or trusted (OG0015). A view may not call anything
/// that changes state (OG0008).
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
        Rules.UnnamedRandom, Rules.ViewWrites, Rules.NoCodec, Rules.ExternalApi, Rules.Culture);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var state = new State(start.Compilation, start.Options);
            start.RegisterSymbolAction(State.AnalyzeType, SymbolKind.NamedType);
            start.RegisterSymbolAction(State.AnalyzeMemberDeclaration, SymbolKind.Field, SymbolKind.Property, SymbolKind.Method);
            start.RegisterOperationAction(State.AnalyzeFloat, FloatKinds);
            start.RegisterOperationAction(state.AnalyzeSimOperation, OperationKind.PropertyReference, OperationKind.FieldReference,
                OperationKind.MethodReference, OperationKind.Invocation, OperationKind.ObjectCreation, OperationKind.Loop,
                OperationKind.Lock, OperationKind.Await, OperationKind.Binary, OperationKind.Unary, OperationKind.Conversion,
                OperationKind.InterpolatedString);
            start.RegisterOperationAction(state.AnalyzeViewWrite, OperationKind.SimpleAssignment, OperationKind.CompoundAssignment,
                OperationKind.Increment, OperationKind.Decrement);
            start.RegisterOperationAction(state.AnalyzeViewCall, OperationKind.Invocation);
        });
    }

    private sealed class State
    {
        private readonly Compilation _compilation;
        private readonly ConcurrentDictionary<ISymbol, bool> _writes = new(SymbolEqualityComparer.Default);

        private readonly AnalyzerOptions _options;
        private readonly ConcurrentDictionary<ISymbol, Found?> _chains = new(SymbolEqualityComparer.Default);
        private readonly ConcurrentDictionary<SyntaxTree, string[]> _trusted = new();
        private readonly string[] _assemblyTrusted;

        public State(Compilation compilation, AnalyzerOptions options)
        {
            _compilation = compilation;
            _options = options;
            _assemblyTrusted = compilation.Assembly.GetAttributes()
                .Where(a => Codecs.IsCoreAttribute(a, "SimTrustAttribute") && a.ConstructorArguments.Length == 1)
                .Select(a => a.ConstructorArguments[0].Value as string ?? string.Empty)
                .Where(n => n.Length > 0)
                .ToArray();
        }

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

        // ---- rules checked in sim code, through calls (OG0003-OG0007, OG0015, OG0016) ------------------------------

        private sealed class Hit
        {
            public Hit(DiagnosticDescriptor rule, string text, SyntaxNode? at = null)
            {
                Rule = rule;
                Text = text;
                At = at;
            }

            public DiagnosticDescriptor Rule { get; }

            public string Text { get; }

            public SyntaxNode? At { get; }
        }

        /// <summary>A violation found below a call: the rule, and the names from the called method down to the offending code.</summary>
        private sealed class Found
        {
            public Found(DiagnosticDescriptor rule, string[] path)
            {
                Rule = rule;
                Path = path;
            }

            public DiagnosticDescriptor Rule { get; }

            public string[] Path { get; }
        }

        private sealed class Walk
        {
            public readonly HashSet<ISymbol> InProgress = new(SymbolEqualityComparer.Default);
            public int Cuts;
        }

        public void AnalyzeSimOperation(OperationAnalysisContext context)
        {
            if (ScopeOf(context.ContainingSymbol) != Scope.Sim)
            {
                return;
            }

            var op = context.Operation;
            if (HardHit(op) is { } hit)
            {
                context.ReportDiagnostic(Diagnostic.Create(hit.Rule, (hit.At ?? op.Syntax).GetLocation(), hit.Text));
                return;
            }

            var tree = op.Syntax.SyntaxTree;
            var callee = CalledSymbol(op);
            if (callee is null)
            {
                return;
            }

            if (IsInSource(callee))
            {
                foreach (var body in Bodies(callee))
                {
                    if (SimAllow.IsMarkedSafe(body) || SimAllow.IsTrusted(body, Trusted(tree)))
                    {
                        continue;
                    }

                    if (Summarize(body, new Walk()) is { } found)
                    {
                        var chain = string.Join(" \u2192 ", new[] { Display(MemberOf(context.ContainingSymbol)) }.Concat(found.Path));
                        context.ReportDiagnostic(Diagnostic.Create(found.Rule, op.Syntax.GetLocation(), chain));
                        return;
                    }
                }

                return;
            }

            if (ExternalHit(op, callee, tree) is { } name)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.ExternalApi, op.Syntax.GetLocation(), name));
            }
        }

        /// <summary>Follows a method of this compilation, and everything it calls in this compilation, looking for a sim-rule violation.</summary>
        private Found? Summarize(IMethodSymbol method, Walk walk)
        {
            method = method.OriginalDefinition;
            if (_chains.TryGetValue(method, out var cached))
            {
                return cached;
            }

            // Sim members are checked where they are written; reporting them again at every call site would only repeat.
            if (method.DeclaringSyntaxReferences.IsDefaultOrEmpty || ScopeOf(method) == Scope.Sim)
            {
                return null;
            }

            // A method already being analyzed counts as clean so recursion ends. Such a result is not cached unless it found something.
            if (!walk.InProgress.Add(method))
            {
                walk.Cuts++;
                return null;
            }

            var cuts = walk.Cuts;
            Found? hard = method.IsAsync ? new Found(Rules.Threads, [$"async method '{method.Name}'"]) : null;
            Found? soft = null;
            foreach (var reference in method.DeclaringSyntaxReferences)
            {
                if (hard is not null)
                {
                    break;
                }

                var tree = reference.SyntaxTree;
                var root = _compilation.GetSemanticModel(tree).GetOperation(reference.GetSyntax());
                if (root is null)
                {
                    continue;
                }

                foreach (var op in root.DescendantsAndSelf())
                {
                    var found = Inspect(op, walk, tree);
                    if (found is null)
                    {
                        continue;
                    }

                    if (found.Rule != Rules.ExternalApi)
                    {
                        hard = found;
                        break;
                    }

                    soft ??= found;
                }
            }

            walk.InProgress.Remove(method);
            var result = hard ?? soft;
            if (result is not null)
            {
                result = new Found(result.Rule, [Display(method), .. result.Path]);
            }

            if (result is not null || walk.Cuts == cuts)
            {
                _chains[method] = result;
            }

            return result;
        }

        private Found? Inspect(IOperation op, Walk walk, SyntaxTree tree)
        {
            if (IsFloatOp(op))
            {
                return new Found(Rules.FloatInSim, [$"float expression '{Snippet(op)}'"]);
            }

            if (HardHit(op) is { } hit)
            {
                return new Found(hit.Rule, [hit.Text]);
            }

            var callee = CalledSymbol(op);
            if (callee is null)
            {
                return null;
            }

            if (IsInSource(callee))
            {
                foreach (var body in Bodies(callee))
                {
                    if (SimAllow.IsMarkedSafe(body) || SimAllow.IsTrusted(body, Trusted(tree)))
                    {
                        continue;
                    }

                    if (Summarize(body, walk) is { } sub)
                    {
                        return sub;
                    }
                }

                return null;
            }

            return ExternalHit(op, callee, tree) is { } name ? new Found(Rules.ExternalApi, [name]) : null;
        }

        private static bool IsFloatOp(IOperation op)
            => op.Type is not null && Codecs.IsFloatScalar(op.Type) && (!op.IsImplicit || op.Kind == OperationKind.Conversion)
            && Array.IndexOf(FloatKinds, op.Kind) >= 0;

        private bool IsInSource(ISymbol symbol) => SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, _compilation.Assembly);

        private static IEnumerable<IMethodSymbol> Bodies(ISymbol symbol)
        {
            switch (symbol)
            {
                case IMethodSymbol m:
                    yield return m;
                    break;
                case IPropertySymbol p:
                    if (p.GetMethod is { } g)
                    {
                        yield return g;
                    }

                    if (p.SetMethod is { } s)
                    {
                        yield return s;
                    }

                    break;
            }
        }

        private static ISymbol? CalledSymbol(IOperation op) => op switch
        {
            IInvocationOperation i => i.TargetMethod,
            IObjectCreationOperation c => c.Constructor,
            IPropertyReferenceOperation p => p.Property,
            IMethodReferenceOperation m => m.Method,
            IBinaryOperation { OperatorMethod: { } m } => m,
            IUnaryOperation { OperatorMethod: { } m } => m,
            IConversionOperation { OperatorMethod: { } m } => m,
            IFieldReferenceOperation f => f.Field,
            _ => null,
        };

        private static ISymbol MemberOf(ISymbol? symbol)
        {
            while (symbol is IMethodSymbol { MethodKind: MethodKind.AnonymousFunction or MethodKind.LocalFunction } m)
            {
                symbol = m.ContainingSymbol;
            }

            return symbol!;
        }

        private static string Display(ISymbol symbol)
        {
            if (symbol is IMethodSymbol { MethodKind: MethodKind.Constructor } ctor)
            {
                return "new " + ctor.ContainingType.Name;
            }

            var name = symbol is IMethodSymbol { AssociatedSymbol: { } associated } ? associated.Name : symbol.Name;
            return symbol.ContainingType is { } type ? type.Name + "." + name : name;
        }

        private string[] Trusted(SyntaxTree tree) => _trusted.GetOrAdd(tree, t =>
        {
            var list = new List<string>(_assemblyTrusted);
            var provider = _options.AnalyzerConfigOptionsProvider;
            foreach (var options in new[] { provider.GlobalOptions, provider.GetOptions(t) })
            {
                if (options.TryGetValue("onegame_sim_trusted", out var value))
                {
                    list.AddRange(value.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries).Select(v => v.Trim()).Where(v => v.Length > 0));
                }
            }

            return list.ToArray();
        });

        /// <summary>The name of an API outside the compilation that nothing vouches for, or null.</summary>
        private string? ExternalHit(IOperation op, ISymbol callee, SyntaxTree tree)
        {
            if (callee is IFieldSymbol || Banned.Match(callee) is not null || SimAllow.IsFrameworkSafe(callee) || SimAllow.IsMarkedSafe(callee)
                || SimAllow.IsTrusted(callee, Trusted(tree)))
            {
                return null;
            }

            return Display(callee);
        }

        /// <summary>A violation that is certain wherever it appears in sim code: a banned member, an unordered loop, a lock, culture-sensitive text.</summary>
        private static Hit? HardHit(IOperation op)
        {
            switch (op)
            {
                case IForEachLoopOperation loop:
                    return Banned.IsUnordered(Unwrap(loop.Collection).Type)
                        ? new Hit(Rules.UnorderedIteration, loop.Collection.Type!.ToDisplayString(), loop.Collection.Syntax)
                        : null;
                case ILockOperation:
                    return new Hit(Rules.Threads, "lock");
                case IAwaitOperation:
                    return new Hit(Rules.Threads, "await");
                case IInterpolatedStringOperation text:
                    return InterpolationText(text) is { } holes ? new Hit(Rules.Culture, holes) : null;
                case IBinaryOperation { OperatorKind: BinaryOperatorKind.Add, Type.SpecialType: SpecialType.System_String } concat:
                    foreach (var operand in new[] { concat.LeftOperand, concat.RightOperand })
                    {
                        if (CultureRules.IsCultureNumber(Unwrap(operand).Type))
                        {
                            return new Hit(Rules.Culture, "string concatenation of " + Unwrap(operand).Type!.Name);
                        }
                    }

                    break;
            }

            var symbol = CalledSymbol(op);
            if (symbol is null)
            {
                return null;
            }

            if (Banned.Match(symbol) is { } rule)
            {
                var type = (symbol.ContainingType ?? (symbol as INamedTypeSymbol))?.Name;
                return new Hit(rule, symbol is IMethodSymbol { MethodKind: MethodKind.Constructor } ? "new " + type : type + "." + symbol.Name);
            }

            if (op is IInvocationOperation invocation)
            {
                var method = invocation.TargetMethod;
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
                    return new Hit(Rules.UnorderedIteration, receiver!.ToDisplayString());
                }
            }

            if (symbol.ContainingAssembly is not null && CultureRules.Match(symbol, op as IInvocationOperation) is { } culture)
            {
                return new Hit(Rules.Culture, "'" + culture + "'");
            }

            return null;
        }

        // An interpolation hole formats a number or date with the current culture unless a provider or an invariant wrapper is given.
        private static string? InterpolationText(IInterpolatedStringOperation text)
        {
            for (var p = text.Parent; p is not null; p = p.Parent)
            {
                if (p is IConversionOperation conversion)
                {
                    if (conversion.Type?.Name is "FormattableString" or "IFormattable")
                    {
                        return null;
                    }
                }
                else if (p is IInvocationOperation invocation)
                {
                    if (invocation.TargetMethod.Parameters.Any(q => q.Type.Name is "IFormatProvider" or "CultureInfo"))
                    {
                        return null;
                    }

                    break;
                }
                else if (p is not (IArgumentOperation or IInterpolatedStringHandlerCreationOperation))
                {
                    break;
                }
            }

            foreach (var part in text.Parts)
            {
                if (part is IInterpolationOperation hole && CultureRules.IsCultureNumber(Unwrap(hole.Expression).Type)
                    && !(hole.FormatString?.ConstantValue is { HasValue: true, Value: string { Length: > 0 } f } && f[0] is 'x' or 'X'))
                {
                    return "string interpolation of " + Unwrap(hole.Expression).Type!.Name;
                }
            }

            return null;
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
