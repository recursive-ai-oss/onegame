using System.Numerics;
using OneGame.Gltf;
using OneGame.Render3D;

namespace OneGame.Host;

/// <summary>
/// The models a game's entities can show, by name. Each keeps its imported glTF data, which picking reads without a graphics
/// device, and its GPU copy, which is uploaded the first time a frame draws it.
/// </summary>
public sealed class ModelLibrary : IDisposable
{
    private readonly List<Entry> _entries = [];
    private readonly Dictionary<string, int> _ids = new(StringComparer.Ordinal);

    /// <summary>The model names in the order they were added.</summary>
    public IEnumerable<string> Ids => _entries.Select(e => e.Id);

    /// <summary>Adds a model under a name.</summary>
    /// <param name="id">The name entities use.</param>
    /// <param name="model">The imported model.</param>
    /// <exception cref="ArgumentException">The name is taken.</exception>
    public void Add(string id, GltfModel model)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentNullException.ThrowIfNull(model);
        if (_ids.ContainsKey(id))
        {
            throw new ArgumentException($"A model named '{id}' already exists.", nameof(id));
        }

        _ids.Add(id, _entries.Count);
        _entries.Add(new Entry(id, model));
    }

    /// <summary>The imported model with this name.</summary>
    /// <exception cref="UnknownModelException">There is none.</exception>
    public GltfModel this[string id] => _entries[IndexOf(id)].Source;

    internal int IndexOf(string id) => _ids.TryGetValue(id, out var i) ? i : throw new UnknownModelException(id, Ids);

    internal int Count => _entries.Count;

    internal Entry At(int index) => _entries[index];

    /// <summary>Releases the GPU copies. The renderer that made them must still be alive.</summary>
    public void Dispose()
    {
        foreach (var e in _entries)
        {
            foreach (var instance in e.Pool)
            {
                instance.Dispose();
            }

            e.Pool.Clear();
            e.Batch = null;
            e.Gpu?.Dispose();
            e.Gpu = null;
        }
    }

    internal sealed class Entry
    {
        public Entry(string id, GltfModel source)
        {
            Id = id;
            Source = source;
            var meshNodes = new List<int>();
            for (var i = 0; i < source.Nodes.Length; i++)
            {
                if (source.Nodes[i].Mesh >= 0)
                {
                    meshNodes.Add(i);
                }
            }

            MeshNodes = [.. meshNodes];
            var bounds = source.ComputeWorldBounds();
            Min = bounds?.Min ?? Vector3.Zero;
            Max = bounds?.Max ?? Vector3.Zero;
            HasBounds = bounds is not null;
        }

        public string Id { get; }

        public GltfModel Source { get; }

        public int[] MeshNodes { get; }

        public Vector3 Min { get; }

        public Vector3 Max { get; }

        public bool HasBounds { get; }

        public Model3D? Gpu { get; set; }

        public List<ModelInstance3D> Pool { get; } = [];

        public int Used { get; set; }

        public InstanceBatch3D? Batch { get; set; }

        private Matrix4x4[]? _nodeInverse;

        /// <summary>
        /// Intersects a ray with the model's triangles at its rest pose, both sides. The ray is in world space; <paramref name="placement"/> puts the model
        /// in the world. <paramref name="t"/> is the distance along the (unit) direction.
        /// </summary>
        public bool Intersect(Matrix4x4 placement, Vector3 origin, Vector3 direction, out float t)
        {
            t = 0f;
            if (!Matrix4x4.Invert(placement, out var toModel))
            {
                return false;
            }

            // The direction is not renormalized, so t stays a distance along the original ray whatever the scale.
            var o = Vector3.Transform(origin, toModel);
            var d = Vector3.TransformNormal(direction, toModel);
            if (HasBounds && !HitsBox(o, d, Min, Max))
            {
                return false;
            }

            _nodeInverse ??= BuildInverses();
            var nodes = Source.Nodes;
            var best = float.PositiveInfinity;
            foreach (var n in MeshNodes)
            {
                var inv = _nodeInverse[n];
                var lo = Vector3.Transform(o, inv);
                var ld = Vector3.TransformNormal(d, inv);
                foreach (var prim in Source.Meshes[nodes[n].Mesh].Primitives)
                {
                    if (prim.Topology != GltfTopology.Triangles)
                    {
                        continue;
                    }

                    var pos = prim.Positions;
                    var idx = prim.Indices;
                    for (var i = 0; i + 2 < idx.Length; i += 3)
                    {
                        if (HitsTriangle(lo, ld, pos[idx[i]], pos[idx[i + 1]], pos[idx[i + 2]], out var hit) && hit < best)
                        {
                            best = hit;
                        }
                    }
                }
            }

            t = best;
            return float.IsFinite(best);
        }

        private Matrix4x4[] BuildInverses()
        {
            var inverse = new Matrix4x4[Source.Nodes.Length];
            foreach (var n in MeshNodes)
            {
                inverse[n] = Matrix4x4.Invert(Source.Nodes[n].World, out var inv) ? inv : Matrix4x4.Identity;
            }

            return inverse;
        }

        // Slab test of a ray against a box.
        private static bool HitsBox(Vector3 o, Vector3 d, Vector3 min, Vector3 max)
        {
            var tMin = 0f;
            var tMax = float.PositiveInfinity;
            for (var axis = 0; axis < 3; axis++)
            {
                var oa = axis == 0 ? o.X : axis == 1 ? o.Y : o.Z;
                var da = axis == 0 ? d.X : axis == 1 ? d.Y : d.Z;
                var lo = axis == 0 ? min.X : axis == 1 ? min.Y : min.Z;
                var hi = axis == 0 ? max.X : axis == 1 ? max.Y : max.Z;
                if (MathF.Abs(da) < 1e-12f)
                {
                    if (oa < lo || oa > hi)
                    {
                        return false;
                    }

                    continue;
                }

                var t1 = (lo - oa) / da;
                var t2 = (hi - oa) / da;
                tMin = MathF.Max(tMin, MathF.Min(t1, t2));
                tMax = MathF.Min(tMax, MathF.Max(t1, t2));
                if (tMin > tMax)
                {
                    return false;
                }
            }

            return true;
        }

        // Moller-Trumbore, both faces.
        private static bool HitsTriangle(Vector3 o, Vector3 d, Vector3 a, Vector3 b, Vector3 c, out float t)
        {
            t = 0f;
            var e1 = b - a;
            var e2 = c - a;
            var p = Vector3.Cross(d, e2);
            var det = Vector3.Dot(e1, p);
            if (MathF.Abs(det) < 1e-12f)
            {
                return false;
            }

            var inv = 1f / det;
            var s = o - a;
            var u = Vector3.Dot(s, p) * inv;
            if (u is < 0f or > 1f)
            {
                return false;
            }

            var q = Vector3.Cross(s, e1);
            var v = Vector3.Dot(d, q) * inv;
            if (v < 0f || u + v > 1f)
            {
                return false;
            }

            t = Vector3.Dot(e2, q) * inv;
            return t > 0f;
        }
    }
}
