namespace OneGame.Gltf;

/// <summary>
/// A line of poses along one value, for example speed: idle at 0, walk at 1, run at 2. Between two neighbours both count in proportion;
/// outside the ends the nearest one counts alone.
/// </summary>
public sealed class BlendSpace1D
{
    private readonly float[] _positions;

    /// <summary>Creates the space from the positions of its samples.</summary>
    /// <param name="positions">Where each sample sits, strictly increasing, at least one.</param>
    /// <exception cref="ArgumentException">The positions are empty or not increasing.</exception>
    public BlendSpace1D(params float[] positions)
    {
        ArgumentNullException.ThrowIfNull(positions);
        if (positions.Length == 0)
        {
            throw new ArgumentException("A blend space needs at least one sample.", nameof(positions));
        }

        for (var i = 1; i < positions.Length; i++)
        {
            if (!(positions[i] > positions[i - 1]))
            {
                throw new ArgumentException("Blend space positions must be strictly increasing.", nameof(positions));
            }
        }

        _positions = (float[])positions.Clone();
    }

    /// <summary>The number of samples.</summary>
    public int Count => _positions.Length;

    /// <summary>Finds the two samples around a value.</summary>
    /// <param name="value">The input value.</param>
    /// <param name="lower">The sample at or below the value.</param>
    /// <param name="upper">The sample above it (equal to <paramref name="lower"/> at the ends).</param>
    /// <param name="t">The weight of <paramref name="upper"/>; <paramref name="lower"/> has 1 - t.</param>
    public void Locate(float value, out int lower, out int upper, out float t)
    {
        var n = _positions.Length;
        if (float.IsNaN(value) || value <= _positions[0])
        {
            lower = upper = 0;
            t = 0f;
            return;
        }

        if (value >= _positions[n - 1])
        {
            lower = upper = n - 1;
            t = 0f;
            return;
        }

        var hi = 1;
        while (_positions[hi] < value)
        {
            hi++;
        }

        // An exact hit on a sample counts that sample alone.
        if (_positions[hi] == value)
        {
            lower = upper = hi;
            t = 0f;
            return;
        }

        lower = hi - 1;
        upper = hi;
        t = (value - _positions[lower]) / (_positions[upper] - _positions[lower]);
    }

    /// <summary>Writes one weight per sample; they add up to 1.</summary>
    /// <param name="value">The input value.</param>
    /// <param name="weights">Receives <see cref="Count"/> weights.</param>
    public void Weights(float value, Span<float> weights)
    {
        weights[.._positions.Length].Clear();
        Locate(value, out var a, out var b, out var t);
        weights[a] += 1f - t;
        weights[b] += t;
    }
}

/// <summary>
/// A plane of poses, for example direction by speed. The samples are joined into triangles once (a Delaunay triangulation); a point inside a
/// triangle blends its three corners by barycentric weights, and a point outside the outline counts as the nearest point on it.
/// </summary>
public sealed class BlendSpace2D
{
    private readonly Vector2[] _points;
    private readonly int[] _tris;

    /// <summary>Creates the space from the positions of its samples.</summary>
    /// <param name="points">Where each sample sits: at least three, not all on one line, no two equal.</param>
    /// <exception cref="ArgumentException">The points cannot form a triangle.</exception>
    public BlendSpace2D(params Vector2[] points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Length < 3)
        {
            throw new ArgumentException("A 2D blend space needs at least three samples.", nameof(points));
        }

        _points = (Vector2[])points.Clone();
        var tris = new List<int>();

        // Brute force: a triple is a triangle when no other point lies inside its circumcircle. Spaces have a handful of samples.
        for (var i = 0; i < points.Length; i++)
        {
            for (var j = i + 1; j < points.Length; j++)
            {
                for (var k = j + 1; k < points.Length; k++)
                {
                    var area = Cross(points[j] - points[i], points[k] - points[i]);
                    if (MathF.Abs(area) < 1e-9f)
                    {
                        continue;
                    }

                    var empty = true;
                    for (var m = 0; m < points.Length && empty; m++)
                    {
                        if (m != i && m != j && m != k && InCircle(points[i], points[j], points[k], points[m], area > 0f))
                        {
                            empty = false;
                        }
                    }

                    if (empty && !Overlaps(tris, points, i, j, k))
                    {
                        tris.Add(i);
                        tris.Add(j);
                        tris.Add(k);
                    }
                }
            }
        }

        if (tris.Count == 0)
        {
            throw new ArgumentException("The blend space points lie on one line; use BlendSpace1D.", nameof(points));
        }

        _tris = [.. tris];
    }

    /// <summary>The number of samples.</summary>
    public int Count => _points.Length;

    /// <summary>The number of triangles.</summary>
    public int TriangleCount => _tris.Length / 3;

    /// <summary>Writes one weight per sample; they add up to 1 and at most three are not zero.</summary>
    /// <param name="point">The input point.</param>
    /// <param name="weights">Receives <see cref="Count"/> weights.</param>
    public void Weights(Vector2 point, Span<float> weights)
    {
        weights[.._points.Length].Clear();
        if (float.IsNaN(point.X) || float.IsNaN(point.Y))
        {
            point = Vector2.Zero;
        }

        var bestDist = float.MaxValue;
        var bestTri = 0;
        Vector3 bestBary = default;
        for (var t = 0; t < _tris.Length; t += 3)
        {
            var a = _points[_tris[t]];
            var b = _points[_tris[t + 1]];
            var c = _points[_tris[t + 2]];
            var bary = Barycentric(point, a, b, c);
            if (bary.X >= -1e-6f && bary.Y >= -1e-6f && bary.Z >= -1e-6f)
            {
                Write(weights, t, bary);
                return;
            }

            // Outside every triangle so far: remember the closest point of this one, and use the best at the end.
            var closest = ClosestOnTriangle(point, a, b, c);
            var d = Vector2.DistanceSquared(point, closest);
            if (d < bestDist)
            {
                bestDist = d;
                bestTri = t;
                bestBary = Barycentric(closest, a, b, c);
            }
        }

        Write(weights, bestTri, bestBary);
    }

    private void Write(Span<float> weights, int t, Vector3 bary)
    {
        var x = Math.Max(0f, bary.X);
        var y = Math.Max(0f, bary.Y);
        var z = Math.Max(0f, bary.Z);
        var sum = x + y + z;
        weights[_tris[t]] += x / sum;
        weights[_tris[t + 1]] += y / sum;
        weights[_tris[t + 2]] += z / sum;
    }

    // Points on one circle (a square, a regular ring) allow several triangulations whose triangles cross; the first found wins, so no area counts twice.
    private static bool Overlaps(List<int> tris, Vector2[] p, int i, int j, int k)
    {
        for (var t = 0; t < tris.Count; t += 3)
        {
            if (!Separated(p[i], p[j], p[k], p[tris[t]], p[tris[t + 1]], p[tris[t + 2]]))
            {
                return true;
            }
        }

        return false;
    }

    // Separating axis test with touching allowed: two triangles that only share an edge or a corner do not overlap.
    private static bool Separated(Vector2 a0, Vector2 a1, Vector2 a2, Vector2 b0, Vector2 b1, Vector2 b2)
    {
        Span<Vector2> a = [a0, a1, a2];
        Span<Vector2> b = [b0, b1, b2];
        for (var pass = 0; pass < 2; pass++)
        {
            var edges = pass == 0 ? a : b;
            for (var e = 0; e < 3; e++)
            {
                var d = edges[(e + 1) % 3] - edges[e];
                var axis = new Vector2(-d.Y, d.X);
                Range(a, axis, out var aMin, out var aMax);
                Range(b, axis, out var bMin, out var bMax);
                var eps = 1e-6f * axis.Length();
                if (aMax <= bMin + eps || bMax <= aMin + eps)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void Range(Span<Vector2> poly, Vector2 axis, out float min, out float max)
    {
        min = float.MaxValue;
        max = float.MinValue;
        foreach (var v in poly)
        {
            var d = Vector2.Dot(v, axis);
            min = Math.Min(min, d);
            max = Math.Max(max, d);
        }
    }

    private static float Cross(Vector2 a, Vector2 b) => (a.X * b.Y) - (a.Y * b.X);

    private static bool InCircle(Vector2 a, Vector2 b, Vector2 c, Vector2 p, bool counterClockwise)
    {
        var ax = a.X - p.X;
        var ay = a.Y - p.Y;
        var bx = b.X - p.X;
        var by = b.Y - p.Y;
        var cx = c.X - p.X;
        var cy = c.Y - p.Y;
        var det = (((ax * ax) + (ay * ay)) * ((bx * cy) - (cx * by)))
            - (((bx * bx) + (by * by)) * ((ax * cy) - (cx * ay)))
            + (((cx * cx) + (cy * cy)) * ((ax * by) - (bx * ay)));
        return counterClockwise ? det > 1e-9f : det < -1e-9f;
    }

    private static Vector3 Barycentric(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        var v0 = b - a;
        var v1 = c - a;
        var v2 = p - a;
        var d = Cross(v0, v1);
        var v = Cross(v2, v1) / d;
        var w = Cross(v0, v2) / d;
        return new Vector3(1f - v - w, v, w);
    }

    private static Vector2 ClosestOnTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        var best = ClosestOnSegment(p, a, b);
        var other = ClosestOnSegment(p, b, c);
        if (Vector2.DistanceSquared(p, other) < Vector2.DistanceSquared(p, best))
        {
            best = other;
        }

        other = ClosestOnSegment(p, c, a);
        return Vector2.DistanceSquared(p, other) < Vector2.DistanceSquared(p, best) ? other : best;
    }

    private static Vector2 ClosestOnSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var t = Math.Clamp(Vector2.Dot(p - a, ab) / ab.LengthSquared(), 0f, 1f);
        return a + (ab * t);
    }
}
