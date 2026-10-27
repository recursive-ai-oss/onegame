using System.Numerics;
using System.Runtime.InteropServices;
using OneGame.Gpu;
using OneGame.Pages.Shaders;
using EntryLayout = OneShader.Layouts.EntryLayout;
using VertexAttributeOverride = OneShader.Layouts.VertexAttributeOverride;
using VertexLayoutOverrides = OneShader.Layouts.VertexLayoutOverrides;

namespace OneGame.Pages;

/// <summary>One instanced quad of the page shader. The layout matches <c>PageIn</c> in <c>Page.hlsl</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct PageInstance
{
    public const int Size = 144;

    public Vector4 Rect;
    public Vector4 M;
    public Vector2 T;
    public Vector4 Radii;
    public Vector4 RadiiY;
    public Vector4 Widths;
    public Vector4 Uv;
    public uint C0;
    public uint C1;
    public Vector4 Grad;
    public uint Kind;
    public uint Sub;
    public uint ClipId;
    public float Pad;
}

/// <summary>The pipelines of the page shader and the small resources they share. One per device and target format.</summary>
internal sealed class PageGpu : IDisposable
{
    public const GpuFormat LayerFormat = GpuFormat.Rgba8Unorm;

    private readonly Dictionary<(GpuTexture, GpuTexture), GpuBindGroup> _groups = [];
    private readonly Dictionary<(GpuTexture, GpuTexture), GpuBindGroup> _nearestGroups = [];
    private readonly Dictionary<GpuBuffer, (uint Width, uint Height, GpuBuffer Buffer, GpuBindGroup Group)> _frames = [];
    private readonly List<IDisposable> _owned = [];

    public PageGpu(GpuDevice device, GpuFormat targetFormat)
    {
        Device = device;
        TargetFormat = targetFormat;
        var shared = new EntryLayout[]
        {
            Page.VSMain.Layout, Page.PSMain.Layout, Page.PSLayer.Layout, Page.PSBlend.Layout, Page.PSMaskCombine.Layout, Page.PSMaskApply.Layout,
            Page.PSColor.Layout, Page.PSBlur.Layout, Page.PSDropTint.Layout, Page.PSOver.Layout, Page.PSBackdrop.Layout, Page.PSProject.Layout,
            Page.PSPresent.Layout, Page.PSEncode.Layout,
        };
        Layout = device.CreatePipelineLayout(ShaderLayouts.Bindings(shared, widenSets: [(0, ShaderStage.Vertex | ShaderStage.Pixel)]));

        var vertex = Shader(ShaderStage.Vertex, Page.VSMain.EntryPoint, Page.VSMain.SpirV, Page.VSMain.Msl, Page.VSMain.MslFunctionName, Page.VSMain.Layout, Page.VSMain.Wgsl, Page.VSMain.WgslEntryPoint);
        InstanceLayout = ShaderLayouts.Vertex(Page.VSMain.Layout, VertexStepMode.Instance, new VertexLayoutOverrides
        {
            Stride = PageInstance.Size,
            Attributes = new Dictionary<uint, VertexAttributeOverride>
            {
                [7] = new(OneShader.VertexFormat.Unorm8x4, 104),
                [8] = new(OneShader.VertexFormat.Unorm8x4, 108),
                [9] = new(null, 112),
                [10] = new(null, 128),
            },
        });

        var over = BlendState.Premultiplied;
        var add = new BlendState(
            new BlendComponent(BlendFactor.One, BlendFactor.One, BlendOperation.Add),
            new BlendComponent(BlendFactor.One, BlendFactor.One, BlendOperation.Add));
        Prim = Pipe(vertex, Page.PSMain.Stage, Page.PSMain.EntryPoint, Page.PSMain.SpirV, Page.PSMain.Msl, Page.PSMain.MslFunctionName, Page.PSMain.Layout, Page.PSMain.Wgsl, Page.PSMain.WgslEntryPoint, LayerFormat, over, "page prim");
        LayerOver = Pipe(vertex, Page.PSLayer.Stage, Page.PSLayer.EntryPoint, Page.PSLayer.SpirV, Page.PSLayer.Msl, Page.PSLayer.MslFunctionName, Page.PSLayer.Layout, Page.PSLayer.Wgsl, Page.PSLayer.WgslEntryPoint, LayerFormat, over, "page layer");
        LayerAdd = Pipe(vertex, Page.PSLayer.Stage, Page.PSLayer.EntryPoint, Page.PSLayer.SpirV, Page.PSLayer.Msl, Page.PSLayer.MslFunctionName, Page.PSLayer.Layout, Page.PSLayer.Wgsl, Page.PSLayer.WgslEntryPoint, LayerFormat, add, "page add");
        Blend = Pipe(vertex, Page.PSBlend.Stage, Page.PSBlend.EntryPoint, Page.PSBlend.SpirV, Page.PSBlend.Msl, Page.PSBlend.MslFunctionName, Page.PSBlend.Layout, Page.PSBlend.Wgsl, Page.PSBlend.WgslEntryPoint, LayerFormat, null, "page blend");
        MaskCombine = Pipe(vertex, Page.PSMaskCombine.Stage, Page.PSMaskCombine.EntryPoint, Page.PSMaskCombine.SpirV, Page.PSMaskCombine.Msl, Page.PSMaskCombine.MslFunctionName, Page.PSMaskCombine.Layout, Page.PSMaskCombine.Wgsl, Page.PSMaskCombine.WgslEntryPoint, LayerFormat, null, "page mask combine");
        MaskApply = Pipe(vertex, Page.PSMaskApply.Stage, Page.PSMaskApply.EntryPoint, Page.PSMaskApply.SpirV, Page.PSMaskApply.Msl, Page.PSMaskApply.MslFunctionName, Page.PSMaskApply.Layout, Page.PSMaskApply.Wgsl, Page.PSMaskApply.WgslEntryPoint, LayerFormat, over, "page mask");
        Color = Pipe(vertex, Page.PSColor.Stage, Page.PSColor.EntryPoint, Page.PSColor.SpirV, Page.PSColor.Msl, Page.PSColor.MslFunctionName, Page.PSColor.Layout, Page.PSColor.Wgsl, Page.PSColor.WgslEntryPoint, LayerFormat, null, "page filter");
        Blur = Pipe(vertex, Page.PSBlur.Stage, Page.PSBlur.EntryPoint, Page.PSBlur.SpirV, Page.PSBlur.Msl, Page.PSBlur.MslFunctionName, Page.PSBlur.Layout, Page.PSBlur.Wgsl, Page.PSBlur.WgslEntryPoint, LayerFormat, null, "page blur");
        DropTint = Pipe(vertex, Page.PSDropTint.Stage, Page.PSDropTint.EntryPoint, Page.PSDropTint.SpirV, Page.PSDropTint.Msl, Page.PSDropTint.MslFunctionName, Page.PSDropTint.Layout, Page.PSDropTint.Wgsl, Page.PSDropTint.WgslEntryPoint, LayerFormat, null, "page shadow");
        Over = Pipe(vertex, Page.PSOver.Stage, Page.PSOver.EntryPoint, Page.PSOver.SpirV, Page.PSOver.Msl, Page.PSOver.MslFunctionName, Page.PSOver.Layout, Page.PSOver.Wgsl, Page.PSOver.WgslEntryPoint, LayerFormat, null, "page over");
        Backdrop = Pipe(vertex, Page.PSBackdrop.Stage, Page.PSBackdrop.EntryPoint, Page.PSBackdrop.SpirV, Page.PSBackdrop.Msl, Page.PSBackdrop.MslFunctionName, Page.PSBackdrop.Layout, Page.PSBackdrop.Wgsl, Page.PSBackdrop.WgslEntryPoint, LayerFormat, null, "page backdrop");
        Project = Pipe(vertex, Page.PSProject.Stage, Page.PSProject.EntryPoint, Page.PSProject.SpirV, Page.PSProject.Msl, Page.PSProject.MslFunctionName, Page.PSProject.Layout, Page.PSProject.Wgsl, Page.PSProject.WgslEntryPoint, LayerFormat, over, "page project");
        Encode = Pipe(vertex, Page.PSEncode.Stage, Page.PSEncode.EntryPoint, Page.PSEncode.SpirV, Page.PSEncode.Msl, Page.PSEncode.MslFunctionName, Page.PSEncode.Layout, Page.PSEncode.Wgsl, Page.PSEncode.WgslEntryPoint, LayerFormat, over, "page encode");
        Present = Pipe(vertex, Page.PSPresent.Stage, Page.PSPresent.EntryPoint, Page.PSPresent.SpirV, Page.PSPresent.Msl, Page.PSPresent.MslFunctionName, Page.PSPresent.Layout, Page.PSPresent.Wgsl, Page.PSPresent.WgslEntryPoint, targetFormat, over, "page present");
        vertex.Dispose();

        LinearClamp = device.CreateSampler(SamplerDesc.Linear with { AddressU = AddressMode.ClampToEdge, AddressV = AddressMode.ClampToEdge, MipFilter = FilterMode.Nearest });
        NearestClamp = device.CreateSampler(new SamplerDesc { AddressU = AddressMode.ClampToEdge, AddressV = AddressMode.ClampToEdge, MaxAnisotropy = 1 });
        _owned.Add(LinearClamp);
        _owned.Add(NearestClamp);

        // A transparent texel stands in wherever a shader's second texture is unused.
        Blank = device.CreateTexture(new TextureDesc(1, 1, LayerFormat, TextureUsage.Sampled | TextureUsage.CopyDestination) { Label = "page blank" });
        device.WriteTexture(Blank, 0, 0, new byte[4]);
        _owned.Add(Blank);
    }

    public GpuDevice Device { get; }

    public GpuFormat TargetFormat { get; }

    public GpuPipelineLayout Layout { get; }

    public VertexBufferLayout InstanceLayout { get; }

    public GpuGraphicsPipeline Prim { get; }

    public GpuGraphicsPipeline LayerOver { get; }

    public GpuGraphicsPipeline LayerAdd { get; }

    public GpuGraphicsPipeline Blend { get; }

    public GpuGraphicsPipeline MaskCombine { get; }

    public GpuGraphicsPipeline MaskApply { get; }

    public GpuGraphicsPipeline Color { get; }

    public GpuGraphicsPipeline Blur { get; }

    public GpuGraphicsPipeline DropTint { get; }

    public GpuGraphicsPipeline Over { get; }

    public GpuGraphicsPipeline Backdrop { get; }

    public GpuGraphicsPipeline Project { get; }

    public GpuGraphicsPipeline Encode { get; }

    public GpuGraphicsPipeline Present { get; }

    public GpuSampler LinearClamp { get; }

    public GpuSampler NearestClamp { get; }

    public GpuTexture Blank { get; }

    /// <summary>A bind group for set 1 sampling <paramref name="first"/> (and <paramref name="second"/>, or a blank texel) with a linear or nearest sampler.</summary>
    public GpuBindGroup Sampled(GpuTexture first, GpuTexture? second = null, bool nearest = false)
    {
        var table = nearest ? _nearestGroups : _groups;
        var key = (first, second ?? Blank);
        if (!table.TryGetValue(key, out var group))
        {
            if (_groups.Count + _nearestGroups.Count > 2048)
            {
                // Pooled layer textures are stable in a steady frame, so this only happens when a game keeps making new textures.
                Device.WaitIdle();
                foreach (var g in _groups.Values.Concat(_nearestGroups.Values))
                {
                    g.Dispose();
                }

                _groups.Clear();
                _nearestGroups.Clear();
            }

            group = Device.CreateBindGroup(Layout, 1,
            [
                BindGroupEntry.ForTexture(0, first),
                BindGroupEntry.ForSampler(1, nearest ? NearestClamp : LinearClamp),
                BindGroupEntry.ForTexture(2, key.Item2),
            ]);
            table[key] = group;
        }

        return group;
    }

    /// <summary>Set 0 for a target of this size: the viewport constants and the plan's clip table.</summary>
    public GpuBindGroup Frame(uint width, uint height, GpuBuffer clips)
    {
        if (_frames.TryGetValue(clips, out var entry) && entry.Width == width && entry.Height == height)
        {
            return entry.Group;
        }

        if (entry.Group is not null)
        {
            Device.WaitIdle();
            entry.Group.Dispose();
            entry.Buffer.Dispose();
        }

        var buffer = Device.CreateBuffer(new BufferDesc(16, BufferUsage.Uniform | BufferUsage.CopyDestination) { Label = "page frame" });
        Device.WriteBuffer(buffer, 0, MemoryMarshal.AsBytes<float>([width, height, 0f, 0f]));
        var group = Device.CreateBindGroup(Layout, 0, [BindGroupEntry.ForBuffer(0, buffer), BindGroupEntry.ForBuffer(1, clips)]);
        _frames[clips] = (width, height, buffer, group);
        return group;
    }

    /// <summary>Forgets the set 0 group of a clip buffer that is being replaced.</summary>
    public void ForgetFrame(GpuBuffer clips)
    {
        if (_frames.Remove(clips, out var entry))
        {
            Device.WaitIdle();
            entry.Group.Dispose();
            entry.Buffer.Dispose();
        }
    }

    public void Dispose()
    {
        Device.WaitIdle();
        foreach (var g in _groups.Values.Concat(_nearestGroups.Values))
        {
            g.Dispose();
        }

        foreach (var f in _frames.Values)
        {
            f.Group.Dispose();
            f.Buffer.Dispose();
        }

        _groups.Clear();
        _nearestGroups.Clear();
        _frames.Clear();
        foreach (var o in _owned)
        {
            o.Dispose();
        }

        foreach (var p in new[] { Prim, LayerOver, LayerAdd, Blend, MaskCombine, MaskApply, Color, Blur, DropTint, Over, Backdrop, Project, Encode, Present })
        {
            p.Dispose();
        }

        Layout.Dispose();
    }

    private GpuShader Shader(ShaderStage stage, string entry, ReadOnlySpan<byte> spirv, ReadOnlySpan<byte> msl, string mslName, EntryLayout layout, ReadOnlySpan<byte> wgsl, string wgslName) =>
        Device.CreateShader(new ShaderSource(stage, entry)
        {
            SpirV = spirv,
            Msl = msl,
            MslFunctionName = mslName,
            Layout = layout,
            Wgsl = wgsl,
            WgslEntryPoint = wgslName,
        });

    private GpuGraphicsPipeline Pipe(GpuShader vertex, OneShader.ShaderStage stage, string entry, ReadOnlySpan<byte> spirv, ReadOnlySpan<byte> msl, string mslName, EntryLayout layout, ReadOnlySpan<byte> wgsl, string wgslName, GpuFormat format, BlendState? blend, string label)
    {
        using var pixel = Shader(ShaderStage.Pixel, entry, spirv, msl, mslName, layout, wgsl, wgslName);
        return Device.CreateGraphicsPipeline(new GraphicsPipelineDesc
        {
            Layout = Layout,
            Vertex = vertex,
            Pixel = pixel,
            VertexBuffers = [InstanceLayout],
            Topology = PrimitiveTopology.TriangleList,
            Cull = CullMode.None,
            ColorTargets = [new ColorTargetState(format) { Blend = blend }],
            Label = label,
        });
    }
}
