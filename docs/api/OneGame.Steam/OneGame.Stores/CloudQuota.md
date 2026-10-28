[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Stores

# CloudQuota struct

**Namespace:** `OneGame.Stores`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`CloudQuota`](CloudQuota.md)&gt;

```csharp
public readonly struct CloudQuota : IEquatable<CloudQuota>
```

Cloud storage quota in bytes.

## Constructors

| Member | Description |
| --- | --- |
| [`CloudQuota(ulong, ulong)`](#m-ctor-ulong-ulong) | Cloud storage quota in bytes. |

### <a id="m-ctor-ulong-ulong"></a>CloudQuota(ulong, ulong)

```csharp
public CloudQuota(ulong TotalBytes, ulong AvailableBytes);
```

Cloud storage quota in bytes.

**Parameters**

- `TotalBytes` (`ulong`): All the space the player has for this game.
- `AvailableBytes` (`ulong`): The space still free.

## Properties

| Member | Description |
| --- | --- |
| [`AvailableBytes`](#m-availablebytes) | The space still free. |
| [`TotalBytes`](#m-totalbytes) | All the space the player has for this game. |

### <a id="m-availablebytes"></a>AvailableBytes

```csharp
public ulong AvailableBytes { get; init; }
```

The space still free.

**Type:** `ulong`

### <a id="m-totalbytes"></a>TotalBytes

```csharp
public ulong TotalBytes { get; init; }
```

All the space the player has for this game.

**Type:** `ulong`
