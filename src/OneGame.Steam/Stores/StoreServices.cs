namespace OneGame.Stores;

/// <summary>Outcome of a store operation that changes something. Failures are values, not exceptions, because a missing or
/// offline store is a normal state for a shipped game.</summary>
public enum StoreResult
{
    /// <summary>The store accepted the request. Some stores confirm asynchronously; see the service for details.</summary>
    Ok,

    /// <summary>No store is connected (not launched through the store, client not running, library missing).</summary>
    Unavailable,

    /// <summary>The store rejected the request, for example an unknown achievement or stat name, or a type mismatch.</summary>
    Rejected,

    /// <summary>An argument was empty, too long or otherwise unusable. Nothing was sent.</summary>
    InvalidArgument,

    /// <summary>The named item does not exist.</summary>
    NotFound,

    /// <summary>The data is larger than the store allows. Nothing was sent.</summary>
    TooLarge,

    /// <summary>The player turned the feature off (for example cloud saves in the store settings).</summary>
    Disabled,

    /// <summary>The store reported a failure it gave no further reason for, such as an exceeded quota.</summary>
    Failed,
}

/// <summary>A connected store (Steam today; others later) and the services it provides. Every property is non-null: when no store
/// is connected the services are no-ops that return <see cref="StoreResult.Unavailable"/>, so game code never branches on the store.</summary>
public interface IStorePlatform : IDisposable
{
    /// <summary>Short store name, such as <c>steam</c>, or <c>none</c> when no store is connected.</summary>
    string Name { get; }

    /// <summary>True when a store is connected and the services talk to it.</summary>
    bool IsAvailable { get; }

    /// <summary>Why the store is not connected, in plain words for logs. Null when <see cref="IsAvailable"/>.</summary>
    string? UnavailableReason { get; }

    /// <summary>True when the store asked for the game to be started again through the store's own launcher. The game should exit
    /// at once, before opening a window.</summary>
    bool RelaunchRequested { get; }

    /// <summary>Delivers store events and async results. Call once per frame from the game loop on the same thread every time.
    /// No threads are started on the game's behalf. Cheap when nothing is pending.</summary>
    void Pump();

    /// <summary>The signed-in player.</summary>
    IStoreIdentity Identity { get; }

    /// <summary>Achievements.</summary>
    IAchievements Achievements { get; }

    /// <summary>Numeric stats.</summary>
    IStats Stats { get; }

    /// <summary>Cloud-synced save files.</summary>
    ICloudSaves Cloud { get; }

    /// <summary>What the player is doing, shown to friends.</summary>
    IRichPresence Presence { get; }

    /// <summary>The store's in-game overlay.</summary>
    IOverlay Overlay { get; }

    /// <summary>Which app and DLC the player owns.</summary>
    IOwnership Ownership { get; }

    /// <summary>Language and device facts the store knows.</summary>
    IStoreEnvironment Environment { get; }
}

/// <summary>The signed-in player.</summary>
public interface IStoreIdentity
{
    /// <summary>Store-specific account id; 0 when unknown. For Steam this is the 64-bit SteamID.</summary>
    ulong UserId { get; }

    /// <summary>The name the player goes by in the store. Empty when unknown.</summary>
    string DisplayName { get; }
}

/// <summary>Achievements, named by the id configured in the store's partner site.</summary>
public interface IAchievements
{
    /// <summary>Unlocks an achievement and asks the store to save it. Idempotent.</summary>
    StoreResult Unlock(string id);

    /// <summary>Reads whether an achievement is unlocked.</summary>
    StoreResult TryGetUnlocked(string id, out bool unlocked);

    /// <summary>Shows "n of max" progress toward an achievement. Progress itself is kept in a stat the game sets.</summary>
    StoreResult ShowProgress(string id, uint current, uint max);

    /// <summary>Relocks an achievement. For development and tests.</summary>
    StoreResult Clear(string id);

    /// <summary>Raised from <see cref="IStorePlatform.Pump"/> when the store confirms an achievement was saved.</summary>
    event Action<string>? Stored;
}

/// <summary>Numeric stats, named by the id configured in the store's partner site. Setting only changes the local copy until
/// <see cref="Commit"/>.</summary>
public interface IStats
{
    /// <summary>Sets an integer stat.</summary>
    StoreResult SetInt(string id, int value);

    /// <summary>Sets a floating-point stat.</summary>
    StoreResult SetFloat(string id, float value);

    /// <summary>Reads an integer stat.</summary>
    StoreResult TryGetInt(string id, out int value);

    /// <summary>Reads a floating-point stat.</summary>
    StoreResult TryGetFloat(string id, out float value);

    /// <summary>Sends changed stats and achievements to the store. The store rate-limits this; call at natural breaks (end of a
    /// level, return to the menu), not every frame.</summary>
    StoreResult Commit();

    /// <summary>Raised from <see cref="IStorePlatform.Pump"/> with whether the store stored the stats.</summary>
    event Action<bool>? Committed;
}

/// <summary>Cloud-synced save files. Names are relative paths with forward slashes, at most 260 bytes.</summary>
public interface ICloudSaves
{
    /// <summary>Largest file the store accepts, in bytes.</summary>
    const int MaxFileBytes = 100 * 1024 * 1024;

    /// <summary>True when the player has cloud saves turned on for the account and for this game.</summary>
    bool IsEnabled { get; }

    /// <summary>Writes a file, replacing any existing one.</summary>
    StoreResult Write(string name, ReadOnlySpan<byte> data);

    /// <summary>Reads a whole file. The size is checked against <paramref name="maxBytes"/> before any buffer is allocated.</summary>
    StoreResult Read(string name, out byte[] data, int maxBytes = MaxFileBytes);

    /// <summary>Tests whether a file exists.</summary>
    bool Exists(string name);

    /// <summary>Deletes a file from the cloud and the local copy.</summary>
    StoreResult Delete(string name);

    /// <summary>Lists the files the store tracks for this game.</summary>
    IReadOnlyList<CloudFile> List();

    /// <summary>Reads the player's cloud quota.</summary>
    StoreResult TryGetQuota(out CloudQuota quota);
}

/// <summary>One cloud file.</summary>
/// <param name="Name">Relative path.</param>
/// <param name="Size">Size in bytes.</param>
public readonly record struct CloudFile(string Name, int Size);

/// <summary>Cloud storage quota in bytes.</summary>
/// <param name="TotalBytes">All the space the player has for this game.</param>
/// <param name="AvailableBytes">The space still free.</param>
public readonly record struct CloudQuota(ulong TotalBytes, ulong AvailableBytes);

/// <summary>Rich presence: key/value text friends see next to the player's name.</summary>
public interface IRichPresence
{
    /// <summary>Sets a key. The key <c>status</c> is shown as the player's status line. An empty value removes the key.</summary>
    StoreResult Update(string key, string value);

    /// <summary>Removes every key this game set.</summary>
    StoreResult Clear();
}

/// <summary>Which overlay page to open.</summary>
public enum OverlayPage
{
    /// <summary>Friends list.</summary>
    Friends,

    /// <summary>Community hub of the game.</summary>
    Community,

    /// <summary>The player's own profile.</summary>
    Players,

    /// <summary>Settings.</summary>
    Settings,

    /// <summary>Official game group.</summary>
    OfficialGameGroup,

    /// <summary>Stats page.</summary>
    Stats,

    /// <summary>Achievements page.</summary>
    Achievements,
}

/// <summary>The store's in-game overlay. Games usually pause when it opens.</summary>
public interface IOverlay
{
    /// <summary>True when the overlay can be shown (the player has it enabled and it hooked the renderer).</summary>
    bool IsEnabled { get; }

    /// <summary>True while the overlay is on screen. Updated from <see cref="IStorePlatform.Pump"/>.</summary>
    bool IsActive { get; }

    /// <summary>Raised from <see cref="IStorePlatform.Pump"/> when the overlay opens (true) or closes (false).</summary>
    event Action<bool>? ActiveChanged;

    /// <summary>Opens an overlay page.</summary>
    StoreResult Open(OverlayPage page);

    /// <summary>Opens a web page in the overlay browser. Only http and https URLs are accepted.</summary>
    StoreResult OpenWebPage(string url);

    /// <summary>Opens a store page for an app in the overlay.</summary>
    StoreResult OpenStorePage(uint appId);
}

/// <summary>Ownership of the app and its DLC.</summary>
public interface IOwnership
{
    /// <summary>The store's id for this app; 0 when unknown.</summary>
    uint AppId { get; }

    /// <summary>True when the signed-in player owns this app (or is allowed to play it through family sharing or a trial).</summary>
    bool OwnsApp { get; }

    /// <summary>True when the player owns a DLC and has it installed.</summary>
    bool HasDlc(uint dlcAppId);

    /// <summary>The store's build number of the installed version; 0 when unknown.</summary>
    int BuildId { get; }
}

/// <summary>Language and device facts reported by the store.</summary>
public interface IStoreEnvironment
{
    /// <summary>The language the player chose for this game, as the store names it (for Steam, <c>english</c>, <c>german</c>,
    /// <c>schinese</c>…). Empty when unknown.</summary>
    string Language { get; }

    /// <summary>The same language as a BCP 47 tag (<c>en</c>, <c>de</c>, <c>zh-CN</c>, <c>pt-BR</c>…), using the codes in Steam's language table, or empty when unknown or unmapped.</summary>
    string LanguageTag { get; }

    /// <summary>True on a handheld the store knows about (Steam Deck): use controller-first layouts and larger text.</summary>
    bool IsHandheld { get; }

    /// <summary>True when the store's couch-mode interface (Big Picture) is showing.</summary>
    bool IsCouchMode { get; }
}
