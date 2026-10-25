namespace OneGame.Gpu;

/// <summary>
/// One compiled shader entry point in every form a backend may need. Fill it from the members the shader build generates:
/// <c>SpirV</c> for Vulkan, <c>Msl</c> and <c>MslFunctionName</c> for Metal, <c>Wgsl</c> and <c>WgslEntryPoint</c> for the browser, and the file's <c>Bindings.All</c> so a backend that numbers
/// resources per shader file (Metal) can place each (set, binding) pair.
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
    /// The resources of the shader file this entry point came from, with their Metal argument indices. Metal needs it to bind a
    /// bind group's resources; other backends ignore it.
    /// </summary>
    public ReadOnlySpan<OneShader.ResourceBinding> Bindings { get; init; }

    /// <summary>
    /// The buffer index the interleaved vertex buffer is bound at, for a Metal vertex shader (<c>MslVertexBufferIndex</c>). 0 means the default, 30.
    /// </summary>
    public int MslVertexBufferIndex { get; init; }

    /// <summary>Compute shaders on Metal: the thread-group size from the generated <c>ThreadGroupSizeX</c>, <c>ThreadGroupSizeY</c> and <c>ThreadGroupSizeZ</c>.</summary>
    public uint ComputeGroupX { get; init; }

    /// <summary>See <see cref="ComputeGroupX"/>.</summary>
    public uint ComputeGroupY { get; init; }

    /// <summary>See <see cref="ComputeGroupX"/>.</summary>
    public uint ComputeGroupZ { get; init; }

    /// <summary>
    /// The Metal buffer index of the shader's push-constant block (the generated <c>Bindings.PushConstants.MslIndex</c>), or -1 when the
    /// shader has none. Other backends ignore it.
    /// </summary>
    public int MslPushConstantIndex { get; init; } = -1;

    /// <summary>
    /// The WGSL bind group of the shader's push-constant block (the generated <c>Bindings.PushConstants.WgslGroup</c>), or -1 when it has none.
    /// WebGPU has no push constants: the backend binds the block as a uniform buffer here. Other backends ignore it.
    /// </summary>
    public int WgslPushGroup { get; init; } = -1;

    /// <summary>The WGSL binding number of the push-constant block (<c>Bindings.PushConstants.WgslBinding</c>); see <see cref="WgslPushGroup"/>.</summary>
    public int WgslPushBinding { get; init; } = -1;
}
