[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# SequenceFrame record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SequenceFrame`](SequenceFrame.md)&gt;

```csharp
public sealed record SequenceFrame : IEquatable<SequenceFrame>
```

One frame of a captured sequence.

## Constructors

| Member | Description |
| --- | --- |
| [`SequenceFrame(string, long, string, string)`](#m-ctor-string-long-string-string) | One frame of a captured sequence. |

### <a id="m-ctor-string-long-string-string"></a>SequenceFrame(string, long, string, string)

```csharp
public SequenceFrame(string File, long Tick, string Hash, string Sha256);
```

One frame of a captured sequence.

**Parameters**

- `File` (`string`): The PNG's file name, relative to the sequence folder.
- `Tick` (`long`): The world's tick for this frame.
- `Hash` (`string`): The state hash at that tick.
- `Sha256` (`string`): Hash of the PNG bytes.

## Properties

| Member | Description |
| --- | --- |
| [`File`](#m-file) | The PNG's file name, relative to the sequence folder. |
| [`Hash`](#m-hash) | The state hash at that tick. |
| [`Sha256`](#m-sha256) | Hash of the PNG bytes. |
| [`Tick`](#m-tick) | The world's tick for this frame. |

### <a id="m-file"></a>File

```csharp
public string File { get; init; }
```

The PNG's file name, relative to the sequence folder.

**Type:** `string`

### <a id="m-hash"></a>Hash

```csharp
public string Hash { get; init; }
```

The state hash at that tick.

**Type:** `string`

### <a id="m-sha256"></a>Sha256

```csharp
public string Sha256 { get; init; }
```

Hash of the PNG bytes.

**Type:** `string`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The world's tick for this frame.

**Type:** `long`
