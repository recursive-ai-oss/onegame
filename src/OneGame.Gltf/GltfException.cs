namespace OneGame.Gltf;

/// <summary>The file cannot be imported: it is damaged, hostile, over a limit or uses something the importer does not support.</summary>
public sealed class GltfException : Exception
{
    /// <summary>Creates the exception.</summary>
    /// <param name="code">The stable error code; see the interface document for the list.</param>
    /// <param name="message">What is wrong and where.</param>
    public GltfException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    /// <summary>A short stable code a program can act on, such as <c>buffer_range</c> or <c>limit_exceeded</c>.</summary>
    public string Code { get; }
}

/// <summary>Something that loaded but was degraded or ignored.</summary>
/// <param name="Code">A short stable code.</param>
/// <param name="Message">What happened and where.</param>
public readonly record struct GltfWarning(string Code, string Message);
