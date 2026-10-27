using System.Runtime.InteropServices;
using System.Text.Json;
using OneGame.AotSmoke.Shaders;
using OneGame.Control;
using OneGame.Gpu;
using OneGame.Gpu.Graph;

namespace OneGame.AotSmoke;

/// <summary>Exercises the render graph, a compute dispatch (direct and indirect), cube and 3D textures, push-constant layouts and timestamps on a real device.</summary>
internal static class GraphSmoke
{
    sealed class ClearPass : IGraphPass
    {
        public string Name => "clear";

        public GraphTexture Target { get; set; }

        public void Setup(PassBuilder builder) => builder.ColorTarget(Target, LoadOp.Clear, 0, 1, 0, 1);

        public void Execute(PassContext context)
        {
        }
    }

    sealed class CopyPass : IGraphPass
    {
        public string Name => "copy";

        public GraphTexture Source { get; set; }

        public GraphTexture Target { get; set; }

        public void Setup(PassBuilder builder)
        {
            builder.CopySource(Source);
            builder.CopyDestination(Target);
        }

        public void Execute(PassContext context) =>
            context.Encoder.CopyTexture(context.Texture(Source), context.Texture(Target));
    }

    sealed class ComputePass(GpuComputePipeline pipeline, GpuBindGroup group, GpuBuffer indirect) : IGraphPass
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
            context.Compute.Dispatch(1);
            context.Compute.DispatchIndirect(indirect, 0);
        }
    }

    public static bool Run(GpuDevice device)
    {
        // Compute, direct and indirect, into a storage buffer.
        using var cs = device.CreateShader(new ShaderSource(ShaderStage.Compute, SmokeCompute.CSMain.EntryPoint) { SpirV = SmokeCompute.CSMain.SpirV, Msl = SmokeCompute.CSMain.Msl, MslFunctionName = SmokeCompute.CSMain.MslFunctionName, Layout = SmokeCompute.CSMain.Layout });
        using var layout = device.CreatePipelineLayout([new ResourceBinding(0, 0, 1, ResourceKind.StorageBuffer, ShaderStage.Compute)]);
        using var output = device.CreateBuffer(new BufferDesc(256, BufferUsage.Storage | BufferUsage.CopySource));
        using var group = device.CreateBindGroup(layout, 0, [BindGroupEntry.ForBuffer(0, output)]);
        using var pipeline = device.CreateComputePipeline(new ComputePipelineDesc { Layout = layout, Compute = cs });
        using var args = device.CreateBuffer(new BufferDesc(DispatchIndirectArgs.Size, BufferUsage.Indirect | BufferUsage.CopyDestination));
        device.WriteBuffer(args, 0, MemoryMarshal.AsBytes<DispatchIndirectArgs>([new DispatchIndirectArgs(4, 1, 1)]));

        // The graph: a cleared transient is copied into a second one, a third pass is culled, and the compute pass has a side effect.
        using var graph = new RenderGraph(device);
        using var screen = device.CreateTexture(new TextureDesc(8, 8, GpuFormat.Rgba8Unorm, TextureUsage.RenderTarget | TextureUsage.CopySource | TextureUsage.CopyDestination));
        var clear = new ClearPass();
        var copy = new CopyPass();
        var compute = new ComputePass(pipeline, group, args);
        var desc = new TextureDesc(8, 8, GpuFormat.Rgba8Unorm, TextureUsage.None);
        for (var frame = 0; frame < 3; frame++)
        {
            graph.BeginFrame();
            var a = graph.CreateTexture("a", desc);
            var unused = graph.CreateTexture("unused", desc);
            var target = graph.ImportTexture("screen", screen);
            clear.Target = a;
            copy.Source = a;
            copy.Target = target;
            graph.AddPass(clear);
            graph.AddPass(compute);
            graph.AddPass(copy);
            graph.AddPass("dead", b => b.ColorTarget(unused), _ => { });
            graph.Execute();
        }

        var values = new uint[64];
        device.ReadBuffer(output, 0, MemoryMarshal.AsBytes(values.AsSpan()));
        for (uint i = 0; i < 32; i++)
        {
            if (values[i] != i * 3 + 1)
            {
                Console.Error.WriteLine($"graph: compute value {i} is {values[i]}");
                return false;
            }
        }

        var px = new byte[8 * 8 * 4];
        device.ReadTexture(screen, 0, 0, px);
        if (px[0] != 0 || px[1] != 255 || px[2] != 0)
        {
            Console.Error.WriteLine($"graph: copied pixel is {px[0]},{px[1]},{px[2]}");
            return false;
        }

        var report = graph.Describe();
        var json = JsonSerializer.Serialize(report, ControlJson.Default.RenderGraphReport);
        if (report.Stats.CulledPasses != 1 || !json.Contains("\"dead\"", StringComparison.Ordinal) || report.Stats.Reused == 0)
        {
            Console.Error.WriteLine("graph: unexpected report " + json);
            return false;
        }

        // Cube and 3D textures, a push-constant layout and a bind group with a view of one face.
        using var cube = device.CreateTexture(new TextureDesc(4, 4, GpuFormat.Rgba8Unorm, TextureUsage.RenderTarget | TextureUsage.Sampled) { Dimension = TextureDimension.Cube, ArrayLayers = 6 });
        using var volume = device.CreateTexture(new TextureDesc(4, 4, GpuFormat.Rgba8Unorm, TextureUsage.Sampled | TextureUsage.CopyDestination) { Dimension = TextureDimension.D3, Depth = 4 });
        device.WriteTexture(volume, new TextureRegion(0, 0, 0, 0, 1, 2, 2, 2), new byte[2 * 2 * 2 * 4]);
        using var pushLayout = device.CreatePipelineLayout([], new PushConstantRange(16, ShaderStage.Pixel));
        device.WaitIdle();
        Console.WriteLine($"graph: {report.Stats.LivePasses} passes ran, {report.Stats.CulledPasses} culled, {report.Stats.PhysicalTextures} physical texture(s) for {report.Stats.TransientResources} transient(s); compute direct and indirect ok; timing {(graph.TimingEnabled ? "on" : "off")}");
        return true;
    }
}
