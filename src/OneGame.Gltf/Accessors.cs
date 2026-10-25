using System.Buffers.Binary;
using System.Globalization;

namespace OneGame.Gltf;

/// <summary>Buffers, buffer views and accessors of one file, with every read bounds-checked in 64-bit arithmetic.</summary>
internal sealed class Accessors
{
    // Offsets are bounded well below long.MaxValue so sums of a few of them cannot overflow.
    private const long MaxOffset = 1L << 40;
    private const int Byte = 5120, UByte = 5121, Short = 5122, UShort = 5123, UInt = 5125, Float = 5126;

    private readonly ReadOnlyMemory<byte>[] _buffers;
    private readonly BufferView[] _views;
    private readonly Accessor[] _accessors;
    private readonly GltfLimits _limits;
    private readonly Budget _budget;

    private readonly record struct BufferView(int Buffer, long Offset, long Length, int Stride);

    private readonly record struct Sparse(int Count, int IndexView, long IndexOffset, int IndexType, int ValueView, long ValueOffset);

    private readonly record struct Accessor(int View, long Offset, int ComponentType, bool Normalized, int Count, int Components, int Columns, Sparse? Sparse);

    internal Accessors(ReadOnlyMemory<byte>[] buffers, JObj root, GltfLimits limits, Budget budget)
    {
        _buffers = buffers;
        _limits = limits;
        _budget = budget;
        var viewCount = root.ArrayLength("bufferViews", limits.MaxBufferViews);
        _views = new BufferView[viewCount];
        for (var i = 0; i < viewCount; i++)
        {
            var v = root.Item("bufferViews", i);
            var buffer = v.ReqRef("buffer", buffers.Length);
            var offset = v.Long("byteOffset", 0, 0, MaxOffset);
            var length = v.ReqLong("byteLength", 1, MaxOffset);
            var stride = v.Int("byteStride", 0, 0, 252);
            if (stride != 0 && (stride < 4 || (stride & 3) != 0))
            {
                throw v.Error("schema", "byteStride", "must be a multiple of 4 between 4 and 252.");
            }

            // glTF: a view lies inside its buffer. Offsets are at most 2^53 so the sum cannot overflow a long.
            if (offset > buffers[buffer].Length || length > buffers[buffer].Length - offset)
            {
                throw v.Error("buffer_range", "byteLength", $"reaches byte {(offset + length).ToString(CultureInfo.InvariantCulture)} of a {buffers[buffer].Length.ToString(CultureInfo.InvariantCulture)} byte buffer.");
            }

            _views[i] = new BufferView(buffer, offset, length, stride);
        }

        var count = root.ArrayLength("accessors", limits.MaxAccessors);
        _accessors = new Accessor[count];
        for (var i = 0; i < count; i++)
        {
            _accessors[i] = ParseAccessor(root.Item("accessors", i));
        }
    }

    internal int Count => _accessors.Length;

    internal int ViewCount => _views.Length;

    /// <summary>The bytes of a buffer view (images stored in a view).</summary>
    internal ReadOnlyMemory<byte> ViewMemory(int view) => _buffers[_views[view].Buffer].Slice((int)_views[view].Offset, (int)_views[view].Length);

    internal int ElementCount(int accessor) => _accessors[accessor].Count;

    private Accessor ParseAccessor(JObj a)
    {
        var view = a.Ref("bufferView", _views.Length);
        var offset = a.Long("byteOffset", 0, 0, MaxOffset);
        var ct = a.ReqInt("componentType");
        if (ct is not (Byte or UByte or Short or UShort or UInt or Float))
        {
            throw a.Error("accessor_invalid", "componentType", $"{ct.ToString(CultureInfo.InvariantCulture)} is not a glTF component type.");
        }

        var count = a.ReqInt("count", 1);
        if (count > _limits.MaxElementsPerAccessor)
        {
            throw a.Error("limit_exceeded", "count", $"is {count.ToString(CultureInfo.InvariantCulture)}; the limit is {_limits.MaxElementsPerAccessor.ToString(CultureInfo.InvariantCulture)}.");
        }

        int components, columns;
        switch (a.Str("type"))
        {
            case "SCALAR": components = 1; columns = 1; break;
            case "VEC2": components = 2; columns = 1; break;
            case "VEC3": components = 3; columns = 1; break;
            case "VEC4": components = 4; columns = 1; break;
            case "MAT2": components = 4; columns = 2; break;
            case "MAT3": components = 9; columns = 3; break;
            case "MAT4": components = 16; columns = 4; break;
            default: throw a.Error("accessor_invalid", "type", "must be SCALAR, VEC2, VEC3, VEC4, MAT2, MAT3 or MAT4.");
        }

        if (ct == UInt && components != 1)
        {
            // glTF: 32-bit unsigned integers are only for indices.
            throw a.Error("accessor_invalid", "componentType", "UNSIGNED_INT is only valid for SCALAR accessors.");
        }

        Sparse? sparse = null;
        if (a.Obj("sparse") is { } sp)
        {
            var n = sp.ReqInt("count", 1, count);
            var idx = sp.Obj("indices") ?? throw sp.Error("schema", "indices", "is required.");
            var vals = sp.Obj("values") ?? throw sp.Error("schema", "values", "is required.");
            var it = idx.ReqInt("componentType");
            if (it is not (UByte or UShort or UInt))
            {
                throw idx.Error("sparse_invalid", "componentType", "must be UNSIGNED_BYTE, UNSIGNED_SHORT or UNSIGNED_INT.");
            }

            sparse = new Sparse(n, idx.ReqRef("bufferView", _views.Length), idx.Long("byteOffset", 0, 0, MaxOffset), it, vals.ReqRef("bufferView", _views.Length), vals.Long("byteOffset", 0, 0, MaxOffset));
        }

        var acc = new Accessor(view, offset, ct, a.Bool("normalized", false), count, components, columns, sparse);
        if (view >= 0)
        {
            CheckRange(a, acc);
        }

        if (sparse is { } s)
        {
            CheckSparse(a, acc, s);
        }

        return acc;
    }

    private static int ComponentSize(int ct) => ct switch { Byte or UByte => 1, Short or UShort => 2, _ => 4 };

    /// <summary>Bytes of one element, with matrix columns padded to 4 bytes (glTF section Accessors, matrix alignment).</summary>
    private static int ElementSize(Accessor a)
    {
        var cs = ComponentSize(a.ComponentType);
        if (a.Columns == 1)
        {
            return a.Components * cs;
        }

        var rows = a.Components / a.Columns;
        var column = (rows * cs + 3) / 4 * 4;
        return column * a.Columns;
    }

    private int StrideOf(Accessor a) => _views[a.View].Stride != 0 ? _views[a.View].Stride : ElementSize(a);

    private void CheckRange(JObj json, Accessor a)
    {
        var view = _views[a.View];
        var size = ElementSize(a);
        var stride = StrideOf(a);
        if (stride < size)
        {
            throw json.Error("accessor_invalid", "bufferView", $"has a byteStride of {stride.ToString(CultureInfo.InvariantCulture)}, smaller than the {size.ToString(CultureInfo.InvariantCulture)} byte element.");
        }

        // The last element ends at offset + (count - 1) * stride + size. Count <= 16M and stride <= 252, so this cannot overflow.
        var end = a.Offset + ((long)(a.Count - 1) * stride) + size;
        if (a.Offset > view.Length || end > view.Length)
        {
            throw json.Error("buffer_range", "count", $"needs {end.ToString(CultureInfo.InvariantCulture)} bytes of a {view.Length.ToString(CultureInfo.InvariantCulture)} byte buffer view.");
        }
    }

    private void CheckSparse(JObj json, Accessor a, Sparse s)
    {
        var idxSize = ComponentSize(s.IndexType);
        var iv = _views[s.IndexView];
        if (s.IndexOffset > iv.Length || (long)s.Count * idxSize > iv.Length - s.IndexOffset)
        {
            throw json.Error("buffer_range", "sparse.indices", "reach outside their buffer view.");
        }

        var vv = _views[s.ValueView];
        var size = ElementSize(a);
        if (s.ValueOffset > vv.Length || (long)s.Count * size > vv.Length - s.ValueOffset)
        {
            throw json.Error("buffer_range", "sparse.values", "reach outside their buffer view.");
        }
    }

    private ReadOnlySpan<byte> ViewBytes(int view) => _buffers[_views[view].Buffer].Span.Slice((int)_views[view].Offset, (int)_views[view].Length);

    /// <summary>Returns the type of an accessor's elements.</summary>
    internal (int ComponentType, int Components, bool Normalized) Describe(int accessor)
    {
        var a = _accessors[accessor];
        return (a.ComponentType, a.Components, a.Normalized);
    }

    /// <summary>Reads all components as floats; normalized integers are mapped to 0..1 or -1..1 (glTF Appendix, normalized conversion).</summary>
    internal float[] ReadFloats(int accessor, string what)
    {
        var a = _accessors[accessor];
        var total = (long)a.Count * a.Components;
        _budget.Charge(total * 4, what);
        var result = new float[total];
        var cs = ComponentSize(a.ComponentType);
        var rows = a.Components / a.Columns;
        var column = a.Columns == 1 ? 0 : (rows * cs + 3) / 4 * 4;
        if (a.View >= 0)
        {
            var data = ViewBytes(a.View);
            var stride = StrideOf(a);
            for (var i = 0; i < a.Count; i++)
            {
                var at = (int)(a.Offset + ((long)i * stride));
                for (var j = 0; j < a.Components; j++)
                {
                    var p = a.Columns == 1 ? at + (j * cs) : at + ((j / rows) * column) + ((j % rows) * cs);
                    result[(i * a.Components) + j] = ToFloat(data, p, a.ComponentType, a.Normalized);
                }
            }
        }

        if (a.Sparse is { } s)
        {
            var indices = SparseIndices(a, s, what);
            var values = ViewBytes(s.ValueView);
            var size = ElementSize(a);
            for (var n = 0; n < s.Count; n++)
            {
                var at = (int)(s.ValueOffset + ((long)n * size));
                for (var j = 0; j < a.Components; j++)
                {
                    var p = a.Columns == 1 ? at + (j * cs) : at + ((j / rows) * column) + ((j % rows) * cs);
                    result[((long)indices[n] * a.Components) + j] = ToFloat(values, p, a.ComponentType, a.Normalized);
                }
            }
        }

        return result;
    }

    /// <summary>Reads all components as unsigned integers (indices, joints). Float accessors are refused.</summary>
    internal uint[] ReadUInts(int accessor, string what)
    {
        var a = _accessors[accessor];
        if (a.ComponentType is Float or Byte or Short)
        {
            throw new GltfException("accessor_invalid", $"{what}: accessor {accessor.ToString(CultureInfo.InvariantCulture)} must hold unsigned integers.");
        }

        var total = (long)a.Count * a.Components;
        _budget.Charge(total * 4, what);
        var result = new uint[total];
        var cs = ComponentSize(a.ComponentType);
        if (a.View >= 0)
        {
            var data = ViewBytes(a.View);
            var stride = StrideOf(a);
            for (var i = 0; i < a.Count; i++)
            {
                var at = (int)(a.Offset + ((long)i * stride));
                for (var j = 0; j < a.Components; j++)
                {
                    result[(i * a.Components) + j] = ToUInt(data, at + (j * cs), a.ComponentType);
                }
            }
        }

        if (a.Sparse is { } s)
        {
            var indices = SparseIndices(a, s, what);
            var values = ViewBytes(s.ValueView);
            var size = ElementSize(a);
            for (var n = 0; n < s.Count; n++)
            {
                var at = (int)(s.ValueOffset + ((long)n * size));
                for (var j = 0; j < a.Components; j++)
                {
                    result[((long)indices[n] * a.Components) + j] = ToUInt(values, at + (j * cs), a.ComponentType);
                }
            }
        }

        return result;
    }

    private uint[] SparseIndices(Accessor a, Sparse s, string what)
    {
        _budget.Charge((long)s.Count * 4, what);
        var indices = new uint[s.Count];
        var data = ViewBytes(s.IndexView);
        var size = ComponentSize(s.IndexType);
        long previous = -1;
        for (var n = 0; n < s.Count; n++)
        {
            var v = ToUInt(data, (int)(s.IndexOffset + ((long)n * size)), s.IndexType);
            // glTF: sparse indices strictly increase and stay below the accessor count.
            if (v >= (uint)a.Count || v <= previous)
            {
                throw new GltfException("sparse_invalid", $"{what}: sparse index {n.ToString(CultureInfo.InvariantCulture)} ({v.ToString(CultureInfo.InvariantCulture)}) is out of order or beyond the accessor's {a.Count.ToString(CultureInfo.InvariantCulture)} elements.");
            }

            previous = v;
            indices[n] = v;
        }

        return indices;
    }

    private static float ToFloat(ReadOnlySpan<byte> d, int p, int ct, bool normalized)
    {
        switch (ct)
        {
            case Float:
                return BinaryPrimitives.ReadSingleLittleEndian(d[p..]);
            case UByte:
                return normalized ? d[p] / 255f : d[p];
            case Byte:
                return normalized ? Math.Max((sbyte)d[p] / 127f, -1f) : (sbyte)d[p];
            case UShort:
                {
                    var v = BinaryPrimitives.ReadUInt16LittleEndian(d[p..]);
                    return normalized ? v / 65535f : v;
                }

            case Short:
                {
                    var v = BinaryPrimitives.ReadInt16LittleEndian(d[p..]);
                    return normalized ? Math.Max(v / 32767f, -1f) : v;
                }

            default:
                return BinaryPrimitives.ReadUInt32LittleEndian(d[p..]);
        }
    }

    private static uint ToUInt(ReadOnlySpan<byte> d, int p, int ct) => ct switch
    {
        UByte => d[p],
        UShort => BinaryPrimitives.ReadUInt16LittleEndian(d[p..]),
        _ => BinaryPrimitives.ReadUInt32LittleEndian(d[p..]),
    };
}
