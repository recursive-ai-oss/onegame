using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using RecursiveAI.OneGpu;
using OneGame.Gpu.Graph;
using OneGame.Render3D.Shaders;

namespace OneGame.Render3D;

[InlineArray(16)]
internal struct ShaftLightRows
{
    private Vector4 _first;
}

/// <summary>Light shaft pass constants (Shafts.hlsl, <c>ShaftParams</c>).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct ShaftConstants
{
    public Vector4 A;
    public Vector4 B;
    public Vector4 C;
    public ShaftLightRows Lights;
}

// Light shafts. Technique: a ray march per pixel at a fraction of the frame size, not a froxel volume. The shadow maps already exist in
// a form a ray can look up directly, the march needs no persistent 3D texture (memory stays at one small image), it works unchanged for
// directional, spot and point lights, and the quality ladder has two plain levers, steps and image size. The march runs from the camera to
// the first surface (distance from the depth prepass) or the maximum distance, with a per-pixel jitter; a depth-aware upsample then adds the
// result to the lit scene. Single scattering with a Henyey-Greenstein phase function follows the usual real-time formulation (see the
// light shaft and participating media chapters in published course notes on real-time rendering).
public sealed partial class Renderer3D
{
    /// <summary>Most lights that make light shafts in one frame; extra opted-in lights are ignored.</summary>
    public const int MaxVolumetricLights = 4;

    private readonly Dictionary<(GpuTexture, GpuTexture, GpuTexture), GpuBindGroup> _shaftGroups = [];
    private readonly ShaftMarchPass _shaftMarchPass = new();
    private readonly ShaftCompositePass _shaftCompositePass = new();
    private GpuPipelineLayout _shaftLayout = null!;
    private GpuGraphicsPipeline _shaftMarchPipeline = null!;
    private GpuGraphicsPipeline _shaftCompositePipeline = null!;
    private GpuBuffer _shaftBuffer = null!;
    private ShaftConstants _shaftConstants;
    private int _shaftLights;
    private bool _shaftActive;
    private int _shaftSteps;
    private float _shaftScale;

    private void InitShafts()
    {
        var device = Device;
        _shaftLayout = Own(device.CreatePipelineLayout(ShaderLayouts.Bindings([Shafts.PSMarch.Layout, Shafts.PSComposite.Layout])));
        var march = Own(device.CreateShader(new ShaderSource(ShaderStage.Pixel, Shafts.PSMarch.EntryPoint) { SpirV = Shafts.PSMarch.SpirV, Wgsl = Shafts.PSMarch.Wgsl, WgslEntryPoint = Shafts.PSMarch.WgslEntryPoint, Msl = Shafts.PSMarch.Msl, MslFunctionName = Shafts.PSMarch.MslFunctionName, Layout = Shafts.PSMarch.Layout }));
        var composite = Own(device.CreateShader(new ShaderSource(ShaderStage.Pixel, Shafts.PSComposite.EntryPoint) { SpirV = Shafts.PSComposite.SpirV, Wgsl = Shafts.PSComposite.Wgsl, WgslEntryPoint = Shafts.PSComposite.WgslEntryPoint, Msl = Shafts.PSComposite.Msl, MslFunctionName = Shafts.PSComposite.MslFunctionName, Layout = Shafts.PSComposite.Layout }));
        GraphicsPipelineDesc Desc(GpuShader pixel, BlendState? blend, string label) => new()
        {
            Layout = _shaftLayout,
            Vertex = _postVs,
            Pixel = pixel,
            Topology = PrimitiveTopology.TriangleList,
            Cull = CullMode.None,
            ColorTargets = [new ColorTargetState(HdrFormat) { Blend = blend }],
            Label = label,
        };

        var add = new BlendComponent(BlendFactor.One, BlendFactor.One, BlendOperation.Add);
        _shaftMarchPipeline = Own(device.CreateGraphicsPipeline(Desc(march, null, "light shafts march")));
        _shaftCompositePipeline = Own(device.CreateGraphicsPipeline(Desc(composite, new BlendState(add, add), "light shafts composite")));
        _shaftBuffer = Own(device.CreateBuffer(new BufferDesc((ulong)Marshal.SizeOf<ShaftConstants>(), BufferUsage.Uniform | BufferUsage.CopyDestination) { Label = "3d light shafts" }));
    }

    private void DropShaftGroups()
    {
        foreach (var g in _shaftGroups.Values)
        {
            _deferred.Add(g);
        }

        _shaftGroups.Clear();
    }

    private void DisposeShafts()
    {
        foreach (var g in _shaftGroups.Values)
        {
            g.Dispose();
        }

        _shaftGroups.Clear();
    }

    // Called at the start of the light loop: forgets the previous frame's lights and decides whether the scene and level allow shafts at all.
    private bool BeginShafts(Scene3D scene, in QualitySettings q)
    {
        _shaftLights = 0;
        _shaftActive = false;
        var v = scene.Volumetric;
        return v.Enabled && q.VolumetricSteps > 0 && q.VolumetricScale > 0f && v.Density > 0f && v.Intensity > 0f;
    }

    // Records one opted-in light as the shader reads it (the same four vectors as a clustered light, with the shadow record in Spot.w).
    private void AddShaftLight(in LightRecord rec, Light3D light)
    {
        if (_shaftLights == MaxVolumetricLights || light.VolumetricIntensity <= 0f)
        {
            return;
        }

        var at = _shaftLights++ * 4;
        _shaftConstants.Lights[at] = rec.PosRange;
        _shaftConstants.Lights[at + 1] = rec.DirType;
        _shaftConstants.Lights[at + 2] = rec.Color * light.VolumetricIntensity;
        _shaftConstants.Lights[at + 3] = rec.Spot;
    }

    // After the lights are known: fills the constants and uploads them. Shafts run when at least one light opted in.
    private void FinishShafts(Scene3D scene, in QualitySettings q, int width, int height, int lowWidth, int lowHeight)
    {
        _shaftActive = _shaftLights > 0;
        if (!_shaftActive)
        {
            return;
        }

        var v = scene.Volumetric;
        var far = float.IsFinite(scene.Camera.Far) ? scene.Camera.Far : 1000f;
        _shaftSteps = q.VolumetricSteps;
        _shaftScale = q.VolumetricScale;
        _shaftConstants.A = new Vector4(q.VolumetricSteps, MathF.Min(v.MaxDistance, far), v.Density, v.Anisotropy);
        _shaftConstants.B = new Vector4(v.Intensity, v.LinkToFog && scene.Fog.Enabled ? 1f : 0f, lowWidth, lowHeight);
        _shaftConstants.C = new Vector4(width, height, _shaftLights, 0f);
        Device.WriteBuffer(_shaftBuffer, 0, MemoryMarshal.AsBytes(new ReadOnlySpan<ShaftConstants>(in _shaftConstants)));
    }

    // The size of the image the shafts are marched at (a share of the frame, at least one pixel).
    private static int ShaftSize(int full, float scale) => Math.Max(1, (int)MathF.Ceiling(full * scale));

    // Adds the march pass: reads the shadow atlas and the prepass image, writes the in-scattered light. Returns it (invalid when shafts are off).
    private GraphTexture AddShaftMarch(RenderGraph graph, GraphTexture gbuffer, GraphTexture atlas, int width, int height)
    {
        if (!_shaftActive)
        {
            return default;
        }

        var scatter = graph.CreateTexture("shafts_scatter", new TextureDesc((uint)ShaftSize(width, _shaftScale), (uint)ShaftSize(height, _shaftScale), HdrFormat, TextureUsage.None));
        _shaftMarchPass.Owner = this;
        _shaftMarchPass.Gbuffer = gbuffer;
        _shaftMarchPass.Atlas = atlas;
        _shaftMarchPass.Target = scatter;
        graph.AddPass(_shaftMarchPass);
        return scatter;
    }

    private void AddShaftComposite(RenderGraph graph, GraphTexture scatter, GraphTexture gbuffer, GraphTexture hdr)
    {
        if (!scatter.IsValid)
        {
            return;
        }

        _shaftCompositePass.Owner = this;
        _shaftCompositePass.Scatter = scatter;
        _shaftCompositePass.Gbuffer = gbuffer;
        _shaftCompositePass.Target = hdr;
        graph.AddPass(_shaftCompositePass);
    }

    private GpuBindGroup ShaftGroup(GpuTexture gbuffer, GpuTexture atlas, GpuTexture scatter)
    {
        var key = (gbuffer, atlas, scatter);
        if (_shaftGroups.TryGetValue(key, out var group))
        {
            return group;
        }

        if (_shaftGroups.Count >= 32)
        {
            DropShaftGroups();
        }

        group = Device.CreateBindGroup(_shaftLayout, 0,
        [
            BindGroupEntry.ForBuffer(0, _frameBuffer, 0, (ulong)Marshal.SizeOf<FrameConstants>()),
            BindGroupEntry.ForBuffer(1, _shaftBuffer),
            BindGroupEntry.ForTexture(2, gbuffer),
            BindGroupEntry.ForTexture(3, atlas),
            BindGroupEntry.ForBuffer(4, _shadowBuffer),
            BindGroupEntry.ForSampler(5, _shadowSampler),
            BindGroupEntry.ForTexture(6, scatter),
        ]);
        _shaftGroups[key] = group;
        return group;
    }

    private sealed class ShaftMarchPass : IGraphPass
    {
        public Renderer3D? Owner;
        public GraphTexture Gbuffer;
        public GraphTexture Atlas;
        public GraphTexture Target;

        public string Name => "light_shafts";

        public void Setup(PassBuilder builder)
        {
            builder.Read(Gbuffer);
            if (Atlas.IsValid)
            {
                builder.Read(Atlas);
            }

            builder.ColorTarget(Target, LoadOp.DontCare);
        }

        public void Execute(PassContext context)
        {
            var r = Owner!;
            var pass = context.Render;
            pass.SetPipeline(r._shaftMarchPipeline);
            pass.SetBindGroup(0, r.ShaftGroup(context.Texture(Gbuffer), Atlas.IsValid ? context.Texture(Atlas) : r._shadowDummy, r._white));
            pass.Draw(3);
        }
    }

    // Adds the marched light onto the lit scene (before bloom and tone mapping read it).
    private sealed class ShaftCompositePass : IGraphPass
    {
        public Renderer3D? Owner;
        public GraphTexture Scatter;
        public GraphTexture Gbuffer;
        public GraphTexture Target;

        public string Name => "light_shafts_composite";

        public void Setup(PassBuilder builder)
        {
            builder.Read(Scatter);
            builder.Read(Gbuffer);
            builder.ColorTarget(Target, LoadOp.Load);
        }

        public void Execute(PassContext context)
        {
            var r = Owner!;
            var pass = context.Render;
            pass.SetPipeline(r._shaftCompositePipeline);
            pass.SetBindGroup(0, r.ShaftGroup(context.Texture(Gbuffer), r._shadowDummy, context.Texture(Scatter)));
            pass.Draw(3);
        }
    }
}
