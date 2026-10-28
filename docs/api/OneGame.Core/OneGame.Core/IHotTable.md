[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# IHotTable interface

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public interface IHotTable
```

A struct-of-arrays table the world snapshots and hashes. The generator emits one for each [`HotDataAttribute`](HotDataAttribute.md) struct.

## Properties

| Member | Description |
| --- | --- |
| [`Count`](#m-count) | The number of rows. |
| [`Name`](#m-name) | The table's name in snapshots; unique within a world. |
| [`Visibility`](#m-visibility) | Whether subscribers receive the table: [`Visibility.Everyone`](Visibility.md#m-everyone) or [`Visibility.Hidden`](Visibility.md#m-hidden). |

### <a id="m-count"></a>Count

```csharp
int Count { get; }
```

The number of rows.

**Type:** `int`

### <a id="m-name"></a>Name

```csharp
string Name { get; }
```

The table's name in snapshots; unique within a world.

**Type:** `string`

### <a id="m-visibility"></a>Visibility

```csharp
Visibility Visibility { get; }
```

Whether subscribers receive the table: [`Visibility.Everyone`](Visibility.md#m-everyone) or [`Visibility.Hidden`](Visibility.md#m-hidden).

**Type:** [`Visibility`](Visibility.md)

## Methods

| Member | Description |
| --- | --- |
| [`Clear()`](#m-clear) | Removes every row. |
| [`CopyFrom(IHotTable)`](#m-copyfrom-ihottable) | Makes this table hold exactly the rows of `source`, a table of the same type. |
| [`Read(ref SyncReader)`](#m-read-syncreader) | Replaces all rows. |
| [`TrySetFromText(int, string, string)`](#m-trysetfromtext-int-string-string) | Sets one scalar cell from text (numbers, bool, string, Fixed, enums by number), for the harness. |
| [`Write(SyncWriter)`](#m-write-syncwriter) | Writes all rows. |
| [`WriteHash(SyncWriter)`](#m-writehash-syncwriter) | Writes what the world hash and state digests cover. |
| [`WriteJson(JsonOut)`](#m-writejson-jsonout) | Writes the rows as a JSON array of objects. |

### <a id="m-clear"></a>Clear()

```csharp
void Clear();
```

Removes every row.

### <a id="m-copyfrom-ihottable"></a>CopyFrom(IHotTable)

```csharp
void CopyFrom(IHotTable source);
```

Makes this table hold exactly the rows of `source`, a table of the same type. Generated tables copy the columns in place without allocating once they have room; the default goes through the encoded form.

**Parameters**

- `source` ([`IHotTable`](IHotTable.md))

### <a id="m-read-syncreader"></a>Read(ref SyncReader)

```csharp
void Read(ref SyncReader reader);
```

Replaces all rows. All rows are decoded before any is stored, so an error leaves the table unchanged.

**Parameters**

- `reader` ([`SyncReader`](SyncReader.md))

### <a id="m-trysetfromtext-int-string-string"></a>TrySetFromText(int, string, string)

```csharp
bool TrySetFromText(int row, string column, string text);
```

Sets one scalar cell from text (numbers, bool, string, Fixed, enums by number), for the harness. The change bypasses commands.

**Parameters**

- `row` (`int`): The row index.
- `column` (`string`): The column (field) name.
- `text` (`string`): The new value.

**Returns:** `bool` — False when the row or column does not exist, the column has no text form, or the text does not parse. Generated tables implement this; a hand-written table does not support it.

### <a id="m-write-syncwriter"></a>Write(SyncWriter)

```csharp
void Write(SyncWriter writer);
```

Writes all rows.

**Parameters**

- `writer` ([`SyncWriter`](SyncWriter.md))

### <a id="m-writehash-syncwriter"></a>WriteHash(SyncWriter)

```csharp
void WriteHash(SyncWriter writer);
```

Writes what the world hash and state digests cover. The default is the whole table; a table whose full encoding is large can write a digest of itself here (the hash must change whenever the state does).

**Parameters**

- `writer` ([`SyncWriter`](SyncWriter.md)): Receives the bytes.

### <a id="m-writejson-jsonout"></a>WriteJson(JsonOut)

```csharp
void WriteJson(JsonOut json);
```

Writes the rows as a JSON array of objects.

**Parameters**

- `json` ([`JsonOut`](JsonOut.md))
