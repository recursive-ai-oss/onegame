using System.Runtime.InteropServices;
using OneGame.Gpu;
using OneGame.Render3D.Shaders;

namespace OneGame.Render3D;

// Billboards: camera-facing quads drawn at the end of the forward pass, depth tested and not depth writing. Every batch of the frame is
// written into one vertex buffer (blended batches sorted far to near) and drawn with one instanced draw each.
public sealed partial class Renderer3D
{
    [StructLayout(LayoutKind.Sequential)]
    private struct BillboardGpu
    {
        public Vector4 PosRot;
        public Vector4 SizeAdd;
        public Vector4 Uv;
        public Vector4 Color;
    }

    private struct BillboardDraw
    {
        public BillboardTexture3D? Texture;
        public int First;
        public int Count;
    }

    private GpuPipelineLayout? _bbLayout;
    private GpuShader? _bbVs;
    private GpuShader? _bbPs;
    private GpuSampler? _bbSampler;
    private VertexBufferLayout? _bbVertex;
    private GpuBuffer? _bbBuffer;
    private GpuBindGroup? _bbSet0;
    private GpuBuffer? _bbSet0For;
    private BillboardTexture3D? _softSpot;
    private BillboardGpu[] _bbStaging = new BillboardGpu[256];
    private BillboardDraw[] _bbDraws = new BillboardDraw[8];
    private float[] _bbKeys = new float[256];
    private int[] _bbOrder = new int[256];
    private int _bbCount;
    private int _bbDrawCount;
    private int _bbCapacity;

    /// <summary>A round white spot that fades smoothly to nothing at its edge: what billboards show when their batch has no texture.</summary>
    public BillboardTexture3D SoftSpot => _softSpot ??= MakeSoftSpot();

    /// <summary>Uploads an image for billboards.</summary>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="rgba">Straight-alpha sRGB bytes (R, G, B, A), top row first. They are premultiplied in linear light here, so filtering and blending are exact.</param>
    /// <param name="label">A name for debugging.</param>
    /// <returns>The texture. Dispose it before the renderer.</returns>
    /// <exception cref="ArgumentException">The sizes are not positive or the data is not <c>width * height * 4</c> bytes.</exception>
    public BillboardTexture3D CreateBillboardTexture(int width, int height, ReadOnlySpan<byte> rgba, string? label = null)
    {
        if (width < 1 || height < 1 || rgba.Length != checked(width * height * 4))
        {
            throw new ArgumentException("the pixel data must be width * height * 4 bytes");
        }

        EnsureBillboards();
        var data = new byte[rgba.Length];
        for (var i = 0; i < data.Length; i += 4)
        {
            var a = rgba[i + 3] / 255f;
            for (var c = 0; c < 3; c++)
            {
                data[i + c] = EncodeSrgb(DecodeSrgb(rgba[i + c]) * a);
            }

            data[i + 3] = rgba[i + 3];
        }

        var tex = Device.CreateTexture(new TextureDesc((uint)width, (uint)height, GpuFormat.Rgba8UnormSrgb, TextureUsage.Sampled | TextureUsage.CopyDestination) { Label = label ?? "billboard" });
        Device.WriteTexture(tex, 0, 0, data);
        var group = Device.CreateBindGroup(_bbLayout!, 1, [BindGroupEntry.ForTexture(0, tex), BindGroupEntry.ForSampler(1, _bbSampler!)]);
        return new BillboardTexture3D(tex, group, width, height);
    }

    private BillboardTexture3D MakeSoftSpot()
    {
        const int n = 64;
        var px = new byte[n * n * 4];
        for (var y = 0; y < n; y++)
        {
            for (var x = 0; x < n; x++)
            {
                var dx = ((x + 0.5f) / n * 2f) - 1f;
                var dy = ((y + 0.5f) / n * 2f) - 1f;
                var a = Math.Clamp(1f - MathF.Sqrt((dx * dx) + (dy * dy)), 0f, 1f);
                a = a * a * (3f - (2f * a));
                var i = ((y * n) + x) * 4;
                px[i] = px[i + 1] = px[i + 2] = 255;
                px[i + 3] = (byte)MathF.Round(a * 255f);
            }
        }

        return CreateBillboardTexture(n, n, px, "soft spot");
    }

    private static float DecodeSrgb(byte b)
    {
        var c = b / 255f;
        return c <= 0.04045f ? c / 12.92f : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
    }

    private static byte EncodeSrgb(float l)
    {
        l = Math.Clamp(l, 0f, 1f);
        var c = l <= 0.0031308f ? l * 12.92f : (1.055f * MathF.Pow(l, 1f / 2.4f)) - 0.055f;
        return (byte)MathF.Round(c * 255f);
    }

    private void EnsureBillboards()
    {
        if (_bbLayout is not null)
        {
            return;
        }

        _bbLayout = Own(Device.CreatePipelineLayout(ShaderLayouts.Bindings([Billboard.VSBillboard.Layout, Billboard.PSBillboard.Layout])));
        _bbVs = Own(Device.CreateShader(new ShaderSource(ShaderStage.Vertex, Billboard.VSBillboard.EntryPoint) { SpirV = Billboard.VSBillboard.SpirV, Wgsl = Billboard.VSBillboard.Wgsl, WgslEntryPoint = Billboard.VSBillboard.WgslEntryPoint, Msl = Billboard.VSBillboard.Msl, MslFunctionName = Billboard.VSBillboard.MslFunctionName, Layout = Billboard.VSBillboard.Layout }));
        _bbPs = Own(Device.CreateShader(new ShaderSource(ShaderStage.Pixel, Billboard.PSBillboard.EntryPoint) { SpirV = Billboard.PSBillboard.SpirV, Wgsl = Billboard.PSBillboard.Wgsl, WgslEntryPoint = Billboard.PSBillboard.WgslEntryPoint, Msl = Billboard.PSBillboard.Msl, MslFunctionName = Billboard.PSBillboard.MslFunctionName, Layout = Billboard.PSBillboard.Layout }));
        _bbSampler = Own(Device.CreateSampler(SamplerDesc.Linear with { AddressU = AddressMode.ClampToEdge, AddressV = AddressMode.ClampToEdge }));
        _bbVertex = ShaderLayouts.Vertex(Billboard.VSBillboard.Layout, VertexStepMode.Instance);
    }

    // Sorts and uploads this frame's billboards. Runs while the frame is prepared, once the camera is known.
    private void CollectBillboards(Scene3D scene, in Matrix4x4 view)
    {
        _bbCount = 0;
        _bbDrawCount = 0;
        foreach (var batch in scene.Billboards)
        {
            if (!batch.Visible || batch.Count == 0)
            {
                continue;
            }

            EnsureBillboards();
            var n = batch.Count;
            var raw = batch.Raw;
            if (_bbCount + n > _bbStaging.Length)
            {
                Array.Resize(ref _bbStaging, Math.Max(_bbCount + n, _bbStaging.Length * 2));
            }

            if (n > _bbKeys.Length)
            {
                _bbKeys = new float[n * 2];
                _bbOrder = new int[n * 2];
            }

            // Blended quads must be drawn far to near. Without any, the order is the caller's.
            var sort = false;
            for (var i = 0; i < n; i++)
            {
                _bbOrder[i] = i;
                if (!raw[i].Additive)
                {
                    sort = true;
                }
            }

            if (sort)
            {
                for (var i = 0; i < n; i++)
                {
                    // Cameras look down -Z in view space, so farther quads have the more negative z.
                    var p = raw[i].Position;
                    _bbKeys[i] = (p.X * view.M13) + (p.Y * view.M23) + (p.Z * view.M33) + view.M43;
                }

                Array.Sort(_bbKeys, _bbOrder, 0, n);
            }

            var first = _bbCount;
            for (var k = 0; k < n; k++)
            {
                ref var q = ref raw[_bbOrder[k]];
                var uv = q.Uv.Z > 0f && q.Uv.W > 0f ? q.Uv : new Vector4(0, 0, 1, 1);
                var a = Math.Clamp(q.Color.W, 0f, 1f);
                _bbStaging[_bbCount++] = new BillboardGpu
                {
                    PosRot = new Vector4(q.Position, q.Rotation),
                    SizeAdd = new Vector4(q.Size.X, q.Size.Y, q.Additive ? 1f : 0f, 0f),
                    Uv = uv,
                    Color = new Vector4(q.Color.X * a, q.Color.Y * a, q.Color.Z * a, a),
                };
            }

            if (_bbDrawCount == _bbDraws.Length)
            {
                Array.Resize(ref _bbDraws, _bbDrawCount * 2);
            }

            _bbDraws[_bbDrawCount++] = new BillboardDraw { Texture = batch.Texture ?? SoftSpot, First = first, Count = n };
        }

        if (_bbCount == 0)
        {
            return;
        }

        if (_bbBuffer is null || _bbCount > _bbCapacity)
        {
            if (_bbBuffer is not null)
            {
                _owned.Remove(_bbBuffer);
                _deferred.Add(_bbBuffer);
            }

            _bbCapacity = Math.Max(_bbCount, Math.Max(_bbCapacity * 2, 256));
            _bbBuffer = Own(Device.CreateBuffer(new BufferDesc((ulong)_bbCapacity * 64, BufferUsage.Vertex | BufferUsage.CopyDestination) { Label = "3d billboards" }));
        }

        Device.WriteBuffer(_bbBuffer, 0, MemoryMarshal.AsBytes(new ReadOnlySpan<BillboardGpu>(_bbStaging, 0, _bbCount)));
    }

    private void DrawBillboards(GpuRenderPass pass, int samples)
    {
        if (_bbDrawCount == 0)
        {
            return;
        }

        if (_bbSet0 is null || !ReferenceEquals(_bbSet0For, _frameBuffer))
        {
            if (_bbSet0 is not null)
            {
                _owned.Remove(_bbSet0);
                _deferred.Add(_bbSet0);
            }

            _bbSet0 = Own(Device.CreateBindGroup(_bbLayout!, 0, [BindGroupEntry.ForBuffer(0, _frameBuffer, 0, (ulong)Marshal.SizeOf<FrameConstants>())]));
            _bbSet0For = _frameBuffer;
        }

        pass.SetPipeline(BillboardPipeline(samples));
        pass.SetBindGroup(0, _bbSet0);
        for (var i = 0; i < _bbDrawCount; i++)
        {
            ref var d = ref _bbDraws[i];
            pass.SetBindGroup(1, d.Texture!.Group);
            pass.SetVertexBuffer(0, _bbBuffer!, (ulong)d.First * 64);
            pass.Draw(6, (uint)d.Count);
        }
    }

    private GpuGraphicsPipeline BillboardPipeline(int samples)
    {
        var key = -100 - samples;
        if (!_pipelines.TryGetValue(key, out var pipeline))
        {
            pipeline = Device.CreateGraphicsPipeline(new GraphicsPipelineDesc
            {
                Layout = _bbLayout!,
                Vertex = _bbVs!,
                Pixel = _bbPs!,
                VertexBuffers = [_bbVertex!],
                Topology = PrimitiveTopology.TriangleList,
                Cull = CullMode.None,
                ColorTargets = [new ColorTargetState(HdrFormat) { Blend = BlendState.Premultiplied }],
                DepthStencil = new DepthStencilState(DepthFormat) { DepthWrite = false, DepthCompare = CompareFunction.Less },
                SampleCount = (uint)samples,
                Label = "billboards",
            });
            _pipelines[key] = pipeline;
        }

        return pipeline;
    }
}
