using System.Globalization;
using System.Numerics;
using System.Text;

// Writes the example character as a binary glTF 2.0 file: a box-built figure with a 14-joint skeleton, a skin, and the clips idle, walk,
// run and jump. The art is plain code so anyone can read and change it; the output is deterministic.
//   dotnet run --project examples/character/art -- examples/character/models
// Units are metres, +Y up, the figure faces -Z (+X is its right). The legs of walk and run are solved with two-bone IK against a foot path
// that moves backwards at exactly the root's speed during the stance phase, so planted feet do not slide.
var outDir = args.Length > 0 ? args[0] : "models";
Directory.CreateDirectory(outDir);
foreach (var (name, bytes) in new[] { ("figure", Figure.Write(skinned: true)), ("ground", Figure.Write(skinned: false)) })
{
    File.WriteAllBytes(Path.Combine(outDir, name + ".glb"), bytes);
    Console.WriteLine($"{name}.glb  {bytes.Length} bytes");
}
return 0;

/// <summary>One joint of the skeleton.</summary>
sealed record Joint(string Name, int Parent, Vector3 Local);

static class Figure
{
    public const float L1 = 0.43f;
    public const float L2 = 0.43f;
    public const float AnkleY = 0.05f;
    public const float ThighDrop = 0.04f;

    // Order matters: parents before children, and the skin's joint order is this order.
    public static readonly Joint[] Joints =
    [
        new("Root", -1, new Vector3(0, 0, 0)),
        new("Hips", 0, new Vector3(0, 0.95f, 0)),
        new("Chest", 1, new Vector3(0, 0.22f, 0)),
        new("Head", 2, new Vector3(0, 0.40f, 0)),
        new("UpperArmR", 2, new Vector3(0.24f, 0.30f, 0)),
        new("LowerArmR", 4, new Vector3(0, -0.28f, 0)),
        new("UpperArmL", 2, new Vector3(-0.24f, 0.30f, 0)),
        new("LowerArmL", 6, new Vector3(0, -0.28f, 0)),
        new("ThighR", 1, new Vector3(0.1f, -ThighDrop, 0)),
        new("ShinR", 8, new Vector3(0, -L1, 0)),
        new("FootR", 9, new Vector3(0, -L2, 0)),
        new("ThighL", 1, new Vector3(-0.1f, -ThighDrop, 0)),
        new("ShinL", 11, new Vector3(0, -L1, 0)),
        new("FootL", 12, new Vector3(0, -L2, 0)),
    ];

    static int J(string name) => Array.FindIndex(Joints, j => j.Name == name);

    static Vector3 RestWorld(int joint)
    {
        var p = Vector3.Zero;
        for (var n = joint; n >= 0; n = Joints[n].Parent)
        {
            p += Joints[n].Local;
        }

        return p;
    }

    // ---------------------------------------------------------------------------------------------------------------- geometry

    sealed class Geo
    {
        public readonly List<Vector3> P = [];
        public readonly List<Vector3> N = [];
        public readonly List<Vector4> C = [];
        public readonly List<byte> Bone = [];
        public readonly List<ushort> I = [];

        public void Box(string joint, Vector3 center, Vector3 size, int rgb)
        {
            var color = Linear(rgb);
            var b = (byte)J(joint);
            var h = size / 2f;
            (Vector3 N, Vector3[] C)[] faces =
            [
                (Vector3.UnitX, [Q(1, -1, -1), Q(1, 1, -1), Q(1, 1, 1), Q(1, -1, 1)]),
                (-Vector3.UnitX, [Q(-1, -1, 1), Q(-1, 1, 1), Q(-1, 1, -1), Q(-1, -1, -1)]),
                (Vector3.UnitY, [Q(-1, 1, -1), Q(-1, 1, 1), Q(1, 1, 1), Q(1, 1, -1)]),
                (-Vector3.UnitY, [Q(-1, -1, 1), Q(-1, -1, -1), Q(1, -1, -1), Q(1, -1, 1)]),
                (Vector3.UnitZ, [Q(-1, -1, 1), Q(1, -1, 1), Q(1, 1, 1), Q(-1, 1, 1)]),
                (-Vector3.UnitZ, [Q(1, -1, -1), Q(-1, -1, -1), Q(-1, 1, -1), Q(1, 1, -1)]),
            ];
            Vector3 Q(float x, float y, float z) => center + new Vector3(x * h.X, y * h.Y, z * h.Z);
            foreach (var (n, c) in faces)
            {
                var first = (ushort)P.Count;
                foreach (var corner in c)
                {
                    P.Add(corner);
                    N.Add(n);
                    C.Add(color);
                    Bone.Add(b);
                }

                // Counter-clockwise seen from outside.
                I.AddRange([first, (ushort)(first + 1), (ushort)(first + 2), first, (ushort)(first + 2), (ushort)(first + 3)]);
            }
        }
    }

    static Vector4 Linear(int rgb)
    {
        static float Lin(float c) => c <= 0.04045f ? c / 12.92f : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
        return new Vector4(Lin(((rgb >> 16) & 255) / 255f), Lin(((rgb >> 8) & 255) / 255f), Lin((rgb & 255) / 255f), 1f);
    }

    // A floor of one-metre tiles in two shades so that movement across it can be seen: x from -10 to 10, z from -60 to 10.
    static Geo Floor()
    {
        var g = new Geo();
        var light = Linear(0x8a9a8c);
        var dark = Linear(0x748476);
        for (var x = -10; x < 10; x++)
        {
            for (var z = -60; z < 10; z++)
            {
                var first = (ushort)g.P.Count;
                foreach (var (px, pz) in new[] { (x, z + 1), (x + 1, z + 1), (x + 1, z), (x, z) })
                {
                    g.P.Add(new Vector3(px, 0, pz));
                    g.N.Add(Vector3.UnitY);
                    g.C.Add(((x + z) & 1) == 0 ? light : dark);
                    g.Bone.Add(0);
                }

                g.I.AddRange([first, (ushort)(first + 1), (ushort)(first + 2), first, (ushort)(first + 2), (ushort)(first + 3)]);
            }
        }

        return g;
    }

    static Geo Body()
    {
        const int shirt = 0x3d7ea6, skin = 0xe8b894, trousers = 0x2f3e56, shoes = 0x3a2a24, dark = 0x22262b;
        var g = new Geo();
        g.Box("Hips", new Vector3(0, 0.95f, 0), new Vector3(0.30f, 0.20f, 0.18f), trousers);
        g.Box("Chest", new Vector3(0, 1.17f + 0.14f, 0), new Vector3(0.38f, 0.52f, 0.20f), shirt);
        g.Box("Head", new Vector3(0, 1.57f + 0.10f, 0), new Vector3(0.22f, 0.24f, 0.22f), skin);
        g.Box("Head", new Vector3(0, 1.57f + 0.11f, -0.12f), new Vector3(0.14f, 0.05f, 0.03f), dark);
        foreach (var (s, x) in new[] { ("R", 0.24f), ("L", -0.24f) })
        {
            g.Box("UpperArm" + s, new Vector3(x, 1.47f - 0.14f, 0), new Vector3(0.10f, 0.30f, 0.10f), shirt);
            g.Box("LowerArm" + s, new Vector3(x, 1.19f - 0.13f, 0), new Vector3(0.09f, 0.28f, 0.09f), skin);
        }

        foreach (var (s, x) in new[] { ("R", 0.1f), ("L", -0.1f) })
        {
            g.Box("Thigh" + s, new Vector3(x, 0.91f - 0.215f, 0), new Vector3(0.13f, 0.43f, 0.13f), trousers);
            g.Box("Shin" + s, new Vector3(x, 0.48f - 0.215f, 0), new Vector3(0.11f, 0.43f, 0.11f), trousers);
            g.Box("Foot" + s, new Vector3(x, 0.03f, -0.05f), new Vector3(0.12f, 0.08f, 0.26f), shoes);
        }

        return g;
    }

    // ---------------------------------------------------------------------------------------------------------------- animation

    /// <summary>A named clip: for each channel the key values; all channels share the key times.</summary>
    sealed class Clip(string name, float[] times)
    {
        public string Name { get; } = name;
        public float[] Times { get; } = times;
        public List<(int Node, bool Rotation, Vector4[] Values)> Tracks { get; } = [];

        public void Rot(string joint, Func<int, float> angleX) =>
            Tracks.Add((J(joint), true, Times.Select((_, k) => { var q = Quaternion.CreateFromAxisAngle(Vector3.UnitX, angleX(k)); return new Vector4(q.X, q.Y, q.Z, q.W); }).ToArray()));

        public void Move(string joint, Func<int, Vector3> position) =>
            Tracks.Add((J(joint), false, Times.Select((_, k) => { var p = position(k); return new Vector4(p.X, p.Y, p.Z, 0); }).ToArray()));
    }

    static float Smooth(float w) => w * w * (3f - (2f * w));

    /// <summary>Planar two-bone solve in (forward, down) with the knee forward: returns thigh, shin and foot rotations about X.</summary>
    static (float Thigh, float Shin, float Foot) Leg(float f, float d)
    {
        var r = MathF.Min(MathF.Sqrt((f * f) + (d * d)), (L1 + L2) - 1e-4f);
        var beta = MathF.Acos(((L1 * L1) + (r * r) - (L2 * L2)) / (2f * L1 * r));
        var t1 = MathF.Atan2(f, d) + beta;
        var kf = L1 * MathF.Sin(t1);
        var kd = L1 * MathF.Cos(t1);
        var t2 = MathF.Atan2(f - kf, d - kd);
        return (t1, t2 - t1, -t2);
    }

    // Walk or run: one cycle of `period` seconds; the foot is planted for `duty` of it and then moves back (in the hips' frame) by 2*half
    // at exactly the root's speed, which is 2*half / (duty * period).
    static Clip Locomotion(string name, float period, float duty, float half, float lift, float hipsY, float bob, float armSwing, float elbow)
    {
        const int N = 48;
        var times = Enumerable.Range(0, N + 1).Select(k => period * k / N).ToArray();
        var speed = 2f * half / (duty * period);
        var c = new Clip(name, times);
        float Phase(int k, float offset) { var u = (k / (float)N) + offset; return u - MathF.Floor(u); }
        (float F, float Lift) Foot(int k, float offset)
        {
            var u = Phase(k, offset);
            if (u < duty)
            {
                return (half * (1f - (2f * u / duty)), 0f);
            }

            var w = (u - duty) / (1f - duty);
            return (-half + (2f * half * Smooth(w)), lift * MathF.Sin(MathF.PI * w));
        }

        float HipsY(int k) => hipsY + (bob * MathF.Cos(4f * MathF.PI * k / N));
        foreach (var (s, offset) in new[] { ("R", 0f), ("L", 0.5f) })
        {
            (float T, float S, float F) Solve(int k)
            {
                var (f, l) = Foot(k, offset);
                return Leg(f, HipsY(k) - ThighDrop - AnkleY - l) is var (t, sh, fo) ? (t, sh, fo) : default;
            }

            c.Rot("Thigh" + s, k => Solve(k).T);
            c.Rot("Shin" + s, k => Solve(k).S);
            c.Rot("Foot" + s, k => Solve(k).F);
        }

        // Arms swing against the legs: the right arm with the left foot's position.
        c.Rot("UpperArmR", k => armSwing * Foot(k, 0.5f).F / half);
        c.Rot("UpperArmL", k => armSwing * Foot(k, 0f).F / half);
        c.Rot("LowerArmR", _ => elbow);
        c.Rot("LowerArmL", _ => elbow);
        c.Move("Hips", k => new Vector3(0, HipsY(k), 0));
        c.Move("Root", k => new Vector3(0, 0, -speed * times[k]));
        return c;
    }

    static Clip Idle()
    {
        const int N = 8;
        var times = Enumerable.Range(0, N + 1).Select(k => 2f * k / N).ToArray();
        var c = new Clip("idle", times);
        float Wave(int k) => MathF.Sin(MathF.Tau * k / N);
        c.Move("Hips", k => new Vector3(0, 0.95f - (0.006f * (0.5f - (0.5f * MathF.Cos(MathF.Tau * k / N)))), 0));
        c.Rot("Chest", k => 0.025f * Wave(k));
        c.Rot("UpperArmR", k => 0.04f * Wave(k));
        c.Rot("UpperArmL", k => -0.04f * Wave(k));
        c.Rot("LowerArmR", _ => 0.08f);
        c.Rot("LowerArmL", _ => 0.08f);
        return c;
    }

    // Crouch, launch, a tucked flight on a parabola, landing. The root carries the jump (up 0.5 m and forward 0.6 m); the legs only pose.
    static Clip Jump()
    {
        const int N = 32;
        const float T = 0.8f;
        var times = Enumerable.Range(0, N + 1).Select(k => T * k / N).ToArray();
        var c = new Clip("jump", times);
        float AirU(int k) => Math.Clamp((times[k] - 0.3f) / 0.35f, 0f, 1f);
        float HipsY(int k)
        {
            var t = times[k];
            return t switch
            {
                < 0.2f => 0.95f - (0.20f * Smooth(t / 0.2f)),
                < 0.3f => 0.75f + (0.22f * Smooth((t - 0.2f) / 0.1f)),
                < 0.65f => 0.97f,
                _ => 0.97f - (0.20f * Smooth((t - 0.65f) / 0.075f)) + (t > 0.725f ? 0.18f * Smooth((t - 0.725f) / 0.075f) : 0f),
            };
        }

        (float F, float D) FootTarget(int k)
        {
            var u = AirU(k);
            var air = MathF.Sin(MathF.PI * u);
            var hips = HipsY(k);
            var ground = hips - ThighDrop - AnkleY;
            return (0.12f * air, Math.Max(0.3f, ground - (air * 0.18f)));
        }

        foreach (var s in new[] { "R", "L" })
        {
            c.Rot("Thigh" + s, k => Leg(FootTarget(k).F, FootTarget(k).D).Thigh);
            c.Rot("Shin" + s, k => Leg(FootTarget(k).F, FootTarget(k).D).Shin);
            c.Rot("Foot" + s, k => Leg(FootTarget(k).F, FootTarget(k).D).Foot);
        }

        float Arms(int k) => times[k] < 0.2f ? -0.6f * Smooth(times[k] / 0.2f) : -0.6f + (2.2f * Smooth(Math.Clamp((times[k] - 0.2f) / 0.2f, 0f, 1f))) - (1.0f * Smooth(Math.Clamp((times[k] - 0.55f) / 0.2f, 0f, 1f)));
        c.Rot("UpperArmR", Arms);
        c.Rot("UpperArmL", Arms);
        c.Rot("LowerArmR", _ => 0.3f);
        c.Rot("LowerArmL", _ => 0.3f);
        c.Move("Hips", k => new Vector3(0, HipsY(k), 0));
        c.Move("Root", k =>
        {
            var u = AirU(k);
            return new Vector3(0, 0.5f * 4f * u * (1f - u), -0.6f * u);
        });
        return c;
    }

    // ---------------------------------------------------------------------------------------------------------------- glTF writer

    static readonly float[] Weight = [1f, 0f, 0f, 0f];

    static byte[] JointBytes(List<byte> bones)
    {
        var data = new byte[bones.Count * 4];
        for (var i = 0; i < bones.Count; i++)
        {
            data[i * 4] = bones[i];
        }

        return data;
    }

    static string F(float v) => v.ToString("0.#######", CultureInfo.InvariantCulture);

    public static byte[] Write(bool skinned)
    {
        var geo = skinned ? Body() : Floor();
        var clips = !skinned ? [] : new[]
        {
            Idle(),
            Locomotion("walk", 1.0f, 0.55f, 0.275f, 0.07f, 0.89f, 0.008f, 0.45f, 0.25f),
            Locomotion("run", 0.7f, 0.35f, 0.37f, 0.16f, 0.82f, 0.03f, 0.9f, 1.3f),
            Jump(),
        };

        var bin = new MemoryStream();
        var views = new StringBuilder();
        var accessors = new StringBuilder();
        var viewCount = 0;
        var accessorCount = 0;
        int View(byte[] data, int target = 0)
        {
            while (bin.Length % 4 != 0)
            {
                bin.WriteByte(0);
            }

            var offset = (int)bin.Length;
            bin.Write(data);
            if (views.Length > 0)
            {
                views.Append(',');
            }

            views.Append(CultureInfo.InvariantCulture, $"{{\"buffer\":0,\"byteOffset\":{offset},\"byteLength\":{data.Length}{(target != 0 ? $",\"target\":{target}" : string.Empty)}}}");
            return viewCount++;
        }

        int Accessor(byte[] data, int component, int count, string type, string extra = "", int target = 0)
        {
            var view = View(data, target);
            if (accessors.Length > 0)
            {
                accessors.Append(',');
            }

            accessors.Append(CultureInfo.InvariantCulture, $"{{\"bufferView\":{view},\"componentType\":{component},\"count\":{count},\"type\":\"{type}\"{extra}}}");
            return accessorCount++;
        }

        byte[] Floats(IEnumerable<float> values)
        {
            var list = values.ToList();
            var b = new byte[list.Count * 4];
            for (var i = 0; i < list.Count; i++)
            {
                BitConverter.TryWriteBytes(b.AsSpan(i * 4, 4), list[i]);
            }

            return b;
        }

        var n = geo.P.Count;
        var min = geo.P.Aggregate(Vector3.Min);
        var max = geo.P.Aggregate(Vector3.Max);
        var aPos = Accessor(Floats(geo.P.SelectMany(p => new[] { p.X, p.Y, p.Z })), 5126, n, "VEC3", $",\"min\":[{F(min.X)},{F(min.Y)},{F(min.Z)}],\"max\":[{F(max.X)},{F(max.Y)},{F(max.Z)}]", 34962);
        var aNor = Accessor(Floats(geo.N.SelectMany(p => new[] { p.X, p.Y, p.Z })), 5126, n, "VEC3", string.Empty, 34962);
        var aCol = Accessor(Floats(geo.C.SelectMany(p => new[] { p.X, p.Y, p.Z, p.W })), 5126, n, "VEC4", string.Empty, 34962);
        var aJoints = skinned ? Accessor(JointBytes(geo.Bone), 5121, n, "VEC4", string.Empty, 34962) : -1;
        var aWeights = skinned ? Accessor(Floats(geo.Bone.SelectMany(_ => Weight)), 5126, n, "VEC4", string.Empty, 34962) : -1;
        var idx = new byte[geo.I.Count * 2];
        for (var i = 0; i < geo.I.Count; i++)
        {
            BitConverter.TryWriteBytes(idx.AsSpan(i * 2, 2), geo.I[i]);
        }

        var aIdx = Accessor(idx, 5123, geo.I.Count, "SCALAR", string.Empty, 34963);
        var ibm = Floats(Joints.Select((_, i) => Matrix4x4.CreateTranslation(-RestWorld(i))).SelectMany(m => new[]
        {
            m.M11, m.M12, m.M13, m.M14, m.M21, m.M22, m.M23, m.M24, m.M31, m.M32, m.M33, m.M34, m.M41, m.M42, m.M43, m.M44,
        }));
        var aIbm = skinned ? Accessor(ibm, 5126, Joints.Length, "MAT4") : -1;

        var anims = new StringBuilder();
        foreach (var clip in clips)
        {
            var time = Accessor(Floats(clip.Times), 5126, clip.Times.Length, "SCALAR", $",\"min\":[{F(clip.Times[0])}],\"max\":[{F(clip.Times[^1])}]");
            var samplers = new StringBuilder();
            var channels = new StringBuilder();
            var s = 0;
            foreach (var (node, rotation, values) in clip.Tracks)
            {
                var data = Floats(values.SelectMany(v => rotation ? new[] { v.X, v.Y, v.Z, v.W } : new[] { v.X, v.Y, v.Z }));
                var output = Accessor(data, 5126, values.Length, rotation ? "VEC4" : "VEC3");
                if (s > 0)
                {
                    samplers.Append(',');
                    channels.Append(',');
                }

                samplers.Append(CultureInfo.InvariantCulture, $"{{\"input\":{time},\"output\":{output},\"interpolation\":\"LINEAR\"}}");
                channels.Append(CultureInfo.InvariantCulture, $"{{\"sampler\":{s},\"target\":{{\"node\":{node},\"path\":\"{(rotation ? "rotation" : "translation")}\"}}}}");
                s++;
            }

            if (anims.Length > 0)
            {
                anims.Append(',');
            }

            anims.Append(CultureInfo.InvariantCulture, $"{{\"name\":\"{clip.Name}\",\"samplers\":[{samplers}],\"channels\":[{channels}]}}");
        }

        while (bin.Length % 4 != 0)
        {
            bin.WriteByte(0);
        }

        var nodes = new StringBuilder();
        for (var i = 0; skinned && i < Joints.Length; i++)
        {
            var kids = Enumerable.Range(0, Joints.Length).Where(k => Joints[k].Parent == i).ToArray();
            var l = Joints[i].Local;
            nodes.Append(CultureInfo.InvariantCulture, $"{{\"name\":\"{Joints[i].Name}\",\"translation\":[{F(l.X)},{F(l.Y)},{F(l.Z)}]{(kids.Length > 0 ? $",\"children\":[{string.Join(',', kids)}]" : string.Empty)}}},");
        }

        nodes.Append(CultureInfo.InvariantCulture, $"{{\"name\":\"{(skinned ? "Body" : "Ground")}\",\"mesh\":0{(skinned ? ",\"skin\":0" : string.Empty)}}}");
        var jointList = string.Join(',', Enumerable.Range(0, Joints.Length));
        var sceneNodes = skinned ? $"0,{Joints.Length}" : "0";
        var skins = skinned ? $"\"skins\":[{{\"joints\":[{jointList}],\"inverseBindMatrices\":{aIbm},\"skeleton\":0}}]," : string.Empty;
        var skinAttributes = skinned ? $",\"JOINTS_0\":{aJoints},\"WEIGHTS_0\":{aWeights}" : string.Empty;
        var animations = skinned ? $"\"animations\":[{anims}]," : string.Empty;
        var json = $"{{\"asset\":{{\"version\":\"2.0\",\"generator\":\"character example art\"}},\"scene\":0,\"scenes\":[{{\"nodes\":[{sceneNodes}]}}],"
            + $"\"nodes\":[{nodes}],{skins}"
            + $"\"meshes\":[{{\"primitives\":[{{\"attributes\":{{\"POSITION\":{aPos},\"NORMAL\":{aNor},\"COLOR_0\":{aCol}{skinAttributes}}},\"indices\":{aIdx},\"material\":0}}]}}],"
            + $"\"materials\":[{{\"name\":\"{(skinned ? "figure" : "ground")}\",\"pbrMetallicRoughness\":{{\"baseColorFactor\":[1,1,1,1],\"metallicFactor\":0,\"roughnessFactor\":{(skinned ? "0.65" : "0.95")}}}}}],"
            + $"{animations}\"accessors\":[{accessors}],\"bufferViews\":[{views}],\"buffers\":[{{\"byteLength\":{bin.Length}}}]}}";
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var pad = (4 - (jsonBytes.Length % 4)) % 4;
        var jsonLen = jsonBytes.Length + pad;
        using var output2 = new MemoryStream();
        using var w = new BinaryWriter(output2);
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
        return output2.ToArray();
    }
}
