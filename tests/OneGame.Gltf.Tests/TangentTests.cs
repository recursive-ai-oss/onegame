using System.Globalization;
using System.Numerics;
using System.Text;

namespace OneGame.Gltf.Tests;

public class TangentTests
{
    [Fact]
    public void GeneratedTangentsAgreeWithShippedOnes()
    {
        // NormalTangentMirrorTest ships tangents, including a mirrored half. Regenerating them must give the same tangent space.
        var path = TestData.Path_("NormalTangentMirrorTest/glTF/NormalTangentMirrorTest.gltf");
        var shipped = GltfLoader.LoadFile(path, new GltfLoadOptions { Tangents = TangentMode.Never }).Meshes[0].Primitives[0];
        var generated = GltfLoader.LoadFile(path, new GltfLoadOptions { Tangents = TangentMode.Always }).Meshes[0].Primitives[0];
        Assert.NotNull(shipped.Tangents);
        Assert.False(shipped.TangentsGenerated);
        Assert.True(generated.TangentsGenerated);

        // Triangle order is preserved, so corner i of both primitives is the same corner of the same triangle.
        Assert.Equal(shipped.Indices.Length, generated.Indices.Length);
        int directionBad = 0, signBad = 0;
        double worstAngle = 0;
        for (var c = 0; c < shipped.Indices.Length; c++)
        {
            var s = shipped.Tangents![shipped.Indices[c]];
            var g = generated.Tangents![generated.Indices[c]];
            Assert.Equal(shipped.Positions[shipped.Indices[c]], generated.Positions[generated.Indices[c]]);
            var dot = Vector3.Dot(Vector3.Normalize(new Vector3(s.X, s.Y, s.Z)), new Vector3(g.X, g.Y, g.Z));
            var angle = Math.Acos(Math.Clamp(dot, -1f, 1f)) * 180 / Math.PI;
            worstAngle = Math.Max(worstAngle, angle);
            if (angle > 2)
            {
                directionBad++;
            }

            if (s.W != g.W)
            {
                signBad++;
            }
        }

        Console.WriteLine($"tangents: {shipped.Indices.Length} corners, worst angle {worstAngle:F2} deg, {directionBad} over 2 deg, {signBad} sign differences");
        Assert.Equal(0, signBad);
        Assert.Equal(0, directionBad);
    }

    [Fact]
    public void GeneratedTangentsAreOrthonormalToTheNormal()
    {
        var p = TestData.Load("NormalTangentTest/glTF/NormalTangentTest.gltf").Meshes[0].Primitives[0];
        Assert.True(p.TangentsGenerated);
        Assert.NotNull(p.Tangents);
        for (var i = 0; i < p.VertexCount; i++)
        {
            var t = p.Tangents![i];
            var tv = new Vector3(t.X, t.Y, t.Z);
            Assert.InRange(tv.Length(), 0.999f, 1.001f);
            Assert.InRange(Vector3.Dot(tv, p.Normals[i]), -0.01f, 0.01f);
            Assert.True(t.W is 1f or -1f);
        }
    }

    [Fact]
    public void NoTangentsWithoutANormalMapUnlessAsked()
    {
        var path = TestData.Path_("Fox/glTF-Binary/Fox.glb");
        Assert.Null(GltfLoader.LoadFile(path).Meshes[0].Primitives[0].Tangents);
        Assert.NotNull(GltfLoader.LoadFile(path, new GltfLoadOptions { Tangents = TangentMode.Always }).Meshes[0].Primitives[0].Tangents);
    }

    private const string QuadJson = """
        {"asset":{"version":"2.0"},"scenes":[{"nodes":[0]}],"nodes":[{"mesh":0}],
         "materials":[{"normalTexture":{"index":0}}],"textures":[{"source":0}],
         "images":[{"uri":"data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg=="}],
         "meshes":[{"primitives":[{"attributes":{"POSITION":0,"NORMAL":1,"TEXCOORD_0":2},"indices":3,"material":0}]}],
         "buffers":[{"byteLength":BYTES,"uri":"data:application/octet-stream;base64,DATA"}],
         "bufferViews":[{"buffer":0,"byteOffset":0,"byteLength":48},{"buffer":0,"byteOffset":48,"byteLength":48},{"buffer":0,"byteOffset":96,"byteLength":32},{"buffer":0,"byteOffset":128,"byteLength":12}],
         "accessors":[{"bufferView":0,"componentType":5126,"count":4,"type":"VEC3"},{"bufferView":1,"componentType":5126,"count":4,"type":"VEC3"},{"bufferView":2,"componentType":5126,"count":4,"type":"VEC2"},{"bufferView":3,"componentType":5123,"count":6,"type":"SCALAR"}]}
        """;

    /// <summary>A unit quad in the XY plane facing +Z. UV (0,0) is the picture's top left, so v grows downwards: uv = (x, 1 - y), or (1 - x, 1 - y) mirrored.</summary>
    private static GltfModel Quad(bool mirror)
    {
        float[] uv = mirror ? [1, 1, 0, 1, 0, 0, 1, 0] : [0, 1, 1, 1, 1, 0, 0, 0];
        var data = new List<byte>();
        void F(params float[] v)
        {
            foreach (var x in v)
            {
                data.AddRange(BitConverter.GetBytes(x));
            }
        }

        F(0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 1, 0);
        F(0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1);
        F(uv);
        foreach (var i in new ushort[] { 0, 1, 2, 0, 2, 3 })
        {
            data.AddRange(BitConverter.GetBytes(i));
        }

        var json = QuadJson.Replace("BYTES", data.Count.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal).Replace("DATA", Convert.ToBase64String([.. data]), StringComparison.Ordinal);
        return GltfLoader.Load(Encoding.UTF8.GetBytes(json));
    }

    [Fact]
    public void QuadTangentFollowsTheUDirectionAndTheSignFollowsTheMirror()
    {
        // u grows with +X, so the tangent is +X. v grows downwards, so the image's "up" is +Y and cross(N, T) = +Y matches it: w = +1.
        var plain = Quad(mirror: false).Meshes[0].Primitives[0];
        Assert.True(plain.TangentsGenerated);
        Assert.All(plain.Tangents!, t =>
        {
            Assert.InRange(t.X, 0.999f, 1.001f);
            Assert.Equal(1f, t.W);
        });

        // Mirroring u turns the tangent to -X; cross(N, T) is then -Y against the image's up (+Y), so w = -1.
        var mirrored = Quad(mirror: true).Meshes[0].Primitives[0];
        Assert.All(mirrored.Tangents!, t =>
        {
            Assert.InRange(t.X, -1.001f, -0.999f);
            Assert.Equal(-1f, t.W);
        });
    }

    [Fact]
    public void MirroredUvsOnOneSharedVertexSplitIt()
    {
        // Two triangles share an edge, one with mirrored UVs: the shared vertices need a tangent space for each orientation.
        float[] pos = [0, 0, 0, 1, 0, 0, 0, 1, 0, 1, 1, 0];
        float[] nor = [0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1];
        float[] uv = [0, 1, 1, 1, 0, 0, -1, 0];
        var data = new List<byte>();
        foreach (var f in pos.Concat(nor).Concat(uv))
        {
            data.AddRange(BitConverter.GetBytes(f));
        }

        // Triangles (0,1,2) and (1,3,2): the second has its UV map mirrored relative to its positions, so vertices 1 and 2 are used by both orientations.
        foreach (var i in new ushort[] { 0, 1, 2, 1, 3, 2 })
        {
            data.AddRange(BitConverter.GetBytes(i));
        }

        var json = QuadJson.Replace("BYTES", data.Count.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("DATA", Convert.ToBase64String([.. data]), StringComparison.Ordinal);
        var model = GltfLoader.Load(Encoding.UTF8.GetBytes(json));
        var p = model.Meshes[0].Primitives[0];
        Assert.Equal(6, p.VertexCount);
        Assert.Equal(p.VertexCount, p.Tangents!.Length);
        Assert.Contains(p.Tangents, t => t.W > 0);
        Assert.Contains(p.Tangents, t => t.W < 0);
    }
}
