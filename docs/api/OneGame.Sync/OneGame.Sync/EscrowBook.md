[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# EscrowBook class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)  
**Implements:** [`IHotTable`](../../OneGame.Core/OneGame.Core/IHotTable.md)

```csharp
public sealed class EscrowBook : IHotTable
```

The escrow records of a scope, kept in its world (so saves, snapshots and replays carry them): on the coordinator, the trades; on every participant, a receipt for each step it took, keyed by transaction and leg. A step that finds its receipt does nothing and answers the same as the first time, which is what makes retries, duplicates and reordering harmless. Add it with [`Escrow.Install()`](Escrow.md#m-install-world-iescrowhandler).

## Fields

| Member | Description |
| --- | --- |
| [`DefaultCollectTicks`](#m-defaultcollectticks) | Ticks the coordinator waits for both values to be held before it cancels. |
| [`TableName`](#m-tablename) | The table's name in snapshots. |

### <a id="m-defaultcollectticks"></a>DefaultCollectTicks

```csharp
public const int DefaultCollectTicks = 300;
```

Ticks the coordinator waits for both values to be held before it cancels. Default 300.

**Type:** `int`

### <a id="m-tablename"></a>TableName

```csharp
public const string TableName = "og.escrow";
```

The table's name in snapshots.

**Type:** `string`

## Properties

| Member | Description |
| --- | --- |
| [`CollectTicks`](#m-collectticks) | Ticks the coordinator waits for both values before cancelling. |
| [`Count`](#m-count) | The number of rows. |
| [`Handler`](#m-handler) | How the game debits and credits its holdings. |
| [`Name`](#m-name) | The table's name in snapshots; unique within a world. |
| [`Trades`](#m-trades) | The trades this scope coordinates, oldest first. |
| [`Visibility`](#m-visibility) | Whether subscribers receive the table: [`Visibility.Everyone`](../../OneGame.Core/OneGame.Core/Visibility.md#m-everyone) or [`Visibility.Hidden`](../../OneGame.Core/OneGame.Core/Visibility.md#m-hidden). |

### <a id="m-collectticks"></a>CollectTicks

```csharp
public int CollectTicks { get; set; }
```

Ticks the coordinator waits for both values before cancelling.

**Type:** `int`

### <a id="m-count"></a>Count

```csharp
public int Count { get; }
```

The number of rows.

**Type:** `int`

### <a id="m-handler"></a>Handler

```csharp
public IEscrowHandler Handler { get; }
```

How the game debits and credits its holdings.

**Type:** [`IEscrowHandler`](IEscrowHandler.md)

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The table's name in snapshots; unique within a world.

**Type:** `string`

### <a id="m-trades"></a>Trades

```csharp
public IReadOnlyList<EscrowTrade> Trades { get; }
```

The trades this scope coordinates, oldest first.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`EscrowTrade`](EscrowTrade.md)&gt;

### <a id="m-visibility"></a>Visibility

```csharp
public Visibility Visibility { get; }
```

Whether subscribers receive the table: [`Visibility.Everyone`](../../OneGame.Core/OneGame.Core/Visibility.md#m-everyone) or [`Visibility.Hidden`](../../OneGame.Core/OneGame.Core/Visibility.md#m-hidden).

**Type:** [`Visibility`](../../OneGame.Core/OneGame.Core/Visibility.md)

## Methods

| Member | Description |
| --- | --- |
| [`Abort(ulong, int)`](#m-abort-ulong-int) | Makes sure a withdraw that has not happened never will. |
| [`Advance(long)`](#m-advance-long) | Moves every trade along: timeouts, decisions, completion. |
| [`Clear()`](#m-clear) | Removes every row. |
| [`Credit(ulong, int, uint, uint, long)`](#m-credit-ulong-int-uint-uint-long) | Credits a receiver once. |
| [`FindTrade(ulong)`](#m-findtrade-ulong) | The trade with this id, or null. |
| [`InFlight(IEnumerable<EscrowBook>)`](#m-inflight-ienumerable-escrowbook) | The value that has left a giver and not yet arrived, by kind, over the books of all the scopes: a withdraw receipt with no credit receipt for the same transaction and leg anywhere. |
| [`Of(World)`](#m-of-world) | The book of a world, or null. |
| [`Open(ulong, long, EscrowLeg, EscrowLeg)`](#m-open-ulong-long-escrowleg-escrowleg) | Opens a trade. |
| [`Read(ref SyncReader)`](#m-read-syncreader) | Replaces all rows. |
| [`ReceiptCount(EscrowStep)`](#m-receiptcount-escrowstep) | How many receipts of a step this book holds (for tests and dashboards). |
| [`ReportDelivered(ulong, int, long)`](#m-reportdelivered-ulong-int-long) | A participant confirmed a credit. |
| [`ReportHold(ulong, int, bool, long)`](#m-reporthold-ulong-int-bool-long) | A participant answered a withdraw (`held`: the value is held, otherwise it is not and will not be) or an abort (`held` false: stopped; true: it was already taken). |
| [`Withdraw(ulong, int, uint, uint, long)`](#m-withdraw-ulong-int-uint-uint-long) | Takes a giver's value and keeps a receipt. |
| [`Write(SyncWriter)`](#m-write-syncwriter) | Writes all rows. |
| [`WriteJson(JsonOut)`](#m-writejson-jsonout) | Writes the rows as a JSON array of objects. |

### <a id="m-abort-ulong-int"></a>Abort(ulong, int)

```csharp
public EscrowReply Abort(ulong tx, int leg);
```

Makes sure a withdraw that has not happened never will. Answers ok if it never happened, "held" if the value was already taken.

**Parameters**

- `tx` (`ulong`)
- `leg` (`int`)

**Returns:** [`EscrowReply`](EscrowReply.md)

### <a id="m-advance-long"></a>Advance(long)

```csharp
public void Advance(long tick);
```

Moves every trade along: timeouts, decisions, completion. Runs once per tick on the coordinator's desk.

**Parameters**

- `tick` (`long`)

### <a id="m-clear"></a>Clear()

```csharp
public void Clear();
```

Removes every row.

### <a id="m-credit-ulong-int-uint-uint-long"></a>Credit(ulong, int, uint, uint, long)

```csharp
public EscrowReply Credit(ulong tx, int leg, uint owner, uint kind, long amount);
```

Credits a receiver once. Repeats answer ok without crediting again.

**Parameters**

- `tx` (`ulong`)
- `leg` (`int`)
- `owner` (`uint`)
- `kind` (`uint`)
- `amount` (`long`)

**Returns:** [`EscrowReply`](EscrowReply.md)

### <a id="m-findtrade-ulong"></a>FindTrade(ulong)

```csharp
public EscrowTrade? FindTrade(ulong tx);
```

The trade with this id, or null.

**Parameters**

- `tx` (`ulong`)

**Returns:** [`EscrowTrade`](EscrowTrade.md)?

### <a id="m-inflight-ienumerable-escrowbook"></a>InFlight(IEnumerable&lt;EscrowBook&gt;)

```csharp
public static SortedDictionary<uint, long> InFlight(IEnumerable<EscrowBook> books);
```

The value that has left a giver and not yet arrived, by kind, over the books of all the scopes: a withdraw receipt with no credit receipt for the same transaction and leg anywhere. Holdings plus this sum is constant at every tick; a test asserts that.

**Parameters**

- `books` ([`IEnumerable`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable-1)&lt;[`EscrowBook`](EscrowBook.md)&gt;): The books of every participating scope.

**Returns:** [`SortedDictionary`](https://learn.microsoft.com/dotnet/api/system.collections.generic.sorteddictionary-2)`<uint, long>`

### <a id="m-of-world"></a>Of(World)

```csharp
public static EscrowBook? Of(World world);
```

The book of a world, or null.

**Parameters**

- `world` ([`World`](../../OneGame.Core/OneGame.Core/World.md))

**Returns:** [`EscrowBook`](EscrowBook.md)?

### <a id="m-open-ulong-long-escrowleg-escrowleg"></a>Open(ulong, long, EscrowLeg, EscrowLeg)

```csharp
public EscrowReply Open(ulong tx, long tick, EscrowLeg a, EscrowLeg b);
```

Opens a trade. A repeat of an open transaction id is accepted and changes nothing.

**Parameters**

- `tx` (`ulong`)
- `tick` (`long`)
- `a` ([`EscrowLeg`](EscrowLeg.md))
- `b` ([`EscrowLeg`](EscrowLeg.md))

**Returns:** [`EscrowReply`](EscrowReply.md)

### <a id="m-read-syncreader"></a>Read(ref SyncReader)

```csharp
public void Read(ref SyncReader reader);
```

Replaces all rows. All rows are decoded before any is stored, so an error leaves the table unchanged.

**Parameters**

- `reader` ([`SyncReader`](../../OneGame.Core/OneGame.Core/SyncReader.md))

### <a id="m-receiptcount-escrowstep"></a>ReceiptCount(EscrowStep)

```csharp
public int ReceiptCount(EscrowStep step);
```

How many receipts of a step this book holds (for tests and dashboards).

**Parameters**

- `step` ([`EscrowStep`](EscrowStep.md))

**Returns:** `int`

### <a id="m-reportdelivered-ulong-int-long"></a>ReportDelivered(ulong, int, long)

```csharp
public void ReportDelivered(ulong tx, int leg, long tick);
```

A participant confirmed a credit.

**Parameters**

- `tx` (`ulong`)
- `leg` (`int`)
- `tick` (`long`)

### <a id="m-reporthold-ulong-int-bool-long"></a>ReportHold(ulong, int, bool, long)

```csharp
public void ReportHold(ulong tx, int leg, bool held, long tick);
```

A participant answered a withdraw (`held`: the value is held, otherwise it is not and will not be) or an abort (`held` false: stopped; true: it was already taken).

**Parameters**

- `tx` (`ulong`)
- `leg` (`int`)
- `held` (`bool`)
- `tick` (`long`)

### <a id="m-withdraw-ulong-int-uint-uint-long"></a>Withdraw(ulong, int, uint, uint, long)

```csharp
public EscrowReply Withdraw(ulong tx, int leg, uint owner, uint kind, long amount);
```

Takes a giver's value and keeps a receipt. Repeats answer ok without taking anything again.

**Parameters**

- `tx` (`ulong`)
- `leg` (`int`)
- `owner` (`uint`)
- `kind` (`uint`)
- `amount` (`long`)

**Returns:** [`EscrowReply`](EscrowReply.md)

### <a id="m-write-syncwriter"></a>Write(SyncWriter)

```csharp
public void Write(SyncWriter writer);
```

Writes all rows.

**Parameters**

- `writer` ([`SyncWriter`](../../OneGame.Core/OneGame.Core/SyncWriter.md))

### <a id="m-writejson-jsonout"></a>WriteJson(JsonOut)

```csharp
public void WriteJson(JsonOut json);
```

Writes the rows as a JSON array of objects.

**Parameters**

- `json` ([`JsonOut`](../../OneGame.Core/OneGame.Core/JsonOut.md))
