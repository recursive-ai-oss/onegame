using System.Numerics;
using OneGame.Gpu;

namespace OneGame.Render2D;

/// <summary>A grid of equally sized tiles in one texture. Tile id 0 means "empty"; id n is the n-th cell counted left to right, top to bottom, starting at 1.</summary>
public sealed class TileSet : IDisposable
{
    private readonly uint[] _average;

    internal TileSet(Texture2D texture, int tileWidth, int tileHeight, int columns, int rows, uint[] average)
    {
        Texture = texture;
        TileWidth = tileWidth;
        TileHeight = tileHeight;
        Columns = columns;
        Rows = rows;
        _average = average;
    }

    /// <summary>The tile image.</summary>
    public Texture2D Texture { get; }

    /// <summary>Width of one tile in texels.</summary>
    public int TileWidth { get; }

    /// <summary>Height of one tile in texels.</summary>
    public int TileHeight { get; }

    /// <summary>Tiles per row.</summary>
    public int Columns { get; }

    /// <summary>Rows of tiles.</summary>
    public int Rows { get; }

    /// <summary>The largest valid tile id.</summary>
    public int TileCount => Columns * Rows;

    /// <summary>The mean colour of tile <paramref name="id"/> as the renderer stores colours (packed R, G, B, A; premultiplied, sRGB-encoded). Used for far zoom levels.</summary>
    internal uint Average(int id) => id <= 0 || id > _average.Length ? 0 : _average[id - 1];

    /// <summary>Releases the texture.</summary>
    public void Dispose() => Texture.Dispose();
}

/// <summary>What the tilemap drew last frame; for tests, captures and tuning.</summary>
/// <param name="Level">The level of detail drawn: 0 is individual tiles, level n is one colour per 2^n by 2^n tiles.</param>
/// <param name="ChunkQuads">Chunk quads drawn.</param>
/// <param name="ChunkUploads">Chunks uploaded to the GPU this frame.</param>
/// <param name="DroppedChunks">Visible chunks that did not fit in the GPU pool (the map's pool is too small for this zoom).</param>
public readonly record struct TileMapStats(int Level, int ChunkQuads, int ChunkUploads, int DroppedChunks);

/// <summary>
/// A rectangular map of tile ids, stored in chunks of 64 by 64 so a very large map costs memory only where it has tiles. Zoomed out, the renderer draws
/// from coarser levels (one averaged colour per 2^n by 2^n tiles) instead of drawing every tile, so the work depends on the screen and not on the map.
/// World +Y is down: tile (0, 0) is at <see cref="Origin"/> and tile (x, y) covers <see cref="TileSize"/> world units to the right and down.
/// </summary>
public sealed class TileMap : IDisposable
{
    /// <summary>Tiles per chunk edge.</summary>
    public const int ChunkSize = 64;

    /// <summary>The largest width or height in tiles.</summary>
    public const int MaxSize = 1 << 16;

    private const int ChunkTiles = ChunkSize * ChunkSize;

    private readonly ushort[]?[] _chunks;
    private readonly int[] _versions;
    private readonly int[] _chunksX;
    private readonly int[] _chunksY;
    private readonly uint[][][] _coarse;
    private readonly bool[][] _stale;
    private readonly bool[][] _emptyCoarse;
    private readonly int[][] _coarseVersion;

    /// <summary>Makes an empty map.</summary>
    /// <param name="tileSet">The tiles to draw with.</param>
    /// <param name="width">Width in tiles, 1 to <see cref="MaxSize"/>.</param>
    /// <param name="height">Height in tiles, 1 to <see cref="MaxSize"/>.</param>
    /// <param name="tileSize">World units per tile edge.</param>
    public TileMap(TileSet tileSet, int width, int height, float tileSize)
    {
        ArgumentNullException.ThrowIfNull(tileSet);
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(width, MaxSize);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(height, MaxSize);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(tileSize, 0f);
        TileSet = tileSet;
        Width = width;
        Height = height;
        TileSize = tileSize;
        var big = Math.Max(width, height);
        MaxLevel = 0;
        while ((ChunkSize << MaxLevel) < big)
        {
            MaxLevel++;
        }

        _chunksX = new int[MaxLevel + 1];
        _chunksY = new int[MaxLevel + 1];
        for (var l = 0; l <= MaxLevel; l++)
        {
            _chunksX[l] = CeilDiv(width, ChunkSize << l);
            _chunksY[l] = CeilDiv(height, ChunkSize << l);
        }

        _chunks = new ushort[_chunksX[0] * _chunksY[0]][];
        _versions = new int[_chunks.Length];
        _coarse = new uint[MaxLevel + 1][][];
        _stale = new bool[MaxLevel + 1][];
        _emptyCoarse = new bool[MaxLevel + 1][];
        _coarseVersion = new int[MaxLevel + 1][];
        for (var l = 1; l <= MaxLevel; l++)
        {
            var n = _chunksX[l] * _chunksY[l];
            _coarse[l] = new uint[n][];
            _stale[l] = new bool[n];
            _emptyCoarse[l] = new bool[n];
            _coarseVersion[l] = new int[n];
            Array.Fill(_stale[l], true);
        }
    }

    /// <summary>The tiles this map draws with.</summary>
    public TileSet TileSet { get; }

    /// <summary>Width in tiles.</summary>
    public int Width { get; }

    /// <summary>Height in tiles.</summary>
    public int Height { get; }

    /// <summary>World units per tile edge.</summary>
    public float TileSize { get; }

    /// <summary>The world position of the map's top-left corner.</summary>
    public Vector2 Origin { get; set; }

    /// <summary>The coarsest level of detail the map can be drawn at.</summary>
    public int MaxLevel { get; }

    /// <summary>Once a tile would be smaller than this many pixels on screen, the renderer switches to the next coarser level. 4 by default.</summary>
    public float LodThreshold { get; set; } = 4f;

    /// <summary>Chunks the GPU keeps resident for this map. It must cover the chunks visible at once (about 135 on a 4K screen at the default threshold).</summary>
    public int PoolChunks { get; init; } = 256;

    /// <summary>What the last <see cref="Renderer2D.DrawScene"/> drew for this map.</summary>
    public TileMapStats LastStats { get; internal set; }

    internal TileGpu? Gpu { get; set; }

    /// <summary>Reads a tile id; 0 outside the map.</summary>
    /// <param name="x">Column.</param>
    /// <param name="y">Row.</param>
    /// <returns>The tile id.</returns>
    public ushort Get(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
        {
            return 0;
        }

        var chunk = _chunks[((y / ChunkSize) * _chunksX[0]) + (x / ChunkSize)];
        return chunk is null ? (ushort)0 : chunk[((y % ChunkSize) * ChunkSize) + (x % ChunkSize)];
    }

    /// <summary>Writes a tile id.</summary>
    /// <param name="x">Column.</param>
    /// <param name="y">Row.</param>
    /// <param name="id">The tile id: 0 for empty, otherwise 1 to <see cref="TileSet.TileCount"/>.</param>
    public void Set(int x, int y, ushort id)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
        {
            throw new ArgumentOutOfRangeException(nameof(x), $"tile ({x}, {y}) is outside the {Width}x{Height} map");
        }

        if (id > TileSet.TileCount)
        {
            throw new ArgumentOutOfRangeException(nameof(id), $"tile id {id} is not in the tile set (1 to {TileSet.TileCount}, or 0 for empty)");
        }

        var ci = ((y / ChunkSize) * _chunksX[0]) + (x / ChunkSize);
        var chunk = _chunks[ci];
        if (chunk is null)
        {
            if (id == 0)
            {
                return;
            }

            chunk = new ushort[ChunkTiles];
            _chunks[ci] = chunk;
        }

        ref var cell = ref chunk[((y % ChunkSize) * ChunkSize) + (x % ChunkSize)];
        if (cell == id)
        {
            return;
        }

        cell = id;
        _versions[ci]++;
        for (var l = 1; l <= MaxLevel; l++)
        {
            _stale[l][(((y >> (6 + l)) * _chunksX[l])) + (x >> (6 + l))] = true;
        }
    }

    /// <summary>Releases the GPU memory of this map.</summary>
    public void Dispose()
    {
        Gpu?.Dispose();
        Gpu = null;
    }

    internal int ChunksX(int level) => _chunksX[level];

    internal int ChunksY(int level) => _chunksY[level];

    /// <summary>Width of the map at a level, in coarse tiles.</summary>
    internal int LevelWidth(int level) => CeilDiv(Width, 1 << level);

    internal int LevelHeight(int level) => CeilDiv(Height, 1 << level);

    internal ushort[]? IdChunk(int cx, int cy, out int version)
    {
        var i = (cy * _chunksX[0]) + cx;
        version = _versions[i];
        return _chunks[i];
    }

    /// <summary>
    /// The colours of one chunk at a coarse level, built from the level below when the tiles under it changed. Each entry is the mean of four colours
    /// of the finer level, averaged in linear light on premultiplied values. Returns null when the chunk is entirely empty.
    /// </summary>
    internal uint[]? ColorChunk(int level, int cx, int cy, out int version)
    {
        var ci = (cy * _chunksX[level]) + cx;
        if (_stale[level][ci])
        {
            Build(level, cx, cy, ci);
            _stale[level][ci] = false;
            _coarseVersion[level][ci]++;
        }

        version = _coarseVersion[level][ci];
        return _emptyCoarse[level][ci] ? null : _coarse[level][ci];
    }

    private void Build(int level, int cx, int cy, int ci)
    {
        var data = _coarse[level][ci] ??= new uint[ChunkTiles];
        Array.Clear(data);
        var any = false;
        for (var qy = 0; qy < 2; qy++)
        {
            for (var qx = 0; qx < 2; qx++)
            {
                var ccx = (cx * 2) + qx;
                var ccy = (cy * 2) + qy;
                if (ccx >= _chunksX[level - 1] || ccy >= _chunksY[level - 1])
                {
                    continue;
                }

                // The four children of a quadrant of this chunk are a 64x64 block in one child chunk.
                if (level == 1)
                {
                    var ids = IdChunk(ccx, ccy, out _);
                    if (ids is not null)
                    {
                        any |= AverageIds(ids, data, qx * 32, qy * 32);
                    }
                }
                else
                {
                    var child = ColorChunk(level - 1, ccx, ccy, out _);
                    if (child is not null)
                    {
                        any |= AverageColors(child, data, qx * 32, qy * 32);
                    }
                }
            }
        }

        _emptyCoarse[level][ci] = !any;
    }

    private bool AverageIds(ushort[] ids, uint[] output, int ox, int oy)
    {
        var any = false;
        for (var j = 0; j < 32; j++)
        {
            for (var i = 0; i < 32; i++)
            {
                Span<uint> four =
                [
                    TileSet.Average(ids[(((2 * j) * ChunkSize) + (2 * i))]),
                    TileSet.Average(ids[(((2 * j) * ChunkSize) + (2 * i) + 1)]),
                    TileSet.Average(ids[(((2 * j) + 1) * ChunkSize) + (2 * i)]),
                    TileSet.Average(ids[(((2 * j) + 1) * ChunkSize) + (2 * i) + 1]),
                ];
                var c = Mean(four);
                output[((oy + j) * ChunkSize) + ox + i] = c;
                any |= c != 0;
            }
        }

        return any;
    }

    private static bool AverageColors(uint[] child, uint[] output, int ox, int oy)
    {
        var any = false;
        for (var j = 0; j < 32; j++)
        {
            for (var i = 0; i < 32; i++)
            {
                Span<uint> four =
                [
                    child[((2 * j) * ChunkSize) + (2 * i)],
                    child[((2 * j) * ChunkSize) + (2 * i) + 1],
                    child[(((2 * j) + 1) * ChunkSize) + (2 * i)],
                    child[(((2 * j) + 1) * ChunkSize) + (2 * i) + 1],
                ];
                var c = Mean(four);
                output[((oy + j) * ChunkSize) + ox + i] = c;
                any |= c != 0;
            }
        }

        return any;
    }

    private static uint Mean(ReadOnlySpan<uint> four)
    {
        if ((four[0] | four[1] | four[2] | four[3]) == 0)
        {
            return 0;
        }

        float r = 0, g = 0, b = 0, a = 0;
        foreach (var c in four)
        {
            r += Colors.ToLinear((byte)c);
            g += Colors.ToLinear((byte)(c >> 8));
            b += Colors.ToLinear((byte)(c >> 16));
            a += (byte)(c >> 24);
        }

        return Colors.ToSrgb(r * 0.25f) | ((uint)Colors.ToSrgb(g * 0.25f) << 8) | ((uint)Colors.ToSrgb(b * 0.25f) << 16)
            | ((uint)(byte)MathF.Round(a * 0.25f) << 24);
    }

    private static int CeilDiv(int a, int b) => (a + b - 1) / b;
}
