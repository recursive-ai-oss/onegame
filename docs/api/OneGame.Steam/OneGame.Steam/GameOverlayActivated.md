[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam

# GameOverlayActivated struct

**Namespace:** `OneGame.Steam`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)  
**Implements:** [`ISteamCallbackData`](ISteamCallbackData-1.md)&lt;[`GameOverlayActivated`](GameOverlayActivated.md)&gt;, [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`GameOverlayActivated`](GameOverlayActivated.md)&gt;

```csharp
public readonly struct GameOverlayActivated : ISteamCallbackData<GameOverlayActivated>, IEquatable<GameOverlayActivated>
```

The overlay opened or closed. Only the documented first field is read; the struct may be longer.

## Constructors

| Member | Description |
| --- | --- |
| [`GameOverlayActivated(bool)`](#m-ctor-bool) | The overlay opened or closed. |

### <a id="m-ctor-bool"></a>GameOverlayActivated(bool)

```csharp
public GameOverlayActivated(bool Active);
```

The overlay opened or closed. Only the documented first field is read; the struct may be longer.

**Parameters**

- `Active` (`bool`): True when the overlay just opened.

## Properties

| Member | Description |
| --- | --- |
| [`Active`](#m-active) | True when the overlay just opened. |

### <a id="m-active"></a>Active

```csharp
public bool Active { get; init; }
```

True when the overlay just opened.

**Type:** `bool`
