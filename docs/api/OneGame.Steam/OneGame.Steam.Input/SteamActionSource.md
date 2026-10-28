[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam.Input

# SteamActionSource class

**Namespace:** `OneGame.Steam.Input`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class SteamActionSource : IDisposable
```

Feeds Steam Input into the engine's [`ActionMap`](../../OneGame.Platform/OneGame.Platform/ActionMap.md): an action of the map and a Steam action of the same name and kind (button, vector, one-axis) are one action, so game code reads `actions.Pressed("jump")` whether the player uses a keyboard, a pad or a Steam controller configured in Steam's own binding screen. Declare the actions once in the map and build the Steam declaration (and its actions file) from it with [`SteamActionSource.CreateActions()`](SteamActionSource.md#m-createactions-actionmap-int). Values are read when the map updates, from whatever [`SteamInputService.Update()`](SteamInputService.md#m-update) last saw, so keep calling [`SteamInputBridge.Update()`](SteamInputBridge.md#m-update-string) (or the service) once a frame in `OnEachFrame`. Steam keeps one action set active at a time (the one that lists the open screen, else the first), so with Steam the contexts that matter are the ones named in `OnScreens`.

## Constructors

| Member | Description |
| --- | --- |
| [`SteamActionSource(SteamInputService, ActionMap)`](#m-ctor-steaminputservice-actionmap) | Attaches to a map. |

### <a id="m-ctor-steaminputservice-actionmap"></a>SteamActionSource(SteamInputService, ActionMap)

```csharp
public SteamActionSource(SteamInputService service, ActionMap map);
```

Attaches to a map.

**Parameters**

- `service` ([`SteamInputService`](SteamInputService.md)): The Steam Input service (real or the harness stand-in).
- `map` ([`ActionMap`](../../OneGame.Platform/OneGame.Platform/ActionMap.md)): The game's actions. Only actions whose name and kind match a Steam action are fed.

## Properties

| Member | Description |
| --- | --- |
| [`FedActions`](#m-fedactions) | How many actions of the map are fed from Steam. |

### <a id="m-fedactions"></a>FedActions

```csharp
public int FedActions { get; }
```

How many actions of the map are fed from Steam.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`CreateActions(ActionMap, int)`](#m-createactions-actionmap-int) | Builds the Steam declaration from the map: one action set per context (named after it, with the screens it lists), a button action per button, a joystick-move stick action per vector and a one-axis action per axis. |
| [`Dispose()`](#m-dispose) | Detaches from the map and lets go of every action it held. |

### <a id="m-createactions-actionmap-int"></a>CreateActions(ActionMap, int)

```csharp
public static SteamInputActions CreateActions(ActionMap map, int majorRevision = 0);
```

Builds the Steam declaration from the map: one action set per context (named after it, with the screens it lists), a button action per button, a joystick-move stick action per vector and a one-axis action per axis. The result writes the in-game actions file like any [`SteamInputActions`](SteamInputActions.md).

**Parameters**

- `map` ([`ActionMap`](../../OneGame.Platform/OneGame.Platform/ActionMap.md)): The game's actions.
- `majorRevision` (`int`): The file's major revision; bump it when the actions change.

**Returns:** [`SteamInputActions`](SteamInputActions.md) — The declaration.

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Detaches from the map and lets go of every action it held.
