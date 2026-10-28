[API reference](../../index.md) › [OneGame.Server.Live](../index.md) › OneGame.Server.Live

# ServerStateResult record

**Namespace:** `OneGame.Server.Live`  
**Package:** [`RecursiveAI.OneGame.Server.Live`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ServerStateResult`](ServerStateResult.md)&gt;

```csharp
public sealed record ServerStateResult : IEquatable<ServerStateResult>
```

Result of `hash` and `step`.

## Constructors

| Member | Description |
| --- | --- |
| [`ServerStateResult(long, ScopeInfo[])`](#m-ctor-long-scopeinfo) | Result of `hash` and `step`. |

### <a id="m-ctor-long-scopeinfo"></a>ServerStateResult(long, ScopeInfo\[\])

```csharp
public ServerStateResult(long Tick, ScopeInfo[] Scopes);
```

Result of `hash` and `step`.

**Parameters**

- `Tick` (`long`)
- `Scopes` ([`ScopeInfo`](../../OneGame.Server/OneGame.Server/ScopeInfo.md)\[\])

## Properties

| Member | Description |
| --- | --- |
| [`Scopes`](#m-scopes) |  |
| [`Tick`](#m-tick) |  |

### <a id="m-scopes"></a>Scopes

```csharp
public ScopeInfo[] Scopes { get; init; }
```

**Type:** [`ScopeInfo`](../../OneGame.Server/OneGame.Server/ScopeInfo.md)\[\]

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

**Type:** `long`
