using Microsoft.CodeAnalysis;

namespace OneGame.Generators;

/// <summary>Generates the code behind entities. A placeholder: it registers no output yet.</summary>
[Generator(LanguageNames.CSharp)]
public sealed class EntityGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Entity discovery and emission are added here.
    }
}
