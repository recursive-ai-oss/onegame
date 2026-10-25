using System.Runtime.InteropServices;

namespace OneGame.Steam.Native;

/// <summary>The real native boundary: forwards each call to the Steam library's flat C API. Interface pointers are fetched once, after
/// a successful init. Create it only after <see cref="SteamLibrary.TryLoad"/> succeeded.</summary>
internal sealed unsafe class SteamNativeApi : ISteamNative
{
    private const int ErrorMessageBytes = 1024; // size of the buffer the init call fills in, per the API docs

    private nint _user;
    private nint _stats;
    private nint _friends;
    private nint _utils;
    private nint _apps;
    private nint _storage;
    private bool _lastCallbackHeld;

    public int Pipe { get; private set; }

    public bool RestartAppIfNecessary(uint appId) => SteamApi.RestartAppIfNecessary(appId) != 0;

    public SteamInitStatus Init(out string message)
    {
        var buffer = stackalloc byte[ErrorMessageBytes];
        buffer[0] = 0;
        var result = SteamApi.InitFlat(buffer);
        buffer[ErrorMessageBytes - 1] = 0;
        message = Marshal.PtrToStringUTF8((nint)buffer) ?? string.Empty;
        if (result != 0)
        {
            return result is >= 1 and <= 3 ? (SteamInitStatus)result : SteamInitStatus.FailedGeneric;
        }

        _user = SteamApi.GetUser();
        _stats = SteamApi.GetUserStats();
        _friends = SteamApi.GetFriends();
        _utils = SteamApi.GetUtils();
        _apps = SteamApi.GetApps();
        _storage = SteamApi.GetRemoteStorage();
        if (_user == 0 || _stats == 0 || _friends == 0 || _utils == 0 || _apps == 0 || _storage == 0)
        {
            SteamApi.Shutdown();
            message = "Steam initialized but did not hand out every interface this version of the game needs.";
            return SteamInitStatus.VersionMismatch;
        }

        // Callbacks are delivered by the game loop calling RunFrame; the library must not dispatch them itself.
        SteamApi.ManualDispatchInit();
        Pipe = SteamApi.GetHSteamPipe();
        return SteamInitStatus.Ok;
    }

    public void Shutdown() => SteamApi.Shutdown();

    public ISteamInputNative? CreateInput()
    {
        var input = SteamApi.GetInput();
        return input == 0 ? null : new SteamInputNative(input, _utils);
    }

    public void RunFrame() => SteamApi.ManualDispatchRunFrame(Pipe);

    public bool TryGetNextCallback(out int callbackId, out ReadOnlySpan<byte> data)
    {
        CallbackMsg msg = default;
        if (SteamApi.ManualDispatchGetNextCallback(Pipe, &msg) == 0)
        {
            callbackId = 0;
            data = default;
            return false;
        }

        _lastCallbackHeld = true;
        callbackId = msg.CallbackId;
        data = msg.Param == null || msg.ParamSize <= 0 ? default : new ReadOnlySpan<byte>(msg.Param, msg.ParamSize);
        return true;
    }

    public void FreeLastCallback()
    {
        if (_lastCallbackHeld)
        {
            _lastCallbackHeld = false;
            SteamApi.ManualDispatchFreeLastCallback(Pipe);
        }
    }

    public bool GetCallResult(ulong call, Span<byte> buffer, int expectedCallbackId, out bool failed)
    {
        byte failedByte = 0;
        bool ok;
        fixed (byte* p = buffer)
        {
            ok = SteamApi.ManualDispatchGetAPICallResult(Pipe, call, p, buffer.Length, expectedCallbackId, &failedByte) != 0;
        }

        failed = failedByte != 0;
        return ok;
    }

    public ulong GetSteamId() => SteamApi.UserGetSteamID(_user);

    public string GetPersonaName() => Text(SteamApi.FriendsGetPersonaName(_friends));

    public bool SetRichPresence(string key, string value) => SteamApi.FriendsSetRichPresence(_friends, key, value) != 0;

    public void ClearRichPresence() => SteamApi.FriendsClearRichPresence(_friends);

    public void ActivateOverlay(string dialog) => SteamApi.FriendsActivateGameOverlay(_friends, dialog);

    public void ActivateOverlayToWebPage(string url) => SteamApi.FriendsActivateGameOverlayToWebPage(_friends, url, 0);

    public void ActivateOverlayToStore(uint appId) => SteamApi.FriendsActivateGameOverlayToStore(_friends, appId, 0);

    public bool IsOverlayEnabled() => SteamApi.UtilsIsOverlayEnabled(_utils) != 0;

    public bool IsRunningOnDeck() => SteamApi.UtilsIsSteamRunningOnSteamDeck(_utils) != 0;

    public bool IsBigPicture() => SteamApi.UtilsIsSteamInBigPictureMode(_utils) != 0;

    public uint GetAppId() => SteamApi.UtilsGetAppID(_utils);

    public string GetUiLanguage() => Text(SteamApi.UtilsGetSteamUILanguage(_utils));

    public bool IsSubscribed() => SteamApi.AppsBIsSubscribed(_apps) != 0;

    public bool IsDlcInstalled(uint appId) => SteamApi.AppsBIsDlcInstalled(_apps, appId) != 0;

    public string GetGameLanguage() => Text(SteamApi.AppsGetCurrentGameLanguage(_apps));

    public int GetBuildId() => SteamApi.AppsGetAppBuildId(_apps);

    public bool SetAchievement(string name) => SteamApi.UserStatsSetAchievement(_stats, name) != 0;

    public bool ClearAchievement(string name) => SteamApi.UserStatsClearAchievement(_stats, name) != 0;

    public bool GetAchievement(string name, out bool unlocked)
    {
        byte value = 0;
        var ok = SteamApi.UserStatsGetAchievement(_stats, name, &value) != 0;
        unlocked = ok && value != 0;
        return ok;
    }

    public bool IndicateAchievementProgress(string name, uint current, uint max) =>
        SteamApi.UserStatsIndicateAchievementProgress(_stats, name, current, max) != 0;

    public bool StoreStats() => SteamApi.UserStatsStoreStats(_stats) != 0;

    public bool SetStat(string name, int value) => SteamApi.UserStatsSetStatInt32(_stats, name, value) != 0;

    public bool SetStat(string name, float value) => SteamApi.UserStatsSetStatFloat(_stats, name, value) != 0;

    public bool GetStat(string name, out int value)
    {
        int v = 0;
        var ok = SteamApi.UserStatsGetStatInt32(_stats, name, &v) != 0;
        value = ok ? v : 0;
        return ok;
    }

    public bool GetStat(string name, out float value)
    {
        float v = 0;
        var ok = SteamApi.UserStatsGetStatFloat(_stats, name, &v) != 0;
        value = ok ? v : 0;
        return ok;
    }

    public ulong RequestNumberOfCurrentPlayers() => SteamApi.UserStatsGetNumberOfCurrentPlayers(_stats);

    public bool CloudWrite(string name, ReadOnlySpan<byte> data)
    {
        fixed (byte* p = data)
        {
            return SteamApi.RemoteStorageFileWrite(_storage, name, p, data.Length) != 0;
        }
    }

    public int CloudRead(string name, Span<byte> buffer)
    {
        fixed (byte* p = buffer)
        {
            return SteamApi.RemoteStorageFileRead(_storage, name, p, buffer.Length);
        }
    }

    public bool CloudExists(string name) => SteamApi.RemoteStorageFileExists(_storage, name) != 0;

    public bool CloudDelete(string name) => SteamApi.RemoteStorageFileDelete(_storage, name) != 0;

    public int CloudSize(string name) => SteamApi.RemoteStorageGetFileSize(_storage, name);

    public int CloudFileCount() => SteamApi.RemoteStorageGetFileCount(_storage);

    public string CloudFileName(int index, out int size)
    {
        int s = 0;
        var name = Text(SteamApi.RemoteStorageGetFileNameAndSize(_storage, index, &s));
        size = s;
        return name;
    }

    public bool CloudQuota(out ulong total, out ulong available)
    {
        ulong t = 0, a = 0;
        var ok = SteamApi.RemoteStorageGetQuota(_storage, &t, &a) != 0;
        total = t;
        available = a;
        return ok;
    }

    public bool CloudEnabledForAccount() => SteamApi.RemoteStorageIsCloudEnabledForAccount(_storage) != 0;

    public bool CloudEnabledForApp() => SteamApi.RemoteStorageIsCloudEnabledForApp(_storage) != 0;

    private static string Text(byte* utf8) => utf8 == null ? string.Empty : Marshal.PtrToStringUTF8((nint)utf8) ?? string.Empty;
}
