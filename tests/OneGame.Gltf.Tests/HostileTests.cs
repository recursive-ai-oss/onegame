using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace OneGame.Gltf.Tests;

/// <summary>Malformed and hostile files are refused with a <see cref="GltfException"/> and a stable code, and never crash or allocate by their own claims.</summary>
public class HostileTests
{
    /// <summary>One triangle: positions at byte 0 (36 bytes) and three 16-bit indices at byte 36 (6 bytes).</summary>
    internal static string Triangle(ushort i0 = 0, ushort i1 = 1, ushort i2 = 2)
    {
        var bytes = new List<byte>();
        foreach (var f in new float[] { 0, 0, 0, 1, 0, 0, 0, 1, 0 })
        {
            bytes.AddRange(BitConverter.GetBytes(f));
        }

        foreach (var i in new[] { i0, i1, i2 })
        {
            bytes.AddRange(BitConverter.GetBytes(i));
        }

        return """
            {"asset":{"version":"2.0"},"scene":0,"scenes":[{"nodes":[0]}],"nodes":[{"mesh":0}],
             "meshes":[{"primitives":[{"attributes":{"POSITION":0},"indices":1}]}],
             "buffers":[{"byteLength":42,"uri":"data:application/octet-stream;base64,DATA"}],
             "bufferViews":[{"buffer":0,"byteOffset":0,"byteLength":36},{"buffer":0,"byteOffset":36,"byteLength":6}],
             "accessors":[{"bufferView":0,"componentType":5126,"count":3,"type":"VEC3"},{"bufferView":1,"componentType":5123,"count":3,"type":"SCALAR"}]}
            """.Replace("DATA", Convert.ToBase64String([.. bytes]), StringComparison.Ordinal);
    }

    private static GltfModel Load(string json, GltfLoadOptions? options = null, IGltfResources? resources = null)
        => GltfLoader.Load(Encoding.UTF8.GetBytes(json), options, resources);

    private static GltfException Refused(string json, GltfLoadOptions? options = null, IGltfResources? resources = null)
        => Assert.Throws<GltfException>(() => Load(json, options, resources));

    private static string Edit(string from, string to)
    {
        var json = Triangle();
        Assert.Contains(from, json, StringComparison.Ordinal);
        return json.Replace(from, to, StringComparison.Ordinal);
    }

    [Fact]
    public void TheBaseTriangleLoads()
    {
        var m = Load(Triangle());
        var p = m.Meshes[0].Primitives[0];
        Assert.Equal(3, p.VertexCount);
        Assert.Equal(new uint[] { 0, 1, 2 }, p.Indices);
        // No NORMAL: flat normals were generated, facing +Z for this counter-clockwise triangle.
        Assert.True(p.NormalsGenerated);
        Assert.All(p.Normals, n => Assert.Equal(System.Numerics.Vector3.UnitZ, n));
        Assert.Empty(m.Warnings);
    }

    [Theory]
    [InlineData("\"count\":3,\"type\":\"VEC3\"", "\"count\":30000000,\"type\":\"VEC3\"", "limit_exceeded")] // beyond the element limit
    [InlineData("\"count\":3,\"type\":\"VEC3\"", "\"count\":1000,\"type\":\"VEC3\"", "buffer_range")] // lying count, within the limit
    [InlineData("\"count\":3,\"type\":\"VEC3\"", "\"count\":2147483647,\"type\":\"VEC3\"", "limit_exceeded")]
    [InlineData("\"count\":3,\"type\":\"VEC3\"", "\"count\":0,\"type\":\"VEC3\"", "schema")]
    [InlineData("\"count\":3,\"type\":\"VEC3\"", "\"count\":-3,\"type\":\"VEC3\"", "schema")]
    [InlineData("\"count\":3,\"type\":\"SCALAR\"", "\"count\":4000,\"type\":\"SCALAR\"", "buffer_range")]
    [InlineData("\"byteLength\":36}", "\"byteLength\":100}", "buffer_range")] // view longer than the buffer
    [InlineData("\"byteOffset\":36,\"byteLength\":6", "\"byteOffset\":40,\"byteLength\":6", "buffer_range")] // view ends past the buffer
    [InlineData("\"byteOffset\":36,\"byteLength\":6", "\"byteOffset\":4503599627370496,\"byteLength\":6", "schema")] // 2^52
    [InlineData("\"byteLength\":36}", "\"byteLength\":9223372036854775807}", "schema")]
    [InlineData("\"byteOffset\":36,\"byteLength\":6", "\"byteOffset\":9223372036854775807,\"byteLength\":6", "schema")]
    [InlineData("\"byteLength\":36}", "\"byteLength\":36,\"byteStride\":2}", "schema")]
    [InlineData("\"byteLength\":36}", "\"byteLength\":36,\"byteStride\":1000}", "schema")]
    [InlineData("\"byteLength\":36}", "\"byteLength\":36,\"byteStride\":4}", "accessor_invalid")] // stride smaller than a VEC3
    [InlineData("\"componentType\":5126", "\"componentType\":1234", "accessor_invalid")]
    [InlineData("\"componentType\":5123,\"count\":3,\"type\":\"SCALAR\"", "\"componentType\":5125,\"count\":3,\"type\":\"VEC2\"", "accessor_invalid")]
    [InlineData("\"type\":\"VEC3\"", "\"type\":\"VEC9\"", "accessor_invalid")]
    [InlineData("{\"bufferView\":1,\"componentType\":5123", "{\"bufferView\":7,\"componentType\":5123", "reference")]
    [InlineData("\"attributes\":{\"POSITION\":0}", "\"attributes\":{\"POSITION\":9}", "reference")]
    [InlineData("\"attributes\":{\"POSITION\":0}", "\"attributes\":{}", "schema")]
    [InlineData("\"attributes\":{\"POSITION\":0}", "\"attributes\":{\"POSITION\":1}", "accessor_invalid")] // a scalar index accessor as positions
    [InlineData("\"byteLength\":42,\"uri\"", "\"byteLength\":100,\"uri\"", "buffer_truncated")]
    [InlineData("\"byteLength\":42,\"uri\"", "\"byteLength\":1000000000000,\"uri\"", "limit_exceeded")] // declared huge buffer
    [InlineData("\"byteLength\":42,\"uri\"", "\"byteLength\":0,\"uri\"", "schema")]
    [InlineData("\"nodes\":[{\"mesh\":0}]", "\"nodes\":[{\"mesh\":5}]", "reference")]
    [InlineData("\"nodes\":[{\"mesh\":0}]", "\"nodes\":[{\"mesh\":0,\"children\":[0]}]", "node_graph")] // a node that is its own child
    [InlineData("\"nodes\":[{\"mesh\":0}]", "\"nodes\":[{\"mesh\":0,\"children\":[1]},{\"children\":[0]}]", "node_graph")] // a cycle
    [InlineData("\"nodes\":[{\"mesh\":0}]", "\"nodes\":[{\"mesh\":0,\"children\":[1,2]},{\"children\":[2]},{}]", "node_graph")] // two parents
    [InlineData("\"nodes\":[{\"mesh\":0}]", "\"nodes\":[{\"mesh\":0,\"matrix\":[1,2,3]}]", "schema")]
    [InlineData("\"nodes\":[{\"mesh\":0}]", "\"nodes\":[{\"mesh\":0,\"rotation\":\"x\"}]", "schema")]
    [InlineData("\"nodes\":[{\"mesh\":0}]", "\"nodes\":[{\"mesh\":0,\"skin\":3}]", "reference")]
    [InlineData("\"scenes\":[{\"nodes\":[0]}]", "\"scenes\":[{\"nodes\":[4]}]", "reference")]
    [InlineData("\"scenes\":[{\"nodes\":[0]}]", "\"scenes\":[{\"nodes\":[0.5]}]", "reference")]
    [InlineData("\"asset\":{\"version\":\"2.0\"}", "\"asset\":{\"version\":\"1.0\"}", "unsupported_version")]
    [InlineData("\"asset\":{\"version\":\"2.0\"}", "\"asset\":{}", "unsupported_version")]
    [InlineData("\"asset\":{\"version\":\"2.0\"}", "\"asset\":5", "schema")]
    [InlineData("\"asset\":{\"version\":\"2.0\"}", "\"asset\":{\"version\":\"2.0\"},\"extensionsRequired\":[\"KHR_draco_mesh_compression\"]", "unsupported_extension")]
    [InlineData("\"asset\":{\"version\":\"2.0\"}", "\"asset\":{\"version\":\"2.0\"},\"extensionsRequired\":[7]", "schema")]
    [InlineData("\"meshes\":[{", "\"meshes\":[{\"weights\":[1],", "schema")]
    [InlineData("\"indices\":1}", "\"indices\":1,\"mode\":9}", "schema")]
    [InlineData("\"indices\":1}", "\"indices\":1,\"targets\":[{\"POSITION\":1}]}", "accessor_invalid")]
    [InlineData("\"indices\":1}", "\"indices\":1,\"material\":0}", "reference")]
    public void HostileFilesAreRefused(string from, string to, string code)
    {
        Assert.Equal(code, Refused(Edit(from, to)).Code);
    }

    [Fact]
    public void StringsThatAreNotUtf8AreRefusedNotThrown()
    {
        // The JSON reader hands out the bytes untouched until a string is requested.
        byte[] Doc(byte[] generator) => [.. Encoding.UTF8.GetBytes("{\"asset\":{\"version\":\"2.0\",\"generator\":\""), .. generator, .. Encoding.UTF8.GetBytes("\"}}")];
        Assert.Equal("json_invalid", Assert.Throws<GltfException>(() => GltfLoader.Load(Doc([0xFF, 0xFE]))).Code);
        Assert.Single(Load(Triangle()).Meshes);
        var badName = HostileTests.Triangle().Replace("\"POSITION\":0}", "\"POSITION\":0,\"X\":1}", StringComparison.Ordinal);
        var bytes = Encoding.UTF8.GetBytes(badName);
        var at = Array.IndexOf(bytes, (byte)'X');
        bytes[at] = 0xC3;
        bytes[at + 1] = 0x28;
        Assert.Throws<GltfException>(() => GltfLoader.Load(bytes));
    }

    [Fact]
    public void ColorAttributesMustBeVec3OrVec4()
    {
        var json = Edit("\"attributes\":{\"POSITION\":0}", "\"attributes\":{\"POSITION\":0,\"COLOR_0\":1}");
        Assert.Equal("accessor_invalid", Refused(json).Code);
    }

    [Fact]
    public void IndicesBeyondTheVertexCountAreRefused()
    {
        Assert.Equal("index_out_of_range", Refused(Triangle(0, 1, 3)).Code);
        Assert.Equal("index_out_of_range", Refused(Triangle(0, 1, 65535)).Code);
    }

    [Fact]
    public void OverlappingBufferViewsAreAllowedAsTheSpecificationAllowsSharing()
    {
        // Both views cover the same bytes, so the second accessor reads the position data as 16-bit values: still inside the buffer.
        var json = Edit("\"byteOffset\":36,\"byteLength\":6", "\"byteOffset\":0,\"byteLength\":36");
        var m = Load(json.Replace("\"count\":3,\"type\":\"SCALAR\"", "\"count\":3,\"type\":\"SCALAR\"", StringComparison.Ordinal));
        Assert.Single(m.Meshes);
    }

    [Fact]
    public void ADeclaredHugeBufferIsRefusedBeforeAnythingIsAllocated()
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        var e = Refused(Edit("\"byteLength\":42,\"uri\"", "\"byteLength\":1000000000000,\"uri\""));
        Assert.Equal("limit_exceeded", e.Code);
        Assert.True(GC.GetAllocatedBytesForCurrentThread() - before < 2_000_000);
    }

    [Fact]
    public void ALyingAccessorCountDoesNotAllocateItsClaim()
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal("buffer_range", Refused(Edit("\"count\":3,\"type\":\"VEC3\"", "\"count\":10000000,\"type\":\"VEC3\"")).Code);
        Assert.True(GC.GetAllocatedBytesForCurrentThread() - before < 2_000_000);
    }

    [Fact]
    public void ASparseAccessorCannotClaimMoreThanItsLimits()
    {
        // No bufferView at all, only a sparse part: the count is bounded by the element limit, not by any data.
        const string sparse = """
            {"asset":{"version":"2.0"},"meshes":[{"primitives":[{"attributes":{"POSITION":0},"mode":0}]}],
             "buffers":[{"byteLength":16,"uri":"data:application/octet-stream;base64,AAAAAAAAAAAAAAAAAAAAAA=="}],
             "bufferViews":[{"buffer":0,"byteLength":4},{"buffer":0,"byteOffset":4,"byteLength":12}],
             "accessors":[{"componentType":5126,"count":COUNT,"type":"VEC3","sparse":{"count":SC,"indices":{"bufferView":0,"componentType":5121},"values":{"bufferView":1}}}]}
            """;
        string Make(string count, string sc) => sparse.Replace("COUNT", count, StringComparison.Ordinal).Replace("SC", sc, StringComparison.Ordinal);
        Assert.Equal("limit_exceeded", Refused(Make("2000000000", "1")).Code);
        Assert.Equal("schema", Refused(Make("3", "5")).Code); // more substitutions than elements
        Assert.Equal("buffer_range", Refused(Make("3", "3")).Code); // 3 indices fit, 3 VEC3 values (36 bytes) do not
        // One substitution at index 0 (value 0): valid, and everything else reads as zero.
        var m = Load(Make("100", "1"));
        Assert.Equal(100, m.Meshes[0].Primitives[0].VertexCount);
    }

    [Fact]
    public void SparseIndicesMustIncreaseAndStayInRange()
    {
        // Two substitutions: indices {5, 3} (decreasing) and {1, 9} (past the end of a 4-element accessor).
        string Doc(string indexBytes) => """
            {"asset":{"version":"2.0"},"meshes":[{"primitives":[{"attributes":{"POSITION":0}}]}],
             "buffers":[{"byteLength":32,"uri":"data:application/octet-stream;base64,DATA"}],
             "bufferViews":[{"buffer":0,"byteLength":2},{"buffer":0,"byteOffset":8,"byteLength":24}],
             "accessors":[{"componentType":5126,"count":4,"type":"VEC3","sparse":{"count":2,"indices":{"bufferView":0,"componentType":5121},"values":{"bufferView":1}}}]}
            """.Replace("DATA", Convert.ToBase64String([.. indexBytes.Split(',').Select(b => byte.Parse(b, CultureInfo.InvariantCulture)), .. new byte[30]]), StringComparison.Ordinal);
        Assert.Equal("sparse_invalid", Refused(Doc("5,3")).Code);
        Assert.Equal("sparse_invalid", Refused(Doc("1,9")).Code);
        Assert.Equal("sparse_invalid", Refused(Doc("2,2")).Code);
        Assert.Single(Load(Doc("1,3")).Meshes);
    }

    [Fact]
    public void JsonThatIsNotJsonOrTooDeepIsRefused()
    {
        Assert.Equal("json_invalid", Refused("not json").Code);
        Assert.Equal("json_invalid", Refused("").Code);
        Assert.Equal("json_invalid", Refused("{\"a\":1,}").Code);
        Assert.Equal("json_invalid", Refused("{\"a\":1} trailing").Code);
        Assert.Equal("json_invalid", Refused(new string('[', 200_000)).Code);
        Assert.Equal("json_invalid", Refused(string.Concat(Enumerable.Repeat("{\"a\":", 50_000))).Code);
        Assert.Equal("schema", Refused("[]").Code);
        Assert.Equal("schema", Refused("5").Code);
    }

    [Fact]
    public void ALongNodeChainIsBoundedByTheDepthLimitAndNeverRecursesOnInput()
    {
        string Chain(int n)
        {
            var sb = new StringBuilder("{\"asset\":{\"version\":\"2.0\"},\"scenes\":[{\"nodes\":[0]}],\"nodes\":[");
            for (var i = 0; i < n; i++)
            {
                sb.Append(CultureInfo.InvariantCulture, $"{(i == 0 ? "" : ",")}{{\"children\":[{(i + 1 < n ? i + 1 : 0)}]}}");
            }

            // The last node points at node 0 only when it is meant to close a cycle; here it must not.
            return sb.ToString().Replace("{\"children\":[0]}]", "{}]", StringComparison.Ordinal).Replace("{\"children\":[0]}", "{}", StringComparison.Ordinal) + "]}";
        }

        Assert.Equal("limit_exceeded", Refused(Chain(1000)).Code);
        var deep = new GltfLoadOptions { Limits = new GltfLimits { MaxNodeDepth = 1_000_000, MaxNodes = 1_000_000 } };
        var m = Load(Chain(50_000), deep);
        Assert.Equal(50_000, m.Nodes.Length);
        Assert.Equal(49_999, m.Nodes[^1].Depth);
        // World matrices of a 50 000 deep chain are computed without recursion too.
        var world = new System.Numerics.Matrix4x4[m.Nodes.Length];
        m.CreateRestPose().ComputeWorld(m, world);
    }

    [Fact]
    public void LimitsCanBeTightened()
    {
        var tight = new GltfLoadOptions { Limits = new GltfLimits { MaxNodes = 0 } };
        Assert.Equal("limit_exceeded", Refused(Triangle(), tight).Code);
        Assert.Equal("limit_exceeded", Refused(Triangle(), new GltfLoadOptions { Limits = new GltfLimits { MaxBufferBytes = 10 } }).Code);
        Assert.Equal("limit_exceeded", Refused(Triangle(), new GltfLoadOptions { Limits = new GltfLimits { MaxOutputBytes = 20 } }).Code);
        Assert.Equal("limit_exceeded", Refused(Triangle(), new GltfLoadOptions { Limits = new GltfLimits { MaxVerticesPerPrimitive = 2 } }).Code);
        Assert.Equal("limit_exceeded", Refused(Triangle(), new GltfLoadOptions { Limits = new GltfLimits { MaxJsonBytes = 20 } }).Code);
        Assert.Equal("file_too_large", Assert.Throws<GltfException>(() => GltfLoader.Load(new byte[100], new GltfLoadOptions { Limits = new GltfLimits { MaxFileBytes = 10 } })).Code);
    }

    [Fact]
    public void ExternalFilesNeedAResolverAndDataUrisNeedValidBase64()
    {
        var external = Edit("data:application/octet-stream;base64," + Triangle().Split("base64,")[1].Split('"')[0], "buffer.bin");
        Assert.Equal("external_not_allowed", Refused(external).Code);
        Assert.Equal("uri_not_allowed", Refused(Edit("base64,", "base64,!!!!")).Code);
        Assert.Equal("uri_not_allowed", Refused(Edit("data:application/octet-stream;base64,", "data:text/plain,")).Code);
    }

    private sealed class CountingResources : IGltfResources
    {
        internal List<string> Reads { get; } = [];

        public byte[]? Read(string relativeUri, long maxBytes)
        {
            Reads.Add(relativeUri);
            return null;
        }
    }

    [Theory]
    [InlineData("../secret.bin")]
    [InlineData("..%2fsecret.bin")]
    [InlineData("%2e%2e/secret.bin")]
    [InlineData("%2E%2E%2Fsecret.bin")]
    [InlineData("a/../../secret.bin")]
    [InlineData("a/%2e%2e/%2e%2e/secret.bin")]
    [InlineData("/etc/passwd")]
    [InlineData("%2fetc%2fpasswd")]
    [InlineData("\\windows\\system32\\x.bin")]
    [InlineData("..\\secret.bin")]
    [InlineData("a\\..\\..\\secret.bin")]
    [InlineData("C:\\secret.bin")]
    [InlineData("C:/secret.bin")]
    [InlineData("file:///etc/passwd")]
    [InlineData("http://example.com/x.bin")]
    [InlineData("https://example.com/x.bin")]
    [InlineData("ftp://example.com/x.bin")]
    [InlineData("//example.com/x.bin")]
    [InlineData("x.bin?query")]
    [InlineData("x.bin#frag")]
    [InlineData("a%00b.bin")]
    public void UriEscapesNeverReachTheResolver(string uri)
    {
        var resources = new CountingResources();
        var e = Refused(Edit("data:application/octet-stream;base64," + Triangle().Split("base64,")[1].Split('"')[0], uri.Replace("\\", "\\\\", StringComparison.Ordinal)), null, resources);
        Assert.Equal("uri_not_allowed", e.Code);
        Assert.Empty(resources.Reads);
    }

    [Theory]
    [InlineData("buffer.bin")]
    [InlineData("sub/dir/buffer.bin")]
    [InlineData("my%20file.bin")]
    [InlineData("%252e%252e/x.bin")] // a literal "%2e%2e" folder name after one decoding: harmless
    [InlineData("..x/y.bin")]
    [InlineData("a..b.bin")]
    [InlineData("%")] // not an escape: a file with that name
    [InlineData("%zz")]
    public void PlainRelativeUrisReachTheResolver(string uri)
    {
        var resources = new CountingResources();
        var e = Refused(Edit("data:application/octet-stream;base64," + Triangle().Split("base64,")[1].Split('"')[0], uri.Replace("\\", "\\\\", StringComparison.Ordinal)), null, resources);
        Assert.Equal("resource_missing", e.Code);
        Assert.Single(resources.Reads);
        Assert.DoesNotContain("..", resources.Reads[0].Split('/'));
    }

    [Fact]
    public void TheFolderResolverStaysInsideItsFolder()
    {
        var dir = Path.Combine(Path.GetTempPath(), "gltf-res-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dir, "model"));
        File.WriteAllBytes(Path.Combine(dir, "secret.bin"), [1, 2, 3]);
        File.WriteAllBytes(Path.Combine(dir, "model", "ok.bin"), [4, 5]);
        try
        {
            var res = new GltfFolderResources(Path.Combine(dir, "model"));
            Assert.Equal(new byte[] { 4, 5 }, res.Read("ok.bin", 100));
            Assert.Null(res.Read("missing.bin", 100));
            Assert.Equal("uri_not_allowed", Assert.Throws<GltfException>(() => res.Read("../secret.bin", 100)).Code);
            Assert.Equal("limit_exceeded", Assert.Throws<GltfException>(() => res.Read("ok.bin", 1)).Code);
            // A sibling folder that merely starts with the same name is outside.
            Directory.CreateDirectory(Path.Combine(dir, "model2"));
            File.WriteAllBytes(Path.Combine(dir, "model2", "x.bin"), [9]);
            Assert.Equal("uri_not_allowed", Assert.Throws<GltfException>(() => res.Read("../model2/x.bin", 100)).Code);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void ADataUriCannotClaimMoreThanTheBufferLimit()
    {
        var payload = Convert.ToBase64String(new byte[100_000]);
        var json = Triangle().Replace(Triangle().Split("base64,")[1].Split('"')[0], payload, StringComparison.Ordinal);
        var options = new GltfLoadOptions { Limits = new GltfLimits { MaxBufferBytes = 50_000 } };
        Assert.Equal("limit_exceeded", Refused(json, options).Code);
    }

    [Fact]
    public void AnimationsWithBadTimesOrSizesAreRefused()
    {
        // Positions double as keyframe data here: input = 3 floats of times, output = 3 floats.
        string Doc(string times, string interpolation = "LINEAR", int outCount = 3)
        {
            var bytes = new List<byte>();
            foreach (var t in times.Split(',').Select(x => float.Parse(x, CultureInfo.InvariantCulture)))
            {
                bytes.AddRange(BitConverter.GetBytes(t));
            }

            for (var i = 0; i < 9; i++)
            {
                bytes.AddRange(BitConverter.GetBytes(i * 0.5f));
            }

            return """
                {"asset":{"version":"2.0"},"nodes":[{}],
                 "buffers":[{"byteLength":48,"uri":"data:application/octet-stream;base64,DATA"}],
                 "bufferViews":[{"buffer":0,"byteLength":12},{"buffer":0,"byteOffset":12,"byteLength":36}],
                 "accessors":[{"bufferView":0,"componentType":5126,"count":3,"type":"SCALAR"},{"bufferView":1,"componentType":5126,"count":OUT,"type":"VEC3"}],
                 "animations":[{"samplers":[{"input":0,"output":1,"interpolation":"INTERP"}],"channels":[{"sampler":0,"target":{"node":0,"path":"translation"}}]}]}
                """.Replace("DATA", Convert.ToBase64String([.. bytes]), StringComparison.Ordinal).Replace("OUT", outCount.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal).Replace("INTERP", interpolation, StringComparison.Ordinal);
        }

        Assert.Single(Load(Doc("0,1,2")).Animations);
        Assert.Equal("animation_invalid", Refused(Doc("0,2,1")).Code);
        Assert.Equal("animation_invalid", Refused(Doc("-1,0,1")).Code);
        Assert.Equal("animation_invalid", Refused(Doc("0,NaN,1".Replace("NaN", "1e39", StringComparison.Ordinal))).Code);
        Assert.Equal("animation_invalid", Refused(Doc("0,1,2", "CUBICSPLINE")).Code); // needs 3 values per key
        Assert.Equal("schema", Refused(Doc("0,1,2", "WOBBLE")).Code);
        Assert.Equal("animation_invalid", Refused(Doc("0,1,2", "LINEAR", 2)).Code);
    }

    [Fact]
    public void EveryTruncationOfABinaryFileIsRefusedCleanly()
    {
        var bytes = File.ReadAllBytes(TestData.Path_("Box/glTF-Binary/Box.glb"));
        for (var length = 0; length < bytes.Length; length++)
        {
            var e = Assert.Throws<GltfException>(() => GltfLoader.Load(bytes.AsMemory(0, length)));
            Assert.NotEmpty(e.Code);
        }

        Assert.Single(GltfLoader.Load(bytes).Meshes);
    }

    [Fact]
    public void GlbChunkHeadersThatLieAreRefused()
    {
        var good = File.ReadAllBytes(TestData.Path_("Box/glTF-Binary/Box.glb"));
        byte[] With(Action<byte[]> patch)
        {
            var copy = (byte[])good.Clone();
            patch(copy);
            return copy;
        }

        // Header length beyond the data; a JSON chunk longer than the file; a chunk length that is not a multiple of 4; a huge BIN chunk.
        Assert.Equal("glb_invalid", Assert.Throws<GltfException>(() => GltfLoader.Load(With(b => BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(8), (uint)b.Length + 4)))).Code);
        Assert.Equal("glb_invalid", Assert.Throws<GltfException>(() => GltfLoader.Load(With(b => BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(12), uint.MaxValue - 3)))).Code);
        Assert.Equal("glb_invalid", Assert.Throws<GltfException>(() => GltfLoader.Load(With(b => BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(12), BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(12)) - 1)))).Code);
        Assert.Equal("glb_invalid", Assert.Throws<GltfException>(() => GltfLoader.Load(With(b => BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(16), 0x12345678)))).Code); // first chunk is not JSON
        Assert.Equal("unsupported_version", Assert.Throws<GltfException>(() => GltfLoader.Load(With(b => BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(4), 1)))).Code);
        var binStart = 20 + (int)BinaryPrimitives.ReadUInt32LittleEndian(good.AsSpan(12));
        Assert.Equal("glb_invalid", Assert.Throws<GltfException>(() => GltfLoader.Load(With(b => BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(binStart), 0x7FFFFFF0)))).Code);
        // A BIN chunk shortened leaves bytes that do not form a chunk.
        var shortBin = With(b => BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(binStart), 8));
        Assert.Equal("glb_invalid", Assert.Throws<GltfException>(() => GltfLoader.Load(shortBin)).Code);
    }

    [Fact]
    public void TooManyThingsIsALimitNotACrash()
    {
        var many = new StringBuilder("{\"asset\":{\"version\":\"2.0\"},\"nodes\":[");
        for (var i = 0; i < 1000; i++)
        {
            many.Append(i == 0 ? "{}" : ",{}");
        }

        many.Append("]}");
        var e = Refused(many.ToString(), new GltfLoadOptions { Limits = new GltfLimits { MaxNodes = 999 } });
        Assert.Equal("limit_exceeded", e.Code);
        Assert.Equal(1000, Load(many.ToString()).Nodes.Length);
    }

    [Fact]
    public void ASkinWhoseVerticesNameMissingJointsIsRefused()
    {
        var bytes = new List<byte>();
        foreach (var f in new float[] { 0, 0, 0, 1, 0, 0, 0, 1, 0 })
        {
            bytes.AddRange(BitConverter.GetBytes(f));
        }

        bytes.AddRange([0, 0, 0, 5, 0, 0, 0, 5, 0, 0, 0, 5]); // JOINTS_0: joint 5 does not exist in a one-joint skin
        foreach (var f in new float[] { 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0 })
        {
            bytes.AddRange(BitConverter.GetBytes(f));
        }

        var json = """
            {"asset":{"version":"2.0"},"nodes":[{"mesh":0,"skin":0},{}],
             "skins":[{"joints":[1]}],
             "meshes":[{"primitives":[{"attributes":{"POSITION":0,"JOINTS_0":1,"WEIGHTS_0":2}}]}],
             "buffers":[{"byteLength":96,"uri":"data:application/octet-stream;base64,DATA"}],
             "bufferViews":[{"buffer":0,"byteLength":36},{"buffer":0,"byteOffset":36,"byteLength":12},{"buffer":0,"byteOffset":48,"byteLength":48}],
             "accessors":[{"bufferView":0,"componentType":5126,"count":3,"type":"VEC3"},{"bufferView":1,"componentType":5121,"count":3,"type":"VEC4"},{"bufferView":2,"componentType":5126,"count":3,"type":"VEC4"}]}
            """.Replace("DATA", Convert.ToBase64String([.. bytes]), StringComparison.Ordinal);
        Assert.Equal("skin_invalid", Refused(json).Code);
        Assert.Single(Load(json.Replace("\"joints\":[1]", "\"joints\":[1,1,1,1,1,1]", StringComparison.Ordinal)).Skins);
    }

    private static uint Crc(ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in data)
        {
            crc ^= b;
            for (var k = 0; k < 8; k++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
            }
        }

        return ~crc;
    }

    /// <summary>A PNG that is only a signature and a header claiming a size, with no pixel data.</summary>
    private static byte[] PngClaiming(int width, int height)
    {
        var png = new List<byte> { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var ihdr = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr, width);
        BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(4), height);
        ihdr[8] = 8;
        ihdr[9] = 6;
        var typed = new byte[17];
        "IHDR"u8.CopyTo(typed);
        ihdr.CopyTo(typed.AsSpan(4));
        var length = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, 13);
        var crc = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crc, Crc(typed));
        png.AddRange(length);
        png.AddRange(typed);
        png.AddRange(crc);
        return [.. png];
    }

    private static byte[] Chunk(string type, byte[] data)
    {
        var typed = new byte[4 + data.Length];
        Encoding.ASCII.GetBytes(type).CopyTo(typed, 0);
        data.CopyTo(typed, 4);
        var chunk = new byte[12 + data.Length];
        BinaryPrimitives.WriteInt32BigEndian(chunk, data.Length);
        typed.CopyTo(chunk, 4);
        BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(8 + data.Length), Crc(typed));
        return chunk;
    }

    [Fact]
    public void DecodedPixelsAreStraightAlphaRgbaInReadingOrder()
    {
        // A 2x1 RGBA PNG: (10,20,30,255) then (200,100,50,128).
        var ihdr = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr, 2);
        BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(4), 1);
        ihdr[8] = 8;
        ihdr[9] = 6;
        var raw = new MemoryStream();
        using (var z = new System.IO.Compression.ZLibStream(raw, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
        {
            z.Write(new byte[] { 0, 10, 20, 30, 255, 200, 100, 50, 128 });
        }

        var png = new List<byte> { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        png.AddRange(Chunk("IHDR", ihdr));
        png.AddRange(Chunk("IDAT", raw.ToArray()));
        png.AddRange(Chunk("IEND", []));
        var m = Load($$"""{"asset":{"version":"2.0"},"images":[{"uri":"data:image/png;base64,{{Convert.ToBase64String([.. png])}}"}]}""");
        var image = m.Images[0];
        Assert.Equal(ImageStatus.Decoded, image.Status);
        Assert.Equal(2, image.Width);
        Assert.Equal(1, image.Height);
        Assert.Equal(new byte[] { 10, 20, 30, 255, 200, 100, 50, 128 }, image.Pixels);
    }

    [Fact]
    public void AnImageThatClaimsAHugeSizeIsRefusedFromItsHeader()
    {
        var uri = "data:image/png;base64," + Convert.ToBase64String(PngClaiming(60000, 60000));
        var json = $$"""{"asset":{"version":"2.0"},"images":[{"uri":"{{uri}}"}]}""";
        var before = GC.GetAllocatedBytesForCurrentThread();
        var e = Refused(json);
        Assert.True(e.Code is "limit_exceeded" or "image_invalid", e.Code);
        Assert.True(GC.GetAllocatedBytesForCurrentThread() - before < 20_000_000);
    }

    [Fact]
    public void AnImageThatIsNotAnImageIsRefusedOrReportedUnsupported()
    {
        var truncatedPng = PngClaiming(4, 4);
        var json = $$"""{"asset":{"version":"2.0"},"images":[{"uri":"data:image/png;base64,{{Convert.ToBase64String(truncatedPng)}}"}]}""";
        Assert.Equal("image_invalid", Refused(json).Code);

        // WebP is reported, not decoded: the model still loads.
        var webp = Encoding.ASCII.GetBytes("RIFF\0\0\0\0WEBPVP8 ");
        var ok = Load($$"""{"asset":{"version":"2.0"},"images":[{"uri":"data:image/webp;base64,{{Convert.ToBase64String(webp)}}"}]}""");
        Assert.Equal(ImageStatus.Unsupported, ok.Images[0].Status);
        Assert.Equal("webp", ok.Images[0].Format);
        Assert.Null(ok.Images[0].Pixels);
        Assert.Contains(ok.Warnings, w => w.Code == "unsupported_image");

        var garbage = Load($$"""{"asset":{"version":"2.0"},"images":[{"uri":"data:application/octet-stream;base64,{{Convert.ToBase64String(new byte[64])}}"}]}""");
        Assert.Equal(ImageStatus.Unsupported, garbage.Images[0].Status);
    }

    [Fact]
    public void ADecodedImageLimitIsEnforced()
    {
        var uri = "data:image/png;base64," + Convert.ToBase64String(PngClaiming(5000, 5000));
        var json = $$"""{"asset":{"version":"2.0"},"images":[{"uri":"{{uri}}"}]}""";
        var options = new GltfLoadOptions { Limits = new GltfLimits { MaxImageDimension = 4096 } };
        Assert.True(Refused(json, options).Code is "limit_exceeded" or "image_invalid");
    }
}
