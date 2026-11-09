[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# AtlasInfo record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`AtlasInfo`](AtlasInfo.md)&gt;

```csharp
public sealed record AtlasInfo : IEquatable<AtlasInfo>
```

The art atlas (SVG and image pages) as reported by `info`.

## Constructors

| Member | Description |
| --- | --- |
| [`AtlasInfo(int, int, int, int, int, long, long, long, int, long, string?)`](#m-ctor-int-int-int-int-int-long-long-long-int-long-string) | The art atlas (SVG and image pages) as reported by `info`. |

### <a id="m-ctor-int-int-int-int-int-long-long-long-int-long-string"></a>AtlasInfo(int, int, int, int, int, long, long, long, int, long, string?)

```csharp
public AtlasInfo(int Pages, int MaxPages, int PageSize, int Entries, int Standalone, long StandaloneBytes, long StandaloneLimit, long Overflowed, int PageClears, long Refusals, string? LastRefusal);
```

The art atlas (SVG and image pages) as reported by `info`.

**Parameters**

- `Pages` (`int`): Pages in use.
- `MaxPages` (`int`): Pages the atlas may grow to.
- `PageSize` (`int`): Page edge in pixels.
- `Entries` (`int`): Rasterized entries held, on pages and standalone.
- `Standalone` (`int`): Standalone textures held (big rasters and overflow from full pages).
- `StandaloneBytes` (`long`): GPU bytes the standalone textures take.
- `StandaloneLimit` (`long`): The standalone texture budget in bytes.
- `Overflowed` (`long`): Rasters that got their own texture because every page was in use in the frame, since start.
- `PageClears` (`int`): Pages cleared to make room, since start.
- `Refusals` (`long`): Images that could not be drawn (atlas full for the frame, too large, undecodable), since start. Anything above zero is a defect.
- `LastRefusal` (`string?`): What the latest refusal was, or null.

## Properties

| Member | Description |
| --- | --- |
| [`Entries`](#m-entries) | Rasterized entries held, on pages and standalone. |
| [`LastRefusal`](#m-lastrefusal) | What the latest refusal was, or null. |
| [`MaxPages`](#m-maxpages) | Pages the atlas may grow to. |
| [`Overflowed`](#m-overflowed) | Rasters that got their own texture because every page was in use in the frame, since start. |
| [`PageClears`](#m-pageclears) | Pages cleared to make room, since start. |
| [`PageSize`](#m-pagesize) | Page edge in pixels. |
| [`Pages`](#m-pages) | Pages in use. |
| [`Refusals`](#m-refusals) | Images that could not be drawn (atlas full for the frame, too large, undecodable), since start. |
| [`Standalone`](#m-standalone) | Standalone textures held (big rasters and overflow from full pages). |
| [`StandaloneBytes`](#m-standalonebytes) | GPU bytes the standalone textures take. |
| [`StandaloneLimit`](#m-standalonelimit) | The standalone texture budget in bytes. |

### <a id="m-entries"></a>Entries

```csharp
public int Entries { get; init; }
```

Rasterized entries held, on pages and standalone.

**Type:** `int`

### <a id="m-lastrefusal"></a>LastRefusal

```csharp
public string? LastRefusal { get; init; }
```

What the latest refusal was, or null.

**Type:** `string?`

### <a id="m-maxpages"></a>MaxPages

```csharp
public int MaxPages { get; init; }
```

Pages the atlas may grow to.

**Type:** `int`

### <a id="m-overflowed"></a>Overflowed

```csharp
public long Overflowed { get; init; }
```

Rasters that got their own texture because every page was in use in the frame, since start.

**Type:** `long`

### <a id="m-pageclears"></a>PageClears

```csharp
public int PageClears { get; init; }
```

Pages cleared to make room, since start.

**Type:** `int`

### <a id="m-pagesize"></a>PageSize

```csharp
public int PageSize { get; init; }
```

Page edge in pixels.

**Type:** `int`

### <a id="m-pages"></a>Pages

```csharp
public int Pages { get; init; }
```

Pages in use.

**Type:** `int`

### <a id="m-refusals"></a>Refusals

```csharp
public long Refusals { get; init; }
```

Images that could not be drawn (atlas full for the frame, too large, undecodable), since start. Anything above zero is a defect.

**Type:** `long`

### <a id="m-standalone"></a>Standalone

```csharp
public int Standalone { get; init; }
```

Standalone textures held (big rasters and overflow from full pages).

**Type:** `int`

### <a id="m-standalonebytes"></a>StandaloneBytes

```csharp
public long StandaloneBytes { get; init; }
```

GPU bytes the standalone textures take.

**Type:** `long`

### <a id="m-standalonelimit"></a>StandaloneLimit

```csharp
public long StandaloneLimit { get; init; }
```

The standalone texture budget in bytes.

**Type:** `long`
