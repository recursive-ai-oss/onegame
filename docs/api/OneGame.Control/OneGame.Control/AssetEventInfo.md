[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# AssetEventInfo record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`AssetEventInfo`](AssetEventInfo.md)&gt;

```csharp
public sealed record AssetEventInfo : IEquatable<AssetEventInfo>
```

One asset event in the `assets` report.

## Constructors

| Member | Description |
| --- | --- |
| [`AssetEventInfo(long, string, string, string, int, string?)`](#m-ctor-long-string-string-string-int-string) | One asset event in the `assets` report. |

### <a id="m-ctor-long-string-string-string-int-string"></a>AssetEventInfo(long, string, string, string, int, string?)

```csharp
public AssetEventInfo(long Sequence, string Type, string Path, string Kind, int Version, string? Message);
```

One asset event in the `assets` report.

**Parameters**

- `Sequence` (`long`): Counts up from 1.
- `Type` (`string`): `loaded`, `reloaded`, `failed`, `pending`, `applied` or `released`.
- `Path` (`string`): The asset path.
- `Kind` (`string`): The asset kind.
- `Version` (`int`): The version after the event.
- `Message` (`string?`): The error or note.

## Properties

| Member | Description |
| --- | --- |
| [`Kind`](#m-kind) | The asset kind. |
| [`Message`](#m-message) | The error or note. |
| [`Path`](#m-path) | The asset path. |
| [`Sequence`](#m-sequence) | Counts up from 1. |
| [`Type`](#m-type) | `loaded`, `reloaded`, `failed`, `pending`, `applied` or `released`. |
| [`Version`](#m-version) | The version after the event. |

### <a id="m-kind"></a>Kind

```csharp
public string Kind { get; init; }
```

The asset kind.

**Type:** `string`

### <a id="m-message"></a>Message

```csharp
public string? Message { get; init; }
```

The error or note.

**Type:** `string?`

### <a id="m-path"></a>Path

```csharp
public string Path { get; init; }
```

The asset path.

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

`loaded`, `reloaded`, `failed`, `pending`, `applied` or `released`.

**Type:** `string`

### <a id="m-version"></a>Version

```csharp
public int Version { get; init; }
```

The version after the event.

**Type:** `int`
