[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Stores

# IStoreIdentity interface

**Namespace:** `OneGame.Stores`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)

```csharp
public interface IStoreIdentity
```

The signed-in player.

## Properties

| Member | Description |
| --- | --- |
| [`DisplayName`](#m-displayname) | The name the player goes by in the store. |
| [`UserId`](#m-userid) | Store-specific account id; 0 when unknown. |

### <a id="m-displayname"></a>DisplayName

```csharp
string DisplayName { get; }
```

The name the player goes by in the store. Empty when unknown.

**Type:** `string`

### <a id="m-userid"></a>UserId

```csharp
ulong UserId { get; }
```

Store-specific account id; 0 when unknown. For Steam this is the 64-bit SteamID.

**Type:** `ulong`
