namespace OneGame.Gltf;

/// <summary>Imports glTF 2.0 files (<c>.gltf</c> and <c>.glb</c>) into a <see cref="GltfModel"/>.</summary>
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
