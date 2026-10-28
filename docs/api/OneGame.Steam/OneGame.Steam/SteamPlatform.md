[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam

# SteamPlatform static class

**Namespace:** `OneGame.Steam`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)

```csharp
public static class SteamPlatform
```

Entry point: connects to Steam, or falls back to a store that does nothing.

## Methods

| Member | Description |
| --- | --- |
| [`Start(SteamOptions?)`](#m-start-steamoptions) | Connects to Steam. |

### <a id="m-start-steamoptions"></a>Start(SteamOptions?)

```csharp
public static IStorePlatform Start(SteamOptions? options = null);
```

Connects to Steam. Never throws for an absent Steam: when the library or client is missing, or Steam refuses the app, the result is a [`NullStorePlatform`](../OneGame.Stores/NullStorePlatform.md) whose [`IStorePlatform.UnavailableReason`](../OneGame.Stores/IStorePlatform.md#m-unavailablereason) says why.

**Parameters**

- `options` ([`SteamOptions`](SteamOptions.md)?): Connection settings, or null for the defaults.

**Returns:** [`IStorePlatform`](../OneGame.Stores/IStorePlatform.md)
