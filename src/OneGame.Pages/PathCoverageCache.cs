using System.Numerics;
using OneGame.Gpu;
using OneRender.Painting;

namespace OneGame.Pages;

/// <summary>An 8-bit coverage texture for the device rectangle (X, Y, W, H) of a path.</summary>
internal readonly record struct PathCoverage(GpuTexture Texture, int X, int Y, int W, int H);

/// <summary>
/// Path coverage bitmaps made on the CPU and kept while pages keep asking for them: the key is the path's content, the matrix, the rectangle and the
/// fill rule, so an animated page re-rasterizes only the paths that move.
/// </summary>
internal sealed class PathCoverageCache : IDisposable
{
    private const int MaxEntries = 256;
    private const int MaxPixels = 4096 * 4096;
    private const int KeepFrames = 4;

    private readonly struct Key(ulong hash, int m11, int m12, int m21, int m22, int dx, int dy, int x0, int y0, int x1, int y1, bool evenOdd) : IEquatable<Key>
    {
        private readonly ulong _hash = hash;
        private readonly int _m11 = m11, _m12 = m12, _m21 = m21, _m22 = m22, _dx = dx, _dy = dy, _x0 = x0, _y0 = y0, _x1 = x1, _y1 = y1;
        private readonly bool _eo = evenOdd;

        public bool Equals(Key o) => _hash == o._hash && _m11 == o._m11 && _m12 == o._m12 && _m21 == o._m21 && _m22 == o._m22 && _dx == o._dx && _dy == o._dy
            && _x0 == o._x0 && _y0 == o._y0 && _x1 == o._x1 && _y1 == o._y1 && _eo == o._eo;

        public override bool Equals(object? obj) => obj is Key k && Equals(k);

        public override int GetHashCode() => HashCode.Combine(_hash, _m11, _m12, _m21, _m22, HashCode.Combine(_dx, _dy, _x0, _y0, _x1, _y1, _eo));
    }

    private sealed class Entry
    {
        public required GpuTexture Texture;
        public int W;
        public int H;
        public int X;
        public int Y;
        public int LastFrame;
    }

    private readonly PageGpu _gpu;
    private readonly PathRaster _raster = new();
    private readonly Dictionary<Key, Entry> _entries = [];
    private readonly List<Key> _dead = [];
    private byte[] _scratch = new byte[1024];
    private int _frame;

    public PathCoverageCache(PageGpu gpu) => _gpu = gpu;

    public int Count => _entries.Count;

    /// <summary>Counts of bitmaps made (not found in the cache) since the cache was created.</summary>
    public int Rasterized { get; private set; }

    /// <summary>Marks the start of a frame and drops what no frame has asked for lately.</summary>
    public void BeginFrame(int frame)
    {
        if (frame == _frame)
        {
            return;
        }

        _frame = frame;
        _dead.Clear();
        foreach (var (key, e) in _entries)
        {
            if (frame - e.LastFrame > KeepFrames)
            {
                _dead.Add(key);
            }
        }

        if (_dead.Count == 0)
        {
            return;
        }

        _gpu.Device.WaitIdle();
        foreach (var key in _dead)
        {
            var e = _entries[key];
            _gpu.Forget(e.Texture);
            e.Texture.Dispose();
            _entries.Remove(key);
        }
    }

    /// <summary>The coverage of a path item over a device rectangle. False when the rectangle is empty or the path is too big or too complex.</summary>
    public bool TryGet(in DisplayItem it, in Matrix3x2 ctm, int x0, int y0, int x1, int y1, out PathCoverage coverage)
    {
        coverage = default;
        var w = x1 - x0;
        var h = y1 - y0;
        if (w <= 0 || h <= 0 || (long)w * h > MaxPixels)
        {
            return false;
        }

        var evenOdd = it.FillRule == FillRule.EvenOdd;
        var key = new Key(it.ContentHash, Bits(ctm.M11), Bits(ctm.M12), Bits(ctm.M21), Bits(ctm.M22), Bits(ctm.M31), Bits(ctm.M32), x0, y0, x1, y1, evenOdd);
        if (_entries.TryGetValue(key, out var e))
        {
            e.LastFrame = _frame;
            coverage = new PathCoverage(e.Texture, e.X, e.Y, e.W, e.H);
            return true;
        }

        if (_entries.Count >= MaxEntries)
        {
            return false;
        }

        if (_scratch.Length < w * h)
        {
            _scratch = new byte[w * h];
        }

        if (!_raster.Fill(it.PathVerbs, it.PathCoords, ctm, evenOdd, x0, y0, w, h, _scratch))
        {
            return false;
        }

        var tex = _gpu.Device.CreateTexture(new TextureDesc((uint)w, (uint)h, GpuFormat.R8Unorm, TextureUsage.Sampled | TextureUsage.CopyDestination) { Label = "page path" });
        _gpu.Device.WriteTexture(tex, 0, 0, _scratch.AsSpan(0, w * h));
        Rasterized++;
        _entries[key] = new Entry { Texture = tex, W = w, H = h, X = x0, Y = y0, LastFrame = _frame };
        coverage = new PathCoverage(tex, x0, y0, w, h);
        return true;
    }

    public void Dispose()
    {
        _gpu.Device.WaitIdle();
        foreach (var e in _entries.Values)
        {
            e.Texture.Dispose();
        }

        _entries.Clear();
    }

    private static int Bits(float f) => BitConverter.SingleToInt32Bits(f);
}
