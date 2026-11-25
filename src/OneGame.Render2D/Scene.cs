using System.Numerics;
using RecursiveAI.OneGpu;
using OneGame.UI;

namespace OneGame.Render2D;

/// <summary>Mirror flags for a sprite.</summary>
[Flags]
public enum SpriteFlip
{
    /// <summary>No mirroring.</summary>
    None = 0,

    /// <summary>Mirror left to right.</summary>
    Horizontal = 1,

    /// <summary>Mirror top to bottom.</summary>
    Vertical = 2,
}

/// <summary>
/// One sprite to draw this frame. World coordinates have +X right and +Y down, like the screen. Build one with <see cref="Create"/> and set what differs
/// from the defaults; a struct made with <c>new Sprite()</c> has no texture and is skipped.
/// </summary>
public struct Sprite
{
    /// <summary>The image to draw. Sprites sharing a texture and material are drawn together.</summary>
    public Texture2D? Texture { get; set; }

    /// <summary>The part of the texture to draw, in pixels. An empty rectangle (the default) means the whole texture.</summary>
    public PixelRect Source { get; set; }

    /// <summary>Where the pivot is, in world units.</summary>
    public Vector2 Position { get; set; }

    /// <summary>The pivot as a fraction of the sprite: (0,0) top-left, (0.5,0.5) centre.</summary>
    public Vector2 Pivot { get; set; }

    /// <summary>World units per source pixel, per axis.</summary>
    public Vector2 Scale { get; set; }

    /// <summary>Rotation in radians about the pivot; positive turns clockwise on screen.</summary>
    public float Rotation { get; set; }

    /// <summary>Colour multiplied into the texture, sRGB with straight alpha. White draws the texture unchanged.</summary>
    public Color Tint { get; set; }

    /// <summary>Draw order inside the layer: lower is behind. Equal values draw in submission order.</summary>
    public float Z { get; set; }

    /// <summary>Mirroring.</summary>
    public SpriteFlip Flip { get; set; }

    /// <summary>A custom pixel shader, or null for the plain one.</summary>
    public Material? Material { get; set; }

    /// <summary>Makes a sprite with the pivot at its centre, scale 1, white tint and no rotation.</summary>
    /// <param name="texture">The image.</param>
    /// <param name="position">Where the pivot goes, in world units.</param>
    /// <returns>The sprite.</returns>
    public static Sprite Create(Texture2D texture, Vector2 position) => new()
    {
        Texture = texture,
        Position = position,
        Pivot = new Vector2(0.5f, 0.5f),
        Scale = Vector2.One,
        Tint = Color.White,
    };
}

/// <summary>The view onto the world: where it looks, how far in, and how it is turned.</summary>
public struct Camera2D
{
    /// <summary>The world point at the centre of the screen.</summary>
    public Vector2 Position { get; set; }

    /// <summary>Physical pixels per world unit. A HiDPI game multiplies its zoom by the display scale.</summary>
    public float Zoom { get; set; }

    /// <summary>Rotation in radians; positive turns the view clockwise.</summary>
    public float Rotation { get; set; }

    /// <summary>Round sprite positions to whole pixels, which keeps pixel art crisp while the camera moves. Meant for an unrotated camera.</summary>
    public bool PixelSnap { get; set; }

    /// <summary>A camera looking at the origin at zoom 1.</summary>
    public static Camera2D Default => new() { Zoom = 1 };

    /// <summary>The matrix taking world points to framebuffer pixels (row vectors) for a layer that follows the camera by <paramref name="parallax"/>.</summary>
    /// <param name="screenSize">Target size in pixels.</param>
    /// <param name="parallax">1 follows the camera exactly; 0 stays fixed on screen; between scrolls slower.</param>
    /// <returns>The world-to-screen matrix.</returns>
    public readonly Matrix3x2 View(Vector2 screenSize, Vector2 parallax) =>
        Matrix3x2.CreateTranslation(-Position * parallax)
        * Matrix3x2.CreateScale(Zoom)
        * Matrix3x2.CreateRotation(-Rotation)
        * Matrix3x2.CreateTranslation(screenSize * 0.5f);
}

/// <summary>A layer of a <see cref="Scene2D"/>. Layers draw in the order they were added; each follows the camera by its own parallax factor.</summary>
public sealed class Layer2D
{
    internal Layer2D(string name, int index, Vector2 parallax)
    {
        Name = name;
        Index = index;
        Parallax = parallax;
    }

    /// <summary>The layer's name, for debugging and captures.</summary>
    public string Name { get; }

    /// <summary>Position in draw order; 0 is the back.</summary>
    public int Index { get; }

    /// <summary>How much the layer follows the camera position, per axis. 1 is the world itself; a distant backdrop uses something below 1.</summary>
    public Vector2 Parallax { get; set; }

    /// <summary>Layers that are not visible are skipped.</summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// A camera for this layer alone, or null (the default) to follow the camera the scene is drawn with. A layer with its own camera draws the
    /// world at another zoom or place than the layers beside it, which is how a minimap or a close-up shares a frame with the main view.
    /// <see cref="Parallax"/> still applies on top of it.
    /// </summary>
    public Camera2D? Camera { get; set; }

    /// <summary>
    /// When true the layer ignores every camera and its coordinates are framebuffer pixels (origin top-left, one unit per pixel): the layer for a
    /// HUD, labels and menus drawn over a zoomed and panned world. Wins over <see cref="Camera"/>.
    /// </summary>
    public bool ScreenSpace { get; set; }

    /// <summary>
    /// A framebuffer rectangle the layer is confined to, or null for the whole target. The layer's camera is centred on the rectangle (a
    /// <see cref="ScreenSpace"/> layer puts its origin at the rectangle's top-left) and nothing outside it is drawn.
    /// </summary>
    public Rect? Viewport { get; set; }

    /// <summary>
    /// A render texture to draw this layer into instead of the frame, or null (the default). The layer's camera, viewport and screen-space rules then
    /// refer to the texture's size. The texture is cleared to <see cref="TargetClear"/> the first time a layer draws into it in a frame; a layer that
    /// has nothing to draw leaves it as it was, so a texture can be redrawn only when its contents change. Sprites that show the texture belong on a
    /// later layer; an earlier layer shows what the previous frame left.
    /// </summary>
    public RenderTexture2D? Target { get; set; }

    /// <summary>The colour <see cref="Target"/> is cleared to (sRGB, straight alpha), transparent by default; null keeps what the texture holds, so drawing accumulates.</summary>
    public Color? TargetClear { get; set; } = new Color(0, 0, 0, 0);

    /// <summary>
    /// Effects run over this layer alone, after it is drawn and before the layers above it (a HUD on a later layer stays sharp), or null for none.
    /// The layer is drawn into an image of its own, the chain runs over it, and the result is blended onto the frame (or onto <see cref="Target"/>).
    /// An empty chain, like null, costs nothing and adds no pass.
    /// </summary>
    public PostChain? Post { get; set; }

    /// <summary>The matrix taking this layer's coordinates to framebuffer pixels (row vectors), for the scene's camera and target size.</summary>
    /// <param name="sceneCamera">The camera the scene is drawn with; used unless the layer has its own <see cref="Camera"/>.</param>
    /// <param name="screenSize">Target size in pixels.</param>
    /// <returns>The layer-to-screen matrix.</returns>
    public Matrix3x2 View(in Camera2D sceneCamera, Vector2 screenSize)
    {
        var origin = Vector2.Zero;
        var size = screenSize;
        if (Viewport is { } vp)
        {
            origin = new Vector2(vp.X, vp.Y);
            size = new Vector2(vp.Width, vp.Height);
        }

        if (ScreenSpace)
        {
            return Matrix3x2.CreateTranslation(origin);
        }

        return (Camera ?? sceneCamera).View(size, Parallax) * Matrix3x2.CreateTranslation(origin);
    }

    /// <summary>Whether sprites on this layer snap to whole pixels for the scene's camera.</summary>
    internal bool Snaps(in Camera2D sceneCamera) => !ScreenSpace && (Camera ?? sceneCamera).PixelSnap;

    /// <summary>The framebuffer rectangle this layer may draw in, as whole pixels clipped to the target.</summary>
    internal void Scissor(int width, int height, out int x, out int y, out int w, out int h)
    {
        if (Viewport is not { } vp)
        {
            x = 0;
            y = 0;
            w = width;
            h = height;
            return;
        }

        var x0 = Math.Clamp((int)MathF.Floor(vp.X), 0, width);
        var y0 = Math.Clamp((int)MathF.Floor(vp.Y), 0, height);
        var x1 = Math.Clamp((int)MathF.Ceiling(vp.X + vp.Width), 0, width);
        var y1 = Math.Clamp((int)MathF.Ceiling(vp.Y + vp.Height), 0, height);
        x = x0;
        y = y0;
        w = Math.Max(x1 - x0, 0);
        h = Math.Max(y1 - y0, 0);
    }
}

/// <summary>
/// Receives the markers a <see cref="Scene2D"/> holds (see <see cref="Scene2D.Mark"/>) at their place in the draw order, while
/// <see cref="Renderer2D.DrawScene"/> runs. The host's immediate-mode text uses it to interleave with sprites.
/// </summary>
public interface ISceneMarkerSink
{
    /// <summary>Called when a marker's turn comes; draw whatever it stands for on <see cref="Renderer2D.Ui"/>.</summary>
    /// <param name="id">The number given to <see cref="Scene2D.Mark"/>.</param>
    /// <param name="layer">The layer the marker was put on.</param>
    /// <param name="view">That layer's matrix to framebuffer pixels, as <see cref="Layer2D.View"/> gives it.</param>
    void DrawMarker(int id, Layer2D layer, in Matrix3x2 view);
}

/// <summary>
/// Everything to draw in one frame of the world: sprites and tilemaps on layers. Fill it each frame (or keep entries that do not move and refill the rest),
/// then pass it to <see cref="Renderer2D.DrawScene"/>. It reuses its storage, so a steady scene allocates nothing.
/// </summary>
public sealed class Scene2D
{
    private readonly List<Layer2D> _layers = [];
    private Sprite[] _sprites = new Sprite[256];
    private int _spriteCount;
    private readonly List<TileMap> _maps = [];
    // Keys and values are sorted together with the default comparer, which allocates nothing (a comparer object would).
    private UInt128[] _keys = new UInt128[256];
    private int[] _values = new int[256];
    private int _entryCount;
    private UInt128 _lastKey;
    private bool _sorted = true;
    private uint _sequence;

    /// <summary>Makes a scene with one layer, "default", that follows the camera exactly.</summary>
    public Scene2D() => AddLayer("default", Vector2.One);

    /// <summary>The layers, back to front.</summary>
    public IReadOnlyList<Layer2D> Layers => _layers;

    /// <summary>The first layer.</summary>
    public Layer2D DefaultLayer => _layers[0];

    /// <summary>Finds the layer with a name, or adds it in front of the existing ones. Layers stay between frames, so a game asks for its layers by name every frame.</summary>
    /// <param name="name">The layer's name.</param>
    /// <param name="parallax">The parallax a new layer gets (the layer's own value is left alone when it exists); the camera exactly when null.</param>
    /// <returns>The layer.</returns>
    public Layer2D GetLayer(string name, Vector2? parallax = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        foreach (var l in _layers)
        {
            if (string.Equals(l.Name, name, StringComparison.Ordinal))
            {
                return l;
            }
        }

        return AddLayer(name, parallax ?? Vector2.One);
    }

    /// <summary>Adds a layer in front of the existing ones.</summary>
    /// <param name="name">A name for debugging.</param>
    /// <param name="parallax">How much the layer follows the camera; see <see cref="Layer2D.Parallax"/>.</param>
    /// <returns>The layer.</returns>
    public Layer2D AddLayer(string name, Vector2 parallax)
    {
        var l = new Layer2D(name, _layers.Count, parallax);
        _layers.Add(l);
        return l;
    }

    /// <summary>Removes everything drawn so far. Layers stay.</summary>
    public void Clear()
    {
        _spriteCount = 0;
        _entryCount = 0;
        _maps.Clear();
        _sorted = true;
        _lastKey = 0;
        _sequence = 0;
    }

    /// <summary>Queues a sprite on the default layer.</summary>
    /// <param name="sprite">The sprite.</param>
    public void Draw(in Sprite sprite) => Draw(_layers[0], sprite);

    /// <summary>Queues a sprite.</summary>
    /// <param name="layer">The layer it belongs to.</param>
    /// <param name="sprite">The sprite.</param>
    public void Draw(Layer2D layer, in Sprite sprite)
    {
        if (_spriteCount == _sprites.Length)
        {
            Array.Resize(ref _sprites, _spriteCount * 2);
        }

        _sprites[_spriteCount] = sprite;
        Add(layer, sprite.Z, _spriteCount, Kind.Sprite);
        _spriteCount++;
    }

    /// <summary>Queues a tilemap.</summary>
    /// <param name="layer">The layer it belongs to.</param>
    /// <param name="map">The map.</param>
    /// <param name="z">Draw order inside the layer, as for sprites.</param>
    public void Draw(Layer2D layer, TileMap map, float z = 0)
    {
        _maps.Add(map);
        Add(layer, z, _maps.Count - 1, Kind.Map);
    }

    /// <summary>
    /// Puts a marker in the draw order: when <see cref="Renderer2D.DrawScene"/> reaches it (by layer, then <paramref name="z"/>, then submission order, like a sprite)
    /// it calls the sink's <see cref="ISceneMarkerSink.DrawMarker"/>. Markers cost no draw of their own.
    /// </summary>
    /// <param name="layer">The layer it belongs to.</param>
    /// <param name="z">Draw order inside the layer, as for sprites.</param>
    /// <param name="id">A number the sink understands; 0 to 2^29.</param>
    public void Mark(Layer2D layer, float z, int id)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(id);
        Add(layer, z, id, Kind.Marker);
    }

    private enum Kind
    {
        Sprite = 0,
        Map = 1,
        Marker = 2,
    }

    private void Add(Layer2D layer, float z, int index, Kind kind)
    {
        if (_entryCount == _keys.Length)
        {
            Array.Resize(ref _keys, _entryCount * 2);
            Array.Resize(ref _values, _entryCount * 2);
        }

        // Order by layer, then z, then submission order; the key packs all three so one sort does it.
        var bits = BitConverter.SingleToUInt32Bits(z);
        bits = (bits & 0x80000000u) != 0 ? ~bits : bits | 0x80000000u;
        var key = ((UInt128)(uint)layer.Index << 64) | ((UInt128)bits << 32) | _sequence++;
        if (_entryCount > 0 && key < _lastKey)
        {
            _sorted = false;
        }

        _lastKey = key;
        _keys[_entryCount] = key;
        _values[_entryCount++] = (index << 2) | (int)kind;
    }

    internal int EntryCount => _entryCount;

    /// <summary>Puts the entries in draw order. Entries already submitted in order (the usual case) are not touched.</summary>
    internal void Sort()
    {
        if (!_sorted)
        {
            Array.Sort(_keys, _values, 0, _entryCount);
            _sorted = true;
        }
    }

    internal int LayerOf(int entry) => (int)(uint)(_keys[entry] >> 64);

    internal bool IsTileMap(int entry) => (_values[entry] & 3) == (int)Kind.Map;

    internal bool IsMarker(int entry) => (_values[entry] & 3) == (int)Kind.Marker;

    internal int IndexOf(int entry) => _values[entry] >> 2;

    internal ref readonly Sprite SpriteAt(int index) => ref _sprites[index];

    internal TileMap MapAt(int index) => _maps[index];
}

/// <summary>
/// A pixel shader with parameters, usable on sprites. Make one with <see cref="Renderer2D.CreateMaterial"/>. The shader takes the renderer's quad
/// vertex output (see the material contract in the renderer's documentation) and up to eight float4 parameters; a parameter change takes effect
/// on every sprite that uses the material in the frame being built.
/// </summary>
public sealed class Material : IDisposable
{
    private readonly Vector4[] _params = new Vector4[8];
    private readonly bool _ownsPipeline;
    private bool _disposed;

    internal Renderer2D? Tracker { get; init; }

    internal Material(GpuBuffer buffer, GpuBindGroup group, GpuGraphicsPipeline pipeline, bool ownsPipeline)
    {
        _ownsPipeline = ownsPipeline;
        Buffer = buffer;
        Group = group;
        Pipeline = pipeline;
        Version = 1;
    }

    /// <summary>The number of parameter slots.</summary>
    public int ParameterCount => _params.Length;

    internal GpuBuffer Buffer { get; }

    internal GpuBindGroup Group { get; }

    internal GpuGraphicsPipeline Pipeline { get; private set; }

    internal bool OwnsPipeline => _ownsPipeline;

    /// <summary>Swaps in a new pipeline and returns the old one for the caller to release once the GPU is done with it.</summary>
    internal GpuGraphicsPipeline SwapPipeline(GpuGraphicsPipeline next)
    {
        var old = Pipeline;
        Pipeline = next;
        return old;
    }

    internal int Version { get; private set; }

    internal int UploadedVersion { get; set; }

    internal int LastFrame { get; set; } = -1;

    /// <summary>Sets parameter <paramref name="slot"/>, which the shader reads as <c>P[slot]</c>.</summary>
    /// <param name="slot">0 to 7.</param>
    /// <param name="value">The value.</param>
    public void SetParameter(int slot, Vector4 value)
    {
        if ((uint)slot >= (uint)_params.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(slot), "a material has 8 parameter slots (0 to 7)");
        }

        if (_params[slot] != value)
        {
            _params[slot] = value;
            Version++;
        }
    }

    /// <summary>Reads back parameter <paramref name="slot"/>.</summary>
    /// <param name="slot">0 to 7.</param>
    /// <returns>The current value.</returns>
    public Vector4 GetParameter(int slot) => _params[slot];

    internal ReadOnlySpan<Vector4> Parameters => _params;

    /// <summary>Releases the GPU objects. Do not draw with the material afterwards.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Tracker?.Untrack(this);
        Group.Dispose();
        Buffer.Dispose();
        if (_ownsPipeline)
        {
            Pipeline.Dispose();
        }
    }
}
