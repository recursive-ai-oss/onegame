[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# ScopeHashAt record

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ScopeHashAt`](ScopeHashAt.md)&gt;

```csharp
public sealed record ScopeHashAt : IEquatable<ScopeHashAt>
```

The recorded hash of one scope at one tick.

## Constructors

| Member | Description |
| --- | --- |
| [`ScopeHashAt(uint, long, string?, string[])`](#m-ctor-uint-long-string-string) | The recorded hash of one scope at one tick. |

### <a id="m-ctor-uint-long-string-string"></a>ScopeHashAt(uint, long, string?, string\[\])

```csharp
public ScopeHashAt(uint Scope, long Tick, string? Hash, string[] Views);
```

The recorded hash of one scope at one tick.

**Parameters**

- `Scope` (`uint`): The scope id.
- `Tick` (`long`): The tick asked for.
- `Hash` (`string?`): The hash of the whole scope as 16 hex digits, or null when that tick is not (or no longer) recorded.
- `Views` (`string[]`): The hash of what each connected client's replica must hold at that tick (its audience's view of the scope); equal to `Hash` when everything is visible to everyone.

## Properties

| Member | Description |
| --- | --- |
| [`Clients`](#m-clients) | The names of the connections the entries of [`ScopeHashAt.Views`](ScopeHashAt.md#m-views) belong to, in the same order (empty for a tick that is not recorded). |
| [`Hash`](#m-hash) | The hash of the whole scope as 16 hex digits, or null when that tick is not (or no longer) recorded. |
| [`Scope`](#m-scope) | The scope id. |
| [`Tick`](#m-tick) | The tick asked for. |
| [`View`](#m-view) | When a client was named in the question: the view hash that client's replica must hold at this tick (16 hex digits), or null when the tick is not recorded or that client was not subscribed to the scope then. |
| [`Views`](#m-views) | The hash of what each connected client's replica must hold at that tick (its audience's view of the scope); equal to `Hash` when everything is visible to everyone. |

### <a id="m-clients"></a>Clients

```csharp
public string[] Clients { get; init; }
```

The names of the connections the entries of [`ScopeHashAt.Views`](ScopeHashAt.md#m-views) belong to, in the same order (empty for a tick that is not recorded).

**Type:** `string[]`

### <a id="m-hash"></a>Hash

```csharp
public string? Hash { get; init; }
```

The hash of the whole scope as 16 hex digits, or null when that tick is not (or no longer) recorded.

**Type:** `string?`

### <a id="m-scope"></a>Scope

```csharp
public uint Scope { get; init; }
```

The scope id.

**Type:** `uint`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The tick asked for.

**Type:** `long`

### <a id="m-view"></a>View

```csharp
public string? View { get; init; }
```

When a client was named in the question: the view hash that client's replica must hold at this tick (16 hex digits), or null when the tick is not recorded or that client was not subscribed to the scope then. This is the authority's `ViewHashFor(connection)`.

**Type:** `string?`

### <a id="m-views"></a>Views

```csharp
public string[] Views { get; init; }
```

The hash of what each connected client's replica must hold at that tick (its audience's view of the scope); equal to `Hash` when everything is visible to everyone.

**Type:** `string[]`
