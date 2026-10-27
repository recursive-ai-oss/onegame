using OneGame.Host;

namespace OneGame.Control;

/// <summary>One loaded asset in the <c>assets</c> report.</summary>
/// <param name="Path">The path the game asked for.</param>
/// <param name="Kind"><c>texture</c>, <c>svg</c>, <c>data</c>, <c>sound</c>, <c>model</c> or <c>font</c>.</param>
/// <param name="Version">1 after the first load, plus one per applied reload.</param>
/// <param name="References">Loads not yet released.</param>
/// <param name="State"><c>ready</c>, <c>loading</c> (background load), <c>pending</c> (changed data waiting for apply), <c>error</c> (the last reload or load failed; the old content is in use) or <c>failed</c> (never loaded).</param>
/// <param name="Digest">Digest of the file's bytes as last read.</param>
/// <param name="File">The file on disk, when there is one.</param>
/// <param name="Error">The reason of the last failure.</param>
public sealed record AssetInfo(string Path, string Kind, int Version, int References, string State, string Digest, string? File, string? Error);

/// <summary>One asset event in the <c>assets</c> report.</summary>
/// <param name="Sequence">Counts up from 1.</param>
/// <param name="Type"><c>loaded</c>, <c>reloaded</c>, <c>failed</c>, <c>pending</c>, <c>applied</c> or <c>released</c>.</param>
/// <param name="Path">The asset path.</param>
/// <param name="Kind">The asset kind.</param>
/// <param name="Version">The version after the event.</param>
/// <param name="Message">The error or note.</param>
public sealed record AssetEventInfo(long Sequence, string Type, string Path, string Kind, int Version, string? Message);

/// <summary>Result of the <c>assets</c> operation.</summary>
/// <param name="Assets">Every loaded asset.</param>
/// <param name="Events">Events after <c>since</c>, oldest first (the last 256 are kept).</param>
/// <param name="LastSequence">Sequence of the newest event.</param>
/// <param name="DataDigest">Digest of all data files: equal when the game runs with the same data.</param>
/// <param name="Changed">Paths whose file changed and were read again by this call.</param>
/// <param name="Applied">How many pending data assets <c>apply_data</c> swapped in.</param>
public sealed record AssetsResult(AssetInfo[] Assets, AssetEventInfo[] Events, long LastSequence, string DataDigest, string[] Changed, int Applied);

/// <summary>
/// Development-build asset hot reload. It compares each loaded file's write time and length with what was last read and asks
/// <see cref="Assets.Reload"/> for the ones that changed. Exports leave the control layer out, so they carry no watcher.
/// </summary>
public sealed class AssetReloader(Assets assets)
{
    private readonly Assets _assets = assets ?? throw new ArgumentNullException(nameof(assets));
    private long _nextCheck;

    /// <summary>Checks the files at most every 250 ms. Cheap enough for every pass of a game loop.</summary>
    /// <returns>The paths that were read again.</returns>
    public IReadOnlyList<string> Poll()
    {
        var now = Environment.TickCount64;
        if (now < _nextCheck)
        {
            return [];
        }

        _nextCheck = now + 250;
        return Check();
    }

    /// <summary>Checks every loaded file now.</summary>
    /// <returns>The paths that were read again.</returns>
    public IReadOnlyList<string> Check()
    {
        var changed = new List<string>();
        foreach (var a in _assets.Items.ToArray())
        {
            if (a.FilePath is not { } file || !a.CanReload || !a.IsReady)
            {
                continue;
            }

            var info = new FileInfo(file);
            var now = info.Exists ? (info.LastWriteTimeUtc, info.Length) : (DateTime.MinValue, -1L);
            if (now.Item1 == a.StampUtc && now.Item2 == a.StampLength)
            {
                continue;
            }

            // A file that is gone is an error to report; the loaded content stays.
            changed.Add(a.Path);
            _assets.Reload(a);
        }

        return changed;
    }
}

public sealed partial class ControlSurface
{
    private AssetReloader? _assetReloader;

    private AssetReloader AssetWatcher => _assetReloader ??= new AssetReloader(Game.Assets);

    private AssetsResult AssetsOp(Args a)
    {
        var assets = Game.Assets;
        var changed = a.Bool("check", true) ? [.. AssetWatcher.Check()] : Array.Empty<string>();
        if (a.Str("reload") is { } path)
        {
            var hit = assets.Items.Where(x => x.Path == path).ToArray();
            if (hit.Length == 0)
            {
                throw new ControlException("not_found", $"No loaded asset '{path}'. The 'assets' operation lists them.");
            }

            foreach (var x in hit)
            {
                assets.Reload(x);
            }

            changed = [.. changed.Append(path)];
        }

        var applied = a.Bool("apply_data", false) ? assets.ApplyPendingData() : 0;
        var since = (long)a.Double("since", 0);
        var list = assets.Items.OrderBy(x => x.Path, StringComparer.Ordinal).ThenBy(x => x.Kind)
            .Select(x => new AssetInfo(x.Path, Name(x.Kind), x.Version, x.References, StateOf(x), x.Digest, x.FilePath, x.Error)).ToArray();
        var events = assets.Events.Where(e => e.Sequence > since).Select(e => new AssetEventInfo(e.Sequence, e.Type, e.Path, Name(e.Kind), e.Version, e.Message)).ToArray();
        return new AssetsResult(list, events, assets.LastSequence, assets.DataDigest, changed, applied);
    }

    private static string Name(AssetKind k) => k.ToString().ToLowerInvariant();

    private static string StateOf(Asset a) =>
        !a.IsReady ? (a.Error is null ? "loading" : "failed") : a.IsPending ? "pending" : a.Error is null ? "ready" : "error";
}
