[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# EscrowReply struct

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`EscrowReply`](EscrowReply.md)&gt;

```csharp
public readonly struct EscrowReply : IEquatable<EscrowReply>
```

The answer to an escrow step: accepted (first time or repeat), or declined with a short reason.

## Constructors

| Member | Description |
| --- | --- |
| [`EscrowReply(bool, string?)`](#m-ctor-bool-string) | The answer to an escrow step: accepted (first time or repeat), or declined with a short reason. |

### <a id="m-ctor-bool-string"></a>EscrowReply(bool, string?)

```csharp
public EscrowReply(bool Accepted, string? Reason);
```

The answer to an escrow step: accepted (first time or repeat), or declined with a short reason.

**Parameters**

- `Accepted` (`bool`): True when the step is in effect.
- `Reason` (`string?`): Why not, when declined.

## Properties

| Member | Description |
| --- | --- |
| [`Accepted`](#m-accepted) | True when the step is in effect. |
| [`Ok`](#m-ok) | The step is in effect. |
| [`Reason`](#m-reason) | Why not, when declined. |

### <a id="m-accepted"></a>Accepted

```csharp
public bool Accepted { get; init; }
```

True when the step is in effect.

**Type:** `bool`

### <a id="m-ok"></a>Ok

```csharp
public static EscrowReply Ok { get; }
```

The step is in effect.

**Type:** [`EscrowReply`](EscrowReply.md)

### <a id="m-reason"></a>Reason

```csharp
public string? Reason { get; init; }
```

Why not, when declined.

**Type:** `string?`

## Methods

| Member | Description |
| --- | --- |
| [`No(string)`](#m-no-string) | The step was declined. |

### <a id="m-no-string"></a>No(string)

```csharp
public static EscrowReply No(string reason);
```

The step was declined.

**Parameters**

- `reason` (`string`)

**Returns:** [`EscrowReply`](EscrowReply.md)
