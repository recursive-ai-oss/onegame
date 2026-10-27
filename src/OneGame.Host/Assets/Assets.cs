using System.Runtime.InteropServices;
using System.Threading.Tasks;
using OneGame.Audio;
using OneGame.Gltf;
using OneGame.Render2D;
using OneGame.Text;
using OneImage;
using OneImage.Svg;
using OneYaml;

namespace OneGame.Host;

/// <summary>Decoded pixels waiting to become a texture: straight-alpha sRGB RGBA, top row first.</summary>
internal sealed record Pixels(int Width, int Height, byte[] Rgba);

/// <summary>
/// The asset service: loads files from the game's content folders into the types the engine draws, plays and reads, caches them by path
/// and, in development, swaps an edited file in under the existing handle. Everything here is built on the low-level loaders
/// (OneImage, OneYaml, OneAudio, <see cref="GltfLoader"/>, <see cref="FontLibrary"/>), which stay usable directly.
/// </summary>
/// <remarks>
/// A <see cref="GameInstance"/> owns one (<see cref="GameInstance.Assets"/>); its 2D stage attaches the renderer when it makes one. Files are
/// untrusted: each kind has a size limit checked before the file is read into memory, and decoders run with the same limits the
/// art atlas uses. Reload logic lives here; what watches the files lives in the development control layer, so exports contain no watcher.
/// </remarks>
public sealed class Assets : IDisposable
{
    private const int MaxEvents = 256;

    private static readonly ImageConfiguration SvgConfig = new ImageConfiguration().Add(SvgFormat.Instance);
    private static Assets? s_active;

    private readonly Dictionary<string, Asset> _cache = new(StringComparer.Ordinal);
    private readonly List<AssetEvent> _events = [];
    private Renderer2D? _renderer;
    private long _sequence;

    /// <summary>Creates a service over <paramref name="source"/> (files from the default roots when null) and makes it <see cref="Active"/>.</summary>
    /// <param name="source">Where content comes from.</param>
    public Assets(IAssetSource? source = null)
    {
        Source = source ?? new FileAssetSource();
        Volatile.Write(ref s_active, this);
    }

    /// <summary>The most recently created service, so tools can find a game's assets without being handed them.</summary>
    public static Assets? Active => Volatile.Read(ref s_active);

    /// <summary>Where content is read from.</summary>
    public IAssetSource Source { get; }

    /// <summary>Largest raster or SVG file read, in bytes. Default 32 MiB.</summary>
    public long MaxImageBytes { get; set; } = 32L * 1024 * 1024;

    /// <summary>Largest YAML file read, in bytes. Default 8 MiB.</summary>
    public long MaxDataBytes { get; set; } = 8L * 1024 * 1024;

    /// <summary>Largest audio file read, in bytes. Default 64 MiB (the audio service also limits clip length).</summary>
    public long MaxSoundBytes { get; set; } = 64L * 1024 * 1024;

    /// <summary>Raised for every <see cref="AssetEvent"/>.</summary>
    public event Action<AssetEvent>? EventRaised;

    /// <summary>The 2D renderer textures are uploaded with, or null until a stage attached one.</summary>
    public Renderer2D? Renderer => _renderer;

    /// <summary>The newest event's sequence number, or 0.</summary>
    public long LastSequence => _sequence;

    /// <summary>The last 256 events, oldest first.</summary>
    public IReadOnlyList<AssetEvent> Events => _events;

    /// <summary>The loaded assets, in no particular order.</summary>
    public IEnumerable<Asset> Items => _cache.Values;

    /// <summary>A digest over every data asset (path and file digest, in path order): equal when the game runs with the same data files.</summary>
    public string DataDigest
    {
        get
        {
            var h = Fnv.Start;
            foreach (var a in _cache.Values.Where(a => a.Kind == AssetKind.Data).OrderBy(a => a.Key, StringComparer.Ordinal))
            {
                h = Fnv.Add(h, Encoding.UTF8.GetBytes(a.Key + a.Digest));
            }

            return Fnv.Hex(h);
        }
    }

    /// <summary>Gives textures a renderer to upload with. Textures loaded earlier are uploaded now; when the renderer is a different one, they are rebuilt on it.</summary>
    /// <param name="renderer">The renderer.</param>
    public void AttachRenderer(Renderer2D renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        if (ReferenceEquals(_renderer, renderer))
        {
            return;
        }

        var had = _renderer is not null;
        _renderer = renderer;
        foreach (var a in _cache.Values.ToArray())
        {
            if (a.Kind is not (AssetKind.Texture or AssetKind.Svg) || !a.IsReady)
            {
                continue;
            }

            if (had)
            {
                // The old texture belongs to the old renderer; read the file again for the new one.
                a.Free();
                a.Staged = null;
                Reload(a);
            }
            else if (a.Staged is { } staged)
            {
                a.Install(staged);
            }
        }
    }

    // ---- loading ----

    /// <summary>Loads a raster image (PNG, JPEG, WebP and the other formats OneImage reads) as a texture. Edits to the file show from the next frame in development.</summary>
    /// <param name="path">Path under a content folder, like <c>art/ship.png</c>.</param>
    /// <param name="filter">How the texture is filtered when scaled; <see cref="TextureFilter.Nearest"/> for pixel art.</param>
    /// <param name="background">Read and decode on another thread and return at once; see <see cref="Asset.IsReady"/>.</param>
    /// <returns>The shared handle; <see cref="Asset{T}.Value"/> is the texture.</returns>
    /// <exception cref="AssetException">The file is missing, too large or damaged (not for a background load, which reports through the handle).</exception>
    public Asset<Texture2D> LoadTexture(string path, TextureFilter filter = TextureFilter.Linear, bool background = false) =>
        Load<Texture2D>(AssetKind.Texture, path, filter.ToString(), background, MaxImageBytes, canReload: true, deferred: false,
            bytes => DecodeImage(path, bytes, svg: false, 0, 0), UploadTexture(filter), static t => t.Dispose());

    /// <summary>Loads an SVG drawn at the given pixel size as a texture, so it is crisp at that size. The same file at another size is another asset.</summary>
    /// <param name="path">Path under a content folder, like <c>art/icon.svg</c>.</param>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="background">Decode on another thread and return at once.</param>
    /// <returns>The shared handle.</returns>
    /// <exception cref="AssetException">The file is missing, too large or damaged.</exception>
    public Asset<Texture2D> LoadSvg(string path, int width, int height, bool background = false)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(width, 8192);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(height, 8192);
        return Load<Texture2D>(AssetKind.Svg, path, $"{width}x{height}", background, Math.Min(MaxImageBytes, 4L * 1024 * 1024), canReload: true, deferred: false,
            bytes => DecodeImage(path, bytes, svg: true, width, height), UploadTexture(TextureFilter.Linear), static t => t.Dispose());
    }

    /// <summary>
    /// Reads a YAML file into one of the game's own types through its source-generated metadata
    /// (<c>[YamlSerializable]</c> context). Create the context with <c>UnmappedMemberHandling = Disallow</c> to reject unknown keys.
    /// A reload of a data file is read and checked when the file changes but only swapped in by <see cref="ApplyPendingData"/>, because sim code reads data.
    /// </summary>
    /// <typeparam name="T">The data type.</typeparam>
    /// <param name="path">Path under a content folder, like <c>data/ships.yaml</c>.</param>
    /// <param name="typeInfo">Generated metadata, like <c>GameYaml.Default.Ships</c>.</param>
    /// <param name="background">Read and parse on another thread and return at once.</param>
    /// <returns>The shared handle; <see cref="Asset{T}.Value"/> is the data.</returns>
    /// <exception cref="AssetException">The file is missing, too large or does not match the type.</exception>
    public Asset<T> LoadData<T>(string path, YamlTypeInfo<T> typeInfo, bool background = false)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        return Load<T>(AssetKind.Data, path, typeof(T).FullName ?? typeof(T).Name, background, MaxDataBytes, canReload: true, deferred: true,
            bytes => ParseYaml(path, bytes, typeInfo), static (p, _) => (T)p, null);
    }

    /// <summary>Decodes an audio clip with the game's audio service (<see cref="AudioService.Active"/>). A reload replaces <see cref="Asset{T}.Value"/>; voices already playing keep the old clip.</summary>
    /// <param name="path">Path under a content folder, like <c>data/hit.ogg</c>.</param>
    /// <param name="background">Read the file on another thread; decoding finishes on the caller's thread.</param>
    /// <returns>The shared handle.</returns>
    /// <exception cref="AssetException">The file is missing, too large, damaged, or no audio service exists.</exception>
    public Asset<Sound> LoadSound(string path, bool background = false) =>
        Load<Sound>(AssetKind.Sound, path, "", background, MaxSoundBytes, canReload: true, deferred: false, static b => b,
            (p, _) => (AudioService.Active ?? throw new AssetException("no_audio", path, "No audio service exists. Create one with AudioService.Create() before loading sounds.")).LoadSound(new MemoryStream((byte[])p), path), null);

    /// <summary>Imports a glTF model (<c>.gltf</c> with its buffers and images in the same folder, or <c>.glb</c>). Only the model file itself is watched for edits.</summary>
    /// <param name="path">Path under a content folder, like <c>art/ship.glb</c>.</param>
    /// <param name="background">Import on another thread and return at once.</param>
    /// <returns>The shared handle.</returns>
    /// <exception cref="AssetException">The file is missing, too large, damaged or uses something unsupported.</exception>
    public Asset<GltfModel> LoadModel(string path, bool background = false)
    {
        var dir = Source.PhysicalPath(path) is { } full ? Path.GetDirectoryName(full) : null;
        return Load<GltfModel>(AssetKind.Model, path, "", background, new GltfLimits().MaxFileBytes, canReload: true, deferred: false,
            bytes => GltfLoader.Load(bytes, null, dir is null ? null : new GltfFolderResources(dir)), static (p, _) => (GltfModel)p, null);
    }

    /// <summary>Adds a font file to a library. Fonts are loaded once and not hot reloaded.</summary>
    /// <param name="path">Path under a content folder, like <c>fonts/Body.ttf</c>.</param>
    /// <param name="library">The library to add the face to.</param>
    /// <returns>The shared handle; <see cref="Asset{T}.Value"/> names the face.</returns>
    /// <exception cref="AssetException">The file is missing, too large or not a usable font.</exception>
    public Asset<LoadedFont> LoadFont(string path, FontLibrary library)
    {
        ArgumentNullException.ThrowIfNull(library);
        return Load<LoadedFont>(AssetKind.Font, path, library.GetHashCode().ToString(CultureInfo.InvariantCulture), false, FontLibrary.MaxFontBytes, canReload: false, deferred: false,
            static b => b, (p, _) => new LoadedFont(library, library.Add((byte[])p)), null);
    }

    /// <summary>Finishes background loads that completed. The host calls this every frame; call it yourself when you load without a host.</summary>
    /// <returns>How many assets became ready or failed.</returns>
    public int Update()
    {
        var n = 0;
        foreach (var a in _cache.Values.Where(a => a.Background is { IsCompleted: true }).ToArray())
        {
            Finish(a);
            n++;
        }

        return n;
    }

    // ---- lifetime ----

    /// <summary>Gives one reference back; at zero the asset is freed and forgotten. Same as <see cref="Asset.Release"/>.</summary>
    /// <param name="asset">The handle.</param>
    public void Release(Asset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        if (!_cache.ContainsKey(asset.Key) || --asset.References > 0)
        {
            return;
        }

        _cache.Remove(asset.Key);
        asset.Free();
        Raise("released", asset, null);
    }

    /// <summary>Frees every asset. The service is unusable afterwards.</summary>
    public void Dispose()
    {
        foreach (var a in _cache.Values)
        {
            a.Free();
        }

        _cache.Clear();
        Interlocked.CompareExchange(ref s_active, null, this);
    }

    // ---- development reload ----

    /// <summary>
    /// Reads an asset's file again and swaps the result in under the same handle. A file that cannot be read or decoded leaves the old content
    /// in use and is reported as a <c>failed</c> event with <see cref="Asset.Error"/> set. Data assets are staged, not swapped in: see <see cref="ApplyPendingData"/>.
    /// Development tools call this when the file changed; nothing in a running game does.
    /// </summary>
    /// <param name="asset">The asset.</param>
    /// <returns>True when new content was swapped in or staged.</returns>
    public bool Reload(Asset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        if (!asset.CanReload || !asset.IsReady)
        {
            return false;
        }

        Stamp(asset);
        try
        {
            var (prepared, digest) = Prepare(asset);
            if (asset.Deferred)
            {
                asset.Staged = prepared;
                asset.StagedDigest = digest;
                asset.IsPending = true;
                asset.Error = null;
                Raise("pending", asset, "The data file changed. It is not applied while the game runs; call ApplyPendingData between runs.");
                return true;
            }

            asset.Install(prepared);
            asset.Digest = digest;
            asset.Version++;
            asset.Error = null;
            Raise("reloaded", asset, null);
            return true;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            asset.Error = ex.Message;
            Raise("failed", asset, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Swaps in the data files that changed on disk and were staged by <see cref="Reload"/>. Call it at a boundary: before a new game or a replay starts,
    /// never while a recording is running, because a recording made with other data would not replay.
    /// </summary>
    /// <returns>How many data assets changed.</returns>
    public int ApplyPendingData()
    {
        var n = 0;
        foreach (var a in _cache.Values.Where(a => a.IsPending).ToArray())
        {
            a.Install(a.Staged!);
            a.Staged = null;
            a.IsPending = false;
            a.Digest = a.StagedDigest ?? a.Digest;
            a.Version++;
            Raise("applied", a, null);
            n++;
        }

        return n;
    }

    // ---- internals ----

    internal T Resolve<T>(Asset<T> asset)
        where T : class
    {
        if (asset.Background is not null)
        {
            Finish(asset);
        }

        if (asset.Current is null && asset.Staged is { } staged && !asset.Deferred)
        {
            asset.Install(staged);
        }

        return asset.Current
            ?? (asset.Error is { } e ? throw new AssetException("bad_format", asset.Path, e) : throw new AssetException("no_renderer", asset.Path, "The texture is decoded but there is no renderer to upload it with yet. Read Value from a draw callback, or call Assets.AttachRenderer."));
    }

    private Asset<T> Load<T>(AssetKind kind, string path, string variant, bool background, long maxBytes, bool canReload, bool deferred,
        Func<byte[], object> decode, Func<object, T?, T?> apply, Action<T>? free)
        where T : class
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        var key = $"{kind}|{path}|{variant}";
        if (_cache.TryGetValue(key, out var existing))
        {
            existing.References++;
            return (Asset<T>)existing;
        }

        var asset = new Asset<T>(this, key, kind, path) { MaxBytes = maxBytes, Deferred = deferred, DecodeFn = decode, ApplyFn = apply, FreeFn = free, CanReload = canReload };
        asset.References = 1;
        Stamp(asset);
        if (background)
        {
            _cache[key] = asset;
            asset.Background = Task.Run(() => Prepare(asset));
            return asset;
        }

        // A foreground failure throws before anything is cached.
        var (prepared, digest) = Prepare(asset);
        asset.Digest = digest;
        _cache[key] = asset;
        try
        {
            asset.Install(prepared);
        }
        catch (Exception ex)
        {
            _cache.Remove(key);
            throw Wrap(path, ex);
        }

        asset.Version = 1;
        asset.IsReady = true;
        asset.Done.TrySetResult();
        Raise("loaded", asset, null);
        return asset;
    }

    private void Finish(Asset asset)
    {
        var task = asset.Background!;
        asset.Background = null;
        try
        {
            var (prepared, digest) = task.GetAwaiter().GetResult();
            asset.Install(prepared);
            asset.Digest = digest;
            asset.Version = 1;
            asset.IsReady = true;
            CompleteReady(asset, null);
            Raise("loaded", asset, null);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            var wrapped = Wrap(asset.Path, ex);
            asset.Error = wrapped.Message;
            CompleteReady(asset, wrapped);
            Raise("failed", asset, wrapped.Message);
        }
    }

    private static void CompleteReady(Asset asset, Exception? error)
    {
        // The generic handle owns the completion source; reach it through the abstract Ready task's source.
        asset.Complete(error);
    }

    private (object Prepared, string Digest) Prepare(Asset asset)
    {
        try
        {
            var bytes = Source.Read(asset.Path, asset.MaxBytes);
            return (asset.Decode(bytes), Fnv.Hex(Fnv.Add(Fnv.Start, bytes)));
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            throw Wrap(asset.Path, ex);
        }
    }

    private void Stamp(Asset asset)
    {
        if (Source.PhysicalPath(asset.Path) is { } full && new FileInfo(full) is { Exists: true } info)
        {
            asset.FilePath = full;
            asset.StampUtc = info.LastWriteTimeUtc;
            asset.StampLength = info.Length;
        }
    }

    private void Raise(string type, Asset asset, string? message)
    {
        var e = new AssetEvent(++_sequence, type, asset.Path, asset.Kind, asset.Version, message);
        _events.Add(e);
        if (_events.Count > MaxEvents)
        {
            _events.RemoveAt(0);
        }

        EventRaised?.Invoke(e);
    }

    private static AssetException Wrap(string path, Exception ex) =>
        ex as AssetException ?? new AssetException("bad_format", path, ex.Message, ex);

    private Func<object, Texture2D?, Texture2D?> UploadTexture(TextureFilter filter) => (prepared, current) =>
    {
        var px = (Pixels)prepared;
        if (_renderer is not { } r)
        {
            return null;
        }

        if (current is null)
        {
            return r.CreateTexture(px.Width, px.Height, px.Rgba, filter);
        }

        r.ReplaceTexture(current, px.Width, px.Height, px.Rgba, filter);
        return current;
    };

    private static object ParseYaml<T>(string path, byte[] bytes, YamlTypeInfo<T> typeInfo)
        where T : class
    {
        try
        {
            return YamlSerializer.Deserialize(bytes, typeInfo) ?? throw new AssetException("bad_format", path, "The file is empty; expected a mapping for " + typeof(T).Name + ".");
        }
        catch (YamlException ex)
        {
            throw new AssetException("bad_format", path, ex.Message, ex);
        }
    }

    private static Pixels DecodeImage(string path, byte[] bytes, bool svg, int width, int height)
    {
        var options = new LoadOptions
        {
            Configuration = svg ? SvgConfig : ImageConfiguration.Default,

            // The same budgets the art atlas applies to hostile files.
            Limits = new ImageLimits { MaxDimension = svg ? 16384 : 8192, MaxPixelsPerFrame = svg ? 16384L * 16384 : 4096L * 4096, MaxFrames = 256, MaxTotalPixels = 4096L * 4096 * 4 },
            AutoOrient = true,
        };
        var image = Image.Load(bytes, options);
        if (svg)
        {
            // An SVG is drawn at the target size, not scaled up from its natural size.
            image = image.Resize(width, height, Fit.Stretch);
        }

        var px = image.ToArray<Rgba32>();
        return new Pixels(image.Width, image.Height, MemoryMarshal.AsBytes(px.AsSpan()).ToArray());
    }
}

/// <summary>FNV-1a, 64 bit: a cheap label for "which bytes", not a security hash.</summary>
internal static class Fnv
{
    internal const ulong Start = 14695981039346656037UL;

    internal static ulong Add(ulong h, ReadOnlySpan<byte> data)
    {
        foreach (var b in data)
        {
            h = (h ^ b) * 1099511628211UL;
        }

        return h;
    }

    internal static string Hex(ulong h) => h.ToString("x16", CultureInfo.InvariantCulture);
}
