[API reference](../../index.md) › [OneGame.Server.Live](../index.md) › OneGame.Server.Live

# ServerStoppedLine record

**Namespace:** `OneGame.Server.Live`  
**Package:** [`RecursiveAI.OneGame.Server.Live`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ServerStoppedLine`](ServerStoppedLine.md)&gt;

```csharp
public sealed record ServerStoppedLine : IEquatable<ServerStoppedLine>
```

The line printed when the server has stopped.

## Constructors

| Member | Description |
| --- | --- |
| [`ServerStoppedLine(bool, long, ScopeInfo[])`](#m-ctor-bool-long-scopeinfo) | The line printed when the server has stopped. |

### <a id="m-ctor-bool-long-scopeinfo"></a>ServerStoppedLine(bool, long, ScopeInfo\[\])

```csharp
public ServerStoppedLine(bool Stopped, long Tick, ScopeInfo[] Scopes);
```

The line printed when the server has stopped.

**Parameters**

- `Stopped` (`bool`): Always true.
- `Tick` (`long`): The last tick.
- `Scopes` ([`ScopeInfo`](../../OneGame.Server/OneGame.Server/ScopeInfo.md)\[\]): The scopes' final state.

## Properties

| Member | Description |
| --- | --- |
| [`Scopes`](#m-scopes) | The scopes' final state. |
| [`Stopped`](#m-stopped) | Always true. |
| [`Tick`](#m-tick) | The last tick. |

### <a id="m-scopes"></a>Scopes

```csharp
public ScopeInfo[] Scopes { get; init; }
```

The scopes' final state.

**Type:** [`ScopeInfo`](../../OneGame.Server/OneGame.Server/ScopeInfo.md)\[\]

### <a id="m-stopped"></a>Stopped

```csharp
public bool Stopped { get; init; }
```

Always true.

**Type:** `bool`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The last tick.

**Type:** `long`
