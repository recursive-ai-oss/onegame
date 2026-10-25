using System.Numerics;
using System.Runtime.InteropServices;
using OneGame.Gpu;
using OneGame.UI;

namespace OneGame.Render2D;

/// <summary>Per-frame settings.</summary>
public struct FrameOptions
{
    /// <summary>Colour to clear the target to, sRGB straight alpha, or null to keep what the target holds.</summary>
    public Color? Clear { get; set; }
}

/// <summary>What a frame cost, counted when it was recorded.</summary>
/// <param name="DrawCalls">Draw calls recorded.</param>
/// <param name="Passes">Render passes begun (each offscreen group or post-processing step is one more).</param>
/// <param name="Sprites">Sprites drawn.</param>
/// <param name="SpritesCulled">Sprites skipped because they were off screen.</param>
/// <param name="QuadInstances">Quad instances uploaded (sprites, tile chunks, composites).</param>
/// <param name="UiInstances">UI primitives uploaded.</param>
/// <param name="TileChunkQuads">Tilemap chunk quads drawn.</param>
/// <param name="ChunkUploads">Tilemap chunks uploaded to the GPU.</param>
/// <param name="UploadedBytes">Bytes of instance and clip data written.</param>
public readonly record struct FrameStats(
    int DrawCalls,
    int Passes,
    int Sprites,
    int SpritesCulled,
    int QuadInstances,
    int UiInstances,
    int TileChunkQuads,
    int ChunkUploads,
    long UploadedBytes);

/// <summary>
/// The 2D renderer: sprites, tilemaps, parallax layers, custom materials, post-processing and the UI display list, drawn through the GPU layer.
/// A frame is built with <see cref="BeginFrame"/>, any number of draw calls, and <see cref="EndFrame"/>, which uploads everything once and records the
/// passes into an encoder the caller then submits. Colours are sRGB in the API; blending is linear and premultiplied. See the renderer's interface
/// document for the full contract.
/// </summary>
public sealed partial class Renderer2D : IDisposable
{
    private const int MaxClips = 4096;

    private struct DrawOp
    {
        public int Target;
        public GpuGraphicsPipeline Pipeline;
        public GpuBindGroup Texture;
        public GpuBindGroup? Extra;
        public bool IsUi;
        public int First;
        public int Count;
        public int Sx, Sy, Sw, Sh;
    }

    private sealed class TargetState : IDisposable
    {
        public required GpuTexture Texture;
        public required GpuBuffer Constants;
        public required GpuBindGroup Frame;
        public GpuBindGroup? Sample;
        public bool InUse;
        public bool Pooled;

        public void Dispose()
        {
            Sample?.Dispose();
            Frame.Dispose();
            Constants.Dispose();
            if (Pooled)
            {
                Texture.Dispose();
            }
        }
    }

    private struct FrameTarget
    {
        public TargetState State;
        public bool HasClear;
        public Color Clear;
        public bool Begun;
    }

    private readonly GpuDevice _device;
    private readonly GpuFormat _format;
    private readonly IShaderSource _shaders;
    private readonly GpuPipelineLayout _layoutPlain;
    private readonly GpuPipelineLayout _layoutParams;
    private readonly GpuPipelineLayout _layoutTile;
    private readonly GpuShader _quadVertex;
    private readonly GpuShader _spritePixel;
    private readonly GpuShader _tileVertex;
    private readonly GpuShader _tileIdsPixel;
    private readonly GpuShader _tileFlatPixel;
    private readonly GpuShader _uiVertex;
    private readonly GpuShader _uiPixel;
    private readonly GpuGraphicsPipeline _spritePipeline;
    private readonly GpuGraphicsPipeline _tileIdsPipeline;
    private readonly GpuGraphicsPipeline _tileFlatPipeline;
    private readonly GpuGraphicsPipeline _uiPipeline;
    private readonly GpuGraphicsPipeline _blurPipeline;
    private readonly GpuGraphicsPipeline _gradePipeline;
    private readonly GpuShader _blurPixel;
    private readonly GpuShader _gradePixel;
    private readonly GpuBuffer _clipBuffer;
    private readonly GpuSampler _clampLinear;
    private readonly Dictionary<GpuTexture, TargetState> _finalTargets = [];
    private readonly List<TargetState> _temps = [];
    private readonly List<TileGpu> _tileStates = [];
    private readonly List<DrawOp> _ops = [];
    private readonly List<FrameTarget> _targets = [];
    private readonly List<Material> _usedMaterials = [];
    private readonly Texture2D _white;
    private QuadInstance[] _quads = new QuadInstance[1024];
    private int _quadCount;
    private UiInstance[] _uiItems = new UiInstance[1024];
    private int _uiCount;
    private readonly Vector4[] _clipData = new Vector4[MaxClips * 4];
    private int _clipCount;
    private GpuBuffer? _quadBuffer;
    private ulong _quadCapacity;
    private GpuBuffer? _uiBuffer;
    private ulong _uiCapacity;
    private Matrix3x2[] _layerViews = new Matrix3x2[8];
    private int _frame;
    private bool _inFrame;
    private int _width;
    private int _height;
    private int _current;
    private int _spriteCount;
    private int _culledCount;
    private int _tileQuadCount;
    private int _uploadCount;
    private bool _disposed;

    private Renderer2D(GpuDevice device, GpuFormat format, IShaderSource shaders)
    {
        _device = device;
        _format = format;
        _shaders = shaders;

        _layoutPlain = device.CreatePipelineLayout(Layouts.Concat(Layouts.Frame, Layouts.Texture));
        _layoutParams = device.CreatePipelineLayout(Layouts.Concat(Layouts.Frame, Layouts.Texture, Layouts.Params));
        _layoutTile = device.CreatePipelineLayout(Layouts.Concat(Layouts.Frame, Layouts.Texture, Layouts.TilePools));

        _quadVertex = Shader(ShaderNames.Quad, ShaderNames.Vertex);
        _spritePixel = Shader(ShaderNames.Quad, ShaderNames.Sprite);
        _blurPixel = Shader(ShaderNames.Quad, ShaderNames.Blur);
        _gradePixel = Shader(ShaderNames.Quad, ShaderNames.Grade);
        _tileVertex = Shader(ShaderNames.Tile, ShaderNames.Vertex);
        _tileIdsPixel = Shader(ShaderNames.Tile, ShaderNames.TileIds);
        _tileFlatPixel = Shader(ShaderNames.Tile, ShaderNames.TileFlat);
        _uiVertex = Shader(ShaderNames.Ui, ShaderNames.Vertex);
        _uiPixel = Shader(ShaderNames.Ui, ShaderNames.UiPixel);

        _spritePipeline = Pipeline(_layoutPlain, _quadVertex, _spritePixel, Layouts.QuadVertex(), "sprite");
        _blurPipeline = Pipeline(_layoutParams, _quadVertex, _blurPixel, Layouts.QuadVertex(), "blur");
        _gradePipeline = Pipeline(_layoutParams, _quadVertex, _gradePixel, Layouts.QuadVertex(), "grade");
        _tileIdsPipeline = Pipeline(_layoutTile, _tileVertex, _tileIdsPixel, Layouts.QuadVertex(true), "tile ids");
        _tileFlatPipeline = Pipeline(_layoutTile, _tileVertex, _tileFlatPixel, Layouts.QuadVertex(true), "tile flat");
        _uiPipeline = Pipeline(_layoutPlain, _uiVertex, _uiPixel, Layouts.UiVertex(), "ui");

        _clipBuffer = device.CreateBuffer(new BufferDesc((ulong)_clipData.Length * 16, BufferUsage.Storage | BufferUsage.CopyDestination) { Label = "2d clips" });
        _clampLinear = device.CreateSampler(SamplerDesc.Linear with { AddressU = AddressMode.ClampToEdge, AddressV = AddressMode.ClampToEdge });

        _white = CreateTexture(1, 1, [255, 255, 255, 255], TextureFilter.Nearest);
        Ui = new UiCanvas(this);
    }

    /// <summary>
    /// Makes a renderer that draws into targets of <paramref name="targetFormat"/>.
    /// </summary>
    /// <param name="device">The GPU device. It must outlive the renderer.</param>
    /// <param name="targetFormat">An sRGB colour format (<c>Rgba8UnormSrgb</c> or <c>Bgra8UnormSrgb</c>) or <c>Rgba16Float</c>: the shaders output linear light and rely on the target to encode it.</param>
    /// <param name="shaders">The compiled built-in shaders.</param>
    /// <returns>The renderer.</returns>
    public static Renderer2D Create(GpuDevice device, GpuFormat targetFormat, IShaderSource shaders)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(shaders);
        if (targetFormat is not (GpuFormat.Rgba8UnormSrgb or GpuFormat.Bgra8UnormSrgb or GpuFormat.Rgba16Float))
        {
            throw new ArgumentException($"{targetFormat} is not a format the 2D renderer can draw into: use Rgba8UnormSrgb, Bgra8UnormSrgb or Rgba16Float so linear blending is encoded correctly", nameof(targetFormat));
        }

        return new Renderer2D(device, targetFormat, shaders);
    }

    /// <summary>The GPU device.</summary>
    public GpuDevice Device => _device;

    /// <summary>The format of the targets this renderer draws into.</summary>
    public GpuFormat TargetFormat => _format;

    /// <summary>The UI drawing surface for the current frame.</summary>
    public UiCanvas Ui { get; }

    /// <summary>Stats of the last frame ended.</summary>
    public FrameStats LastFrame { get; private set; }

    private GpuShader Shader(string program, string entry)
    {
        var code = _shaders.Load(program, entry);
        return _device.CreateShader(code.ToSource());
    }

    private GpuGraphicsPipeline Pipeline(GpuPipelineLayout layout, GpuShader vs, GpuShader ps, VertexBufferLayout vertex, string label) =>
        _device.CreateGraphicsPipeline(new GraphicsPipelineDesc
        {
            Layout = layout,
            Vertex = vs,
            Pixel = ps,
            VertexBuffers = [vertex],
            Topology = PrimitiveTopology.TriangleList,
            Cull = CullMode.None,
            ColorTargets = [new ColorTargetState(_format) { Blend = BlendState.Premultiplied }],
            Label = "2d " + label,
        });

    // ---- resources ----

    /// <summary>Creates a texture from straight-alpha sRGB pixels (R, G, B, A bytes, top row first). The renderer premultiplies in linear light.</summary>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="rgba">Exactly <c>width * height * 4</c> bytes.</param>
    /// <param name="filter">How the texture is filtered when scaled.</param>
    /// <returns>The texture.</returns>
    public Texture2D CreateTexture(int width, int height, ReadOnlySpan<byte> rgba, TextureFilter filter = TextureFilter.Linear)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        if (rgba.Length != checked(width * height * 4))
        {
            throw new ArgumentException($"a {width}x{height} texture needs {width * height * 4} bytes, got {rgba.Length}", nameof(rgba));
        }

        var stored = new byte[rgba.Length];
        Colors.PremultiplySrgb(rgba, stored);
        var tex = CreateRaw(width, height, GpuFormat.Rgba8UnormSrgb, filter);
        _device.WriteTexture(tex.Texture, 0, 0, stored);
        return tex;
    }

    internal Texture2D CreateRaw(int width, int height, GpuFormat format, TextureFilter filter)
    {
        var gpu = _device.CreateTexture(new TextureDesc((uint)width, (uint)height, format, TextureUsage.Sampled | TextureUsage.CopyDestination));
        var f = filter == TextureFilter.Linear ? FilterMode.Linear : FilterMode.Nearest;
        var sampler = _device.CreateSampler(new SamplerDesc { MagFilter = f, MinFilter = f, MipFilter = FilterMode.Nearest, AddressU = AddressMode.ClampToEdge, AddressV = AddressMode.ClampToEdge, MaxAnisotropy = 1 });
        return new Texture2D(gpu, sampler, format);
    }

    /// <summary>Creates a tile set from a grid of tiles.</summary>
    /// <param name="tileWidth">Width of one tile in texels.</param>
    /// <param name="tileHeight">Height of one tile in texels.</param>
    /// <param name="columns">Tiles per row.</param>
    /// <param name="rows">Rows of tiles.</param>
    /// <param name="rgba">Straight-alpha sRGB pixels of the whole grid, <c>columns * tileWidth</c> by <c>rows * tileHeight</c>.</param>
    /// <param name="filter">Filtering; use <see cref="TextureFilter.Nearest"/> for pixel art.</param>
    /// <returns>The tile set.</returns>
    public TileSet CreateTileSet(int tileWidth, int tileHeight, int columns, int rows, ReadOnlySpan<byte> rgba, TextureFilter filter = TextureFilter.Nearest)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(tileWidth, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(tileHeight, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(columns, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(rows, 1);
        var w = checked(tileWidth * columns);
        var h = checked(tileHeight * rows);
        var tex = CreateTexture(w, h, rgba, filter);
        var average = new uint[columns * rows];
        var stored = new byte[rgba.Length];
        Colors.PremultiplySrgb(rgba, stored);
        for (var t = 0; t < average.Length; t++)
        {
            var x0 = (t % columns) * tileWidth;
            var y0 = (t / columns) * tileHeight;
            double r = 0, g = 0, b = 0, a = 0;
            for (var y = 0; y < tileHeight; y++)
            {
                for (var x = 0; x < tileWidth; x++)
                {
                    var i = (((y0 + y) * w) + x0 + x) * 4;
                    r += Colors.ToLinear(stored[i]);
                    g += Colors.ToLinear(stored[i + 1]);
                    b += Colors.ToLinear(stored[i + 2]);
                    a += stored[i + 3];
                }
            }

            var n = tileWidth * tileHeight;
            average[t] = Colors.ToSrgb((float)(r / n)) | ((uint)Colors.ToSrgb((float)(g / n)) << 8) | ((uint)Colors.ToSrgb((float)(b / n)) << 16)
                | ((uint)(byte)Math.Round(a / n) << 24);
        }

        return new TileSet(tex, tileWidth, tileHeight, columns, rows, average);
    }

    /// <summary>Creates a material from a custom pixel shader that draws sprites. See the renderer's interface document for what the shader receives.</summary>
    /// <param name="pixelShader">The compiled pixel stage.</param>
    /// <param name="label">A name for debugging.</param>
    /// <returns>The material.</returns>
    public Material CreateMaterial(ShaderCode pixelShader, string? label = null)
    {
        if (pixelShader.Stage != ShaderStage.Pixel)
        {
            throw new ArgumentException("a material needs a pixel shader", nameof(pixelShader));
        }

        using var ps = _device.CreateShader(pixelShader.ToSource());
        var pipeline = Pipeline(_layoutParams, _quadVertex, ps, Layouts.QuadVertex(), label ?? "material");
        return NewMaterial(pipeline, ownsPipeline: true);
    }

    /// <summary>
    /// Replaces the pixel shader of a material made by <see cref="CreateMaterial"/> without touching its parameters, so a shader edited
    /// while the game runs shows from the next frame on. The new shader must keep the interface the material was made with (same
    /// resources and vertex inputs); it is the caller's job to check that, for example with <c>InterfaceCompatibility.Compare</c>.
    /// </summary>
    /// <param name="material">A material from <see cref="CreateMaterial"/>.</param>
    /// <param name="pixelShader">The new compiled pixel stage.</param>
    /// <exception cref="ArgumentException">The shader is not a pixel shader, or the material is one of the renderer's own.</exception>
    public void ReplaceMaterialShader(Material material, ShaderCode pixelShader)
    {
        ArgumentNullException.ThrowIfNull(material);
        if (pixelShader.Stage != ShaderStage.Pixel)
        {
            throw new ArgumentException("a material needs a pixel shader", nameof(pixelShader));
        }

        if (!material.OwnsPipeline)
        {
            throw new ArgumentException("the renderer's own materials cannot be replaced", nameof(material));
        }

        using var ps = _device.CreateShader(pixelShader.ToSource());
        var pipeline = Pipeline(_layoutParams, _quadVertex, ps, Layouts.QuadVertex(), "material");
        var old = material.SwapPipeline(pipeline);

        // Frames in flight may still reference the old pipeline; a reload is a development action, so waiting for the device is fine.
        _device.WaitIdle();
        old.Dispose();
    }

    private Material NewMaterial(GpuGraphicsPipeline pipeline, bool ownsPipeline)
    {
        var buffer = _device.CreateBuffer(new BufferDesc(128, BufferUsage.Uniform | BufferUsage.CopyDestination) { Label = "2d material" });
        var group = _device.CreateBindGroup(_layoutParams, 2, [BindGroupEntry.ForBuffer(0, buffer)]);
        return new Material(buffer, group, pipeline, ownsPipeline);
    }

    /// <summary>Makes an empty post-processing chain. Add effects, then pass it to <see cref="DrawScene"/>.</summary>
    /// <returns>The chain.</returns>
    public PostChain CreatePostChain() => new(this);

    internal Material CreateBlurMaterial() => NewMaterial(_blurPipeline, ownsPipeline: false);

    internal Material CreateGradeMaterial() => NewMaterial(_gradePipeline, ownsPipeline: false);

    /// <summary>Forgets what the renderer keeps for a target texture (its constants and bind group). Call it when a target is destroyed, for example on swapchain resize.</summary>
    /// <param name="texture">The target.</param>
    public void ForgetTarget(GpuTexture texture)
    {
        if (_finalTargets.Remove(texture, out var ts))
        {
            ts.Dispose();
        }
    }

    // ---- frame ----

    /// <summary>Starts a frame that draws into <paramref name="target"/>. The target needs <c>RenderTarget</c> usage and the renderer's format.</summary>
    /// <param name="target">The texture to draw into: an offscreen texture or a swapchain image.</param>
    /// <param name="options">Per-frame settings.</param>
    public void BeginFrame(GpuTexture target, in FrameOptions options = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_inFrame)
        {
            throw new InvalidOperationException("BeginFrame was called twice without EndFrame");
        }

        if (target.Format != _format)
        {
            throw new ArgumentException($"the target is {target.Format} but this renderer draws into {_format}", nameof(target));
        }

        if ((target.Usage & TextureUsage.RenderTarget) == 0)
        {
            throw new ArgumentException("the target texture was not created with RenderTarget usage", nameof(target));
        }

        _inFrame = true;
        _frame++;
        _width = (int)target.Width;
        _height = (int)target.Height;
        _ops.Clear();
        _targets.Clear();
        _usedMaterials.Clear();
        _quadCount = 0;
        _uiCount = 0;
        _clipCount = 0;
        _spriteCount = 0;
        _culledCount = 0;
        _tileQuadCount = 0;
        _uploadCount = 0;
        _targets.Add(new FrameTarget
        {
            State = FinalState(target),
            HasClear = options.Clear.HasValue,
            Clear = options.Clear.GetValueOrDefault(),
        });
        _current = 0;
        Ui.Reset(_width, _height);
    }

    private TargetState FinalState(GpuTexture target)
    {
        if (!_finalTargets.TryGetValue(target, out var ts))
        {
            ts = NewTargetState(target, pooled: false);
            _finalTargets.Add(target, ts);
        }

        return ts;
    }

    private TargetState NewTargetState(GpuTexture texture, bool pooled)
    {
        var constants = _device.CreateBuffer(new BufferDesc(16, BufferUsage.Uniform | BufferUsage.CopyDestination) { Label = "2d frame" });
        _device.WriteBuffer(constants, 0, MemoryMarshal.AsBytes<float>([texture.Width, texture.Height, 0f, 0f]));
        var group = _device.CreateBindGroup(_layoutPlain, 0, [BindGroupEntry.ForBuffer(0, constants), BindGroupEntry.ForBuffer(1, _clipBuffer)]);
        return new TargetState { Texture = texture, Constants = constants, Frame = group, Pooled = pooled };
    }

    private int AcquireTemp()
    {
        TargetState? found = null;
        foreach (var t in _temps)
        {
            if (!t.InUse && t.Texture.Width == _width && t.Texture.Height == _height)
            {
                found = t;
                break;
            }
        }

        if (found is null)
        {
            var tex = _device.CreateTexture(new TextureDesc((uint)_width, (uint)_height, _format, TextureUsage.RenderTarget | TextureUsage.Sampled) { Label = "2d offscreen" });
            found = NewTargetState(tex, pooled: true);
            found.Sample = _device.CreateBindGroup(_layoutPlain, 1, [BindGroupEntry.ForTexture(0, tex), BindGroupEntry.ForSampler(1, _clampLinear)]);
            _temps.Add(found);
        }

        found.InUse = true;
        _targets.Add(new FrameTarget { State = found, HasClear = true, Clear = new Color(0, 0, 0, 0) });
        return _targets.Count - 1;
    }

    private void ReleaseTemp(int target) => _targets[target].State.InUse = false;

    private GpuBindGroup TextureGroup(Texture2D t) =>
        t.Group ??= _device.CreateBindGroup(_layoutPlain, 1, [BindGroupEntry.ForTexture(0, t.Texture), BindGroupEntry.ForSampler(1, t.Sampler)]);

    private void AddQuadOp(GpuGraphicsPipeline pipeline, GpuBindGroup texture, GpuBindGroup? extra, int first, int count, int target, int sx, int sy, int sw, int sh)
    {
        if (count == 0 || sw <= 0 || sh <= 0)
        {
            return;
        }

        if (_ops.Count > 0)
        {
            ref var last = ref CollectionsMarshal.AsSpan(_ops)[^1];
            if (!last.IsUi && last.Target == target && last.Pipeline == pipeline && last.Texture == texture && last.Extra == extra
                && last.Sx == sx && last.Sy == sy && last.Sw == sw && last.Sh == sh && last.First + last.Count == first)
            {
                last.Count += count;
                return;
            }
        }

        _ops.Add(new DrawOp { Target = target, Pipeline = pipeline, Texture = texture, Extra = extra, First = first, Count = count, Sx = sx, Sy = sy, Sw = sw, Sh = sh });
    }

    private int AppendQuad(in QuadInstance q)
    {
        if (_quadCount == _quads.Length)
        {
            Array.Resize(ref _quads, _quadCount * 2);
        }

        _quads[_quadCount] = q;
        return _quadCount++;
    }

    private void UseMaterial(Material m)
    {
        if (m.LastFrame != _frame)
        {
            m.LastFrame = _frame;
            _usedMaterials.Add(m);
        }
    }

    // ---- scene ----

    /// <summary>
    /// Draws a scene's layers back to front through <paramref name="camera"/>. With a post chain the scene goes into an offscreen image first, the
    /// chain runs over it, and the result lands on the current target; anything drawn after (usually the UI) is not post-processed.
    /// </summary>
    /// <param name="scene">The layers, sprites and tilemaps.</param>
    /// <param name="camera">The view.</param>
    /// <param name="post">Effects to apply to the finished scene, or null.</param>
    public void DrawScene(Scene2D scene, in Camera2D camera, PostChain? post = null)
    {
        RequireFrame();
        ArgumentNullException.ThrowIfNull(scene);
        var chain = post is { PassCount: > 0 } ? post : null;
        var finalTarget = _current;
        var sceneTarget = chain is null ? finalTarget : AcquireTemp();
        if (chain is not null)
        {
            // The scene starts from the frame's clear colour so the effects see the background too.
            var ft = _targets[sceneTarget];
            var frameClear = _targets[finalTarget];
            ft.Clear = frameClear.HasClear ? frameClear.Clear : new Color(0, 0, 0, 0);
            _targets[sceneTarget] = ft;
        }

        var size = new Vector2(_width, _height);
        var layers = scene.Layers;
        if (_layerViews.Length < layers.Count)
        {
            Array.Resize(ref _layerViews, layers.Count);
        }

        for (var i = 0; i < layers.Count; i++)
        {
            _layerViews[i] = camera.View(size, layers[i].Parallax);
        }

        var cam = camera;
        scene.Sort();
        for (var n = 0; n < scene.EntryCount; n++)
        {
            var layerIndex = scene.LayerOf(n);
            if (!layers[layerIndex].Visible)
            {
                continue;
            }

            if (scene.IsTileMap(n))
            {
                DrawTileMap(scene.MapAt(scene.IndexOf(n)), _layerViews[layerIndex], sceneTarget);
            }
            else
            {
                DrawSprite(in scene.SpriteAt(scene.IndexOf(n)), _layerViews[layerIndex], in cam, sceneTarget);
            }
        }

        if (chain is not null)
        {
            RunPost(chain, sceneTarget, finalTarget);
        }
    }

    private void DrawSprite(in Sprite s, in Matrix3x2 view, in Camera2D camera, int target)
    {
        var tex = s.Texture;
        if (tex is null)
        {
            return;
        }

        var src = s.Source.Width <= 0 || s.Source.Height <= 0 ? new PixelRect(0, 0, tex.Width, tex.Height) : s.Source;
        var size = new Vector2(src.Width * s.Scale.X, src.Height * s.Scale.Y);
        var (sin, cos) = MathF.SinCos(s.Rotation);
        var ax = new Vector2(cos, sin) * size.X;
        var ay = new Vector2(-sin, cos) * size.Y;
        var originWorld = s.Position - (ax * s.Pivot.X) - (ay * s.Pivot.Y);
        var o = Vector2.Transform(originWorld, view);
        var axs = Vector2.TransformNormal(ax, view);
        var ays = Vector2.TransformNormal(ay, view);
        if (camera.PixelSnap)
        {
            var pivot = Vector2.Transform(s.Position, view);
            o += new Vector2(MathF.Round(pivot.X), MathF.Round(pivot.Y)) - pivot;
        }

        var minX = MathF.Min(MathF.Min(o.X, o.X + axs.X), MathF.Min(o.X + ays.X, o.X + axs.X + ays.X));
        var maxX = MathF.Max(MathF.Max(o.X, o.X + axs.X), MathF.Max(o.X + ays.X, o.X + axs.X + ays.X));
        var minY = MathF.Min(MathF.Min(o.Y, o.Y + axs.Y), MathF.Min(o.Y + ays.Y, o.Y + axs.Y + ays.Y));
        var maxY = MathF.Max(MathF.Max(o.Y, o.Y + axs.Y), MathF.Max(o.Y + ays.Y, o.Y + axs.Y + ays.Y));
        if (maxX < 0 || maxY < 0 || minX > _width || minY > _height)
        {
            _culledCount++;
            return;
        }

        float u = src.X / (float)tex.Width, v = src.Y / (float)tex.Height, uw = src.Width / (float)tex.Width, vh = src.Height / (float)tex.Height;
        if ((s.Flip & SpriteFlip.Horizontal) != 0)
        {
            u += uw;
            uw = -uw;
        }

        if ((s.Flip & SpriteFlip.Vertical) != 0)
        {
            v += vh;
            vh = -vh;
        }

        var first = AppendQuad(new QuadInstance { Origin = o, AxisX = axs, AxisY = ays, Uv = new Vector4(u, v, uw, vh), Color = Colors.Pack(s.Tint) });
        _spriteCount++;
        GpuGraphicsPipeline pipeline = _spritePipeline;
        GpuBindGroup? extra = null;
        if (s.Material is { } m)
        {
            UseMaterial(m);
            pipeline = m.Pipeline;
            extra = m.Group;
        }

        AddQuadOp(pipeline, TextureGroup(tex), extra, first, 1, target, 0, 0, _width, _height);
    }

    // ---- post ----

    private void RunPost(PostChain chain, int sceneTarget, int finalTarget)
    {
        // Each pass reads the previous image and writes the next; the last writes the real target. Two offscreen images alternate:
        // a source is released right after the pass that reads it, so the next pass can pick the same texture up as its destination.
        var read = sceneTarget;
        var passes = chain.Passes;
        for (var i = 0; i < passes.Count; i++)
        {
            var last = i == passes.Count - 1;
            var dest = last ? finalTarget : AcquireTemp();
            var pass = passes[i];
            pass.Prepare(_width, _height);
            UseMaterial(pass.Material);
            var first = AppendQuad(FullScreen());
            AddQuadOp(pass.Material.Pipeline, _targets[read].State.Sample!, pass.Material.Group, first, 1, dest, 0, 0, _width, _height);
            ReleaseTemp(read);
            read = dest;
        }
    }

    private QuadInstance FullScreen(float opacity = 1f) => new()
    {
        Origin = Vector2.Zero,
        AxisX = new Vector2(_width, 0),
        AxisY = new Vector2(0, _height),
        Uv = new Vector4(0, 0, 1, 1),
        Color = Colors.Pack(Color.White, opacity),
    };

    // ---- tilemaps ----

    private void DrawTileMap(TileMap map, in Matrix3x2 view, int target)
    {
        var gpu = map.Gpu ??= NewTileGpu(map);
        var scale = MathF.Sqrt(MathF.Abs((view.M11 * view.M22) - (view.M12 * view.M21)));
        var tilePx = map.TileSize * scale;
        var level = 0;
        while (level < map.MaxLevel && tilePx * (1 << level) < map.LodThreshold)
        {
            level++;
        }

        if (!Matrix3x2.Invert(view, out var inv))
        {
            return;
        }

        // The screen's corners in tile units, to find which chunks can be seen.
        Span<Vector2> corners = [Vector2.Zero, new(_width, 0), new(0, _height), new(_width, _height)];
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach (var c in corners)
        {
            var t = (Vector2.Transform(c, inv) - map.Origin) / map.TileSize;
            minX = MathF.Min(minX, t.X);
            maxX = MathF.Max(maxX, t.X);
            minY = MathF.Min(minY, t.Y);
            maxY = MathF.Max(maxY, t.Y);
        }

        var span = (float)(TileMap.ChunkSize << level);
        var cx0 = Math.Max((int)MathF.Floor(minX / span), 0);
        var cy0 = Math.Max((int)MathF.Floor(minY / span), 0);
        var cx1 = Math.Min((int)MathF.Floor(maxX / span), map.ChunksX(level) - 1);
        var cy1 = Math.Min((int)MathF.Floor(maxY / span), map.ChunksY(level) - 1);

        var first = _quadCount;
        var uploads = 0;
        var dropped = 0;
        var coarse = map.TileSize * (1 << level);
        var lw = map.LevelWidth(level);
        var lh = map.LevelHeight(level);
        for (var cy = cy0; cy <= cy1; cy++)
        {
            for (var cx = cx0; cx <= cx1; cx++)
            {
                int version;
                ushort[]? ids = null;
                uint[]? colors = null;
                if (level == 0)
                {
                    ids = map.IdChunk(cx, cy, out version);
                    if (ids is null)
                    {
                        continue;
                    }
                }
                else
                {
                    colors = map.ColorChunk(level, cx, cy, out version);
                    if (colors is null)
                    {
                        continue;
                    }
                }

                var slot = gpu.Acquire(((long)level << 48) | ((long)cy << 24) | (long)cx, version, _frame, out var upload);
                if (slot < 0)
                {
                    dropped++;
                    continue;
                }

                if (upload)
                {
                    gpu.Upload(_device, level, slot, ids, colors);
                    uploads++;
                }

                var nx = Math.Min(TileMap.ChunkSize, lw - (cx * TileMap.ChunkSize));
                var ny = Math.Min(TileMap.ChunkSize, lh - (cy * TileMap.ChunkSize));
                var originWorld = map.Origin + (new Vector2(cx, cy) * (TileMap.ChunkSize * coarse));
                AppendQuad(new QuadInstance
                {
                    Origin = Vector2.Transform(originWorld, view),
                    AxisX = Vector2.TransformNormal(new Vector2(nx * coarse, 0), view),
                    AxisY = Vector2.TransformNormal(new Vector2(0, ny * coarse), view),
                    Uv = new Vector4(0, 0, nx, ny),
                    Color = 0xFFFFFFFFu,
                    Extra = (uint)slot,
                });
            }
        }

        var count = _quadCount - first;
        _tileQuadCount += count;
        _uploadCount += uploads;
        map.LastStats = new TileMapStats(level, count, uploads, dropped);
        AddQuadOp(level == 0 ? _tileIdsPipeline : _tileFlatPipeline, TextureGroup(map.TileSet.Texture), gpu.Group, first, count, target, 0, 0, _width, _height);
    }

    private TileGpu NewTileGpu(TileMap map)
    {
        var g = new TileGpu(_device, _layoutTile, map);
        _tileStates.Add(g);
        return g;
    }

    // ---- ending the frame ----

    /// <summary>Uploads everything the frame drew and records its passes into <paramref name="encoder"/>. Submit the encoder afterwards.</summary>
    /// <param name="encoder">The command encoder to record into.</param>
    /// <returns>What the frame cost.</returns>
    public FrameStats EndFrame(GpuCommandEncoder encoder)
    {
        RequireFrame();
        ArgumentNullException.ThrowIfNull(encoder);
        Ui.Finish();
        _inFrame = false;
        long bytes = 0;
        bytes += UploadQuads();
        bytes += UploadUi();
        if (_clipCount > 0)
        {
            var span = MemoryMarshal.AsBytes(new ReadOnlySpan<Vector4>(_clipData, 0, _clipCount * 4));
            _device.WriteBuffer(_clipBuffer, 0, span);
            bytes += span.Length;
        }

        foreach (var m in _usedMaterials)
        {
            if (m.UploadedVersion != m.Version)
            {
                _device.WriteBuffer(m.Buffer, 0, MemoryMarshal.AsBytes(m.Parameters));
                m.UploadedVersion = m.Version;
            }
        }

        var passes = 0;
        var draws = 0;
        var current = -1;
        GpuRenderPass? pass = null;
        GpuGraphicsPipeline? pipeline = null;
        var ui = false;
        var ops = CollectionsMarshal.AsSpan(_ops);
        for (var i = 0; i < ops.Length; i++)
        {
            ref var op = ref ops[i];
            if (op.Target != current)
            {
                pass?.EndPass();
                pass = BeginPass(encoder, op.Target);
                passes++;
                current = op.Target;
                pipeline = null;
                pass.SetBindGroup(0, _targets[current].State.Frame);
                ui = !op.IsUi;
            }

            if (op.Pipeline != pipeline)
            {
                pass!.SetPipeline(op.Pipeline);
                pipeline = op.Pipeline;
            }

            if (op.IsUi != ui)
            {
                pass!.SetVertexBuffer(0, op.IsUi ? _uiBuffer! : _quadBuffer!);
                ui = op.IsUi;
            }

            pass!.SetBindGroup(1, op.Texture);
            if (op.Extra is not null)
            {
                pass.SetBindGroup(2, op.Extra);
            }

            pass.SetScissor(op.Sx, op.Sy, (uint)op.Sw, (uint)op.Sh);
            pass.Draw(6, (uint)op.Count, 0, (uint)op.First);
            draws++;
        }

        pass?.EndPass();

        // A target that was only cleared still needs its pass.
        for (var t = 0; t < _targets.Count; t++)
        {
            var ft = _targets[t];
            if (!ft.Begun && ft.HasClear && ft.State.Pooled == false)
            {
                BeginPass(encoder, t).EndPass();
                passes++;
            }
        }

        LastFrame = new FrameStats(draws, passes, _spriteCount, _culledCount, _quadCount, _uiCount, _tileQuadCount, _uploadCount, bytes);
        return LastFrame;
    }

    private GpuRenderPass BeginPass(GpuCommandEncoder encoder, int target)
    {
        var ft = _targets[target];
        ColorAttachment attachment;
        if (!ft.Begun && ft.HasClear)
        {
            var c = ft.Clear;
            // The clear value is written to an sRGB target as linear light, so decode the colour first.
            var a = c.A / 255f;
            attachment = ColorAttachment.Clear(ft.State.Texture, Colors.ToLinear(c.R) * a, Colors.ToLinear(c.G) * a, Colors.ToLinear(c.B) * a, a);
        }
        else
        {
            attachment = new ColorAttachment { Target = ft.State.Texture, Load = LoadOp.Load, Store = StoreOp.Store };
        }

        ft.Begun = true;
        _targets[target] = ft;
        return encoder.BeginRenderPass(attachment);
    }

    private long UploadQuads()
    {
        if (_quadCount == 0)
        {
            return 0;
        }

        var bytes = (ulong)_quadCount * QuadInstance.Size;
        if (_quadBuffer is null || _quadCapacity < bytes)
        {
            _quadBuffer?.Dispose();
            _quadCapacity = Math.Max(bytes, (ulong)_quads.Length * QuadInstance.Size);
            _quadBuffer = _device.CreateBuffer(new BufferDesc(_quadCapacity, BufferUsage.Vertex | BufferUsage.CopyDestination) { Label = "2d quads" });
        }

        _device.WriteBuffer(_quadBuffer, 0, MemoryMarshal.AsBytes(new ReadOnlySpan<QuadInstance>(_quads, 0, _quadCount)));
        return (long)bytes;
    }

    private long UploadUi()
    {
        if (_uiCount == 0)
        {
            return 0;
        }

        var bytes = (ulong)_uiCount * UiInstance.Size;
        if (_uiBuffer is null || _uiCapacity < bytes)
        {
            _uiBuffer?.Dispose();
            _uiCapacity = Math.Max(bytes, (ulong)_uiItems.Length * UiInstance.Size);
            _uiBuffer = _device.CreateBuffer(new BufferDesc(_uiCapacity, BufferUsage.Vertex | BufferUsage.CopyDestination) { Label = "2d ui" });
        }

        _device.WriteBuffer(_uiBuffer, 0, MemoryMarshal.AsBytes(new ReadOnlySpan<UiInstance>(_uiItems, 0, _uiCount)));
        return (long)bytes;
    }

    private void RequireFrame()
    {
        if (!_inFrame)
        {
            throw new InvalidOperationException("call BeginFrame before drawing");
        }
    }

    /// <summary>Releases every GPU object the renderer made. Dispose the textures, tile sets and materials you created yourself first or after; they are independent.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _device.WaitIdle();
        foreach (var t in _tileStates)
        {
            t.Dispose();
        }

        foreach (var t in _temps)
        {
            t.Dispose();
        }

        foreach (var t in _finalTargets.Values)
        {
            t.Dispose();
        }

        Ui.Dispose();
        _white.Dispose();
        _quadBuffer?.Dispose();
        _uiBuffer?.Dispose();
        _clipBuffer.Dispose();
        _clampLinear.Dispose();
        foreach (var p in new IDisposable[] { _spritePipeline, _tileIdsPipeline, _tileFlatPipeline, _uiPipeline, _blurPipeline, _gradePipeline, _quadVertex, _spritePixel, _blurPixel, _gradePixel, _tileVertex, _tileIdsPixel, _tileFlatPixel, _uiVertex, _uiPixel, _layoutPlain, _layoutParams, _layoutTile })
        {
            p.Dispose();
        }
    }
}
