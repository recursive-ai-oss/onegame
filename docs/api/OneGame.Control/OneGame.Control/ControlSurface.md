[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# ControlSurface class

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IControlEndpoint`](IControlEndpoint.md)

```csharp
public sealed class ControlSurface : IControlEndpoint
```

The control layer: every operation of [`ControlOps`](ControlOps.md) run against one [`GameInstance`](../../OneGame.Host/OneGame.Host/GameInstance.md). The command line, the test API and live control (MCP) all call [`ControlSurface.Invoke()`](ControlSurface.md#m-invoke-string-string); none of them reaches into the game any other way.

## Constructors

| Member | Description |
| --- | --- |
| [`ControlSurface(GameInstance, string?)`](#m-ctor-gameinstance-string) | Creates a control surface over a game. |

### <a id="m-ctor-gameinstance-string"></a>ControlSurface(GameInstance, string?)

```csharp
public ControlSurface(GameInstance game, string? outputDirectory = null);
```

Creates a control surface over a game.

**Parameters**

- `game` ([`GameInstance`](../../OneGame.Host/OneGame.Host/GameInstance.md)): The game to control.
- `outputDirectory` (`string?`): Where captures and snapshots are written and read. Relative paths in operations resolve here and may not leave it. Defaults to the current directory; the game runner passes its `--artifacts` folder.

## Properties

| Member | Description |
| --- | --- |
| [`Audio`](#m-audio) | The game's audio, for the `audio` operations. |
| [`Game`](#m-game) | The controlled game. |
| [`Instructions`](#m-instructions) | A sentence or two for an agent that connects: where to start and what changes state. |
| [`Operations`](#m-operations) | The operations, each of which is an MCP tool. |
| [`OutputDirectory`](#m-outputdirectory) | The folder operations may write to and read from. |
| [`Overlay`](#m-overlay) | The debug panel of this game. |
| [`Purchases`](#m-purchases) | The game's purchase service, for the `store` operation. |

### <a id="m-audio"></a>Audio

```csharp
public AudioService? Audio { get; set; }
```

The game's audio, for the `audio` operations. Defaults to the most recently created [`AudioService`](../../OneGame.Audio/OneGame.Audio/AudioService.md), so a game needs to do nothing; set it to point at another.

**Type:** [`AudioService`](../../OneGame.Audio/OneGame.Audio/AudioService.md)?

### <a id="m-game"></a>Game

```csharp
public GameInstance Game { get; }
```

The controlled game.

**Type:** [`GameInstance`](../../OneGame.Host/OneGame.Host/GameInstance.md)

### <a id="m-instructions"></a>Instructions

```csharp
public string Instructions { get; }
```

A sentence or two for an agent that connects: where to start and what changes state.

**Type:** `string`

### <a id="m-operations"></a>Operations

```csharp
public IReadOnlyList<ControlOp> Operations { get; }
```

The operations, each of which is an MCP tool.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ControlOp`](ControlOp.md)&gt;

### <a id="m-outputdirectory"></a>OutputDirectory

```csharp
public string OutputDirectory { get; }
```

The folder operations may write to and read from.

**Type:** `string`

### <a id="m-overlay"></a>Overlay

```csharp
public DebugOverlay Overlay { get; }
```

The debug panel of this game.

**Type:** [`DebugOverlay`](DebugOverlay.md)

### <a id="m-purchases"></a>Purchases

```csharp
public PurchaseService? Purchases { get; set; }
```

The game's purchase service, for the `store` operation. Defaults to the most recently created [`PurchaseService`](../../OneGame.Purchases/OneGame.Purchases/PurchaseService.md), so a game needs to do nothing; set it to point at another.

**Type:** [`PurchaseService`](../../OneGame.Purchases/OneGame.Purchases/PurchaseService.md)?

## Methods

| Member | Description |
| --- | --- |
| [`Call(string, string?)`](#m-call-string-string) | Runs an operation and returns its JSON text. |
| [`Invoke(string, string?)`](#m-invoke-string-string) | Runs an operation. |
| [`InvokeAsync(string, string?)`](#m-invokeasync-string-string) | Runs an operation like [`ControlSurface.Invoke()`](ControlSurface.md#m-invoke-string-string). |
| [`PollShaders()`](#m-pollshaders) | Checks the registered shader files and the loaded asset files for edits, as the `shaders` and `assets` operations do. |
| [`ResolveEntityId(string)`](#m-resolveentityid-string) | Finds an entity by numeric id, type name or `Type#n`, as operations do. |

### <a id="m-call-string-string"></a>Call(string, string?)

```csharp
public string Call(string op, string? argsJson = null);
```

Runs an operation and returns its JSON text.

**Parameters**

- `op` (`string`)
- `argsJson` (`string?`)

**Returns:** `string`

**Exceptions**

- [`ControlException`](ControlException.md): The operation is unknown, its arguments are wrong, or it failed.

### <a id="m-invoke-string-string"></a>Invoke(string, string?)

```csharp
public ControlResult Invoke(string op, string? argsJson = null);
```

Runs an operation. `argsJson` is a JSON object (or null for none).

**Parameters**

- `op` (`string`)
- `argsJson` (`string?`)

**Returns:** [`ControlResult`](ControlResult.md)

**Exceptions**

- [`ControlException`](ControlException.md): The operation is unknown, its arguments are wrong, or it failed.

### <a id="m-invokeasync-string-string"></a>InvokeAsync(string, string?)

```csharp
public ValueTask<ControlResult> InvokeAsync(string op, string? argsJson = null);
```

Runs an operation like [`ControlSurface.Invoke()`](ControlSurface.md#m-invoke-string-string). Operations that read pixels back from the graphics device can take a while on a platform that cannot block (a browser page), so they finish here; everything else completes before this returns.

**Parameters**

- `op` (`string`)
- `argsJson` (`string?`)

**Returns:** [`ValueTask`](https://learn.microsoft.com/dotnet/api/system.threading.tasks.valuetask-1)&lt;[`ControlResult`](ControlResult.md)&gt;

**Exceptions**

- [`ControlException`](ControlException.md): The operation is unknown, its arguments are wrong, or it failed.

### <a id="m-pollshaders"></a>PollShaders()

```csharp
public void PollShaders();
```

Checks the registered shader files and the loaded asset files for edits, as the `shaders` and `assets` operations do. Cheap; call it from a game loop.

### <a id="m-resolveentityid-string"></a>ResolveEntityId(string)

```csharp
public EntityId ResolveEntityId(string spec);
```

Finds an entity by numeric id, type name or `Type#n`, as operations do.

**Parameters**

- `spec` (`string`): The entity.

**Returns:** [`EntityId`](../../OneGame.Core/OneGame.Core/EntityId.md) — Its id.

**Exceptions**

- [`ControlException`](ControlException.md): There is no such entity.
