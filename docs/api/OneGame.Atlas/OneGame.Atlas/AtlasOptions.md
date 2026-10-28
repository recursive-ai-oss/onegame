[API reference](../../index.md) › [OneGame.Atlas](../index.md) › OneGame.Atlas

# AtlasOptions class

**Namespace:** `OneGame.Atlas`  
**Package:** [`RecursiveAI.OneGame.Atlas`](../index.md)

```csharp
public sealed class AtlasOptions
```

How an atlas is sized.

## Constructors

| Member | Description |
| --- | --- |
| [`AtlasOptions()`](#m-ctor) |  |

### <a id="m-ctor"></a>AtlasOptions()

```csharp
public AtlasOptions();
```

## Fields

| Member | Description |
| --- | --- |
| [`MaxPageSize`](#m-maxpagesize) | The largest page edge accepted. |
| [`MinPageSize`](#m-minpagesize) | The smallest page edge accepted. |

### <a id="m-maxpagesize"></a>MaxPageSize

```csharp
public const int MaxPageSize = 4096;
```

The largest page edge accepted.

**Type:** `int`

### <a id="m-minpagesize"></a>MinPageSize

```csharp
public const int MinPageSize = 64;
```

The smallest page edge accepted.

**Type:** `int`

## Properties

| Member | Description |
| --- | --- |
| [`MaxPages`](#m-maxpages) | Gets how many pages may exist. |
| [`Padding`](#m-padding) | Gets the transparent border kept around every entry so bilinear sampling never reads a neighbour. |
| [`PageSize`](#m-pagesize) | Gets the page edge in pixels (pages are square). |

### <a id="m-maxpages"></a>MaxPages

```csharp
public int MaxPages { get; init; }
```

Gets how many pages may exist. When all are full the least recently used page is cleared and reused. Default 4.

**Type:** `int`

### <a id="m-padding"></a>Padding

```csharp
public int Padding { get; init; }
```

Gets the transparent border kept around every entry so bilinear sampling never reads a neighbour. Default 1.

**Type:** `int`

### <a id="m-pagesize"></a>PageSize

```csharp
public int PageSize { get; init; }
```

Gets the page edge in pixels (pages are square). Default 1024.

**Type:** `int`
