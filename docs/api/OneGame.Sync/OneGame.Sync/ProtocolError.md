[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# ProtocolError struct

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ProtocolError`](ProtocolError.md)&gt;

```csharp
public readonly struct ProtocolError : IEquatable<ProtocolError>
```

A message from a peer that could not be applied; the replica asked for a fresh snapshot.

## Constructors

| Member | Description |
| --- | --- |
| [`ProtocolError(ScopeId, long, string)`](#m-ctor-scopeid-long-string) | A message from a peer that could not be applied; the replica asked for a fresh snapshot. |

### <a id="m-ctor-scopeid-long-string"></a>ProtocolError(ScopeId, long, string)

```csharp
public ProtocolError(ScopeId Scope, long Tick, string Reason);
```

A message from a peer that could not be applied; the replica asked for a fresh snapshot.

**Parameters**

- `Scope` ([`ScopeId`](ScopeId.md)): The scope concerned.
- `Tick` (`long`): The replica's tick when it happened.
- `Reason` (`string`): What was wrong.

## Properties

| Member | Description |
| --- | --- |
| [`Reason`](#m-reason) | What was wrong. |
| [`Scope`](#m-scope) | The scope concerned. |
| [`Tick`](#m-tick) | The replica's tick when it happened. |

### <a id="m-reason"></a>Reason

```csharp
public string Reason { get; init; }
```

What was wrong.

**Type:** `string`

### <a id="m-scope"></a>Scope

```csharp
public ScopeId Scope { get; init; }
```

The scope concerned.

**Type:** [`ScopeId`](ScopeId.md)

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The replica's tick when it happened.

**Type:** `long`
