using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using OneImage;

namespace OneGame.Gltf;

/// <summary>One import run. Fields are filled in dependency order: buffers, accessors, images, materials, meshes, nodes, animations.</summary>
internal sealed partial class Importer
{
    internal static readonly string[] SupportedExtensions =
    [
        "KHR_lights_punctual", "KHR_materials_emissive_strength", "KHR_texture_transform", "KHR_materials_unlit", "KHR_mesh_quantization",
        "KHR_materials_clearcoat", "KHR_materials_transmission", "KHR_materials_ior", "KHR_materials_specular", "KHR_materials_sheen", "KHR_materials_volume",
    ];

    private readonly GltfLoadOptions _options;
    private readonly GltfLimits _limits;
    private readonly IGltfResources? _resources;
    private readonly Budget _budget;
    private readonly List<GltfWarning> _warnings = [];
    private readonly GltfModel _model = new();
    private JObj _root;
    private ReadOnlyMemory<byte> _bin;
    private Accessors _acc = null!;

    internal Importer(GltfLoadOptions options, IGltfResources? resources)
    {
        _options = options;
        _limits = options.Limits ?? new GltfLimits();
        _resources = resources;
        _budget = new Budget(_limits.MaxOutputBytes);
    }

    private const int MaxWarnings = 1000;

    internal void Warn(string code, string message)
    {
        // A hostile file can provoke one warning per attribute or channel; keep the list bounded.
        if (_warnings.Count < MaxWarnings)
        {
            _warnings.Add(new GltfWarning(code, message));
        }
        else if (_warnings.Count == MaxWarnings)
        {
            _warnings.Add(new GltfWarning("warnings_truncated", "Further warnings were dropped."));
        }
    }

    internal GltfModel Run(ReadOnlyMemory<byte> file)
    {
        if (file.Length > _limits.MaxFileBytes)
        {
            throw new GltfException("file_too_large", $"The file is {file.Length} bytes; the limit is {_limits.MaxFileBytes}.");
        }

        ReadOnlyMemory<byte> json;
        if (GlbContainer.IsGlb(file.Span))
        {
            GlbContainer.Split(file, _limits, out json, out _bin);
            _model.IsBinary = true;
        }
        else
        {
            json = file;
            if (json.Span.StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }))
            {
                json = json[3..];
            }

            if (json.Length > _limits.MaxJsonBytes)
            {
                throw new GltfException("limit_exceeded", $"The JSON text is {json.Length} bytes; the limit is {_limits.MaxJsonBytes}.");
            }
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = _limits.MaxJsonDepth, CommentHandling = JsonCommentHandling.Disallow, AllowTrailingCommas = false });
        }
        catch (JsonException ex)
        {
            throw new GltfException("json_invalid", $"The JSON is not valid: {ex.Message}");
        }

        using (doc)
        {
            _root = new JObj(doc.RootElement, "glTF");
            Build();
        }

        _model.Warnings = [.. _warnings];
        _model.OutputBytes = _budget.Used;
        return _model;
    }

    private void Build()
    {
        ReadAsset();
        ReadExtensionLists();
        var buffers = ReadBuffers();
        _acc = new Accessors(buffers, _root, _limits, _budget);
        var samplers = ReadSamplers();
        _model.Samplers = samplers;
        _model.Images = ReadImages();
        _model.Textures = ReadTextures();
        _model.Materials = ReadMaterials();
        _model.Meshes = ReadMeshes();
        ReadNodes();
        _model.Skins = ReadSkins();
        ValidateSkinUse();
        _model.Cameras = ReadCameras();
        _model.Lights = ReadLights();
        ReadScenes();
        _model.Animations = ReadAnimations();
    }

    private void ReadAsset()
    {
        var asset = _root.Obj("asset") ?? throw _root.Error("schema", "asset", "is required.");
        var version = asset.Str("version");
        if (!version.StartsWith("2.", StringComparison.Ordinal))
        {
            throw new GltfException("unsupported_version", $"asset.version '{GltfUri.Show(version)}' is not glTF 2.x.");
        }

        _model.Generator = asset.Str("generator");
        _model.Copyright = asset.Str("copyright");
    }

    private void ReadExtensionLists()
    {
        var used = _root.Strings("extensionsUsed", 1000);
        var required = _root.Strings("extensionsRequired", 1000);
        foreach (var r in required)
        {
            if (Array.IndexOf(SupportedExtensions, r) < 0)
            {
                throw new GltfException("unsupported_extension", $"The file requires the extension '{GltfUri.Show(r)}', which this importer does not support.");
            }
        }

        foreach (var u in used)
        {
            if (Array.IndexOf(SupportedExtensions, u) < 0)
            {
                Warn("ignored_extension", $"The extension '{GltfUri.Show(u)}' is not supported and was ignored.");
            }
        }

        _model.ExtensionsUsed = used;
        _model.ExtensionsRequired = required;
    }

    private ReadOnlyMemory<byte>[] ReadBuffers()
    {
        var count = _root.ArrayLength("buffers", _limits.MaxBuffers);
        var result = new ReadOnlyMemory<byte>[count];
        long total = 0;
        for (var i = 0; i < count; i++)
        {
            var b = _root.Item("buffers", i);
            var length = b.ReqLong("byteLength", 1);
            if (length > _limits.MaxBufferBytes)
            {
                throw b.Error("limit_exceeded", "byteLength", $"is {length.ToString(CultureInfo.InvariantCulture)}; the limit is {_limits.MaxBufferBytes.ToString(CultureInfo.InvariantCulture)}.");
            }

            total += length;
            if (total > _limits.MaxTotalBufferBytes)
            {
                throw b.Error("limit_exceeded", "byteLength", $"brings the buffers to {total.ToString(CultureInfo.InvariantCulture)} bytes; the limit is {_limits.MaxTotalBufferBytes.ToString(CultureInfo.InvariantCulture)}.");
            }

            ReadOnlyMemory<byte> data;
            if (!b.TryGet("uri", out _))
            {
                if (i != 0 || !_model.IsBinary || _bin.Length == 0)
                {
                    throw b.Error("buffer_truncated", "uri", "is missing and there is no GLB binary chunk for this buffer.");
                }

                data = _bin;
            }
            else
            {
                data = LoadBytes(b.Str("uri"), length, b.Path("uri"));
            }

            if (data.Length < length)
            {
                throw b.Error("buffer_truncated", "byteLength", $"is {length.ToString(CultureInfo.InvariantCulture)} but only {data.Length.ToString(CultureInfo.InvariantCulture)} bytes are there.");
            }

            result[i] = data[..(int)length];
        }

        return result;
    }

    /// <summary>Loads the bytes behind a uri: a data uri or an external file through the resolver.</summary>
    private ReadOnlyMemory<byte> LoadBytes(string uri, long expected, string where)
    {
        if (GltfUri.IsData(uri))
        {
            return GltfUri.DecodeData(uri, Math.Max(expected, 1), _budget);
        }

        var path = GltfUri.CheckRelative(uri);
        if (_resources is null)
        {
            throw new GltfException("external_not_allowed", $"{where} names the external file '{GltfUri.Show(path)}', but no resolver was given.");
        }

        var bytes = _resources.Read(path, Math.Min(Math.Max(expected, 1), _limits.MaxBufferBytes))
            ?? throw new GltfException("resource_missing", $"{where}: the file '{GltfUri.Show(path)}' does not exist.");
        _budget.Charge(bytes.Length, where);
        return bytes;
    }

    private GltfSampler[] ReadSamplers()
    {
        var count = _root.ArrayLength("samplers", _limits.MaxSmallLists);
        var result = new GltfSampler[count];
        for (var i = 0; i < count; i++)
        {
            var s = _root.Item("samplers", i);
            result[i] = new GltfSampler
            {
                Name = s.Str("name"),
                MagFilter = s.Int("magFilter", 0) switch { 9728 => GltfMagFilter.Nearest, 9729 => GltfMagFilter.Linear, _ => GltfMagFilter.Unspecified },
                MinFilter = s.Int("minFilter", 0) switch
                {
                    9728 => GltfMinFilter.Nearest,
                    9729 => GltfMinFilter.Linear,
                    9984 => GltfMinFilter.NearestMipmapNearest,
                    9985 => GltfMinFilter.LinearMipmapNearest,
                    9986 => GltfMinFilter.NearestMipmapLinear,
                    9987 => GltfMinFilter.LinearMipmapLinear,
                    _ => GltfMinFilter.Unspecified,
                },
                WrapS = Wrap(s.Int("wrapS", 10497)),
                WrapT = Wrap(s.Int("wrapT", 10497)),
            };
        }

        return result;
    }

    private static GltfWrap Wrap(int code) => code switch { 33071 => GltfWrap.ClampToEdge, 33648 => GltfWrap.MirroredRepeat, _ => GltfWrap.Repeat };

    private GltfImage[] ReadImages()
    {
        var count = _root.ArrayLength("images", _limits.MaxImages);
        var result = new GltfImage[count];
        for (var i = 0; i < count; i++)
        {
            var im = _root.Item("images", i);
            var image = new GltfImage { Name = im.Str("name"), MimeType = im.Str("mimeType") };
            ReadOnlyMemory<byte> bytes;
            if (im.TryGet("uri", out _))
            {
                bytes = LoadBytes(im.Str("uri"), _limits.MaxFileBytes, im.Path("uri"));
            }
            else if (im.Ref("bufferView", _acc.ViewCount) is var view and >= 0)
            {
                bytes = _acc.ViewMemory(view);
            }
            else
            {
                throw im.Error("schema", "uri", "or bufferView is required.");
            }

            DecodeImage(image, bytes, i);
            result[i] = image;
        }

        return result;
    }

    private void DecodeImage(GltfImage image, ReadOnlyMemory<byte> bytes, int index)
    {
        var s = bytes.Span;
        if (s.Length >= 8 && s[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            image.Format = "png";
        }
        else if (s.Length >= 3 && s[0] == 0xFF && s[1] == 0xD8 && s[2] == 0xFF)
        {
            image.Format = "jpeg";
        }
        else if (s.Length >= 12 && s[..4].SequenceEqual("RIFF"u8) && s[8..12].SequenceEqual("WEBP"u8))
        {
            image.Format = "webp";
        }
        else if (s.Length >= 12 && s[1..7].SequenceEqual("KTX 20"u8))
        {
            image.Format = "ktx2";
        }

        if (image.Format is not ("png" or "jpeg"))
        {
            image.Status = ImageStatus.Unsupported;
            Warn("unsupported_image", $"images[{index.ToString(CultureInfo.InvariantCulture)}] is {(image.Format == "unknown" ? "in an unknown format" : image.Format)}; only PNG and JPEG are read, so it has no pixels.");
            return;
        }

        var load = new LoadOptions
        {
            Configuration = ImageConfiguration.Default,
            Limits = new ImageLimits { MaxDimension = _limits.MaxImageDimension, MaxPixelsPerFrame = _limits.MaxImagePixels, MaxFrames = 1, MaxTotalPixels = _limits.MaxImagePixels },
            AutoOrient = false,
        };
        try
        {
            // The header is read first, so the size limits apply before any pixel memory exists.
            var info = Image.Identify(bytes, load);
            if (info.Width <= 0 || info.Height <= 0 || info.Width > _limits.MaxImageDimension || info.Height > _limits.MaxImageDimension || (long)info.Width * info.Height > _limits.MaxImagePixels)
            {
                throw new GltfException("limit_exceeded", $"images[{index.ToString(CultureInfo.InvariantCulture)}] is {info.Width}x{info.Height}; the limits are {_limits.MaxImageDimension} per side and {_limits.MaxImagePixels} pixels.");
            }

            image.Width = info.Width;
            image.Height = info.Height;
            if (!_options.DecodeImages)
            {
                image.Status = ImageStatus.HeaderOnly;
                return;
            }

            _budget.Charge((long)info.Width * info.Height * 4, $"images[{index.ToString(CultureInfo.InvariantCulture)}] pixels");
            var decoded = Image.Load(bytes, load);
            // Rgba32 is four bytes in R, G, B, A order, so the pixels are written straight into the byte array.
            var raw = new byte[(long)info.Width * info.Height * 4];
            decoded.CopyPixelsTo<Rgba32>(MemoryMarshal.Cast<byte, Rgba32>(raw.AsSpan()), new ImageRect(0, 0, info.Width, info.Height));
            image.Pixels = raw;
            image.Status = ImageStatus.Decoded;
        }
        catch (ImageException ex)
        {
            throw new GltfException("image_invalid", $"images[{index.ToString(CultureInfo.InvariantCulture)}] could not be read: {ex.Message}");
        }
    }

    private GltfTexture[] ReadTextures()
    {
        var count = _root.ArrayLength("textures", _limits.MaxTextures);
        var result = new GltfTexture[count];
        for (var i = 0; i < count; i++)
        {
            var t = _root.Item("textures", i);
            var source = t.Ref("source", _model.Images.Length);
            if (source < 0)
            {
                Warn("unsupported_texture_source", $"textures[{i.ToString(CultureInfo.InvariantCulture)}] has no PNG or JPEG source (it needs an extension this importer does not read).");
            }

            result[i] = new GltfTexture { Name = t.Str("name"), Source = source, Sampler = t.Ref("sampler", _model.Samplers.Length) };
        }

        return result;
    }
}
