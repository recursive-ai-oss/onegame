using System.Runtime.InteropServices;

namespace OneGame.Gpu.Vulkan.Tests;

public sealed class RenderTests
{
    const int Size = 64;

    static GpuShader Shader(GpuDevice d, ShaderStage stage, string file) => d.CreateShader(stage, TestFiles.Spirv(file), "main");

    static GraphicsPipelineDesc TrianglePipeline(GpuDevice d, GpuFormat format, out GpuShader vs, out GpuShader fs, out GpuPipelineLayout layout)
    {
        vs = Shader(d, ShaderStage.Vertex, "triangle.vert");
        fs = Shader(d, ShaderStage.Pixel, "triangle.frag");
        layout = d.CreatePipelineLayout([]);
        return new GraphicsPipelineDesc
        {
            Layout = layout,
            Vertex = vs,
            Pixel = fs,
            ColorTargets = [new ColorTargetState(format)],
            Cull = CullMode.Back,
        };
    }

    [Fact]
    public void TriangleMatchesGolden()
    {
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        using var target = d.CreateTexture(new TextureDesc(Size, Size, GpuFormat.Rgba8Unorm, TextureUsage.RenderTarget | TextureUsage.CopySource));
        var desc = TrianglePipeline(d, GpuFormat.Rgba8Unorm, out var vs, out var fs, out var layout);
        using var pipeline = d.CreateGraphicsPipeline(desc);

        var enc = d.BeginCommands();
        var pass = enc.BeginRenderPass(ColorAttachment.Clear(target, 0.1f, 0.2f, 0.3f, 1f));
        pass.SetPipeline(pipeline);
        pass.Draw(3);
        pass.EndPass();
        d.Submit(enc);

        var pixels = new byte[Size * Size * 4];
        d.ReadTexture(target, 0, 0, pixels);
        Golden.Assert_("triangle", Size, Size, pixels);

        // Clip space is +Y up: the third vertex (0, 0.8) is the blue apex, near the top of the image.
        var top = (10 * Size + 32) * 4;
        var bottomLeft = (52 * Size + 14) * 4;
        Assert.True(pixels[top + 2] > pixels[top], "apex should be blue-ish");
        Assert.True(pixels[bottomLeft] > pixels[bottomLeft + 2], "bottom-left corner should be red-ish");
        vs.Dispose();
        fs.Dispose();
        layout.Dispose();
    }

    [Fact]
    public void BackFaceIsCulledWhenWindingIsFlipped()
    {
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        using var target = d.CreateTexture(new TextureDesc(Size, Size, GpuFormat.Rgba8Unorm, TextureUsage.RenderTarget | TextureUsage.CopySource));
        var desc = TrianglePipeline(d, GpuFormat.Rgba8Unorm, out var vs, out var fs, out var layout);
        // The triangle winds counter-clockwise in +Y-up clip space, so declaring clockwise as front makes it a back face.
        var culled = new GraphicsPipelineDesc { Layout = layout, Vertex = vs, Pixel = fs, ColorTargets = desc.ColorTargets, Cull = CullMode.Back, Front = FrontFace.Clockwise };
        using var pipeline = d.CreateGraphicsPipeline(culled);

        var enc = d.BeginCommands();
        var pass = enc.BeginRenderPass(ColorAttachment.Clear(target, 0, 0, 0, 1));
        pass.SetPipeline(pipeline);
        pass.Draw(3);
        pass.EndPass();
        d.Submit(enc);
        var pixels = new byte[Size * Size * 4];
        d.ReadTexture(target, 0, 0, pixels);
        Assert.All(Enumerable.Range(0, Size * Size), i => Assert.Equal(0, pixels[i * 4] + pixels[i * 4 + 1] + pixels[i * 4 + 2]));
        vs.Dispose();
        fs.Dispose();
        layout.Dispose();
    }

    [StructLayout(LayoutKind.Sequential)]
    struct Vertex
    {
        public float X, Y, U, V;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct Params
    {
        public float R, G, B, A;
        public float OffsetX, OffsetY, PadX, PadY;
    }

    [Fact]
    public void TexturedAlphaBlendedQuadsMatchGolden()
    {
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        using var target = d.CreateTexture(new TextureDesc(Size, Size, GpuFormat.Rgba8Unorm, TextureUsage.RenderTarget | TextureUsage.CopySource));

        // 4x4 checker of opaque orange and half-transparent white, with a mip chain generated on the GPU.
        const int T = 4;
        var texels = new byte[T * T * 4];
        for (var y = 0; y < T; y++)
        {
            for (var x = 0; x < T; x++)
            {
                var o = (y * T + x) * 4;
                if (((x + y) & 1) == 0)
                {
                    texels[o] = 255;
                    texels[o + 1] = 128;
                    texels[o + 2] = 0;
                    texels[o + 3] = 255;
                }
                else
                {
                    texels[o] = 255;
                    texels[o + 1] = 255;
                    texels[o + 2] = 255;
                    texels[o + 3] = 128;
                }
            }
        }

        using var texture = d.CreateTexture(new TextureDesc(T, T, GpuFormat.Rgba8Unorm, TextureUsage.Sampled | TextureUsage.CopyDestination | TextureUsage.CopySource) { MipLevels = GpuTexture.FullMipCount(T, T) });
        d.WriteTexture(texture, 0, 0, texels);

        Vertex[] quad =
        [
            new() { X = -0.5f, Y = -0.5f, U = 0, V = 1 },
            new() { X = 0.5f, Y = -0.5f, U = 1, V = 1 },
            new() { X = -0.5f, Y = 0.5f, U = 0, V = 0 },
            new() { X = 0.5f, Y = 0.5f, U = 1, V = 0 },
        ];
        using var vb = d.CreateBuffer(new BufferDesc((ulong)(quad.Length * 16), BufferUsage.Vertex | BufferUsage.CopyDestination));
        d.WriteBuffer(vb, 0, MemoryMarshal.AsBytes<Vertex>(quad));
        ushort[] idx = [0, 1, 2, 2, 1, 3];
        using var ib = d.CreateBuffer(new BufferDesc((ulong)(idx.Length * 2), BufferUsage.Index | BufferUsage.CopyDestination));
        d.WriteBuffer(ib, 0, MemoryMarshal.AsBytes<ushort>(idx));

        using var ub1 = d.CreateBuffer(new BufferDesc(32, BufferUsage.Uniform | BufferUsage.CopyDestination));
        using var ub2 = d.CreateBuffer(new BufferDesc(32, BufferUsage.Uniform | BufferUsage.CopyDestination));
        d.WriteBuffer(ub1, 0, MemoryMarshal.AsBytes<Params>([new Params { R = 1, G = 1, B = 1, A = 1, OffsetX = -0.25f, OffsetY = -0.25f }]));
        d.WriteBuffer(ub2, 0, MemoryMarshal.AsBytes<Params>([new Params { R = 1, G = 1, B = 1, A = 0.5f, OffsetX = 0.25f, OffsetY = 0.25f }]));

        using var sampler = d.CreateSampler(SamplerDesc.Nearest);
        using var vs = Shader(d, ShaderStage.Vertex, "quad.vert");
        using var fs = Shader(d, ShaderStage.Pixel, "quad.frag");
        using var layout = d.CreatePipelineLayout(
        [
            new ResourceBinding(0, 0, 1, ResourceKind.ConstantBuffer, ShaderStage.Vertex | ShaderStage.Pixel),
            new ResourceBinding(0, 1, 1, ResourceKind.SampledTexture, ShaderStage.Pixel),
            new ResourceBinding(0, 2, 1, ResourceKind.Sampler, ShaderStage.Pixel),
        ]);
        using var group1 = d.CreateBindGroup(layout, 0, [BindGroupEntry.ForBuffer(0, ub1), BindGroupEntry.ForTexture(1, texture), BindGroupEntry.ForSampler(2, sampler)]);
        using var group2 = d.CreateBindGroup(layout, 0, [BindGroupEntry.ForBuffer(0, ub2), BindGroupEntry.ForTexture(1, texture), BindGroupEntry.ForSampler(2, sampler)]);
        using var pipeline = d.CreateGraphicsPipeline(new GraphicsPipelineDesc
        {
            Layout = layout,
            Vertex = vs,
            Pixel = fs,
            VertexBuffers =
            [
                new VertexBufferLayout
                {
                    Stride = 16,
                    Attributes = new[] { new VertexElement(0, VertexFormat.Float2, 0), new VertexElement(1, VertexFormat.Float2, 8) },
                },
            ],
            ColorTargets = [new ColorTargetState(GpuFormat.Rgba8Unorm) { Blend = BlendState.AlphaBlend }],
        });

        var enc = d.BeginCommands();
        enc.GenerateMips(texture);
        var pass = enc.BeginRenderPass(ColorAttachment.Clear(target, 0.1f, 0.2f, 0.6f, 1f));
        pass.SetPipeline(pipeline);
        pass.SetVertexBuffer(0, vb);
        pass.SetIndexBuffer(ib, IndexFormat.Index16);
        pass.SetBindGroup(0, group1);
        pass.DrawIndexed(6);
        pass.SetBindGroup(0, group2);
        pass.DrawIndexed(6);
        pass.EndPass();
        d.Submit(enc);

        var pixels = new byte[Size * Size * 4];
        d.ReadTexture(target, 0, 0, pixels);
        Golden.Assert_("textured-quads", Size, Size, pixels);

        // The generated top mip of a checker averages orange and half-transparent white.
        var mip = new byte[4];
        d.ReadTexture(texture, 2, 0, mip);
        Assert.InRange(mip[0], 250, 255);
        Assert.InRange(mip[1], 185, 200);
    }

    [Fact]
    public void ComputeWritesStorageBuffer()
    {
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        const int N = 1024;
        var input = Enumerable.Range(0, N).Select(i => (uint)(i * 5)).ToArray();
        using var inBuf = d.CreateBuffer(new BufferDesc(N * 4, BufferUsage.Storage | BufferUsage.CopyDestination));
        using var outBuf = d.CreateBuffer(new BufferDesc(N * 4, BufferUsage.Storage | BufferUsage.CopySource));
        d.WriteBuffer(inBuf, 0, MemoryMarshal.AsBytes<uint>(input));

        using var cs = Shader(d, ShaderStage.Compute, "fill.comp");
        using var layout = d.CreatePipelineLayout(
        [
            new ResourceBinding(0, 0, 1, ResourceKind.StorageBuffer, ShaderStage.Compute),
            new ResourceBinding(0, 1, 1, ResourceKind.ReadOnlyStorageBuffer, ShaderStage.Compute),
        ]);
        using var group = d.CreateBindGroup(layout, 0, [BindGroupEntry.ForBuffer(0, outBuf), BindGroupEntry.ForBuffer(1, inBuf)]);
        using var pipeline = d.CreateComputePipeline(new ComputePipelineDesc { Layout = layout, Compute = cs });

        var enc = d.BeginCommands();
        var pass = enc.BeginComputePass();
        pass.SetPipeline(pipeline);
        pass.SetBindGroup(0, group);
        pass.Dispatch(N / 64);
        pass.EndPass();
        d.Submit(enc);

        var result = new uint[N];
        d.ReadBuffer(outBuf, 0, MemoryMarshal.AsBytes(result.AsSpan()));
        for (uint i = 0; i < N; i++)
        {
            Assert.Equal(input[i] * 3u + i * i + 7u, result[i]);
        }
    }

    [Fact]
    public void ChainedComputeDispatchesAreOrdered()
    {
        // The second dispatch reads what the first wrote (the same buffer bound as output then input): needs a barrier between them.
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        const int N = 256;
        using var a = d.CreateBuffer(new BufferDesc(N * 4, BufferUsage.Storage | BufferUsage.CopyDestination | BufferUsage.CopySource));
        using var b = d.CreateBuffer(new BufferDesc(N * 4, BufferUsage.Storage | BufferUsage.CopyDestination | BufferUsage.CopySource));
        d.WriteBuffer(a, 0, MemoryMarshal.AsBytes<uint>(new uint[N]));
        d.WriteBuffer(b, 0, MemoryMarshal.AsBytes<uint>(new uint[N]));
        using var cs = Shader(d, ShaderStage.Compute, "fill.comp");
        using var layout = d.CreatePipelineLayout(
        [
            new ResourceBinding(0, 0, 1, ResourceKind.StorageBuffer, ShaderStage.Compute),
            new ResourceBinding(0, 1, 1, ResourceKind.ReadOnlyStorageBuffer, ShaderStage.Compute),
        ]);
        using var ab = d.CreateBindGroup(layout, 0, [BindGroupEntry.ForBuffer(0, a), BindGroupEntry.ForBuffer(1, b)]);
        using var ba = d.CreateBindGroup(layout, 0, [BindGroupEntry.ForBuffer(0, b), BindGroupEntry.ForBuffer(1, a)]);
        using var pipeline = d.CreateComputePipeline(new ComputePipelineDesc { Layout = layout, Compute = cs });

        var enc = d.BeginCommands();
        var pass = enc.BeginComputePass();
        pass.SetPipeline(pipeline);
        for (var round = 0; round < 4; round++)
        {
            pass.SetBindGroup(0, round % 2 == 0 ? ab : ba);
            pass.Dispatch(N / 64);
        }

        pass.EndPass();
        d.Submit(enc);

        // Replay on the CPU.
        uint[] x = new uint[N], y = new uint[N];
        for (var round = 0; round < 4; round++)
        {
            var (dst, src) = round % 2 == 0 ? (x, y) : (y, x);
            for (uint i = 0; i < N; i++)
            {
                dst[i] = src[i] * 3u + i * i + 7u;
            }
        }

        var got = new uint[N];
        d.ReadBuffer(a, 0, MemoryMarshal.AsBytes(got.AsSpan()));
        Assert.Equal(x, got);
    }

    [Fact]
    public void BufferRoundTripThroughCopyAndReadback()
    {
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        var data = Enumerable.Range(0, 1000).Select(i => (byte)(i * 7)).ToArray();
        using var src = d.CreateBuffer(new BufferDesc(1000, BufferUsage.CopyDestination | BufferUsage.CopySource));
        using var dst = d.CreateBuffer(new BufferDesc(1000, BufferUsage.CopyDestination | BufferUsage.CopySource));
        d.WriteBuffer(src, 0, data);
        var enc = d.BeginCommands();
        enc.CopyBuffer(src, 0, dst, 0, 1000);
        d.Submit(enc);
        var back = new byte[1000];
        d.ReadBuffer(dst, 0, back);
        Assert.Equal(data, back);
    }

    [Fact]
    public void MultisampledTargetResolves()
    {
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        using var msaa = d.CreateTexture(new TextureDesc(Size, Size, GpuFormat.Rgba8Unorm, TextureUsage.RenderTarget) { SampleCount = 4 });
        using var resolved = d.CreateTexture(new TextureDesc(Size, Size, GpuFormat.Rgba8Unorm, TextureUsage.RenderTarget | TextureUsage.CopySource));
        var desc = TrianglePipeline(d, GpuFormat.Rgba8Unorm, out var vs, out var fs, out var layout);
        using var pipeline = d.CreateGraphicsPipeline(new GraphicsPipelineDesc { Layout = layout, Vertex = vs, Pixel = fs, ColorTargets = desc.ColorTargets, SampleCount = 4 });
        var enc = d.BeginCommands();
        var pass = enc.BeginRenderPass(new ColorAttachment { Target = msaa, Resolve = resolved, Load = LoadOp.Clear, Store = StoreOp.Discard, ClearA = 1 });
        pass.SetPipeline(pipeline);
        pass.Draw(3);
        pass.EndPass();
        d.Submit(enc);
        var pixels = new byte[Size * Size * 4];
        d.ReadTexture(resolved, 0, 0, pixels);
        Golden.Assert_("triangle-msaa", Size, Size, pixels);
        vs.Dispose();
        fs.Dispose();
        layout.Dispose();
    }

    [Fact]
    public void DepthTestOrdersOverlappingTriangles()
    {
        // A triangle at depth 0 passes LessEqual against a cleared depth of 1, so the depth-tested image equals the plain one.
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        using var target = d.CreateTexture(new TextureDesc(Size, Size, GpuFormat.Rgba8Unorm, TextureUsage.RenderTarget | TextureUsage.CopySource));
        using var depth = d.CreateTexture(new TextureDesc(Size, Size, GpuFormat.Depth32Float, TextureUsage.RenderTarget));
        var desc = TrianglePipeline(d, GpuFormat.Rgba8Unorm, out var vs, out var fs, out var layout);
        using var pipeline = d.CreateGraphicsPipeline(new GraphicsPipelineDesc
        {
            Layout = layout,
            Vertex = vs,
            Pixel = fs,
            ColorTargets = desc.ColorTargets,
            DepthStencil = new DepthStencilState(GpuFormat.Depth32Float) { DepthCompare = CompareFunction.LessEqual },
        });
        var enc = d.BeginCommands();
        var pass = enc.BeginRenderPass(ColorAttachment.Clear(target, 0, 0, 0, 1), new DepthAttachment { Target = depth, Load = LoadOp.Clear, Store = StoreOp.Discard, ClearDepth = 1f });
        pass.SetPipeline(pipeline);
        pass.Draw(3);
        pass.EndPass();
        d.Submit(enc);
        var pixels = new byte[Size * Size * 4];
        d.ReadTexture(target, 0, 0, pixels);
        Golden.Assert_("triangle-depth", Size, Size, pixels);
        vs.Dispose();
        fs.Dispose();
        layout.Dispose();
    }

    [Fact]
    public void SteadyStateFramesAllocateNothing()
    {
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        using var target = d.CreateTexture(new TextureDesc(Size, Size, GpuFormat.Rgba8Unorm, TextureUsage.RenderTarget | TextureUsage.CopySource));
        var desc = TrianglePipeline(d, GpuFormat.Rgba8Unorm, out var vs, out var fs, out var layout);
        using var pipeline = d.CreateGraphicsPipeline(desc);
        using var ub = d.CreateBuffer(new BufferDesc(256, BufferUsage.Uniform | BufferUsage.CopyDestination));
        var bytes = new byte[256];

        void Frame()
        {
            d.WriteBuffer(ub, 0, bytes);
            var enc = d.BeginCommands();
            var pass = enc.BeginRenderPass(ColorAttachment.Clear(target, 0.1f, 0.1f, 0.1f, 1f));
            pass.SetPipeline(pipeline);
            pass.Draw(3);
            pass.EndPass();
            d.Submit(enc);
        }

        // Pools (encoders, staging memory, lists) grow until they match how far the GPU lags the CPU, which varies by device.
        // Steady state means that growth has stopped: three bursts in a row that allocate nothing.
        var zeroBursts = 0;
        for (var burst = 0; burst < 30 && zeroBursts < 3; burst++)
        {
            d.WaitIdle();
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 100; i++)
            {
                Frame();
            }

            zeroBursts = GC.GetAllocatedBytesForCurrentThread() == before ? zeroBursts + 1 : 0;
        }

        d.WaitIdle();
        Assert.Equal(3, zeroBursts);
        vs.Dispose();
        fs.Dispose();
        layout.Dispose();
    }

    [Fact]
    public void ResourcesDisposedWhileInFlightAreReleasedSafely()
    {
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        for (var i = 0; i < 20; i++)
        {
            using var target = d.CreateTexture(new TextureDesc(Size, Size, GpuFormat.Rgba8Unorm, TextureUsage.RenderTarget));
            var enc = d.BeginCommands();
            var pass = enc.BeginRenderPass(ColorAttachment.Clear(target, 1, 0, 0, 1));
            pass.EndPass();
            d.Submit(enc);
        }
    }

    [Fact]
    public void BadArgumentsThrowTypedErrors()
    {
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        using var buf = d.CreateBuffer(new BufferDesc(16, BufferUsage.Vertex));
        Assert.Throws<ArgumentException>(() => d.WriteBuffer(buf, 0, new byte[4]));
        Assert.Throws<ArgumentException>(() => d.CreateShader(ShaderStage.Vertex, new byte[] { 1, 2, 3 }, "main"));
        Assert.Throws<ArgumentOutOfRangeException>(() => d.CreateBuffer(new BufferDesc(0, BufferUsage.Vertex)));
        using var tex = d.CreateTexture(new TextureDesc(4, 4, GpuFormat.Rgba8Unorm, TextureUsage.CopyDestination));
        Assert.Throws<ArgumentException>(() => d.WriteTexture(tex, 0, 0, new byte[10]));
        var enc = d.BeginCommands();
        d.Submit(enc);
        Assert.Throws<InvalidOperationException>(() => d.Submit(enc));
    }

    [Fact]
    public void AdapterSelectionByKindAndName()
    {
        var adapters = VulkanGpu.EnumerateAdapters();
        Assert.NotEmpty(adapters);
        using var sw = VulkanGpu.CreateDevice(new GpuDeviceOptions { Adapter = adapters[0].Name });
        Assert.Equal(adapters[0].Name, sw.Adapter.Name);
        Assert.Throws<GpuUnsupportedException>(() => VulkanGpu.CreateDevice(new GpuDeviceOptions { Adapter = "no such adapter" }));
    }
}
