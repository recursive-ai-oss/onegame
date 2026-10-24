using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace OneGame.Generators.Tests;

public class GeneratorTests
{
    public static TheoryData<string, string, string> Shapes() => new()
    {
        { "not partial", Harness.Usings + "namespace T; [Entity] public class E : Entity { }", "OG0010" },
        { "wrong base", Harness.Usings + "namespace T; [Entity] public partial class E { }", "OG0010" },
        { "nested", Harness.Usings + "namespace T; public class Outer { [Entity] public partial class E : Entity { } }", "OG0010" },
        { "generic", Harness.Usings + "namespace T; [Entity] public partial class E<TT> : Entity { }", "OG0010" },
        { "no default constructor", Harness.Usings + "namespace T; [Entity] public partial class E : Entity { public E(int a) { } }", "OG0010" },
        { "not partial struct", Harness.Usings + "namespace T; [SyncedStruct] public struct S { public int A { get; set; } }", "OG0010" },
        { "synced auto property", Harness.Entity("[Synced] public int Plain { get; set; }"), "OG0012" },
        { "synced get only", Harness.Entity("[Synced] public partial int GetOnly { get; }"), "OG0012" },
        { "synced init", Harness.Entity("[Synced] public partial int Init { get; init; }"), "OG0012" },
        { "list with setter", Harness.Entity("[Synced] public SyncList<int> Settable { get; set; } = new();"), "OG0012" },
        { "readonly struct field", Harness.Usings + "namespace T; [SyncedStruct] public partial struct S { public readonly int A; }", "OG0012" },
        { "overloaded command", Harness.Entity("[Command] public void Go(int a) { } [Command] public void Go(string b) { }"), "OG0013" },
        { "command returning int", Harness.Entity("[Command] public int Go() => 0;"), "OG0013" },
        { "command static", Harness.Entity("[Command] public static void Go() { }"), "OG0013" },
        { "command ref argument", Harness.Entity("[Command] public void Go(ref int a) { }"), "OG0013" },
        { "command object argument", Harness.Entity("[Command] public void Go(object a) { }"), "OG0013" },
        { "migration wrong signature", Harness.Entity("[Migrate(1)] static void M(int a) { }").Replace("[Entity]", "[Entity(Schema = 2)]"), "OG0014" },
        { "migration not older", Harness.Entity("[Migrate(2)] static void M(ref SyncReader r, E e) { }").Replace("[Entity]", "[Entity(Schema = 2)]"), "OG0014" },
        { "migration duplicate", Harness.Entity("[Migrate(1)] static void A(ref SyncReader r, E e) { } [Migrate(1)] static void B(ref SyncReader r, E e) { }").Replace("[Entity]", "[Entity(Schema = 2)]"), "OG0014" },
    };

    [Theory]
    [MemberData(nameof(Shapes))]
    public async Task DeclarationProblemsAreDiagnostics(string name, string source, string id)
    {
        var diagnostics = await Harness.Diagnose(source);
        Assert.True(diagnostics.Any(d => d.Id == id), $"{name}: expected {id}, got:\n{Harness.Describe(diagnostics)}");
    }

    [Fact]
    public async Task MoreThanSixtyFourSyncedPropertiesIsRefused()
    {
        var members = string.Concat(Enumerable.Range(0, 65).Select(i => $"[Synced] public partial int P{i} {{ get; set; }}\n"));
        var diagnostics = await Harness.Diagnose(Harness.Entity(members));
        Assert.Contains(diagnostics, d => d.Id == "OG0011");
    }

    [Fact]
    public async Task SixtyFourPropertiesWork()
    {
        var members = string.Concat(Enumerable.Range(0, 62).Select(i => $"[Synced] public partial int P{i} {{ get; set; }}\n"));
        Assert.Empty(await Harness.Diagnose(Harness.Entity(members)));
    }

    [Fact]
    public async Task DiagnosticsPointAtTheOffendingCode()
    {
        var source = Harness.Entity("protected override void OnTick() { var t = DateTime.Now; }");
        var d = (await Harness.Diagnose(source)).Single();
        Assert.Equal("OG0004", d.Id);
        Assert.Equal("DateTime.Now", source.Substring(d.Location.SourceSpan.Start, d.Location.SourceSpan.Length));
    }

    [Fact]
    public void RegistrationClassListsEveryEntity()
    {
        var compilation = Harness.Compile(Harness.Usings + "namespace A; [Entity] public partial class One : Entity { }\n[Entity] public partial class Two : Entity { }");
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new EntityGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, TestContext.Current.CancellationToken);
        var text = driver.GetRunResult().GeneratedTrees.Single(t => t.FilePath.Contains("EntityTypes", StringComparison.Ordinal)).ToString();
        Assert.Contains("namespace OneGame.Generated.T", text, StringComparison.Ordinal);
        Assert.Contains("new global::A.One()", text, StringComparison.Ordinal);
        Assert.Contains("new global::A.Two()", text, StringComparison.Ordinal);
    }

    [Fact]
    public void AnUnrelatedEditLeavesGeneratorOutputsCached()
    {
        var entity = CSharpSyntaxTree.ParseText(Harness.Entity("[Command] public void Go(int a) { }"), new CSharpParseOptions(LanguageVersion.Latest), cancellationToken: TestContext.Current.CancellationToken);
        var other1 = CSharpSyntaxTree.ParseText("namespace Z; public class Unrelated { public int A; }", new CSharpParseOptions(LanguageVersion.Latest), cancellationToken: TestContext.Current.CancellationToken);
        var other2 = CSharpSyntaxTree.ParseText("namespace Z; public class Unrelated { public int A; public int B; void M() { } }", new CSharpParseOptions(LanguageVersion.Latest), cancellationToken: TestContext.Current.CancellationToken);
        var compilation = Harness.Compile(entity.ToString()).AddSyntaxTrees(other1);
        var driver = CSharpGeneratorDriver.Create(
            [new EntityGenerator().AsSourceGenerator()], driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var edited = compilation.ReplaceSyntaxTree(other1, other2);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(edited, TestContext.Current.CancellationToken);
        var result = driver.GetRunResult().Results.Single();
        var steps = result.TrackedSteps["Entity"].SelectMany(s => s.Outputs).ToList();
        Assert.NotEmpty(steps);
        Assert.All(steps, o => Assert.True(o.Reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged, o.Reason.ToString()));

        // Changing the entity itself does re-run it.
        var changed = edited.ReplaceSyntaxTree(edited.SyntaxTrees.First(), CSharpSyntaxTree.ParseText(Harness.Entity("[Command] public void Go(int a, int b) { }"), new CSharpParseOptions(LanguageVersion.Latest), cancellationToken: TestContext.Current.CancellationToken));
        driver = (CSharpGeneratorDriver)driver.RunGenerators(changed, TestContext.Current.CancellationToken);
        var after = driver.GetRunResult().Results.Single().TrackedSteps["Entity"].SelectMany(s => s.Outputs).ToList();
        Assert.Contains(after, o => o.Reason is IncrementalStepRunReason.Modified or IncrementalStepRunReason.New);
    }
}
