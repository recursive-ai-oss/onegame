using System.Runtime.InteropServices;

namespace OneGame.Steam.Native;

/// <summary>Layout of the record the manual-dispatch loop fills in: user handle, callback id, pointer to the callback data and its
/// size. Natural alignment, 24 bytes on 64-bit targets.</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct CallbackMsg
{
    public int User;
    public int CallbackId;
    public byte* Param;
    public int ParamSize;
}

/// <summary>Direct calls into the Steam redistributable's flat C API (the <c>SteamAPI_*</c> exports of <c>steam_api</c>). Only 64-bit targets are supported, where there is a single calling convention. Booleans cross as one
/// byte, as in the C++ <c>bool</c> the library was built with. Every entry point is named explicitly so the set can be checked
/// against the library's export table.</summary>
internal static unsafe partial class SteamApi
{
    /// <summary>The logical library name; <see cref="SteamLibrary"/> maps it to the platform file.</summary>
    internal const string Library = "steam_api";

    [LibraryImport(Library, EntryPoint = "SteamAPI_IsSteamRunning")]
    internal static partial byte IsSteamRunning();

    [LibraryImport(Library, EntryPoint = "SteamAPI_InitFlat")]
    internal static partial int InitFlat(byte* errMsg);

    [LibraryImport(Library, EntryPoint = "SteamAPI_Shutdown")]
    internal static partial void Shutdown();

    [LibraryImport(Library, EntryPoint = "SteamAPI_RestartAppIfNecessary")]
    internal static partial byte RestartAppIfNecessary(uint ownAppId);

    [LibraryImport(Library, EntryPoint = "SteamAPI_GetHSteamPipe")]
    internal static partial int GetHSteamPipe();

    [LibraryImport(Library, EntryPoint = "SteamAPI_ManualDispatch_Init")]
    internal static partial void ManualDispatchInit();

    [LibraryImport(Library, EntryPoint = "SteamAPI_ManualDispatch_RunFrame")]
    internal static partial void ManualDispatchRunFrame(int pipe);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ManualDispatch_GetNextCallback")]
    internal static partial byte ManualDispatchGetNextCallback(int pipe, CallbackMsg* msg);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ManualDispatch_FreeLastCallback")]
    internal static partial void ManualDispatchFreeLastCallback(int pipe);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ManualDispatch_GetAPICallResult")]
    internal static partial byte ManualDispatchGetAPICallResult(int pipe, ulong call, byte* buffer, int size, int expectedCallback, byte* failed);

    [LibraryImport(Library, EntryPoint = "SteamAPI_SteamUser_v023")]
    internal static partial nint GetUser();

    [LibraryImport(Library, EntryPoint = "SteamAPI_SteamUserStats_v013")]
    internal static partial nint GetUserStats();

    [LibraryImport(Library, EntryPoint = "SteamAPI_SteamFriends_v018")]
    internal static partial nint GetFriends();

    [LibraryImport(Library, EntryPoint = "SteamAPI_SteamUtils_v010")]
    internal static partial nint GetUtils();

    [LibraryImport(Library, EntryPoint = "SteamAPI_SteamApps_v009")]
    internal static partial nint GetApps();

    [LibraryImport(Library, EntryPoint = "SteamAPI_SteamRemoteStorage_v016")]
    internal static partial nint GetRemoteStorage();

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamUser_GetSteamID")]
    internal static partial ulong UserGetSteamID(nint self);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamUser_BLoggedOn")]
    internal static partial byte UserBLoggedOn(nint self);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamFriends_GetPersonaName")]
    internal static partial byte* FriendsGetPersonaName(nint self);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamFriends_SetRichPresence", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial byte FriendsSetRichPresence(nint self, string key, string value);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamFriends_ClearRichPresence")]
    internal static partial void FriendsClearRichPresence(nint self);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamFriends_ActivateGameOverlay", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void FriendsActivateGameOverlay(nint self, string dialog);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamFriends_ActivateGameOverlayToWebPage", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void FriendsActivateGameOverlayToWebPage(nint self, string url, int mode);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamFriends_ActivateGameOverlayToStore")]
    internal static partial void FriendsActivateGameOverlayToStore(nint self, uint appId, int flag);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamUtils_IsOverlayEnabled")]
    internal static partial byte UtilsIsOverlayEnabled(nint self);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamUtils_IsSteamRunningOnSteamDeck")]
    internal static partial byte UtilsIsSteamRunningOnSteamDeck(nint self);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamUtils_IsSteamInBigPictureMode")]
    internal static partial byte UtilsIsSteamInBigPictureMode(nint self);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamUtils_GetAppID")]
    internal static partial uint UtilsGetAppID(nint self);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamUtils_GetSteamUILanguage")]
    internal static partial byte* UtilsGetSteamUILanguage(nint self);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamApps_BIsSubscribed")]
    internal static partial byte AppsBIsSubscribed(nint self);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamApps_BIsSubscribedApp")]
    internal static partial byte AppsBIsSubscribedApp(nint self, uint appId);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamApps_BIsDlcInstalled")]
    internal static partial byte AppsBIsDlcInstalled(nint self, uint appId);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamApps_GetCurrentGameLanguage")]
    internal static partial byte* AppsGetCurrentGameLanguage(nint self);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamApps_GetAppBuildId")]
    internal static partial int AppsGetAppBuildId(nint self);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamUserStats_SetAchievement", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial byte UserStatsSetAchievement(nint self, string name);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamUserStats_ClearAchievement", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial byte UserStatsClearAchievement(nint self, string name);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamUserStats_GetAchievement", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial byte UserStatsGetAchievement(nint self, string name, byte* achieved);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamUserStats_IndicateAchievementProgress", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial byte UserStatsIndicateAchievementProgress(nint self, string name, uint current, uint max);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamUserStats_StoreStats")]
    internal static partial byte UserStatsStoreStats(nint self);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamUserStats_SetStatInt32", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial byte UserStatsSetStatInt32(nint self, string name, int value);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamUserStats_SetStatFloat", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial byte UserStatsSetStatFloat(nint self, string name, float value);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamUserStats_GetStatInt32", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial byte UserStatsGetStatInt32(nint self, string name, int* value);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamUserStats_GetStatFloat", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial byte UserStatsGetStatFloat(nint self, string name, float* value);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamUserStats_GetNumberOfCurrentPlayers")]
    internal static partial ulong UserStatsGetNumberOfCurrentPlayers(nint self);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamRemoteStorage_FileWrite", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial byte RemoteStorageFileWrite(nint self, string name, byte* data, int size);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamRemoteStorage_FileRead", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int RemoteStorageFileRead(nint self, string name, byte* data, int size);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamRemoteStorage_FileExists", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial byte RemoteStorageFileExists(nint self, string name);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamRemoteStorage_FileDelete", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial byte RemoteStorageFileDelete(nint self, string name);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamRemoteStorage_GetFileSize", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int RemoteStorageGetFileSize(nint self, string name);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamRemoteStorage_GetFileCount")]
    internal static partial int RemoteStorageGetFileCount(nint self);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamRemoteStorage_GetFileNameAndSize")]
    internal static partial byte* RemoteStorageGetFileNameAndSize(nint self, int index, int* size);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamRemoteStorage_GetQuota")]
    internal static partial byte RemoteStorageGetQuota(nint self, ulong* total, ulong* available);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamRemoteStorage_IsCloudEnabledForAccount")]
    internal static partial byte RemoteStorageIsCloudEnabledForAccount(nint self);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamRemoteStorage_IsCloudEnabledForApp")]
    internal static partial byte RemoteStorageIsCloudEnabledForApp(nint self);

}
