[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# AssetEvent record

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`AssetEvent`](AssetEvent.md)&gt;

```csharp
public sealed record AssetEvent : IEquatable<AssetEvent>
```

One thing that happened to an asset, for the control layer and tests.

## Constructors

| Member | Description |
| --- | --- |
| [`AssetEvent(long, string, string, AssetKind, int, string?)`](#m-ctor-long-string-string-assetkind-int-string) | One thing that happened to an asset, for the control layer and tests. |

### <a id="m-ctor-long-string-string-assetkind-int-string"></a>AssetEvent(long, string, string, AssetKind, int, string?)

```csharp
public AssetEvent(long Sequence, string Type, string Path, AssetKind Kind, int Version, string? Message);
```

One thing that happened to an asset, for the control layer and tests.

**Parameters**

- `Sequence` (`long`): Counts up from 1.
- `Type` (`string`): `loaded`, `reloaded`, `failed` (a load or a reload; the old content stays), `pending` (a data file changed and waits to be applied), `applied` (pending data swapped in) or `released`.
- `Path` (`string`): The asset's path.
- `Kind` ([`AssetKind`](AssetKind.md)): The asset's kind.
- `Version` (`int`): The asset's version after the event.
- `Message` (`string?`): The error for `failed`, or a note.

## Properties

| Member | Description |
| --- | --- |
| [`Kind`](#m-kind) | The asset's kind. |
| [`Message`](#m-message) | The error for `failed`, or a note. |
| [`Path`](#m-path) | The asset's path. |
| [`Sequence`](#m-sequence) | Counts up from 1. |
| [`Type`](#m-type) | `loaded`, `reloaded`, `failed` (a load or a reload; the old content stays), `pending` (a data file changed and waits to be applied), `applied` (pending data swapped in) or `released`. |
| [`Version`](#m-version) | The asset's version after the event. |

### <a id="m-kind"></a>Kind

```csharp
public AssetKind Kind { get; init; }
```

The asset's kind.

**Type:** [`AssetKind`](AssetKind.md)

### <a id="m-message"></a>Message

```csharp
public string? Message { get; init; }
```

The error for `failed`, or a note.

**Type:** `string?`

### <a id="m-path"></a>Path

```csharp
public string Path { get; init; }
```

The asset's path.

**Type:** `string`

### <a id="m-sequence"></a>Sequence

```csharp
public long Sequence { get; init; }
```

Counts up from 1.

**Type:** `long`

### <a id="m-type"></a>Type

```csharp
public string Type { get; init; }
```

`loaded`, `reloaded`, `failed` (a load or a reload; the old content stays), `pending` (a data file changed and waits to be applied), `applied` (pending data swapped in) or `released`.

**Type:** `string`

### <a id="m-version"></a>Version

```csharp
public int Version { get; init; }
```

The asset's version after the event.

**Type:** `int`
