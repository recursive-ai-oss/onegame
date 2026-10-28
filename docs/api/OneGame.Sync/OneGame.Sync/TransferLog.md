[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# TransferLog class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)  
**Implements:** [`IHotTable`](../../OneGame.Core/OneGame.Core/IHotTable.md)

```csharp
public sealed class TransferLog : IHotTable
```

The state of a scope's authority transfers, kept in the world so that a save, a snapshot-based restart and a replay carry it: the bundles that left this scope and are not confirmed yet, and the ids of the transfers this scope has installed. Add it to every world that sends or receives transfers with [`TransferLog.AddTo()`](TransferLog.md#m-addto-world), in the same order on every node. It is a hidden table: subscribers never get it.

## Constructors

| Member | Description |
| --- | --- |
| [`TransferLog()`](#m-ctor) |  |

### <a id="m-ctor"></a>TransferLog()

```csharp
public TransferLog();
```

## Fields

| Member | Description |
| --- | --- |
| [`TableName`](#m-tablename) | The table's name in snapshots. |

### <a id="m-tablename"></a>TableName

```csharp
public const string TableName = "og.transfers";
```

The table's name in snapshots.

**Type:** `string`

## Properties

| Member | Description |
| --- | --- |
| [`Count`](#m-count) | The number of rows. |
| [`Name`](#m-name) | The table's name in snapshots; unique within a world. |
| [`OpenCount`](#m-opencount) | Number of outgoing transfers that are not confirmed yet. |
| [`Visibility`](#m-visibility) | Whether subscribers receive the table: [`Visibility.Everyone`](../../OneGame.Core/OneGame.Core/Visibility.md#m-everyone) or [`Visibility.Hidden`](../../OneGame.Core/OneGame.Core/Visibility.md#m-hidden). |

### <a id="m-count"></a>Count

```csharp
public int Count { get; }
```

The number of rows.

**Type:** `int`

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The table's name in snapshots; unique within a world.

**Type:** `string`

### <a id="m-opencount"></a>OpenCount

```csharp
public int OpenCount { get; }
```

Number of outgoing transfers that are not confirmed yet.

**Type:** `int`

### <a id="m-visibility"></a>Visibility

```csharp
public Visibility Visibility { get; }
```

Whether subscribers receive the table: [`Visibility.Everyone`](../../OneGame.Core/OneGame.Core/Visibility.md#m-everyone) or [`Visibility.Hidden`](../../OneGame.Core/OneGame.Core/Visibility.md#m-hidden).

**Type:** [`Visibility`](../../OneGame.Core/OneGame.Core/Visibility.md)

## Methods

| Member | Description |
| --- | --- |
| [`AddTo(World)`](#m-addto-world) | Adds a transfer log to a world and returns it. |
| [`Clear()`](#m-clear) | Removes every row. |
| [`IsDone(ulong)`](#m-isdone-ulong) | Whether this scope has installed the transfer with this id. |
| [`Of(World)`](#m-of-world) | The world's transfer log, or null if it has none. |
| [`Read(ref SyncReader)`](#m-read-syncreader) | Replaces all rows. |
| [`Write(SyncWriter)`](#m-write-syncwriter) | Writes all rows. |
| [`WriteJson(JsonOut)`](#m-writejson-jsonout) | Writes the rows as a JSON array of objects. |

### <a id="m-addto-world"></a>AddTo(World)

```csharp
public static TransferLog AddTo(World world);
```

Adds a transfer log to a world and returns it.

**Parameters**

- `world` ([`World`](../../OneGame.Core/OneGame.Core/World.md)): The world; add the table before any save is loaded.

**Returns:** [`TransferLog`](TransferLog.md)

### <a id="m-clear"></a>Clear()

```csharp
public void Clear();
```

Removes every row.

### <a id="m-isdone-ulong"></a>IsDone(ulong)

```csharp
public bool IsDone(ulong id);
```

Whether this scope has installed the transfer with this id.

**Parameters**

- `id` (`ulong`)

**Returns:** `bool`

### <a id="m-of-world"></a>Of(World)

```csharp
public static TransferLog? Of(World world);
```

The world's transfer log, or null if it has none.

**Parameters**

- `world` ([`World`](../../OneGame.Core/OneGame.Core/World.md))

**Returns:** [`TransferLog`](TransferLog.md)?

### <a id="m-read-syncreader"></a>Read(ref SyncReader)

```csharp
public void Read(ref SyncReader reader);
```

Replaces all rows. All rows are decoded before any is stored, so an error leaves the table unchanged.

**Parameters**

- `reader` ([`SyncReader`](../../OneGame.Core/OneGame.Core/SyncReader.md))

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
