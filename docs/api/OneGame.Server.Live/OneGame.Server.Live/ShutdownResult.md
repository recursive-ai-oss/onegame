[API reference](../../index.md) › [OneGame.Server.Live](../index.md) › OneGame.Server.Live

# ShutdownResult record

**Namespace:** `OneGame.Server.Live`  
**Package:** [`RecursiveAI.OneGame.Server.Live`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ShutdownResult`](ShutdownResult.md)&gt;

```csharp
public sealed record ShutdownResult : IEquatable<ShutdownResult>
```

Result of `shutdown`.

## Constructors

| Member | Description |
| --- | --- |
| [`ShutdownResult(bool, long)`](#m-ctor-bool-long) | Result of `shutdown`. |

### <a id="m-ctor-bool-long"></a>ShutdownResult(bool, long)

```csharp
public ShutdownResult(bool Stopping, long Tick);
```

Result of `shutdown`.

**Parameters**

- `Stopping` (`bool`)
- `Tick` (`long`)

## Properties

| Member | Description |
| --- | --- |
| [`Stopping`](#m-stopping) |  |
| [`Tick`](#m-tick) |  |

### <a id="m-stopping"></a>Stopping

```csharp
public bool Stopping { get; init; }
```

**Type:** `bool`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

**Type:** `long`
