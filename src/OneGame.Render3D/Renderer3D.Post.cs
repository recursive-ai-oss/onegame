using System.Runtime.InteropServices;
using OneGame.Gpu;
using OneGame.Gpu.Graph;
using OneGame.Render3D.Shaders;

namespace OneGame.Render3D;

public sealed partial class Renderer3D
{
    private const int MaxBloomLevels = 5;
    private const int PostSlots = (MaxBloomLevels * 2) + 1;

    private static readonly string[] DownNames = ["bloom_down_0", "bloom_down_1", "bloom_down_2", "bloom_down_3", "bloom_down_4"];
    private static readonly string[] UpNames = ["bloom_up_0", "bloom_up_1", "bloom_up_2", "bloom_up_3", "bloom_up_4"];
    private static readonly string[] LevelNames = ["bloom_0", "bloom_1", "bloom_2", "bloom_3", "bloom_4"];

    private readonly PostConstants[] _postConstants = new PostConstants[PostSlots];
    private readonly Dictionary<(GpuTexture, GpuTexture), GpuBindGroup> _postGroups = [];
    private readonly Dictionary<GpuFormat, GpuGraphicsPipeline> _tonemapPipelines = [];
    private readonly BloomPass[] _downPasses = new BloomPass[MaxBloomLevels];
    private readonly BloomPass[] _upPasses = new BloomPass[MaxBloomLevels];
    private readonly TonemapPass _tonemapPass = new();
    private readonly GraphTexture[] _bloomLevels = new GraphTexture[MaxBloomLevels];
    private GpuPipelineLayout _postLayout = null!;
    private GpuShader _postVs = null!;
    private GpuShader _downPs = null!;
    private GpuShader _upPs = null!;
    private GpuShader _tonemapPs = null!;
    private GpuGraphicsPipeline _downPipeline = null!;
    private GpuGraphicsPipeline _upPipeline = null!;
    private GpuBuffer _postBuffer = null!;

    private void InitPost()
    {
        var device = Device;
        ReadOnlySpan<ResourceBinding> bindings =
        [
            new(0, 0, 1, ResourceKind.ConstantBuffer, ShaderStage.Pixel) { Dynamic = true },
            new(0, 1, 1, ResourceKind.SampledTexture, ShaderStage.Pixel),
            new(0, 2, 1, ResourceKind.SampledTexture, ShaderStage.Pixel),
            new(0, 3, 1, ResourceKind.Sampler, ShaderStage.Pixel),
        ];
        _postLayout = Own(device.CreatePipelineLayout(bindings));
        _postVs = Own(device.CreateShader(ShaderStage.Vertex, Post.VSFull.SpirV, Post.VSFull.EntryPoint));
        _downPs = Own(device.CreateShader(ShaderStage.Pixel, Post.PSDown.SpirV, Post.PSDown.EntryPoint));
        _upPs = Own(device.CreateShader(ShaderStage.Pixel, Post.PSUp.SpirV, Post.PSUp.EntryPoint));
        _tonemapPs = Own(device.CreateShader(ShaderStage.Pixel, Post.PSTonemap.SpirV, Post.PSTonemap.EntryPoint));
        _postBuffer = Own(device.CreateBuffer(new BufferDesc((ulong)PostSlots * GpuDevice.DynamicOffsetAlignment, BufferUsage.Uniform | BufferUsage.CopyDestination) { Label = "3d post" }));
        _postSampler = Own(device.CreateSampler(SamplerDesc.Linear));
        _downPipeline = Own(device.CreateGraphicsPipeline(PostPipeline(_downPs, HdrFormat, null, "bloom down")));
        var add = new BlendComponent(BlendFactor.One, BlendFactor.One, BlendOperation.Add);
        _upPipeline = Own(device.CreateGraphicsPipeline(PostPipeline(_upPs, HdrFormat, new BlendState(add, add), "bloom up")));
        for (var i = 0; i < MaxBloomLevels; i++)
        {
            _downPasses[i] = new BloomPass { Owner = this, Name = DownNames[i], Slot = i, Down = true };
            _upPasses[i] = new BloomPass { Owner = this, Name = UpNames[i], Slot = MaxBloomLevels + i, Down = false };
        }
    }

    private GpuSampler _postSampler = null!;

    private GraphicsPipelineDesc PostPipeline(GpuShader pixel, GpuFormat format, BlendState? blend, string label) => new()
    {
        Layout = _postLayout,
        Vertex = _postVs,
        Pixel = pixel,
        Topology = PrimitiveTopology.TriangleList,
        Cull = CullMode.None,
        ColorTargets = [new ColorTargetState(format) { Blend = blend }],
        Label = label,
    };

    private void DisposePost()
    {
        foreach (var g in _postGroups.Values)
        {
            g.Dispose();
        }

        _postGroups.Clear();
        foreach (var p in _tonemapPipelines.Values)
        {
            p.Dispose();
        }

        _tonemapPipelines.Clear();
    }

    private GpuBindGroup PostGroup(GpuTexture color, GpuTexture bloom)
    {
        if (_postGroups.TryGetValue((color, bloom), out var group))
        {
            return group;
        }

        // Pooled graph textures come back as the same objects every frame; a pool that churns would grow this table, so it is bounded.
        if (_postGroups.Count >= 64)
        {
            foreach (var g in _postGroups.Values)
            {
                _deferred.Add(g);
            }

            _postGroups.Clear();
        }

        group = Device.CreateBindGroup(_postLayout, 0,
        [
            BindGroupEntry.ForBuffer(0, _postBuffer, 0, 16),
            BindGroupEntry.ForTexture(1, color),
            BindGroupEntry.ForTexture(2, bloom),
            BindGroupEntry.ForSampler(3, _postSampler),
        ]);
        _postGroups[(color, bloom)] = group;
        return group;
    }

    private GpuGraphicsPipeline TonemapPipeline(GpuFormat format)
    {
        if (!_tonemapPipelines.TryGetValue(format, out var pipeline))
        {
            pipeline = Device.CreateGraphicsPipeline(PostPipeline(_tonemapPs, format, null, "tonemap"));
            _tonemapPipelines[format] = pipeline;
        }

        return pipeline;
    }

    private void AddPostPasses(RenderGraph graph, PostSettings post, bool bloomAllowed, GraphTexture hdr, GraphTexture target, int width, int height, GpuFormat targetFormat)
    {
        var levels = 0;
        GraphTexture bloom = default;
        if (post.Bloom && bloomAllowed && post.BloomStrength > 0f)
        {
            var w = Math.Max(width / 2, 1);
            var h = Math.Max(height / 2, 1);
            var src = hdr;
            var sw = width;
            var sh = height;
            for (var i = 0; i < MaxBloomLevels; i++)
            {
                var level = graph.CreateTexture(LevelNames[i], new TextureDesc((uint)w, (uint)h, HdrFormat, TextureUsage.None));
                _bloomLevels[i] = level;
                var pass = _downPasses[i];
                pass.Source = src;
                pass.Target = level;
                _postConstants[i].P = new Vector4(1f / sw, 1f / sh, i == 0 ? 1f : 0f, 0f);
                graph.AddPass(pass);
                levels++;
                src = level;
                sw = w;
                sh = h;
                if (w == 1 && h == 1)
                {
                    break;
                }

                w = Math.Max(w / 2, 1);
                h = Math.Max(h / 2, 1);
            }

            // Each smaller level is blurred up and added onto the next larger one.
            for (var i = levels - 1; i >= 1; i--)
            {
                var pass = _upPasses[i];
                pass.Source = _bloomLevels[i];
                pass.Target = _bloomLevels[i - 1];
                var lw = Math.Max(width >> (i + 1), 1);
                var lh = Math.Max(height >> (i + 1), 1);
                _postConstants[MaxBloomLevels + i].P = new Vector4(1f / lw, 1f / lh, 0f, 0f);
                graph.AddPass(pass);
            }

            bloom = _bloomLevels[0];
        }

        var encode = targetFormat is GpuFormat.Rgba8Unorm or GpuFormat.Bgra8Unorm or GpuFormat.Rgb10A2Unorm;
        _postConstants[PostSlots - 1].P = new Vector4(post.Exposure, levels > 0 ? post.BloomStrength : 0f, (float)(int)post.ToneMapper, encode ? 1f : 0f);
        Device.WriteBuffer(_postBuffer, 0, MemoryMarshal.AsBytes(new ReadOnlySpan<PostConstants>(_postConstants)));

        _tonemapPass.Owner = this;
        _tonemapPass.Hdr = hdr;
        _tonemapPass.Bloom = bloom;
        _tonemapPass.Target = target;
        _tonemapPass.Format = targetFormat;
        graph.AddPass(_tonemapPass);
    }

    private sealed class BloomPass : IGraphPass
    {
        public Renderer3D? Owner;
        public string Name { get; init; } = "";
        public int Slot { get; init; }
        public bool Down { get; init; }
        public GraphTexture Source;
        public GraphTexture Target;

        public void Setup(PassBuilder builder)
        {
            builder.Read(Source);
            builder.ColorTarget(Target, Down ? LoadOp.DontCare : LoadOp.Load);
        }

        public void Execute(PassContext context)
        {
            var r = Owner!;
            var pass = context.Render;
            pass.SetPipeline(Down ? r._downPipeline : r._upPipeline);
            Span<uint> offset = stackalloc uint[1];
            offset[0] = (uint)Slot * GpuDevice.DynamicOffsetAlignment;
            pass.SetBindGroup(0, r.PostGroup(context.Texture(Source), r._white), offset);
            pass.Draw(3);
        }
    }

    private sealed class TonemapPass : IGraphPass
    {
        public Renderer3D? Owner;
        public GraphTexture Hdr;
        public GraphTexture Bloom;
        public GraphTexture Target;
        public GpuFormat Format;

        public string Name => "tonemap";

        public void Setup(PassBuilder builder)
        {
            builder.Read(Hdr);
            if (Bloom.IsValid)
            {
                builder.Read(Bloom);
            }

            builder.ColorTarget(Target, LoadOp.DontCare);
        }

        public void Execute(PassContext context)
        {
            var r = Owner!;
            var pass = context.Render;
            pass.SetPipeline(r.TonemapPipeline(Format));
            Span<uint> offset = stackalloc uint[1];
            offset[0] = (PostSlots - 1) * GpuDevice.DynamicOffsetAlignment;
            pass.SetBindGroup(0, r.PostGroup(context.Texture(Hdr), Bloom.IsValid ? context.Texture(Bloom) : r._white), offset);
            pass.Draw(3);
        }
    }
}
