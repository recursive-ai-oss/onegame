using System;
using System.Globalization;
using System.Numerics;
using System.Text;
using OneGame.Core;
using OneGame.Gltf;
using OneGame.Host;
using OneGame.Render3D;
using OneGame.UI;

namespace WaterExample;

/// <summary>
/// A pond: a water sheet between two banks, a white tower and a red buoy that drifts. Everything is built in code as glTF text, so the
/// example needs no art files. The sheet's material is named "Water"; the stage opts that material in to planar reflection by name.
/// </summary>
public static class PondModels
{
    /// <summary>The still scenery: the water sheet at y = 0, two banks and a tower.</summary>
    public static GltfModel Scenery { get; } = Build(
        Part.Ground(14f, "Water", "BLEND", 0.02f, 0.12f, 0.2f, 0.75f, 0.03f),
        Part.Box(new Vector3(-9f, 0.25f, -2f), new Vector3(8f, 1f, 24f), "Bank", "OPAQUE", 0.25f, 0.4f, 0.15f, 1f, 1f),
        Part.Box(new Vector3(9f, 0.25f, -2f), new Vector3(8f, 1f, 24f), "Bank", "OPAQUE", 0.25f, 0.4f, 0.15f, 1f, 1f),
        Part.Box(new Vector3(-1.5f, 1.8f, -6f), new Vector3(1.2f, 4f, 1.2f), "Tower", "OPAQUE", 0.9f, 0.9f, 0.85f, 1f, 1f));

    /// <summary>The buoy: a red cube the view moves about.</summary>
    public static GltfModel Buoy { get; } = Build(
        Part.Box(Vector3.Zero, new Vector3(1.6f, 1.6f, 1.6f), "Buoy", "OPAQUE", 0.85f, 0.15f, 0.1f, 1f, 1f));

    private sealed record Part(Vector3[] Positions, Vector3[] Normals, ushort[] Indices, Vector3 Offset, string Name, string Alpha, float[] Color, float Roughness)
    {
        public static Part Ground(float half, string name, string alpha, float r, float g, float b, float a, float roughness) => new(
            [new(-half, 0, half), new(half, 0, half), new(half, 0, -half), new(-half, 0, -half)],
            [Vector3.UnitY, Vector3.UnitY, Vector3.UnitY, Vector3.UnitY],
            [0, 1, 2, 0, 2, 3], Vector3.Zero, name, alpha, [r, g, b, a], roughness);

        public static Part Box(Vector3 center, Vector3 size, string name, string alpha, float r, float g, float b, float a, float roughness)
        {
            var h = size / 2;
            var pos = new Vector3[24];
            var nrm = new Vector3[24];
            var idx = new ushort[36];
            Vector3[] axes = [Vector3.UnitX, -Vector3.UnitX, Vector3.UnitY, -Vector3.UnitY, Vector3.UnitZ, -Vector3.UnitZ];
            for (var f = 0; f < 6; f++)
            {
                var n = axes[f];
                var u = Vector3.Normalize(Vector3.Cross(Math.Abs(n.Y) > 0.5f ? Vector3.UnitX : Vector3.UnitY, n));
                var v = Vector3.Cross(n, u);
                var c = n * Vector3.Dot(Vector3.Abs(n), h);
                var uu = u * Vector3.Dot(Vector3.Abs(u), h);
                var vv = v * Vector3.Dot(Vector3.Abs(v), h);
                Vector3[] corners = [c - uu - vv, c + uu - vv, c + uu + vv, c - uu + vv];
                for (var k = 0; k < 4; k++)
                {
                    pos[(f * 4) + k] = corners[k];
                    nrm[(f * 4) + k] = n;
                }

                ushort o = (ushort)(f * 4);
                ushort[] tri = [o, (ushort)(o + 1), (ushort)(o + 2), o, (ushort)(o + 2), (ushort)(o + 3)];

                // The corner order above can wind either way depending on the face; flip a triangle pair that faces inwards.
                var facing = Vector3.Dot(Vector3.Cross(corners[1] - corners[0], corners[2] - corners[0]), n);
                if (facing < 0)
                {
                    tri = [o, (ushort)(o + 2), (ushort)(o + 1), o, (ushort)(o + 3), (ushort)(o + 2)];
                }

                tri.CopyTo(idx, f * 6);
            }

            return new Part(pos, nrm, idx, center, name, alpha, [r, g, b, a], roughness);
        }
    }

    private static GltfModel Build(params Part[] parts)
    {
        var data = new System.Collections.Generic.List<byte>();
        var views = new StringBuilder();
        var accessors = new StringBuilder();
        var meshes = new StringBuilder();
        var nodes = new StringBuilder();
        var materials = new StringBuilder();
        string N(float x) => x.ToString("R", CultureInfo.InvariantCulture);
        void Add(Vector3 v)
        {
            data.AddRange(BitConverter.GetBytes(v.X));
            data.AddRange(BitConverter.GetBytes(v.Y));
            data.AddRange(BitConverter.GetBytes(v.Z));
        }

        for (var i = 0; i < parts.Length; i++)
        {
            var p = parts[i];
            var posAt = data.Count;
            foreach (var v in p.Positions) { Add(v); }
            var nrmAt = data.Count;
            foreach (var v in p.Normals) { Add(v); }
            var idxAt = data.Count;
            foreach (var v in p.Indices) { data.AddRange(BitConverter.GetBytes(v)); }
            while (data.Count % 4 != 0) { data.Add(0); }

            var min = p.Positions[0];
            var max = p.Positions[0];
            foreach (var v in p.Positions) { min = Vector3.Min(min, v); max = Vector3.Max(max, v); }
            var n = p.Positions.Length;
            var a = i * 3;
            views.Append(CultureInfo.InvariantCulture, $"{{\"buffer\":0,\"byteOffset\":{posAt},\"byteLength\":{n * 12}}},{{\"buffer\":0,\"byteOffset\":{nrmAt},\"byteLength\":{n * 12}}},{{\"buffer\":0,\"byteOffset\":{idxAt},\"byteLength\":{p.Indices.Length * 2}}},");
            accessors.Append(CultureInfo.InvariantCulture, $"{{\"bufferView\":{a},\"componentType\":5126,\"count\":{n},\"type\":\"VEC3\",\"min\":[{N(min.X)},{N(min.Y)},{N(min.Z)}],\"max\":[{N(max.X)},{N(max.Y)},{N(max.Z)}]}},");
            accessors.Append(CultureInfo.InvariantCulture, $"{{\"bufferView\":{a + 1},\"componentType\":5126,\"count\":{n},\"type\":\"VEC3\"}},");
            accessors.Append(CultureInfo.InvariantCulture, $"{{\"bufferView\":{a + 2},\"componentType\":5123,\"count\":{p.Indices.Length},\"type\":\"SCALAR\"}},");
            meshes.Append(CultureInfo.InvariantCulture, $"{{\"primitives\":[{{\"attributes\":{{\"POSITION\":{a},\"NORMAL\":{a + 1}}},\"indices\":{a + 2},\"material\":{i}}}]}},");
            nodes.Append(CultureInfo.InvariantCulture, $"{{\"mesh\":{i},\"translation\":[{N(p.Offset.X)},{N(p.Offset.Y)},{N(p.Offset.Z)}]}},");
            materials.Append(CultureInfo.InvariantCulture, $"{{\"name\":\"{p.Name}\",\"alphaMode\":\"{p.Alpha}\",\"pbrMetallicRoughness\":{{\"baseColorFactor\":[{N(p.Color[0])},{N(p.Color[1])},{N(p.Color[2])},{N(p.Color[3])}],\"metallicFactor\":0,\"roughnessFactor\":{N(p.Roughness)}}}}},");
        }

        static string Trim(StringBuilder b) => b.ToString().TrimEnd(',');
        var roots = string.Join(',', System.Linq.Enumerable.Range(0, parts.Length));
        var uri = "data:application/octet-stream;base64," + Convert.ToBase64String(data.ToArray());
        var json = string.Create(CultureInfo.InvariantCulture,
            $"{{\"asset\":{{\"version\":\"2.0\"}},\"scene\":0,\"scenes\":[{{\"nodes\":[{roots}]}}],\"nodes\":[{Trim(nodes)}],\"meshes\":[{Trim(meshes)}],\"materials\":[{Trim(materials)}],\"buffers\":[{{\"byteLength\":{data.Count},\"uri\":\"{uri}\"}}],\"bufferViews\":[{Trim(views)}],\"accessors\":[{Trim(accessors)}]}}");
        return GltfLoader.Load(Encoding.UTF8.GetBytes(json));
    }
}

/// <summary>The scene's one piece of state: a tick counter the buoy's drift is read from. The view turns it into a position, so animation never writes state.</summary>
[Entity]
public partial class Pond : Entity, IView3D
{
    /// <summary>Ticks since the game began.</summary>
    [Synced] public partial int Ticks { get; private set; }

    protected override void OnTick() => Ticks++;

    [View]
    public void Describe3D(SceneWriter scene)
    {
        scene.Model("pond", Transform3.Identity, pickable: false);

        // The buoy bobs and drifts side to side; floats are fine in view code.
        var t = (Ticks + scene.Alpha.ToSingle()) / GameInstance.TickRate;
        var x = 3.2f * MathF.Sin(t * 0.7f);
        var y = 1.4f + (0.15f * MathF.Sin(t * 2.3f));
        scene.Model("buoy", Transform3.At(new FVec3(Fixed.FromDouble(x), Fixed.FromDouble(y), Fixed.FromInt(-3))), pickable: false);

        scene.DirectionalLight(Transform3.LookAt(FVec3.Zero, new FVec3(-2, -3, -2)), new Vector3(1f, 0.96f, 0.9f), 3f);
        scene.Camera(Transform3.LookAt(new FVec3(0, Fixed.FromRatio(8, 5), 9), new FVec3(0, Fixed.FromRatio(6, 5), -6)), 38f);
    }
}

public sealed class WaterGame : GameDefinition
{
    /// <summary>Builds the scene from the world. One game definition drives one game.</summary>
    public SceneView3D View { get; } = CreateView();

    public override string Name => "Water";

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.WaterExample.EntityTypes.Register(registry);

    public override void Setup(World world) => world.Add(new Pond());

    public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => Ui.Column(Ui.Label("water")).Padding(Length.Px(8)).TextColor(Color.White));

    public override IRenderStage CreateRenderStage(HostResources resources)
    {
        ArgumentNullException.ThrowIfNull(resources);
        return resources.Fonts is not null && resources.Shaders is not null ? new PondStage(resources, View) : new StubStage();
    }

    private static SceneView3D CreateView()
    {
        var view = new SceneView3D();
        view.Models.Add("pond", PondModels.Scenery);
        view.Models.Add("buoy", PondModels.Buoy);
        return view;
    }

    private sealed class PondStage(HostResources resources, SceneView3D view) : Render3DStage(resources, view)
    {
        protected override void UpdateScene(Scene3D scene, in FrameInfo frame)
        {
            scene.Samples = 1;
            scene.Environment.Intensity = 0.8f;
            scene.Environment.BackgroundBlur = 1f;

            // The mirror plane is the water's height. The clock drives UvScroll (set here, it moves nothing until the material has a texture).
            scene.Reflection.Height = 0f;
            scene.Time = frame.Tick / (double)GameInstance.TickRate;

            // Opting in is by material name; setting an unchanged value costs nothing, so doing it every frame is fine.
            foreach (var instance in scene.Models)
            {
                if (instance.Model.FindMaterial("Water") is { } water)
                {
                    water.PlanarReflection = true;
                    water.UvScroll = new Vector2(0.02f, 0.01f);
                }
            }
        }
    }
}
