[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# StateResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`StateResult`](StateResult.md)&gt;

```csharp
public sealed record StateResult : IEquatable<StateResult>
```

Result of `step`, `hash` and the operations that change the world.

## Constructors

| Member | Description |
| --- | --- |
| [`StateResult(long, long, string)`](#m-ctor-long-long-string) | Result of `step`, `hash` and the operations that change the world. |

### <a id="m-ctor-long-long-string"></a>StateResult(long, long, string)

```csharp
public StateResult(long Frame, long Tick, string Hash);
```

Result of `step`, `hash` and the operations that change the world.

**Parameters**

- `Frame` (`long`)
- `Tick` (`long`)
- `Hash` (`string`)

## Properties

| Member | Description |
| --- | --- |
| [`Frame`](#m-frame) |  |
| [`Hash`](#m-hash) |  |
| [`Tick`](#m-tick) |  |

### <a id="m-frame"></a>Frame

```csharp
public long Frame { get; init; }
```

**Type:** `long`

### <a id="m-hash"></a>Hash

```csharp
public string Hash { get; init; }
```

**Type:** `string`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

**Type:** `long`
