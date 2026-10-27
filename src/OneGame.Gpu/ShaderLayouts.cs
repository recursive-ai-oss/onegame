using OneShader.Layouts;
using OneShader.Layouts.Vulkan;

namespace OneGame.Gpu;

/// <summary>
/// Turns the layouts the shader build generates (<c>Shader.Entry.Layout</c>) into this layer's own binding, push-constant and vertex types,
/// so a pipeline layout is declared once, in the HLSL, and cannot disagree with the shaders that use it.
/// </summary>
/// <remarks>
/// Several entry points that share one pipeline layout (a vertex shader and some pixel variants, or a family of compute passes) are
/// combined with <see cref="PipelineLayout.TryCreateUnion"/>: a resource at the same set and binding must agree on kind and count, and its
/// stages are the union of the entry points' stages.
/// </remarks>
public static class ShaderLayouts
{
    /// <summary>The resource bindings that cover every entry point in <paramref name="entries"/>, ordered by set then binding.</summary>
    /// <param name="entries">The entry points that run with the pipeline layout.</param>
    /// <param name="dynamic">(set, binding) pairs of buffers to declare with a dynamic offset.</param>
    /// <param name="widenSets">Stages to add to every binding of a set, so a bind group made once fits pipelines whose shaders use it from different stages.</param>
    /// <returns>Bindings for <see cref="GpuDevice.CreatePipelineLayout(ReadOnlySpan{ResourceBinding})"/>.</returns>
    /// <exception cref="ArgumentException">Two entry points declare different resources at the same slot, or one uses a resource kind this layer has no binding for.</exception>
    public static ResourceBinding[] Bindings(ReadOnlySpan<EntryLayout> entries, ReadOnlySpan<(uint Set, uint Binding)> dynamic = default, ReadOnlySpan<(uint Set, ShaderStage Stages)> widenSets = default)
    {
        if (!PipelineLayout.TryCreateUnion(entries, out var union, out var error))
        {
            throw new ArgumentException(error!.Message, nameof(entries));
        }

        var widen = new Dictionary<uint, OneShader.ShaderStages>();
        foreach (var (set, stages) in widenSets)
        {
            widen[set] = Unmap(stages);
        }

        var options = new VulkanLayoutOptions { DynamicBindings = [.. dynamic.ToArray()], WidenSets = widen };
        var result = new List<ResourceBinding>();
        foreach (var set in union!.ToVulkan(options).SetLayouts)
        {
            foreach (var b in set.Bindings)
            {
                var resource = union!.Groups.Single(g => g.Set == set.Set).Entries.Single(e => e.Resource.Binding == b.Binding).Resource;
                result.Add(new ResourceBinding(set.Set, b.Binding, b.DescriptorCount, Map(resource.Kind, resource.Name), Map(b.StageFlags)) { Dynamic = b.IsDynamic });
            }
        }

        return [.. result];
    }

    /// <summary>The push-constant range that covers the blocks of <paramref name="entries"/>, or an empty range when none has one.</summary>
    /// <param name="entries">The entry points that run with the pipeline layout.</param>
    /// <returns>The largest block, visible to every stage that declares one.</returns>
    /// <exception cref="ArgumentException">Two entry points declare incompatible push-constant blocks.</exception>
    public static PushConstantRange PushConstants(ReadOnlySpan<EntryLayout> entries)
    {
        if (!PipelineLayout.TryCreateUnion(entries, out var union, out var error))
        {
            throw new ArgumentException(error!.Message, nameof(entries));
        }

        uint size = 0;
        var stages = ShaderStage.None;
        foreach (var block in union!.PushConstants)
        {
            size = Math.Max(size, block.Size);
            stages |= Map(block.Stages);
        }

        return new PushConstantRange(size, stages);
    }

    /// <summary>The vertex buffer layout of a vertex entry point.</summary>
    /// <param name="entry">The vertex shader's layout.</param>
    /// <param name="step">Whether the buffer advances per vertex or per instance.</param>
    /// <param name="overrides">What the engine's own vertex stores differently from the shader's declared inputs (a packed colour, a narrower integer, a smaller stride).</param>
    /// <returns>The layout, with the engine's vertex formats.</returns>
    /// <exception cref="ArgumentException">The entry point has no vertex input, the overrides don't fit it, or an input uses a format this layer has no vertex format for.</exception>
    public static VertexBufferLayout Vertex(EntryLayout entry, VertexStepMode step = VertexStepMode.Vertex, VertexLayoutOverrides? overrides = null)
    {
        if (overrides is not null)
        {
            if (!entry.TryWithVertexOverrides(overrides, out var changed, out var error))
            {
                throw new ArgumentException(error!.Message, nameof(overrides));
            }

            entry = changed!;
        }

        var vertex = entry.Vertex ?? throw new ArgumentException($"entry point {entry.Name} has no vertex input", nameof(entry));
        var elements = new VertexElement[vertex.Attributes.Length];
        for (var i = 0; i < elements.Length; i++)
        {
            var a = vertex.Attributes[i];
            elements[i] = new VertexElement(a.Location, Map(a.Format, entry.Name), a.Offset);
        }

        return new VertexBufferLayout { Stride = vertex.Stride, StepMode = step, Attributes = elements };
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

    static ShaderStage Map(ShaderStageFlags stages)
    {
        var result = ShaderStage.None;
        if ((stages & ShaderStageFlags.Vertex) != 0)
        {
            result |= ShaderStage.Vertex;
        }

        if ((stages & ShaderStageFlags.Fragment) != 0)
        {
            result |= ShaderStage.Pixel;
        }

        if ((stages & ShaderStageFlags.Compute) != 0)
        {
            result |= ShaderStage.Compute;
        }

        return result;
    }

    static OneShader.ShaderStages Unmap(ShaderStage stages)
    {
        var result = OneShader.ShaderStages.None;
        if ((stages & ShaderStage.Vertex) != 0)
        {
            result |= OneShader.ShaderStages.Vertex;
        }

        if ((stages & ShaderStage.Pixel) != 0)
        {
            result |= OneShader.ShaderStages.Pixel;
        }

        if ((stages & ShaderStage.Compute) != 0)
        {
            result |= OneShader.ShaderStages.Compute;
        }

        return result;
    }

    static ResourceKind Map(OneShader.ResourceKind kind, string name) => kind switch
    {
        OneShader.ResourceKind.ConstantBuffer => ResourceKind.ConstantBuffer,
        OneShader.ResourceKind.StorageBuffer => ResourceKind.StorageBuffer,
        OneShader.ResourceKind.ReadOnlyStorageBuffer => ResourceKind.ReadOnlyStorageBuffer,
        OneShader.ResourceKind.SampledTexture => ResourceKind.SampledTexture,
        OneShader.ResourceKind.Sampler => ResourceKind.Sampler,
        OneShader.ResourceKind.StorageTexture => ResourceKind.StorageTexture,
        _ => throw new ArgumentException($"{name} is a {kind}, which this layer has no binding for"),
    };

    static VertexFormat Map(OneShader.VertexFormat format, string entry) => format switch
    {
        OneShader.VertexFormat.Float32 => VertexFormat.Float1,
        OneShader.VertexFormat.Float32x2 => VertexFormat.Float2,
        OneShader.VertexFormat.Float32x3 => VertexFormat.Float3,
        OneShader.VertexFormat.Float32x4 => VertexFormat.Float4,
        OneShader.VertexFormat.Float16x2 => VertexFormat.Half2,
        OneShader.VertexFormat.Float16x4 => VertexFormat.Half4,
        OneShader.VertexFormat.Uint32 => VertexFormat.UInt1,
        OneShader.VertexFormat.Uint32x2 => VertexFormat.UInt2,
        OneShader.VertexFormat.Uint32x4 => VertexFormat.UInt4,
        OneShader.VertexFormat.Sint32 => VertexFormat.SInt1,
        OneShader.VertexFormat.Sint32x2 => VertexFormat.SInt2,
        OneShader.VertexFormat.Unorm8x4 => VertexFormat.UNorm8x4,
        OneShader.VertexFormat.Uint8x4 => VertexFormat.UInt8x4,
        OneShader.VertexFormat.Unorm16x2 => VertexFormat.UNorm16x2,
        OneShader.VertexFormat.Unorm16x4 => VertexFormat.UNorm16x4,
        _ => throw new ArgumentException($"entry point {entry} reads a {format} input, which this layer has no vertex format for"),
    };
}
