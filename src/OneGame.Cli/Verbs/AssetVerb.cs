using System.Diagnostics;
using OneGame.Gltf;
using OneGame.Host;

namespace OneGame.Cli.Verbs;

/// <summary>Asset tools. <c>asset inspect &lt;file.gltf|file.glb&gt;</c> loads a glTF file the way the engine would and summarises it.</summary>
internal sealed class AssetVerb : IVerb
{
    private const int DefaultMaxItems = 200;

    public string Name => "asset";

    public string Summary => "Asset tools: inspect <file.gltf|file.glb> [--max-items N]  summarise a glTF model: counts, bounds, meshes, materials, images, animations, extensions, warnings.";

    public int Run(string[] args, bool json, TextWriter output, TextWriter error)
    {
        if (args.Length == 0 || args[0] != "inspect")
        {
            throw new CliException(ExitCodes.Usage, "bad_args", $"Usage: onegame asset inspect <file.gltf|file.glb> [--max-items N] [--json]. {Summary}");
        }

        string? file = null;
        var maxItems = DefaultMaxItems;
        for (var i = 1; i < args.Length; i++)
        {
            if (args[i] == "--max-items" && i + 1 < args.Length && int.TryParse(args[i + 1], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out maxItems))
            {
                i++;
            }
            else if (args[i].StartsWith("--", StringComparison.Ordinal) || file is not null)
            {
                throw new CliException(ExitCodes.Usage, "bad_args", $"Unexpected argument '{args[i]}'. Usage: onegame asset inspect <file.gltf|file.glb> [--max-items N].");
            }
            else
            {
                file = args[i];
            }
        }

        if (file is null)
        {
            throw new CliException(ExitCodes.Usage, "bad_args", "Missing the file. Usage: onegame asset inspect <file.gltf|file.glb> [--max-items N].");
        }

        if (!File.Exists(file))
        {
            throw new CliException(ExitCodes.RuntimeError, "file_not_found", $"'{file}' does not exist.");
        }

        var clock = Stopwatch.StartNew();
        GltfModel model;
        try
        {
            // Pixels are not needed to describe a model, so only image headers are read.
            model = GltfLoader.LoadFile(file, new GltfLoadOptions { DecodeImages = false });
        }
        catch (GltfException ex)
        {
            throw new CliException(ExitCodes.Failed, "gltf_" + ex.Code, ex.Message);
        }

        var result = Summarize(Path.GetFullPath(file), new FileInfo(file).Length, model, maxItems, clock.Elapsed.TotalSeconds);
        if (json)
        {
            output.WriteLine(JsonSerializer.Serialize(result, CliJson.Default.AssetInspectResult));
        }
        else
        {
            WriteText(output, result);
        }

        return ExitCodes.Ok;
    }

    internal static AssetInspectResult Summarize(string path, long bytes, GltfModel model, int maxItems, double seconds)
    {
        var truncated = false;
        T[] Cap<T>(IEnumerable<T> items, int total)
        {
            if (total > maxItems)
            {
                truncated = true;
            }

            return [.. items.Take(maxItems)];
        }

        var prims = model.Meshes.SelectMany(m => m.Primitives).ToArray();
        var counts = new AssetCounts(
            model.Scenes.Length, model.Nodes.Length, model.Meshes.Length, prims.Length,
            prims.Sum(p => (long)p.VertexCount),
            prims.Sum(p => (long)p.Indices.Length),
            prims.Where(p => p.Topology == GltfTopology.Triangles).Sum(p => (long)p.Indices.Length / 3),
            prims.Where(p => p.Topology == GltfTopology.Lines).Sum(p => (long)p.Indices.Length / 2),
            prims.Where(p => p.Topology == GltfTopology.Points).Sum(p => (long)p.Indices.Length),
            prims.Sum(p => p.MorphTargets.Length),
            model.Materials.Length, model.Textures.Length, model.Images.Length, model.Samplers.Length, model.Skins.Length,
            model.Animations.Length, model.Cameras.Length, model.Lights.Length);

        var world = model.ComputeWorldBounds();
        var roots = model.Nodes.Where(n => n.Parent < 0).ToArray();
        var nodeInfo = new AssetNodes(
            model.Nodes.Length,
            model.Nodes.Length == 0 ? 0 : model.Nodes.Max(n => n.Depth),
            roots.Length,
            Cap(roots.Select(n => n.Name), roots.Length),
            model.Nodes.Count(n => n.Mesh >= 0),
            model.Nodes.Count(n => n.Skin >= 0),
            model.Nodes.Count(n => n.Camera >= 0),
            model.Nodes.Count(n => n.Light >= 0));

        var meshes = Cap(model.Meshes.Select((m, i) => new AssetMesh(
            i, m.Name, m.Primitives.Length, m.Primitives.Sum(p => p.VertexCount), m.Primitives.Sum(p => p.Indices.Length), m.MorphTargetCount,
            [.. m.Primitives.Select(p => p.Material).Distinct()],
            m.Primitives.Any(p => p.Tangents is not null), m.Primitives.Any(p => p.Joints is not null), Box(m.Bounds))), model.Meshes.Length);

        var materials = Cap(model.Materials.Select((m, i) => new AssetMaterial(
            i, m.Name, m.AlphaMode.ToString(), m.DoubleSided, m.Unlit, Slots(m), MaterialExtensions(m))), model.Materials.Length);

        var images = Cap(model.Images.Select((im, i) => new AssetImage(
            i, im.Name, im.Format, im.MimeType, im.Width, im.Height, im.Status.ToString(), im.UsedAsColor, im.UsedAsData)), model.Images.Length);

        var animations = Cap(model.Animations.Select((a, i) => new AssetAnimation(
            i, a.Name, a.Duration, a.Channels.Length,
            [.. a.Channels.Select(c => c.Path.ToString()).Distinct()],
            [.. a.Samplers.Select(s => s.Interpolation.ToString()).Distinct()])), model.Animations.Length);

        return new AssetInspectResult(
            true, path, model.IsBinary ? "glb" : "gltf", bytes, model.Generator, model.Copyright, counts,
            world is { } w ? Box(w) : null, nodeInfo, meshes, materials, images, animations,
            new AssetExtensions(model.ExtensionsUsed, model.ExtensionsRequired),
            [.. model.Warnings.Select(x => new AssetWarning(x.Code, x.Message))],
            truncated, model.OutputBytes, seconds);
    }

    private static AssetBox Box(GltfBounds b) => new([b.Min.X, b.Min.Y, b.Min.Z], [b.Max.X, b.Max.Y, b.Max.Z]);

    private static int Slots(GltfMaterial m) => new object?[]
    {
        m.BaseColorTexture, m.MetallicRoughnessTexture, m.NormalTexture, m.OcclusionTexture, m.EmissiveTexture,
    }.Count(x => x is not null);

    private static string[] MaterialExtensions(GltfMaterial m)
    {
        var list = new List<string>();
        if (m.Unlit)
        {
            list.Add("KHR_materials_unlit");
        }

        if (m.EmissiveStrength != 1)
        {
            list.Add("KHR_materials_emissive_strength");
        }

        if (m.Clearcoat is not null)
        {
            list.Add("KHR_materials_clearcoat");
        }

        if (m.Transmission is not null)
        {
            list.Add("KHR_materials_transmission");
        }

        if (m.Ior != 1.5f)
        {
            list.Add("KHR_materials_ior");
        }

        if (m.Specular is not null)
        {
            list.Add("KHR_materials_specular");
        }

        if (m.Sheen is not null)
        {
            list.Add("KHR_materials_sheen");
        }

        if (m.Volume is not null)
        {
            list.Add("KHR_materials_volume");
        }

        if (new[] { m.BaseColorTexture, m.MetallicRoughnessTexture, m.NormalTexture, m.OcclusionTexture, m.EmissiveTexture }.Any(t => t?.Transform is not null))
        {
            list.Add("KHR_texture_transform");
        }

        return [.. list];
    }

    private static void WriteText(TextWriter o, AssetInspectResult r)
    {
        var c = r.Counts;
        o.WriteLine($"{r.File} ({r.Format}, {r.Bytes} bytes){(r.Generator.Length > 0 ? ", " + r.Generator : "")}");
        o.WriteLine($"  scenes {c.Scenes}, nodes {c.Nodes} (depth {r.Nodes.MaxDepth}), meshes {c.Meshes}, primitives {c.Primitives}, vertices {c.Vertices}, triangles {c.Triangles}, morph targets {c.MorphTargets}");
        o.WriteLine($"  materials {c.Materials}, textures {c.Textures}, images {c.Images}, skins {c.Skins}, animations {c.Animations}, cameras {c.Cameras}, lights {c.Lights}");
        if (r.Bounds is { } b)
        {
            o.WriteLine(System.FormattableString.Invariant($"  bounds ({b.Min[0]:0.###}, {b.Min[1]:0.###}, {b.Min[2]:0.###}) to ({b.Max[0]:0.###}, {b.Max[1]:0.###}, {b.Max[2]:0.###})"));
        }

        if (r.Extensions.Used.Length > 0)
        {
            o.WriteLine($"  extensions used: {string.Join(", ", r.Extensions.Used)}; required: {(r.Extensions.Required.Length == 0 ? "none" : string.Join(", ", r.Extensions.Required))}");
        }

        foreach (var m in r.Materials)
        {
            o.WriteLine($"  material {m.Index} '{m.Name}': {m.AlphaMode}{(m.DoubleSided ? ", double-sided" : "")}{(m.Unlit ? ", unlit" : "")}, {m.TextureSlots} textures{(m.Extensions.Length > 0 ? ", " + string.Join(" ", m.Extensions) : "")}");
        }

        foreach (var a in r.Animations)
        {
            o.WriteLine(System.FormattableString.Invariant($"  animation {a.Index} '{a.Name}': {a.Duration:0.###} s, {a.Channels} channels"));
        }

        foreach (var w in r.Warnings)
        {
            o.WriteLine($"  warning {w.Code}: {w.Message}");
        }
    }
}
