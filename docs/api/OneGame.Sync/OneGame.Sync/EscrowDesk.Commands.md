[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# EscrowDesk.Commands struct

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)  
**Declared in:** [`EscrowDesk`](EscrowDesk.md)

```csharp
public readonly struct EscrowDesk.Commands
```

Sends commands to this entity. Each call encodes the arguments and queues the command on the authority.

## Methods

| Member | Description |
| --- | --- |
| [`Abort(ulong, int)`](#m-abort-ulong-int) | Sends `Abort` to the authority; the handler runs there on the next tick. |
| [`Credit(ulong, int, uint, uint, long)`](#m-credit-ulong-int-uint-uint-long) | Sends `Credit` to the authority; the handler runs there on the next tick. |
| [`Open(ulong, uint, uint, uint, uint, long, uint, uint, uint, uint, long)`](#m-open-ulong-uint-uint-uint-uint-long-uint-uint-uint-uint-long) | Sends `Open` to the authority; the handler runs there on the next tick. |
| [`Report(ulong, int, int, bool)`](#m-report-ulong-int-int-bool) | Sends `Report` to the authority; the handler runs there on the next tick. |
| [`Withdraw(ulong, int, uint, uint, long)`](#m-withdraw-ulong-int-uint-uint-long) | Sends `Withdraw` to the authority; the handler runs there on the next tick. |

### <a id="m-abort-ulong-int"></a>Abort(ulong, int)

```csharp
public CommandStatus Abort(ulong tx, int leg);
```

Sends `Abort` to the authority; the handler runs there on the next tick.

**Parameters**

- `tx` (`ulong`)
- `leg` (`int`)

**Returns:** [`CommandStatus`](../../OneGame.Core/OneGame.Core/CommandStatus.md)

### <a id="m-credit-ulong-int-uint-uint-long"></a>Credit(ulong, int, uint, uint, long)

```csharp
public CommandStatus Credit(ulong tx, int leg, uint owner, uint kind, long amount);
```

Sends `Credit` to the authority; the handler runs there on the next tick.

**Parameters**

- `tx` (`ulong`)
- `leg` (`int`)
- `owner` (`uint`)
- `kind` (`uint`)
- `amount` (`long`)

**Returns:** [`CommandStatus`](../../OneGame.Core/OneGame.Core/CommandStatus.md)

### <a id="m-open-ulong-uint-uint-uint-uint-long-uint-uint-uint-uint-long"></a>Open(ulong, uint, uint, uint, uint, long, uint, uint, uint, uint, long)

```csharp
public CommandStatus Open(ulong tx, uint scopeA, uint giverA, uint receiverA, uint kindA, long amountA, uint scopeB, uint giverB, uint receiverB, uint kindB, long amountB);
```

Sends `Open` to the authority; the handler runs there on the next tick.

**Parameters**

- `tx` (`ulong`)
- `scopeA` (`uint`)
- `giverA` (`uint`)
- `receiverA` (`uint`)
- `kindA` (`uint`)
- `amountA` (`long`)
- `scopeB` (`uint`)
- `giverB` (`uint`)
- `receiverB` (`uint`)
- `kindB` (`uint`)
- `amountB` (`long`)

**Returns:** [`CommandStatus`](../../OneGame.Core/OneGame.Core/CommandStatus.md)

### <a id="m-report-ulong-int-int-bool"></a>Report(ulong, int, int, bool)

```csharp
public CommandStatus Report(ulong tx, int leg, int step, bool ok);
```

Sends `Report` to the authority; the handler runs there on the next tick.

**Parameters**

- `tx` (`ulong`)
- `leg` (`int`)
- `step` (`int`)
- `ok` (`bool`)

**Returns:** [`CommandStatus`](../../OneGame.Core/OneGame.Core/CommandStatus.md)

### <a id="m-withdraw-ulong-int-uint-uint-long"></a>Withdraw(ulong, int, uint, uint, long)

```csharp
public CommandStatus Withdraw(ulong tx, int leg, uint owner, uint kind, long amount);
```

Sends `Withdraw` to the authority; the handler runs there on the next tick.

**Parameters**

- `tx` (`ulong`)
- `leg` (`int`)
- `owner` (`uint`)
- `kind` (`uint`)
- `amount` (`long`)

**Returns:** [`CommandStatus`](../../OneGame.Core/OneGame.Core/CommandStatus.md)
