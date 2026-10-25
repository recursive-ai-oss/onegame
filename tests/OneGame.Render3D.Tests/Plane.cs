using System.Text;
using OneGame.Gltf;
using OneGame.Gpu;

namespace OneGame.Render3D.Tests;

/// <summary>Small generated scenes for numeric checks: a flat square in the z = 0 plane facing +Z, with a chosen material.</summary>
internal static class Plane
{
    /// <summary>A glTF document (embedded buffer) with a 10 x 10 square and the given material JSON, e.g. <c>{"pbrMetallicRoughness":{...}}</c>.</summary>
    public static byte[] Gltf(string materialJson, string extensionsUsed = "", float half = 5f, string nodeExtra = "")
    {
        var data = new List<byte>();
        void F(float v) => data.AddRange(BitConverter.GetBytes(v));
        void U(ushort v) => data.AddRange(BitConverter.GetBytes(v));
        foreach (var (x, y) in new[] { (-half, -half), (half, -half), (half, half), (-half, half) })
        {
            F(x);
            F(y);
            F(0);
        }

        foreach (var _ in new int[4])
        {
            F(0);
            F(0);
            F(1);
        }

        foreach (var (u, v) in new[] { (0f, 1f), (1f, 1f), (1f, 0f), (0f, 0f) })
        {
            F(u);
            F(v);
        }

        foreach (var i in new ushort[] { 0, 1, 2, 0, 2, 3 })
        {
            U(i);
        }

        var uri = "data:application/octet-stream;base64," + Convert.ToBase64String(data.ToArray());
        var used = extensionsUsed.Length > 0 ? $"\"extensionsUsed\":[{extensionsUsed}]," : "";
        var json = $$"""
        {"asset":{"version":"2.0"},{{used}}"scene":0,"scenes":[{"nodes":[0]}],"nodes":[{"mesh":0{{nodeExtra}}}],
         "meshes":[{"primitives":[{"attributes":{"POSITION":0,"NORMAL":1,"TEXCOORD_0":2},"indices":3,"material":0}]}],
         "materials":[{{materialJson}}],
         "buffers":[{"byteLength":{{data.Count}},"uri":"{{uri}}"}],
         "bufferViews":[{"buffer":0,"byteOffset":0,"byteLength":48},{"buffer":0,"byteOffset":48,"byteLength":48},{"buffer":0,"byteOffset":96,"byteLength":32},{"buffer":0,"byteOffset":128,"byteLength":12}],
         "accessors":[
           {"bufferView":0,"componentType":5126,"count":4,"type":"VEC3","min":[{{-half}},{{-half}},0],"max":[{{half}},{{half}},0]},
           {"bufferView":1,"componentType":5126,"count":4,"type":"VEC3"},
           {"bufferView":2,"componentType":5126,"count":4,"type":"VEC2"},
           {"bufferView":3,"componentType":5123,"count":6,"type":"SCALAR"}]}
        """;
        return Encoding.UTF8.GetBytes(json);
    }

    /// <summary>A rough, non-metallic white-ish surface whose index of refraction is 1, which removes the specular reflection: it is a pure Lambert surface.</summary>
    public static byte[] Lambert(float albedo) => Gltf(
        """{"pbrMetallicRoughness":{"baseColorFactor":[@A@,@A@,@A@,1],"metallicFactor":0,"roughnessFactor":1},"extensions":{"KHR_materials_ior":{"ior":1.0}}}""".Replace("@A@", albedo.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        "\"KHR_materials_ior\"");

    /// <summary>A flat colour surface (KHR_materials_unlit): the shader returns the base colour untouched, so exposure and tone mapping can be checked alone.</summary>
    public static byte[] Unlit(float r, float g, float b) => Gltf(
        """{"pbrMetallicRoughness":{"baseColorFactor":[@R@,@G@,@B@,1]},"extensions":{"KHR_materials_unlit":{}}}""".Replace("@R@", r.ToString(System.Globalization.CultureInfo.InvariantCulture)).Replace("@G@", g.ToString(System.Globalization.CultureInfo.InvariantCulture)).Replace("@B@", b.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        "\"KHR_materials_unlit\"");

    /// <summary>An orthographic camera looking straight down at the plane, covering <paramref name="width"/> x its aspect world units.</summary>
    public static Camera3D TopDown(float height, float distance = 10f) => new()
    {
        World = Matrix4x4.CreateTranslation(0, 0, distance),
        Orthographic = true,
        OrthoHeight = height,
        Near = 0.1f,
        Far = distance + 5f,
    };

    /// <summary>World position of the centre of pixel (x, y) of a target seen by <see cref="TopDown"/>.</summary>
    public static Vector3 PixelToWorld(int x, int y, int w, int h, float orthoHeight)
    {
        var orthoWidth = orthoHeight * w / h;
        return new Vector3((((x + 0.5f) / w) - 0.5f) * orthoWidth, (0.5f - ((y + 0.5f) / h)) * orthoHeight, 0);
    }

    public static float[] ReadLinear(Rig rig)
    {
        var bytes = new byte[rig.Target.Width * rig.Target.Height * 8];
        rig.Device.ReadTexture(rig.Target.Target, 0, 0, bytes);
        var half = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, Half>(bytes.AsSpan());
        var result = new float[half.Length];
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = (float)half[i];
        }

        return result;
    }

    public static float[] ReadAfterRender(Rig rig, Scene3D scene)
    {
        rig.Target.Render(scene);
        return ReadLinear(rig);
    }

    public static (Rig Rig, Scene3D Scene) Setup(byte[] gltf, int size = 64, float orthoHeight = 8f)
    {
        var rig = new Rig(size, size, GpuFormat.Rgba16Float);
        var model = rig.Own(rig.Renderer.Upload(GltfLoader.Load(gltf)));
        var scene = new Scene3D { Camera = TopDown(orthoHeight), Samples = 1 };
        scene.Add(model);
        scene.Environment.Intensity = 0f;
        scene.Environment.Background = BackgroundMode.Color;
        scene.Environment.BackgroundColor = Vector3.Zero;
        scene.Post.Bloom = false;
        scene.Post.ToneMapper = ToneMapper.None;
        return (rig, scene);
    }
}
