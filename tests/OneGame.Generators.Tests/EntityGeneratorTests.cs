using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace OneGame.Generators.Tests;

public class EntityGeneratorTests
{
    [Fact]
    public void RunsOverASourceWithoutDiagnostics()
    {
        var compilation = CSharpCompilation.Create(
            "Probe",
            [CSharpSyntaxTree.ParseText("class C { }", cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new EntityGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics, TestContext.Current.CancellationToken);

        Assert.Empty(diagnostics);
        Assert.Empty(driver.GetRunResult().GeneratedTrees);
    }
}
