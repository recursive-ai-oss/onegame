using System.Text;
using OneGame.Steam.Native;
using OneGame.Stores;

namespace OneGame.Steam;

/// <summary>Settings for connecting to Steam.</summary>
public sealed class SteamOptions
{
    /// <summary>The game's Steam app id. Needed for <see cref="RelaunchThroughSteam"/>; otherwise Steam learns it from how the game
    /// was launched, or in development from a <c>steam_appid.txt</c> file next to the executable (see the interface notes).</summary>
    public uint AppId { get; init; }

    /// <summary>When true and the game was started outside Steam, ask Steam to start it again through the client instead of running
    /// without a connection. <see cref="IStorePlatform.RelaunchRequested"/> is then true and the game should exit at once. A
    /// <c>steam_appid.txt</c> file next to the executable turns this off, so development builds keep running.</summary>
    public bool RelaunchThroughSteam { get; init; }
}

/// <summary>Entry point: connects to Steam, or falls back to a store that does nothing.</summary>
public static class SteamPlatform
{
    /// <summary>Connects to Steam. Never throws for an absent Steam: when the library or client is missing, or Steam refuses the app,
    /// the result is a <see cref="NullStorePlatform"/> whose <see cref="IStorePlatform.UnavailableReason"/> says why.</summary>
    /// <param name="options">Connection settings, or null for the defaults.</param>
    public static IStorePlatform Start(SteamOptions? options = null)
    {
        options ??= new SteamOptions();
        if (!SteamLibrary.TryLoad(out _, out var failure))
        {
            return new NullStorePlatform(failure ?? "The Steam library could not be loaded.");
        }

        try
        {
            return Start(new SteamNativeApi(), options);
        }
        catch (EntryPointNotFoundException e)
        {
            return new NullStorePlatform($"The Steam library is too old or too new for this game ({e.Message}).");
        }
        catch (BadImageFormatException e)
        {
            return new NullStorePlatform($"The Steam library does not match this process ({e.Message}).");
        }
    }

    internal static IStorePlatform Start(ISteamNative native, SteamOptions options)
    {
        if (options.RelaunchThroughSteam && options.AppId != 0 && native.RestartAppIfNecessary(options.AppId))
        {
            return new NullStorePlatform("The game was started outside Steam and has asked Steam to start it again.", relaunchRequested: true);
        }

        var status = native.Init(out var message);
        if (status != SteamInitStatus.Ok)
        {
            return new NullStorePlatform($"Steam did not start ({status}): {message}");
        }

        return new SteamStorePlatform(native, SteamLayout.Current);
    }
}

/// <summary>The connected Steam store. Also exposes the typed callback and call-result channel for Steam features the neutral
/// services do not cover.</summary>
public sealed class SteamStorePlatform : IStorePlatform
{
    private readonly ISteamNative _native;
    private readonly Services _services;
    private bool _disposed;

    internal SteamStorePlatform(ISteamNative native, SteamPacking packing)
    {
        _native = native;
        Callbacks = new SteamCallbacks(native, packing);
        _services = new Services(this);
    }

    /// <summary>Typed Steam callbacks and call results, delivered from <see cref="Pump"/>.</summary>
    public SteamCallbacks Callbacks { get; }

    /// <summary>Asks Steam how many players the game has now. The handler runs from <see cref="Pump"/>.</summary>
    public void RequestNumberOfCurrentPlayers(Action<NumberOfCurrentPlayers, bool> handler)
    {
        if (_disposed)
        {
            handler(default, true);
            return;
        }

        Callbacks.Track(_native.RequestNumberOfCurrentPlayers(), handler);
    }

    /// <inheritdoc />
    public string Name => "steam";

    /// <inheritdoc />
    public bool IsAvailable => !_disposed;

    /// <inheritdoc />
    public string? UnavailableReason => _disposed ? "Steam has been shut down." : null;

    /// <inheritdoc />
    public bool RelaunchRequested => false;

    /// <inheritdoc />
    public void Pump()
    {
        if (!_disposed)
        {
            Callbacks.Pump();
        }
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
        if (!_disposed)
        {
            _disposed = true;
            _native.Shutdown();
        }
    }

    private sealed class Services : IStoreIdentity, IAchievements, IStats, ICloudSaves, IRichPresence, IOverlay, IOwnership, IStoreEnvironment
    {
        // Documented limits: stat and achievement names 128 bytes, cloud paths 260, rich presence keys 64 and values 256.
        private const int MaxNameBytes = 128;
        private const int MaxCloudNameBytes = 260;
        private const int MaxPresenceKeyBytes = 64;
        private const int MaxPresenceValueBytes = 256;

        private readonly SteamStorePlatform _owner;
        private bool _overlayActive;

        internal Services(SteamStorePlatform owner)
        {
            _owner = owner;
            owner.Callbacks.Subscribe<UserAchievementStored>(e =>
            {
                // Both progress numbers zero means the achievement is fully unlocked (ISteamUserStats docs).
                if (e.CurrentProgress == 0 && e.MaxProgress == 0)
                {
                    Stored?.Invoke(e.Name);
                }
            });
            owner.Callbacks.Subscribe<UserStatsStored>(e => Committed?.Invoke(e.Succeeded));
            owner.Callbacks.Subscribe<GameOverlayActivated>(e =>
            {
                _overlayActive = e.Active;
                ActiveChanged?.Invoke(e.Active);
            });
        }

        private ISteamNative N => _owner._native;

        private bool Down => _owner._disposed;

        // Identity
        public ulong UserId => Down ? 0 : N.GetSteamId();

        public string DisplayName => Down ? string.Empty : N.GetPersonaName();

        // Achievements
        public event Action<string>? Stored;

        public StoreResult Unlock(string id)
        {
            if (Down)
            {
                return StoreResult.Unavailable;
            }

            if (!ValidName(id, MaxNameBytes))
            {
                return StoreResult.InvalidArgument;
            }

            if (N.GetAchievement(id, out var already) && already)
            {
                return StoreResult.Ok;
            }

            if (!N.SetAchievement(id))
            {
                return StoreResult.Rejected;
            }

            // Steam shows the unlock pop-up when the change is stored. A failed store is retried by Steam when the game exits.
            N.StoreStats();
            return StoreResult.Ok;
        }

        public StoreResult TryGetUnlocked(string id, out bool unlocked)
        {
            unlocked = false;
            if (Down)
            {
                return StoreResult.Unavailable;
            }

            if (!ValidName(id, MaxNameBytes))
            {
                return StoreResult.InvalidArgument;
            }

            return N.GetAchievement(id, out unlocked) ? StoreResult.Ok : StoreResult.NotFound;
        }

        public StoreResult ShowProgress(string id, uint current, uint max)
        {
            if (Down)
            {
                return StoreResult.Unavailable;
            }

            if (!ValidName(id, MaxNameBytes) || max == 0 || current >= max)
            {
                return StoreResult.InvalidArgument;
            }

            return N.IndicateAchievementProgress(id, current, max) ? StoreResult.Ok : StoreResult.Rejected;
        }

        public StoreResult Clear(string id)
        {
            if (Down)
            {
                return StoreResult.Unavailable;
            }

            if (!ValidName(id, MaxNameBytes))
            {
                return StoreResult.InvalidArgument;
            }

            return N.ClearAchievement(id) ? StoreResult.Ok : StoreResult.Rejected;
        }

        // Stats
        public event Action<bool>? Committed;

        public StoreResult SetInt(string id, int value) =>
            Down ? StoreResult.Unavailable :
            !ValidName(id, MaxNameBytes) ? StoreResult.InvalidArgument :
            N.SetStat(id, value) ? StoreResult.Ok : StoreResult.Rejected;

        public StoreResult SetFloat(string id, float value) =>
            Down ? StoreResult.Unavailable :
            !ValidName(id, MaxNameBytes) ? StoreResult.InvalidArgument :
            N.SetStat(id, value) ? StoreResult.Ok : StoreResult.Rejected;

        public StoreResult TryGetInt(string id, out int value)
        {
            value = 0;
            return Down ? StoreResult.Unavailable :
                !ValidName(id, MaxNameBytes) ? StoreResult.InvalidArgument :
                N.GetStat(id, out value) ? StoreResult.Ok : StoreResult.NotFound;
        }

        public StoreResult TryGetFloat(string id, out float value)
        {
            value = 0;
            return Down ? StoreResult.Unavailable :
                !ValidName(id, MaxNameBytes) ? StoreResult.InvalidArgument :
                N.GetStat(id, out value) ? StoreResult.Ok : StoreResult.NotFound;
        }

        public StoreResult Commit() =>
            Down ? StoreResult.Unavailable : N.StoreStats() ? StoreResult.Ok : StoreResult.Rejected;

        // Cloud
        public bool IsEnabled => !Down && N.CloudEnabledForAccount() && N.CloudEnabledForApp();

        public StoreResult Write(string name, ReadOnlySpan<byte> data)
        {
            if (Down)
            {
                return StoreResult.Unavailable;
            }

            if (!ValidCloudName(name))
            {
                return StoreResult.InvalidArgument;
            }

            if (data.Length > ICloudSaves.MaxFileBytes)
            {
                return StoreResult.TooLarge;
            }

            if (!IsEnabled)
            {
                return StoreResult.Disabled;
            }

            // A false return means a bad stream, an oversize file or an exhausted quota (ISteamRemoteStorage docs).
            return N.CloudWrite(name, data) ? StoreResult.Ok : StoreResult.Failed;
        }

        public StoreResult Read(string name, out byte[] data, int maxBytes = ICloudSaves.MaxFileBytes)
        {
            data = [];
            if (Down)
            {
                return StoreResult.Unavailable;
            }

            if (!ValidCloudName(name) || maxBytes < 0)
            {
                return StoreResult.InvalidArgument;
            }

            if (!N.CloudExists(name))
            {
                return StoreResult.NotFound;
            }

            // The size comes from the store; check it against the caller's limit before allocating anything.
            var size = N.CloudSize(name);
            if (size < 0)
            {
                return StoreResult.Failed;
            }

            if (size > Math.Min(maxBytes, ICloudSaves.MaxFileBytes))
            {
                return StoreResult.TooLarge;
            }

            var buffer = new byte[size];
            var read = N.CloudRead(name, buffer);
            if (read != size)
            {
                return StoreResult.Failed;
            }

            data = buffer;
            return StoreResult.Ok;
        }

        public bool Exists(string name) => !Down && ValidCloudName(name) && N.CloudExists(name);

        public StoreResult Delete(string name) =>
            Down ? StoreResult.Unavailable :
            !ValidCloudName(name) ? StoreResult.InvalidArgument :
            !N.CloudExists(name) ? StoreResult.NotFound :
            N.CloudDelete(name) ? StoreResult.Ok : StoreResult.Failed;

        public IReadOnlyList<CloudFile> List()
        {
            if (Down)
            {
                return [];
            }

            var count = Math.Max(0, N.CloudFileCount());
            var files = new List<CloudFile>(count);
            for (var i = 0; i < count; i++)
            {
                var name = N.CloudFileName(i, out var size);
                if (name.Length > 0)
                {
                    files.Add(new CloudFile(name, size));
                }
            }

            return files;
        }

        public StoreResult TryGetQuota(out CloudQuota quota)
        {
            quota = default;
            if (Down)
            {
                return StoreResult.Unavailable;
            }

            if (!N.CloudQuota(out var total, out var available))
            {
                return StoreResult.Failed;
            }

            quota = new CloudQuota(total, available);
            return StoreResult.Ok;
        }

        // Rich presence
        public StoreResult Update(string key, string value)
        {
            if (Down)
            {
                return StoreResult.Unavailable;
            }

            if (!ValidName(key, MaxPresenceKeyBytes) || value is null || !FitsText(value, MaxPresenceValueBytes))
            {
                return StoreResult.InvalidArgument;
            }

            // An empty value deletes the key.
            return N.SetRichPresence(key, value) ? StoreResult.Ok : StoreResult.Rejected;
        }

        public StoreResult Clear()
        {
            if (Down)
            {
                return StoreResult.Unavailable;
            }

            N.ClearRichPresence();
            return StoreResult.Ok;
        }

        // Overlay
        public event Action<bool>? ActiveChanged;

        public bool IsActive => !Down && _overlayActive;

        bool IOverlay.IsEnabled => !Down && N.IsOverlayEnabled();

        public StoreResult Open(OverlayPage page)
        {
            if (Down)
            {
                return StoreResult.Unavailable;
            }

            // Dialog names from the ISteamFriends reference.
            var dialog = page switch
            {
                OverlayPage.Friends => "friends",
                OverlayPage.Community => "community",
                OverlayPage.Players => "players",
                OverlayPage.Settings => "settings",
                OverlayPage.OfficialGameGroup => "officialgamegroup",
                OverlayPage.Stats => "stats",
                OverlayPage.Achievements => "achievements",
                _ => null,
            };
            if (dialog is null)
            {
                return StoreResult.InvalidArgument;
            }

            N.ActivateOverlay(dialog);
            return StoreResult.Ok;
        }

        public StoreResult OpenWebPage(string url)
        {
            if (Down)
            {
                return StoreResult.Unavailable;
            }

            if (url is null || !(url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                || url.Contains('\0'))
            {
                return StoreResult.InvalidArgument;
            }

            N.ActivateOverlayToWebPage(url);
            return StoreResult.Ok;
        }

        public StoreResult OpenStorePage(uint appId)
        {
            if (Down)
            {
                return StoreResult.Unavailable;
            }

            if (appId == 0)
            {
                return StoreResult.InvalidArgument;
            }

            N.ActivateOverlayToStore(appId);
            return StoreResult.Ok;
        }

        // Ownership
        public uint AppId => Down ? 0 : N.GetAppId();

        public bool OwnsApp => !Down && N.IsSubscribed();

        public bool HasDlc(uint dlcAppId) => !Down && dlcAppId != 0 && N.IsDlcInstalled(dlcAppId);

        public int BuildId => Down ? 0 : N.GetBuildId();

        // Environment
        public string Language
        {
            get
            {
                if (Down)
                {
                    return string.Empty;
                }

                var language = N.GetGameLanguage();
                return language.Length > 0 ? language : N.GetUiLanguage();
            }
        }

        public string LanguageTag => Down ? string.Empty : SteamLanguages.ToTag(Language);

        public bool IsHandheld => !Down && N.IsRunningOnDeck();

        public bool IsCouchMode => !Down && N.IsBigPicture();

        private static bool FitsText(string text, int maxBytes) =>
            text.Length <= maxBytes && Encoding.UTF8.GetByteCount(text) <= maxBytes && !text.Contains('\0');

        private static bool ValidName(string? name, int maxBytes) =>
            !string.IsNullOrEmpty(name) && FitsText(name, maxBytes);

        private static bool ValidCloudName(string? name) =>
            ValidName(name, MaxCloudNameBytes) && name![0] != '/' && !name.Contains('\\') && !name.Split('/').Contains("..");
    }
}

/// <summary>Maps Steam's language names to BCP 47 tags, from the table in Steam's localization documentation.</summary>
internal static class SteamLanguages
{
    internal static string ToTag(string steamName) => steamName switch
    {
        "arabic" => "ar",
        "bulgarian" => "bg",
        "schinese" => "zh-CN",
        "tchinese" => "zh-TW",
        "czech" => "cs",
        "danish" => "da",
        "dutch" => "nl",
        "english" => "en",
        "finnish" => "fi",
        "french" => "fr",
        "german" => "de",
        "greek" => "el",
        "hungarian" => "hu",
        "indonesian" => "id",
        "italian" => "it",
        "japanese" => "ja",
        "koreana" => "ko",
        "malay" => "ms",
        "norwegian" => "no",
        "polish" => "pl",
        "portuguese" => "pt",
        "brazilian" => "pt-BR",
        "romanian" => "ro",
        "russian" => "ru",
        "spanish" => "es",
        "latam" => "es-419",
        "swedish" => "sv",
        "thai" => "th",
        "turkish" => "tr",
        "ukrainian" => "uk",
        "vietnamese" => "vi",
        _ => string.Empty,
    };
}
