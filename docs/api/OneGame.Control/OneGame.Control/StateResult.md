[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# StateResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`StateResult`](StateResult.md)&gt;

```csharp
public sealed record StateResult : IEquatable<StateResult>
```

Result of `step`, `hash` and the operations that change the world (`screen_open`, `fixture_load`, `reset`, `snapshot_load`).

## Constructors

| Member | Description |
| --- | --- |
| [`StateResult(long, long, string)`](#m-ctor-long-long-string) | Result of `step`, `hash` and the operations that change the world (`screen_open`, `fixture_load`, `reset`, `snapshot_load`). |

### <a id="m-ctor-long-long-string"></a>StateResult(long, long, string)

```csharp
public StateResult(long Frame, long Tick, string Hash);
```

Result of `step`, `hash` and the operations that change the world (`screen_open`, `fixture_load`, `reset`, `snapshot_load`).

**Parameters**

- `Frame` (`long`): Frames run so far.
- `Tick` (`long`): The world's tick afterwards.
- `Hash` (`string`): The state hash afterwards; equal hashes at equal ticks mean equal state.

## Properties

| Member | Description |
| --- | --- |
| [`Frame`](#m-frame) | Frames run so far. |
| [`Hash`](#m-hash) | The state hash afterwards; equal hashes at equal ticks mean equal state. |
| [`Tick`](#m-tick) | The world's tick afterwards. |

### <a id="m-frame"></a>Frame

```csharp
public long Frame { get; init; }
```

Frames run so far.

**Type:** `long`

### <a id="m-hash"></a>Hash

```csharp
public string Hash { get; init; }
```

The state hash afterwards; equal hashes at equal ticks mean equal state.

**Type:** `string`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The world's tick afterwards.

**Type:** `long`
