using System.Globalization;

namespace OneGame.Gltf;

/// <summary>Mesh processing on imported primitives: topology expansion, vertex duplication, flat normals and tangents.</summary>
internal static class MeshOps
{
    /// <summary>Turns strips, fans and loops into lists (glTF section Primitives, mode). Returns the new index list and topology.</summary>
    internal static uint[] Expand(uint[] source, int mode, GltfLimits limits, Budget budget, out GltfTopology topology, out int dropped)
    {
        dropped = 0;
        long n = source.Length;
        long count;
        switch (mode)
        {
            case 0:
                topology = GltfTopology.Points;
                count = n;
                break;
            case 1:
                topology = GltfTopology.Lines;
                count = n / 2 * 2;
                dropped = (int)(n - count);
                break;
            case 2:
                topology = GltfTopology.Lines;
                count = n >= 2 ? n * 2 : 0;
                break;
            case 3:
                topology = GltfTopology.Lines;
                count = n >= 2 ? (n - 1) * 2 : 0;
                break;
            case 4:
                topology = GltfTopology.Triangles;
                count = n / 3 * 3;
                dropped = (int)(n - count);
                break;
            default:
                topology = GltfTopology.Triangles;
                count = n >= 3 ? (n - 2) * 3 : 0;
                break;
        }

        if (count > limits.MaxIndicesPerPrimitive)
        {
            throw new GltfException("limit_exceeded", $"A primitive expands to {count.ToString(CultureInfo.InvariantCulture)} indices; the limit is {limits.MaxIndicesPerPrimitive.ToString(CultureInfo.InvariantCulture)}.");
        }

        if (mode is 0 or 1 or 4)
        {
            if (count == n)
            {
                return source;
            }

            budget.Charge(count * 4, "indices");
            return source.AsSpan(0, (int)count).ToArray();
        }

        budget.Charge(count * 4, "indices");
        var result = new uint[count];
        var o = 0;
        switch (mode)
        {
            case 2:
                for (var i = 0; i < n; i++)
                {
                    result[o++] = source[i];
                    result[o++] = source[(i + 1) % n];
                }

                break;
            case 3:
                for (var i = 0; i + 1 < n; i++)
                {
                    result[o++] = source[i];
                    result[o++] = source[i + 1];
                }

                break;
            case 5:
                // Every other triangle swaps two vertices to keep the winding (glTF: triangle strip).
                for (var i = 0; i + 2 < n; i++)
                {
                    if ((i & 1) == 0)
                    {
                        result[o++] = source[i];
                        result[o++] = source[i + 1];
                    }
                    else
                    {
                        result[o++] = source[i + 1];
                        result[o++] = source[i];
                    }

                    result[o++] = source[i + 2];
                }

                break;
            default:
                // Triangle fan: (i + 1, i + 2, 0) in the specification, the same cyclic order as (0, i + 1, i + 2).
                for (var i = 0; i + 2 < n; i++)
                {
                    result[o++] = source[0];
                    result[o++] = source[i + 1];
                    result[o++] = source[i + 2];
                }

                break;
        }

        return result;
    }

    private static T[]? Gather<T>(T[]? a, int[] src)
    {
        if (a is null)
        {
            return null;
        }

        var r = new T[src.Length];
        for (var i = 0; i < src.Length; i++)
        {
            r[i] = a[src[i]];
        }

        return r;
    }

    /// <summary>Bytes of one vertex across all streams, for charging the budget before a vertex copy.</summary>
    private static long BytesPerVertex(GltfPrimitive p)
    {
        long b = 24;
        b += p.Tangents is null ? 0 : 16;
        b += p.Uv0 is null ? 0 : 8;
        b += p.Uv1 is null ? 0 : 8;
        b += p.Colors is null ? 0 : 16;
        b += p.Joints is null ? 0 : 8;
        b += p.Weights is null ? 0 : 16;
        foreach (var t in p.MorphTargets)
        {
            b += (t.Positions is null ? 0 : 12) + (t.Normals is null ? 0 : 12) + (t.Tangents is null ? 0 : 12);
        }

        return b;
    }

    /// <summary>Rebuilds every per-vertex stream so that new vertex i is old vertex <paramref name="src"/>[i].</summary>
    internal static void Remap(GltfPrimitive p, int[] src, GltfLimits limits, Budget budget)
    {
        if (src.Length > limits.MaxVerticesPerPrimitive)
        {
            throw new GltfException("limit_exceeded", $"A primitive grows to {src.Length.ToString(CultureInfo.InvariantCulture)} vertices; the limit is {limits.MaxVerticesPerPrimitive.ToString(CultureInfo.InvariantCulture)}.");
        }

        budget.Charge(src.Length * BytesPerVertex(p), "vertex copy");
        p.Positions = Gather(p.Positions, src)!;
        p.Normals = Gather(p.Normals, src)!;
        p.Tangents = Gather(p.Tangents, src);
        p.Uv0 = Gather(p.Uv0, src);
        p.Uv1 = Gather(p.Uv1, src);
        p.Colors = Gather(p.Colors, src);
        p.Joints = Gather(p.Joints, src);
        p.Weights = Gather(p.Weights, src);
        foreach (var t in p.MorphTargets)
        {
            t.Positions = Gather(t.Positions, src);
            t.Normals = Gather(t.Normals, src);
            t.Tangents = Gather(t.Tangents, src);
        }
    }

    /// <summary>Gives every triangle its own three vertices and the face normal (glTF: flat shading when NORMAL is absent).</summary>
    internal static void FlatNormals(GltfPrimitive p, GltfLimits limits, Budget budget)
    {
        var idx = p.Indices;
        var src = new int[idx.Length];
        for (var i = 0; i < idx.Length; i++)
        {
            src[i] = (int)idx[i];
        }

        var oldPositions = p.Positions;
        Remap(p, src, limits, budget);
        var normals = p.Normals;
        for (var t = 0; t + 2 < idx.Length; t += 3)
        {
            var a = oldPositions[src[t]];
            var n = Vector3.Cross(oldPositions[src[t + 1]] - a, oldPositions[src[t + 2]] - a);
            var len = n.Length();
            n = len > 1e-30f && float.IsFinite(len) ? n / len : Vector3.UnitZ;
            normals[t] = normals[t + 1] = normals[t + 2] = n;
        }

        budget.Charge(idx.Length * 4L, "indices");
        var fresh = new uint[idx.Length];
        for (var i = 0; i < fresh.Length; i++)
        {
            fresh[i] = (uint)i;
        }

        p.Indices = fresh;
        p.NormalsGenerated = true;
        p.HasNormals = true;
    }

    private readonly record struct CornerKey(Vector3 Position, Vector3 Normal, Vector2 Uv, int Orientation);

    /// <summary>
    /// Generates tangents with the method of the MikkTSpace description: a tangent and bitangent per triangle from the position and
    /// UV derivatives, accumulated per vertex weighted by the corner angle over all corners that share position, normal, UV and UV
    /// orientation, then orthogonalized against the normal. Corners of mirrored UV orientation get their own vertex. The UV's V axis is
    /// flipped because glTF's texture origin is the top left. The sign follows glTF: bitangent = cross(normal, tangent) * w.
    /// </summary>
    internal static void GenerateTangents(GltfPrimitive p, Vector2[] uv, GltfLimits limits, Budget budget)
    {
        var idx = p.Indices;
        var vertexCount = p.Positions.Length;
        var groupOf = new int[idx.Length];
        var keys = new Dictionary<CornerKey, int>();
        var sumT = new List<Vector3>();
        var sumB = new List<Vector3>();
        var firstVertex = new List<int>();
        Array.Fill(groupOf, -1);
        var pos = p.Positions;
        var nor = p.Normals;
        for (var t = 0; t + 2 < idx.Length; t += 3)
        {
            int i0 = (int)idx[t], i1 = (int)idx[t + 1], i2 = (int)idx[t + 2];
            var e1 = pos[i1] - pos[i0];
            var e2 = pos[i2] - pos[i0];
            var du1 = uv[i1].X - uv[i0].X;
            var dv1 = (1 - uv[i1].Y) - (1 - uv[i0].Y);
            var du2 = uv[i2].X - uv[i0].X;
            var dv2 = (1 - uv[i2].Y) - (1 - uv[i0].Y);
            var det = (du1 * dv2) - (du2 * dv1);
            if (!float.IsFinite(det) || MathF.Abs(det) < 1e-20f || Vector3.Cross(e1, e2).LengthSquared() < 1e-30f)
            {
                continue;
            }

            var tangent = ((e1 * dv2) - (e2 * dv1)) / det;
            var bitangent = ((e2 * du1) - (e1 * du2)) / det;
            if (!float.IsFinite(tangent.LengthSquared()) || !float.IsFinite(bitangent.LengthSquared()))
            {
                continue;
            }

            var orientation = det > 0 ? 1 : -1;
            for (var k = 0; k < 3; k++)
            {
                var v = (int)idx[t + k];
                var vPrev = (int)idx[t + ((k + 2) % 3)];
                var vNext = (int)idx[t + ((k + 1) % 3)];
                var a = Vector3.Normalize(pos[vNext] - pos[v]);
                var b = Vector3.Normalize(pos[vPrev] - pos[v]);
                var angle = MathF.Acos(Math.Clamp(Vector3.Dot(a, b), -1f, 1f));
                if (!float.IsFinite(angle))
                {
                    angle = 0;
                }

                // Each corner contributes the triangle's direction projected into the tangent plane of the vertex normal and normalized,
                // weighted by the corner angle only, so a large triangle does not outvote its small neighbours.
                var tp = Project(tangent, nor[v]);
                var bp = Project(bitangent, nor[v]);
                if (tp.LengthSquared() == 0 || bp.LengthSquared() == 0)
                {
                    continue;
                }

                var key = new CornerKey(pos[v], nor[v], uv[v], orientation);
                if (!keys.TryGetValue(key, out var g))
                {
                    g = sumT.Count;
                    keys[key] = g;
                    sumT.Add(Vector3.Zero);
                    sumB.Add(Vector3.Zero);
                    firstVertex.Add(v);
                }

                sumT[g] += tp * angle;
                sumB[g] += bp * angle;
                groupOf[t + k] = g;
            }
        }

        // One output vertex per group: the first group to use an original vertex keeps it, later ones get a copy.
        var owner = new int[vertexCount];
        Array.Fill(owner, -1);
        var outVertex = new int[sumT.Count];
        var extra = new List<int>();
        for (var g = 0; g < outVertex.Length; g++)
        {
            var v = firstVertex[g];
            if (owner[v] < 0)
            {
                owner[v] = g;
                outVertex[g] = v;
            }
            else
            {
                outVertex[g] = vertexCount + extra.Count;
                extra.Add(v);
            }
        }

        if (extra.Count > 0)
        {
            var src = new int[vertexCount + extra.Count];
            for (var i = 0; i < vertexCount; i++)
            {
                src[i] = i;
            }

            for (var i = 0; i < extra.Count; i++)
            {
                src[vertexCount + i] = extra[i];
            }

            Remap(p, src, limits, budget);
        }

        for (var c = 0; c < idx.Length; c++)
        {
            if (groupOf[c] >= 0)
            {
                idx[c] = (uint)outVertex[groupOf[c]];
            }
        }

        var total = p.Positions.Length;
        budget.Charge(total * 16L, "tangents");
        var tangents = new Vector4[total];
        for (var i = 0; i < total; i++)
        {
            tangents[i] = Fallback(p.Normals[i]);
        }

        for (var g = 0; g < outVertex.Length; g++)
        {
            var v = outVertex[g];
            var n = p.Normals[v];
            var t = sumT[g] - (n * Vector3.Dot(n, sumT[g]));
            if (t.LengthSquared() < 1e-30f || !float.IsFinite(t.LengthSquared()))
            {
                continue;
            }

            t = Vector3.Normalize(t);
            // glTF: bitangent = cross(normal, tangent) * w, and the bitangent points along the image's up direction.
            var w = Vector3.Dot(Vector3.Cross(n, t), sumB[g]) < 0 ? -1f : 1f;
            tangents[v] = new Vector4(t, w);
        }

        p.Tangents = tangents;
        p.TangentsGenerated = true;
    }

    /// <summary>The part of a vector perpendicular to a normal, normalized; zero when nothing is left.</summary>
    private static Vector3 Project(Vector3 v, Vector3 n)
    {
        var p = v - (n * Vector3.Dot(n, v));
        var len = p.Length();
        return len > 1e-20f && float.IsFinite(len) ? p / len : Vector3.Zero;
    }

    private static Vector4 Fallback(Vector3 n)
    {
        // Any unit vector perpendicular to the normal, for vertices no usable triangle touches.
        var axis = MathF.Abs(n.X) < 0.9f ? Vector3.UnitX : Vector3.UnitY;
        var t = axis - (n * Vector3.Dot(n, axis));
        t = t.LengthSquared() > 1e-20f ? Vector3.Normalize(t) : Vector3.UnitX;
        return new Vector4(t, 1);
    }

    internal static GltfBounds ComputeBounds(Vector3[] positions)
    {
        if (positions.Length == 0)
        {
            return default;
        }

        var min = new Vector3(float.PositiveInfinity);
        var max = new Vector3(float.NegativeInfinity);
        foreach (var v in positions)
        {
            if (float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z))
            {
                min = Vector3.Min(min, v);
                max = Vector3.Max(max, v);
            }
        }

        return float.IsFinite(min.X) ? new GltfBounds(min, max) : default;
    }
}
