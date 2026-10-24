using System.Numerics;

namespace OneGame.UI;

public partial class Element
{
    private struct CacheEntry
    {
        public byte Kind;
        public float A;
        public float Cb;
        public float Value;
    }

    private CacheEntry[]? _cache;
    private int _cacheCount;
    private int _cacheNext;
    private float _scrollX;
    private float _scrollY;

    // The border box relative to the parent's border box (or to the screen for fixed elements).
    internal float LX;
    internal float LY;
    internal float LW;
    internal float LH;

    // Padding-box size and scrollable content size, in pixels.
    internal float PW;
    internal float PH;
    internal float SW;
    internal float SH;

    // Padding-box origin and content-box origin relative to the border box, and the far border widths.
    internal float PX;
    internal float PY;
    internal float BRw;
    internal float BBw;
    internal float CX;
    internal float CY;
    internal float CW;

    internal float CommitW = float.NaN;
    internal float CommitH = float.NaN;
    internal float CommitCb = float.NaN;
    internal bool HasCommit;
    internal List<TextLine>? TextLines;

    /// <summary>Natural size of the content for leaf elements such as images; layout uses it when no width or height is set. Zero means none.</summary>
    public Vector2 IntrinsicSize { get; set; }

    /// <summary>The border box relative to the parent's border box, as of the last layout.</summary>
    public Rect Bounds => new(LX, LY, LW, LH);

    /// <summary>The border box in screen pixels as of the last layout. Scroll offsets are applied; transforms are not.</summary>
    public Rect AbsoluteBounds
    {
        get
        {
            float x = 0;
            float y = 0;
            for (var e = this; e is not null; e = e._parent)
            {
                x += e.LX;
                y += e.LY;
                if (e.Computed.E(StyleProp.Position) == (int)PositionMode.Fixed)
                {
                    break;
                }

                if (e._parent is { } p)
                {
                    x -= p._scrollX;
                    y -= p._scrollY;
                }
            }

            return new Rect(x, y, LW, LH);
        }
    }

    /// <summary>Horizontal scroll offset in pixels, clamped to the scrollable range.</summary>
    public float ScrollX
    {
        get => _scrollX;
        set => ScrollTo(value, _scrollY);
    }

    /// <summary>Vertical scroll offset in pixels, clamped to the scrollable range.</summary>
    public float ScrollY
    {
        get => _scrollY;
        set => ScrollTo(_scrollX, value);
    }

    /// <summary>The size of the scrollable content, which is at least the padding box.</summary>
    public Vector2 ScrollSize => new(SW, SH);

    /// <summary>The largest valid scroll offsets.</summary>
    public Vector2 MaxScroll => new(MathF.Max(0, SW - PW), MathF.Max(0, SH - PH));

    /// <summary>Sets the scroll offsets, clamped to the scrollable range. Only elements with a scrolling <c>overflow</c> scroll.</summary>
    public void ScrollTo(float x, float y)
    {
        var max = MaxScroll;
        x = Math.Clamp(x, 0, max.X);
        y = Math.Clamp(y, 0, max.Y);
        if (x != _scrollX || y != _scrollY)
        {
            _scrollX = x;
            _scrollY = y;
            MarkPaint();
        }
    }

    internal void ClampScroll()
    {
        var max = MaxScroll;
        _scrollX = Math.Clamp(_scrollX, 0, max.X);
        _scrollY = Math.Clamp(_scrollY, 0, max.Y);
    }

    internal bool Scrolls(bool horizontal)
    {
        var o = Computed.E(horizontal ? StyleProp.OverflowX : StyleProp.OverflowY);
        return o != (int)OverflowMode.Visible;
    }

    internal bool InLayout => _visible && Computed.E(StyleProp.Display) == (int)DisplayMode.Flex;

    internal bool TryGetCache(byte kind, float a, float cb, out float value)
    {
        if (_cache is not null)
        {
            for (var i = 0; i < _cacheCount; i++)
            {
                ref var e = ref _cache[i];
                if (e.Kind == kind && e.A.Equals(a) && e.Cb.Equals(cb))
                {
                    value = e.Value;
                    return true;
                }
            }
        }

        value = 0;
        return false;
    }

    internal void PutCache(byte kind, float a, float cb, float value)
    {
        _cache ??= new CacheEntry[8];
        if (_cacheCount < _cache.Length)
        {
            _cache[_cacheCount++] = new CacheEntry { Kind = kind, A = a, Cb = cb, Value = value };
            return;
        }

        _cache[_cacheNext] = new CacheEntry { Kind = kind, A = a, Cb = cb, Value = value };
        _cacheNext = (_cacheNext + 1) % _cache.Length;
    }

    internal void ClearCache()
    {
        _cacheCount = 0;
        _cacheNext = 0;
    }
}
