[API reference](../../index.md) › [OneGame.Atlas](../index.md) › OneGame.Atlas

# AtlasPages class

**Namespace:** `OneGame.Atlas`  
**Package:** [`RecursiveAI.OneGame.Atlas`](../index.md)

```csharp
public sealed class AtlasPages
```

A growing set of equal-sized pages with a shelf packer. Entries are never moved. When every page is full, the page that was used least recently is cleared as a whole (every entry on it is gone, [`AtlasPages.Epoch`](AtlasPages.md#m-epoch) grows, and the owner drops them); pages in use during the current frame are never cleared, so a request that cannot fit is refused instead.

## Constructors

| Member | Description |
| --- | --- |
| [`AtlasPages(AtlasFormat, AtlasOptions?)`](#m-ctor-atlasformat-atlasoptions) | Creates an empty atlas; pages are allocated when the first entry needs them. |

### <a id="m-ctor-atlasformat-atlasoptions"></a>AtlasPages(AtlasFormat, AtlasOptions?)

```csharp
public AtlasPages(AtlasFormat format, AtlasOptions? options = null);
```

Creates an empty atlas; pages are allocated when the first entry needs them.

**Parameters**

- `format` ([`AtlasFormat`](AtlasFormat.md)): Pixel layout of every page.
- `options` ([`AtlasOptions`](AtlasOptions.md)?): Sizes; the defaults when `null`.

## Properties

| Member | Description |
| --- | --- |
| [`Epoch`](#m-epoch) | Gets a counter that grows whenever a page is cleared. |
| [`Format`](#m-format) | Gets the pixel layout of every page. |
| [`Frame`](#m-frame) | Gets the current frame number, as set by [`AtlasPages.BeginFrame()`](AtlasPages.md#m-beginframe-long). |
| [`MaxEntrySize`](#m-maxentrysize) | Gets the largest entry edge that can ever fit: the page minus both paddings. |
| [`PageCount`](#m-pagecount) | Gets the pages created so far, in index order. |
| [`PageSize`](#m-pagesize) | Gets the page edge in pixels. |
| [`this[int]`](#m-item-int) | Returns a page. |

### <a id="m-epoch"></a>Epoch

```csharp
public int Epoch { get; }
```

Gets a counter that grows whenever a page is cleared. Quads built from earlier lookups must be rebuilt when it changes.

**Type:** `int`

### <a id="m-format"></a>Format

```csharp
public AtlasFormat Format { get; }
```

Gets the pixel layout of every page.

**Type:** [`AtlasFormat`](AtlasFormat.md)

### <a id="m-frame"></a>Frame

```csharp
public long Frame { get; }
```

Gets the current frame number, as set by [`AtlasPages.BeginFrame()`](AtlasPages.md#m-beginframe-long).

**Type:** `long`

### <a id="m-maxentrysize"></a>MaxEntrySize

```csharp
public int MaxEntrySize { get; }
```

Gets the largest entry edge that can ever fit: the page minus both paddings.

**Type:** `int`

### <a id="m-pagecount"></a>PageCount

```csharp
public int PageCount { get; }
```

Gets the pages created so far, in index order.

**Type:** `int`

### <a id="m-pagesize"></a>PageSize

```csharp
public int PageSize { get; }
```

Gets the page edge in pixels.

**Type:** `int`

### <a id="m-item-int"></a>this\[int\]

```csharp
public AtlasPage this[int index] { get; }
```

Returns a page.

**Parameters**

- `index` (`int`): Page index below [`AtlasPages.PageCount`](AtlasPages.md#m-pagecount).

**Type:** [`AtlasPage`](AtlasPage.md)

## Methods

| Member | Description |
| --- | --- |
| [`BeginFrame(long)`](#m-beginframe-long) | Starts a frame. |
| [`Touch(int)`](#m-touch-int) | Marks a page as used in the current frame; call it for every entry looked up. |
| [`TryAllocate(int, int, out AtlasSlot, out int)`](#m-tryallocate-int-int-atlasslot-int) | Reserves a rectangle. |
| [`Upload(AtlasSlot, int, int, ReadOnlySpan<byte>, int)`](#m-upload-atlasslot-int-int-readonlyspan-byte-int) | Copies pixels into a reserved slot and marks the region dirty. |

### <a id="m-beginframe-long"></a>BeginFrame(long)

```csharp
public void BeginFrame(long frame);
```

Starts a frame. Pages touched in this frame are protected from clearing until the next call.

**Parameters**

- `frame` (`long`): A number that grows with every frame.

### <a id="m-touch-int"></a>Touch(int)

```csharp
public void Touch(int page);
```

Marks a page as used in the current frame; call it for every entry looked up.

**Parameters**

- `page` (`int`): Page index.

### <a id="m-tryallocate-int-int-atlasslot-int"></a>TryAllocate(int, int, out AtlasSlot, out int)

```csharp
public bool TryAllocate(int width, int height, out AtlasSlot slot, out int clearedPage);
```

Reserves a rectangle.

**Parameters**

- `width` (`int`): Usable width, at most [`AtlasPages.MaxEntrySize`](AtlasPages.md#m-maxentrysize).
- `height` (`int`): Usable height, at most [`AtlasPages.MaxEntrySize`](AtlasPages.md#m-maxentrysize).
- `slot` ([`AtlasSlot`](AtlasSlot.md)): Where it went.
- `clearedPage` (`int`): The page that had to be cleared to make room, or -1. The owner removes every entry on that page.

**Returns:** `bool` — False when the request is too large or nothing can be cleared now.

### <a id="m-upload-atlasslot-int-int-readonlyspan-byte-int"></a>Upload(AtlasSlot, int, int, ReadOnlySpan&lt;byte&gt;, int)

```csharp
public void Upload(AtlasSlot slot, int width, int height, ReadOnlySpan<byte> source, int sourceStride);
```

Copies pixels into a reserved slot and marks the region dirty.

**Parameters**

- `slot` ([`AtlasSlot`](AtlasSlot.md)): A slot from [`AtlasPages.TryAllocate()`](AtlasPages.md#m-tryallocate-int-int-atlasslot-int).
- `width` (`int`): Width of the pixels to copy.
- `height` (`int`): Height of the pixels to copy.
- `source` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`): Source rows.
- `sourceStride` (`int`): Bytes per source row.
