[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam.Native

# SteamInitStatus enum

**Namespace:** `OneGame.Steam.Native`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)

```csharp
public enum SteamInitStatus : int
```

Result of the library's init call. The numbers are the documented values of the init result enum.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-ok"></a>`Ok` | `0` | Initialized. |
| <a id="m-failedgeneric"></a>`FailedGeneric` | `1` | Something went wrong; the message says what. |
| <a id="m-nosteamclient"></a>`NoSteamClient` | `2` | The Steam client is not running or its pipe could not be opened. |
| <a id="m-versionmismatch"></a>`VersionMismatch` | `3` | The Steam client is older than the library the game ships with. |
