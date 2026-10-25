namespace OneGame.Steam.Native;

/// <summary>Result of the library's init call. The numbers are the documented values of the init result enum.</summary>
public enum SteamInitStatus
{
    /// <summary>Initialized.</summary>
    Ok = 0,

    /// <summary>Something went wrong; the message says what.</summary>
    FailedGeneric = 1,

    /// <summary>The Steam client is not running or its pipe could not be opened.</summary>
    NoSteamClient = 2,

    /// <summary>The Steam client is older than the library the game ships with.</summary>
    VersionMismatch = 3,
}

/// <summary>The thin boundary between the managed services and the native library: one method per native call, in managed types.
/// The real implementation calls the flat C API; tests supply a fake. Strings are UTF-8 on the wire; a null return means empty.</summary>
internal interface ISteamNative
{
    // Lifecycle
    bool RestartAppIfNecessary(uint appId);
    SteamInitStatus Init(out string message);
    void Shutdown();

    // Manual callback dispatch. The span from TryGetNextCallback is valid until FreeLastCallback.
    int Pipe { get; }
    void RunFrame();
    bool TryGetNextCallback(out int callbackId, out ReadOnlySpan<byte> data);
    void FreeLastCallback();
    bool GetCallResult(ulong call, Span<byte> buffer, int expectedCallbackId, out bool failed);

    // User and friends
    ulong GetSteamId();
    string GetPersonaName();
    bool SetRichPresence(string key, string value);
    void ClearRichPresence();
    void ActivateOverlay(string dialog);
    void ActivateOverlayToWebPage(string url);
    void ActivateOverlayToStore(uint appId);

    // Utils and apps
    bool IsOverlayEnabled();
    bool IsRunningOnDeck();
    bool IsBigPicture();
    uint GetAppId();
    string GetUiLanguage();
    bool IsSubscribed();
    bool IsDlcInstalled(uint appId);
    string GetGameLanguage();
    int GetBuildId();

    // Stats and achievements
    bool SetAchievement(string name);
    bool ClearAchievement(string name);
    bool GetAchievement(string name, out bool unlocked);
    bool IndicateAchievementProgress(string name, uint current, uint max);
    bool StoreStats();
    bool SetStat(string name, int value);
    bool SetStat(string name, float value);
    bool GetStat(string name, out int value);
    bool GetStat(string name, out float value);
    ulong RequestNumberOfCurrentPlayers();

    // Cloud
    bool CloudWrite(string name, ReadOnlySpan<byte> data);
    int CloudRead(string name, Span<byte> buffer);
    bool CloudExists(string name);
    bool CloudDelete(string name);
    int CloudSize(string name);
    int CloudFileCount();
    string CloudFileName(int index, out int size);
    bool CloudQuota(out ulong total, out ulong available);
    bool CloudEnabledForAccount();
    bool CloudEnabledForApp();

    // Steam Input; null when the library does not hand out the interface.
    ISteamInputNative? CreateInput() => null;
}
