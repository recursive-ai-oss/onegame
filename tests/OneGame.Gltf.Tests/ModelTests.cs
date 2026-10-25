using System.Globalization;
using System.Numerics;
using System.Text;

namespace OneGame.Gltf.Tests;

/// <summary>Details of the model that the sample files do not exercise: extensions, topology, attribute encodings, packing.</summary>
public class ModelTests
{
    private static GltfModel Load(string json, GltfLoadOptions? options = null) => GltfLoader.Load(Encoding.UTF8.GetBytes(json), options);

    private static string F(float v) => v.ToString("R", CultureInfo.InvariantCulture);

    /// <summary>A document with the given extra top-level JSON members and one triangle at node 0.</summary>
    private static string WithExtras(string members)
    {
        var json = HostileTests.Triangle();
        return json.Replace("\"asset\":{\"version\":\"2.0\"},", "\"asset\":{\"version\":\"2.0\"}," + members + ",", StringComparison.Ordinal);
    }

    [Fact]
    public void LightsCamerasAndMaterialExtensionsAreParsed()
    {
        var json = WithExtras("""
            "extensionsUsed":["KHR_lights_punctual","KHR_materials_clearcoat","KHR_materials_transmission","KHR_materials_ior","KHR_materials_specular","KHR_materials_sheen","KHR_materials_volume","KHR_materials_unlit","KHR_materials_emissive_strength","KHR_texture_transform"],
            "extensions":{"KHR_lights_punctual":{"lights":[
              {"type":"directional","color":[1,0.5,0.25],"intensity":3,"name":"sun"},
              {"type":"point","range":10,"intensity":100},
              {"type":"spot","spot":{"innerConeAngle":0.1,"outerConeAngle":0.5}}]}},
            "cameras":[{"type":"perspective","name":"main","perspective":{"yfov":0.8,"znear":0.1}},
                       {"type":"orthographic","orthographic":{"xmag":2,"ymag":1,"znear":0.5,"zfar":50}}],
            "images":[{"uri":"data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg=="}],
            "textures":[{"source":0}],
            "materials":[
             {"name":"coat","pbrMetallicRoughness":{"baseColorFactor":[0.1,0.2,0.3,0.4],"metallicFactor":0.25,"roughnessFactor":0.75,"baseColorTexture":{"index":0,"texCoord":1,"extensions":{"KHR_texture_transform":{"offset":[0.5,0.25],"rotation":1.5707964,"scale":[2,3],"texCoord":0}}}},
              "alphaMode":"MASK","alphaCutoff":0.3,"doubleSided":true,"emissiveFactor":[1,0,0],
              "extensions":{"KHR_materials_clearcoat":{"clearcoatFactor":0.9,"clearcoatRoughnessFactor":0.1,"clearcoatNormalTexture":{"index":0,"scale":0.5}},
                            "KHR_materials_transmission":{"transmissionFactor":0.6},
                            "KHR_materials_ior":{"ior":1.33},
                            "KHR_materials_specular":{"specularFactor":0.5,"specularColorFactor":[0.9,0.8,0.7],"specularColorTexture":{"index":0}},
                            "KHR_materials_sheen":{"sheenColorFactor":[0.1,0.2,0.3],"sheenRoughnessFactor":0.4},
                            "KHR_materials_volume":{"thicknessFactor":2,"attenuationDistance":5,"attenuationColor":[0,1,0]},
                            "KHR_materials_emissive_strength":{"emissiveStrength":4}}},
             {"name":"flat","extensions":{"KHR_materials_unlit":{}}}]
            """);
        json = json.Replace("\"nodes\":[{\"mesh\":0}]", "\"nodes\":[{\"mesh\":0,\"extensions\":{\"KHR_lights_punctual\":{\"light\":1}}},{\"camera\":1}]", StringComparison.Ordinal);
        var m = Load(json);

        Assert.Equal(3, m.Lights.Length);
        Assert.Equal(GltfLightType.Directional, m.Lights[0].Type);
        Assert.Equal(new Vector3(1, 0.5f, 0.25f), m.Lights[0].Color);
        Assert.Equal(3f, m.Lights[0].Intensity);
        Assert.Equal("sun", m.Lights[0].Name);
        Assert.Equal(10f, m.Lights[1].Range);
        Assert.Null(m.Lights[0].Range);
        Assert.Equal(0.5f, m.Lights[2].OuterConeAngle);
        Assert.Equal(0.1f, m.Lights[2].InnerConeAngle);
        Assert.Equal(1, m.Nodes[0].Light);
        Assert.Equal(-1, m.Nodes[1].Light);

        Assert.Equal(2, m.Cameras.Length);
        Assert.True(m.Cameras[0].Perspective);
        Assert.Equal(0.8f, m.Cameras[0].YFov);
        Assert.Null(m.Cameras[0].ZFar);
        Assert.Null(m.Cameras[0].AspectRatio);
        Assert.False(m.Cameras[1].Perspective);
        Assert.Equal(50f, m.Cameras[1].ZFar);
        Assert.Equal(1, m.Nodes[1].Camera);

        var c = m.Materials[0];
        Assert.Equal(new Vector4(0.1f, 0.2f, 0.3f, 0.4f), c.BaseColorFactor);
        Assert.Equal(0.25f, c.MetallicFactor);
        Assert.Equal(0.75f, c.RoughnessFactor);
        Assert.Equal(GltfAlphaMode.Mask, c.AlphaMode);
        Assert.Equal(0.3f, c.AlphaCutoff);
        Assert.True(c.DoubleSided);
        Assert.Equal(4f, c.EmissiveStrength);
        Assert.Equal(1.33f, c.Ior);
        Assert.Equal(0.9f, c.Clearcoat!.Factor);
        Assert.Equal(0.5f, c.Clearcoat.NormalScale);
        Assert.NotNull(c.Clearcoat.NormalTexture);
        Assert.Equal(0.6f, c.Transmission!.Factor);
        Assert.Equal(0.5f, c.Specular!.Factor);
        Assert.Equal(new Vector3(0.9f, 0.8f, 0.7f), c.Specular.ColorFactor);
        Assert.Equal(0.4f, c.Sheen!.RoughnessFactor);
        Assert.Equal(2f, c.Volume!.ThicknessFactor);
        Assert.Equal(5f, c.Volume.AttenuationDistance);
        Assert.Equal(new Vector3(0, 1, 0), c.Volume.AttenuationColor);
        Assert.False(c.Unlit);
        Assert.True(m.Materials[1].Unlit);
        Assert.Null(m.Materials[1].Clearcoat);
        Assert.Equal(1.5f, m.Materials[1].Ior);

        // The extension's texCoord replaces the one on the texture info; the matrix is T * R * S for column vectors, with the rotation that turns the image clockwise on screen (v points down).
        var t = c.BaseColorTexture!;
        Assert.Equal(0, t.TexCoord);
        Assert.Equal(new Vector2(0.5f, 0.25f), t.Transform!.Value.Offset);
        var uv = Vector2.Transform(new Vector2(1, 0), t.UvMatrix);
        // (1,0) scaled to (2,0), rotated a quarter turn (v down) to (0,-2), moved by the offset: (0.5, -1.75).
        Assert.Equal(0.5f, uv.X, 4);
        Assert.Equal(-1.75f, uv.Y, 4);

        // Colour and data usage of the one image: base colour and specular colour are sRGB, the coat normal is data.
        Assert.True(m.Images[0].UsedAsColor);
        Assert.True(m.Images[0].UsedAsData);
    }

    [Fact]
    public void UnknownExtensionsAreIgnoredWithAWarningWhenNotRequired()
    {
        var m = Load(WithExtras("\"extensionsUsed\":[\"VENDOR_made_up\",\"KHR_materials_iridescence\"]"));
        Assert.Equal(2, m.Warnings.Count(w => w.Code == "ignored_extension"));
    }

    [Fact]
    public void TopologiesAreExpandedToLists()
    {
        // Eight vertices on a line of the XY plane; indices 0..7 are used as written.
        string Doc(int mode, string indices, int count)
        {
            var bytes = new List<byte>();
            for (var i = 0; i < 8; i++)
            {
                foreach (var f in new float[] { i, i % 2, 0 })
                {
                    bytes.AddRange(BitConverter.GetBytes(f));
                }
            }

            foreach (var i in indices.Split(',').Select(x => ushort.Parse(x, CultureInfo.InvariantCulture)))
            {
                bytes.AddRange(BitConverter.GetBytes(i));
            }

            return $$$"""
                {"asset":{"version":"2.0"},"meshes":[{"primitives":[{"attributes":{"POSITION":0,"NORMAL":0},"indices":1,"mode":{{{mode}}}}]}],
                 "buffers":[{"byteLength":{{{bytes.Count}}},"uri":"data:application/octet-stream;base64,{{{Convert.ToBase64String([.. bytes])}}}"}],
                 "bufferViews":[{"buffer":0,"byteLength":96},{"buffer":0,"byteOffset":96,"byteLength":{{{count * 2}}} }],
                 "accessors":[{"bufferView":0,"componentType":5126,"count":8,"type":"VEC3"},{"bufferView":1,"componentType":5123,"count":{{{count}}},"type":"SCALAR"}]}
                """;
        }

        uint[] Run(int mode, string indices, int count, out GltfTopology topology)
        {
            var p = Load(Doc(mode, indices, count)).Meshes[0].Primitives[0];
            topology = p.Topology;
            return p.Indices;
        }

        Assert.Equal(new uint[] { 0, 1, 2 }, Run(0, "0,1,2", 3, out var t));
        Assert.Equal(GltfTopology.Points, t);
        Assert.Equal(new uint[] { 0, 1, 2, 3 }, Run(1, "0,1,2,3", 4, out t));
        Assert.Equal(GltfTopology.Lines, t);
        Assert.Equal(new uint[] { 0, 1, 1, 2, 2, 0 }, Run(2, "0,1,2", 3, out t)); // line loop closes
        Assert.Equal(new uint[] { 0, 1, 1, 2, 2, 3 }, Run(3, "0,1,2,3", 4, out t)); // line strip
        Assert.Equal(new uint[] { 0, 1, 2, 3, 4, 5 }, Run(4, "0,1,2,3,4,5", 6, out t));
        Assert.Equal(GltfTopology.Triangles, t);
        // Triangle strip: every second triangle swaps two indices to keep the winding.
        Assert.Equal(new uint[] { 0, 1, 2, 2, 1, 3, 2, 3, 4 }, Run(5, "0,1,2,3,4", 5, out t));
        // Triangle fan: all triangles share vertex 0.
        Assert.Equal(new uint[] { 0, 1, 2, 0, 2, 3, 0, 3, 4 }, Run(6, "0,1,2,3,4", 5, out t));
        // Incomplete trailing shapes are dropped with a warning.
        var m = Load(Doc(4, "0,1,2,3", 4));
        Assert.Equal(3, m.Meshes[0].Primitives[0].Indices.Length);
        Assert.Contains(m.Warnings, w => w.Code == "incomplete_shape");
        // Strips and fans too short to make a triangle give an empty primitive, not an error.
        Assert.Empty(Run(5, "0,1", 2, out t));
    }

    [Fact]
    public void NormalizedAndInterleavedAttributesAreDecoded()
    {
        // One buffer view with a 16 byte stride: float position (12 bytes) then a normalized UNSIGNED_BYTE colour (4 bytes).
        var bytes = new List<byte>();
        for (var i = 0; i < 3; i++)
        {
            foreach (var f in new float[] { i, 0, 0 })
            {
                bytes.AddRange(BitConverter.GetBytes(f));
            }

            bytes.AddRange([(byte)(i * 127), 255, 0, 64]);
        }

        var json = $$$"""
            {"asset":{"version":"2.0"},"meshes":[{"primitives":[{"attributes":{"POSITION":0,"COLOR_0":1,"NORMAL":0}}]}],
             "buffers":[{"byteLength":48,"uri":"data:application/octet-stream;base64,{{{Convert.ToBase64String([.. bytes])}}}"}],
             "bufferViews":[{"buffer":0,"byteLength":48,"byteStride":16}],
             "accessors":[{"bufferView":0,"componentType":5126,"count":3,"type":"VEC3"},{"bufferView":0,"byteOffset":12,"componentType":5121,"normalized":true,"count":3,"type":"VEC4"}]}
            """;
        var p = Load(json).Meshes[0].Primitives[0];
        Assert.Equal(new Vector3(2, 0, 0), p.Positions[2]);
        Assert.NotNull(p.Colors);
        Assert.Equal(new Vector4(0, 1, 0, 64 / 255f), p.Colors![0]);
        Assert.Equal(254 / 255f, p.Colors[2].X, 5);
    }

    [Fact]
    public void NormalizedShortsAndSignedBytesClampAtMinusOne()
    {
        // Position as normalized SHORT triples (KHR_mesh_quantization): -32768 and -32767 both map to -1.
        var bytes = new List<byte>();
        foreach (var s in new short[] { -32768, -32767, 0, 32767, 16384, 0, 0, 0, 0, 0, 0, 0 })
        {
            bytes.AddRange(BitConverter.GetBytes(s));
        }

        var json = $$$"""
            {"asset":{"version":"2.0"},"extensionsUsed":["KHR_mesh_quantization"],"extensionsRequired":["KHR_mesh_quantization"],
             "meshes":[{"primitives":[{"attributes":{"POSITION":0,"NORMAL":0},"mode":0}]}],
             "buffers":[{"byteLength":24,"uri":"data:application/octet-stream;base64,{{{Convert.ToBase64String([.. bytes])}}}"}],
             "bufferViews":[{"buffer":0,"byteLength":24,"byteStride":8}],
             "accessors":[{"bufferView":0,"componentType":5122,"normalized":true,"count":3,"type":"VEC3"}]}
            """;
        // A VEC3 of shorts is 6 bytes, padded to a stride of 8 here.
        var m = Load(json);
        var p = m.Meshes[0].Primitives[0];
        Assert.Equal(-1f, p.Positions[0].X);
        Assert.Equal(-1f, p.Positions[0].Y);
        Assert.Equal(0f, p.Positions[0].Z);
        Assert.Equal(-32768 / 32768f, p.Positions[0].X, 3);
        Assert.Empty(m.Warnings);
    }

    [Fact]
    public void IntegerNumbersWrittenWithADecimalPointAreAccepted()
    {
        var json = HostileTests.Triangle().Replace("\"scene\":0", "\"scene\":0.0", StringComparison.Ordinal).Replace("\"mesh\":0", "\"mesh\":0.0", StringComparison.Ordinal);
        Assert.Single(Load(json).Meshes);
        Assert.Throws<GltfException>(() => Load(HostileTests.Triangle().Replace("\"mesh\":0", "\"mesh\":0.5", StringComparison.Ordinal)));
    }

    [Fact]
    public void AByteOrderMarkIsTolerated()
    {
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes(HostileTests.Triangle())).ToArray();
        Assert.Single(GltfLoader.Load(bytes).Meshes);
    }

    [Fact]
    public void NodeTransformsComposeTranslationRotationScaleInOrder()
    {
        // glTF: world = T * R * S for column vectors, so a point is scaled first, then rotated, then moved, and children follow their parent.
        var json = HostileTests.Triangle().Replace(
            "\"nodes\":[{\"mesh\":0}]",
            "\"nodes\":[{\"translation\":[1,2,3],\"rotation\":[0,0,0.7071068,0.7071068],\"scale\":[2,2,2],\"children\":[1]},{\"mesh\":0,\"translation\":[1,0,0]}]",
            StringComparison.Ordinal).Replace("\"nodes\":[0]", "\"nodes\":[0]", StringComparison.Ordinal);
        var m = Load(json);
        // Child origin (0,0,0) -> its translation (1,0,0) -> scaled (2,0,0) -> rotated a quarter turn about Z to (0,2,0) -> moved to (1,4,3).
        var p = Vector3.Transform(Vector3.Zero, m.Nodes[1].World);
        Assert.Equal(1f, p.X, 4);
        Assert.Equal(4f, p.Y, 4);
        Assert.Equal(3f, p.Z, 4);
        Assert.Equal(0, m.Nodes[1].Depth - 1);
        Assert.Equal(0, m.Nodes[1].Parent);
        Assert.Equal(-1, m.Nodes[0].Parent);

        // The same transform given as a matrix (column-major) is the same node.
        var matrixJson = HostileTests.Triangle().Replace(
            "\"nodes\":[{\"mesh\":0}]",
            "\"nodes\":[{\"matrix\":[0,2,0,0,-2,0,0,0,0,0,2,0,1,2,3,1],\"children\":[1]},{\"mesh\":0,\"translation\":[1,0,0]}]",
            StringComparison.Ordinal);
        var viaMatrix = Load(matrixJson);
        Assert.True(viaMatrix.Nodes[0].FromMatrix);
        var q = Vector3.Transform(Vector3.Zero, viaMatrix.Nodes[1].World);
        Assert.Equal(1f, q.X, 4);
        Assert.Equal(4f, q.Y, 4);
        Assert.Equal(3f, q.Z, 4);
        Assert.Equal(2f, viaMatrix.Nodes[0].Scale.X, 4);
    }

    [Fact]
    public void PackedVerticesAndIndicesFollowTheDocumentedLayout()
    {
        var p = TestData.Load("Fox/glTF-Binary/Fox.glb").Meshes[0].Primitives[0];
        var v = p.PackVertices();
        Assert.Equal(p.VertexCount * GltfPrimitive.PackedVertexSize, v.Length);
        var k = 5;
        var at = k * GltfPrimitive.PackedVertexSize;
        float Read(int o) => BitConverter.ToSingle(v, at + o);
        Assert.Equal(p.Positions[k], new Vector3(Read(0), Read(4), Read(8)));
        Assert.Equal(p.Normals[k], new Vector3(Read(12), Read(16), Read(20)));
        Assert.Equal(new Vector4(0, 0, 0, 1), new Vector4(Read(24), Read(28), Read(32), Read(36)));
        Assert.Equal(p.Uv0![k], new Vector2(Read(40), Read(44)));
        Assert.Equal(Vector2.Zero, new Vector2(Read(48), Read(52)));
        Assert.Equal(Vector4.One, new Vector4(Read(56), Read(60), Read(64), Read(68)));
        Assert.Equal(p.Joints![k].B, BitConverter.ToUInt16(v, at + 74));
        Assert.Equal(p.Weights![k], new Vector4(Read(80), Read(84), Read(88), Read(92)));

        var i16 = p.PackIndices(out var width);
        Assert.Equal(IndexWidth.U16, width);
        Assert.Equal(p.Indices.Length * 2, i16.Length);
        Assert.Equal((ushort)p.Indices[7], BitConverter.ToUInt16(i16, 14));
    }

    [Fact]
    public void ImagesInBufferViewsAndSamplersAreRead()
    {
        // The binary Fox keeps its PNG in a buffer view.
        var m = TestData.Load("Fox/glTF-Binary/Fox.glb");
        Assert.Equal("png", m.Images[0].Format);
        Assert.Equal(ImageStatus.Decoded, m.Images[0].Status);
        Assert.Equal(m.Images[0].Width * m.Images[0].Height * 4, m.Images[0].Pixels!.Length);
        Assert.Contains(m.Images[0].Pixels!, b => b != 0);

        var s = Load(HostileTests.Triangle().Replace("\"nodes\":[{\"mesh\":0}]", "\"nodes\":[{\"mesh\":0}],\"samplers\":[{\"magFilter\":9728,\"minFilter\":9987,\"wrapS\":33071,\"wrapT\":33648},{}]", StringComparison.Ordinal)).Samplers;
        Assert.Equal(GltfMagFilter.Nearest, s[0].MagFilter);
        Assert.Equal(GltfMinFilter.LinearMipmapLinear, s[0].MinFilter);
        Assert.Equal(GltfWrap.ClampToEdge, s[0].WrapS);
        Assert.Equal(GltfWrap.MirroredRepeat, s[0].WrapT);
        Assert.Equal(GltfWrap.Repeat, s[1].WrapS);
        Assert.Equal(GltfMagFilter.Unspecified, s[1].MagFilter);
    }

    [Fact]
    public void ImageDecodingCanBeSkipped()
    {
        var m = TestData.Load("Fox/glTF-Binary/Fox.glb", new GltfLoadOptions { DecodeImages = false });
        Assert.Equal(ImageStatus.HeaderOnly, m.Images[0].Status);
        Assert.Null(m.Images[0].Pixels);
        Assert.True(m.Images[0].Width > 0);
    }

    [Theory]
    [InlineData("a/b.png", "a/b.png")]
    [InlineData("my%20tex.png", "my tex.png")]
    [InlineData("caf%C3%A9.png", "caf\u00e9.png")]
    public void RelativeUrisAreDecodedOnce(string uri, string expected) => Assert.Equal(expected, GltfUri.CheckRelative(uri));

    [Theory]
    [InlineData("")]
    [InlineData("..")]
    [InlineData("a/..")]
    [InlineData("a/../b")]
    [InlineData("%2e%2e")]
    [InlineData("/abs")]
    [InlineData("c:x")]
    [InlineData("http://a/b")]
    [InlineData("a\\b")]
    public void UnsafeUrisAreRefused(string uri) => Assert.Equal("uri_not_allowed", Assert.Throws<GltfException>(() => GltfUri.CheckRelative(uri)).Code);
}
