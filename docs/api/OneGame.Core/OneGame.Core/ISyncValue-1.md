[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# ISyncValue&lt;TSelf&gt; interface

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)`<TSelf>`

```csharp
public interface ISyncValue<TSelf> : IEquatable<TSelf> where TSelf : ISyncValue<TSelf>
```

A value type with a binary codec. Generated code, [`Fixed`](Fixed.md), [`EntityId`](EntityId.md) and other engine value types implement it, and a synced property may have any type that does.

**Type parameters**

- `TSelf`: The implementing type.

## Methods

| Member | Description |
| --- | --- |
| [`Read(ref SyncReader)`](#m-read-syncreader) | Reads a value written by [`ISyncValue<TSelf>.Write()`](ISyncValue-1.md#m-write-syncwriter). |
| [`Write(SyncWriter)`](#m-write-syncwriter) | Writes the value in the sync format. |
| [`WriteJson(JsonOut)`](#m-writejson-jsonout) | Writes the value as JSON for inspection. |

### <a id="m-read-syncreader"></a>Read(ref SyncReader)

```csharp
static abstract TSelf Read(ref SyncReader reader);
```

Reads a value written by [`ISyncValue<TSelf>.Write()`](ISyncValue-1.md#m-write-syncwriter). Problems are recorded on the reader, and a default value is returned.

**Parameters**

- `reader` ([`SyncReader`](SyncReader.md))

**Returns:** `TSelf`

### <a id="m-write-syncwriter"></a>Write(SyncWriter)

```csharp
void Write(SyncWriter writer);
```

Writes the value in the sync format.

**Parameters**

- `writer` ([`SyncWriter`](SyncWriter.md))

### <a id="m-writejson-jsonout"></a>WriteJson(JsonOut)

```csharp
void WriteJson(JsonOut json);
```

Writes the value as JSON for inspection.

**Parameters**

- `json` ([`JsonOut`](JsonOut.md))
