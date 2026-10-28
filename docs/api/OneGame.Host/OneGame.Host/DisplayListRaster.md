[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# DisplayListRaster static class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public static class DisplayListRaster
```

A small CPU rasterizer for display lists: flat rectangles, borders and one bar per text line, with clips and group opacity. Corner radii, images, shadows and transforms are ignored. Integer math only, so the output is identical on every machine.

## Methods

| Member | Description |
| --- | --- |
| [`Paint(Span<byte>, int, int, DisplayList)`](#m-paint-span-byte-int-int-displaylist) | Paints `list` over `rgba` (width × height, RGBA8). |

### <a id="m-paint-span-byte-int-int-displaylist"></a>Paint(Span&lt;byte&gt;, int, int, DisplayList)

```csharp
public static void Paint(Span<byte> rgba, int width, int height, DisplayList list);
```

Paints `list` over `rgba` (width × height, RGBA8).

**Parameters**

- `rgba` ([`Span`](https://learn.microsoft.com/dotnet/api/system.span-1)`<byte>`)
- `width` (`int`)
- `height` (`int`)
- `list` ([`DisplayList`](../../OneGame.UI/OneGame.UI/DisplayList.md))
