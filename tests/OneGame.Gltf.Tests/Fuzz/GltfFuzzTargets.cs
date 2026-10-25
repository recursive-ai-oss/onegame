using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace OneGame.Gltf.Tests.Fuzzing;

/// <summary>One fuzz target: a function of a byte array that returns normally (true when the input was accepted) for any input.</summary>
public sealed class GltfFuzzTarget(string name, Func<byte[], bool> run, Func<IEnumerable<byte[]>> seeds)
{
    /// <summary>The target's name; also the folder of its committed inputs.</summary>
    public string Name { get; } = name;

    /// <summary>Runs the target. A <see cref="GltfException"/> is the expected way to refuse input and returns false; anything else escapes as a finding.</summary>
    public bool Run(byte[] input) => run(input);

    /// <summary>Valid inputs to mutate.</summary>
    public IEnumerable<byte[]> Seeds() => seeds();
}

/// <summary>
/// The glTF importer's untrusted-input paths as functions of bytes: GLB chunking, JSON, accessors, whole-file import, images and uris.
/// Limits are tight so that no input can ask for much memory; the importer must refuse, never crash.
/// </summary>
public static partial class GltfFuzzTargets
{
    private static readonly GltfLimits Tight = new()
    {
        MaxFileBytes = 4 << 20, MaxJsonBytes = 1 << 20, MaxBufferBytes = 1 << 20, MaxTotalBufferBytes = 2 << 20,
        MaxElementsPerAccessor = 50_000, MaxVerticesPerPrimitive = 50_000, MaxIndicesPerPrimitive = 150_000, MaxKeyframes = 50_000,
        MaxNodes = 10_000, MaxMeshes = 1000, MaxPrimitives = 1000, MaxAccessors = 10_000, MaxBufferViews = 10_000, MaxImages = 8,
        MaxImageDimension = 2048, MaxImagePixels = 4 << 20, MaxOutputBytes = 6 << 20,
    };

    // Decoding is the one place a small input legitimately turns into a larger allocation, so the image target caps pixels hard.
    private static readonly GltfLimits ImageTight = new() { MaxImageDimension = 256, MaxImagePixels = 65_536, MaxOutputBytes = 1 << 20 };

    /// <summary>Every target.</summary>
    public static IReadOnlyList<GltfFuzzTarget> All { get; } =
    [
        new GltfFuzzTarget("gltf-glb-chunks", RunChunks, ChunkSeeds),
        new GltfFuzzTarget("gltf-json", RunImport, JsonSeeds),
        new GltfFuzzTarget("gltf-edit", RunEdit, EditSeeds),
        new GltfFuzzTarget("gltf-accessors", RunAccessors, AccessorSeeds),
        new GltfFuzzTarget("gltf-import", RunImport, ImportSeeds),
        new GltfFuzzTarget("gltf-image", RunImage, ImageSeeds),
        new GltfFuzzTarget("gltf-uri", RunUri, UriSeeds),
    ];

    private static string? ModelsRoot()
    {
        var d = Path.Combine(AppContext.BaseDirectory, "testdata", "gltf");
        return Directory.Exists(d) ? d : null;
    }

    private static bool TryLoad(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            _ = GltfLoader.Load(bytes, new GltfLoadOptions { Limits = Tight, DecodeImages = false });
            return true;
        }
        catch (GltfException)
        {
            return false;
        }
    }

    // GLB container -------------------------------------------------------------------------------------------------------------

    private static bool RunChunks(byte[] input)
    {
        try
        {
            GlbContainer.Split(input, Tight, out var json, out var bin);
            return json.Length >= 0 && bin.Length >= 0;
        }
        catch (GltfException)
        {
            return false;
        }
    }

    private static IEnumerable<byte[]> ChunkSeeds()
    {
        yield return Glb(Encoding.UTF8.GetBytes("{\"asset\":{\"version\":\"2.0\"}}  "), []);
        yield return Glb(Encoding.UTF8.GetBytes("{\"asset\":{\"version\":\"2.0\"}}  "), new byte[64]);
        foreach (var s in ImportSeeds().Where(s => s.Length is > 4 and < 20_000 && s[0] == (byte)0x67))
        {
            yield return s;
        }
    }

    private static byte[] Glb(byte[] json, byte[] bin)
    {
        var padded = json.Length % 4 == 0 ? json : [.. json, .. Enumerable.Repeat((byte)' ', 4 - (json.Length % 4))];
        var total = 12 + 8 + padded.Length + (bin.Length > 0 ? 8 + bin.Length : 0);
        var data = new byte[total];
        BinaryPrimitives.WriteUInt32LittleEndian(data, 0x46546C67);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4), 2);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(8), (uint)total);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(12), (uint)padded.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(16), 0x4E4F534A);
        padded.CopyTo(data, 20);
        if (bin.Length > 0)
        {
            var at = 20 + padded.Length;
            BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(at), (uint)bin.Length);
            BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(at + 4), 0x004E4942);
            bin.CopyTo(data, at + 8);
        }

        return data;
    }

    // Whole files ---------------------------------------------------------------------------------------------------------------

    private static bool RunImport(byte[] input) => TryLoad(input);

    private static IEnumerable<byte[]> ImportSeeds()
    {
        yield return Glb(Encoding.UTF8.GetBytes(KitchenSink(out var bin)), bin);
        if (ModelsRoot() is { } root)
        {
            foreach (var rel in new[] { "Box/glTF-Binary/Box.glb", "AnimatedMorphCube/glTF-Binary/AnimatedMorphCube.glb", "EmissiveStrengthTest/glTF-Binary/EmissiveStrengthTest.glb", "Fox/glTF-Binary/Fox.glb" })
            {
                if (File.Exists(Path.Combine(root, rel)))
                {
                    yield return File.ReadAllBytes(Path.Combine(root, rel));
                }
            }
        }
    }

    private static IEnumerable<byte[]> JsonSeeds()
    {
        yield return Encoding.UTF8.GetBytes(KitchenSinkEmbedded());
        if (ModelsRoot() is { } root)
        {
            foreach (var rel in new[] { "Box/glTF-Embedded/Box.gltf", "SimpleSparseAccessor/glTF-Embedded/SimpleSparseAccessor.gltf" })
            {
                if (File.Exists(Path.Combine(root, rel)))
                {
                    yield return File.ReadAllBytes(Path.Combine(root, rel));
                }
            }
        }
    }

    // Numeric edits of a rich document --------------------------------------------------------------------------------------------

    [GeneratedRegex(@"-?\d+(\.\d+)?")]
    private static partial Regex Number();

    private static readonly string[] Interesting =
    [
        "0", "1", "2", "3", "-1", "255", "256", "4294967295", "2147483647", "2147483648", "-2147483648", "9223372036854775807", "4503599627370496",
        "1e39", "0.5", "16777216", "65535", "65536", "5120", "5126", "9999",
    ];

    private static bool RunEdit(byte[] input)
    {
        var template = KitchenSinkEmbedded();
        var matches = Number().Matches(template);
        var sb = new StringBuilder(template.Length + 64);
        var last = 0;
        var edits = new Dictionary<int, string>();
        for (var i = 0; i + 2 < input.Length && edits.Count < 8; i += 3)
        {
            var token = input[i] % matches.Count;
            var value = input[i + 1] < Interesting.Length * 8
                ? Interesting[input[i + 1] % Interesting.Length]
                : ((short)((input[i + 1] << 8) | input[i + 2])).ToString(CultureInfo.InvariantCulture);
            edits[token] = value;
        }

        for (var t = 0; t < matches.Count; t++)
        {
            sb.Append(template, last, matches[t].Index - last);
            sb.Append(edits.TryGetValue(t, out var v) ? v : matches[t].Value);
            last = matches[t].Index + matches[t].Length;
        }

        sb.Append(template, last, template.Length - last);
        return TryLoad(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    private static IEnumerable<byte[]> EditSeeds()
    {
        yield return [];
        yield return [3, 5, 0];
        yield return [40, 9, 1, 90, 2, 0];
    }

    // Accessors with every parameter taken from the input ---------------------------------------------------------------------------

    /// <summary>The first 16 bytes choose an accessor's type, count, offsets, stride and sparse part; the rest is the buffer.</summary>
    private static bool RunAccessors(byte[] input)
    {
        if (input.Length < 16)
        {
            return false;
        }

        int[] componentTypes = [5120, 5121, 5122, 5123, 5125, 5126];
        string[] types = ["SCALAR", "VEC2", "VEC3", "VEC4", "MAT2", "MAT3", "MAT4"];
        var componentType = componentTypes[input[0] % componentTypes.Length];
        var type = types[input[1] % types.Length];
        var normalized = (input[2] & 1) != 0;
        var count = (input[3] % 40) + 1;
        var accessorOffset = input[4] * 3;
        var viewOffset = input[5];
        var stride = (input[6] & 3) == 0 ? (input[7] % 70) : 0;
        var useView = (input[8] & 7) != 0;
        var sparse = (input[8] & 8) != 0;
        var sparseCount = (input[9] % count) + 1;
        var indexType = new[] { 5121, 5123, 5125 }[input[10] % 3];
        var indexOffset = input[11];
        var valueOffset = input[12];
        var mode = new[] { "POSITION", "indices", "weights" }[input[13] % 3];
        var data = input[16..];
        var length = Math.Max(data.Length, 1);
        var view = useView ? $"{{\"buffer\":0,\"byteOffset\":{viewOffset},\"byteLength\":{Math.Max(1, length - Math.Min(viewOffset, length - 1))}{(stride > 0 ? ",\"byteStride\":" + stride : "")}}}" : "{\"buffer\":0,\"byteLength\":1}";
        var accessor = $"{{\"componentType\":{componentType},\"count\":{count},\"type\":\"{type}\",\"normalized\":{(normalized ? "true" : "false")}"
            + (useView ? $",\"bufferView\":1,\"byteOffset\":{accessorOffset}" : string.Empty)
            + (sparse ? $",\"sparse\":{{\"count\":{sparseCount},\"indices\":{{\"bufferView\":2,\"byteOffset\":{indexOffset},\"componentType\":{indexType}}},\"values\":{{\"bufferView\":3,\"byteOffset\":{valueOffset}}}}}" : string.Empty) + "}";
        var reference = mode switch
        {
            "POSITION" => "\"attributes\":{\"POSITION\":0}",
            "indices" => "\"attributes\":{\"POSITION\":1},\"indices\":0",
            _ => "\"attributes\":{\"POSITION\":1,\"JOINTS_0\":0,\"WEIGHTS_0\":0}",
        };
        var json = $"{{\"asset\":{{\"version\":\"2.0\"}},\"meshes\":[{{\"primitives\":[{{{reference}}}]}}],"
            + $"\"buffers\":[{{\"byteLength\":{length},\"uri\":\"data:application/octet-stream;base64,{Convert.ToBase64String(data.Length == 0 ? new byte[1] : data)}\"}}],"
            + $"\"bufferViews\":[{{\"buffer\":0,\"byteLength\":1}},{view},{{\"buffer\":0,\"byteLength\":{length}}},{{\"buffer\":0,\"byteLength\":{length}}}],"
            + $"\"accessors\":[{accessor},{{\"componentType\":5126,\"count\":3,\"type\":\"VEC3\",\"bufferView\":2}}]}}";
        return TryLoad(Encoding.UTF8.GetBytes(json));
    }

    private static IEnumerable<byte[]> AccessorSeeds()
    {
        var floats = new byte[96];
        for (var i = 0; i < 24; i++)
        {
            BitConverter.GetBytes(i * 0.25f).CopyTo(floats, i * 4);
        }

        // Floats VEC3 positions with a view; 16-bit indices that are valid for a 3-vertex triangle; a float VEC3 accessor with a sparse part;
        // joints and weights as bytes for three vertices.
        var indexData = new byte[96];
        var k = 0;
        foreach (var v in new ushort[] { 0, 1, 2, 2, 1, 0, 0, 1, 2, 0, 1, 2 })
        {
            BitConverter.GetBytes(v).CopyTo(indexData, k);
            k += 2;
        }

        yield return [5, 2, 0, 5, 0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, .. floats];
        yield return [3, 0, 0, 11, 0, 0, 1, 0, 1, 0, 0, 0, 0, 1, 0, 0, .. indexData];
        yield return [5, 2, 0, 5, 0, 0, 1, 0, 9, 0, 0, 0, 0, 0, 0, 0, .. floats];
        yield return [1, 3, 1, 2, 0, 0, 0, 0, 1, 0, 0, 0, 0, 2, 0, 0, .. floats];
    }

    // Images and uris ---------------------------------------------------------------------------------------------------------------

    private static bool RunImage(byte[] input)
    {
        var json = $"{{\"asset\":{{\"version\":\"2.0\"}},\"images\":[{{\"uri\":\"data:image/png;base64,{Convert.ToBase64String(input)}\"}}]}}";
        try
        {
            _ = GltfLoader.Load(Encoding.UTF8.GetBytes(json), new GltfLoadOptions { Limits = ImageTight, DecodeImages = true });
            return true;
        }
        catch (GltfException)
        {
            return false;
        }
    }

    private static IEnumerable<byte[]> ImageSeeds()
    {
        yield return Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");
    }

    private static bool RunUri(byte[] input)
    {
        var text = Encoding.UTF8.GetString(input);
        try
        {
            var path = GltfUri.CheckRelative(text);
            // Whatever is accepted must be a relative path that cannot climb or name a scheme.
            if (path.StartsWith('/') || path.Contains(':', StringComparison.Ordinal) || path.Contains('\\', StringComparison.Ordinal) || path.Split('/').Contains(".."))
            {
                throw new InvalidOperationException($"accepted an unsafe uri: {path}");
            }

            return true;
        }
        catch (GltfException)
        {
            return false;
        }
    }

    private static IEnumerable<byte[]> UriSeeds()
    {
        foreach (var s in new[] { "textures/a.png", "a%20b.bin", "../x", "%2e%2e/x", "C:\\x", "http://x/y", "a/./b.png" })
        {
            yield return Encoding.UTF8.GetBytes(s);
        }
    }

    // A document that uses every part of the model ------------------------------------------------------------------------------------

    internal static string KitchenSinkEmbedded()
    {
        var json = KitchenSink(out var bin);
        return json.Replace("\"buffers\":[{\"byteLength\":" + bin.Length.ToString(CultureInfo.InvariantCulture) + "}]", "\"buffers\":[{\"byteLength\":" + bin.Length.ToString(CultureInfo.InvariantCulture) + ",\"uri\":\"data:application/octet-stream;base64," + Convert.ToBase64String(bin) + "\"}]", StringComparison.Ordinal);
    }

    /// <summary>
    /// A small valid file with a skinned morphing mesh, normalized colours, a sparse accessor, a camera, a light, materials with
    /// extensions and three kinds of animation. The binary part is returned separately.
    /// </summary>
    internal static string KitchenSink(out byte[] bin)
    {
        var b = new List<byte>();
        var views = new List<string>();
        var accessors = new List<string>();

        int View(IEnumerable<byte> bytes)
        {
            while (b.Count % 4 != 0)
            {
                b.Add(0);
            }

            var start = b.Count;
            b.AddRange(bytes);
            views.Add($"{{\"buffer\":0,\"byteOffset\":{start},\"byteLength\":{b.Count - start}}}");
            return views.Count - 1;
        }

        int Acc(int view, int componentType, int count, string type, string extra = "")
        {
            accessors.Add($"{{\"bufferView\":{view},\"componentType\":{componentType},\"count\":{count},\"type\":\"{type}\"{extra}}}");
            return accessors.Count - 1;
        }

        IEnumerable<byte> Floats(params float[] v) => v.SelectMany(BitConverter.GetBytes);
        IEnumerable<byte> UShorts(params ushort[] v) => v.SelectMany(BitConverter.GetBytes);

        var pos = Acc(View(Floats(0, 0, 0, 1, 0, 0, 0, 1, 0, 1, 1, 0)), 5126, 4, "VEC3");
        var nor = Acc(View(Floats(0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1)), 5126, 4, "VEC3");
        var uv = Acc(View(Floats(0, 1, 1, 1, 0, 0, 1, 0)), 5126, 4, "VEC2");
        var col = Acc(View([255, 0, 0, 255, 0, 255, 0, 255, 0, 0, 255, 255, 255, 255, 255, 128]), 5121, 4, "VEC4", ",\"normalized\":true");
        var joints = Acc(View([0, 1, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0]), 5121, 4, "VEC4");
        var weights = Acc(View(Floats(1, 0, 0, 0, 0.5f, 0.5f, 0, 0, 1, 0, 0, 0, 0.25f, 0.75f, 0, 0)), 5126, 4, "VEC4");
        var idx = Acc(View(UShorts(0, 1, 2, 1, 3, 2)), 5123, 6, "SCALAR");
        var morph = Acc(View(Floats(0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1)), 5126, 4, "VEC3");
        var ibm = Acc(View(Floats(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, -1, 0, 0, 1)), 5126, 2, "MAT4");
        var times = Acc(View(Floats(0, 0.5f, 1)), 5126, 3, "SCALAR");
        var moves = Acc(View(Floats(0, 0, 0, 1, 0, 0, 2, 0, 0)), 5126, 3, "VEC3");
        var turns = Acc(View(Floats(0, 0, 0, 1, 0, 0, 0.7071f, 0.7071f, 0, 0, 0, 1)), 5126, 3, "VEC4");
        var wts = Acc(View(Floats(0, 0.5f, 1)), 5126, 3, "SCALAR");
        var splineTimes = Acc(View(Floats(0, 1)), 5126, 2, "SCALAR");
        var spline = Acc(View(Floats(0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 3, 0, 0, 0, 0, 0)), 5126, 6, "VEC3");
        var sparseIdx = View([1, 3]);
        var sparseVal = View(Floats(5, 5, 5, 6, 6, 6));
        _ = Acc(-1, 5126, 4, "VEC3", $",\"sparse\":{{\"count\":2,\"indices\":{{\"bufferView\":{sparseIdx},\"componentType\":5121}},\"values\":{{\"bufferView\":{sparseVal}}}}}");
        accessors[^1] = accessors[^1].Replace("\"bufferView\":-1,", string.Empty, StringComparison.Ordinal);
        bin = [.. b];

        var template = """
            {"asset":{"version":"2.0","generator":"fuzz"},"scene":0,
             "extensionsUsed":["KHR_lights_punctual","KHR_materials_emissive_strength","KHR_materials_clearcoat","KHR_texture_transform"],
             "extensions":{"KHR_lights_punctual":{"lights":[{"type":"spot","intensity":2,"spot":{"outerConeAngle":0.5}}]}},
             "scenes":[{"nodes":[0,3]}],
             "nodes":[{"mesh":0,"skin":0,"children":[1]},{"translation":[0,1,0],"children":[2]},{"rotation":[0,0,0,1],"scale":[1,1,1]},{"camera":0,"extensions":{"KHR_lights_punctual":{"light":0}},"matrix":[1,0,0,0,0,1,0,0,0,0,1,0,0,0,5,1]}],
             "cameras":[{"type":"perspective","perspective":{"yfov":0.7,"znear":0.1,"zfar":100}}],
             "skins":[{"joints":[1,2],"inverseBindMatrices":@ibm@}],
             "materials":[{"pbrMetallicRoughness":{"baseColorFactor":[1,1,1,1]},"emissiveFactor":[1,0,0],"extensions":{"KHR_materials_emissive_strength":{"emissiveStrength":2},"KHR_materials_clearcoat":{"clearcoatFactor":1}}}],
             "meshes":[{"weights":[0.5],"primitives":[{"attributes":{"POSITION":@pos@,"NORMAL":@nor@,"TEXCOORD_0":@uv@,"COLOR_0":@col@,"JOINTS_0":@joints@,"WEIGHTS_0":@weights@},"indices":@idx@,"material":0,"targets":[{"POSITION":@morph@}]},
                                          {"attributes":{"POSITION":@LAST@},"mode":0,"targets":[{"POSITION":@morph@}]}]}],
             "animations":[{"samplers":[{"input":@times@,"output":@moves@},{"input":@times@,"output":@turns@,"interpolation":"STEP"},{"input":@times@,"output":@wts@},{"input":@splineTimes@,"output":@spline@,"interpolation":"CUBICSPLINE"}],
                            "channels":[{"sampler":0,"target":{"node":1,"path":"translation"}},{"sampler":1,"target":{"node":2,"path":"rotation"}},{"sampler":2,"target":{"node":0,"path":"weights"}},{"sampler":3,"target":{"node":3,"path":"scale"}}]}],
             "buffers":[{"byteLength":@LENGTH@}],
             "bufferViews":[@VIEWS@],
             "accessors":[@ACCESSORS@]}
            """;

        return template.Replace("@ibm@", ibm.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("@pos@", pos.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("@nor@", nor.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("@uv@", uv.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("@col@", col.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("@joints@", joints.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("@weights@", weights.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("@idx@", idx.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("@morph@", morph.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("@times@", times.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("@moves@", moves.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("@turns@", turns.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("@wts@", wts.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("@splineTimes@", splineTimes.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("@spline@", spline.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("@LAST@", (accessors.Count - 1).ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("@LENGTH@", bin.Length.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("@VIEWS@", string.Join(",", views), StringComparison.Ordinal)
            .Replace("@ACCESSORS@", string.Join(",", accessors), StringComparison.Ordinal);
    }
}
