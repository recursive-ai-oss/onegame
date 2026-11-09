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
    public IEnumerable<string> Ids => _entries.Where(e => !e.IsDead).Select(e => e.Id);

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

        Place(id, new Entry(id, model));
    }

    /// <summary>Whether a model or mesh with this name is in the library.</summary>
    /// <param name="id">The name.</param>
    public bool Contains(string id) => id is not null && _ids.ContainsKey(id);

    /// <summary>Replaces the model named <paramref name="id"/>, or adds it. Entities drawing the name show the new model from the next frame.</summary>
    /// <param name="id">The name entities use.</param>
    /// <param name="model">The new imported model.</param>
    public void Replace(string id, GltfModel model)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentNullException.ThrowIfNull(model);
        if (_ids.ContainsKey(id))
        {
            Retire(id);
        }

        Place(id, new Entry(id, model));
    }

    /// <summary>
    /// Adds a mesh built at run time under a name, or replaces the one with that name; entities draw it with <c>SceneWriter.Model</c> like any model,
    /// and the next frame shows the new geometry. The vertices are copied (into pooled memory) and uploaded when the next frame is built, so many
    /// calls in one frame cost one upload each, and the spans need not outlive the call. See <see cref="DynamicMesh3D"/> for how buffers are reused.
    /// Empty geometry keeps the name but draws nothing. Call it from the thread that renders. Meshes cannot be picked.
    /// </summary>
    /// <param name="id">The name entities use.</param>
    /// <param name="material">The shared material, from <see cref="Renderer3D.CreateMaterial"/> or a model's materials.</param>
    /// <param name="vertices">The vertices.</param>
    /// <param name="indices">Triangle list indices; a multiple of 3, each below the vertex count.</param>
    /// <exception cref="ArgumentException">The geometry is invalid, or <paramref name="id"/> names an imported model (remove it first).</exception>
    public void SetMesh(string id, Material3D material, ReadOnlySpan<MeshVertex3D> vertices, ReadOnlySpan<uint> indices)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentNullException.ThrowIfNull(material);
        var empty = vertices.IsEmpty || indices.IsEmpty;
        if (!empty)
        {
            DynamicMesh3D.Validate(vertices, indices);
        }

        Entry entry;
        if (_ids.TryGetValue(id, out var slot))
        {
            entry = _entries[slot];
            if (!entry.IsMesh)
            {
                throw new ArgumentException($"'{id}' is an imported model; call Remove first to use the name for a mesh.", nameof(id));
            }
        }
        else
        {
            entry = Entry.ForMesh(id);
            Place(id, entry);
        }

        entry.SetPending(material, empty ? default : vertices, empty ? default : indices);
    }

    /// <summary>
    /// Takes a model or mesh out of the library. Its GPU memory is released when the next frame is built (and the device frees it once the frames
    /// that used it have finished). Entities still drawing the name draw nothing until it is added again.
    /// </summary>
    /// <param name="id">The name.</param>
    /// <returns>False when there was none.</returns>
    public bool Remove(string id)
    {
        if (id is null || !_ids.ContainsKey(id))
        {
            return false;
        }

        Retire(id);
        return true;
    }

    private readonly List<Entry> _dead = [];
    private readonly Stack<int> _freeSlots = new();

    // Draws made by the last Prepare hold slot numbers, so a freed slot is only given to another name once the next frame is built.
    private readonly List<int> _slotsToFree = [];

    private void Place(string id, Entry entry)
    {
        int slot;
        if (_freeSlots.Count > 0)
        {
            slot = _freeSlots.Pop();
            _entries[slot] = entry;
        }
        else
        {
            slot = _entries.Count;
            _entries.Add(entry);
        }

        _ids[id] = slot;
    }

    private void Retire(string id)
    {
        var slot = _ids[id];
        _dead.Add(_entries[slot]);
        _entries[slot] = Entry.Tombstone();
        _ids.Remove(id);
        _slotsToFree.Add(slot);
    }

    /// <summary>Releases what removed and replaced entries held and uploads meshes that changed. Called when a frame is built, before any draw.</summary>
    internal void Collect(Renderer3D renderer)
    {
        foreach (var e in _dead)
        {
            e.Release();
        }

        _dead.Clear();
        foreach (var s in _slotsToFree)
        {
            _freeSlots.Push(s);
        }

        _slotsToFree.Clear();
        for (var i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].IsMesh)
            {
                _entries[i].UploadPending(renderer);
            }
        }
    }

    /// <summary>The imported model with this name.</summary>
    /// <exception cref="UnknownModelException">There is none.</exception>
    public GltfModel this[string id] => _entries[IndexOf(id)] is { IsMesh: false } e ? e.Source : throw new ArgumentException($"'{id}' is a mesh built at run time and has no imported model.", nameof(id));

    internal int IndexOf(string id) => _ids.TryGetValue(id, out var i) ? i : throw new UnknownModelException(id, Ids);

    internal int Count => _entries.Count;

    internal Entry At(int index) => _entries[index];

    /// <summary>Releases the GPU copies. The renderer that made them must still be alive.</summary>
    public void Dispose()
    {
        foreach (var e in _dead)
        {
            e.Release();
        }

        _dead.Clear();
        foreach (var e in _entries)
        {
            e.Release();
        }
    }

    internal sealed class Entry
    {
        private static GltfModel? s_none;
        private MeshVertex3D[]? _pendingVertices;
        private uint[]? _pendingIndices;
        private int _pendingVertexCount;
        private int _pendingIndexCount;
        private Material3D? _pendingMaterial;
        private bool _dirty;

        private Entry(string id, bool mesh, bool dead)
        {
            Id = id;
            IsMesh = mesh;
            IsDead = dead;
            Source = NoSource;
            MeshNodes = [];
        }

        // Entries without an imported model (meshes built at run time, removed names) share one empty stand-in.
        private static GltfModel NoSource => s_none ??= OneGame.Gltf.GltfLoader.Load("{\"asset\":{\"version\":\"2.0\"}}"u8.ToArray());

        public static Entry ForMesh(string id) => new(id, true, false);

        public static Entry Tombstone() => new(string.Empty, false, true);

        public bool IsMesh { get; }

        /// <summary>True for the stand-in of a removed name; draws that still point at it are skipped.</summary>
        public bool IsDead { get; }

        public DynamicMesh3D? Mesh { get; private set; }

        /// <summary>Triangles the model draws: a mesh's pending or uploaded geometry, else the uploaded model's.</summary>
        public int TriangleCount => IsMesh ? (_dirty ? _pendingIndexCount / 3 : Mesh?.TriangleCount ?? 0) : Gpu?.TriangleCount ?? 0;

        /// <summary>True when there is nothing to draw: removed, or a mesh with no geometry yet.</summary>
        public bool NothingToDraw => IsDead || (IsMesh && Gpu is null);

        public void SetPending(Material3D material, ReadOnlySpan<MeshVertex3D> vertices, ReadOnlySpan<uint> indices)
        {
            _pendingMaterial = material;
            _pendingVertexCount = vertices.Length;
            _pendingIndexCount = indices.Length;
            if (vertices.Length > 0)
            {
                ReturnPending();
                _pendingVertices = System.Buffers.ArrayPool<MeshVertex3D>.Shared.Rent(vertices.Length);
                _pendingIndices = System.Buffers.ArrayPool<uint>.Shared.Rent(indices.Length);
                vertices.CopyTo(_pendingVertices);
                indices.CopyTo(_pendingIndices);
            }

            _dirty = true;
        }

        public void UploadPending(Renderer3D renderer)
        {
            if (!_dirty)
            {
                return;
            }

            _dirty = false;
            var material = _pendingMaterial!;
            if (_pendingVertexCount == 0)
            {
                DropGpu();
                return;
            }

            var v = new ReadOnlySpan<MeshVertex3D>(_pendingVertices, 0, _pendingVertexCount);
            var i = new ReadOnlySpan<uint>(_pendingIndices, 0, _pendingIndexCount);
            if (Mesh is not null && !ReferenceEquals(Mesh.Material, material))
            {
                DropGpu();
            }

            if (Mesh is null)
            {
                Mesh = renderer.CreateMesh(material, v, i, Id);
                Gpu = Mesh.Model;
            }
            else
            {
                Mesh.Update(v, i);
            }

            // The copy is only needed until the upload; keeping one per live mesh would hold the whole world on the CPU twice.
            ReturnPending();
        }

        private void ReturnPending()
        {
            if (_pendingVertices is not null)
            {
                System.Buffers.ArrayPool<MeshVertex3D>.Shared.Return(_pendingVertices);
                _pendingVertices = null;
            }

            if (_pendingIndices is not null)
            {
                System.Buffers.ArrayPool<uint>.Shared.Return(_pendingIndices);
                _pendingIndices = null;
            }
        }

        private void DropGpu()
        {
            foreach (var instance in Pool)
            {
                instance.Dispose();
            }

            Pool.Clear();
            Used = 0;
            Batch = null;
            Mesh?.Dispose();
            Mesh = null;
            Gpu?.Dispose();
            Gpu = null;
        }

        /// <summary>Frees the GPU copy and any pending data.</summary>
        public void Release()
        {
            DropGpu();
            _dirty = false;
            ReturnPending();
        }


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

        /// <summary>True once any frame drew this model in an explicit pose.</summary>
        public bool Posed { get; set; }

        private NodePose? _rest;

        public NodePose Rest => _rest ??= Source.CreateRestPose();

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
            if (IsMesh || IsDead || !Matrix4x4.Invert(placement, out var toModel))
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
