using System.Runtime.InteropServices;
using OneGame.Gltf;
using OneGame.Gpu;
using OneGame.Gpu.Graph;
using OneGame.Render3D.Shaders;

namespace OneGame.Render3D;

/// <summary>What one frame of 3D drew.</summary>
/// <param name="Objects">Mesh nodes drawn.</param>
/// <param name="DrawCalls">Indexed draws recorded (sky and post excluded).</param>
/// <param name="Triangles">Triangles submitted.</param>
/// <param name="Lights">Punctual lights used.</param>
/// <param name="Clusters">Light clusters in the grid.</param>
public readonly record struct FrameStats3D(int Objects, int DrawCalls, long Triangles, int Lights, int Clusters)
{
    /// <summary>Shadow map tiles rendered (cascades, spot maps, point faces).</summary>
    public int ShadowTiles { get; init; }

    /// <summary>Lights that cast shadows.</summary>
    public int ShadowLights { get; init; }

    /// <summary>Copies in all instance batches.</summary>
    public int Instances { get; init; }

    /// <summary>Copies of batches that survived culling against the camera.</summary>
    public int InstancesDrawn { get; init; }

    /// <summary>The quality level the frame was drawn at.</summary>
    public QualityLevel Quality { get; init; }

    /// <summary>Whether ambient occlusion ran.</summary>
    public bool Ssao { get; init; }
}

/// <summary>
/// The physically based 3D renderer. It uploads glTF models (<see cref="Upload"/>) and adds a frame to a render graph
/// (<see cref="Render"/>): cluster light assignment, one forward pass for opaque, masked and blended surfaces plus the backdrop, a bloom
/// chain, and tone mapping into the target. See <c>docs/interfaces/render3d.md</c>.
/// </summary>
public sealed partial class Renderer3D : IDisposable
{
    /// <summary>Most punctual lights one frame uses; extra lights are ignored.</summary>
    public const int MaxLights = 512;

    /// <summary>Most lights one cluster can hold; the rest are dropped.</summary>
    public const int MaxLightsPerCluster = 63;

    /// <summary>Screen tile size of the light grid, in pixels.</summary>
    public const int TileSize = 64;

    /// <summary>Depth slices of the light grid.</summary>
    public const int DepthSlices = 24;

    /// <summary>Format of the HDR image the scene is lit into.</summary>
    public const GpuFormat HdrFormat = GpuFormat.Rgba16Float;

    /// <summary>Format of the depth buffer.</summary>
    public const GpuFormat DepthFormat = GpuFormat.Depth32Float;

    private const int ClusterStride = MaxLightsPerCluster + 1;

    private readonly List<IDisposable> _owned = [];
    private readonly GpuPipelineLayout _layout;
    private readonly GpuShader _pbrVs;
    private readonly GpuShader _pbrPs;
    private readonly GpuShader _pbrVsInst;
    private readonly GpuShader _outlineVs;
    private readonly GpuShader _outlineVsInst;
    private readonly GpuShader _shadowMaskPs;
    private readonly GpuShader _prepassPs;
    private readonly GpuShader _outlinePs;
    private readonly VertexBufferLayout _instanceLayout;
    private readonly GpuShader _skyVs;
    private readonly GpuShader _skyPs;
    private readonly GpuSampler _envSampler;
    private readonly GpuSampler _defaultSampler;
    private readonly GpuTexture _white;
    private readonly Dictionary<int, GpuGraphicsPipeline> _pipelines = [];
    private readonly Dictionary<GltfSampler, GpuSampler> _samplers = [];
    private readonly VertexBufferLayout _vertexLayout;
    private readonly List<IDisposable> _deferred = [];

    /// <summary>Creates a renderer on a device.</summary>
    /// <param name="device">The graphics device. It must outlive the renderer.</param>
    public Renderer3D(GpuDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);
        Device = device;

        _layout = Own(device.CreatePipelineLayout(ShaderLayouts.Bindings([Pbr.VSMain.Layout, Pbr.PSMain.Layout, Pbr.VSInst.Layout, Pbr.VSOutline.Layout, Pbr.VSOutlineInst.Layout, Pbr.PSShadowMask.Layout, Pbr.PSPrepass.Layout, Pbr.PSOutline.Layout, Pbr.VSSky.Layout, Pbr.PSSky.Layout], [(1, 0)])));
        _pbrVs = Own(device.CreateShader(new ShaderSource(ShaderStage.Vertex, Pbr.VSMain.EntryPoint) { SpirV = Pbr.VSMain.SpirV, Wgsl = Pbr.VSMain.Wgsl, WgslEntryPoint = Pbr.VSMain.WgslEntryPoint, Msl = Pbr.VSMain.Msl, MslFunctionName = Pbr.VSMain.MslFunctionName, Layout = Pbr.VSMain.Layout }));
        _pbrPs = Own(device.CreateShader(new ShaderSource(ShaderStage.Pixel, Pbr.PSMain.EntryPoint) { SpirV = Pbr.PSMain.SpirV, Wgsl = Pbr.PSMain.Wgsl, WgslEntryPoint = Pbr.PSMain.WgslEntryPoint, Msl = Pbr.PSMain.Msl, MslFunctionName = Pbr.PSMain.MslFunctionName, Layout = Pbr.PSMain.Layout }));
        _pbrVsInst = Own(device.CreateShader(new ShaderSource(ShaderStage.Vertex, Pbr.VSInst.EntryPoint) { SpirV = Pbr.VSInst.SpirV, Wgsl = Pbr.VSInst.Wgsl, WgslEntryPoint = Pbr.VSInst.WgslEntryPoint, Msl = Pbr.VSInst.Msl, MslFunctionName = Pbr.VSInst.MslFunctionName, Layout = Pbr.VSInst.Layout }));
        _outlineVs = Own(device.CreateShader(new ShaderSource(ShaderStage.Vertex, Pbr.VSOutline.EntryPoint) { SpirV = Pbr.VSOutline.SpirV, Wgsl = Pbr.VSOutline.Wgsl, WgslEntryPoint = Pbr.VSOutline.WgslEntryPoint, Msl = Pbr.VSOutline.Msl, MslFunctionName = Pbr.VSOutline.MslFunctionName, Layout = Pbr.VSOutline.Layout }));
        _outlineVsInst = Own(device.CreateShader(new ShaderSource(ShaderStage.Vertex, Pbr.VSOutlineInst.EntryPoint) { SpirV = Pbr.VSOutlineInst.SpirV, Wgsl = Pbr.VSOutlineInst.Wgsl, WgslEntryPoint = Pbr.VSOutlineInst.WgslEntryPoint, Msl = Pbr.VSOutlineInst.Msl, MslFunctionName = Pbr.VSOutlineInst.MslFunctionName, Layout = Pbr.VSOutlineInst.Layout }));
        _shadowMaskPs = Own(device.CreateShader(new ShaderSource(ShaderStage.Pixel, Pbr.PSShadowMask.EntryPoint) { SpirV = Pbr.PSShadowMask.SpirV, Wgsl = Pbr.PSShadowMask.Wgsl, WgslEntryPoint = Pbr.PSShadowMask.WgslEntryPoint, Msl = Pbr.PSShadowMask.Msl, MslFunctionName = Pbr.PSShadowMask.MslFunctionName, Layout = Pbr.PSShadowMask.Layout }));
        _prepassPs = Own(device.CreateShader(new ShaderSource(ShaderStage.Pixel, Pbr.PSPrepass.EntryPoint) { SpirV = Pbr.PSPrepass.SpirV, Wgsl = Pbr.PSPrepass.Wgsl, WgslEntryPoint = Pbr.PSPrepass.WgslEntryPoint, Msl = Pbr.PSPrepass.Msl, MslFunctionName = Pbr.PSPrepass.MslFunctionName, Layout = Pbr.PSPrepass.Layout }));
        _outlinePs = Own(device.CreateShader(new ShaderSource(ShaderStage.Pixel, Pbr.PSOutline.EntryPoint) { SpirV = Pbr.PSOutline.SpirV, Wgsl = Pbr.PSOutline.Wgsl, WgslEntryPoint = Pbr.PSOutline.WgslEntryPoint, Msl = Pbr.PSOutline.Msl, MslFunctionName = Pbr.PSOutline.MslFunctionName, Layout = Pbr.PSOutline.Layout }));
        _skyVs = Own(device.CreateShader(new ShaderSource(ShaderStage.Vertex, Pbr.VSSky.EntryPoint) { SpirV = Pbr.VSSky.SpirV, Wgsl = Pbr.VSSky.Wgsl, WgslEntryPoint = Pbr.VSSky.WgslEntryPoint, Msl = Pbr.VSSky.Msl, MslFunctionName = Pbr.VSSky.MslFunctionName, Layout = Pbr.VSSky.Layout }));
        _skyPs = Own(device.CreateShader(new ShaderSource(ShaderStage.Pixel, Pbr.PSSky.EntryPoint) { SpirV = Pbr.PSSky.SpirV, Wgsl = Pbr.PSSky.Wgsl, WgslEntryPoint = Pbr.PSSky.WgslEntryPoint, Msl = Pbr.PSSky.Msl, MslFunctionName = Pbr.PSSky.MslFunctionName, Layout = Pbr.PSSky.Layout }));

        // The packed glTF vertex (GltfPrimitive.PackVertices) is what the shader declares; joint indices are two 32-bit words, each holding two 16-bit indices.
        _vertexLayout = ShaderLayouts.Vertex(Pbr.VSMain.Layout);

        _instanceLayout = ShaderLayouts.VertexStreams(Pbr.VSInst.Layout, new OneShader.Layouts.VertexStream(0, GltfPrimitive.PackedVertexSize), new OneShader.Layouts.VertexStream(8, 64, OneShader.Layouts.VertexStepMode.Instance))[1];

        _envSampler = Own(device.CreateSampler(SamplerDesc.Linear with { MaxLod = 32f }));
        _defaultSampler = Own(device.CreateSampler(new SamplerDesc
        {
            MagFilter = FilterMode.Linear,
            MinFilter = FilterMode.Linear,
            MipFilter = FilterMode.Linear,
            AddressU = AddressMode.Repeat,
            AddressV = AddressMode.Repeat,
            MaxAnisotropy = 8,
        }));
        _white = Own(device.CreateTexture(new TextureDesc(1, 1, GpuFormat.Rgba8Unorm, TextureUsage.Sampled | TextureUsage.CopyDestination) { Label = "white" }));
        device.WriteTexture(_white, 0, 0, new byte[] { 255, 255, 255, 255 });

        InitFrame();
        InitPost();
        InitSsao();
        InitIbl();
    }

    /// <summary>The device this renderer draws with.</summary>
    public GpuDevice Device { get; }

    /// <summary>What the last <see cref="Render"/> call prepared.</summary>
    public FrameStats3D LastStats { get; private set; }

    private T Own<T>(T resource)
        where T : IDisposable
    {
        _owned.Add(resource);
        return resource;
    }

    /// <summary>Uploads a glTF model: vertex and index buffers, materials, and textures with generated mip levels (colour textures as sRGB).</summary>
    /// <param name="model">The imported model.</param>
    /// <param name="scene">Scene to draw; -1 for the model's default scene.</param>
    /// <returns>The GPU model. Dispose it before the renderer.</returns>
    public Model3D Upload(GltfModel model, int scene = -1)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (scene < 0)
        {
            scene = model.DefaultScene;
        }

        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(scene, model.Scenes.Length);

        var result = new Model3D(model, scene);
        try
        {
            UploadInto(result);
        }
        catch
        {
            result.Dispose();
            throw;
        }

        return result;
    }

    private void UploadInto(Model3D result)
    {
        var model = result.Source;
        var device = Device;
        var encoder = device.BeginCommands();
        var textures = new Dictionary<(int Image, bool Srgb), GpuTexture>();

        GpuTexture TextureFor(TextureRef? reference, bool srgb)
        {
            if (reference is null || (uint)reference.Texture >= (uint)model.Textures.Length)
            {
                return _white;
            }

            var source = model.Textures[reference.Texture].Source;
            if (source < 0 || (uint)source >= (uint)model.Images.Length || model.Images[source].Pixels is not { } pixels)
            {
                return _white;
            }

            if (textures.TryGetValue((source, srgb), out var existing))
            {
                return existing;
            }

            var image = model.Images[source];
            var mips = GpuTexture.FullMipCount((uint)image.Width, (uint)image.Height);
            var texture = result.Own(device.CreateTexture(new TextureDesc((uint)image.Width, (uint)image.Height, srgb ? GpuFormat.Rgba8UnormSrgb : GpuFormat.Rgba8Unorm, TextureUsage.Sampled | TextureUsage.CopyDestination | TextureUsage.CopySource)
            {
                MipLevels = mips,
                Label = string.IsNullOrEmpty(image.Name) ? $"image {source}" : image.Name,
            }));
            device.WriteTexture(texture, 0, 0, pixels);
            encoder.GenerateMips(texture);
            textures[(source, srgb)] = texture;
            return texture;
        }

        GpuSampler SamplerFor(TextureRef? reference)
        {
            if (reference is null || (uint)reference.Texture >= (uint)model.Textures.Length)
            {
                return _defaultSampler;
            }

            var index = model.Textures[reference.Texture].Sampler;
            if (index < 0 || (uint)index >= (uint)model.Samplers.Length)
            {
                return _defaultSampler;
            }

            var sampler = model.Samplers[index];
            if (!_samplers.TryGetValue(sampler, out var made))
            {
                made = Own(device.CreateSampler(MapSampler(sampler)));
                _samplers[sampler] = made;
            }

            return made;
        }

        var materials = new Material3D[model.Materials.Length + 1];
        for (var i = 0; i < materials.Length; i++)
        {
            var m = i < model.Materials.Length ? model.Materials[i] : new GltfMaterial();
            var constants = BuildMaterial(m);
            var buffer = result.Own(device.CreateBuffer(new BufferDesc((ulong)Marshal.SizeOf<MaterialConstants>(), BufferUsage.Uniform | BufferUsage.CopyDestination) { Label = "material " + m.Name }));
            device.WriteBuffer(buffer, 0, MemoryMarshal.AsBytes(new ReadOnlySpan<MaterialConstants>(in constants)));
            var group = result.Own(device.CreateBindGroup(_layout, 2,
            [
                BindGroupEntry.ForBuffer(0, buffer),
                BindGroupEntry.ForTexture(1, TextureFor(m.BaseColorTexture, srgb: true)),
                BindGroupEntry.ForTexture(2, TextureFor(m.MetallicRoughnessTexture, srgb: false)),
                BindGroupEntry.ForTexture(3, TextureFor(m.NormalTexture, srgb: false)),
                BindGroupEntry.ForTexture(4, TextureFor(m.OcclusionTexture, srgb: false)),
                BindGroupEntry.ForTexture(5, TextureFor(m.EmissiveTexture, srgb: true)),
                BindGroupEntry.ForSampler(6, SamplerFor(m.BaseColorTexture)),
                BindGroupEntry.ForSampler(7, SamplerFor(m.MetallicRoughnessTexture)),
                BindGroupEntry.ForSampler(8, SamplerFor(m.NormalTexture)),
                BindGroupEntry.ForSampler(9, SamplerFor(m.OcclusionTexture)),
                BindGroupEntry.ForSampler(10, SamplerFor(m.EmissiveTexture)),
            ]));
            materials[i] = new Material3D(device, m.Name ?? "", constants, buffer, group, m.AlphaMode == GltfAlphaMode.Blend, m.DoubleSided, m.AlphaMode == GltfAlphaMode.Mask);
        }

        var meshes = new Primitive3D[]?[model.Meshes.Length];
        for (var mi = 0; mi < model.Meshes.Length; mi++)
        {
            var prims = new List<Primitive3D>();
            foreach (var p in model.Meshes[mi].Primitives)
            {
                if (p.Topology != GltfTopology.Triangles || p.Indices.Length == 0)
                {
                    continue;   // Lines and points are not drawn by this renderer.
                }

                var vertexBytes = p.PackVertices();
                var indexBytes = p.PackIndices(out var width);
                var vb = result.Own(device.CreateBuffer(new BufferDesc((ulong)vertexBytes.Length, BufferUsage.Vertex | BufferUsage.CopyDestination) { Label = "vertices " + model.Meshes[mi].Name }));
                var ib = result.Own(device.CreateBuffer(new BufferDesc((ulong)indexBytes.Length, BufferUsage.Index | BufferUsage.CopyDestination) { Label = "indices " + model.Meshes[mi].Name }));
                device.WriteBuffer(vb, 0, vertexBytes);
                device.WriteBuffer(ib, 0, indexBytes);
                var material = materials[p.Material >= 0 && p.Material < model.Materials.Length ? p.Material : model.Materials.Length];
                prims.Add(new Primitive3D(p, vb, ib, width == IndexWidth.U16 ? IndexFormat.Index16 : IndexFormat.Index32, material)
                {
                    BaseVertices = p.MorphTargets.Length > 0 ? vertexBytes : null,
                });
            }

            meshes[mi] = [.. prims];
        }

        result.Meshes = meshes;
        result.Materials = materials;
        result.DrawNodes = SceneMeshNodes(model, result.Scene);
        device.Submit(encoder);
    }

    private static int[] SceneMeshNodes(GltfModel model, int scene)
    {
        // Walk down from the scene roots with an explicit stack; the node graph is acyclic and depth-limited by the importer.
        var result = new List<int>();
        var stack = new Stack<int>();
        if (scene >= 0)
        {
            var roots = model.Scenes[scene].Roots;
            for (var i = roots.Length - 1; i >= 0; i--)
            {
                stack.Push(roots[i]);
            }
        }
        else
        {
            for (var i = model.Nodes.Length - 1; i >= 0; i--)
            {
                if (model.Nodes[i].Parent < 0)
                {
                    stack.Push(i);
                }
            }
        }

        while (stack.Count > 0)
        {
            var n = stack.Pop();
            if (model.Nodes[n].Mesh >= 0)
            {
                result.Add(n);
            }

            var children = model.Nodes[n].Children;
            for (var i = children.Length - 1; i >= 0; i--)
            {
                stack.Push(children[i]);
            }
        }

        return [.. result];
    }

    private static SamplerDesc MapSampler(GltfSampler s)
    {
        static AddressMode Wrap(GltfWrap w) => w switch
        {
            GltfWrap.ClampToEdge => AddressMode.ClampToEdge,
            GltfWrap.MirroredRepeat => AddressMode.MirrorRepeat,
            _ => AddressMode.Repeat,
        };

        var minNearest = s.MinFilter is GltfMinFilter.Nearest or GltfMinFilter.NearestMipmapNearest or GltfMinFilter.NearestMipmapLinear;
        var hasMips = s.MinFilter is GltfMinFilter.Unspecified or GltfMinFilter.NearestMipmapNearest or GltfMinFilter.NearestMipmapLinear or GltfMinFilter.LinearMipmapNearest or GltfMinFilter.LinearMipmapLinear;
        var mipNearest = s.MinFilter is GltfMinFilter.NearestMipmapNearest or GltfMinFilter.LinearMipmapNearest;
        return new SamplerDesc
        {
            MagFilter = s.MagFilter == GltfMagFilter.Nearest ? FilterMode.Nearest : FilterMode.Linear,
            MinFilter = minNearest ? FilterMode.Nearest : FilterMode.Linear,
            MipFilter = mipNearest ? FilterMode.Nearest : FilterMode.Linear,
            AddressU = Wrap(s.WrapS),
            AddressV = Wrap(s.WrapT),
            MaxLod = hasMips ? 1000f : 0f,
            MaxAnisotropy = hasMips && !minNearest && s.MagFilter != GltfMagFilter.Nearest ? 8f : 1f,
        };
    }

    private static MaterialConstants BuildMaterial(GltfMaterial m)
    {
        var f0 = (m.Ior - 1f) / (m.Ior + 1f);
        var c = new MaterialConstants
        {
            BaseColor = m.BaseColorFactor,
            Emissive = new Vector4(m.EmissiveFactor * m.EmissiveStrength, 0),
            P0 = new Vector4(m.MetallicFactor, m.RoughnessFactor, m.NormalScale, m.OcclusionStrength),
            P1 = new Vector4(m.AlphaCutoff, (float)(int)m.AlphaMode, m.Unlit ? 1f : 0f, f0 * f0),
            Flags = new UInt4 { X = m.NormalTexture is null ? 0u : 1u },
            StyleA = new Vector4(0f, 3f, 0.04f, 0.5f),
            OutlineColor = new Vector4(0.02f, 0.02f, 0.03f, 1f),
        };
        SetTransform(ref c, 0, m.BaseColorTexture);
        SetTransform(ref c, 1, m.MetallicRoughnessTexture);
        SetTransform(ref c, 2, m.NormalTexture);
        SetTransform(ref c, 3, m.OcclusionTexture);
        SetTransform(ref c, 4, m.EmissiveTexture);
        return c;
    }

    private static void SetTransform(ref MaterialConstants c, int slot, TextureRef? t)
    {
        var uv = t is null ? Matrix3x2.Identity : t.UvMatrix;

        // KHR_texture_transform in row-vector form: u' = u*m11 + v*m21 + m31, v' = u*m12 + v*m22 + m32.
        c.Xf[slot * 2] = new Vector4(uv.M11, uv.M21, uv.M31, t is { TexCoord: 1 } ? 1f : 0f);
        c.Xf[(slot * 2) + 1] = new Vector4(uv.M12, uv.M22, uv.M32, 0f);
    }

    /// <summary>Releases everything the renderer created. Models and environments are disposed separately, before this.</summary>
    public void Dispose()
    {
        Device.WaitIdle();
        DisposeDeferred();
        DisposeFrame();
        DisposePost();
        DisposeSsao();
        DisposeIbl();
        foreach (var p in _pipelines.Values)
        {
            p.Dispose();
        }

        _pipelines.Clear();
        for (var i = _owned.Count - 1; i >= 0; i--)
        {
            _owned[i].Dispose();
        }

        _owned.Clear();
    }

    private void DisposeDeferred()
    {
        foreach (var d in _deferred)
        {
            d.Dispose();
        }

        _deferred.Clear();
    }

    private enum PipeKind
    {
        Main,
        Shadow,
        Prepass,
        Outline,
    }

    // One pipeline per combination of what is drawn (a pass kind, material flags, winding, instancing, sample count), made on first use.
    private GpuGraphicsPipeline ScenePipeline(PipeKind kind, bool blend, bool doubleSided, bool flipped, bool instanced, bool mask, int samples)
    {
        var key = (int)kind | (blend ? 4 : 0) | (doubleSided ? 8 : 0) | (flipped ? 16 : 0) | (instanced ? 32 : 0) | (mask ? 64 : 0) | (samples << 7);
        if (_pipelines.TryGetValue(key, out var pipeline))
        {
            return pipeline;
        }

        var vertexBuffers = instanced ? new[] { _vertexLayout, _instanceLayout } : new[] { _vertexLayout };
        var front = flipped ? FrontFace.Clockwise : FrontFace.CounterClockwise;
        var cull = doubleSided ? CullMode.None : CullMode.Back;
        var desc = kind switch
        {
            PipeKind.Shadow => new GraphicsPipelineDesc
            {
                Layout = _layout,
                Vertex = instanced ? _pbrVsInst : _pbrVs,
                Pixel = mask ? _shadowMaskPs : null,
                VertexBuffers = vertexBuffers,
                Topology = PrimitiveTopology.TriangleList,
                Cull = cull,
                Front = front,
                DepthStencil = new DepthStencilState(DepthFormat) { DepthWrite = true, DepthCompare = CompareFunction.Less, DepthBias = 2, DepthBiasSlopeScale = 2f },
                Label = "shadow",
            },
            PipeKind.Prepass => new GraphicsPipelineDesc
            {
                Layout = _layout,
                Vertex = instanced ? _pbrVsInst : _pbrVs,
                Pixel = _prepassPs,
                VertexBuffers = vertexBuffers,
                Topology = PrimitiveTopology.TriangleList,
                Cull = cull,
                Front = front,
                ColorTargets = [new ColorTargetState(GBufferFormat)],
                DepthStencil = new DepthStencilState(DepthFormat) { DepthWrite = true, DepthCompare = CompareFunction.Less },
                Label = "prepass",
            },
            PipeKind.Outline => new GraphicsPipelineDesc
            {
                Layout = _layout,
                Vertex = instanced ? _outlineVsInst : _outlineVs,
                Pixel = _outlinePs,
                VertexBuffers = vertexBuffers,
                Topology = PrimitiveTopology.TriangleList,
                Cull = CullMode.Front,
                Front = front,
                ColorTargets = [new ColorTargetState(HdrFormat)],
                DepthStencil = new DepthStencilState(DepthFormat) { DepthWrite = true, DepthCompare = CompareFunction.Less },
                SampleCount = (uint)samples,
                Label = "outline",
            },
            _ => new GraphicsPipelineDesc
            {
                Layout = _layout,
                Vertex = instanced ? _pbrVsInst : _pbrVs,
                Pixel = _pbrPs,
                VertexBuffers = vertexBuffers,
                Topology = PrimitiveTopology.TriangleList,
                Cull = cull,
                Front = front,
                ColorTargets = [new ColorTargetState(HdrFormat) { Blend = blend ? BlendState.AlphaBlend : null }],
                DepthStencil = new DepthStencilState(DepthFormat) { DepthWrite = !blend, DepthCompare = CompareFunction.Less },
                SampleCount = (uint)samples,
                Label = "pbr",
            },
        };
        pipeline = Device.CreateGraphicsPipeline(desc);
        _pipelines[key] = pipeline;
        return pipeline;
    }

    private GpuGraphicsPipeline SkyPipeline(int samples)
    {
        var key = -1 - samples;
        if (!_pipelines.TryGetValue(key, out var pipeline))
        {
            pipeline = Device.CreateGraphicsPipeline(new GraphicsPipelineDesc
            {
                Layout = _layout,
                Vertex = _skyVs,
                Pixel = _skyPs,
                Topology = PrimitiveTopology.TriangleList,
                Cull = CullMode.None,
                ColorTargets = [new ColorTargetState(HdrFormat)],
                DepthStencil = new DepthStencilState(DepthFormat) { DepthWrite = false, DepthCompare = CompareFunction.LessEqual },
                SampleCount = (uint)samples,
                Label = "sky",
            });
            _pipelines[key] = pipeline;
        }

        return pipeline;
    }
}
