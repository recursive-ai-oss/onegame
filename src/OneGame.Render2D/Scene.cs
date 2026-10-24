using System.Numerics;
using OneGame.Gpu;
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
        Add(layer, sprite.Z, _spriteCount, false);
        _spriteCount++;
    }

    /// <summary>Queues a tilemap.</summary>
    /// <param name="layer">The layer it belongs to.</param>
    /// <param name="map">The map.</param>
    /// <param name="z">Draw order inside the layer, as for sprites.</param>
    public void Draw(Layer2D layer, TileMap map, float z = 0)
    {
        _maps.Add(map);
        Add(layer, z, _maps.Count - 1, true);
    }

    private void Add(Layer2D layer, float z, int index, bool isMap)
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
        _values[_entryCount++] = (index << 1) | (isMap ? 1 : 0);
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

    internal bool IsTileMap(int entry) => (_values[entry] & 1) != 0;

    internal int IndexOf(int entry) => _values[entry] >> 1;

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

    internal GpuGraphicsPipeline Pipeline { get; }

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
        Group.Dispose();
        Buffer.Dispose();
        if (_ownsPipeline)
        {
            Pipeline.Dispose();
        }
    }
}
