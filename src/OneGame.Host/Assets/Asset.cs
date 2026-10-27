using System.Threading.Tasks;

namespace OneGame.Host;

/// <summary>What kind of content an <see cref="Asset"/> holds.</summary>
public enum AssetKind
{
    /// <summary>A raster image (PNG, JPEG, WebP and the other formats OneImage reads) as a 2D texture.</summary>
    Texture,

    /// <summary>An SVG rasterized at a chosen size as a 2D texture.</summary>
    Svg,

    /// <summary>A YAML file read into one of the game's own types. Sim code may read it, so a reload is held back (see <see cref="Asset.IsPending"/>).</summary>
    Data,

    /// <summary>An audio clip.</summary>
    Sound,

    /// <summary>A glTF model.</summary>
    Model,

    /// <summary>A font file added to a <see cref="OneGame.Text.FontLibrary"/>. Fonts are not hot reloaded.</summary>
    Font,
}

/// <summary>
/// A loaded file. Handles are shared: loading the same path twice returns the same handle and counts one more reference, and
/// <see cref="Release"/> gives it back. A development reload swaps the content under the handle, so hold the handle (or, for
/// textures, the texture itself, which is updated in place) and read <see cref="Asset{T}.Value"/> when you use it.
/// </summary>
public abstract class Asset
{
    private protected Asset(Assets owner, string key, AssetKind kind, string path)
    {
        Owner = owner;
        Key = key;
        Kind = kind;
        Path = path;
    }

    internal Assets Owner { get; }

    internal string Key { get; }

    /// <summary>The path the game asked for, relative to the content folders.</summary>
    public string Path { get; }

    /// <summary>What the asset holds.</summary>
    public AssetKind Kind { get; }

    /// <summary>1 after the first load, plus one for every reload that was applied.</summary>
    public int Version { get; internal set; }

    /// <summary>How many loads have not been released yet.</summary>
    public int References { get; internal set; }

    /// <summary>True once the content is decoded and in use. A background load is not ready until <see cref="Assets.Update"/> or a read of <see cref="Asset{T}.Value"/> finishes it.</summary>
    public bool IsReady { get; internal set; }

    /// <summary>The reason the last load or reload failed, or null. After a failed reload the previous content is still in use.</summary>
    public string? Error { get; internal set; }

    /// <summary>True when a changed data file was read and checked but not swapped in; <see cref="Assets.ApplyPendingData"/> applies it.</summary>
    public bool IsPending { get; internal set; }

    /// <summary>A 64-bit FNV-1a digest of the file's bytes in hex, so a replay or a test can tell which data it ran with.</summary>
    public string Digest { get; internal set; } = "";

    /// <summary>The file on disk, or null when the content does not come from a file. Development reloads watch it.</summary>
    public string? FilePath { get; internal set; }

    /// <summary>The write time of <see cref="FilePath"/> when it was last read.</summary>
    public DateTime StampUtc { get; internal set; }

    /// <summary>The length of <see cref="FilePath"/> when it was last read.</summary>
    public long StampLength { get; internal set; }

    /// <summary>True when an edit to the file can be swapped in while the game runs.</summary>
    public bool CanReload { get; internal set; }

    /// <summary>Completes when the content is ready, or faults with an <see cref="AssetException"/> when the load failed.</summary>
    public Task Ready => ReadyCore;

    internal abstract Task ReadyCore { get; }

    internal long MaxBytes { get; init; }

    /// <summary>Sim-read data: a reload is staged, not swapped in.</summary>
    internal bool Deferred { get; init; }

    /// <summary>A decoded reload or first upload that is waiting (pending data, or a texture with no renderer yet).</summary>
    internal object? Staged { get; set; }

    /// <summary>A background decode that has not been installed yet.</summary>
    internal Task<(object Prepared, string Digest)>? Background { get; set; }

    internal string? StagedDigest { get; set; }

    internal abstract void Complete(Exception? error);

    internal abstract object Decode(byte[] bytes);

    internal abstract void Install(object prepared);

    internal abstract void Free();

    /// <summary>Gives one reference back. At zero the asset leaves the cache and its GPU memory is freed; do not use it afterwards.</summary>
    public void Release() => Owner.Release(this);
}

/// <summary>A loaded file of type <typeparamref name="T"/>.</summary>
/// <typeparam name="T">The loaded type: <see cref="OneGame.Render2D.Texture2D"/>, a data type, <see cref="OneGame.Audio.Sound"/>, <see cref="OneGame.Gltf.GltfModel"/> or <see cref="LoadedFont"/>.</typeparam>
public sealed class Asset<T> : Asset
    where T : class
{
    internal Asset(Assets owner, string key, AssetKind kind, string path)
        : base(owner, key, kind, path)
    {
    }

    internal T? Current { get; set; }

    internal Func<byte[], object> DecodeFn { get; init; } = null!;

    /// <summary>Applies decoded content: returns the value to hold (updating the current one in place where it can), or null when it cannot be used yet.</summary>
    internal Func<object, T?, T?> ApplyFn { get; init; } = null!;

    internal Action<T>? FreeFn { get; init; }

    internal override object Decode(byte[] bytes) => DecodeFn(bytes);

    internal override void Complete(Exception? error)
    {
        if (error is null)
        {
            Done.TrySetResult();
        }
        else
        {
            Done.TrySetException(error);
        }
    }

    internal override void Install(object prepared)
    {
        var value = ApplyFn(prepared, Current);
        if (value is null)
        {
            Staged = prepared;
            return;
        }

        Current = value;
        Staged = null;
    }

    internal override void Free()
    {
        if (Current is not null)
        {
            FreeFn?.Invoke(Current);
        }

        Current = null;
    }

    internal TaskCompletionSource Done { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal override Task ReadyCore => Done.Task;

    /// <summary>The content now. Finishes a background load first (waiting for the decode). Textures are updated in place, so the same object keeps being returned after a reload.</summary>
    /// <exception cref="AssetException">The load failed, or the content needs a device that is not attached yet.</exception>
    public T Value => Owner.Resolve(this);
}

/// <summary>A font added to a library by <see cref="Assets.LoadFont"/>.</summary>
/// <param name="Library">The library the face lives in.</param>
/// <param name="FaceId">The face id to use in glyph keys and font stacks.</param>
public sealed record LoadedFont(OneGame.Text.FontLibrary Library, int FaceId);

/// <summary>One thing that happened to an asset, for the control layer and tests.</summary>
/// <param name="Sequence">Counts up from 1.</param>
/// <param name="Type"><c>loaded</c>, <c>reloaded</c>, <c>failed</c> (a load or a reload; the old content stays), <c>pending</c> (a data file changed and waits to be applied), <c>applied</c> (pending data swapped in) or <c>released</c>.</param>
/// <param name="Path">The asset's path.</param>
/// <param name="Kind">The asset's kind.</param>
/// <param name="Version">The asset's version after the event.</param>
/// <param name="Message">The error for <c>failed</c>, or a note.</param>
public sealed record AssetEvent(long Sequence, string Type, string Path, AssetKind Kind, int Version, string? Message);

/// <summary>A file could not be loaded or reloaded.</summary>
public sealed class AssetException : Exception
{
    /// <summary>Creates the exception.</summary>
    /// <param name="code"><c>not_found</c>, <c>bad_path</c>, <c>too_large</c>, <c>bad_format</c>, <c>no_renderer</c> or <c>no_audio</c>.</param>
    /// <param name="path">The asset path.</param>
    /// <param name="message">What is wrong, with the fix where there is one.</param>
    /// <param name="inner">The decoder's exception, if any.</param>
    public AssetException(string code, string path, string message, Exception? inner = null)
        : base($"{path}: {message}", inner)
    {
        Code = code;
        AssetPath = path;
    }

    /// <summary>The short reason code.</summary>
    public string Code { get; }

    /// <summary>The asset path.</summary>
    public string AssetPath { get; }
}
