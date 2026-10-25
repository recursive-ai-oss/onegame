using System.Runtime.InteropServices;
using OneGame.Gpu;
using OneGame.Gpu.Graph;
using OneGame.Render3D.Shaders;

namespace OneGame.Render3D;

/// <summary>
/// An environment prepared for lighting: a GGX-prefiltered specular cube (roughness grows with the mip level, mip 0 is the sharp
/// environment) and a cosine-convolved diffuse cube. Create one with <see cref="Renderer3D.CreateEnvironment"/>; it is filtered on the
/// GPU, on the render graph, the first time it is used.
/// </summary>
public sealed class EnvironmentMap : IDisposable
{
    internal EnvironmentMap(GpuTexture equirect, GpuTexture source, GpuTexture specular, GpuTexture irradiance, int size, bool followsQuality)
    {
        FollowsQuality = followsQuality;
        Equirect = equirect;
        Source = source;
        Specular = specular;
        Irradiance = irradiance;
        Size = size;
        Pending = true;
    }

    /// <summary>Face size in texels of the specular cube's top mip.</summary>
    public int Size { get; internal set; }

    /// <summary>True when the size was left to the renderer: the cube is rebuilt at <see cref="QualitySettings.EnvironmentSize"/> whenever the scene's quality level changes it.</summary>
    public bool FollowsQuality { get; }

    /// <summary>Number of mip levels of the specular cube (roughness = mip / (count - 1)).</summary>
    public int SpecularMipCount => (int)Specular.MipLevels;

    /// <summary>The prefiltered specular cube (<c>Rgba16Float</c>, six faces, full mip chain).</summary>
    public GpuTexture Specular { get; internal set; }

    /// <summary>The diffuse cube: mean radiance over the cosine-weighted hemisphere, that is irradiance divided by pi (<c>Rgba16Float</c>).</summary>
    public GpuTexture Irradiance { get; }

    /// <summary>True once the filtering passes have been recorded for submission.</summary>
    public bool IsBuilt => !Pending;

    internal GpuTexture? Equirect { get; set; }

    internal GpuTexture Source { get; set; }

    internal bool Pending { get; set; }

    /// <inheritdoc />
    public void Dispose()
    {
        Equirect?.Dispose();
        Source.Dispose();
        Specular.Dispose();
        Irradiance.Dispose();
    }
}

public sealed partial class Renderer3D
{
    private const int LutSize = 128;
    private const int IrradianceSize = 32;
    private const int PrefilterSamples = 512;
    private const int IrradianceSamples = 512;
    private const int LutSamples = 512;

    private GpuPipelineLayout _iblLayout = null!;
    private GpuPipelineLayout _lutLayout = null!;
    private GpuComputePipeline _equirectPipeline = null!;
    private GpuComputePipeline _prefilterPipeline = null!;
    private GpuComputePipeline _irradiancePipeline = null!;
    private GpuComputePipeline _lutPipeline = null!;
    private GpuSampler _equirectSampler = null!;
    private GpuBuffer _lutParams = null!;
    private GpuBindGroup _lutGroup = null!;
    private bool _lutBuilt;
    private EnvironmentMap? _defaultEnvironment;

    /// <summary>The split-sum environment BRDF table (<c>Rg16Float</c>, n.v across, roughness down); filled on first use.</summary>
    public GpuTexture BrdfLut { get; private set; } = null!;

    private void InitIbl()
    {
        var device = Device;
        const ShaderStage cs = ShaderStage.Compute;
        ReadOnlySpan<ResourceBinding> ibl =
        [
            new(0, 0, 1, ResourceKind.ConstantBuffer, cs),
            new(0, 1, 1, ResourceKind.SampledTexture, cs),
            new(0, 2, 1, ResourceKind.Sampler, cs),
            new(0, 3, 1, ResourceKind.StorageTexture, cs),
        ];
        ReadOnlySpan<ResourceBinding> lut =
        [
            new(0, 0, 1, ResourceKind.ConstantBuffer, cs),
            new(0, 1, 1, ResourceKind.StorageTexture, cs),
        ];
        _iblLayout = Own(device.CreatePipelineLayout(ibl));
        _lutLayout = Own(device.CreatePipelineLayout(lut));
        _equirectPipeline = Own(device.CreateComputePipeline(new ComputePipelineDesc
        {
            Layout = _iblLayout,
            Compute = Own(device.CreateShader(new ShaderSource(ShaderStage.Compute, IblEquirect.CSMain.EntryPoint) { SpirV = IblEquirect.CSMain.SpirV, Msl = IblEquirect.CSMain.Msl, MslFunctionName = IblEquirect.CSMain.MslFunctionName, Bindings = IblEquirect.Bindings.All, ComputeGroupX = IblEquirect.CSMain.ThreadGroupSizeX, ComputeGroupY = IblEquirect.CSMain.ThreadGroupSizeY, ComputeGroupZ = IblEquirect.CSMain.ThreadGroupSizeZ })),
            Label = "env equirect",
        }));
        _prefilterPipeline = Own(device.CreateComputePipeline(new ComputePipelineDesc
        {
            Layout = _iblLayout,
            Compute = Own(device.CreateShader(new ShaderSource(ShaderStage.Compute, IblPrefilter.CSMain.EntryPoint) { SpirV = IblPrefilter.CSMain.SpirV, Msl = IblPrefilter.CSMain.Msl, MslFunctionName = IblPrefilter.CSMain.MslFunctionName, Bindings = IblPrefilter.Bindings.All, ComputeGroupX = IblPrefilter.CSMain.ThreadGroupSizeX, ComputeGroupY = IblPrefilter.CSMain.ThreadGroupSizeY, ComputeGroupZ = IblPrefilter.CSMain.ThreadGroupSizeZ })),
            Label = "env prefilter",
        }));
        _irradiancePipeline = Own(device.CreateComputePipeline(new ComputePipelineDesc
        {
            Layout = _iblLayout,
            Compute = Own(device.CreateShader(new ShaderSource(ShaderStage.Compute, IblIrradiance.CSMain.EntryPoint) { SpirV = IblIrradiance.CSMain.SpirV, Msl = IblIrradiance.CSMain.Msl, MslFunctionName = IblIrradiance.CSMain.MslFunctionName, Bindings = IblIrradiance.Bindings.All, ComputeGroupX = IblIrradiance.CSMain.ThreadGroupSizeX, ComputeGroupY = IblIrradiance.CSMain.ThreadGroupSizeY, ComputeGroupZ = IblIrradiance.CSMain.ThreadGroupSizeZ })),
            Label = "env irradiance",
        }));
        _lutPipeline = Own(device.CreateComputePipeline(new ComputePipelineDesc
        {
            Layout = _lutLayout,
            Compute = Own(device.CreateShader(new ShaderSource(ShaderStage.Compute, EnvBrdf.CSMain.EntryPoint) { SpirV = EnvBrdf.CSMain.SpirV, Msl = EnvBrdf.CSMain.Msl, MslFunctionName = EnvBrdf.CSMain.MslFunctionName, Bindings = EnvBrdf.Bindings.All, ComputeGroupX = EnvBrdf.CSMain.ThreadGroupSizeX, ComputeGroupY = EnvBrdf.CSMain.ThreadGroupSizeY, ComputeGroupZ = EnvBrdf.CSMain.ThreadGroupSizeZ })),
            Label = "brdf lut",
        }));
        _equirectSampler = Own(device.CreateSampler(SamplerDesc.Linear with { AddressU = AddressMode.Repeat }));
        BrdfLut = Own(device.CreateTexture(new TextureDesc(LutSize, LutSize, GpuFormat.Rg16Float, TextureUsage.Sampled | TextureUsage.Storage | TextureUsage.CopySource) { Label = "brdf lut" }));
        _lutParams = Own(device.CreateBuffer(new BufferDesc(256, BufferUsage.Uniform | BufferUsage.CopyDestination) { Label = "brdf lut params" }));
        device.WriteBuffer(_lutParams, 0, MemoryMarshal.AsBytes(new ReadOnlySpan<Vector4>([new Vector4(LutSize, LutSamples, 0, 0)])));
        _lutGroup = Own(device.CreateBindGroup(_lutLayout, 0,
        [
            BindGroupEntry.ForBuffer(0, _lutParams, 0, 16),
            BindGroupEntry.ForTexture(1, BrdfLut),
        ]));
    }

    private void DisposeIbl()
    {
        _defaultEnvironment?.Dispose();
        _defaultEnvironment = null;
    }

    /// <summary>
    /// Makes an environment from an equirectangular panorama (see <see cref="HdrImage"/> for the orientation). The cubes are allocated
    /// now and filled by compute passes the first time a frame uses the environment, or by <see cref="PrepareEnvironment"/>.
    /// </summary>
    /// <param name="image">The panorama, linear radiance.</param>
    /// <param name="size">Face size of the specular cube, a power of two from 16 to 1024; 0 (the default) follows the scene's quality level.</param>
    /// <returns>The environment. Dispose it before the renderer.</returns>
    public EnvironmentMap CreateEnvironment(HdrImage image, int size = 0)
    {
        ArgumentNullException.ThrowIfNull(image);
        var follows = size == 0;
        if (follows)
        {
            size = QualitySettings.For(_quality).EnvironmentSize;
        }

        if (size is < 16 or > 1024 || (size & (size - 1)) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "The size must be a power of two from 16 to 1024.");
        }

        var max = (int)Device.Capabilities.MaxTextureSize;
        if (image.Width > max || image.Height > max)
        {
            throw new ArgumentException($"The panorama is larger than the device's {max} texel texture limit.", nameof(image));
        }

        // Half-float range tops out at 65504: clamp so a very bright sun disc cannot become infinity and smear through the filtering.
        var halves = new Half[image.Width * image.Height * 4];
        for (var i = 0; i < image.Width * image.Height; i++)
        {
            halves[i * 4] = (Half)Math.Clamp(image.Rgb[i * 3], 0f, 60000f);
            halves[(i * 4) + 1] = (Half)Math.Clamp(image.Rgb[(i * 3) + 1], 0f, 60000f);
            halves[(i * 4) + 2] = (Half)Math.Clamp(image.Rgb[(i * 3) + 2], 0f, 60000f);
            halves[(i * 4) + 3] = (Half)1f;
        }

        var equirect = Device.CreateTexture(new TextureDesc((uint)image.Width, (uint)image.Height, GpuFormat.Rgba16Float, TextureUsage.Sampled | TextureUsage.CopyDestination) { Label = "panorama" });
        Device.WriteTexture(equirect, 0, 0, MemoryMarshal.AsBytes(halves.AsSpan()));
        var mips = GpuTexture.FullMipCount((uint)size, (uint)size);
        TextureDesc Cube(int s, uint m, string label) => new((uint)s, (uint)s, GpuFormat.Rgba16Float, TextureUsage.Sampled | TextureUsage.Storage | TextureUsage.CopySource | TextureUsage.CopyDestination)
        {
            Dimension = TextureDimension.Cube,
            ArrayLayers = 6,
            MipLevels = m,
            Label = label,
        };

        return new EnvironmentMap(equirect, Device.CreateTexture(Cube(size, mips, "environment source")), Device.CreateTexture(Cube(size, mips, "environment specular")), Device.CreateTexture(Cube(IrradianceSize, 1, "environment irradiance")), size, follows);
    }

    /// <summary>Runs the filtering of an environment now, on its own graph, and waits for it. Frames do this by themselves; tests and tools use it to inspect the cubes.</summary>
    /// <param name="environment">The environment to filter.</param>
    public void PrepareEnvironment(EnvironmentMap environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        using var graph = new RenderGraph(Device, new RenderGraphOptions { Timing = false });
        graph.BeginFrame();
        EnvironmentWork(graph, environment);
        graph.Execute();
        Device.WaitIdle();
        DisposeDeferred();
    }

    private EnvironmentMap DefaultEnvironment()
    {
        // A soft, slightly warm studio sky: bright overhead, darker floor, no sun, so a model without any environment still reads clearly.
        _defaultEnvironment ??= CreateEnvironment(HdrImage.CreateSky(128, 64, new Vector3(0.9f, 0.95f, 1.1f), new Vector3(0.8f, 0.8f, 0.8f), new Vector3(0.25f, 0.23f, 0.2f)), 64);
        return _defaultEnvironment;
    }

    // Adds the passes that fill the BRDF table and the environment cubes, once each. The lambdas live in their own methods: a closure over a
    // parameter is allocated on entry to the method that declares it, which would cost every frame.
    private void EnvironmentWork(RenderGraph graph, EnvironmentMap env)
    {
        if (!_lutBuilt)
        {
            _lutBuilt = true;
            AddLutPass(graph);
        }

        if (env.FollowsQuality && env.Size != QualitySettings.For(_quality).EnvironmentSize)
        {
            ResizeEnvironment(env, QualitySettings.For(_quality).EnvironmentSize);
        }

        if (env.Pending)
        {
            env.Pending = false;
            AddEnvironmentPasses(graph, env);
        }
    }

    // Rebuilds the filtered cubes at another size from the kept panorama. The old cubes go with the frame's deferred disposal, so a
    // frame still in flight is not hurt.
    private void ResizeEnvironment(EnvironmentMap env, int size)
    {
        var mips = GpuTexture.FullMipCount((uint)size, (uint)size);
        TextureDesc Cube(string label) => new((uint)size, (uint)size, GpuFormat.Rgba16Float, TextureUsage.Sampled | TextureUsage.Storage | TextureUsage.CopySource | TextureUsage.CopyDestination)
        {
            Dimension = TextureDimension.Cube,
            ArrayLayers = 6,
            MipLevels = mips,
            Label = label,
        };

        _deferred.Add(env.Source);
        _deferred.Add(env.Specular);
        env.Source = Device.CreateTexture(Cube("environment source"));
        env.Specular = Device.CreateTexture(Cube("environment specular"));
        env.Size = size;
        env.Pending = true;
    }

    private void AddLutPass(RenderGraph graph)
    {
        var lut = graph.ImportTexture("brdf_lut", BrdfLut);
        graph.AddPass("brdf_lut", b =>
        {
            b.Compute();
            b.WriteStorage(lut);
        }, c =>
        {
            c.Compute.SetPipeline(_lutPipeline);
            c.Compute.SetBindGroup(0, _lutGroup);
            c.Compute.Dispatch(LutSize / 8, LutSize / 8, 1);
        });
    }

    private void AddEnvironmentPasses(RenderGraph graph, EnvironmentMap env)
    {
        var device = Device;
        var mips = env.SpecularMipCount;
        var slots = mips + 2;
        var parameters = device.CreateBuffer(new BufferDesc((ulong)slots * 256, BufferUsage.Uniform | BufferUsage.CopyDestination) { Label = "env params" });
        var values = new Vector4[slots * 16];
        values[0] = new Vector4(env.Size, 0, 0, 0);
        for (var m = 1; m < mips; m++)
        {
            var size = Math.Max(env.Size >> m, 1);
            values[m * 16] = new Vector4(size, m / (float)(mips - 1), PrefilterSamples, env.Size);
        }

        values[mips * 16] = new Vector4(IrradianceSize, IrradianceSamples, env.Size, 0);
        device.WriteBuffer(parameters, 0, MemoryMarshal.AsBytes(values.AsSpan()));
        _deferred.Add(parameters);

        GpuBindGroup Group(int slot, GpuTexture? sampled, GpuSampler sampler, GpuTexture storage, uint mip)
        {
            var g = device.CreateBindGroup(_iblLayout, 0,
            [
                BindGroupEntry.ForBuffer(0, parameters, (ulong)slot * 256, 16),
                BindGroupEntry.ForTexture(1, sampled ?? _white),
                BindGroupEntry.ForSampler(2, sampler),
                BindGroupEntry.ForTexture(3, storage, new TextureViewDesc { BaseMip = mip, MipCount = 1, BaseLayer = 0, LayerCount = 6, Kind = TextureViewKind.D2Array }),
            ]);
            _deferred.Add(g);
            return g;
        }

        var equirect = graph.ImportTexture("env_panorama", env.Equirect!);
        var source = graph.ImportTexture("env_source", env.Source);
        var specular = graph.ImportTexture("env_specular", env.Specular);
        var irradiance = graph.ImportTexture("env_irradiance", env.Irradiance);

        var equirectGroup = Group(0, env.Equirect, _equirectSampler, env.Source, 0);
        graph.AddPass("env_panorama_to_cube", b =>
        {
            b.Compute();
            b.Read(equirect);
            b.WriteStorage(source);
        }, c =>
        {
            c.Compute.SetPipeline(_equirectPipeline);
            c.Compute.SetBindGroup(0, equirectGroup);
            c.Compute.Dispatch((uint)(env.Size + 7) / 8, (uint)(env.Size + 7) / 8, 6);
        });
        graph.AddPass("env_source_mips", b => b.ReadWrite(source), c => c.Encoder.GenerateMips(c.Texture(source)));
        graph.AddPass("env_copy_mip0", b =>
        {
            b.CopySource(source);
            b.CopyDestination(specular);
        }, c => c.Encoder.CopyTexture(c.Texture(source), c.Texture(specular)));

        for (var m = 1; m < mips; m++)
        {
            var size = Math.Max(env.Size >> m, 1);
            var group = Group(m, env.Source, _envSampler, env.Specular, (uint)m);
            graph.AddPass("env_prefilter_" + m, b =>
            {
                b.Compute();
                b.Read(source);
                b.WriteStorage(specular);
            }, c =>
            {
                c.Compute.SetPipeline(_prefilterPipeline);
                c.Compute.SetBindGroup(0, group);
                c.Compute.Dispatch((uint)(size + 7) / 8, (uint)(size + 7) / 8, 6);
            });
        }

        var irradianceGroup = Group(mips, env.Source, _envSampler, env.Irradiance, 0);
        graph.AddPass("env_irradiance", b =>
        {
            b.Compute();
            b.Read(source);
            b.WriteStorage(irradiance);
        }, c =>
        {
            c.Compute.SetPipeline(_irradiancePipeline);
            c.Compute.SetBindGroup(0, irradianceGroup);
            c.Compute.Dispatch(IrradianceSize / 8, IrradianceSize / 8, 6);
        });
    }
}
