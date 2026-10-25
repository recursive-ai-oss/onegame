namespace OneGame.Generators.Tests;

public class PredictionRuleTests
{
    private const string Owner = "[Synced, OwnerKey] public partial uint Who { get; set; }\n";
    private const string Secret = "[Synced(Visibility = Visibility.Hidden)] public partial int Secret { get; set; }\n";
    private const string Gold = "[Synced(Visibility = Visibility.Owner)] public partial int Gold { get; set; }\n";

    public static TheoryData<string, string, string> Violations() => new()
    {
        // OG0020: reading what the replica does not hold
        { "reads a hidden member", Harness.Entity(Secret + "[Command(Predicted = true)] public void Go(int a) { N = Secret + a; }"), "OG0020" },
        { "reads a hidden member through a helper", Harness.Entity(Secret + "int Peek() => Secret;\n[Command(Predicted = true)] public void Go(int a) { N = Peek(); }"), "OG0020" },
        { "reads an owner member in an open command", Harness.Entity(Owner + Gold + "[Command(Predicted = true)] public void Go(int a) { N = Gold; }"), "OG0020" },
        { "reads another entity's owner member", Harness.Entity(Owner + Gold + "[Command(Predicted = true, OwnerOnly = true)] public void Go(EntityId other) { N = ((E)World!.Find(other)!).Gold; }"), "OG0020" },
        { "compound assignment reads a hidden member", Harness.Usings + "namespace T; [Entity] public partial class H : Entity { [Synced(Visibility = Visibility.Hidden)] public partial int S { get; set; } [Command(Predicted = true)] public void Go() { S += 1; } }", "OG0022" },

        // OG0021: random streams
        { "draws without saying so", Harness.Entity("[Command(Predicted = true)] public void Go() { N = World!.Rng[\"loot\"].NextInt(10); }"), "OG0021" },
        { "draws through a helper", Harness.Entity("void Roll() { N = World!.Rng[\"loot\"].NextInt(10); }\n[Command(Predicted = true)] public void Go() { Roll(); }"), "OG0021" },

        // OG0022: writing hidden members
        { "writes a hidden member", Harness.Entity(Secret + "[Command(Predicted = true)] public void Go(int a) { Secret = a; }"), "OG0022" },
        { "increments a hidden member", Harness.Entity(Secret + "[Command(Predicted = true)] public void Go() { Secret++; }"), "OG0022" },
        { "writes a hidden member through a helper", Harness.Entity(Secret + "void Hide(int a) { Secret = a; }\n[Command(Predicted = true)] public void Go(int a) { Hide(a); }"), "OG0022" },
    };

    [Theory]
    [MemberData(nameof(Violations))]
    public async Task MisuseIsACompileError(string name, string source, string id)
    {
        var diagnostics = await Harness.Diagnose(source);
        Assert.True(diagnostics.Any(d => d.Id == id), $"{name}: expected {id}, got:\n{Harness.Describe(diagnostics)}");
    }

    public static TheoryData<string, string> Fine() => new()
    {
        { "plain predicted command", Harness.Entity("[Command(Predicted = true)] public void Go(int a) { N += a; L.Add(a); }") },
        { "predicted on a filtered type, reading visible members", Harness.Entity(Owner + Gold + Secret + "[Command(Predicted = true)] public void Go(int a) { N = Who == 0 ? a : 1; }") },
        { "owner-only command reads the owner's own member", Harness.Entity(Owner + Gold + "[Command(Predicted = true, OwnerOnly = true)] public void Go(int a) { Gold += a; }") },
        { "owner member may be written by an open command", Harness.Entity(Owner + Gold + "[Command(Predicted = true)] public void Go(int a) { Gold = a; }") },
        { "random declared", Harness.Entity("[Command(Predicted = true, UsesRandom = true)] public void Go() { N = World!.Rng[\"loot\"].NextInt(10); }") },
        { "an unpredicted command may do all of it", Harness.Entity(Secret + "[Command] public void Go() { Secret = Secret + World!.Rng[\"loot\"].NextInt(10); }") },
        { "the sim may read hidden members", Harness.Entity(Secret + "[Command(Predicted = true)] public void Go() { N = 1; }\nprotected override void OnTick() { Secret++; }") },
    };

    [Theory]
    [MemberData(nameof(Fine))]
    public async Task ValidPredictionCompilesCleanly(string name, string source)
    {
        var diagnostics = await Harness.Diagnose(source);
        Assert.True(diagnostics.Count == 0, $"{name}:\n{Harness.Describe(diagnostics)}");
    }

    [Fact]
    public async Task ThePredictedFlagsReachTheGeneratedEntity()
    {
        var source = Harness.Entity("[Command(Predicted = true)] public void A() { N = 1; }\n[Command(Predicted = true, UsesRandom = true)] public void B() { N = World!.Rng[\"x\"].NextInt(3); }\n[Command] public void C() { N = 3; }");
        Assert.Empty(await Harness.Diagnose(source));
        var compilation = Harness.Compile(source);
        Microsoft.CodeAnalysis.GeneratorDriver driver = Microsoft.CodeAnalysis.CSharp.CSharpGeneratorDriver.Create(new EntityGenerator());
        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        var text = string.Concat(driver.GetRunResult().GeneratedTrees.Select(t => t.ToString()));
        Assert.Contains("IsPredictedCommand", text, StringComparison.Ordinal);
        Assert.Contains("PredictedCommandUsesRandom", text, StringComparison.Ordinal);
        Assert.Contains("CopyStateCore", text, StringComparison.Ordinal);
    }
}
