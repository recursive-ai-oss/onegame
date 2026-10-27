using System.Numerics;
using OneGame.Gpu;
using OneRender.Painting;

namespace OneGame.Pages;

/// <summary>
/// One row of 1024 texels per distinct gradient, holding its colours interpolated linearly in premultiplied sRGB (CSS Images 3 §3.5.2) between
/// the first and the last stop. The shader maps a pixel to a position on that row, so any number of stops, hard edges and repeats cost one lookup.
/// </summary>
internal sealed class GradientRamps : IDisposable
{
    public const int Width = 1024;
    public const int Rows = 256;

    private readonly PageGpu _gpu;
    private readonly Dictionary<ulong, int> _rows = [];
    private readonly byte[] _scratch = new byte[Width * 4];
    private int _next;
    private int _frame = -1;

    public GradientRamps(PageGpu gpu)
    {
        _gpu = gpu;
        Texture = gpu.Device.CreateTexture(new TextureDesc(Width, Rows, GpuFormat.Rgba8Unorm, TextureUsage.Sampled | TextureUsage.CopyDestination) { Label = "page gradients" });
        Group = gpu.Sampled(Texture);
    }

    public GpuTexture Texture { get; }

    public GpuBindGroup Group { get; }

    public int RowsInUse => _next;

    /// <summary>Starts a frame; a table that is nearly full starts over, because the previous frame's plans are done with their rows.</summary>
    public void BeginFrame(int frame)
    {
        if (frame == _frame)
        {
            return;
        }

        _frame = frame;
        if (_next > Rows - 64)
        {
            _rows.Clear();
            _next = 0;
        }
    }

    /// <summary>Finds or builds the row of a gradient. False when the table is full (the caller then paints the last stop).</summary>
    public bool TryRow(in GradientView gradient, out float v, out float first, out float last)
    {
        v = first = last = 0;
        var count = gradient.StopCount;
        if (count < 1)
        {
            return false;
        }

        first = gradient.GetStop(0).Offset;
        last = gradient.GetStop(count - 1).Offset;
        var key = Hash(gradient, count);
        if (!_rows.TryGetValue(key, out var row))
        {
            if (_next >= Rows)
            {
                return false;
            }

            row = _next++;
            Build(gradient, count, first, last);
            _gpu.Device.WriteTexture(Texture, new TextureRegion(0, 0, 0, (uint)row, 0, Width, 1), _scratch);
            _rows[key] = row;
        }

        v = (row + 0.5f) / Rows;
        return true;
    }

    public void Dispose() => Texture.Dispose();

    private static ulong Hash(in GradientView g, int count)
    {
        var h = 14695981039346656037UL;
        for (var i = 0; i < count; i++)
        {
            var s = g.GetStop(i);
            h = Mix(h, BitConverter.SingleToUInt32Bits(s.Offset));
            h = Mix(h, BitConverter.SingleToUInt32Bits(s.Color.R));
            h = Mix(h, BitConverter.SingleToUInt32Bits(s.Color.G));
            h = Mix(h, BitConverter.SingleToUInt32Bits(s.Color.B));
            h = Mix(h, BitConverter.SingleToUInt32Bits(s.Color.A));
        }

        return h;
    }

    private static ulong Mix(ulong h, uint v)
    {
        h ^= v;
        return h * 1099511628211UL;
    }

    private void Build(in GradientView g, int count, float first, float last)
    {
        var range = last - first;
        var k = 0;
        for (var x = 0; x < Width; x++)
        {
            var t = first + ((x + 0.5f) / Width * range);
            // The last stop whose offset is not above t: at equal offsets the later stop wins (a hard edge).
            while (k + 1 < count && g.GetStop(k + 1).Offset <= t)
            {
                k++;
            }

            var a = g.GetStop(k);
            Vector4 c;
            if (k + 1 >= count || t <= a.Offset)
            {
                c = Premul(a.Color);
            }
            else
            {
                var b = g.GetStop(k + 1);
                var span = b.Offset - a.Offset;
                var f = span > 0 ? (t - a.Offset) / span : 0f;
                c = Vector4.Lerp(Premul(a.Color), Premul(b.Color), f);
            }

            var o = x * 4;
            _scratch[o] = PaintColor.ToByte(c.X);
            _scratch[o + 1] = PaintColor.ToByte(c.Y);
            _scratch[o + 2] = PaintColor.ToByte(c.Z);
            _scratch[o + 3] = PaintColor.ToByte(c.W);
            // Walking k forward only works while t grows, which it does.
        }
    }

    private static Vector4 Premul(in PaintColor c) => new(c.R * c.A, c.G * c.A, c.B * c.A, c.A);
}
