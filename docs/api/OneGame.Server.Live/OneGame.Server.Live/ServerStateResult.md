[API reference](../../index.md) › [OneGame.Server.Live](../index.md) › OneGame.Server.Live

# ServerStateResult record

**Namespace:** `OneGame.Server.Live`  
**Package:** [`RecursiveAI.OneGame.Server.Live`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ServerStateResult`](ServerStateResult.md)&gt;

```csharp
public sealed record ServerStateResult : IEquatable<ServerStateResult>
```

Result of `hash` and `step`: the server tick and each scope's tick and state hash. Compare the hashes with a client's view to check for drift.

## Constructors

| Member | Description |
| --- | --- |
| [`ServerStateResult(long, ScopeInfo[])`](#m-ctor-long-scopeinfo) | Result of `hash` and `step`: the server tick and each scope's tick and state hash. |

### <a id="m-ctor-long-scopeinfo"></a>ServerStateResult(long, ScopeInfo\[\])

```csharp
public ServerStateResult(long Tick, ScopeInfo[] Scopes);
```

Result of `hash` and `step`: the server tick and each scope's tick and state hash. Compare the hashes with a client's view to check for drift.

**Parameters**

- `Tick` (`long`): The server's tick count.
- `Scopes` ([`ScopeInfo`](../../OneGame.Server/OneGame.Server/ScopeInfo.md)\[\]): One entry per hosted scope.

## Properties

| Member | Description |
| --- | --- |
| [`Scopes`](#m-scopes) | One entry per hosted scope. |
| [`Tick`](#m-tick) | The server's tick count. |

### <a id="m-scopes"></a>Scopes

```csharp
public ScopeInfo[] Scopes { get; init; }
```

One entry per hosted scope.

**Type:** [`ScopeInfo`](../../OneGame.Server/OneGame.Server/ScopeInfo.md)\[\]

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The server's tick count.

**Type:** `long`
