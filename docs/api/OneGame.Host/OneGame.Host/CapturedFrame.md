[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# CapturedFrame record

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`CapturedFrame`](CapturedFrame.md)&gt;

```csharp
public sealed record CapturedFrame : IEquatable<CapturedFrame>
```

A captured frame: RGBA8 pixels, top row first.

## Constructors

| Member | Description |
| --- | --- |
| [`CapturedFrame(int, int, byte[], string)`](#m-ctor-int-int-byte-string) | A captured frame: RGBA8 pixels, top row first. |

### <a id="m-ctor-int-int-byte-string"></a>CapturedFrame(int, int, byte\[\], string)

```csharp
public CapturedFrame(int Width, int Height, byte[] Rgba, string Renderer);
```

A captured frame: RGBA8 pixels, top row first.

**Parameters**

- `Width` (`int`): Width in pixels.
- `Height` (`int`): Height in pixels.
- `Rgba` (`byte[]`): Pixels, `Width × Height × 4` bytes.
- `Renderer` (`string`): Where the pixels were made: the adapter name, or `cpu`.

## Properties

| Member | Description |
| --- | --- |
| [`Height`](#m-height) | Height in pixels. |
| [`Renderer`](#m-renderer) | Where the pixels were made: the adapter name, or `cpu`. |
| [`Rgba`](#m-rgba) | Pixels, `Width × Height × 4` bytes. |
| [`Width`](#m-width) | Width in pixels. |

### <a id="m-height"></a>Height

```csharp
public int Height { get; init; }
```

Height in pixels.

**Type:** `int`

### <a id="m-renderer"></a>Renderer

```csharp
public string Renderer { get; init; }
```

Where the pixels were made: the adapter name, or `cpu`.

**Type:** `string`

### <a id="m-rgba"></a>Rgba

```csharp
public byte[] Rgba { get; init; }
```

Pixels, `Width × Height × 4` bytes.

**Type:** `byte[]`

### <a id="m-width"></a>Width

```csharp
public int Width { get; init; }
```

Width in pixels.

**Type:** `int`
