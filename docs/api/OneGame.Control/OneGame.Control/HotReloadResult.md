[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# HotReloadResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`HotReloadResult`](HotReloadResult.md)&gt;

```csharp
public sealed record HotReloadResult : IEquatable<HotReloadResult>
```

What a game's live control reports about development hot reload: the `hot_reload` operation.

## Constructors

| Member | Description |
| --- | --- |
| [`HotReloadResult(bool, int, bool, string[], long, string, string?, int, bool, bool)`](#m-ctor-bool-int-bool-string-long-string-string-int-bool-bool) | What a game's live control reports about development hot reload: the `hot_reload` operation. |

### <a id="m-ctor-bool-int-bool-string-long-string-string-int-bool-bool"></a>HotReloadResult(bool, int, bool, string\[\], long, string, string?, int, bool, bool)

```csharp
public HotReloadResult(bool Active, int Applied, bool Pending, string[] LastTypes, long LastTick, string State, string? Message, int Restarts, bool Resumed, bool ReplaySafe);
```

What a game's live control reports about development hot reload: the `hot_reload` operation.

**Parameters**

- `Active` (`bool`): Whether the runtime accepts code edits in this process (the game was started by `onegame run`, which starts it under `dotnet watch`).
- `Applied` (`int`): How many edits the runtime has applied since this process started.
- `Pending` (`bool`): Whether an applied edit has not yet been shown: the screens are rebuilt on the next frame.
- `LastTypes` (`string[]`): The types the last edit changed.
- `LastTick` (`long`): The simulation tick the last edit was shown at, or -1.
- `State` (`string`): `applied` (the last edit is live), `rude` (the last edit could not be applied to the running game and needs a restart), `error` (the last edit does not compile), `restarted` (the game restarted after a rude edit) or `idle` (no edit yet).
- `Message` (`string?`): For `rude`, `error` and `restarted`, what happened, ready to show; otherwise null.
- `Restarts` (`int`): How many times `onegame run` has restarted the game for a rude edit.
- `Resumed` (`bool`): Whether this process loaded the world its predecessor saved before a restart.
- `ReplaySafe` (`bool`): False once any edit was applied: recordings made across an edit, and hashes compared with a clean run, will not reproduce.

## Properties

| Member | Description |
| --- | --- |
| [`Active`](#m-active) | Whether the runtime accepts code edits in this process (the game was started by `onegame run`, which starts it under `dotnet watch`). |
| [`Applied`](#m-applied) | How many edits the runtime has applied since this process started. |
| [`LastTick`](#m-lasttick) | The simulation tick the last edit was shown at, or -1. |
| [`LastTypes`](#m-lasttypes) | The types the last edit changed. |
| [`Message`](#m-message) | For `rude`, `error` and `restarted`, what happened, ready to show; otherwise null. |
| [`Pending`](#m-pending) | Whether an applied edit has not yet been shown: the screens are rebuilt on the next frame. |
| [`ReplaySafe`](#m-replaysafe) | False once any edit was applied: recordings made across an edit, and hashes compared with a clean run, will not reproduce. |
| [`Restarts`](#m-restarts) | How many times `onegame run` has restarted the game for a rude edit. |
| [`Resumed`](#m-resumed) | Whether this process loaded the world its predecessor saved before a restart. |
| [`State`](#m-state) | `applied` (the last edit is live), `rude` (the last edit could not be applied to the running game and needs a restart), `error` (the last edit does not compile), `restarted` (the game restarted after a rude edit) or `idle` (no edit yet). |

### <a id="m-active"></a>Active

```csharp
public bool Active { get; init; }
```

Whether the runtime accepts code edits in this process (the game was started by `onegame run`, which starts it under `dotnet watch`).

**Type:** `bool`

### <a id="m-applied"></a>Applied

```csharp
public int Applied { get; init; }
```

How many edits the runtime has applied since this process started.

**Type:** `int`

### <a id="m-lasttick"></a>LastTick

```csharp
public long LastTick { get; init; }
```

The simulation tick the last edit was shown at, or -1.

**Type:** `long`

### <a id="m-lasttypes"></a>LastTypes

```csharp
public string[] LastTypes { get; init; }
```

The types the last edit changed.

**Type:** `string[]`

### <a id="m-message"></a>Message

```csharp
public string? Message { get; init; }
```

For `rude`, `error` and `restarted`, what happened, ready to show; otherwise null.

**Type:** `string?`

### <a id="m-pending"></a>Pending

```csharp
public bool Pending { get; init; }
```

Whether an applied edit has not yet been shown: the screens are rebuilt on the next frame.

**Type:** `bool`

### <a id="m-replaysafe"></a>ReplaySafe

```csharp
public bool ReplaySafe { get; init; }
```

False once any edit was applied: recordings made across an edit, and hashes compared with a clean run, will not reproduce.

**Type:** `bool`

### <a id="m-restarts"></a>Restarts

```csharp
public int Restarts { get; init; }
```

How many times `onegame run` has restarted the game for a rude edit.

**Type:** `int`

### <a id="m-resumed"></a>Resumed

```csharp
public bool Resumed { get; init; }
```

Whether this process loaded the world its predecessor saved before a restart.

**Type:** `bool`

### <a id="m-state"></a>State

```csharp
public string State { get; init; }
```

`applied` (the last edit is live), `rude` (the last edit could not be applied to the running game and needs a restart), `error` (the last edit does not compile), `restarted` (the game restarted after a rude edit) or `idle` (no edit yet).

**Type:** `string`
