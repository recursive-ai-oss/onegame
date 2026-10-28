[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu.Graph

# ResourceReport record

**Namespace:** `OneGame.Gpu.Graph`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ResourceReport`](ResourceReport.md)&gt;

```csharp
public sealed record ResourceReport : IEquatable<ResourceReport>
```

One texture or buffer of the last frame.

## Constructors

| Member | Description |
| --- | --- |
| [`ResourceReport(string, string, bool, bool, string?, string, ulong, int, string?, string?, string)`](#m-ctor-string-string-bool-bool-string-string-ulong-int-string-string-string) | One texture or buffer of the last frame. |

### <a id="m-ctor-string-string-bool-bool-string-string-ulong-int-string-string-string"></a>ResourceReport(string, string, bool, bool, string?, string, ulong, int, string?, string?, string)

```csharp
public ResourceReport(string Name, string Kind, bool Imported, bool Used, string? Format, string Size, ulong Bytes, int Slot, string? FirstPass, string? LastPass, string Usage);
```

One texture or buffer of the last frame.

**Parameters**

- `Name` (`string`): Resource name.
- `Kind` (`string`): `texture` or `buffer`.
- `Imported` (`bool`): True when the graph does not own it.
- `Used` (`bool`): True when a pass that ran touches it (always true for imports).
- `Format` (`string?`): Texture format; null for buffers.
- `Size` (`string`): Texture size as WxH, WxHxLayers or WxHxDepth; the byte size for buffers.
- `Bytes` (`ulong`): Memory it needs, counting all mips, layers and samples.
- `Slot` (`int`): Physical allocation it lives in; resources with the same slot share memory. -1 for imports and unused resources.
- `FirstPass` (`string?`): First pass that ran and touched it.
- `LastPass` (`string?`): Last pass that ran and touched it.
- `Usage` (`string`): Flags the memory was created with.

## Properties

| Member | Description |
| --- | --- |
| [`Bytes`](#m-bytes) | Memory it needs, counting all mips, layers and samples. |
| [`FirstPass`](#m-firstpass) | First pass that ran and touched it. |
| [`Format`](#m-format) | Texture format; null for buffers. |
| [`Imported`](#m-imported) | True when the graph does not own it. |
| [`Kind`](#m-kind) | `texture` or `buffer`. |
| [`LastPass`](#m-lastpass) | Last pass that ran and touched it. |
| [`Name`](#m-name) | Resource name. |
| [`Size`](#m-size) | Texture size as WxH, WxHxLayers or WxHxDepth; the byte size for buffers. |
| [`Slot`](#m-slot) | Physical allocation it lives in; resources with the same slot share memory. -1 for imports and unused resources. |
| [`Usage`](#m-usage) | Flags the memory was created with. |
| [`Used`](#m-used) | True when a pass that ran touches it (always true for imports). |

### <a id="m-bytes"></a>Bytes

```csharp
public ulong Bytes { get; init; }
```

Memory it needs, counting all mips, layers and samples.

**Type:** `ulong`

### <a id="m-firstpass"></a>FirstPass

```csharp
public string? FirstPass { get; init; }
```

First pass that ran and touched it.

**Type:** `string?`

### <a id="m-format"></a>Format

```csharp
public string? Format { get; init; }
```

Texture format; null for buffers.

**Type:** `string?`

### <a id="m-imported"></a>Imported

```csharp
public bool Imported { get; init; }
```

True when the graph does not own it.

**Type:** `bool`

### <a id="m-kind"></a>Kind

```csharp
public string Kind { get; init; }
```

`texture` or `buffer`.

**Type:** `string`

### <a id="m-lastpass"></a>LastPass

```csharp
public string? LastPass { get; init; }
```

Last pass that ran and touched it.

**Type:** `string?`

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

Resource name.

**Type:** `string`

### <a id="m-size"></a>Size

```csharp
public string Size { get; init; }
```

Texture size as WxH, WxHxLayers or WxHxDepth; the byte size for buffers.

**Type:** `string`

### <a id="m-slot"></a>Slot

```csharp
public int Slot { get; init; }
```

Physical allocation it lives in; resources with the same slot share memory. -1 for imports and unused resources.

**Type:** `int`

### <a id="m-usage"></a>Usage

```csharp
public string Usage { get; init; }
```

Flags the memory was created with.

**Type:** `string`

### <a id="m-used"></a>Used

```csharp
public bool Used { get; init; }
```

True when a pass that ran touches it (always true for imports).

**Type:** `bool`
