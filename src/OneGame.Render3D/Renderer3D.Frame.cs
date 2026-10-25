using System.Runtime.InteropServices;
using OneGame.Gltf;
using OneGame.Gpu;
using OneGame.Gpu.Graph;
using OneGame.Render3D.Shaders;

namespace OneGame.Render3D;

public sealed partial class Renderer3D
{
    private struct DrawItem
    {
        public Primitive3D Primitive;
        public GpuBuffer Vertices;
        public GpuGraphicsPipeline Pipeline;
        public int Slot;
    }

    private readonly LightRecord[] _lightRecords = new LightRecord[MaxLights];
    private readonly ForwardPass _forward = new();
    private readonly ClusterPass _clusterPass = new();
    private GpuBuffer _frameBuffer = null!;
    private GpuBuffer _lightBuffer = null!;
    private GpuBuffer _clusterBuffer = null!;
    private GpuBuffer _jointBuffer = null!;
    private GpuBuffer _objectBuffer = null!;
    private GpuBindGroup? _set0;
    private GpuBindGroup? _set1;
    private GpuBindGroup? _clusterSet0;
    private GpuPipelineLayout _clusterLayout = null!;
    private GpuComputePipeline _clusterPipeline = null!;
    private GpuShader _clusterCs = null!;
    private EnvironmentMap? _boundEnvironment;
    private GpuBuffer? _boundCluster;
    private GpuBuffer? _boundJoints;
    private int _clusterCapacity;
    private int _jointCapacity;
    private int _objectCapacity;
    private ObjectConstants[] _objects = new ObjectConstants[64];
    private Vector4[] _jointRows = new Vector4[256];
    private Matrix4x4[] _skinScratch = new Matrix4x4[64];
    private DrawItem[] _opaque = new DrawItem[64];
    private DrawItem[] _blend = new DrawItem[16];
    private float[] _opaqueKeys = new float[64];
    private float[] _blendKeys = new float[16];
    private int _opaqueCount;
    private int _blendCount;
    private FrameConstants _frame;
    private int _gridX;
    private int _gridY;
    private int _lightCount;
    private int _objectCount;

    private void InitFrame()
    {
        var device = Device;
        _frameBuffer = Own(device.CreateBuffer(new BufferDesc((ulong)Marshal.SizeOf<FrameConstants>(), BufferUsage.Uniform | BufferUsage.CopyDestination) { Label = "3d frame" }));
        _lightBuffer = Own(device.CreateBuffer(new BufferDesc((ulong)(MaxLights * Marshal.SizeOf<LightRecord>()), BufferUsage.Storage | BufferUsage.CopyDestination) { Label = "3d lights" }));
        _clusterCapacity = 1024;
        _clusterBuffer = Own(device.CreateBuffer(new BufferDesc((ulong)_clusterCapacity * ClusterStride * 4, BufferUsage.Storage | BufferUsage.CopyDestination) { Label = "3d clusters" }));
        _jointCapacity = 64;
        _jointBuffer = Own(device.CreateBuffer(new BufferDesc((ulong)_jointCapacity * 64, BufferUsage.Storage | BufferUsage.CopyDestination) { Label = "3d joints" }));
        _objectCapacity = 64;
        _objectBuffer = Own(device.CreateBuffer(new BufferDesc((ulong)_objectCapacity * GpuDevice.DynamicOffsetAlignment, BufferUsage.Uniform | BufferUsage.CopyDestination) { Label = "3d objects" }));
        _set1 = Own(device.CreateBindGroup(_layout, 1, [BindGroupEntry.ForBuffer(0, _objectBuffer, 0, GpuDevice.DynamicOffsetAlignment)]));

        ReadOnlySpan<ResourceBinding> bindings =
        [
            new(0, 0, 1, ResourceKind.ConstantBuffer, ShaderStage.Compute),
            new(0, 1, 1, ResourceKind.ReadOnlyStorageBuffer, ShaderStage.Compute),
            new(0, 2, 1, ResourceKind.StorageBuffer, ShaderStage.Compute),
        ];
        _clusterLayout = Own(device.CreatePipelineLayout(bindings));
        _clusterCs = Own(device.CreateShader(ShaderStage.Compute, Cluster.CSMain.SpirV, Cluster.CSMain.EntryPoint));
        _clusterPipeline = Own(device.CreateComputePipeline(new ComputePipelineDesc { Layout = _clusterLayout, Compute = _clusterCs, Label = "cluster lights" }));
    }

    private void DisposeFrame()
    {
        _set0?.Dispose();
        _set1?.Dispose();
        _clusterSet0?.Dispose();
        _set0 = null;
        _set1 = null;
        _clusterSet0 = null;
    }

    /// <summary>
    /// Adds this frame's passes to a render graph that has begun a frame: light clustering, the forward pass, bloom and tone mapping into
    /// <paramref name="target"/>. The target's earlier contents are replaced; a later pass that draws over it (the 2D renderer) must
    /// declare <c>ReadWrite</c>, not <c>Write</c>, or the graph drops this work as overwritten.
    /// </summary>
    /// <param name="graph">The graph, after <c>BeginFrame</c>.</param>
    /// <param name="scene">What to draw.</param>
    /// <param name="target">The image to draw into: an sRGB 8-bit, 8-bit UNorm or 16-bit float colour target of the given size.</param>
    /// <param name="width">Target width in pixels.</param>
    /// <param name="height">Target height in pixels.</param>
    /// <param name="targetFormat">Format of the target texture.</param>
    public void Render(RenderGraph graph, Scene3D scene, GraphTexture target, int width, int height, GpuFormat targetFormat)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        DisposeDeferred();

        var samples = 1;
        for (var s = 2; s <= Math.Min(scene.Samples, (int)Device.Capabilities.MaxSampleCount); s *= 2)
        {
            samples = s;
        }

        var env = scene.Environment.Map ?? DefaultEnvironment();
        EnvironmentWork(graph, env);
        PrepareFrame(scene, width, height, samples, env);

        var hdr = graph.CreateTexture("scene_hdr", new TextureDesc((uint)width, (uint)height, HdrFormat, TextureUsage.None));
        var depth = graph.CreateTexture("scene_depth", new TextureDesc((uint)width, (uint)height, DepthFormat, TextureUsage.None) { SampleCount = (uint)samples });
        var color = hdr;
        if (samples > 1)
        {
            color = graph.CreateTexture("scene_hdr_ms", new TextureDesc((uint)width, (uint)height, HdrFormat, TextureUsage.None) { SampleCount = (uint)samples });
        }

        var hasClusters = _lightCount > (int)_frame.Counts.X;
        GraphBuffer lights = default;
        GraphBuffer clusters = default;
        if (hasClusters)
        {
            lights = graph.ImportBuffer("lights", _lightBuffer);
            clusters = graph.ImportBuffer("light_clusters", _clusterBuffer);
            _clusterPass.Lights = lights;
            _clusterPass.Clusters = clusters;
            _clusterPass.Owner = this;
            graph.AddPass(_clusterPass);
        }

        var environment = scene.Environment;
        _forward.Owner = this;
        _forward.Color = color;
        _forward.Resolve = samples > 1 ? hdr : default;
        _forward.Depth = depth;
        _forward.Lights = lights;
        _forward.Clusters = clusters;
        _forward.Samples = samples;
        _forward.Sky = environment.Background == BackgroundMode.Environment;
        _forward.Clear = environment.Background == BackgroundMode.Environment ? Vector3.Zero : environment.BackgroundColor;
        graph.AddPass(_forward);

        AddPostPasses(graph, scene.Post, hdr, target, width, height, targetFormat);
        LastStats = new FrameStats3D(_objectCount, _opaqueCount + _blendCount, _drawnTriangles, _lightCount, _gridX * _gridY * DepthSlices);
    }

    private long _drawnTriangles;

    private void PrepareFrame(Scene3D scene, int width, int height, int samples, EnvironmentMap env)
    {
        _ = samples;
        var camera = scene.Camera;
        var aspect = width / (float)height;
        var view = camera.View;
        var proj = camera.Projection(aspect);
        var viewProj = view * proj;
        Matrix4x4.Invert(proj, out var invProj);
        Matrix4x4.Invert(viewProj, out var invViewProj);
        var near = MathF.Max(camera.Near, 1e-4f);
        var far = float.IsFinite(camera.Far) ? camera.Far : 1000f;
        if (far <= near * 1.0001f)
        {
            far = near * 2f;
        }

        _gridX = (width + TileSize - 1) / TileSize;
        _gridY = (height + TileSize - 1) / TileSize;
        var clusters = _gridX * _gridY * DepthSlices;
        EnsureClusterCapacity(clusters);

        // Lights: directional ones first (always applied), then the clustered punctual ones.
        var count = 0;
        var dirCount = 0;
        for (var pass = 0; pass < 2; pass++)
        {
            foreach (var l in scene.Lights)
            {
                if ((l.Kind == LightKind.Directional) != (pass == 0) || count == MaxLights)
                {
                    continue;
                }

                _lightRecords[count++] = MakeLight(l);
                if (pass == 0)
                {
                    dirCount++;
                }
            }
        }

        _lightCount = count;
        if (count > 0)
        {
            Device.WriteBuffer(_lightBuffer, 0, MemoryMarshal.AsBytes(new ReadOnlySpan<LightRecord>(_lightRecords, 0, count)));
        }

        var env0 = scene.Environment;
        var sliceScale = DepthSlices / MathF.Log2(far / near);
        var maxMip = env.SpecularMipCount - 1;
        _frame = new FrameConstants
        {
            View = view,
            Proj = proj,
            ViewProj = viewProj,
            InvProj = invProj,
            InvViewProj = invViewProj,
            CameraPos = new Vector4(camera.Position, camera.Orthographic ? 1f : 0f),
            Grid = new Vector4(_gridX, _gridY, DepthSlices, TileSize),
            Slice = new Vector4(sliceScale, -MathF.Log2(near) * sliceScale, near, far),
            EnvA = new Vector4(maxMip, env0.Intensity, MathF.Cos(env0.Yaw), MathF.Sin(env0.Yaw)),
            ViewportInfo = new Vector4(width, height, 1f / width, 1f / height),
            Counts = new UInt4 { X = (uint)dirCount, Y = (uint)count, Z = ClusterStride },
            Sky = new Vector4(env0.BackgroundBlur * maxMip, 1f, 0, 0),
        };
        Device.WriteBuffer(_frameBuffer, 0, MemoryMarshal.AsBytes(new ReadOnlySpan<FrameConstants>(in _frame)));

        CollectDraws(scene, view, samples);
        RefreshBindGroups(env);
    }

    private static LightRecord MakeLight(Light3D l)
    {
        var dir = l.Direction.LengthSquared() > 0 ? Vector3.Normalize(l.Direction) : -Vector3.UnitZ;
        var color = l.Color * l.Intensity;
        var cos0 = MathF.Cos(l.InnerConeAngle);
        var cos1 = MathF.Cos(l.OuterConeAngle);
        var scale = 1f / MathF.Max(cos0 - cos1, 0.001f);

        // Where an unbounded light has faded below what any display could show (irradiance 0.0005), the cluster test stops caring.
        var peak = MathF.Max(color.X, MathF.Max(color.Y, color.Z));
        var cull = l.Range > 0 ? l.Range : MathF.Sqrt(MathF.Max(peak, 0f) / 0.0005f);
        return new LightRecord
        {
            PosRange = new Vector4(l.Position, MathF.Max(l.Range, 0f)),
            DirType = new Vector4(dir, (float)(int)l.Kind),
            Color = new Vector4(color, 0),
            Spot = new Vector4(scale, -cos1 * scale, cull, 0),
        };
    }

    private void EnsureClusterCapacity(int clusters)
    {
        if (clusters <= _clusterCapacity)
        {
            return;
        }

        _clusterCapacity = Math.Max(clusters, _clusterCapacity * 2);
        _owned.Remove(_clusterBuffer);
        _deferred.Add(_clusterBuffer);
        _clusterBuffer = Own(Device.CreateBuffer(new BufferDesc((ulong)_clusterCapacity * ClusterStride * 4, BufferUsage.Storage | BufferUsage.CopyDestination) { Label = "3d clusters" }));
    }

    private void CollectDraws(Scene3D scene, in Matrix4x4 view, int samples)
    {
        _opaqueCount = 0;
        _blendCount = 0;
        _objectCount = 0;
        _drawnTriangles = 0;
        var jointCount = 0;

        foreach (var instance in scene.Models)
        {
            if (!instance.Visible)
            {
                continue;
            }

            var model = instance.Model;
            var nodes = model.Source.Nodes;
            foreach (var n in model.DrawNodes)
            {
                var node = nodes[n];
                var prims = model.Meshes[node.Mesh]!;
                if (prims.Length == 0)
                {
                    continue;
                }

                var world = instance.NodeWorlds[n] * instance.Transform;
                var slot = _objectCount++;
                if (slot == _objectCapacity)
                {
                    GrowObjects(slot + 1);
                }

                Matrix4x4.Invert(world, out var inverse);
                var info = new UInt4();
                if (node.Skin >= 0 && node.Skin < model.Source.Skins.Length)
                {
                    var skin = model.Source.Skins[node.Skin];
                    var joints = skin.Joints.Length;
                    if (_skinScratch.Length < joints)
                    {
                        _skinScratch = new Matrix4x4[joints];
                    }

                    skin.ComputeJointMatrices(instance.NodeWorlds, n, _skinScratch);
                    if ((jointCount + joints) * 4 > _jointRows.Length)
                    {
                        Array.Resize(ref _jointRows, Math.Max(_jointRows.Length * 2, (jointCount + joints) * 4));
                    }

                    for (var j = 0; j < joints; j++)
                    {
                        var m = _skinScratch[j];
                        var o = (jointCount + j) * 4;
                        _jointRows[o] = new Vector4(m.M11, m.M12, m.M13, m.M14);
                        _jointRows[o + 1] = new Vector4(m.M21, m.M22, m.M23, m.M24);
                        _jointRows[o + 2] = new Vector4(m.M31, m.M32, m.M33, m.M34);
                        _jointRows[o + 3] = new Vector4(m.M41, m.M42, m.M43, m.M44);
                    }

                    info = new UInt4 { X = (uint)jointCount, Y = 1 };
                    jointCount += joints;
                }

                _objects[slot] = new ObjectConstants { Model = world, NormalMat = Matrix4x4.Transpose(inverse), Info = info };
                var flipped = world.GetDeterminant() < 0;

                for (var pi = 0; pi < prims.Length; pi++)
                {
                    var p = prims[pi];
                    var vertices = p.Vertices;
                    if (p.BaseVertices is not null && node.Mesh >= 0)
                    {
                        vertices = instance.MorphedVertices(Device, n, pi, p, instance.Pose.Weights[n]);
                    }

                    var center = Vector3.Transform(p.Center, world);
                    var viewZ = -Vector3.Transform(center, view).Z;
                    var item = new DrawItem
                    {
                        Primitive = p,
                        Vertices = vertices,
                        Slot = slot,
                        Pipeline = ScenePipeline(p.Material.Blend, p.Material.DoubleSided, flipped, samples),
                    };
                    _drawnTriangles += p.IndexCount / 3;
                    if (p.Material.Blend)
                    {
                        if (_blendCount == _blend.Length)
                        {
                            Array.Resize(ref _blend, _blend.Length * 2);
                            Array.Resize(ref _blendKeys, _blend.Length);
                        }

                        _blendKeys[_blendCount] = -viewZ;   // far to near
                        _blend[_blendCount++] = item;
                    }
                    else
                    {
                        if (_opaqueCount == _opaque.Length)
                        {
                            Array.Resize(ref _opaque, _opaque.Length * 2);
                            Array.Resize(ref _opaqueKeys, _opaque.Length);
                        }

                        _opaqueKeys[_opaqueCount] = viewZ;      // near to far, so early depth tests reject more
                        _opaque[_opaqueCount++] = item;
                    }
                }
            }
        }

        Array.Sort(_opaqueKeys, _opaque, 0, _opaqueCount);
        Array.Sort(_blendKeys, _blend, 0, _blendCount);

        if (_objectCount > 0)
        {
            Device.WriteBuffer(_objectBuffer, 0, MemoryMarshal.AsBytes(new ReadOnlySpan<ObjectConstants>(_objects, 0, _objectCount)));
        }

        // The joint buffer always holds at least one matrix so the binding is never empty.
        if (jointCount > 0)
        {
            EnsureJointCapacity(jointCount);
            Device.WriteBuffer(_jointBuffer, 0, MemoryMarshal.AsBytes(new ReadOnlySpan<Vector4>(_jointRows, 0, jointCount * 4)));
        }
    }

    private void GrowObjects(int needed)
    {
        _objectCapacity = Math.Max(needed, _objectCapacity * 2);
        _owned.Remove(_objectBuffer);
        _deferred.Add(_objectBuffer);
        _objectBuffer = Own(Device.CreateBuffer(new BufferDesc((ulong)_objectCapacity * GpuDevice.DynamicOffsetAlignment, BufferUsage.Uniform | BufferUsage.CopyDestination) { Label = "3d objects" }));
        if (_set1 is not null)
        {
            _owned.Remove(_set1);
            _deferred.Add(_set1);
        }

        _set1 = Own(Device.CreateBindGroup(_layout, 1, [BindGroupEntry.ForBuffer(0, _objectBuffer, 0, GpuDevice.DynamicOffsetAlignment)]));
        if (_objects.Length < _objectCapacity)
        {
            Array.Resize(ref _objects, _objectCapacity);
        }
    }

    private void EnsureJointCapacity(int joints)
    {
        if (joints <= _jointCapacity)
        {
            return;
        }

        _jointCapacity = Math.Max(joints, _jointCapacity * 2);
        _owned.Remove(_jointBuffer);
        _deferred.Add(_jointBuffer);
        _jointBuffer = Own(Device.CreateBuffer(new BufferDesc((ulong)_jointCapacity * 64, BufferUsage.Storage | BufferUsage.CopyDestination) { Label = "3d joints" }));
    }

    private void RefreshBindGroups(EnvironmentMap env)
    {
        if (_set0 is null || !ReferenceEquals(_boundEnvironment, env) || !ReferenceEquals(_boundCluster, _clusterBuffer) || !ReferenceEquals(_boundJoints, _jointBuffer))
        {
            if (_set0 is not null)
            {
                _owned.Remove(_set0);
                _deferred.Add(_set0);
            }

            if (_clusterSet0 is not null)
            {
                _owned.Remove(_clusterSet0);
                _deferred.Add(_clusterSet0);
            }

            _set0 = Own(Device.CreateBindGroup(_layout, 0,
            [
                BindGroupEntry.ForBuffer(0, _frameBuffer, 0, (ulong)Marshal.SizeOf<FrameConstants>()),
                BindGroupEntry.ForBuffer(1, _lightBuffer),
                BindGroupEntry.ForBuffer(2, _clusterBuffer),
                BindGroupEntry.ForBuffer(3, _jointBuffer),
                BindGroupEntry.ForTexture(4, env.Irradiance),
                BindGroupEntry.ForTexture(5, env.Specular),
                BindGroupEntry.ForTexture(6, BrdfLut),
                BindGroupEntry.ForSampler(7, _envSampler),
            ]));
            _clusterSet0 = Own(Device.CreateBindGroup(_clusterLayout, 0,
            [
                BindGroupEntry.ForBuffer(0, _frameBuffer, 0, (ulong)Marshal.SizeOf<FrameConstants>()),
                BindGroupEntry.ForBuffer(1, _lightBuffer),
                BindGroupEntry.ForBuffer(2, _clusterBuffer),
            ]));
            _boundEnvironment = env;
            _boundCluster = _clusterBuffer;
            _boundJoints = _jointBuffer;
        }
    }

    private sealed class ClusterPass : IGraphPass
    {
        public Renderer3D? Owner;
        public GraphBuffer Lights;
        public GraphBuffer Clusters;

        public string Name => "cluster_lights";

        public void Setup(PassBuilder builder)
        {
            builder.Compute();
            builder.Read(Lights, BufferRead.Storage);
            builder.WriteStorage(Clusters);
        }

        public void Execute(PassContext context)
        {
            var r = Owner!;
            var pass = context.Compute;
            pass.SetPipeline(r._clusterPipeline);
            pass.SetBindGroup(0, r._clusterSet0!);
            pass.Dispatch((uint)((r._gridX * r._gridY * DepthSlices) + 63) / 64);
        }
    }

    private sealed class ForwardPass : IGraphPass
    {
        public Renderer3D? Owner;
        public GraphTexture Color;
        public GraphTexture Resolve;
        public GraphTexture Depth;
        public GraphBuffer Lights;
        public GraphBuffer Clusters;
        public int Samples;
        public bool Sky;
        public Vector3 Clear;

        public string Name => "forward";

        public void Setup(PassBuilder builder)
        {
            builder.ColorTarget(Color, LoadOp.Clear, Clear.X, Clear.Y, Clear.Z, 1f, Resolve.IsValid ? StoreOp.Discard : StoreOp.Store, 0, 0, Resolve);
            builder.DepthTarget(Depth, LoadOp.Clear, 1f, StoreOp.Discard);
            if (Clusters.IsValid)
            {
                builder.Read(Lights, BufferRead.Storage);
                builder.Read(Clusters, BufferRead.Storage);
            }
        }

        public void Execute(PassContext context)
        {
            var r = Owner!;
            var pass = context.Render;
            pass.SetBindGroup(0, r._set0!);
            DrawList(r, pass, r._opaque, r._opaqueCount);
            if (Sky)
            {
                pass.SetPipeline(r.SkyPipeline(Samples));
                pass.Draw(3);
            }

            DrawList(r, pass, r._blend, r._blendCount);
        }

        private static void DrawList(Renderer3D r, GpuRenderPass pass, DrawItem[] items, int count)
        {
            GpuGraphicsPipeline? pipeline = null;
            Material3D? material = null;
            Span<uint> offset = stackalloc uint[1];
            for (var i = 0; i < count; i++)
            {
                ref var d = ref items[i];
                if (!ReferenceEquals(d.Pipeline, pipeline))
                {
                    pipeline = d.Pipeline;
                    pass.SetPipeline(pipeline);
                }

                offset[0] = (uint)d.Slot * GpuDevice.DynamicOffsetAlignment;
                pass.SetBindGroup(1, r._set1!, offset);
                if (!ReferenceEquals(d.Primitive.Material, material))
                {
                    material = d.Primitive.Material;
                    pass.SetBindGroup(2, material.Group);
                }

                pass.SetVertexBuffer(0, d.Vertices);
                pass.SetIndexBuffer(d.Primitive.Indices, d.Primitive.IndexFormat);
                pass.DrawIndexed(d.Primitive.IndexCount);
            }
        }
    }
}
