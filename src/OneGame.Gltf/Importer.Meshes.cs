using System.Globalization;

namespace OneGame.Gltf;

internal sealed partial class Importer
{
    private static readonly string[] HandledAttributes = ["POSITION", "NORMAL", "TANGENT", "TEXCOORD_0", "TEXCOORD_1", "COLOR_0", "JOINTS_0", "WEIGHTS_0"];
    private int _primitiveTotal;
    private bool _warnedQuantization;

    private static string N(int value) => value.ToString(CultureInfo.InvariantCulture);

    private GltfMesh[] ReadMeshes()
    {
        var count = _root.ArrayLength("meshes", _limits.MaxMeshes);
        var result = new GltfMesh[count];
        for (var i = 0; i < count; i++)
        {
            var m = _root.Item("meshes", i);
            var pc = m.ArrayLength("primitives", _limits.MaxPrimitives);
            if (pc == 0)
            {
                throw m.Error("schema", "primitives", "must hold at least one primitive.");
            }

            _primitiveTotal += pc;
            if (_primitiveTotal > _limits.MaxPrimitives)
            {
                throw m.Error("limit_exceeded", "primitives", $"bring the total to {N(_primitiveTotal)}; the limit is {N(_limits.MaxPrimitives)}.");
            }

            var prims = new GltfPrimitive[pc];
            var targets = -1;
            for (var p = 0; p < pc; p++)
            {
                var prim = ReadPrimitive(m.Item("primitives", p));
                if (targets >= 0 && prim.MorphTargets.Length != targets)
                {
                    throw m.Error("schema", "primitives", "must all have the same number of morph targets.");
                }

                targets = prim.MorphTargets.Length;
                prims[p] = prim;
            }

            var weights = m.Floats("weights", targets) ?? new float[targets];
            var bounds = prims[0].Bounds;
            for (var p = 1; p < pc; p++)
            {
                bounds = new GltfBounds(Vector3.Min(bounds.Min, prims[p].Bounds.Min), Vector3.Max(bounds.Max, prims[p].Bounds.Max));
            }

            result[i] = new GltfMesh { Name = m.Str("name"), Primitives = prims, MorphTargetCount = targets, DefaultWeights = weights, Bounds = bounds };
        }

        return result;
    }

    private GltfPrimitive ReadPrimitive(JObj p)
    {
        var mode = p.Int("mode", 4, 0, 6);
        var attrs = p.Obj("attributes") ?? throw p.Error("schema", "attributes", "is required.");
        var positionAccessor = attrs.ReqRef("POSITION", _acc.Count);
        var vertexCount = _acc.ElementCount(positionAccessor);
        if (vertexCount > _limits.MaxVerticesPerPrimitive)
        {
            throw attrs.Error("limit_exceeded", "POSITION", $"has {N(vertexCount)} vertices; the limit is {N(_limits.MaxVerticesPerPrimitive)}.");
        }

        var prim = new GltfPrimitive { Material = p.Ref("material", _model.Materials.Length) };
        prim.Positions = ReadVec3(attrs, "POSITION", positionAccessor, vertexCount, quantizable: true);
        if (attrs.Ref("NORMAL", _acc.Count) is var na and >= 0)
        {
            prim.Normals = ReadVec3(attrs, "NORMAL", na, vertexCount, quantizable: true);
            prim.HasNormals = true;
        }
        else
        {
            _budget.Charge(vertexCount * 12L, "normals");
            prim.Normals = new Vector3[vertexCount];
            Array.Fill(prim.Normals, Vector3.UnitZ);
        }

        if (attrs.Ref("TANGENT", _acc.Count) is var ta and >= 0)
        {
            var f = ReadAttribute(attrs, "TANGENT", ta, 4, vertexCount, quantizable: true);
            _budget.Charge(vertexCount * 16L, "tangents");
            var t = new Vector4[vertexCount];
            for (var i = 0; i < t.Length; i++)
            {
                t[i] = new Vector4(f[i * 4], f[(i * 4) + 1], f[(i * 4) + 2], f[(i * 4) + 3]);
            }

            prim.Tangents = t;
        }

        prim.Uv0 = ReadUv(attrs, "TEXCOORD_0", vertexCount);
        prim.Uv1 = ReadUv(attrs, "TEXCOORD_1", vertexCount);
        if (attrs.Ref("COLOR_0", _acc.Count) is var ca and >= 0)
        {
            var comps = _acc.Describe(ca).Components;
            if (comps is not (3 or 4))
            {
                throw attrs.Error("accessor_invalid", "COLOR_0", "must be a VEC3 or VEC4 accessor.");
            }

            var f = ReadAttribute(attrs, "COLOR_0", ca, comps, vertexCount, quantizable: false);
            _budget.Charge(vertexCount * 16L, "colors");
            var c = new Vector4[vertexCount];
            for (var i = 0; i < c.Length; i++)
            {
                c[i] = new Vector4(f[i * comps], f[(i * comps) + 1], f[(i * comps) + 2], comps == 4 ? f[(i * comps) + 3] : 1f);
            }

            prim.Colors = c;
        }

        ReadSkinAttributes(attrs, prim, vertexCount);
        foreach (var prop in attrs.Element.EnumerateObject())
        {
            var name = JObj.NameOf(prop);
            if (Array.IndexOf(HandledAttributes, name) < 0 && !name.StartsWith('_'))
            {
                Warn("ignored_attribute", $"The vertex attribute '{GltfUri.Show(name)}' is not used.");
            }
        }

        // Indices are validated against the vertex count before anything is expanded.
        uint[] source;
        if (p.Ref("indices", _acc.Count) is var ia and >= 0)
        {
            if (_acc.Describe(ia).Components != 1)
            {
                throw p.Error("accessor_invalid", "indices", "must be a SCALAR accessor.");
            }

            if (_acc.ElementCount(ia) > _limits.MaxIndicesPerPrimitive)
            {
                throw p.Error("limit_exceeded", "indices", $"has {N(_acc.ElementCount(ia))} indices; the limit is {N(_limits.MaxIndicesPerPrimitive)}.");
            }

            source = _acc.ReadUInts(ia, p.Path("indices"));
            foreach (var v in source)
            {
                if (v >= (uint)vertexCount)
                {
                    throw p.Error("index_out_of_range", "indices", $"contain {v.ToString(CultureInfo.InvariantCulture)}, but the primitive has {N(vertexCount)} vertices.");
                }
            }
        }
        else
        {
            if (vertexCount > _limits.MaxIndicesPerPrimitive)
            {
                throw p.Error("limit_exceeded", "attributes", "make more indices than the limit.");
            }

            _budget.Charge(vertexCount * 4L, "indices");
            source = new uint[vertexCount];
            for (var i = 0; i < source.Length; i++)
            {
                source[i] = (uint)i;
            }
        }

        prim.Indices = MeshOps.Expand(source, mode, _limits, _budget, out var topology, out var dropped);
        prim.Topology = topology;
        if (dropped > 0)
        {
            Warn("incomplete_shape", $"{p.Path("indices")} ends with {N(dropped)} index/indices that do not form a whole shape; they were dropped.");
        }

        ReadTargets(p, prim, vertexCount);

        if (topology == GltfTopology.Triangles)
        {
            if (!prim.HasNormals && _options.GenerateNormals)
            {
                MeshOps.FlatNormals(prim, _limits, _budget);
            }

            WantTangents(prim);
        }

        prim.Bounds = MeshOps.ComputeBounds(prim.Positions);
        return prim;
    }

    private void WantTangents(GltfPrimitive prim)
    {
        if (_options.Tangents == TangentMode.Never || !prim.HasNormals)
        {
            return;
        }

        TextureRef? normalMap = prim.Material >= 0 ? _model.Materials[prim.Material].NormalTexture : null;
        var generate = _options.Tangents == TangentMode.Always || (prim.Tangents is null && normalMap is not null);
        if (!generate)
        {
            return;
        }

        var uv = normalMap is { TexCoord: 1 } ? prim.Uv1 ?? prim.Uv0 : prim.Uv0;
        if (uv is null)
        {
            if (normalMap is not null)
            {
                Warn("no_tangents", "A normal-mapped primitive has no texture coordinates, so tangents cannot be generated.");
            }

            return;
        }

        MeshOps.GenerateTangents(prim, uv, _limits, _budget);
    }

    private void CheckQuantized(string name, int componentType)
    {
        if (componentType != 5126 && !_warnedQuantization && Array.IndexOf(_model.ExtensionsUsed, "KHR_mesh_quantization") < 0)
        {
            _warnedQuantization = true;
            Warn("quantization_without_extension", $"{name} is stored as integers but KHR_mesh_quantization is not declared; it was read anyway.");
        }
    }

    private float[] ReadAttribute(JObj owner, string name, int accessor, int components, int vertexCount, bool quantizable)
    {
        var (ct, comps, _) = _acc.Describe(accessor);
        var colorOk = name == "COLOR_0" && comps is 3 or 4;
        if (comps != components && !colorOk)
        {
            throw owner.Error("accessor_invalid", name, $"must have {N(components)} components.");
        }

        if (_acc.ElementCount(accessor) != vertexCount)
        {
            throw owner.Error("accessor_invalid", name, $"has {N(_acc.ElementCount(accessor))} elements but POSITION has {N(vertexCount)}.");
        }

        if (quantizable)
        {
            CheckQuantized(name, ct);
        }

        return _acc.ReadFloats(accessor, owner.Path(name));
    }

    private Vector3[] ReadVec3(JObj owner, string name, int accessor, int vertexCount, bool quantizable)
    {
        var f = ReadAttribute(owner, name, accessor, 3, vertexCount, quantizable);
        _budget.Charge(vertexCount * 12L, name);
        var v = new Vector3[vertexCount];
        for (var i = 0; i < v.Length; i++)
        {
            v[i] = new Vector3(f[i * 3], f[(i * 3) + 1], f[(i * 3) + 2]);
        }

        return v;
    }

    private Vector2[]? ReadUv(JObj attrs, string name, int vertexCount)
    {
        var a = attrs.Ref(name, _acc.Count);
        if (a < 0)
        {
            return null;
        }

        var f = ReadAttribute(attrs, name, a, 2, vertexCount, quantizable: false);
        _budget.Charge(vertexCount * 8L, name);
        var uv = new Vector2[vertexCount];
        for (var i = 0; i < uv.Length; i++)
        {
            uv[i] = new Vector2(f[i * 2], f[(i * 2) + 1]);
        }

        return uv;
    }

    private void ReadSkinAttributes(JObj attrs, GltfPrimitive prim, int vertexCount)
    {
        var ja = attrs.Ref("JOINTS_0", _acc.Count);
        var wa = attrs.Ref("WEIGHTS_0", _acc.Count);
        if (ja < 0 && wa < 0)
        {
            return;
        }

        if (ja < 0 || wa < 0)
        {
            Warn("incomplete_skin", "A primitive has only one of JOINTS_0 and WEIGHTS_0; the skin attributes were ignored.");
            return;
        }

        if (_acc.Describe(ja).Components != 4 || _acc.ElementCount(ja) != vertexCount)
        {
            throw attrs.Error("accessor_invalid", "JOINTS_0", "must be a VEC4 accessor with one element per vertex.");
        }

        var j = _acc.ReadUInts(ja, attrs.Path("JOINTS_0"));
        _budget.Charge(vertexCount * 8L, "joints");
        var joints = new JointIndices[vertexCount];
        for (var i = 0; i < joints.Length; i++)
        {
            joints[i] = new JointIndices((ushort)j[i * 4], (ushort)j[(i * 4) + 1], (ushort)j[(i * 4) + 2], (ushort)j[(i * 4) + 3]);
        }

        var w = ReadAttribute(attrs, "WEIGHTS_0", wa, 4, vertexCount, quantizable: false);
        _budget.Charge(vertexCount * 16L, "weights");
        var weights = new Vector4[vertexCount];
        for (var i = 0; i < weights.Length; i++)
        {
            weights[i] = new Vector4(w[i * 4], w[(i * 4) + 1], w[(i * 4) + 2], w[(i * 4) + 3]);
        }

        prim.Joints = joints;
        prim.Weights = weights;
    }

    private void ReadTargets(JObj p, GltfPrimitive prim, int vertexCount)
    {
        var count = p.ArrayLength("targets", _limits.MaxMorphTargets);
        var targets = new MorphTarget[count];
        for (var i = 0; i < count; i++)
        {
            var t = p.Item("targets", i);
            var target = new MorphTarget();
            if (t.Ref("POSITION", _acc.Count) is var pa and >= 0)
            {
                target.Positions = ReadVec3(t, "POSITION", pa, vertexCount, quantizable: true);
            }

            if (t.Ref("NORMAL", _acc.Count) is var na and >= 0)
            {
                target.Normals = ReadVec3(t, "NORMAL", na, vertexCount, quantizable: true);
            }

            if (t.Ref("TANGENT", _acc.Count) is var ta and >= 0)
            {
                target.Tangents = ReadVec3(t, "TANGENT", ta, vertexCount, quantizable: true);
            }

            targets[i] = target;
        }

        prim.MorphTargets = targets;
    }
}
