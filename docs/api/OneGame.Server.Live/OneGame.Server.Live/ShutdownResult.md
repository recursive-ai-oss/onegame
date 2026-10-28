[API reference](../../index.md) › [OneGame.Server.Live](../index.md) › OneGame.Server.Live

# ShutdownResult record

**Namespace:** `OneGame.Server.Live`  
**Package:** [`RecursiveAI.OneGame.Server.Live`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ShutdownResult`](ShutdownResult.md)&gt;

```csharp
public sealed record ShutdownResult : IEquatable<ShutdownResult>
```

Result of `shutdown`: the server finishes its current tick, then stops.

## Constructors

| Member | Description |
| --- | --- |
| [`ShutdownResult(bool, long)`](#m-ctor-bool-long) | Result of `shutdown`: the server finishes its current tick, then stops. |

### <a id="m-ctor-bool-long"></a>ShutdownResult(bool, long)

```csharp
public ShutdownResult(bool Stopping, long Tick);
```

Result of `shutdown`: the server finishes its current tick, then stops.

**Parameters**

- `Stopping` (`bool`): True once the stop was requested.
- `Tick` (`long`): The tick the server was at.

## Properties

| Member | Description |
| --- | --- |
| [`Stopping`](#m-stopping) | True once the stop was requested. |
| [`Tick`](#m-tick) | The tick the server was at. |

### <a id="m-stopping"></a>Stopping

```csharp
public bool Stopping { get; init; }
```

True once the stop was requested.

**Type:** `bool`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The tick the server was at.

**Type:** `long`
