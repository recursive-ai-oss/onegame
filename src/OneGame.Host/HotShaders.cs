using OneShader.Reflection;

namespace OneGame.Host;

/// <summary>
/// One shader a game wants to see edited while it runs: the HLSL file, the entry point of its pixel stage, the interface the game was
/// built against and what to do with a recompiled version. Register it with <see cref="HotShaders.Register"/>; development builds watch the file,
/// release builds ignore the registration.
/// </summary>
/// <param name="Name">A short name shown in reports, usually the material's.</param>
/// <param name="Path">The <c>.hlsl</c> file. Relative paths resolve against the working directory.</param>
/// <param name="EntryPoint">The pixel entry point to take from the compiled file.</param>
/// <param name="Baseline">The reflection the game was built with, for example <c>Ripple.Reflection</c> of the generated shader class.</param>
/// <param name="Apply">Swaps the recompiled SPIR-V into the live pipeline, for example by calling <c>Renderer2D.ReplaceMaterialShader</c>.</param>
public sealed record HotShader(string Name, string Path, string EntryPoint, ShaderReflection Baseline, Action<ReadOnlyMemory<byte>> Apply)
{
    /// <summary>The file's last write time when it was registered; later edits are changes. Set by <see cref="HotShaders.Register"/>.</summary>
    public DateTime FileTimeUtc { get; init; }

    /// <summary>The file's length when it was registered. Set by <see cref="HotShaders.Register"/>.</summary>
    public long FileLength { get; init; } = -1;
}

/// <summary>The shaders of a running game that may be recompiled when their files change. Only development builds act on it.</summary>
public sealed class HotShaders
{
    private readonly List<HotShader> _items = [];

    /// <summary>The registered shaders, in registration order.</summary>
    public IReadOnlyList<HotShader> Items => _items;

    /// <summary>Registers a shader for hot reload.</summary>
    /// <param name="shader">The shader.</param>
    /// <returns>The registration, which remembers the file as it is now. Pass it to <see cref="Unregister"/>.</returns>
    public HotShader Register(HotShader shader)
    {
        ArgumentNullException.ThrowIfNull(shader);
        var info = new FileInfo(shader.Path);
        var registered = info.Exists ? shader with { FileTimeUtc = info.LastWriteTimeUtc, FileLength = info.Length } : shader;
        _items.Add(registered);
        return registered;
    }

    /// <summary>Removes a registration, for example when the material is disposed.</summary>
    /// <param name="shader">The shader returned by registration.</param>
    /// <returns>True when it was registered.</returns>
    public bool Unregister(HotShader shader) => _items.Remove(shader);
}
