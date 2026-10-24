namespace OneGame.Stores;

/// <summary>The store used when none is connected: every service is present, reads return empty values and changes return
/// <see cref="StoreResult.Unavailable"/>. Games run unchanged.</summary>
public sealed class NullStorePlatform : IStorePlatform
{
    private readonly Services _services = new();

    /// <summary>Creates a no-op store.</summary>
    /// <param name="reason">Why there is no store, for logs.</param>
    /// <param name="relaunchRequested">Set when the real store asked the game to exit and start again through its launcher.</param>
    public NullStorePlatform(string reason, bool relaunchRequested = false)
    {
        UnavailableReason = reason;
        RelaunchRequested = relaunchRequested;
    }

    /// <inheritdoc />
    public string Name => "none";

    /// <inheritdoc />
    public bool IsAvailable => false;

    /// <inheritdoc />
    public string? UnavailableReason { get; }

    /// <inheritdoc />
    public bool RelaunchRequested { get; }

    /// <inheritdoc />
    public void Pump()
    {
    }

    /// <inheritdoc />
    public IStoreIdentity Identity => _services;

    /// <inheritdoc />
    public IAchievements Achievements => _services;

    /// <inheritdoc />
    public IStats Stats => _services;

    /// <inheritdoc />
    public ICloudSaves Cloud => _services;

    /// <inheritdoc />
    public IRichPresence Presence => _services;

    /// <inheritdoc />
    public IOverlay Overlay => _services;

    /// <inheritdoc />
    public IOwnership Ownership => _services;

    /// <inheritdoc />
    public IStoreEnvironment Environment => _services;

    /// <inheritdoc />
    public void Dispose()
    {
    }

    private sealed class Services : IStoreIdentity, IAchievements, IStats, ICloudSaves, IRichPresence, IOverlay, IOwnership, IStoreEnvironment
    {
        public ulong UserId => 0;

        public string DisplayName => string.Empty;

        public StoreResult Unlock(string id) => StoreResult.Unavailable;

        public StoreResult TryGetUnlocked(string id, out bool unlocked)
        {
            unlocked = false;
            return StoreResult.Unavailable;
        }

        public StoreResult ShowProgress(string id, uint current, uint max) => StoreResult.Unavailable;

        public StoreResult Clear(string id) => StoreResult.Unavailable;

        public event Action<string>? Stored
        {
            add { }
            remove { }
        }

        public StoreResult SetInt(string id, int value) => StoreResult.Unavailable;

        public StoreResult SetFloat(string id, float value) => StoreResult.Unavailable;

        public StoreResult TryGetInt(string id, out int value)
        {
            value = 0;
            return StoreResult.Unavailable;
        }

        public StoreResult TryGetFloat(string id, out float value)
        {
            value = 0;
            return StoreResult.Unavailable;
        }

        public StoreResult Commit() => StoreResult.Unavailable;

        public event Action<bool>? Committed
        {
            add { }
            remove { }
        }

        public bool IsEnabled => false;

        public StoreResult Write(string name, ReadOnlySpan<byte> data) => StoreResult.Unavailable;

        public StoreResult Read(string name, out byte[] data, int maxBytes = ICloudSaves.MaxFileBytes)
        {
            data = [];
            return StoreResult.Unavailable;
        }

        public bool Exists(string name) => false;

        public StoreResult Delete(string name) => StoreResult.Unavailable;

        public IReadOnlyList<CloudFile> List() => [];

        public StoreResult TryGetQuota(out CloudQuota quota)
        {
            quota = default;
            return StoreResult.Unavailable;
        }

        public StoreResult Update(string key, string value) => StoreResult.Unavailable;

        public StoreResult Clear() => StoreResult.Unavailable;

        public bool IsActive => false;

        public event Action<bool>? ActiveChanged
        {
            add { }
            remove { }
        }

        public StoreResult Open(OverlayPage page) => StoreResult.Unavailable;

        public StoreResult OpenWebPage(string url) => StoreResult.Unavailable;

        public StoreResult OpenStorePage(uint appId) => StoreResult.Unavailable;

        public uint AppId => 0;

        public bool OwnsApp => false;

        public bool HasDlc(uint dlcAppId) => false;

        public int BuildId => 0;

        public string Language => string.Empty;

        public string LanguageTag => string.Empty;

        public bool IsHandheld => false;

        public bool IsCouchMode => false;
    }
}
