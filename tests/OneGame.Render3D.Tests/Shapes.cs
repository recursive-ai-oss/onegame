using System.Globalization;
using System.Text;
using OneGame.Gltf;

namespace OneGame.Render3D.Tests;

/// <summary>Generated glTF scenes of boxes and ground planes with known geometry, for shadow, occlusion and style checks.</summary>
internal sealed class Shapes
{
    private sealed record Part(Vector3[] Positions, Vector3[] Normals, ushort[] Indices, string Material, Vector3 Translation);

    private readonly List<Part> _parts = [];

    /// <summary>A box of the given size centred at <paramref name="center"/>.</summary>
    public Shapes Box(Vector3 center, Vector3 size, string material)
    {
        var h = size * 0.5f;
        var pos = new List<Vector3>();
        var nrm = new List<Vector3>();
        var idx = new List<ushort>();
        void Face(Vector3 n, Vector3 u, Vector3 v)
        {
            var o = (ushort)pos.Count;
            var c = n * Vector3.Abs(n).Dot(h);
            var uu = u * Vector3.Abs(u).Dot(h);
            var vv = v * Vector3.Abs(v).Dot(h);
            pos.AddRange([c - uu - vv, c + uu - vv, c + uu + vv, c - uu + vv]);
            nrm.AddRange([n, n, n, n]);
            idx.AddRange([o, (ushort)(o + 1), (ushort)(o + 2), o, (ushort)(o + 2), (ushort)(o + 3)]);
        }

        Face(Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ);
        Face(-Vector3.UnitX, Vector3.UnitZ, Vector3.UnitY);
        Face(Vector3.UnitY, Vector3.UnitZ, Vector3.UnitX);
        Face(-Vector3.UnitY, Vector3.UnitX, Vector3.UnitZ);
        Face(Vector3.UnitZ, Vector3.UnitX, Vector3.UnitY);
        Face(-Vector3.UnitZ, Vector3.UnitY, Vector3.UnitX);
        _parts.Add(new Part([.. pos], [.. nrm], [.. idx], material, center));
        return this;
    }

    /// <summary>A sphere with smooth normals.</summary>
    public Shapes Sphere(Vector3 center, float radius, string material, int segments = 48)
    {
        var rings = segments / 2;
        var pos = new List<Vector3>();
        var nrm = new List<Vector3>();
        var idx = new List<ushort>();
        for (var r = 0; r <= rings; r++)
        {
            var phi = MathF.PI * r / rings;
            for (var s = 0; s <= segments; s++)
            {
                var theta = 2f * MathF.PI * s / segments;
                var n = new Vector3(MathF.Sin(phi) * MathF.Cos(theta), MathF.Cos(phi), MathF.Sin(phi) * MathF.Sin(theta));
                pos.Add(n * radius);
                nrm.Add(n);
            }
        }

        for (var r = 0; r < rings; r++)
        {
            for (var s = 0; s < segments; s++)
            {
                var a = (ushort)((r * (segments + 1)) + s);
                var b = (ushort)(a + segments + 1);
                idx.AddRange([a, (ushort)(a + 1), b, (ushort)(a + 1), (ushort)(b + 1), b]);
            }
        }

        _parts.Add(new Part([.. pos], [.. nrm], [.. idx], material, center));
        return this;
    }

    /// <summary>A horizontal square facing +Y at height <paramref name="y"/>, <paramref name="half"/> units from the centre to each edge.</summary>
    public Shapes Ground(float y, float half, string material)
    {
        _parts.Add(new Part(
            [new(-half, 0, half), new(half, 0, half), new(half, 0, -half), new(-half, 0, -half)],
            [Vector3.UnitY, Vector3.UnitY, Vector3.UnitY, Vector3.UnitY],
            [0, 1, 2, 0, 2, 3],
            material,
            new Vector3(0, y, 0)));
        return this;
    }

    /// <summary>A vertical square facing +Z at depth <paramref name="z"/>, from y = 0 up to <paramref name="height"/>.</summary>
    public Shapes Wall(float z, float half, float height, string material)
    {
        _parts.Add(new Part(
            [new(-half, 0, 0), new(half, 0, 0), new(half, height, 0), new(-half, height, 0)],
            [Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ],
            [0, 1, 2, 0, 2, 3],
            material,
            new Vector3(0, 0, z)));
        return this;
    }

    /// <summary>A material with a plain colour, no specular reflection (index of refraction 1) and full roughness, so shading is Lambert.</summary>
    public static string Matte(float r, float g, float b)
    {
        var c = (float v) => v.ToString("R", CultureInfo.InvariantCulture);
        return "{\"pbrMetallicRoughness\":{\"baseColorFactor\":[" + c(r) + "," + c(g) + "," + c(b) + ",1],\"metallicFactor\":0,\"roughnessFactor\":1},\"extensions\":{\"KHR_materials_ior\":{\"ior\":1.0}}}";
    }

    /// <summary>The glTF document (embedded buffer).</summary>
    public byte[] Build()
    {
        var data = new List<byte>();
        var views = new StringBuilder();
        var accessors = new StringBuilder();
        var meshes = new StringBuilder();
        var nodes = new StringBuilder();
        var materials = new StringBuilder();
        void F(Vector3 v)
        {
            data.AddRange(BitConverter.GetBytes(v.X));
            data.AddRange(BitConverter.GetBytes(v.Y));
            data.AddRange(BitConverter.GetBytes(v.Z));
        }

        for (var i = 0; i < _parts.Count; i++)
        {
            var p = _parts[i];
            var posStart = data.Count;
            foreach (var v in p.Positions)
            {
                F(v);
            }

            var nrmStart = data.Count;
            foreach (var v in p.Normals)
            {
                F(v);
            }

            var idxStart = data.Count;
            foreach (var v in p.Indices)
            {
                data.AddRange(BitConverter.GetBytes(v));
            }

            while (data.Count % 4 != 0)
            {
                data.Add(0);
            }

            var min = p.Positions.Aggregate(Vector3.Min);
            var max = p.Positions.Aggregate(Vector3.Max);
            var nv = p.Positions.Length;
            var a = i * 3;
            string N(float x) => x.ToString("R", CultureInfo.InvariantCulture);
            views.Append(CultureInfo.InvariantCulture, $"{{\"buffer\":0,\"byteOffset\":{posStart},\"byteLength\":{nv * 12}}},{{\"buffer\":0,\"byteOffset\":{nrmStart},\"byteLength\":{nv * 12}}},{{\"buffer\":0,\"byteOffset\":{idxStart},\"byteLength\":{p.Indices.Length * 2}}},");
            accessors.Append(CultureInfo.InvariantCulture, $"{{\"bufferView\":{a},\"componentType\":5126,\"count\":{nv},\"type\":\"VEC3\",\"min\":[{N(min.X)},{N(min.Y)},{N(min.Z)}],\"max\":[{N(max.X)},{N(max.Y)},{N(max.Z)}]}},");
            accessors.Append(CultureInfo.InvariantCulture, $"{{\"bufferView\":{a + 1},\"componentType\":5126,\"count\":{nv},\"type\":\"VEC3\"}},");
            accessors.Append(CultureInfo.InvariantCulture, $"{{\"bufferView\":{a + 2},\"componentType\":5123,\"count\":{p.Indices.Length},\"type\":\"SCALAR\"}},");
            meshes.Append(CultureInfo.InvariantCulture, $"{{\"primitives\":[{{\"attributes\":{{\"POSITION\":{a},\"NORMAL\":{a + 1}}},\"indices\":{a + 2},\"material\":{i}}}]}},");
            nodes.Append(CultureInfo.InvariantCulture, $"{{\"mesh\":{i},\"translation\":[{N(p.Translation.X)},{N(p.Translation.Y)},{N(p.Translation.Z)}]}},");
            materials.Append(p.Material).Append(',');
        }

        static string Trim(StringBuilder b) => b.ToString().TrimEnd(',');
        var uri = "data:application/octet-stream;base64," + Convert.ToBase64String([.. data]);
        var roots = string.Join(',', Enumerable.Range(0, _parts.Count));
        var json = string.Create(CultureInfo.InvariantCulture,
            $"{{\"asset\":{{\"version\":\"2.0\"}},\"extensionsUsed\":[\"KHR_materials_ior\"],\"scene\":0,\"scenes\":[{{\"nodes\":[{roots}]}}],\"nodes\":[{Trim(nodes)}],\"meshes\":[{Trim(meshes)}],\"materials\":[{Trim(materials)}],\"buffers\":[{{\"byteLength\":{data.Count},\"uri\":\"{uri}\"}}],\"bufferViews\":[{Trim(views)}],\"accessors\":[{Trim(accessors)}]}}");
        return Encoding.UTF8.GetBytes(json);
    }

    /// <summary>Loads the document into the GPU model of a rig.</summary>
    public Model3D Upload(Rig rig) => rig.Own(rig.Renderer.Upload(GltfLoader.Load(Build())));
}

internal static class VectorExtensions
{
    public static float Dot(this Vector3 a, Vector3 b) => Vector3.Dot(a, b);
}
