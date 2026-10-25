using System.Globalization;
using System.Numerics;
using System.Text;

// Writes the low-poly models of Lanterns as binary glTF 2.0 files, so the art is plain text code that anyone can read and change.
//   dotnet run --project samples/lanterns/art -- samples/lanterns/models
// Units are metres, +Y up, things face -Z. Colours are written as sRGB hex and stored linear. The output is deterministic.
var outDir = args.Length > 0 ? args[0] : "models";
Directory.CreateDirectory(outDir);
foreach (var (name, model) in Models.All())
{
    var bytes = Glb.Write(model);
    File.WriteAllBytes(Path.Combine(outDir, name + ".glb"), bytes);
    Console.WriteLine($"{name}.glb  {bytes.Length} bytes  {model.Meshes.Sum(m => m.Indices.Count / 3)} triangles");
}

return 0;

/// <summary>A surface material: metallic-roughness, optionally glowing or see-through.</summary>
sealed class Mat(string name, float metal, float rough)
{
    public string Name { get; } = name;
    public float Metal { get; } = metal;
    public float Rough { get; } = rough;
    public Vector3 Emissive { get; init; }
    public float EmissiveStrength { get; init; } = 1f;
    public bool Blend { get; init; }
    public bool DoubleSided { get; init; }
}

/// <summary>Triangles that share one material.</summary>
sealed class Mesh(Mat mat)
{
    public Mat Mat { get; } = mat;
    public List<Vector3> Positions { get; } = [];
    public List<Vector3> Normals { get; } = [];
    public List<Vector4> Colors { get; } = [];
    public List<uint> Indices { get; } = [];

    public uint Vertex(Vector3 p, Vector3 n, Vector4 c)
    {
        Positions.Add(p);
        Normals.Add(Vector3.Normalize(n));
        Colors.Add(c);
        return (uint)Positions.Count - 1;
    }

    /// <summary>Adds a triangle, flipping it when its winding disagrees with the vertex normals (counter-clockwise is front).</summary>
    public void Tri(uint a, uint b, uint c)
    {
        var geometric = Vector3.Cross(Positions[(int)b] - Positions[(int)a], Positions[(int)c] - Positions[(int)a]);
        var wanted = Normals[(int)a] + Normals[(int)b] + Normals[(int)c];
        if (Vector3.Dot(geometric, wanted) < 0f)
        {
            (b, c) = (c, b);
        }

        Indices.Add(a);
        Indices.Add(b);
        Indices.Add(c);
    }

    public void Quad(uint a, uint b, uint c, uint d)
    {
        Tri(a, b, c);
        Tri(a, c, d);
    }
}

/// <summary>A model: one primitive per material.</summary>
sealed class Model
{
    private readonly Dictionary<Mat, Mesh> _byMat = [];
    public List<Mesh> Meshes { get; } = [];

    public Mesh On(Mat m)
    {
        if (!_byMat.TryGetValue(m, out var mesh))
        {
            mesh = new Mesh(m);
            _byMat[m] = mesh;
            Meshes.Add(mesh);
        }

        return mesh;
    }

    /// <summary>A box with its base on <paramref name="center"/>.Y - size.Y/2, turned by <paramref name="yaw"/> about Y.</summary>
    public void Box(Mat m, Vector3 center, Vector3 size, Vector4 color, float yaw = 0f)
    {
        var mesh = On(m);
        var rot = Matrix4x4.CreateRotationY(yaw);
        var h = size / 2f;
        Vector3 P(float x, float y, float z) => center + Vector3.Transform(new Vector3(x * h.X, y * h.Y, z * h.Z), rot);
        (Vector3 N, Vector3[] C)[] faces =
        [
            (Vector3.UnitX, [P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), P(1, -1, 1)]),
            (-Vector3.UnitX, [P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1), P(-1, -1, -1)]),
            (Vector3.UnitY, [P(-1, 1, -1), P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1)]),
            (-Vector3.UnitY, [P(-1, -1, 1), P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1)]),
            (Vector3.UnitZ, [P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1)]),
            (-Vector3.UnitZ, [P(1, -1, -1), P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1)]),
        ];
        foreach (var (n, c) in faces)
        {
            var nr = Vector3.TransformNormal(n, rot);
            mesh.Quad(mesh.Vertex(c[0], nr, color), mesh.Vertex(c[1], nr, color), mesh.Vertex(c[2], nr, color), mesh.Vertex(c[3], nr, color));
        }
    }

    /// <summary>
    /// Revolves a profile (radius, height, colour; bottom to top) about the Y axis at <paramref name="origin"/>. Smooth joints share a
    /// normal; <paramref name="hard"/> joints (by profile index) keep the two segments' own normals, for creases.
    /// </summary>
    public void Lathe(Mat m, Vector3 origin, (float R, float Y, Vector4 C)[] profile, int segments, bool smooth = true, float sx = 1f, float sz = 1f, ISet<int>? hard = null)
    {
        var mesh = On(m);
        var segN = new Vector2[profile.Length - 1];
        for (var i = 0; i < segN.Length; i++)
        {
            var dr = profile[i + 1].R - profile[i].R;
            var dy = profile[i + 1].Y - profile[i].Y;
            segN[i] = Vector2.Normalize(new Vector2(dy, -dr));
        }

        for (var i = 0; i < segN.Length; i++)
        {
            var n0 = segN[i];
            var n1 = segN[i];
            if (smooth && i > 0 && hard?.Contains(i) != true)
            {
                n0 = Vector2.Normalize(segN[i - 1] + segN[i]);
            }

            if (smooth && i + 1 < segN.Length && hard?.Contains(i + 1) != true)
            {
                n1 = Vector2.Normalize(segN[i] + segN[i + 1]);
            }

            var ring0 = new uint[segments + 1];
            var ring1 = new uint[segments + 1];
            for (var j = 0; j <= segments; j++)
            {
                var a = j / (float)segments * MathF.Tau;
                var (s, c) = MathF.SinCos(a);
                ring0[j] = mesh.Vertex(origin + new Vector3(profile[i].R * c * sx, profile[i].Y, profile[i].R * s * sz), new Vector3(n0.X * c / sx, n0.Y, n0.X * s / sz), profile[i].C);
                ring1[j] = mesh.Vertex(origin + new Vector3(profile[i + 1].R * c * sx, profile[i + 1].Y, profile[i + 1].R * s * sz), new Vector3(n1.X * c / sx, n1.Y, n1.X * s / sz), profile[i + 1].C);
            }

            for (var j = 0; j < segments; j++)
            {
                if (profile[i].R == 0f)
                {
                    mesh.Tri(ring0[j], ring1[j], ring1[j + 1]);
                }
                else if (profile[i + 1].R == 0f)
                {
                    mesh.Tri(ring0[j], ring1[j], ring0[j + 1]);
                }
                else
                {
                    mesh.Quad(ring0[j], ring1[j], ring1[j + 1], ring0[j + 1]);
                }
            }
        }
    }

    public void Cylinder(Mat m, Vector3 baseCenter, float radius, float height, Vector4 color, int segments = 10) =>
        Lathe(m, baseCenter, [(0, 0, color), (radius, 0, color), (radius, height, color), (0, height, color)], segments, smooth: false);

    public void Cone(Mat m, Vector3 baseCenter, float radius, float height, Vector4 color, int segments = 10) =>
        Lathe(m, baseCenter, [(0, 0, color), (radius, 0, color), (0, height, color)], segments, smooth: false);

    public void Sphere(Mat m, Vector3 center, float radius, Vector4 color, int segments = 12, int rings = 6, float sx = 1f, float sy = 1f, float sz = 1f)
    {
        var profile = new (float R, float Y, Vector4 C)[rings + 1];
        for (var i = 0; i <= rings; i++)
        {
            var a = -MathF.PI / 2f + (i / (float)rings * MathF.PI);
            profile[i] = (i == 0 || i == rings ? 0f : MathF.Cos(a) * radius, MathF.Sin(a) * radius * sy, color);
        }

        Lathe(m, center, profile, segments, smooth: true, sx, sz);
    }

    /// <summary>A flat ellipse lying on the ground, facing up.</summary>
    public void Disc(Mat m, Vector3 center, float rx, float rz, Vector4 color, int segments = 14)
    {
        var mesh = On(m);
        var hub = mesh.Vertex(center, Vector3.UnitY, color);
        var ring = new uint[segments];
        for (var j = 0; j < segments; j++)
        {
            var a = j / (float)segments * MathF.Tau;
            ring[j] = mesh.Vertex(center + new Vector3(MathF.Cos(a) * rx, 0f, MathF.Sin(a) * rz), Vector3.UnitY, color);
        }

        for (var j = 0; j < segments; j++)
        {
            mesh.Tri(hub, ring[j], ring[(j + 1) % segments]);
        }
    }
}

static class Pal
{
    /// <summary>sRGB hex to a linear colour.</summary>
    public static Vector4 Hex(int rgb, float a = 1f)
    {
        static float Lin(float c) => c <= 0.04045f ? c / 12.92f : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
        return new Vector4(Lin(((rgb >> 16) & 255) / 255f), Lin(((rgb >> 8) & 255) / 255f), Lin((rgb & 255) / 255f), a);
    }

    public static Vector4 Mix(Vector4 a, Vector4 b, float t) => Vector4.Lerp(a, b, t);
}

static class Models
{
    private static readonly Mat Matte = new("matte", 0f, 0.85f);
    private static readonly Mat Wood = new("wood", 0f, 0.7f);
    private static readonly Mat Stone = new("stone", 0f, 0.9f);
    private static readonly Mat Metal = new("metal", 0.9f, 0.4f);
    private static readonly Mat Cloth = new("cloth", 0f, 0.8f);
    private static readonly Mat Skin = new("skin", 0f, 0.55f);
    private static readonly Mat Water = new("water", 0f, 0.06f);
    private static readonly Mat Warm = new("lamp", 0f, 0.4f) { Emissive = new Vector3(1f, 0.62f, 0.25f), EmissiveStrength = 3f };

    // The island is 16 x 8 m of play area (the game's 384 x 192 units at 24 to the metre) plus a margin of rim around it.
    public const float PlayW = 16f;
    public const float PlayD = 8f;
    public const float Rim = 1.4f;

    public static IEnumerable<(string Name, Model Model)> All()
    {
        yield return ("island-a", Island(bridgeEast: true));
        yield return ("island-b", Island(bridgeEast: false));
        yield return ("bridge", Bridge(6.1f));
        yield return ("bridge-stub", Bridge(2.2f));
        yield return ("water", WaterPlane());
        yield return ("walker-me", Walker(Pal.Hex(0xF1E6D2), Pal.Hex(0xE08A2E), Pal.Hex(0xF2C14E)));
        yield return ("walker-other", Walker(Pal.Hex(0x6F86B8), Pal.Hex(0x2B3550), Pal.Hex(0x9CB2D8)));
        yield return ("lantern-red", LanternModel(0xE5484D, new Vector3(1f, 0.10f, 0.08f)));
        yield return ("lantern-blue", LanternModel(0x4C9BE8, new Vector3(0.12f, 0.45f, 1f)));
        yield return ("lantern-gold", LanternModel(0xF2C14E, new Vector3(1f, 0.62f, 0.10f)));
        // What a player is seen to carry when the colours are not theirs to show.
        yield return ("lantern-plain", LanternModel(0xD8DCE6, new Vector3(0.8f, 0.85f, 1f)));
        yield return ("pine", Pine());
        yield return ("oak", Oak());
        yield return ("rock", Rock());
        yield return ("bush", Bush());
    }

    private static float Rand(ref uint s)
    {
        s = (s * 1664525u) + 1013904223u;
        return (s >> 8) / 16777216f;
    }

    // ---- terrain ------------------------------------------------------------------------------------------------

    private static Vector2[] Outline(float hx, float hz, float rc, float d, int arc, out Vector2[] normals)
    {
        var pts = new List<Vector2>();
        var nrm = new List<Vector2>();
        Vector2[] centers = [new(hx - rc, hz - rc), new(-(hx - rc), hz - rc), new(-(hx - rc), -(hz - rc)), new(hx - rc, -(hz - rc))];
        for (var c = 0; c < 4; c++)
        {
            for (var k = 0; k <= arc; k++)
            {
                var t = (c * 90f + (k / (float)arc * 90f)) * MathF.PI / 180f;
                var dir = new Vector2(MathF.Cos(t), MathF.Sin(t));
                pts.Add(centers[c] + (dir * (rc - d)));
                nrm.Add(dir);
            }
        }

        normals = [.. nrm];
        return [.. pts];
    }

    private static Model Island(bool bridgeEast)
    {
        var model = new Model();
        var grass = Pal.Hex(0x4E8A3C);
        var grassDark = Pal.Hex(0x3F7633);
        var sand = Pal.Hex(0xD9C28F);
        var wet = Pal.Hex(0xA8946A);
        var rock = Pal.Hex(0x6E6A66);
        var rockDeep = Pal.Hex(0x3C3A3E);
        var hx = (PlayW / 2f) + Rim;
        var hz = (PlayD / 2f) + Rim;
        const float rc = 2.4f;
        const int arc = 7;
        (float D, float Y, Vector4 C, bool Hard)[] layers =
        [
            (0.95f, 0.00f, grass, false),
            (0.55f, 0.00f, grassDark, false),
            (0.05f, -0.16f, sand, false),
            (0.00f, -0.34f, wet, true),
            (0.08f, -0.55f, rock, true),
            (0.45f, -1.40f, rockDeep, false),
            (0.95f, -2.00f, rockDeep, false),
        ];
        var earth = model.On(Matte);
        var rings = new List<(uint[] Ids, Vector2[] Normals)>();
        var rockMesh = model.On(Stone);

        // Top: a fan from the middle to the first ring.
        var first = Outline(hx, hz, rc, layers[0].D, arc, out _);
        var hub = earth.Vertex(Vector3.Zero, Vector3.UnitY, grass);
        var topRing = first.Select(p => earth.Vertex(new Vector3(p.X, layers[0].Y, p.Y), Vector3.UnitY, layers[0].C)).ToArray();
        for (var j = 0; j < topRing.Length; j++)
        {
            earth.Tri(hub, topRing[j], topRing[(j + 1) % topRing.Length]);
        }

        // Bands between rings. Everything above the water line is soil and sand, everything below is stone.
        for (var k = 0; k + 1 < layers.Length; k++)
        {
            var a = layers[k];
            var b = layers[k + 1];
            var pa = Outline(hx, hz, rc, a.D, arc, out var na);
            var pb = Outline(hx, hz, rc, b.D, arc, out _);
            var dr = -b.D - -a.D;
            var dy = b.Y - a.Y;
            var bandN = new Vector2(-dy, dr);
            bandN = Vector2.Normalize(bandN);
            var mesh = b.Y > -0.5f ? earth : rockMesh;
            var ia = new uint[pa.Length];
            var ib = new uint[pb.Length];
            for (var j = 0; j < pa.Length; j++)
            {
                var o = na[j];
                var nA = bandN;
                var nB = bandN;
                if (!a.Hard && k > 0)
                {
                    nA = Vector2.Normalize(PrevBand(layers, k) + bandN);
                }

                if (!b.Hard && k + 2 < layers.Length)
                {
                    nB = Vector2.Normalize(bandN + PrevBand(layers, k + 1));
                }

                ia[j] = mesh.Vertex(new Vector3(pa[j].X, a.Y, pa[j].Y), new Vector3(nA.X * o.X, nA.Y, nA.X * o.Y), a.C);
                ib[j] = mesh.Vertex(new Vector3(pb[j].X, b.Y, pb[j].Y), new Vector3(nB.X * o.X, nB.Y, nB.X * o.Y), b.C);
            }

            for (var j = 0; j < pa.Length; j++)
            {
                var j2 = (j + 1) % pa.Length;
                mesh.Quad(ia[j], ib[j], ib[j2], ia[j2]);
            }

            rings.Add((ia, na));
        }

        // Meadow patches: a few slightly different greens so the ground does not read as one flat colour.
        uint seed = bridgeEast ? 11u : 29u;
        for (var i = 0; i < 16; i++)
        {
            var rx = 0.8f + (Rand(ref seed) * 1.4f);
            var rz = 0.6f + (Rand(ref seed) * 1.0f);
            var x = ((Rand(ref seed) - 0.5f) * (PlayW - 1f));
            var z = ((Rand(ref seed) - 0.5f) * (PlayD - 1f));
            var tone = Pal.Mix(grass, i % 3 == 0 ? Pal.Hex(0x5E9A45) : Pal.Hex(0x447F36), 0.8f);
            model.Disc(Matte, new Vector3(x, 0.012f + (i * 0.0015f), z), rx, rz, tone);
        }

        // Flower dots, small and bright: nothing to do with play, only a bit of life.
        for (var i = 0; i < 46; i++)
        {
            var x = (Rand(ref seed) - 0.5f) * (PlayW + 1.5f);
            var z = (Rand(ref seed) - 0.5f) * (PlayD + 1.5f);
            var c = (i % 4) switch { 0 => Pal.Hex(0xF4F0E0), 1 => Pal.Hex(0xE9B7D0), 2 => Pal.Hex(0xF7E08A), _ => Pal.Hex(0xB9D6F2) };
            model.Disc(Matte, new Vector3(x, 0.05f + (i * 0.0002f), z), 0.07f, 0.07f, c, 6);
        }

        // Landing pad where the bridge meets the island, with two posts that carry lamps.
        var side = bridgeEast ? 1f : -1f;
        var padX = side * ((PlayW / 2f) + 0.7f);
        for (var i = 0; i < 7; i++)
        {
            model.Box(Wood, new Vector3(padX + (side * (i - 3) * 0.0f), 0.04f, ((i - 3) * 0.26f)), new Vector3(1.9f, 0.1f, 0.24f), Pal.Mix(Pal.Hex(0x9B7448), Pal.Hex(0x7C5A38), (i % 3) / 2f));
        }

        for (var s = -1; s <= 1; s += 2)
        {
            var px = side * ((PlayW / 2f) + 1.5f);
            var pz = s * 1.15f;
            model.Box(Wood, new Vector3(px, 0.55f, pz), new Vector3(0.14f, 1.1f, 0.14f), Pal.Hex(0x5B4128));
            model.Box(Metal, new Vector3(px, 1.14f, pz), new Vector3(0.26f, 0.05f, 0.26f), Pal.Hex(0x2B2B30));
            model.Box(Warm, new Vector3(px, 1.28f, pz), new Vector3(0.18f, 0.24f, 0.18f), Pal.Hex(0xFFD28A));
            model.Box(Metal, new Vector3(px, 1.44f, pz), new Vector3(0.28f, 0.05f, 0.28f), Pal.Hex(0x2B2B30));
        }

        return model;
    }

    private static Vector2 PrevBand((float D, float Y, Vector4 C, bool Hard)[] layers, int k)
    {
        var a = layers[k - 1];
        var b = layers[k];
        return Vector2.Normalize(new Vector2(-(b.Y - a.Y), -b.D - -a.D));
    }

    private static Model WaterPlane()
    {
        var model = new Model();
        var mesh = model.On(Water);
        const float h = 140f;
        var c = Pal.Hex(0x1C6F86);
        mesh.Quad(mesh.Vertex(new Vector3(-h, 0, -h), Vector3.UnitY, c), mesh.Vertex(new Vector3(-h, 0, h), Vector3.UnitY, c), mesh.Vertex(new Vector3(h, 0, h), Vector3.UnitY, c), mesh.Vertex(new Vector3(h, 0, -h), Vector3.UnitY, c));
        return model;
    }

    private static Model Bridge(float length)
    {
        // Along X, centred, deck top at y = 0.1 so it sits just over the pad; 1.7 m wide.
        var model = new Model();
        const float width = 1.7f;
        var planks = (int)MathF.Round(length / 0.26f);
        var step = length / planks;
        uint seed = 5;
        for (var i = 0; i < planks; i++)
        {
            var x = (-length / 2f) + ((i + 0.5f) * step);
            var tone = Pal.Mix(Pal.Hex(0xA37C4E), Pal.Hex(0x7E5C3A), Rand(ref seed));
            model.Box(Wood, new Vector3(x, 0.05f, 0f), new Vector3(step - 0.03f, 0.1f, width), tone);
        }

        foreach (var z in new[] { -0.7f, 0f, 0.7f })
        {
            model.Box(Wood, new Vector3(0f, -0.06f, z), new Vector3(length, 0.12f, 0.14f), Pal.Hex(0x4D3822));
        }

        var posts = Math.Max(2, (int)MathF.Round(length / 1.2f) + 1);
        for (var i = 0; i < posts; i++)
        {
            var x = (-length / 2f) + 0.1f + (i * ((length - 0.2f) / (posts - 1)));
            foreach (var z in new[] { -width / 2f, width / 2f })
            {
                model.Box(Wood, new Vector3(x, 0.52f, z), new Vector3(0.12f, 0.84f, 0.12f), Pal.Hex(0x6A4C2E));
                model.Cylinder(Wood, new Vector3(x, -1.2f, z * 0.8f), 0.07f, 1.3f, Pal.Hex(0x3A2A1A), 7);
            }
        }

        foreach (var z in new[] { -width / 2f, width / 2f })
        {
            model.Box(Wood, new Vector3(0f, 0.8f, z), new Vector3(length, 0.07f, 0.07f), Pal.Hex(0x8A6840));
            model.Box(Wood, new Vector3(0f, 0.5f, z), new Vector3(length, 0.05f, 0.05f), Pal.Hex(0x7A5A38));
        }

        return model;
    }

    // ---- people and lanterns ------------------------------------------------------------------------------------

    private static Model Walker(Vector4 cloak, Vector4 hat, Vector4 scarf)
    {
        var model = new Model();
        var lower = Pal.Mix(cloak, Pal.Hex(0x000000), 0.18f);
        model.Lathe(Cloth, Vector3.Zero, [(0f, 0.02f, lower), (0.30f, 0.02f, lower), (0.27f, 0.16f, cloak), (0.19f, 0.46f, cloak), (0.13f, 0.62f, cloak), (0f, 0.64f, cloak)], 14);
        model.Lathe(Cloth, new Vector3(0f, 0.62f, 0f), [(0f, -0.04f, scarf), (0.145f, -0.04f, scarf), (0.16f, 0.0f, scarf), (0.145f, 0.05f, scarf), (0f, 0.05f, scarf)], 14);
        model.Sphere(Skin, new Vector3(0f, 0.78f, 0f), 0.13f, Pal.Hex(0xF0C9A4), 12, 6);
        model.Lathe(Cloth, new Vector3(0f, 0.86f, 0f), [(0f, 0f, hat), (0.20f, 0f, hat), (0.12f, 0.02f, hat), (0.10f, 0.12f, hat), (0f, 0.26f, hat)], 12, smooth: false);
        model.Sphere(Matte, new Vector3(-0.05f, 0.80f, -0.115f), 0.022f, Pal.Hex(0x1B1B20), 6, 4);
        model.Sphere(Matte, new Vector3(0.05f, 0.80f, -0.115f), 0.022f, Pal.Hex(0x1B1B20), 6, 4);
        return model;
    }

    private static Model LanternModel(int hex, Vector3 glow)
    {
        var model = new Model();
        var paper = new Mat("paper-" + hex.ToString("X6", CultureInfo.InvariantCulture), 0f, 0.5f) { Emissive = glow, EmissiveStrength = 2.2f };
        var iron = Pal.Hex(0x2A2B31);
        var c = Pal.Hex(hex);
        model.Cylinder(Metal, new Vector3(0, 0, 0), 0.13f, 0.04f, iron, 10);
        model.Cylinder(Metal, new Vector3(0, 0.04f, 0), 0.03f, 0.08f, iron, 8);
        model.Lathe(paper, new Vector3(0, 0.12f, 0), [(0f, 0f, c), (0.09f, 0f, c), (0.15f, 0.07f, c), (0.17f, 0.17f, c), (0.15f, 0.27f, c), (0.09f, 0.33f, c), (0f, 0.33f, c)], 12);
        model.Cylinder(Metal, new Vector3(0, 0.45f, 0), 0.10f, 0.03f, iron, 10);
        model.Cone(Metal, new Vector3(0, 0.48f, 0), 0.11f, 0.07f, iron, 10);
        model.Lathe(Metal, new Vector3(0, 0.55f, 0), [(0.0f, 0.0f, iron), (0.03f, 0f, iron), (0.03f, 0.03f, iron), (0f, 0.03f, iron)], 8, smooth: false);
        return model;
    }

    // ---- scenery ------------------------------------------------------------------------------------------------

    private static Model Pine()
    {
        var model = new Model();
        model.Cylinder(Wood, Vector3.Zero, 0.12f, 0.7f, Pal.Hex(0x5A3F28), 7);
        var tones = new[] { Pal.Hex(0x2C5E3A), Pal.Hex(0x336B41), Pal.Hex(0x3C7A4A) };
        for (var i = 0; i < 3; i++)
        {
            model.Cone(Matte, new Vector3(0, 0.55f + (i * 0.62f), 0), 0.95f - (i * 0.25f), 1.15f - (i * 0.1f), tones[i], 9);
        }

        return model;
    }

    private static Model Oak()
    {
        var model = new Model();
        model.Cylinder(Wood, Vector3.Zero, 0.16f, 1.1f, Pal.Hex(0x65472C), 7);
        model.Sphere(Matte, new Vector3(0, 1.75f, 0), 0.95f, Pal.Hex(0x4F9444), 10, 6, 1f, 0.85f, 1f);
        model.Sphere(Matte, new Vector3(0.55f, 1.45f, 0.15f), 0.6f, Pal.Hex(0x58A04A), 9, 5);
        model.Sphere(Matte, new Vector3(-0.5f, 1.5f, -0.2f), 0.58f, Pal.Hex(0x468A3E), 9, 5);
        return model;
    }

    private static Model Rock()
    {
        var model = new Model();
        var mesh = model.On(Stone);
        uint seed = 77;
        const int seg = 8;
        const int rings = 4;
        var grid = new uint[rings + 1, seg];
        var tops = new Vector3[rings + 1, seg];
        for (var i = 0; i <= rings; i++)
        {
            var a = i / (float)rings * (MathF.PI / 2f);
            for (var j = 0; j < seg; j++)
            {
                var t = j / (float)seg * MathF.Tau;
                var r = (i == rings ? 0f : MathF.Cos(a)) * (0.55f + (Rand(ref seed) * 0.25f));
                tops[i, j] = new Vector3(r * MathF.Cos(t), (MathF.Sin(a) * 0.5f) - 0.06f, r * MathF.Sin(t));
            }
        }

        for (var i = 0; i <= rings; i++)
        {
            for (var j = 0; j < seg; j++)
            {
                var p = tops[i, j];
                var shade = Pal.Mix(Pal.Hex(0x77747A), Pal.Hex(0xA09DA2), p.Y + 0.1f);
                grid[i, j] = mesh.Vertex(p, Vector3.Normalize(new Vector3(p.X, MathF.Max(p.Y + 0.35f, 0.15f), p.Z)), shade);
            }
        }

        for (var i = 0; i < rings; i++)
        {
            for (var j = 0; j < seg; j++)
            {
                var j2 = (j + 1) % seg;
                mesh.Quad(grid[i, j], grid[i + 1, j], grid[i + 1, j2], grid[i, j2]);
            }
        }

        return model;
    }

    private static Model Bush()
    {
        var model = new Model();
        model.Sphere(Matte, new Vector3(0, 0.28f, 0), 0.5f, Pal.Hex(0x3F8A3F), 9, 5, 1f, 0.7f, 1f);
        model.Sphere(Matte, new Vector3(0.38f, 0.22f, 0.1f), 0.32f, Pal.Hex(0x4A9A48), 8, 4, 1f, 0.7f, 1f);
        return model;
    }
}

/// <summary>Writes a <see cref="Model"/> as a binary glTF 2.0 file.</summary>
static class Glb
{
    public static byte[] Write(Model model)
    {
        var bin = new MemoryStream();
        var views = new StringBuilder();
        var accessors = new StringBuilder();
        var prims = new StringBuilder();
        var materials = new StringBuilder();
        var accessorCount = 0;
        var viewCount = 0;

        int AddView(ReadOnlySpan<byte> data, int target)
        {
            while (bin.Length % 4 != 0)
            {
                bin.WriteByte(0);
            }

            if (viewCount > 0)
            {
                views.Append(',');
            }

            views.Append(CultureInfo.InvariantCulture, $"{{\"buffer\":0,\"byteOffset\":{bin.Length},\"byteLength\":{data.Length},\"target\":{target}}}");
            bin.Write(data);
            return viewCount++;
        }

        int AddAccessor(int view, int component, int count, string type, string? bounds = null)
        {
            if (accessorCount > 0)
            {
                accessors.Append(',');
            }

            accessors.Append(CultureInfo.InvariantCulture, $"{{\"bufferView\":{view},\"componentType\":{component},\"count\":{count},\"type\":\"{type}\"{bounds}}}");
            return accessorCount++;
        }

        var first = true;
        foreach (var mesh in model.Meshes)
        {
            Validate(mesh);
            var count = mesh.Positions.Count;
            var pos = new byte[count * 12];
            var nrm = new byte[count * 12];
            var col = new byte[count * 16];
            Vector3 lo = new(float.MaxValue), hi = new(float.MinValue);
            for (var i = 0; i < count; i++)
            {
                Put(pos, i * 12, mesh.Positions[i]);
                Put(nrm, i * 12, mesh.Normals[i]);
                BitConverter.TryWriteBytes(col.AsSpan((i * 16), 4), mesh.Colors[i].X);
                BitConverter.TryWriteBytes(col.AsSpan((i * 16) + 4, 4), mesh.Colors[i].Y);
                BitConverter.TryWriteBytes(col.AsSpan((i * 16) + 8, 4), mesh.Colors[i].Z);
                BitConverter.TryWriteBytes(col.AsSpan((i * 16) + 12, 4), mesh.Colors[i].W);
                lo = Vector3.Min(lo, mesh.Positions[i]);
                hi = Vector3.Max(hi, mesh.Positions[i]);
            }

            var idx = new byte[mesh.Indices.Count * 4];
            for (var i = 0; i < mesh.Indices.Count; i++)
            {
                BitConverter.TryWriteBytes(idx.AsSpan(i * 4, 4), mesh.Indices[i]);
            }

            string F(float v) => v.ToString("R", CultureInfo.InvariantCulture);
            var bounds = $",\"min\":[{F(lo.X)},{F(lo.Y)},{F(lo.Z)}],\"max\":[{F(hi.X)},{F(hi.Y)},{F(hi.Z)}]";
            var aPos = AddAccessor(AddView(pos, 34962), 5126, count, "VEC3", bounds);
            var aNrm = AddAccessor(AddView(nrm, 34962), 5126, count, "VEC3");
            var aCol = AddAccessor(AddView(col, 34962), 5126, count, "VEC4");
            var aIdx = AddAccessor(AddView(idx, 34963), 5125, mesh.Indices.Count, "SCALAR");
            var m = mesh.Mat;
            var matIndex = model.Meshes.IndexOf(mesh);
            if (!first)
            {
                prims.Append(',');
                materials.Append(',');
            }

            first = false;
            prims.Append(CultureInfo.InvariantCulture, $"{{\"attributes\":{{\"POSITION\":{aPos},\"NORMAL\":{aNrm},\"COLOR_0\":{aCol}}},\"indices\":{aIdx},\"material\":{matIndex}}}");
            var ext = m.Emissive != Vector3.Zero ? $",\"extensions\":{{\"KHR_materials_emissive_strength\":{{\"emissiveStrength\":{F(m.EmissiveStrength)}}}}}" : string.Empty;
            var emis = m.Emissive != Vector3.Zero ? $",\"emissiveFactor\":[{F(m.Emissive.X)},{F(m.Emissive.Y)},{F(m.Emissive.Z)}]" : string.Empty;
            var alpha = m.Blend ? ",\"alphaMode\":\"BLEND\"" : string.Empty;
            var dbl = m.DoubleSided ? ",\"doubleSided\":true" : string.Empty;
            materials.Append(CultureInfo.InvariantCulture, $"{{\"name\":\"{m.Name}\",\"pbrMetallicRoughness\":{{\"baseColorFactor\":[1,1,1,1],\"metallicFactor\":{F(m.Metal)},\"roughnessFactor\":{F(m.Rough)}}}{emis}{alpha}{dbl}{ext}}}");
        }

        while (bin.Length % 4 != 0)
        {
            bin.WriteByte(0);
        }

        var json = $"{{\"asset\":{{\"version\":\"2.0\",\"generator\":\"lanterns art\"}},\"extensionsUsed\":[\"KHR_materials_emissive_strength\"],\"scene\":0,\"scenes\":[{{\"nodes\":[0]}}],\"nodes\":[{{\"mesh\":0}}],\"meshes\":[{{\"primitives\":[{prims}]}}],\"materials\":[{materials}],\"accessors\":[{accessors}],\"bufferViews\":[{views}],\"buffers\":[{{\"byteLength\":{bin.Length}}}]}}";
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var pad = (4 - (jsonBytes.Length % 4)) % 4;
        var jsonLen = jsonBytes.Length + pad;
        using var output = new MemoryStream();
        using var w = new BinaryWriter(output);
        w.Write(0x46546C67u);
        w.Write(2u);
        w.Write((uint)(12 + 8 + jsonLen + 8 + bin.Length));
        w.Write((uint)jsonLen);
        w.Write(0x4E4F534Au);
        w.Write(jsonBytes);
        for (var i = 0; i < pad; i++)
        {
            w.Write((byte)0x20);
        }

        w.Write((uint)bin.Length);
        w.Write(0x004E4942u);
        w.Write(bin.ToArray());
        w.Flush();
        return output.ToArray();
    }

    private static void Put(byte[] to, int at, Vector3 v)
    {
        BitConverter.TryWriteBytes(to.AsSpan(at, 4), v.X);
        BitConverter.TryWriteBytes(to.AsSpan(at + 4, 4), v.Y);
        BitConverter.TryWriteBytes(to.AsSpan(at + 8, 4), v.Z);
    }

    // A face that points against its own normals would render dark or vanish; catch it here rather than in a screenshot.
    private static void Validate(Mesh mesh)
    {
        var bad = 0;
        for (var i = 0; i < mesh.Indices.Count; i += 3)
        {
            var a = (int)mesh.Indices[i];
            var b = (int)mesh.Indices[i + 1];
            var c = (int)mesh.Indices[i + 2];
            var g = Vector3.Cross(mesh.Positions[b] - mesh.Positions[a], mesh.Positions[c] - mesh.Positions[a]);
            if (g.LengthSquared() > 1e-12f && Vector3.Dot(g, mesh.Normals[a] + mesh.Normals[b] + mesh.Normals[c]) < 0f)
            {
                bad++;
            }
        }

        if (bad > 0)
        {
            throw new InvalidOperationException($"{bad} triangles of material '{mesh.Mat.Name}' face away from their normals.");
        }
    }
}
