[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# IEscrowHandler interface

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public interface IEscrowHandler
```

The game's side of an escrow: how value leaves and enters its holdings. Both calls run inside a scope's simulation (a command on the [`EscrowDesk`](EscrowDesk.md)), so they must be deterministic and must change only that scope's state.

## Methods

| Member | Description |
| --- | --- |
| [`Credit(World, uint, uint, long)`](#m-credit-world-uint-uint-long) | Adds `amount` of `kind` to `owner`'s holdings. |
| [`TryDebit(World, uint, uint, long)`](#m-trydebit-world-uint-uint-long) | Takes `amount` of `kind` out of `owner`'s holdings. |

### <a id="m-credit-world-uint-uint-long"></a>Credit(World, uint, uint, long)

```csharp
bool Credit(World world, uint owner, uint kind, long amount);
```

Adds `amount` of `kind` to `owner`'s holdings. Returns false, changing nothing, if there is no such owner here.

**Parameters**

- `world` ([`World`](../../OneGame.Core/OneGame.Core/World.md))
- `owner` (`uint`)
- `kind` (`uint`)
- `amount` (`long`)

**Returns:** `bool`

### <a id="m-trydebit-world-uint-uint-long"></a>TryDebit(World, uint, uint, long)

```csharp
bool TryDebit(World world, uint owner, uint kind, long amount);
```

Takes `amount` of `kind` out of `owner`'s holdings. Returns false, changing nothing, if the owner does not have it.

**Parameters**

- `world` ([`World`](../../OneGame.Core/OneGame.Core/World.md))
- `owner` (`uint`)
- `kind` (`uint`)
- `amount` (`long`)

**Returns:** `bool`
