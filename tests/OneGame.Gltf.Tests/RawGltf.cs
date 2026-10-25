using System.Buffers.Binary;
using System.Text.Json;

namespace OneGame.Gltf.Tests;

/// <summary>
/// A tiny independent reader used to state expectations: it parses the JSON with System.Text.Json and reads float accessors straight
/// from the buffer bytes, sharing no code with the importer.
/// </summary>
internal sealed class RawGltf : IDisposable
{
    private readonly JsonDocument _doc;
    private readonly byte[][] _buffers;

    internal RawGltf(string path)
    {
        var bytes = File.ReadAllBytes(path);
        byte[]? bin = null;
        ReadOnlyMemory<byte> json = bytes;
        if (bytes.Length > 20 && bytes[0] == 'g' && bytes[1] == 'l' && bytes[2] == 'T' && bytes[3] == 'F')
        {
            var jsonLength = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(12));
            json = bytes.AsMemory(20, jsonLength);
            var binStart = 20 + jsonLength;
            if (binStart + 8 <= bytes.Length)
            {
                var binLength = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(binStart));
                bin = bytes.AsSpan(binStart + 8, binLength).ToArray();
            }
        }

        _doc = JsonDocument.Parse(json);
        Root = _doc.RootElement;
        var dir = Path.GetDirectoryName(path)!;
        var list = new List<byte[]>();
        if (Root.TryGetProperty("buffers", out var buffers))
        {
            foreach (var b in buffers.EnumerateArray())
            {
                if (!b.TryGetProperty("uri", out var uri))
                {
                    list.Add(bin!);
                }
                else if (uri.GetString()!.StartsWith("data:", StringComparison.Ordinal))
                {
                    var s = uri.GetString()!;
                    list.Add(Convert.FromBase64String(s[(s.IndexOf(',', StringComparison.Ordinal) + 1)..]));
                }
                else
                {
                    list.Add(File.ReadAllBytes(Path.Combine(dir, Uri.UnescapeDataString(uri.GetString()!))));
                }
            }
        }

        _buffers = [.. list];
    }

    internal JsonElement Root { get; }

    internal int Count(string array) => Root.TryGetProperty(array, out var a) ? a.GetArrayLength() : 0;

    /// <summary>Reads a FLOAT accessor (SCALAR, VECn or MATn) as doubles, honouring byteStride and the sparse substitution.</summary>
    internal double[] Floats(int accessor)
    {
        var a = Root.GetProperty("accessors")[accessor];
        var count = a.GetProperty("count").GetInt32();
        var comps = a.GetProperty("type").GetString() switch { "SCALAR" => 1, "VEC2" => 2, "VEC3" => 3, "VEC4" => 4, "MAT4" => 16, _ => throw new NotSupportedException() };
        Assert.Equal(5126, a.GetProperty("componentType").GetInt32());
        var result = new double[count * comps];
        if (a.TryGetProperty("bufferView", out var viewIndex))
        {
            var view = Root.GetProperty("bufferViews")[viewIndex.GetInt32()];
            var buffer = _buffers[view.GetProperty("buffer").GetInt32()];
            var start = (view.TryGetProperty("byteOffset", out var vo) ? vo.GetInt32() : 0) + (a.TryGetProperty("byteOffset", out var ao) ? ao.GetInt32() : 0);
            var stride = view.TryGetProperty("byteStride", out var vs) ? vs.GetInt32() : comps * 4;
            for (var i = 0; i < count; i++)
            {
                for (var c = 0; c < comps; c++)
                {
                    result[(i * comps) + c] = BinaryPrimitives.ReadSingleLittleEndian(buffer.AsSpan(start + (i * stride) + (c * 4)));
                }
            }
        }

        if (a.TryGetProperty("sparse", out var sparse))
        {
            var n = sparse.GetProperty("count").GetInt32();
            var iv = sparse.GetProperty("indices");
            var vv = sparse.GetProperty("values");
            var ivw = Root.GetProperty("bufferViews")[iv.GetProperty("bufferView").GetInt32()];
            var vvw = Root.GetProperty("bufferViews")[vv.GetProperty("bufferView").GetInt32()];
            var ibuf = _buffers[ivw.GetProperty("buffer").GetInt32()];
            var vbuf = _buffers[vvw.GetProperty("buffer").GetInt32()];
            var istart = (ivw.TryGetProperty("byteOffset", out var a1) ? a1.GetInt32() : 0) + (iv.TryGetProperty("byteOffset", out var a2) ? a2.GetInt32() : 0);
            var vstart = (vvw.TryGetProperty("byteOffset", out var b1) ? b1.GetInt32() : 0) + (vv.TryGetProperty("byteOffset", out var b2) ? b2.GetInt32() : 0);
            var itype = iv.GetProperty("componentType").GetInt32();
            for (var k = 0; k < n; k++)
            {
                var target = itype switch
                {
                    5121 => ibuf[istart + k],
                    5123 => BinaryPrimitives.ReadUInt16LittleEndian(ibuf.AsSpan(istart + (k * 2))),
                    _ => (int)BinaryPrimitives.ReadUInt32LittleEndian(ibuf.AsSpan(istart + (k * 4))),
                };
                for (var c = 0; c < comps; c++)
                {
                    result[(target * comps) + c] = BinaryPrimitives.ReadSingleLittleEndian(vbuf.AsSpan(vstart + (((k * comps) + c) * 4)));
                }
            }
        }

        return result;
    }

    public void Dispose() => _doc.Dispose();
}

/// <summary>4x4 matrices in the glTF (column-vector) convention with plain double arithmetic, for expected values.</summary>
internal static class D4
{
    internal static double[,] Identity()
    {
        var m = new double[4, 4];
        for (var i = 0; i < 4; i++)
        {
            m[i, i] = 1;
        }

        return m;
    }

    internal static double[,] Mul(double[,] a, double[,] b)
    {
        var r = new double[4, 4];
        for (var i = 0; i < 4; i++)
        {
            for (var j = 0; j < 4; j++)
            {
                for (var k = 0; k < 4; k++)
                {
                    r[i, j] += a[i, k] * b[k, j];
                }
            }
        }

        return r;
    }

    /// <summary>T * R * S for column vectors, from a translation, a quaternion (x, y, z, w) and a scale.</summary>
    internal static double[,] Trs(double[] t, double[] q, double[] s)
    {
        double x = q[0], y = q[1], z = q[2], w = q[3];
        var n = Math.Sqrt((x * x) + (y * y) + (z * z) + (w * w));
        x /= n; y /= n; z /= n; w /= n;
        var m = Identity();
        m[0, 0] = (1 - (2 * ((y * y) + (z * z)))) * s[0];
        m[0, 1] = 2 * ((x * y) - (z * w)) * s[1];
        m[0, 2] = 2 * ((x * z) + (y * w)) * s[2];
        m[1, 0] = 2 * ((x * y) + (z * w)) * s[0];
        m[1, 1] = (1 - (2 * ((x * x) + (z * z)))) * s[1];
        m[1, 2] = 2 * ((y * z) - (x * w)) * s[2];
        m[2, 0] = 2 * ((x * z) - (y * w)) * s[0];
        m[2, 1] = 2 * ((y * z) + (x * w)) * s[1];
        m[2, 2] = (1 - (2 * ((x * x) + (y * y)))) * s[2];
        m[0, 3] = t[0];
        m[1, 3] = t[1];
        m[2, 3] = t[2];
        return m;
    }

    /// <summary>Gauss-Jordan inverse with partial pivoting.</summary>
    internal static double[,] Inverse(double[,] a)
    {
        var m = (double[,])a.Clone();
        var inv = Identity();
        for (var c = 0; c < 4; c++)
        {
            var p = c;
            for (var r = c + 1; r < 4; r++)
            {
                if (Math.Abs(m[r, c]) > Math.Abs(m[p, c]))
                {
                    p = r;
                }
            }

            for (var k = 0; k < 4; k++)
            {
                (m[c, k], m[p, k]) = (m[p, k], m[c, k]);
                (inv[c, k], inv[p, k]) = (inv[p, k], inv[c, k]);
            }

            var d = m[c, c];
            for (var k = 0; k < 4; k++)
            {
                m[c, k] /= d;
                inv[c, k] /= d;
            }

            for (var r = 0; r < 4; r++)
            {
                if (r == c)
                {
                    continue;
                }

                var f = m[r, c];
                for (var k = 0; k < 4; k++)
                {
                    m[r, k] -= f * m[c, k];
                    inv[r, k] -= f * inv[c, k];
                }
            }
        }

        return inv;
    }

    /// <summary>The importer's row-vector matrix as a column-vector matrix (its transpose).</summary>
    internal static double[,] FromNumerics(System.Numerics.Matrix4x4 m)
    {
        var r = new double[4, 4];
        r[0, 0] = m.M11; r[0, 1] = m.M21; r[0, 2] = m.M31; r[0, 3] = m.M41;
        r[1, 0] = m.M12; r[1, 1] = m.M22; r[1, 2] = m.M32; r[1, 3] = m.M42;
        r[2, 0] = m.M13; r[2, 1] = m.M23; r[2, 2] = m.M33; r[2, 3] = m.M43;
        r[3, 0] = m.M14; r[3, 1] = m.M24; r[3, 2] = m.M34; r[3, 3] = m.M44;
        return r;
    }

    /// <summary>A glTF column-major array of 16 as a column-vector matrix.</summary>
    internal static double[,] FromColumnMajor(ReadOnlySpan<double> a)
    {
        var r = new double[4, 4];
        for (var c = 0; c < 4; c++)
        {
            for (var row = 0; row < 4; row++)
            {
                r[row, c] = a[(c * 4) + row];
            }
        }

        return r;
    }

    internal static double MaxDifference(double[,] a, double[,] b)
    {
        var d = 0.0;
        for (var i = 0; i < 4; i++)
        {
            for (var j = 0; j < 4; j++)
            {
                d = Math.Max(d, Math.Abs(a[i, j] - b[i, j]));
            }
        }

        return d;
    }
}
