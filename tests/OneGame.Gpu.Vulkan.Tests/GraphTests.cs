using OneGame.Gpu.Graph;
using OneGame.Gpu.Vulkan.Tests.Shaders;

namespace OneGame.Gpu.Vulkan.Tests;

/// <summary>The render graph on a real device, with the validation layer (and its synchronization checks) required to stay silent.</summary>
public sealed class GraphTests
{
    /// <summary>Draws Src, sampled, over all of Dst.</summary>
    sealed class CopyPass : IGraphPass, IDisposable
    {
        readonly GpuDevice _d;
        readonly GpuShader _vs, _ps;
        readonly GpuPipelineLayout _layout;
        readonly GpuGraphicsPipeline _pipeline;
        readonly GpuSampler _sampler;
        readonly Dictionary<GpuTexture, GpuBindGroup> _groups = [];

        public CopyPass(GpuDevice d, string name, GpuFormat format = GpuFormat.Rgba8Unorm)
        {
            _d = d;
            Name = name;
            _vs = d.CreateShader(new ShaderSource(ShaderStage.Vertex, Sample2D.VSMain.EntryPoint) { SpirV = Sample2D.VSMain.SpirV, Wgsl = Sample2D.VSMain.Wgsl, WgslEntryPoint = Sample2D.VSMain.WgslEntryPoint, Msl = Sample2D.VSMain.Msl, MslFunctionName = Sample2D.VSMain.MslFunctionName, Bindings = Sample2D.Bindings.All });
            _ps = d.CreateShader(new ShaderSource(ShaderStage.Pixel, Sample2D.PSMain.EntryPoint) { SpirV = Sample2D.PSMain.SpirV, Wgsl = Sample2D.PSMain.Wgsl, WgslEntryPoint = Sample2D.PSMain.WgslEntryPoint, Msl = Sample2D.PSMain.Msl, MslFunctionName = Sample2D.PSMain.MslFunctionName, Bindings = Sample2D.Bindings.All });
            _layout = d.CreatePipelineLayout([new ResourceBinding(0, 0, 1, ResourceKind.SampledTexture, ShaderStage.Pixel), new ResourceBinding(0, 1, 1, ResourceKind.Sampler, ShaderStage.Pixel)]);
            _pipeline = d.CreateGraphicsPipeline(new GraphicsPipelineDesc { Layout = _layout, Vertex = _vs, Pixel = _ps, ColorTargets = [new ColorTargetState(format)] });
            _sampler = d.CreateSampler(SamplerDesc.Nearest);
        }

        public string Name { get; }

        public GraphTexture Src { get; set; }

        public GraphTexture Dst { get; set; }

        public GpuTexture? LastDst { get; private set; }

        public void Setup(PassBuilder b)
        {
            b.Read(Src);
            b.ColorTarget(Dst);
        }

        public void Execute(PassContext ctx)
        {
            var src = ctx.Texture(Src);
            LastDst = ctx.Texture(Dst);
            if (!_groups.TryGetValue(src, out var group))
            {
                group = _d.CreateBindGroup(_layout, 0, [BindGroupEntry.ForTexture(0, src), BindGroupEntry.ForSampler(1, _sampler)]);
                _groups[src] = group;
            }

            ctx.Render.SetPipeline(_pipeline);
            ctx.Render.SetBindGroup(0, group);
            ctx.Render.Draw(3);
        }

        public void Dispose()
        {
            foreach (var g in _groups.Values)
            {
                g.Dispose();
            }

            _sampler.Dispose();
            _pipeline.Dispose();
            _layout.Dispose();
            _vs.Dispose();
            _ps.Dispose();
        }
    }

    /// <summary>Clears Dst to a color.</summary>
    sealed class ClearPass(string name, float r, float g, float b) : IGraphPass
    {
        public string Name => name;

        public GraphTexture Dst { get; set; }

        public GpuTexture? LastDst { get; private set; }

        public void Setup(PassBuilder builder) => builder.ColorTarget(Dst, LoadOp.Clear, r, g, b, 1);

        public void Execute(PassContext context) => LastDst = context.Texture(Dst);
    }

    static readonly TextureDesc Small = new(8, 8, GpuFormat.Rgba8Unorm, TextureUsage.None);

    static async Task<byte[]> ReadAsync(GpuDevice d, GpuTexture t)
    {
        var bytes = new byte[t.Width * t.Height * 4];
        await d.ReadTextureAsync(t, 0, 0, bytes);
        return bytes;
    }

    static GpuTexture Output(GpuDevice d) => d.CreateTexture(new TextureDesc(8, 8, GpuFormat.Rgba8Unorm, TextureUsage.RenderTarget | TextureUsage.CopySource));

    [Fact]
    public void PassesRunInDependencyOrderAndUnusedOnesAreCulled()
    {
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        using var graph = new RenderGraph(d);
        using var output = Output(d);
        var log = new List<string>();

        graph.BeginFrame();
        var t1 = graph.CreateTexture("t1", Small);
        var t2 = graph.CreateTexture("t2", Small);
        var unused = graph.CreateTexture("unused", Small);
        var backbuffer = graph.ImportTexture("backbuffer", output);
        graph.AddPass("a", b => b.ColorTarget(t1, LoadOp.Clear, 1, 0, 0, 1), c => log.Add(c.PassName));
        graph.AddPass("dead", b => b.ColorTarget(unused, LoadOp.Clear, 0, 1, 0, 1), c => log.Add(c.PassName));
        graph.AddPass("b", b => { b.Read(t1); b.ColorTarget(t2, LoadOp.Clear, 0, 0, 1, 1); }, c => log.Add(c.PassName));
        graph.AddPass("present", b => { b.Read(t2); b.ColorTarget(backbuffer, LoadOp.Clear, 0, 0, 0, 1); }, c => log.Add(c.PassName));
        graph.Execute();
        d.WaitIdle();

        Assert.Equal(["a", "b", "present"], log);
        var report = graph.Describe();
        Assert.Equal(["a", "dead", "b", "present"], report.Passes.Select(p => p.Name));
        Assert.Equal([0, -1, 1, 2], report.Passes.Select(p => p.Order));
        Assert.Equal([false, true, false, false], report.Passes.Select(p => p.Culled));
        Assert.Equal(1, report.Stats.CulledPasses);
        Assert.False(report.Resources.Single(r => r.Name == "unused").Used);
        Assert.Equal(-1, report.Resources.Single(r => r.Name == "unused").Slot);
        Assert.Equal("a", report.Resources.Single(r => r.Name == "t1").FirstPass);
        Assert.Equal("b", report.Resources.Single(r => r.Name == "t1").LastPass);
    }

    [Fact]
    public async Task AnOverwrittenWriteIsCulledAndSideEffectsSurvive()
    {
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        using var graph = new RenderGraph(d);
        using var output = Output(d);
        var log = new List<string>();

        graph.BeginFrame();
        var scratch = graph.CreateTexture("scratch", Small);
        var screen = graph.ImportTexture("screen", output);
        graph.AddPass("first", b => b.ColorTarget(screen, LoadOp.Clear, 1, 0, 0, 1), c => log.Add("first"));
        graph.AddPass("second", b => b.ColorTarget(screen, LoadOp.Clear, 0, 1, 0, 1), c => log.Add("second"));
        graph.AddPass("probe", b => { b.ColorTarget(scratch, LoadOp.Clear, 0, 0, 0, 1); b.SideEffect(); }, c => log.Add("probe"));
        graph.Execute();
        Assert.Equal(["second", "probe"], log);

        var px = await ReadAsync(d, output);
        Assert.Equal((0, 255, 0), (px[0], px[1], px[2]));
    }

    [Fact]
    public async Task TransientsWithDisjointLifetimesShareMemoryAndPoolAcrossFrames()
    {
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        using var graph = new RenderGraph(d);
        using var output = Output(d);
        var clear = new ClearPass("clear", 1, 0.5f, 0);
        using var c1 = new CopyPass(d, "c1");
        using var c2 = new CopyPass(d, "c2");
        using var c3 = new CopyPass(d, "c3");
        using var c4 = new CopyPass(d, "c4");

        GpuTexture[] first = [];
        for (var frame = 0; frame < 3; frame++)
        {
            graph.BeginFrame();
            var t1 = graph.CreateTexture("t1", Small);
            var t2 = graph.CreateTexture("t2", Small);
            var t3 = graph.CreateTexture("t3", Small);
            var t4 = graph.CreateTexture("t4", Small);
            var screen = graph.ImportTexture("screen", output);
            clear.Dst = t1;
            c1.Src = t1; c1.Dst = t2;
            c2.Src = t2; c2.Dst = t3;
            c3.Src = t3; c3.Dst = t4;
            c4.Src = t4; c4.Dst = screen;
            graph.AddPass(clear);
            graph.AddPass(c1);
            graph.AddPass(c2);
            graph.AddPass(c3);
            graph.AddPass(c4);
            graph.Execute();
            GpuTexture[] physical = [clear.LastDst!, c1.LastDst!, c2.LastDst!, c3.LastDst!];

            // t1 [0..1] and t3 [2..3] never overlap, nor do t2 [1..2] and t4 [3..4]: two textures serve four.
            Assert.Same(physical[0], physical[2]);
            Assert.Same(physical[1], physical[3]);
            Assert.NotSame(physical[0], physical[1]);
            var stats = graph.Stats;
            Assert.Equal(2, stats.PhysicalTextures);
            Assert.Equal(stats.TransientBytes * 2, stats.UnaliasedBytes);
            if (frame == 0)
            {
                first = physical;
                Assert.Equal(2, stats.Created);
            }
            else
            {
                // Same graph next frame: the pool hands back the same memory.
                Assert.Equal(0, stats.Created);
                Assert.Equal(2, stats.Reused);
                Assert.Same(first[0], physical[0]);
                Assert.Same(first[1], physical[1]);
            }

        }

        // 0.5 converts to 127 or 128 depending on the device's rounding.
        var px = await ReadAsync(d, output);
        Assert.Equal((255, 0), (px[0], px[2]));
        Assert.InRange(px[1], 127, 128);
    }

    [Fact]
    public async Task ComputeRasterAndCustomPassesAreOrderedByTheirDeclarations()
    {
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        using var graph = new RenderGraph(d);
        using var output = Output(d);
        using var tex = d.CreateTexture(new TextureDesc(16, 16, GpuFormat.Rgba8Unorm, TextureUsage.Storage | TextureUsage.Sampled));
        using var cs = d.CreateShader(new ShaderSource(ShaderStage.Compute, StorageImage.CSMain.EntryPoint) { SpirV = StorageImage.CSMain.SpirV, Wgsl = StorageImage.CSMain.Wgsl, WgslEntryPoint = StorageImage.CSMain.WgslEntryPoint, Msl = StorageImage.CSMain.Msl, MslFunctionName = StorageImage.CSMain.MslFunctionName, Bindings = StorageImage.Bindings.All, ComputeGroupX = StorageImage.CSMain.ThreadGroupSizeX, ComputeGroupY = StorageImage.CSMain.ThreadGroupSizeY, ComputeGroupZ = StorageImage.CSMain.ThreadGroupSizeZ });
        using var vs = d.CreateShader(new ShaderSource(ShaderStage.Vertex, StorageImage.VSMain.EntryPoint) { SpirV = StorageImage.VSMain.SpirV, Wgsl = StorageImage.VSMain.Wgsl, WgslEntryPoint = StorageImage.VSMain.WgslEntryPoint, Msl = StorageImage.VSMain.Msl, MslFunctionName = StorageImage.VSMain.MslFunctionName, Bindings = StorageImage.Bindings.All });
        using var ps = d.CreateShader(new ShaderSource(ShaderStage.Pixel, StorageImage.PSMain.EntryPoint) { SpirV = StorageImage.PSMain.SpirV, Wgsl = StorageImage.PSMain.Wgsl, WgslEntryPoint = StorageImage.PSMain.WgslEntryPoint, Msl = StorageImage.PSMain.Msl, MslFunctionName = StorageImage.PSMain.MslFunctionName, Bindings = StorageImage.Bindings.All });
        using var computeLayout = d.CreatePipelineLayout([new ResourceBinding(0, 0, 1, ResourceKind.StorageTexture, ShaderStage.Compute)]);
        using var drawLayout = d.CreatePipelineLayout([new ResourceBinding(1, 0, 1, ResourceKind.SampledTexture, ShaderStage.Pixel)]);
        using var computeGroup = d.CreateBindGroup(computeLayout, 0, [BindGroupEntry.ForTexture(0, tex)]);
        using var drawGroup = d.CreateBindGroup(drawLayout, 1, [BindGroupEntry.ForTexture(0, tex)]);
        using var computePipeline = d.CreateComputePipeline(new ComputePipelineDesc { Layout = computeLayout, Compute = cs });
        using var pipeline = d.CreateGraphicsPipeline(new GraphicsPipelineDesc { Layout = drawLayout, Vertex = vs, Pixel = ps, ColorTargets = [new ColorTargetState(GpuFormat.Rgba8Unorm)] });
        using var wide = d.CreateTexture(new TextureDesc(16, 16, GpuFormat.Rgba8Unorm, TextureUsage.RenderTarget | TextureUsage.CopySource));

        graph.BeginFrame();
        var image = graph.ImportTexture("image", tex);
        var screen = graph.ImportTexture("screen", wide);
        graph.AddPass("fill", b => { b.Compute(); b.WriteStorage(image); }, c =>
        {
            c.Compute.SetPipeline(computePipeline);
            c.Compute.SetBindGroup(0, computeGroup);
            c.Compute.Dispatch(2, 2);
        });
        graph.AddPass("draw", b => { b.Read(image); b.ColorTarget(screen); }, c =>
        {
            c.Render.SetPipeline(pipeline);
            c.Render.SetBindGroup(1, drawGroup);
            c.Render.Draw(3);
        });
        graph.AddPass("probe", b => { b.CopySource(screen); b.SideEffect(); }, c => Assert.NotNull(c.Encoder));
        graph.Execute();

        var px = new byte[16 * 16 * 4];
        await d.ReadTextureAsync(wide, 0, 0, px);
        Assert.InRange(px[(5 * 16 + 15) * 4], 254, 255);
        Assert.InRange(px[(15 * 16 + 5) * 4 + 1], 254, 255);
        var report = graph.Describe();
        Assert.Equal(["compute", "raster", "custom"], report.Passes.Select(p => p.Type));
    }

    [Fact]
    public void BadDeclarationsAreRejected()
    {
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        using var graph = new RenderGraph(d);
        using var output = Output(d);

        // Reading something nobody wrote.
        graph.BeginFrame();
        var t = graph.CreateTexture("never-written", Small);
        var screen = graph.ImportTexture("screen", output);
        graph.AddPass("reader", b => { b.Read(t); b.ColorTarget(screen); }, _ => { });
        var ex = Assert.Throws<RenderGraphException>(graph.Execute);
        Assert.Contains("never-written", ex.Message);

        // A stale handle from the previous frame.
        graph.BeginFrame();
        var old = t;
        graph.AddPass("p", b => Assert.Throws<RenderGraphException>(() => b.Read(old)), _ => { });
        graph.Cancel();

        // Reading and rendering to the same texture in one pass.
        graph.BeginFrame();
        var same = graph.CreateTexture("same", Small);
        graph.AddPass("seed", b => b.ColorTarget(same), _ => { });
        Assert.Throws<RenderGraphException>(() => graph.AddPass("loop", b => { b.Read(same); b.ColorTarget(same, LoadOp.Load); }, _ => { }));
        graph.Cancel();

        // Using a resource the pass did not declare.
        graph.BeginFrame();
        var a = graph.CreateTexture("a", Small);
        var b2 = graph.CreateTexture("b", Small);
        var screen2 = graph.ImportTexture("screen", output);
        graph.AddPass("makes", b => { b.ColorTarget(a); b.ColorTarget(b2); }, _ => { });
        graph.AddPass("sneaky", b => { b.Read(a); b.ColorTarget(screen2); }, c => c.Texture(b2));
        Assert.Throws<RenderGraphException>(graph.Execute);
        d.WaitIdle();
    }

    [Fact]
    public void ReportNamesGpuTimeForPasses()
    {
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        if (!d.Capabilities.Timestamps)
        {
            return;
        }

        using var graph = new RenderGraph(d);
        using var output = Output(d);
        Assert.True(graph.TimingEnabled);
        for (var i = 0; i < 4; i++)
        {
            graph.BeginFrame();
            var s = graph.ImportTexture("screen", output);
            graph.AddPass("one", b => b.ColorTarget(s, LoadOp.Clear, 1, 0, 0, 1), _ => { });
            graph.Execute();
        }

        d.WaitIdle();
        var report = graph.Describe();
        Assert.True(report.Timing);
        var pass = Assert.Single(report.Passes);
        Assert.NotNull(pass.GpuMicroseconds);
        Assert.True(pass.GpuMicroseconds >= 0);
        Assert.True(report.TimingFrame >= 1);
    }

    sealed class DispatchPass(GpuComputePipeline pipeline, GpuBindGroup group) : IGraphPass
    {
        public string Name => "compute";

        public void Setup(PassBuilder builder)
        {
            builder.Compute();
            builder.SideEffect();
        }

        public void Execute(PassContext context)
        {
            context.Compute.SetPipeline(pipeline);
            context.Compute.SetBindGroup(0, group);
            context.Compute.Dispatch(4);
        }
    }

    sealed class DrawPass(string name, GpuGraphicsPipeline pipeline) : IGraphPass
    {
        public string Name => name;

        public GraphTexture Target { get; set; }

        public GraphTexture Depth { get; set; }

        public GraphTexture Input0 { get; set; }

        public GraphTexture Input1 { get; set; }

        public void Setup(PassBuilder builder)
        {
            if (Input0.IsValid)
            {
                builder.Read(Input0);
            }

            if (Input1.IsValid)
            {
                builder.Read(Input1);
            }

            builder.ColorTarget(Target, LoadOp.Clear, 0, 0, 0, 1);
            if (Depth.IsValid)
            {
                builder.DepthTarget(Depth);
            }
        }

        public void Execute(PassContext context)
        {
            context.Render.SetPipeline(pipeline);
            context.Render.Draw(3);
        }
    }

    sealed class MipPass : IGraphPass
    {
        public string Name => "mips";

        public GraphTexture Texture { get; set; }

        public void Setup(PassBuilder builder) => builder.ReadWrite(Texture);

        public void Execute(PassContext context) => context.Encoder.GenerateMips(context.Texture(Texture));
    }

    sealed class SeedPass : IGraphPass
    {
        public string Name => "seed";

        public GraphTexture Texture { get; set; }

        public void Setup(PassBuilder builder) => builder.ColorTarget(Texture, LoadOp.Clear, 0.5f, 0.5f, 0.5f, 1);

        public void Execute(PassContext context)
        {
        }
    }

    [Fact]
    public void SteadyStateGraphOfTenPassesAllocatesNothing()
    {
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        using var vs = d.CreateShader(new ShaderSource(ShaderStage.Vertex, Triangle.VSMain.EntryPoint) { SpirV = Triangle.VSMain.SpirV, Wgsl = Triangle.VSMain.Wgsl, WgslEntryPoint = Triangle.VSMain.WgslEntryPoint, Msl = Triangle.VSMain.Msl, MslFunctionName = Triangle.VSMain.MslFunctionName, Bindings = Triangle.Bindings.All });
        using var ps = d.CreateShader(new ShaderSource(ShaderStage.Pixel, Triangle.PSMain.EntryPoint) { SpirV = Triangle.PSMain.SpirV, Wgsl = Triangle.PSMain.Wgsl, WgslEntryPoint = Triangle.PSMain.WgslEntryPoint, Msl = Triangle.PSMain.Msl, MslFunctionName = Triangle.PSMain.MslFunctionName, Bindings = Triangle.Bindings.All });
        using var emptyLayout = d.CreatePipelineLayout([]);
        using var colorPipeline = d.CreateGraphicsPipeline(new GraphicsPipelineDesc { Layout = emptyLayout, Vertex = vs, Pixel = ps, ColorTargets = [new ColorTargetState(GpuFormat.Rgba8Unorm)] });
        using var depthPipeline = d.CreateGraphicsPipeline(new GraphicsPipelineDesc
        {
            Layout = emptyLayout,
            Vertex = vs,
            Pixel = ps,
            ColorTargets = [new ColorTargetState(GpuFormat.Rgba8Unorm)],
            DepthStencil = new DepthStencilState(GpuFormat.Depth32Float),
        });
        using var cs = d.CreateShader(new ShaderSource(ShaderStage.Compute, Fill.CSMain.EntryPoint) { SpirV = Fill.CSMain.SpirV, Wgsl = Fill.CSMain.Wgsl, WgslEntryPoint = Fill.CSMain.WgslEntryPoint, Msl = Fill.CSMain.Msl, MslFunctionName = Fill.CSMain.MslFunctionName, Bindings = Fill.Bindings.All, ComputeGroupX = Fill.CSMain.ThreadGroupSizeX, ComputeGroupY = Fill.CSMain.ThreadGroupSizeY, ComputeGroupZ = Fill.CSMain.ThreadGroupSizeZ });
        using var computeLayout = d.CreatePipelineLayout(
        [
            new ResourceBinding(0, 0, 1, ResourceKind.StorageBuffer, ShaderStage.Compute),
            new ResourceBinding(0, 1, 1, ResourceKind.ReadOnlyStorageBuffer, ShaderStage.Compute),
        ]);
        using var o = d.CreateBuffer(new BufferDesc(256, BufferUsage.Storage));
        using var i = d.CreateBuffer(new BufferDesc(256, BufferUsage.Storage | BufferUsage.CopyDestination));
        using var group = d.CreateBindGroup(computeLayout, 0, [BindGroupEntry.ForBuffer(0, o), BindGroupEntry.ForBuffer(1, i)]);
        using var computePipeline = d.CreateComputePipeline(new ComputePipelineDesc { Layout = computeLayout, Compute = cs });
        using var output = Output(d);
        using var graph = new RenderGraph(d);

        var scene = new DrawPass("scene", depthPipeline);
        var pre = new DrawPass("pre", colorPipeline);
        var blurA = new DrawPass("blur-a", colorPipeline);
        var blurB = new DrawPass("blur-b", colorPipeline);
        var bloom = new DrawPass("bloom", colorPipeline);
        var ui = new DrawPass("ui", colorPipeline);
        var compute = new DispatchPass(computePipeline, group);
        var mips = new MipPass();
        var seed = new SeedPass();
        var mipTexture = new TextureDesc(8, 8, GpuFormat.Rgba8Unorm, TextureUsage.None) { MipLevels = 4 };
        var depthDesc = new TextureDesc(8, 8, GpuFormat.Depth32Float, TextureUsage.None);

        void Frame()
        {
            graph.BeginFrame();
            var color = graph.CreateTexture("scene-color", Small);
            var depth = graph.CreateTexture("scene-depth", depthDesc);
            var half = graph.CreateTexture("half", Small);
            var half2 = graph.CreateTexture("half2", Small);
            var glow = graph.CreateTexture("glow", Small);
            var chain = graph.CreateTexture("mip-chain", mipTexture);
            var bloomOut = graph.CreateTexture("bloom-out", Small);
            var screen = graph.ImportTexture("screen", output);
            scene.Target = color; scene.Depth = depth;
            pre.Input0 = color; pre.Target = half;
            blurA.Input0 = half; blurA.Target = half2;
            blurB.Input0 = half2; blurB.Target = glow;
            seed.Texture = chain;
            mips.Texture = chain;
            bloom.Input0 = chain; bloom.Input1 = glow; bloom.Target = bloomOut;
            ui.Input0 = bloomOut; ui.Input1 = color; ui.Target = screen;
            graph.AddPass(compute);
            graph.AddPass(scene);
            graph.AddPass(pre);
            graph.AddPass(blurA);
            graph.AddPass(blurB);
            graph.AddPass(seed);
            graph.AddPass(mips);
            graph.AddPass(bloom);
            graph.AddPass(ui);
            graph.Execute();
        }

        // Pools (encoders, staging, lists) grow until they match how far the GPU lags the CPU, which varies by device; steady state is
        // three bursts in a row that allocate nothing.
        var zeroBursts = 0;
        for (var burst = 0; burst < 30 && zeroBursts < 3; burst++)
        {
            d.WaitIdle();
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var k = 0; k < 100; k++)
            {
                Frame();
            }

            zeroBursts = GC.GetAllocatedBytesForCurrentThread() == before ? zeroBursts + 1 : 0;
        }

        d.WaitIdle();
        Assert.Equal(3, zeroBursts);
        var report = graph.Describe();
        Assert.True(report.Stats.LivePasses >= 8, $"{report.Stats.LivePasses} passes ran");
    }
}
