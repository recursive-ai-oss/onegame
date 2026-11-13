using System.Runtime.InteropServices;
using RecursiveAI.OneGpu;
using OneGame.Render3D.Shaders;

namespace OneGame.Render3D;

public sealed partial class Renderer3D
{
    private GpuPipelineLayout? _debugLayout;
    private GpuShader? _debugVs;
    private GpuShader? _debugPs;
    private VertexBufferLayout? _debugVertex;
    private GpuBuffer? _debugBuffer;
    private GpuBindGroup? _debugSet;
    private GpuBuffer? _debugSetFor;
    private DebugDraw3D.Primitive[] _debugStaging = new DebugDraw3D.Primitive[256];
    private int _debugCount;
    private int _debugTested;
    private int _debugCapacity;

    /// <summary>Instanced debug draw calls issued by the last frame (zero to two).</summary>
    public int DebugDrawCalls { get; private set; }

    private void EnsureDebug()
    {
        if (_debugLayout is not null)
        {
            return;
        }

        _debugLayout = Own(Device.CreatePipelineLayout(ShaderLayouts.Bindings([Debug3D.VSDebug.Layout, Debug3D.PSDebug.Layout])));
        _debugVs = Own(Device.CreateShader(new ShaderSource(ShaderStage.Vertex, Debug3D.VSDebug.EntryPoint) { SpirV = Debug3D.VSDebug.SpirV, Wgsl = Debug3D.VSDebug.Wgsl, WgslEntryPoint = Debug3D.VSDebug.WgslEntryPoint, Msl = Debug3D.VSDebug.Msl, MslFunctionName = Debug3D.VSDebug.MslFunctionName, Layout = Debug3D.VSDebug.Layout }));
        _debugPs = Own(Device.CreateShader(new ShaderSource(ShaderStage.Pixel, Debug3D.PSDebug.EntryPoint) { SpirV = Debug3D.PSDebug.SpirV, Wgsl = Debug3D.PSDebug.Wgsl, WgslEntryPoint = Debug3D.PSDebug.WgslEntryPoint, Msl = Debug3D.PSDebug.Msl, MslFunctionName = Debug3D.PSDebug.MslFunctionName, Layout = Debug3D.PSDebug.Layout }));
        _debugVertex = ShaderLayouts.Vertex(Debug3D.VSDebug.Layout, VertexStepMode.Instance);
    }


    private void CollectDebug(DebugDraw3D debug)
    {
        _debugCount = debug.Count;
        _debugTested = debug.Tested.Count;
        DebugDrawCalls = 0;
        if (_debugCount == 0) { return; }
        EnsureDebug();
        if (_debugCount > _debugStaging.Length) { Array.Resize(ref _debugStaging, Math.Max(_debugCount, _debugStaging.Length * 2)); }
        debug.Tested.CopyTo(_debugStaging, 0);
        debug.Top.CopyTo(_debugStaging, _debugTested);
        if (_debugBuffer is null || _debugCount > _debugCapacity)
        {
            if (_debugBuffer is not null) { _owned.Remove(_debugBuffer); _deferred.Add(_debugBuffer); }
            _debugCapacity = _debugStaging.Length;
            _debugBuffer = Own(Device.CreateBuffer(new BufferDesc((ulong)_debugCapacity * 64, BufferUsage.Vertex | BufferUsage.CopyDestination) { Label = "debug3d" }));
        }
        Device.WriteBuffer(_debugBuffer, 0, MemoryMarshal.AsBytes(_debugStaging.AsSpan(0, _debugCount)));
    }

    private void DrawDebug(GpuRenderPass pass, int samples)
    {
        if (_debugCount == 0) { return; }
        if (_debugSet is null || !ReferenceEquals(_debugSetFor, _frameBuffer))
        {
            if (_debugSet is not null) { _owned.Remove(_debugSet); _deferred.Add(_debugSet); }
            _debugSet = Own(Device.CreateBindGroup(_debugLayout!, 0, [BindGroupEntry.ForBuffer(0, _frameBuffer, 0, (ulong)Marshal.SizeOf<FrameConstants>())]));
            _debugSetFor = _frameBuffer;
        }
        for (var mode = 0; mode < 2; mode++)
        {
            var count = mode == 0 ? _debugTested : _debugCount - _debugTested;
            if (count == 0) { continue; }
            pass.SetPipeline(DebugPipeline(samples, mode == 1));
            pass.SetBindGroup(0, _debugSet);
            pass.SetVertexBuffer(0, _debugBuffer!, mode == 0 ? 0 : (ulong)_debugTested * 64);
            pass.Draw(9, (uint)count);
            DebugDrawCalls++;
        }
    }

    private GpuGraphicsPipeline DebugPipeline(int samples, bool top)
    {
        var key = -200 - samples - (top ? 16 : 0);
        if (!_pipelines.TryGetValue(key, out var pipeline))
        {
            pipeline = Device.CreateGraphicsPipeline(new GraphicsPipelineDesc
            {
                Layout = _debugLayout!, Vertex = _debugVs!, Pixel = _debugPs!,
                VertexBuffers = [_debugVertex!], Topology = PrimitiveTopology.TriangleList, Cull = CullMode.None,
                ColorTargets = [new ColorTargetState(HdrFormat) { Blend = BlendState.Premultiplied }],
                DepthStencil = new DepthStencilState(DepthFormat) { DepthWrite = false, DepthCompare = top ? CompareFunction.Always : CompareFunction.LessEqual },
                SampleCount = (uint)samples, Label = top ? "debug3d on top" : "debug3d depth tested",
            });
            _pipelines[key] = pipeline;
        }
        return pipeline;
    }
}
