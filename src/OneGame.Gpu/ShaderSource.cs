namespace OneGame.Gpu;

/// <summary>
/// One compiled shader entry point in every form a backend may need. Fill it from the members the shader build generates:
/// <c>SpirV</c> for Vulkan, <c>Msl</c> and <c>MslFunctionName</c> for Metal, <c>Wgsl</c> and <c>WgslEntryPoint</c> for the browser, and the entry point's <c>Layout</c> so a backend that numbers
/// resources per entry point (Metal) can place each (set, binding) pair.
/// </summary>
/// <remarks>A backend reads only the parts it needs and throws <see cref="GpuUnsupportedException"/> when its part is empty.</remarks>
public readonly ref struct ShaderSource
{
    /// <summary>Creates a source for an entry point.</summary>
    /// <param name="stage">Stage the entry point runs in.</param>
    /// <param name="entryPoint">Entry point name as written in the HLSL.</param>
    public ShaderSource(ShaderStage stage, string entryPoint)
    {
        Stage = stage;
        EntryPoint = entryPoint;
    }

    /// <summary>Stage the entry point runs in.</summary>
    public ShaderStage Stage { get; }

    /// <summary>Entry point name as written in the HLSL.</summary>
    public string EntryPoint { get; }

    /// <summary>SPIR-V code (4-byte aligned words, little-endian), for Vulkan.</summary>
    public ReadOnlySpan<byte> SpirV { get; init; }

    /// <summary>UTF-8 Metal Shading Language source, for Metal.</summary>
    public ReadOnlySpan<byte> Msl { get; init; }

    /// <summary>UTF-8 WGSL source, for browser WebGPU.</summary>
    public ReadOnlySpan<byte> Wgsl { get; init; }

    /// <summary>The entry point name in the WGSL. Defaults to <see cref="EntryPoint"/>.</summary>
    public string? WgslEntryPoint { get; init; }

    /// <summary>The function to look up in the MSL source. Defaults to <see cref="EntryPoint"/>.</summary>
    public string? MslFunctionName { get; init; }

    /// <summary>
    /// The entry point's layout from the shader build (<c>Shader.Entry.Layout</c> of the generated class, or <c>EntryLayout.TryCreate</c> for a shader compiled at run time):
    /// its resources with their Metal argument indices, the push-constant block with its Metal index and WGSL group and binding, and the compute thread-group size.
    /// Metal needs it to bind resources, push constants and dispatch; WebGPU needs it for push constants; Vulkan ignores it.
    /// </summary>
    public OneShader.Layouts.EntryLayout? Layout { get; init; }
}
