namespace OneGame.Gltf;

/// <summary>
/// Imports glTF 2.0 files (<c>.gltf</c> and <c>.glb</c>) into a <see cref="GltfModel"/>: meshes, metal/rough materials, textures, skins,
/// animations, cameras and punctual lights. The import is CPU-side and does not need a graphics device; hand the model to
/// <c>Renderer3D.Upload</c> to draw it, or read its meshes directly. Input is untrusted: every count and size is checked against
/// <see cref="GltfLimits"/> before anything is allocated, and a bad file throws <see cref="GltfException"/> rather than crashing.
/// </summary>
/// <example>
/// <code>
/// GltfModel model = GltfLoader.LoadFile("models/Box.glb");
/// Console.WriteLine($"{model.Meshes.Length} meshes, {model.Materials.Length} materials");
/// foreach (var warning in model.Warnings)
/// {
///     Console.WriteLine($"{warning.Code}: {warning.Message}");   // something unsupported was skipped
/// }
/// </code>
/// </example>
public static class GltfLoader
{
    /// <summary>Imports a file already in memory.</summary>
    /// <param name="file">The bytes of a <c>.gltf</c> (JSON) or <c>.glb</c> file; the kind is detected from the first bytes.</param>
    /// <param name="options">Options, or null for the defaults.</param>
    /// <param name="resources">Resolves external buffers and images; null refuses every external uri.</param>
    /// <returns>The model.</returns>
    /// <exception cref="GltfException">The file is damaged, over a limit or uses something unsupported.</exception>
    public static GltfModel Load(ReadOnlyMemory<byte> file, GltfLoadOptions? options = null, IGltfResources? resources = null)
        => new Importer(options ?? new GltfLoadOptions(), resources).Run(file);

    /// <summary>Imports a file from disk; external buffers and images resolve inside the file's folder.</summary>
    /// <param name="path">The path of the <c>.gltf</c> or <c>.glb</c> file.</param>
    /// <param name="options">Options, or null for the defaults.</param>
    /// <returns>The model.</returns>
    /// <exception cref="GltfException">The file is damaged, over a limit, missing or uses something unsupported.</exception>
    public static GltfModel LoadFile(string path, GltfLoadOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(path);
        var limits = options?.Limits ?? new GltfLimits();
        var info = new FileInfo(path);
        if (!info.Exists)
        {
            throw new GltfException("resource_missing", $"'{path}' does not exist.");
        }

        if (info.Length > limits.MaxFileBytes)
        {
            throw new GltfException("file_too_large", $"'{path}' is {info.Length} bytes; the limit is {limits.MaxFileBytes}.");
        }

        var bytes = File.ReadAllBytes(path);
        return Load(bytes, options, new GltfFolderResources(Path.GetDirectoryName(Path.GetFullPath(path)) ?? "."));
    }
}
