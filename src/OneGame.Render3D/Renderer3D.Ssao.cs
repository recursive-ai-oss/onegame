using System.Runtime.InteropServices;
using OneGame.Gpu;
using OneGame.Gpu.Graph;
using OneGame.Render3D.Shaders;

namespace OneGame.Render3D;

// Screen-space ambient occlusion: a depth prepass writes view-space normals and distances, a hemisphere-sampling pass turns them into
// occlusion, a bilateral blur cleans it, and the forward shader dims indirect light by the result.
public sealed partial class Renderer3D
{
    private const GpuFormat GBufferFormat = GpuFormat.Rgba16Float;

    private readonly Dictionary<(GpuTexture, GpuTexture), GpuBindGroup> _ssaoGroups = [];
    private readonly PrepassPass _prepassPass = new();
    private readonly SsaoPass _ssaoRawPass = new() { Blur = false };
    private readonly SsaoPass _ssaoBlurPass = new() { Blur = true };
    private readonly SsaoConstants[] _ssaoConstants = new SsaoConstants[2];
    private GpuPipelineLayout _ssaoLayout = null!;
    private GpuGraphicsPipeline _ssaoPipeline = null!;
    private GpuGraphicsPipeline _blurPipeline = null!;
    private GpuBuffer _ssaoBuffer = null!;
    private bool _ssaoActive;

    private void InitSsao()
    {
        var device = Device;
        ReadOnlySpan<ResourceBinding> bindings =
        [
            new(0, 0, 1, ResourceKind.ConstantBuffer, ShaderStage.Pixel) { Dynamic = true },
            new(0, 1, 1, ResourceKind.SampledTexture, ShaderStage.Pixel),
            new(0, 2, 1, ResourceKind.SampledTexture, ShaderStage.Pixel),
        ];
        _ssaoLayout = Own(device.CreatePipelineLayout(bindings));
        var ssaoPs = Own(device.CreateShader(ShaderStage.Pixel, Ssao.PSSsao.SpirV, Ssao.PSSsao.EntryPoint));
        var blurPs = Own(device.CreateShader(ShaderStage.Pixel, Ssao.PSBlur.SpirV, Ssao.PSBlur.EntryPoint));
        GraphicsPipelineDesc Desc(GpuShader pixel, string label) => new()
        {
            Layout = _ssaoLayout,
            Vertex = _postVs,
            Pixel = pixel,
            Topology = PrimitiveTopology.TriangleList,
            Cull = CullMode.None,
            ColorTargets = [new ColorTargetState(GpuFormat.R8Unorm)],
            Label = label,
        };

        _ssaoPipeline = Own(device.CreateGraphicsPipeline(Desc(ssaoPs, "ssao")));
        _blurPipeline = Own(device.CreateGraphicsPipeline(Desc(blurPs, "ssao blur")));
        _ssaoBuffer = Own(device.CreateBuffer(new BufferDesc(2 * GpuDevice.DynamicOffsetAlignment, BufferUsage.Uniform | BufferUsage.CopyDestination) { Label = "3d ssao" }));
    }

    private void DisposeSsao()
    {
        foreach (var g in _ssaoGroups.Values)
        {
            g.Dispose();
        }

        _ssaoGroups.Clear();
    }

    private GpuBindGroup SsaoGroup(GpuTexture gbuffer, GpuTexture other)
    {
        if (_ssaoGroups.TryGetValue((gbuffer, other), out var group))
        {
            return group;
        }

        if (_ssaoGroups.Count >= 32)
        {
            foreach (var g in _ssaoGroups.Values)
            {
                _deferred.Add(g);
            }

            _ssaoGroups.Clear();
        }

        group = Device.CreateBindGroup(_ssaoLayout, 0,
        [
            BindGroupEntry.ForBuffer(0, _ssaoBuffer, 0, 48),
            BindGroupEntry.ForTexture(1, gbuffer),
            BindGroupEntry.ForTexture(2, other),
        ]);
        _ssaoGroups[(gbuffer, other)] = group;
        return group;
    }

    // Adds the prepass and the two occlusion passes; returns the finished occlusion texture, or an invalid handle when the effect is off.
    private GraphTexture AddSsaoPasses(RenderGraph graph, Scene3D scene, in QualitySettings q, int width, int height)
    {
        if (!_ssaoActive)
        {
            return default;
        }

        var aoW = q.SsaoHalfResolution ? Math.Max(width / 2, 1) : width;
        var aoH = q.SsaoHalfResolution ? Math.Max(height / 2, 1) : height;
        var gbuffer = graph.CreateTexture("ssao_gbuffer", new TextureDesc((uint)width, (uint)height, GBufferFormat, TextureUsage.None));
        var depth = graph.CreateTexture("ssao_depth", new TextureDesc((uint)width, (uint)height, DepthFormat, TextureUsage.None));
        var raw = graph.CreateTexture("ssao_raw", new TextureDesc((uint)aoW, (uint)aoH, GpuFormat.R8Unorm, TextureUsage.None));
        var ao = graph.CreateTexture("ssao", new TextureDesc((uint)aoW, (uint)aoH, GpuFormat.R8Unorm, TextureUsage.None));

        var proj = _frame.Proj;
        var constants = new SsaoConstants
        {
            A = new Vector4(proj.M11, proj.M22, scene.Camera.Orthographic ? 1f : 0f, MathF.Max(scene.Ssao.Radius, 1e-3f)),
            B = new Vector4(2f, 0.05f, Math.Max(q.SsaoSamples, 1), 0f),
            C = new Vector4(width, height, aoW, aoH),
        };
        _ssaoConstants[0] = constants;
        _ssaoConstants[1] = constants;
        Device.WriteBuffer(_ssaoBuffer, 0, MemoryMarshal.AsBytes(new ReadOnlySpan<SsaoConstants>(_ssaoConstants)));

        _prepassPass.Owner = this;
        _prepassPass.Gbuffer = gbuffer;
        _prepassPass.Depth = depth;
        graph.AddPass(_prepassPass);

        _ssaoRawPass.Owner = this;
        _ssaoRawPass.Gbuffer = gbuffer;
        _ssaoRawPass.Target = raw;
        _ssaoRawPass.Slot = 0;
        graph.AddPass(_ssaoRawPass);

        _ssaoBlurPass.Owner = this;
        _ssaoBlurPass.Gbuffer = gbuffer;
        _ssaoBlurPass.Source = raw;
        _ssaoBlurPass.Target = ao;
        _ssaoBlurPass.Slot = 1;
        graph.AddPass(_ssaoBlurPass);
        return ao;
    }

    private sealed class PrepassPass : IGraphPass
    {
        public Renderer3D? Owner;
        public GraphTexture Gbuffer;
        public GraphTexture Depth;

        public string Name => "depth_normal_prepass";

        public void Setup(PassBuilder builder)
        {
            builder.ColorTarget(Gbuffer, LoadOp.Clear, 0f, 0f, 0f, 0f);
            builder.DepthTarget(Depth, LoadOp.Clear, 1f, StoreOp.Discard);
        }

        public void Execute(PassContext context)
        {
            var r = Owner!;
            var pass = context.Render;
            pass.SetBindGroup(0, r.Set0(0, r._shadowDummy, r._white));
            r.DrawRegular(pass, r._opaque, r._opaqueCount, PipeKind.Prepass, 1, -1);
            r.DrawBatches(pass, PipeKind.Prepass, 1, 0, blend: false);
        }
    }

    private sealed class SsaoPass : IGraphPass
    {
        public Renderer3D? Owner;
        public bool Blur;
        public GraphTexture Gbuffer;
        public GraphTexture Source;
        public GraphTexture Target;
        public int Slot;

        public string Name => Blur ? "ssao_blur" : "ssao";

        public void Setup(PassBuilder builder)
        {
            builder.Read(Gbuffer);
            if (Blur)
            {
                builder.Read(Source);
            }

            builder.ColorTarget(Target, LoadOp.DontCare);
        }

        public void Execute(PassContext context)
        {
            var r = Owner!;
            var pass = context.Render;
            pass.SetPipeline(Blur ? r._blurPipeline : r._ssaoPipeline);
            Span<uint> offset = stackalloc uint[1];
            offset[0] = (uint)Slot * GpuDevice.DynamicOffsetAlignment;
            pass.SetBindGroup(0, r.SsaoGroup(context.Texture(Gbuffer), Blur ? context.Texture(Source) : r._white), offset);
            pass.Draw(3);
        }
    }
}
