[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# EscrowDriver class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public sealed class EscrowDriver
```

Host code on the coordinator's node that carries out what the coordinator's [`EscrowBook`](EscrowBook.md) says is still to do: it sends the withdraw, abort and credit steps to the participants' desks as commands and feeds their answers back to the coordinator's desk. It keeps no record of its own beyond which steps it sent recently, so a node restarted from its last save simply starts sending again, and the idempotent steps make the repeats harmless. Call [`EscrowDriver.Pump()`](EscrowDriver.md#m-pump) once per cluster tick.

## Constructors

| Member | Description |
| --- | --- |
| [`EscrowDriver(AuthorityScope, Func<ScopeId, ReplicaScope>, int)`](#m-ctor-authorityscope-func-scopeid-replicascope-int) | Creates a driver. |

### <a id="m-ctor-authorityscope-func-scopeid-replicascope-int"></a>EscrowDriver(AuthorityScope, Func&lt;ScopeId, ReplicaScope&gt;, int)

```csharp
public EscrowDriver(AuthorityScope coordinator, Func<ScopeId, ReplicaScope> participant, int resendTicks = 10);
```

Creates a driver.

**Parameters**

- `coordinator` ([`AuthorityScope`](AuthorityScope.md)): The coordinator's scope; its world has an escrow book ([`Escrow.Install()`](Escrow.md#m-install-world-iescrowhandler)).
- `participant` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-2)&lt;[`ScopeId`](ScopeId.md), [`ReplicaScope`](ReplicaScope.md)&gt;): Finds this node's replica of a participant's scope, or null while it is not connected. The replica's link must carry [`Escrow.CoordinatorTrust()`](Escrow.md#m-coordinatortrust-scopeid).
- `resendTicks` (`int`): Ticks before a step that got no answer is sent again.

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The coordinator's world has no escrow book or desk.

## Properties

| Member | Description |
| --- | --- |
| [`ResendTicks`](#m-resendticks) | Ticks before an unanswered step is sent again. |
| [`StepsSent`](#m-stepssent) | Steps sent so far, repeats included. |

### <a id="m-resendticks"></a>ResendTicks

```csharp
public int ResendTicks { get; set; }
```

Ticks before an unanswered step is sent again.

**Type:** `int`

### <a id="m-stepssent"></a>StepsSent

```csharp
public int StepsSent { get; }
```

Steps sent so far, repeats included.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Pump()`](#m-pump) | Sends what is due. |

### <a id="m-pump"></a>Pump()

```csharp
public void Pump();
```

Sends what is due. Call it once per cluster tick, after the step.
