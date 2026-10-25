using System.Buffers.Binary;
using System.Runtime.InteropServices;
using OneGame.Gpu.Vulkan.Tests.Shaders;

namespace OneGame.Gpu.Vulkan.Tests;

/// <summary>Each feature of the 3D-capable GPU layer, rendered or computed and read back, with the validation layer required to stay silent.</summary>
public sealed class FeatureTests
{



    static GpuGraphicsPipeline Fullscreen(GpuDevice d, GpuShader vs, GpuShader ps, GpuPipelineLayout layout, GpuFormat format, uint samples = 1) =>
        d.CreateGraphicsPipeline(new GraphicsPipelineDesc
        {
            Layout = layout,
            Vertex = vs,
            Pixel = ps,
            ColorTargets = [new ColorTargetState(format)],
            SampleCount = samples,
        });

    static GpuTexture Target(GpuDevice d, uint w, uint h, GpuFormat format = GpuFormat.Rgba8Unorm) =>
        d.CreateTexture(new TextureDesc(w, h, format, TextureUsage.RenderTarget | TextureUsage.CopySource));

    static async Task<byte[]> ReadAsync(GpuDevice d, GpuTexture t, uint mip = 0, uint layer = 0)
    {
        var bytes = new byte[GpuDevice.LevelByteSize(t.Format, t.Width, t.Height, t.Depth, mip)];
        await d.ReadTextureAsync(t, mip, layer, bytes);
        return bytes;
    }

    static void DrawFullscreen(GpuDevice d, GpuTexture target, GpuGraphicsPipeline pipeline, params (uint Set, GpuBindGroup Group)[] groups)
    {
        var enc = d.BeginCommands();
        var pass = enc.BeginRenderPass(ColorAttachment.Clear(target, 0, 0, 0, 1));
        pass.SetPipeline(pipeline);
        foreach (var (set, group) in groups)
        {
            pass.SetBindGroup(set, group);
        }

        pass.Draw(3);
        pass.EndPass();
        d.Submit(enc);
    }

    static void AssertPixel(byte[] pixels, int index, int r, int g, int b, int a = 255, int tolerance = 0)
    {
        var o = index * 4;
        Assert.InRange(pixels[o], Math.Max(r - tolerance, 0), Math.Min(r + tolerance, 255));
        Assert.InRange(pixels[o + 1], Math.Max(g - tolerance, 0), Math.Min(g + tolerance, 255));
        Assert.InRange(pixels[o + 2], Math.Max(b - tolerance, 0), Math.Min(b + tolerance, 255));
        Assert.InRange(pixels[o + 3], Math.Max(a - tolerance, 0), Math.Min(a + tolerance, 255));
    }

    static readonly ResourceBinding SampledAt0 = new(0, 0, 1, ResourceKind.SampledTexture, ShaderStage.Pixel);
    static readonly ResourceBinding SamplerAt1 = new(0, 1, 1, ResourceKind.Sampler, ShaderStage.Pixel);

    [Fact]
    public void CapabilitiesAreReported()
    {
        using var gpu = new GpuFixture();
        var caps = gpu.Device.Capabilities;
        Assert.True(caps.MaxSampleCount >= 1);
        Assert.True(caps.MaxTextureSize >= 4096);
        Assert.True(caps.MaxTextureSize3D >= 256);
#if !WEB_BACKEND
        Assert.True(caps.MaxResourceArrayCount >= 16);
#endif
    }

    [Fact]
    public async Task CubeMapIsRenderedFaceByFaceWithDynamicOffsetsThenSampled()
    {
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        byte[][] colors = [[255, 0, 0], [0, 255, 0], [0, 0, 255], [255, 255, 0], [0, 255, 255], [255, 0, 255]];
        using var cube = d.CreateTexture(new TextureDesc(8, 8, GpuFormat.Rgba8Unorm, TextureUsage.RenderTarget | TextureUsage.Sampled) { Dimension = TextureDimension.Cube, ArrayLayers = 6 });

        // One constant buffer holds all six colors; a dynamic offset picks the face's.
        using var ub = d.CreateBuffer(new BufferDesc(6 * GpuDevice.DynamicOffsetAlignment, BufferUsage.Uniform | BufferUsage.CopyDestination));
        for (var i = 0; i < 6; i++)
        {
            float[] c = [colors[i][0] / 255f, colors[i][1] / 255f, colors[i][2] / 255f, 1f];
            d.WriteBuffer(ub, (ulong)i * GpuDevice.DynamicOffsetAlignment, MemoryMarshal.AsBytes<float>(c));
        }

        using var vs = d.CreateShader(new ShaderSource(ShaderStage.Vertex, FaceFill.VSMain.EntryPoint) { SpirV = FaceFill.VSMain.SpirV, Wgsl = FaceFill.VSMain.Wgsl, WgslEntryPoint = FaceFill.VSMain.WgslEntryPoint, Msl = FaceFill.VSMain.Msl, MslFunctionName = FaceFill.VSMain.MslFunctionName, Bindings = FaceFill.Bindings.All });
        using var ps = d.CreateShader(new ShaderSource(ShaderStage.Pixel, FaceFill.PSMain.EntryPoint) { SpirV = FaceFill.PSMain.SpirV, Wgsl = FaceFill.PSMain.Wgsl, WgslEntryPoint = FaceFill.PSMain.WgslEntryPoint, Msl = FaceFill.PSMain.Msl, MslFunctionName = FaceFill.PSMain.MslFunctionName, Bindings = FaceFill.Bindings.All });
        using var fillLayout = d.CreatePipelineLayout([new ResourceBinding(0, 0, 1, ResourceKind.ConstantBuffer, ShaderStage.Pixel) { Dynamic = true }]);
        using var fillGroup = d.CreateBindGroup(fillLayout, 0, [BindGroupEntry.ForBuffer(0, ub, 0, 16)]);
        using var fillPipeline = Fullscreen(d, vs, ps, fillLayout, GpuFormat.Rgba8Unorm);

        var enc = d.BeginCommands();
        for (uint face = 0; face < 6; face++)
        {
            var pass = enc.BeginRenderPass(new ColorAttachment { Target = cube, Layer = face, Load = LoadOp.DontCare, Store = StoreOp.Store });
            pass.SetPipeline(fillPipeline);
            pass.SetBindGroup(0, fillGroup, [face * GpuDevice.DynamicOffsetAlignment]);
            pass.Draw(3);
            pass.EndPass();
        }

        d.Submit(enc);

        using var svs = d.CreateShader(new ShaderSource(ShaderStage.Vertex, CubeSample.VSMain.EntryPoint) { SpirV = CubeSample.VSMain.SpirV, Wgsl = CubeSample.VSMain.Wgsl, WgslEntryPoint = CubeSample.VSMain.WgslEntryPoint, Msl = CubeSample.VSMain.Msl, MslFunctionName = CubeSample.VSMain.MslFunctionName, Bindings = CubeSample.Bindings.All });
        using var sps = d.CreateShader(new ShaderSource(ShaderStage.Pixel, CubeSample.PSMain.EntryPoint) { SpirV = CubeSample.PSMain.SpirV, Wgsl = CubeSample.PSMain.Wgsl, WgslEntryPoint = CubeSample.PSMain.WgslEntryPoint, Msl = CubeSample.PSMain.Msl, MslFunctionName = CubeSample.PSMain.MslFunctionName, Bindings = CubeSample.Bindings.All });
        using var layout = d.CreatePipelineLayout([SampledAt0, SamplerAt1]);
        using var sampler = d.CreateSampler(SamplerDesc.Nearest);
        using var group = d.CreateBindGroup(layout, 0, [BindGroupEntry.ForTexture(0, cube), BindGroupEntry.ForSampler(1, sampler)]);
        using var pipeline = Fullscreen(d, svs, sps, layout, GpuFormat.Rgba8Unorm);
        using var target = Target(d, 6, 1);
        DrawFullscreen(d, target, pipeline, (0, group));

        var pixels = await ReadAsync(d, target);
        for (var i = 0; i < 6; i++)
        {
            AssertPixel(pixels, i, colors[i][0], colors[i][1], colors[i][2]);
        }

        // Dynamic offsets are checked.
        var bad = d.BeginCommands();
        var p = bad.BeginRenderPass(ColorAttachment.Clear(target, 0, 0, 0, 1));
        p.SetPipeline(fillPipeline);
        Assert.Throws<ArgumentException>(() => p.SetBindGroup(0, fillGroup));
        Assert.Throws<ArgumentException>(() => p.SetBindGroup(0, fillGroup, [12]));
        p.EndPass();
        d.Submit(bad);
    }

    [Fact]
    public async Task TextureViewsSelectMipLevels()
    {
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        using var tex = d.CreateTexture(new TextureDesc(4, 4, GpuFormat.Rgba8Unorm, TextureUsage.Sampled | TextureUsage.CopyDestination) { MipLevels = 3 });
        byte[][] colors = [[255, 0, 0, 255], [0, 255, 0, 255], [0, 0, 255, 255]];
        for (uint m = 0; m < 3; m++)
        {
            var n = (int)(Math.Max(4u >> (int)m, 1) * Math.Max(4u >> (int)m, 1));
            var data = new byte[n * 4];
            for (var i = 0; i < n; i++)
            {
                colors[m].CopyTo(data, i * 4);
            }

            d.WriteTexture(tex, m, 0, data);
        }

        using var vs = d.CreateShader(new ShaderSource(ShaderStage.Vertex, Sample2D.VSMain.EntryPoint) { SpirV = Sample2D.VSMain.SpirV, Wgsl = Sample2D.VSMain.Wgsl, WgslEntryPoint = Sample2D.VSMain.WgslEntryPoint, Msl = Sample2D.VSMain.Msl, MslFunctionName = Sample2D.VSMain.MslFunctionName, Bindings = Sample2D.Bindings.All });
        using var ps = d.CreateShader(new ShaderSource(ShaderStage.Pixel, Sample2D.PSMain.EntryPoint) { SpirV = Sample2D.PSMain.SpirV, Wgsl = Sample2D.PSMain.Wgsl, WgslEntryPoint = Sample2D.PSMain.WgslEntryPoint, Msl = Sample2D.PSMain.Msl, MslFunctionName = Sample2D.PSMain.MslFunctionName, Bindings = Sample2D.Bindings.All });
        using var layout = d.CreatePipelineLayout([SampledAt0, SamplerAt1]);
        using var sampler = d.CreateSampler(SamplerDesc.Nearest);
        using var pipeline = Fullscreen(d, vs, ps, layout, GpuFormat.Rgba8Unorm);
        using var target = Target(d, 1, 1);
        for (uint m = 0; m < 3; m++)
        {
            using var group = d.CreateBindGroup(layout, 0, [BindGroupEntry.ForTexture(0, tex, TextureViewDesc.Mip(m)), BindGroupEntry.ForSampler(1, sampler)]);
            DrawFullscreen(d, target, pipeline, (0, group));
            var px = await ReadAsync(d, target);
            AssertPixel(px, 0, colors[m][0], colors[m][1], colors[m][2]);
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => d.CreateBindGroup(layout, 0, [BindGroupEntry.ForTexture(0, tex, TextureViewDesc.Mip(3)), BindGroupEntry.ForSampler(1, sampler)]));
    }

    [Fact]
    public async Task ArrayLayersAreRenderTargetsAndSampledAsAnArray()
    {
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        using var arr = d.CreateTexture(new TextureDesc(2, 2, GpuFormat.Rgba8Unorm, TextureUsage.RenderTarget | TextureUsage.Sampled) { ArrayLayers = 4 });
        var enc = d.BeginCommands();
        for (uint i = 0; i < 4; i++)
        {
            enc.BeginRenderPass(ColorAttachment.Clear(arr, i / 3f, 1 - i / 3f, 0.5f, 1) with { Layer = i }).EndPass();
        }

        d.Submit(enc);

        using var vs = d.CreateShader(new ShaderSource(ShaderStage.Vertex, Layers.VSMain.EntryPoint) { SpirV = Layers.VSMain.SpirV, Wgsl = Layers.VSMain.Wgsl, WgslEntryPoint = Layers.VSMain.WgslEntryPoint, Msl = Layers.VSMain.Msl, MslFunctionName = Layers.VSMain.MslFunctionName, Bindings = Layers.Bindings.All });
        using var ps = d.CreateShader(new ShaderSource(ShaderStage.Pixel, Layers.PSMain.EntryPoint) { SpirV = Layers.PSMain.SpirV, Wgsl = Layers.PSMain.Wgsl, WgslEntryPoint = Layers.PSMain.WgslEntryPoint, Msl = Layers.PSMain.Msl, MslFunctionName = Layers.PSMain.MslFunctionName, Bindings = Layers.Bindings.All });
        using var layout = d.CreatePipelineLayout([SampledAt0, SamplerAt1]);
        using var sampler = d.CreateSampler(SamplerDesc.Nearest);
        using var group = d.CreateBindGroup(layout, 0, [BindGroupEntry.ForTexture(0, arr), BindGroupEntry.ForSampler(1, sampler)]);
        using var pipeline = Fullscreen(d, vs, ps, layout, GpuFormat.Rgba8Unorm);
        using var target = Target(d, 4, 1);
        DrawFullscreen(d, target, pipeline, (0, group));
        var px = await ReadAsync(d, target);
        for (var i = 0; i < 4; i++)
        {
            AssertPixel(px, i, (int)MathF.Round(i / 3f * 255), (int)MathF.Round((1 - i / 3f) * 255), 128, tolerance: 1);
        }
    }

    [Fact]
    public async Task VolumeTextureIsWrittenSampledAndMipped()
    {
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        using var vol = d.CreateTexture(new TextureDesc(4, 4, GpuFormat.Rgba8Unorm, TextureUsage.Sampled | TextureUsage.CopyDestination | TextureUsage.CopySource)
        {
            Dimension = TextureDimension.D3,
            Depth = 4,
            MipLevels = GpuTexture.FullMipCount(4, 4, 4),
        });
        Assert.Equal(3u, vol.MipLevels);
        var data = new byte[4 * 4 * 4 * 4];
        for (var z = 0; z < 4; z++)
        {
            for (var i = 0; i < 16; i++)
            {
                var o = (z * 16 + i) * 4;
                data[o] = (byte)(z * 60);
                data[o + 1] = (byte)(255 - z * 60);
                data[o + 3] = 255;
            }
        }

        d.WriteTexture(vol, 0, 0, data);

        // A sub-box of slices can be rewritten on its own.
        var redo = new byte[4 * 4 * 1 * 4];
        for (var i = 0; i < 16; i++)
        {
            redo[i * 4] = 200;
            redo[i * 4 + 1] = 10;
            redo[i * 4 + 3] = 255;
        }

        d.WriteTexture(vol, new TextureRegion(0, 0, 0, 0, 3, 4, 4, 1), redo);

        var enc = d.BeginCommands();
        enc.GenerateMips(vol);
        d.Submit(enc);

        using var vs = d.CreateShader(new ShaderSource(ShaderStage.Vertex, Volume.VSMain.EntryPoint) { SpirV = Volume.VSMain.SpirV, Wgsl = Volume.VSMain.Wgsl, WgslEntryPoint = Volume.VSMain.WgslEntryPoint, Msl = Volume.VSMain.Msl, MslFunctionName = Volume.VSMain.MslFunctionName, Bindings = Volume.Bindings.All });
        using var ps = d.CreateShader(new ShaderSource(ShaderStage.Pixel, Volume.PSMain.EntryPoint) { SpirV = Volume.PSMain.SpirV, Wgsl = Volume.PSMain.Wgsl, WgslEntryPoint = Volume.PSMain.WgslEntryPoint, Msl = Volume.PSMain.Msl, MslFunctionName = Volume.PSMain.MslFunctionName, Bindings = Volume.Bindings.All });
        using var layout = d.CreatePipelineLayout([SampledAt0, SamplerAt1]);
        using var sampler = d.CreateSampler(SamplerDesc.Nearest);
        using var group = d.CreateBindGroup(layout, 0, [BindGroupEntry.ForTexture(0, vol), BindGroupEntry.ForSampler(1, sampler)]);
        using var pipeline = Fullscreen(d, vs, ps, layout, GpuFormat.Rgba8Unorm);
        using var target = Target(d, 4, 1);
        DrawFullscreen(d, target, pipeline, (0, group));
        var px = await ReadAsync(d, target);
        AssertPixel(px, 0, 0, 255, 0);
        AssertPixel(px, 1, 60, 195, 0);
        AssertPixel(px, 2, 120, 135, 0);
        AssertPixel(px, 3, 200, 10, 0);

        // The 1x1x1 top level averages all four slices: red (0 + 60 + 120 + 200) / 4, green (255 + 195 + 135 + 10) / 4.
        var top = await ReadAsync(d, vol, 2);
        Assert.InRange(top[0], 95, 97);
        Assert.InRange(top[1], 147, 150);
    }

    [Fact]
    public async Task MultisampledTargetResolves()
    {
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        if (gpu.Device.Capabilities.MaxSampleCount < 4)
        {
            return;
        }

        const uint S = 64;
        using var vs = d.CreateShader(new ShaderSource(ShaderStage.Vertex, Triangle.VSMain.EntryPoint) { SpirV = Triangle.VSMain.SpirV, Wgsl = Triangle.VSMain.Wgsl, WgslEntryPoint = Triangle.VSMain.WgslEntryPoint, Msl = Triangle.VSMain.Msl, MslFunctionName = Triangle.VSMain.MslFunctionName, Bindings = Triangle.Bindings.All });
        using var ps = d.CreateShader(new ShaderSource(ShaderStage.Pixel, Triangle.PSMain.EntryPoint) { SpirV = Triangle.PSMain.SpirV, Wgsl = Triangle.PSMain.Wgsl, WgslEntryPoint = Triangle.PSMain.WgslEntryPoint, Msl = Triangle.PSMain.Msl, MslFunctionName = Triangle.PSMain.MslFunctionName, Bindings = Triangle.Bindings.All });
        using var layout = d.CreatePipelineLayout([]);
        using var single = Fullscreen(d, vs, ps, layout, GpuFormat.Rgba8Unorm);
        using var multi = Fullscreen(d, vs, ps, layout, GpuFormat.Rgba8Unorm, 4);
        using var plain = Target(d, S, S);
        using var ms = d.CreateTexture(new TextureDesc(S, S, GpuFormat.Rgba8Unorm, TextureUsage.RenderTarget) { SampleCount = 4 });
        using var resolved = Target(d, S, S);

        var enc = d.BeginCommands();
        var pass = enc.BeginRenderPass(ColorAttachment.Clear(plain, 0.1f, 0.2f, 0.3f, 1f));
        pass.SetPipeline(single);
        pass.Draw(3);
        pass.EndPass();
        pass = enc.BeginRenderPass(new ColorAttachment
        {
            Target = ms,
            Resolve = resolved,
            Load = LoadOp.Clear,
            Store = StoreOp.Discard,
            ClearR = 0.1f,
            ClearG = 0.2f,
            ClearB = 0.3f,
            ClearA = 1f,
        });
        pass.SetPipeline(multi);
        pass.Draw(3);
        pass.EndPass();
        d.Submit(enc);

        var a = await ReadAsync(d, plain);
        var b = await ReadAsync(d, resolved);
        var edges = 0;
        var interior = 0;
        for (var i = 0; i < S * S; i++)
        {
            var worst = 0;
            for (var c = 0; c < 4; c++)
            {
                worst = Math.Max(worst, Math.Abs(a[i * 4 + c] - b[i * 4 + c]));
            }

            if (worst > 8)
            {
                edges++;
            }
            else if (worst <= 3)
            {
                interior++;
            }
        }

        // Edge pixels get partial coverage; everything else is the same image.
        Assert.InRange(edges, 20, 400);
        Assert.True(interior > S * S * 8 / 10, $"only {interior} pixels match");
        AssertPixel(b, 40 * (int)S + 32, a[(40 * (int)S + 32) * 4], a[(40 * (int)S + 32) * 4 + 1], a[(40 * (int)S + 32) * 4 + 2], tolerance: 3);
    }

    [Fact]
    public async Task ShadowComparisonSamplesDepth()
    {
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        using var depth = d.CreateTexture(new TextureDesc(4, 4, GpuFormat.Depth32Float, TextureUsage.RenderTarget | TextureUsage.Sampled));
        using var depthVs = d.CreateShader(new ShaderSource(ShaderStage.Vertex, Shadow.VSDepth.EntryPoint) { SpirV = Shadow.VSDepth.SpirV, Wgsl = Shadow.VSDepth.Wgsl, WgslEntryPoint = Shadow.VSDepth.WgslEntryPoint, Msl = Shadow.VSDepth.Msl, MslFunctionName = Shadow.VSDepth.MslFunctionName, Bindings = Shadow.Bindings.All });
        using var farVs = d.CreateShader(new ShaderSource(ShaderStage.Vertex, Shadow.VSFar.EntryPoint) { SpirV = Shadow.VSFar.SpirV, Wgsl = Shadow.VSFar.Wgsl, WgslEntryPoint = Shadow.VSFar.WgslEntryPoint, Msl = Shadow.VSFar.Msl, MslFunctionName = Shadow.VSFar.MslFunctionName, Bindings = Shadow.Bindings.All });
        using var vs = d.CreateShader(new ShaderSource(ShaderStage.Vertex, Shadow.VSMain.EntryPoint) { SpirV = Shadow.VSMain.SpirV, Wgsl = Shadow.VSMain.Wgsl, WgslEntryPoint = Shadow.VSMain.WgslEntryPoint, Msl = Shadow.VSMain.Msl, MslFunctionName = Shadow.VSMain.MslFunctionName, Bindings = Shadow.Bindings.All });
        using var ps = d.CreateShader(new ShaderSource(ShaderStage.Pixel, Shadow.PSMain.EntryPoint) { SpirV = Shadow.PSMain.SpirV, Wgsl = Shadow.PSMain.Wgsl, WgslEntryPoint = Shadow.PSMain.WgslEntryPoint, Msl = Shadow.PSMain.Msl, MslFunctionName = Shadow.PSMain.MslFunctionName, Bindings = Shadow.Bindings.All });
        using var emptyLayout = d.CreatePipelineLayout([]);
        using var layout = d.CreatePipelineLayout([SampledAt0, SamplerAt1]);
        using var cmp = d.CreateSampler(SamplerDesc.Nearest with { Compare = CompareFunction.LessEqual });
        using var lookup = Fullscreen(d, vs, ps, layout, GpuFormat.Rgba8Unorm);
        using var target = Target(d, 2, 1);
        using var group = d.CreateBindGroup(layout, 0, [BindGroupEntry.ForTexture(0, depth), BindGroupEntry.ForSampler(1, cmp)]);

        async Task Case(GpuShader depthShader, DepthStencilState state, bool clamp, float clear, bool expectLow, bool expectHigh)
        {
            using var writer = d.CreateGraphicsPipeline(new GraphicsPipelineDesc { Layout = emptyLayout, Vertex = depthShader, DepthStencil = state, DepthClamp = clamp });
            var enc = d.BeginCommands();
            var pass = enc.BeginRenderPass([], new DepthAttachment { Target = depth, Load = LoadOp.Clear, Store = StoreOp.Store, ClearDepth = clear });
            pass.SetPipeline(writer);
            pass.Draw(3);
            pass.EndPass();
            d.Submit(enc);
            DrawFullscreen(d, target, lookup, (0, group));
            var px = await ReadAsync(d, target);

            // The lookup compares 0.4 and 0.6 against the stored depth with "reference <= stored".
            Assert.Equal(expectLow ? 255 : 0, px[0]);
            Assert.Equal(expectHigh ? 255 : 0, px[4]);
        }

        // Stored depth 0.5: 0.4 <= 0.5 passes, 0.6 <= 0.5 fails.
        await Case(depthVs, new DepthStencilState(GpuFormat.Depth32Float), false, 1f, true, false);

        // A constant bias of 2^22 steps moves 0.5 by 0.25 (float depth steps at 0.5 are 2^-24), so 0.6 now passes.
        await Case(depthVs, new DepthStencilState(GpuFormat.Depth32Float) { DepthBias = 1 << 22 }, false, 1f, true, true);

        if (d.Capabilities.DepthClamp)
        {
            // Depth 1.5 is clipped away without clamping, so the cleared 0 stays; with clamping 1.0 is written.
            var always = new DepthStencilState(GpuFormat.Depth32Float) { DepthCompare = CompareFunction.Always };
            await Case(farVs, always, false, 0f, false, false);
            await Case(farVs, always, true, 0f, true, true);
        }
    }

    [Fact]
    public async Task ComputeWritesStorageTextureThenDrawReadsIt()
    {
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        using var tex = d.CreateTexture(new TextureDesc(16, 16, GpuFormat.Rgba8Unorm, TextureUsage.Storage | TextureUsage.Sampled));
        using var cs = d.CreateShader(new ShaderSource(ShaderStage.Compute, StorageImage.CSMain.EntryPoint) { SpirV = StorageImage.CSMain.SpirV, Wgsl = StorageImage.CSMain.Wgsl, WgslEntryPoint = StorageImage.CSMain.WgslEntryPoint, Msl = StorageImage.CSMain.Msl, MslFunctionName = StorageImage.CSMain.MslFunctionName, Bindings = StorageImage.Bindings.All, ComputeGroupX = StorageImage.CSMain.ThreadGroupSizeX, ComputeGroupY = StorageImage.CSMain.ThreadGroupSizeY, ComputeGroupZ = StorageImage.CSMain.ThreadGroupSizeZ });
        using var vs = d.CreateShader(new ShaderSource(ShaderStage.Vertex, StorageImage.VSMain.EntryPoint) { SpirV = StorageImage.VSMain.SpirV, Wgsl = StorageImage.VSMain.Wgsl, WgslEntryPoint = StorageImage.VSMain.WgslEntryPoint, Msl = StorageImage.VSMain.Msl, MslFunctionName = StorageImage.VSMain.MslFunctionName, Bindings = StorageImage.Bindings.All });
        using var ps = d.CreateShader(new ShaderSource(ShaderStage.Pixel, StorageImage.PSMain.EntryPoint) { SpirV = StorageImage.PSMain.SpirV, Wgsl = StorageImage.PSMain.Wgsl, WgslEntryPoint = StorageImage.PSMain.WgslEntryPoint, Msl = StorageImage.PSMain.Msl, MslFunctionName = StorageImage.PSMain.MslFunctionName, Bindings = StorageImage.Bindings.All });
        using var computeLayout = d.CreatePipelineLayout([new ResourceBinding(0, 0, 1, ResourceKind.StorageTexture, ShaderStage.Compute)]);
        using var drawLayout = d.CreatePipelineLayout([new ResourceBinding(1, 0, 1, ResourceKind.SampledTexture, ShaderStage.Pixel)]);
        using var computeGroup = d.CreateBindGroup(computeLayout, 0, [BindGroupEntry.ForTexture(0, tex)]);
        using var drawGroup = d.CreateBindGroup(drawLayout, 1, [BindGroupEntry.ForTexture(0, tex)]);
        using var computePipeline = d.CreateComputePipeline(new ComputePipelineDesc { Layout = computeLayout, Compute = cs });
        using var pipeline = Fullscreen(d, vs, ps, drawLayout, GpuFormat.Rgba8Unorm);
        using var target = Target(d, 16, 16);

        var enc = d.BeginCommands();
        var cp = enc.BeginComputePass();
        cp.SetPipeline(computePipeline);
        cp.SetBindGroup(0, computeGroup);
        cp.Dispatch(2, 2);
        cp.EndPass();
        var pass = enc.BeginRenderPass(ColorAttachment.Clear(target, 0, 0, 0, 1));
        pass.SetPipeline(pipeline);
        pass.SetBindGroup(1, drawGroup);
        pass.Draw(3);
        pass.EndPass();
        d.Submit(enc);

        var px = await ReadAsync(d, target);
        for (var y = 0; y < 16; y++)
        {
            for (var x = 0; x < 16; x++)
            {
                AssertPixel(px, y * 16 + x, (int)MathF.Round(x / 15f * 255), (int)MathF.Round(y / 15f * 255), 128, tolerance: 1);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct Pos
    {
        public float X, Y;
    }

    [Fact]
    public async Task IndirectDrawsAndDispatchesMatchDirectOnes()
    {
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        Pos[] verts =
        [
            new() { X = -0.9f, Y = -0.9f }, new() { X = -0.1f, Y = -0.9f }, new() { X = -0.5f, Y = 0.9f },
            new() { X = 0.1f, Y = -0.9f }, new() { X = 0.9f, Y = -0.9f }, new() { X = 0.5f, Y = 0.9f },
        ];
        using var vb = d.CreateBuffer(new BufferDesc(48, BufferUsage.Vertex | BufferUsage.CopyDestination));
        d.WriteBuffer(vb, 0, MemoryMarshal.AsBytes<Pos>(verts));
        using var ib = d.CreateBuffer(new BufferDesc(12, BufferUsage.Index | BufferUsage.CopyDestination));
        d.WriteBuffer(ib, 0, MemoryMarshal.AsBytes<ushort>([0, 1, 2, 3, 4, 5]));
        using var args = d.CreateBuffer(new BufferDesc(2 * DrawIndirectArgs.Size, BufferUsage.Indirect | BufferUsage.CopyDestination));
        d.WriteBuffer(args, 0, MemoryMarshal.AsBytes<DrawIndirectArgs>([new(3, 1, 0, 0), new(3, 1, 3, 0)]));
        using var iargs = d.CreateBuffer(new BufferDesc(2 * DrawIndexedIndirectArgs.Size, BufferUsage.Indirect | BufferUsage.CopyDestination));
        d.WriteBuffer(iargs, 0, MemoryMarshal.AsBytes<DrawIndexedIndirectArgs>([new(3, 1, 0, 0, 0), new(3, 1, 3, 0, 0)]));

        using var vs = d.CreateShader(new ShaderSource(ShaderStage.Vertex, Indirect.VSMain.EntryPoint) { SpirV = Indirect.VSMain.SpirV, Wgsl = Indirect.VSMain.Wgsl, WgslEntryPoint = Indirect.VSMain.WgslEntryPoint, Msl = Indirect.VSMain.Msl, MslFunctionName = Indirect.VSMain.MslFunctionName, Bindings = Indirect.Bindings.All });
        using var ps = d.CreateShader(new ShaderSource(ShaderStage.Pixel, Indirect.PSMain.EntryPoint) { SpirV = Indirect.PSMain.SpirV, Wgsl = Indirect.PSMain.Wgsl, WgslEntryPoint = Indirect.PSMain.WgslEntryPoint, Msl = Indirect.PSMain.Msl, MslFunctionName = Indirect.PSMain.MslFunctionName, Bindings = Indirect.Bindings.All });
        using var layout = d.CreatePipelineLayout([]);
        using var pipeline = d.CreateGraphicsPipeline(new GraphicsPipelineDesc
        {
            Layout = layout,
            Vertex = vs,
            Pixel = ps,
            VertexBuffers = [new VertexBufferLayout { Stride = 8, Attributes = new[] { new VertexElement(0, VertexFormat.Float2, 0) } }],
            ColorTargets = [new ColorTargetState(GpuFormat.Rgba8Unorm)],
        });
        using var direct = Target(d, 32, 32);
        using var indirect = Target(d, 32, 32);
        using var indexed = Target(d, 32, 32);
        using var single = Target(d, 32, 32);

        var enc = d.BeginCommands();
        var pass = enc.BeginRenderPass(ColorAttachment.Clear(direct, 0, 0, 0, 1));
        pass.SetPipeline(pipeline);
        pass.SetVertexBuffer(0, vb);
        pass.Draw(3);
        pass.Draw(3, 1, 3);
        pass.EndPass();

        pass = enc.BeginRenderPass(ColorAttachment.Clear(indirect, 0, 0, 0, 1));
        pass.SetPipeline(pipeline);
        pass.SetVertexBuffer(0, vb);
        pass.DrawIndirect(args, 0, 2);
        pass.EndPass();

        pass = enc.BeginRenderPass(ColorAttachment.Clear(indexed, 0, 0, 0, 1));
        pass.SetPipeline(pipeline);
        pass.SetVertexBuffer(0, vb);
        pass.SetIndexBuffer(ib, IndexFormat.Index16);
        pass.DrawIndexedIndirect(iargs, 0, 2);
        pass.EndPass();

        pass = enc.BeginRenderPass(ColorAttachment.Clear(single, 0, 0, 0, 1));
        pass.SetPipeline(pipeline);
        pass.SetVertexBuffer(0, vb);
        pass.DrawIndirect(args, DrawIndirectArgs.Size);
        pass.EndPass();
        d.Submit(enc);

        var reference = await ReadAsync(d, direct);
        var red = Enumerable.Range(0, 32 * 32).Count(i => reference[i * 4] == 255);
        Assert.True(red > 200, $"expected two red triangles, found {red} red pixels");
        Assert.Equal(reference, await ReadAsync(d, indirect));
        Assert.Equal(reference, await ReadAsync(d, indexed));

        // Only the second record: the right-hand triangle, so the left half stays black.
        var one = await ReadAsync(d, single);
        Assert.Equal(0, one[(16 * 32 + 8) * 4]);
        Assert.Equal(255, one[(16 * 32 + 24) * 4]);

        // A compute pass writes the dispatch size; a second pass uses it.
        using var argBuf = d.CreateBuffer(new BufferDesc(DispatchIndirectArgs.Size, BufferUsage.Storage | BufferUsage.Indirect | BufferUsage.CopyDestination));
        using var result = d.CreateBuffer(new BufferDesc(64, BufferUsage.Storage | BufferUsage.CopySource | BufferUsage.CopyDestination));
        d.WriteBuffer(argBuf, 0, new byte[DispatchIndirectArgs.Size]);
        d.WriteBuffer(result, 0, new byte[64]);
        using var csArgs = d.CreateShader(new ShaderSource(ShaderStage.Compute, Indirect.CSArgs.EntryPoint) { SpirV = Indirect.CSArgs.SpirV, Wgsl = Indirect.CSArgs.Wgsl, WgslEntryPoint = Indirect.CSArgs.WgslEntryPoint, Msl = Indirect.CSArgs.Msl, MslFunctionName = Indirect.CSArgs.MslFunctionName, Bindings = Indirect.Bindings.All, ComputeGroupX = Indirect.CSArgs.ThreadGroupSizeX, ComputeGroupY = Indirect.CSArgs.ThreadGroupSizeY, ComputeGroupZ = Indirect.CSArgs.ThreadGroupSizeZ });
        using var csFill = d.CreateShader(new ShaderSource(ShaderStage.Compute, Indirect.CSFill.EntryPoint) { SpirV = Indirect.CSFill.SpirV, Wgsl = Indirect.CSFill.Wgsl, WgslEntryPoint = Indirect.CSFill.WgslEntryPoint, Msl = Indirect.CSFill.Msl, MslFunctionName = Indirect.CSFill.MslFunctionName, Bindings = Indirect.Bindings.All, ComputeGroupX = Indirect.CSFill.ThreadGroupSizeX, ComputeGroupY = Indirect.CSFill.ThreadGroupSizeY, ComputeGroupZ = Indirect.CSFill.ThreadGroupSizeZ });
        using var argsLayout = d.CreatePipelineLayout([new ResourceBinding(0, 0, 1, ResourceKind.StorageBuffer, ShaderStage.Compute)]);
        using var fillLayout = d.CreatePipelineLayout([new ResourceBinding(0, 1, 1, ResourceKind.StorageBuffer, ShaderStage.Compute)]);
        using var argsGroup = d.CreateBindGroup(argsLayout, 0, [BindGroupEntry.ForBuffer(0, argBuf)]);
        using var fillGroup = d.CreateBindGroup(fillLayout, 0, [BindGroupEntry.ForBuffer(1, result)]);
        using var argsPipe = d.CreateComputePipeline(new ComputePipelineDesc { Layout = argsLayout, Compute = csArgs });
        using var fillPipe = d.CreateComputePipeline(new ComputePipelineDesc { Layout = fillLayout, Compute = csFill });
        enc = d.BeginCommands();
        var cp = enc.BeginComputePass();
        cp.SetPipeline(argsPipe);
        cp.SetBindGroup(0, argsGroup);
        cp.Dispatch(1);
        cp.SetPipeline(fillPipe);
        cp.SetBindGroup(0, fillGroup);
        cp.DispatchIndirect(argBuf, 0);
        cp.EndPass();
        d.Submit(enc);
        var values = new uint[16];
        await TestRead.Buffer(d, result, values);
        for (uint i = 0; i < 16; i++)
        {
            Assert.Equal(i < 12 ? i + 1 : 0u, values[i]);
        }

        Assert.Throws<ArgumentException>(() =>
        {
            var e = d.BeginCommands();
            var p = e.BeginRenderPass(ColorAttachment.Clear(direct, 0, 0, 0, 1));
            try
            {
                p.SetPipeline(pipeline);
                p.DrawIndirect(vb, 0);
            }
            finally
            {
                p.EndPass();
                d.Submit(e);
            }
        });
    }

    [Fact]
    public async Task PushConstantsReachTheShader()
    {
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        using var vs = d.CreateShader(new ShaderSource(ShaderStage.Vertex, PushFill.VSMain.EntryPoint) { SpirV = PushFill.VSMain.SpirV, Wgsl = PushFill.VSMain.Wgsl, WgslEntryPoint = PushFill.VSMain.WgslEntryPoint, Msl = PushFill.VSMain.Msl, MslFunctionName = PushFill.VSMain.MslFunctionName, Bindings = PushFill.Bindings.All });
        using var ps = d.CreateShader(new ShaderSource(ShaderStage.Pixel, PushFill.PSMain.EntryPoint) { SpirV = PushFill.PSMain.SpirV, Wgsl = PushFill.PSMain.Wgsl, WgslEntryPoint = PushFill.PSMain.WgslEntryPoint, Msl = PushFill.PSMain.Msl, MslFunctionName = PushFill.PSMain.MslFunctionName, Bindings = PushFill.Bindings.All, MslPushConstantIndex = (int)PushFill.Bindings.PushConstants.MslIndex, WgslPushGroup = (int)PushFill.Bindings.PushConstants.WgslGroup, WgslPushBinding = (int)PushFill.Bindings.PushConstants.WgslBinding });
        Assert.Equal(PushFill.Bindings.PushConstants.Size, (uint)Marshal.SizeOf<PushFill.PushConstants>());
        using var layout = d.CreatePipelineLayout([], new PushConstantRange(PushFill.Bindings.PushConstants.Size, ShaderStage.Pixel));
        using var pipeline = Fullscreen(d, vs, ps, layout, GpuFormat.Rgba8Unorm);
        using var target = Target(d, 2, 1);

        var enc = d.BeginCommands();
        var pass = enc.BeginRenderPass(ColorAttachment.Clear(target, 0, 0, 0, 1));
        pass.SetPipeline(pipeline);
        foreach (var (x, color) in new[] { (0, new[] { 1f, 0f, 0.5f, 1f }), (1, new[] { 0f, 1f, 0f, 1f }) })
        {
            // Each draw covers one pixel; the constants change between them.
            pass.SetViewport(x, 0, 1, 1);
            pass.SetScissor(x, 0, 1, 1);
            pass.SetPushConstants(0, MemoryMarshal.AsBytes<float>(color));
            pass.Draw(3);
        }

        pass.EndPass();
        d.Submit(enc);
        var px = await ReadAsync(d, target);
        AssertPixel(px, 0, 255, 0, 128, tolerance: 1);
        AssertPixel(px, 1, 0, 255, 0);

        // Pushed data must fit the declared range.
        enc = d.BeginCommands();
        var p = enc.BeginRenderPass(ColorAttachment.Clear(target, 0, 0, 0, 1));
        p.SetPipeline(pipeline);
        Assert.Throws<ArgumentOutOfRangeException>(() => p.SetPushConstants(8, new byte[16]));
        Assert.Throws<ArgumentOutOfRangeException>(() => p.SetPushConstants(1, new byte[4]));
        p.EndPass();
        d.Submit(enc);
        Assert.Throws<ArgumentException>(() => d.CreatePipelineLayout([], new PushConstantRange(GpuDevice.MaxPushConstantBytes + 4, ShaderStage.Pixel)));
    }

#if !WEB_BACKEND
    [Fact]
    public async Task ResourceArrayIsIndexedNonUniformly()
    {
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        Assert.True(d.Capabilities.NonUniformArrayIndexing);
        byte[][] colors = [[255, 0, 0, 255], [0, 255, 0, 255], [0, 0, 255, 255], [255, 255, 255, 255]];
        var textures = new List<GpuTexture>();
        foreach (var c in colors)
        {
            var t = d.CreateTexture(new TextureDesc(1, 1, GpuFormat.Rgba8Unorm, TextureUsage.Sampled | TextureUsage.CopyDestination));
            d.WriteTexture(t, 0, 0, c);
            textures.Add(t);
        }

        using var vs = d.CreateShader(new ShaderSource(ShaderStage.Vertex, TextureArray.VSMain.EntryPoint) { SpirV = TextureArray.VSMain.SpirV, Msl = TextureArray.VSMain.Msl, MslFunctionName = TextureArray.VSMain.MslFunctionName, Bindings = TextureArray.Bindings.All });
        using var ps = d.CreateShader(new ShaderSource(ShaderStage.Pixel, TextureArray.PSMain.EntryPoint) { SpirV = TextureArray.PSMain.SpirV, Msl = TextureArray.PSMain.Msl, MslFunctionName = TextureArray.PSMain.MslFunctionName, Bindings = TextureArray.Bindings.All });
        using var layout = d.CreatePipelineLayout([new ResourceBinding(0, 0, 4, ResourceKind.SampledTexture, ShaderStage.Pixel), SamplerAt1]);
        using var sampler = d.CreateSampler(SamplerDesc.Nearest);
        using var group = d.CreateBindGroup(layout, 0, [BindGroupEntry.ForTextures(0, textures), BindGroupEntry.ForSampler(1, sampler)]);
        using var pipeline = Fullscreen(d, vs, ps, layout, GpuFormat.Rgba8Unorm);
        using var target = Target(d, 4, 1);
        DrawFullscreen(d, target, pipeline, (0, group));
        var px = await ReadAsync(d, target);
        for (var i = 0; i < 4; i++)
        {
            AssertPixel(px, i, colors[i][0], colors[i][1], colors[i][2]);
        }

        Assert.Throws<ArgumentException>(() => d.CreateBindGroup(layout, 0, [BindGroupEntry.ForTextures(0, textures.Take(3).ToList()), BindGroupEntry.ForSampler(1, sampler)]));
        foreach (var t in textures)
        {
            t.Dispose();
        }
    }
#endif

    [StructLayout(LayoutKind.Sequential)]
    struct Instance
    {
        public float X, Y;
        public byte R, G, B, A;
    }

    [Fact]
    public async Task TenThousandInstancesDrawInOneCall()
    {
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        var inst = new Instance[10_000];
        for (var y = 0; y < 100; y++)
        {
            for (var x = 0; x < 100; x++)
            {
                inst[y * 100 + x] = new Instance { X = x, Y = y, R = (byte)(x * 255 / 99), G = (byte)(y * 255 / 99), B = (byte)(((x + y) & 1) * 255), A = 255 };
            }
        }

        using var corners = d.CreateBuffer(new BufferDesc(32, BufferUsage.Vertex | BufferUsage.CopyDestination));
        d.WriteBuffer(corners, 0, MemoryMarshal.AsBytes<float>([0, 0, 1, 0, 0, 1, 1, 1]));
        using var instances = d.CreateBuffer(new BufferDesc((ulong)(inst.Length * 12), BufferUsage.Vertex | BufferUsage.CopyDestination));
        d.WriteBuffer(instances, 0, MemoryMarshal.AsBytes<Instance>(inst));

        using var vs = d.CreateShader(new ShaderSource(ShaderStage.Vertex, Instanced.VSMain.EntryPoint) { SpirV = Instanced.VSMain.SpirV, Wgsl = Instanced.VSMain.Wgsl, WgslEntryPoint = Instanced.VSMain.WgslEntryPoint, Msl = Instanced.VSMain.Msl, MslFunctionName = Instanced.VSMain.MslFunctionName, Bindings = Instanced.Bindings.All });
        using var ps = d.CreateShader(new ShaderSource(ShaderStage.Pixel, Instanced.PSMain.EntryPoint) { SpirV = Instanced.PSMain.SpirV, Wgsl = Instanced.PSMain.Wgsl, WgslEntryPoint = Instanced.PSMain.WgslEntryPoint, Msl = Instanced.PSMain.Msl, MslFunctionName = Instanced.PSMain.MslFunctionName, Bindings = Instanced.Bindings.All });
        using var layout = d.CreatePipelineLayout([]);
        using var pipeline = d.CreateGraphicsPipeline(new GraphicsPipelineDesc
        {
            Layout = layout,
            Vertex = vs,
            Pixel = ps,
            Topology = PrimitiveTopology.TriangleStrip,
            VertexBuffers =
            [
                new VertexBufferLayout { Stride = 8, Attributes = new[] { new VertexElement(0, VertexFormat.Float2, 0) } },
                new VertexBufferLayout
                {
                    Stride = 12,
                    StepMode = VertexStepMode.Instance,
                    Attributes = new[] { new VertexElement(1, VertexFormat.Float2, 0), new VertexElement(2, VertexFormat.UNorm8x4, 8) },
                },
            ],
            ColorTargets = [new ColorTargetState(GpuFormat.Rgba8Unorm)],
        });
        using var target = Target(d, 100, 100);
        var enc = d.BeginCommands();
        var pass = enc.BeginRenderPass(ColorAttachment.Clear(target, 0, 0, 0, 1));
        pass.SetPipeline(pipeline);
        pass.SetVertexBuffer(0, corners);
        pass.SetVertexBuffer(1, instances);
        pass.Draw(4, 10_000);
        pass.EndPass();
        d.Submit(enc);

        var px = await ReadAsync(d, target);
        var wrong = 0;
        for (var row = 0; row < 100; row++)
        {
            for (var col = 0; col < 100; col++)
            {
                var y = 99 - row;
                var o = (row * 100 + col) * 4;
                if (px[o] != col * 255 / 99 || px[o + 1] != y * 255 / 99 || px[o + 2] != ((col + y) & 1) * 255 || px[o + 3] != 255)
                {
                    wrong++;
                }
            }
        }

        Assert.Equal(0, wrong);
    }

    [Fact]
    public async Task PartialTextureWriteTouchesOnlyItsRectangle()
    {
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        using var tex = d.CreateTexture(new TextureDesc(8, 8, GpuFormat.Rgba8Unorm, TextureUsage.CopyDestination | TextureUsage.CopySource));
        d.WriteTexture(tex, 0, 0, new byte[8 * 8 * 4]);
        var patch = new byte[3 * 2 * 4];
        for (var i = 0; i < patch.Length; i++)
        {
            patch[i] = (byte)(100 + i);
        }

        d.WriteTexture(tex, TextureRegion.Rect(0, 0, 2, 3, 3, 2), patch);
        var px = await ReadAsync(d, tex);
        for (var y = 0; y < 8; y++)
        {
            for (var x = 0; x < 8; x++)
            {
                var inside = x is >= 2 and < 5 && y is >= 3 and < 5;
                for (var c = 0; c < 4; c++)
                {
                    var expected = inside ? patch[((y - 3) * 3 + (x - 2)) * 4 + c] : 0;
                    Assert.Equal(expected, px[(y * 8 + x) * 4 + c]);
                }
            }
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => d.WriteTexture(tex, TextureRegion.Rect(0, 0, 6, 0, 3, 1), new byte[12]));
        Assert.Throws<ArgumentException>(() => d.WriteTexture(tex, TextureRegion.Rect(0, 0, 0, 0, 2, 2), new byte[12]));
    }

    [Fact]
    public async Task WholeTexturesAreCopiedWithTheirMips()
    {
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        var usage = TextureUsage.CopySource | TextureUsage.CopyDestination;
        using var a = d.CreateTexture(new TextureDesc(4, 4, GpuFormat.Rgba8Unorm, usage) { MipLevels = 3, ArrayLayers = 2 });
        using var b = d.CreateTexture(new TextureDesc(4, 4, GpuFormat.Rgba8Unorm, usage) { MipLevels = 3, ArrayLayers = 2 });
        for (uint layer = 0; layer < 2; layer++)
        {
            for (uint mip = 0; mip < 3; mip++)
            {
                var n = (int)(Math.Max(4u >> (int)mip, 1) * Math.Max(4u >> (int)mip, 1)) * 4;
                d.WriteTexture(a, mip, layer, Enumerable.Repeat((byte)(layer * 40 + mip * 10 + 5), n).ToArray());
            }
        }

        var enc = d.BeginCommands();
        enc.CopyTexture(a, b);
        d.Submit(enc);
        Assert.All(await ReadAsync(d, b, 2, 1), v => Assert.Equal(65, v));
        Assert.All(await ReadAsync(d, b, 0, 0), v => Assert.Equal(5, v));
        using var other = d.CreateTexture(new TextureDesc(8, 4, GpuFormat.Rgba8Unorm, usage) { MipLevels = 3, ArrayLayers = 2 });
        Assert.Throws<ArgumentException>(() => d.BeginCommands().CopyTexture(a, other));
    }

    [Fact]
    public void PassTimingWritesStartAndEndTimes()
    {
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        if (!d.Capabilities.Timestamps)
        {
            return;
        }

        using var set = d.CreateTimestampSet(8);
        using var target = Target(d, 64, 64);
        var enc = d.BeginCommands();
        enc.BeginPassTiming(new PassTiming(set, 0, 3));
        enc.BeginRenderPass(ColorAttachment.Clear(target, 0, 0, 0, 1)).EndPass();
        enc.BeginRenderPass(ColorAttachment.Clear(target, 1, 0, 0, 1)).EndPass();
        enc.BeginComputePass().EndPass();
        Assert.Equal(3u, enc.EndPassTiming());
        enc.BeginRenderPass(ColorAttachment.Clear(target, 1, 1, 0, 1)).EndPass();
        d.Submit(enc);
        d.WaitIdle();

        var t = new ulong[6];
        Assert.True(d.TryReadTimestamps(set, 0, t));
        for (var i = 0; i < 3; i++)
        {
            Assert.True(t[i * 2 + 1] >= t[i * 2], $"pass {i} ended before it began");
        }

        Assert.True(t[2] >= t[1] && t[4] >= t[3], "passes run in order");
    }

    [Fact]
    public void CubeAndVolumeValidation()
    {
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        Assert.Throws<ArgumentException>(() => d.CreateTexture(new TextureDesc(8, 8, GpuFormat.Rgba8Unorm, TextureUsage.Sampled) { Dimension = TextureDimension.Cube, ArrayLayers = 5 }));
        Assert.Throws<ArgumentException>(() => d.CreateTexture(new TextureDesc(8, 4, GpuFormat.Rgba8Unorm, TextureUsage.Sampled) { Dimension = TextureDimension.Cube, ArrayLayers = 6 }));
        Assert.Throws<ArgumentException>(() => d.CreateTexture(new TextureDesc(8, 8, GpuFormat.Rgba8Unorm, TextureUsage.RenderTarget) { Dimension = TextureDimension.D3, Depth = 4 }));
        Assert.Throws<ArgumentException>(() => d.CreateTexture(new TextureDesc(8, 8, GpuFormat.Rgba8Unorm, TextureUsage.Sampled) { Depth = 4 }));
        Assert.Throws<ArgumentException>(() => d.CreateTexture(new TextureDesc(8, 8, GpuFormat.Rgba8Unorm, TextureUsage.Sampled) { MipLevels = 5 }));
        var enc = d.BeginCommands();
        d.Submit(enc);
    }
}
