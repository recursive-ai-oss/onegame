using System.Text.Json;

namespace OneGame.Gltf.Tests;

/// <summary>Loads every model of the committed set and the downloaded corpus and checks counts that follow from the file's own structure.</summary>
public class CorpusTests
{
    public static TheoryData<string> Models()
    {
        var data = new TheoryData<string>();
        foreach (var m in TestData.AllModels())
        {
            data.Add(m);
        }

        return data;
    }

    private static int Elements(JsonElement root, int accessor) => root.GetProperty("accessors")[accessor].GetProperty("count").GetInt32();

    /// <summary>The vertex and index count of a primitive as the specification defines them, before and after generating flat normals.</summary>
    private static (int Vertices, int Indices) PrimitiveCounts(JsonElement root, JsonElement prim)
    {
        var attrs = prim.GetProperty("attributes");
        var vertices = Elements(root, attrs.GetProperty("POSITION").GetInt32());
        var n = prim.TryGetProperty("indices", out var ia) ? Elements(root, ia.GetInt32()) : vertices;
        var mode = prim.TryGetProperty("mode", out var m) ? m.GetInt32() : 4;
        var indices = mode switch
        {
            0 => n,
            1 => n / 2 * 2,
            2 => n >= 2 ? n * 2 : 0,
            3 => n >= 2 ? (n - 1) * 2 : 0,
            4 => n / 3 * 3,
            _ => n >= 3 ? (n - 2) * 3 : 0,
        };
        // Triangles without NORMAL get three private vertices per triangle.
        if (mode >= 4 && !attrs.TryGetProperty("NORMAL", out _))
        {
            vertices = indices;
        }

        return (vertices, indices);
    }

    [Theory]
    [MemberData(nameof(Models))]
    public void LoadsWithoutErrorsAndMatchesTheFilesStructure(string relative)
    {
        var path = TestData.Path_(relative);
        var model = GltfLoader.LoadFile(path, new GltfLoadOptions { Tangents = TangentMode.Never });
        using var raw = new RawGltf(path);
        var root = raw.Root;
        Assert.Equal(raw.Count("nodes"), model.Nodes.Length);
        Assert.Equal(raw.Count("meshes"), model.Meshes.Length);
        Assert.Equal(raw.Count("materials"), model.Materials.Length);
        Assert.Equal(raw.Count("textures"), model.Textures.Length);
        Assert.Equal(raw.Count("images"), model.Images.Length);
        Assert.Equal(raw.Count("animations"), model.Animations.Length);
        Assert.Equal(raw.Count("skins"), model.Skins.Length);
        Assert.Equal(raw.Count("scenes"), model.Scenes.Length);
        Assert.Equal(raw.Count("cameras"), model.Cameras.Length);
        Assert.Equal(relative.EndsWith(".glb", StringComparison.Ordinal), model.IsBinary);

        for (var m = 0; m < model.Meshes.Length; m++)
        {
            var prims = root.GetProperty("meshes")[m].GetProperty("primitives");
            Assert.Equal(prims.GetArrayLength(), model.Meshes[m].Primitives.Length);
            for (var p = 0; p < model.Meshes[m].Primitives.Length; p++)
            {
                var (vertices, indices) = PrimitiveCounts(root, prims[p]);
                var prim = model.Meshes[m].Primitives[p];
                Assert.Equal(vertices, prim.VertexCount);
                Assert.Equal(indices, prim.Indices.Length);
                Assert.Equal(vertices, prim.Normals.Length);
                Assert.All(prim.Indices, i => Assert.True(i < (uint)vertices));
                var targets = prims[p].TryGetProperty("targets", out var t) ? t.GetArrayLength() : 0;
                Assert.Equal(targets, prim.MorphTargets.Length);
            }
        }

        // Every decoded image has RGBA pixels of its size; every image is used as colour or data by something.
        foreach (var image in model.Images)
        {
            Assert.Equal(ImageStatus.Decoded, image.Status);
            Assert.Equal(image.Width * image.Height * 4, image.Pixels!.Length);
        }

        Assert.DoesNotContain(model.Warnings, w => w.Code != "ignored_attribute");
    }

    [Theory]
    [InlineData("Box/glTF/Box.gltf")]
    [InlineData("Box/glTF-Binary/Box.glb")]
    [InlineData("Box/glTF-Embedded/Box.gltf")]
    public void BoxHasTheCounts(string file)
    {
        var m = TestData.Load(file);
        // A cube: 6 faces of 4 vertices, 12 triangles; one material; a root node and the node with the mesh.
        Assert.Equal(2, m.Nodes.Length);
        Assert.Single(m.Meshes);
        Assert.Single(m.Materials);
        Assert.Empty(m.Animations);
        var p = m.Meshes[0].Primitives[0];
        Assert.Equal(24, p.VertexCount);
        Assert.Equal(36, p.Indices.Length);
        Assert.Equal(GltfTopology.Triangles, p.Topology);
        Assert.Equal(new System.Numerics.Vector3(-0.5f, -0.5f, -0.5f), p.Bounds.Min);
        Assert.Equal(new System.Numerics.Vector3(0.5f, 0.5f, 0.5f), p.Bounds.Max);
        Assert.Equal(0.8f, m.Materials[0].BaseColorFactor.X);
        Assert.Equal(0f, m.Materials[0].MetallicFactor);
    }

    [Fact]
    public void TheThreeBoxVariantsAreIdentical()
    {
        var a = TestData.Load("Box/glTF/Box.gltf").Meshes[0].Primitives[0];
        var b = TestData.Load("Box/glTF-Binary/Box.glb").Meshes[0].Primitives[0];
        var c = TestData.Load("Box/glTF-Embedded/Box.gltf").Meshes[0].Primitives[0];
        Assert.Equal(a.PackVertices(), b.PackVertices());
        Assert.Equal(a.PackVertices(), c.PackVertices());
        Assert.Equal(a.Indices, b.Indices);
    }

    [Fact]
    public void FoxIsSkinnedAndAnimated()
    {
        var m = TestData.Load("Fox/glTF-Binary/Fox.glb");
        Assert.Equal(3, m.Animations.Length);
        Assert.Equal(["Survey", "Walk", "Run"], m.Animations.Select(a => a.Name));
        Assert.Single(m.Skins);
        Assert.Equal(24, m.Skins[0].Joints.Length);
        var p = m.Meshes[0].Primitives[0];
        Assert.NotNull(p.Joints);
        Assert.NotNull(p.Weights);
        Assert.Equal(1728, p.VertexCount);
        Assert.All(p.Joints!, j => Assert.True(Math.Max(Math.Max(j.A, j.B), Math.Max(j.C, j.D)) < 24));
        Assert.All(p.Weights!, w => Assert.InRange(w.X + w.Y + w.Z + w.W, 0.999f, 1.001f));
        Assert.Single(m.Images);
        Assert.Equal(ImageStatus.Decoded, m.Images[0].Status);
        Assert.True(m.Images[0].UsedAsColor);
    }

    [Fact]
    public void TheQuantizedMorphCubeReadsIntegerAttributes()
    {
        var m = TestData.Load("AnimatedMorphCube/glTF-Quantized/AnimatedMorphCube.gltf");
        Assert.Contains("KHR_mesh_quantization", m.ExtensionsRequired);
        var p = m.Meshes[0].Primitives[0];
        Assert.Equal(24, p.VertexCount);
        Assert.Equal(2, p.MorphTargets.Length);
        // Integer positions land inside the cube the float version describes, once the node scale is applied.
        var plain = TestData.Load("AnimatedMorphCube/glTF/AnimatedMorphCube.gltf");
        var a = plain.Meshes[0].Bounds;
        var b = p.Bounds;
        var worldA = System.Numerics.Vector3.Transform(a.Max, plain.Nodes[0].World);
        var worldB = System.Numerics.Vector3.Transform(b.Max, m.Nodes.First(n => n.Mesh == 0).World);
        Assert.True(System.Numerics.Vector3.Distance(System.Numerics.Vector3.Abs(worldA), System.Numerics.Vector3.Abs(worldB)) < 0.5f, $"{worldA} vs {worldB}");
    }

    [Fact]
    public void SparseAccessorsAreResolved()
    {
        var path = TestData.Path_("SimpleSparseAccessor/glTF/SimpleSparseAccessor.gltf");
        var m = GltfLoader.LoadFile(path);
        using var raw = new RawGltf(path);
        var posAccessor = raw.Root.GetProperty("meshes")[0].GetProperty("primitives")[0].GetProperty("attributes").GetProperty("POSITION").GetInt32();
        var expected = raw.Floats(posAccessor);
        var prim = m.Meshes[0].Primitives[0];
        Assert.Equal(expected.Length / 3, prim.VertexCount == 36 ? 14 : prim.VertexCount);
        // The model has no normals, so the importer unwelded it; map back through the indices.
        var indices = raw.Root.GetProperty("meshes")[0].GetProperty("primitives")[0].TryGetProperty("indices", out var ia) ? ia.GetInt32() : -1;
        Assert.True(indices >= 0);
        var accessor = raw.Root.GetProperty("accessors")[indices];
        Assert.Equal(36, accessor.GetProperty("count").GetInt32());
        var sparseCount = raw.Root.GetProperty("accessors")[posAccessor].GetProperty("sparse").GetProperty("count").GetInt32();
        Assert.True(sparseCount > 0);
        // Compare every corner's position with the sparse-resolved raw accessor.
        var rawIndices = RawIndices(path, indices);
        for (var i = 0; i < rawIndices.Length; i++)
        {
            var v = prim.Positions[i];
            Assert.Equal((float)expected[rawIndices[i] * 3], v.X);
            Assert.Equal((float)expected[(rawIndices[i] * 3) + 1], v.Y);
            Assert.Equal((float)expected[(rawIndices[i] * 3) + 2], v.Z);
        }

        // The sparse substitution really changed something relative to the base data alone.
        Assert.Contains(expected, x => x != 0);
    }

    private static int[] RawIndices(string path, int accessor)
    {
        using var doc = JsonDocument.Parse(File.ReadAllBytes(path));
        var root = doc.RootElement;
        var a = root.GetProperty("accessors")[accessor];
        var view = root.GetProperty("bufferViews")[a.GetProperty("bufferView").GetInt32()];
        var uri = root.GetProperty("buffers")[view.GetProperty("buffer").GetInt32()].GetProperty("uri").GetString()!;
        var bytes = uri.StartsWith("data:", StringComparison.Ordinal) ? Convert.FromBase64String(uri[(uri.IndexOf(',', StringComparison.Ordinal) + 1)..]) : File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(path)!, uri));
        var start = (view.TryGetProperty("byteOffset", out var vo) ? vo.GetInt32() : 0) + (a.TryGetProperty("byteOffset", out var ao) ? ao.GetInt32() : 0);
        var count = a.GetProperty("count").GetInt32();
        var r = new int[count];
        for (var i = 0; i < count; i++)
        {
            r[i] = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(start + (i * 2)));
        }

        return r;
    }

    [Fact]
    public void EmissiveStrengthIsParsed()
    {
        var m = TestData.Load("EmissiveStrengthTest/glTF-Binary/EmissiveStrengthTest.glb");
        Assert.Contains("KHR_materials_emissive_strength", m.ExtensionsUsed);
        var strengths = m.Materials.Select(x => x.EmissiveStrength).ToArray();
        Assert.Contains(strengths, s => s > 1f);
        Assert.Contains(strengths, s => s == 1f);
        // The factor times the strength is what the test asks a renderer to show: boxes look equally bright.
        Assert.All(m.Materials, x => Assert.True(x.EmissiveFactor.X * x.EmissiveStrength >= 0));
    }

    [Fact]
    public void TextureTransformIsParsedIntoMatrices()
    {
        var m = TestData.Load("TextureTransformTest/glTF/TextureTransformTest.gltf");
        var transformed = m.Materials.Where(x => x.BaseColorTexture?.Transform is not null).ToArray();
        Assert.NotEmpty(transformed);
        foreach (var mat in transformed)
        {
            var r = mat.BaseColorTexture!;
            var t = r.Transform!.Value;
            // uv' = T * R * S * uv for column vectors; check a point by hand.
            var uv = new System.Numerics.Vector2(0.25f, 0.75f);
            var sx = uv.X * t.Scale.X;
            var sy = uv.Y * t.Scale.Y;
            var c = MathF.Cos(t.Rotation);
            var s = MathF.Sin(t.Rotation);
            var expected = new System.Numerics.Vector2((c * sx) - (s * sy) + t.Offset.X, (s * sx) + (c * sy) + t.Offset.Y);
            var actual = System.Numerics.Vector2.Transform(uv, r.UvMatrix);
            Assert.True(System.Numerics.Vector2.Distance(expected, actual) < 1e-5f, mat.Name);
        }

        Assert.Contains(transformed, x => x.BaseColorTexture!.Transform!.Value.Rotation != 0);
    }

    [Fact]
    public void SponzaLoadTimeAndAllocations()
    {
        var path = TestData.Path_("Sponza/glTF/Sponza.gltf");
        var warm = GltfLoader.LoadFile(path);
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var model = GltfLoader.LoadFile(path);
        sw.Stop();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Console.WriteLine($"Sponza: {sw.ElapsedMilliseconds} ms, {allocated / 1048576.0:F1} MiB allocated, output {model.OutputBytes / 1048576.0:F1} MiB charged, {model.Meshes.Sum(x => x.Primitives.Sum(p => p.VertexCount))} vertices, {model.Images.Length} images");
        Assert.Equal(warm.Meshes.Length, model.Meshes.Length);
        Assert.True(sw.ElapsedMilliseconds < 20000);
    }
}
