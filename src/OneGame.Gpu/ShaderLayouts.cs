using OneShader.Layouts;

namespace OneGame.Gpu;

/// <summary>
/// Turns the layouts the shader build generates (<c>Shader.Entry.Layout</c>) into this layer's own binding and push-constant types, so a
/// pipeline layout is declared once, in the HLSL, and cannot disagree with the shaders that use it.
/// </summary>
/// <remarks>
/// Several entry points that share one pipeline layout (a vertex shader and some pixel variants, or a family of compute passes) are
/// combined: a resource at the same set and binding must agree on kind and count, and its stages are the union of the entry points' stages.
/// </remarks>
public static class ShaderLayouts
{
    /// <summary>The resource bindings that cover every entry point in <paramref name="entries"/>, ordered by set then binding.</summary>
    /// <param name="entries">The entry points that run with the pipeline layout.</param>
    /// <param name="dynamic">(set, binding) pairs of buffers to declare with a dynamic offset.</param>
    /// <returns>Bindings for <see cref="GpuDevice.CreatePipelineLayout(ReadOnlySpan{ResourceBinding})"/>.</returns>
    /// <exception cref="ArgumentException">Two entry points declare different resources at the same slot, or one uses a resource kind this layer has no binding for.</exception>
    public static ResourceBinding[] Bindings(ReadOnlySpan<EntryLayout> entries, ReadOnlySpan<(uint Set, uint Binding)> dynamic = default)
    {
        var list = new List<ResourceBinding>();
        foreach (var entry in entries)
        {
            foreach (var r in entry.Resources)
            {
                var kind = Map(r.Kind, entry.Name, r.Name);
                var stages = Map(r.Stages);
                var at = list.FindIndex(b => b.Set == r.Set && b.Binding == r.Binding);
                if (at < 0)
                {
                    list.Add(new ResourceBinding(r.Set, r.Binding, r.Count, kind, stages) { Dynamic = Contains(dynamic, r.Set, r.Binding) });
                    continue;
                }

                var have = list[at];
                if (have.Kind != kind || have.Count != r.Count)
                {
                    throw new ArgumentException($"entry point {entry.Name} declares {r.Name} at set {r.Set} binding {r.Binding} as {kind} x{r.Count}, but another entry point declares {have.Kind} x{have.Count} there");
                }

                list[at] = have with { Stages = have.Stages | stages };
            }
        }

        list.Sort((a, b) => a.Set != b.Set ? a.Set.CompareTo(b.Set) : a.Binding.CompareTo(b.Binding));
        return [.. list];
    }

    /// <summary>The push-constant range that covers the blocks of <paramref name="entries"/>, or an empty range when none has one.</summary>
    /// <param name="entries">The entry points that run with the pipeline layout.</param>
    /// <returns>The largest block, visible to every stage that declares one.</returns>
    public static PushConstantRange PushConstants(ReadOnlySpan<EntryLayout> entries)
    {
        uint size = 0;
        var stages = ShaderStage.None;
        foreach (var entry in entries)
        {
            if (entry.PushConstant is { } block)
            {
                size = Math.Max(size, block.Size);
                stages |= Map(block.Stages);
            }
        }

        return new PushConstantRange(size, stages);
    }

    static bool Contains(ReadOnlySpan<(uint Set, uint Binding)> pairs, uint set, uint binding)
    {
        foreach (var p in pairs)
        {
            if (p.Set == set && p.Binding == binding)
            {
                return true;
            }
        }

        return false;
    }

    static ShaderStage Map(OneShader.ShaderStages stages)
    {
        var result = ShaderStage.None;
        if ((stages & OneShader.ShaderStages.Vertex) != 0)
        {
            result |= ShaderStage.Vertex;
        }

        if ((stages & OneShader.ShaderStages.Pixel) != 0)
        {
            result |= ShaderStage.Pixel;
        }

        if ((stages & OneShader.ShaderStages.Compute) != 0)
        {
            result |= ShaderStage.Compute;
        }

        return result;
    }

    static ResourceKind Map(OneShader.ResourceKind kind, string entry, string name) => kind switch
    {
        OneShader.ResourceKind.ConstantBuffer => ResourceKind.ConstantBuffer,
        OneShader.ResourceKind.StorageBuffer => ResourceKind.StorageBuffer,
        OneShader.ResourceKind.ReadOnlyStorageBuffer => ResourceKind.ReadOnlyStorageBuffer,
        OneShader.ResourceKind.SampledTexture => ResourceKind.SampledTexture,
        OneShader.ResourceKind.Sampler => ResourceKind.Sampler,
        OneShader.ResourceKind.StorageTexture => ResourceKind.StorageTexture,
        _ => throw new ArgumentException($"entry point {entry} uses {name} as {kind}, which this layer has no binding for"),
    };
}
