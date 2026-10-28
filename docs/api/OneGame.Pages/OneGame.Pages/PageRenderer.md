[API reference](../../index.md) › [OneGame.Pages](../index.md) › OneGame.Pages

# PageRenderer class

**Namespace:** `OneGame.Pages`  
**Package:** [`RecursiveAI.OneGame.Pages`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class PageRenderer : IDisposable
```

The GPU side of pages: turns display lists into passes of a render graph. One per device and target format. It keeps the textures, pipelines and buffers it needs between frames, so a frame that changes nothing allocates nothing.

## Properties

| Member | Description |
| --- | --- |
| [`Stats`](#m-stats) | What the most recent call drew. |

### <a id="m-stats"></a>Stats

```csharp
public PageStats Stats { get; }
```

What the most recent call drew.

**Type:** [`PageStats`](PageStats.md)

## Methods

| Member | Description |
| --- | --- |
| [`AddPasses(RenderGraph, GraphTexture, IReadOnlyList<PageScreen>, PageScopes, in PageFrame)`](#m-addpasses-rendergraph-graphtexture-ireadonlylist-pagescreen-pagescopes-pageframe) | Adds the passes that draw one layer of the screens onto `target`, in the order given. |
| [`Create(GpuDevice, GpuFormat, GlyphAtlas, ArtAtlas)`](#m-create-gpudevice-gpuformat-glyphatlas-artatlas) | Creates a renderer for targets of one format. |
| [`Dispose()`](#m-dispose) |  |

### <a id="m-addpasses-rendergraph-graphtexture-ireadonlylist-pagescreen-pagescopes-pageframe"></a>AddPasses(RenderGraph, GraphTexture, IReadOnlyList&lt;PageScreen&gt;, PageScopes, in PageFrame)

```csharp
public void AddPasses(RenderGraph graph, GraphTexture target, IReadOnlyList<PageScreen> screens, PageScopes scopes, in PageFrame frame);
```

Adds the passes that draw one layer of the screens onto `target`, in the order given. Without [`PageFrame.Clear`](PageFrame.md#m-clear) the target must already have been written by an earlier pass.

**Parameters**

- `graph` ([`RenderGraph`](../../OneGame.Gpu/OneGame.Gpu.Graph/RenderGraph.md)): The frame's graph, already begun.
- `target` ([`GraphTexture`](../../OneGame.Gpu/OneGame.Gpu.Graph/GraphTexture.md)): The target as a graph resource.
- `screens` ([`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`PageScreen`](PageScreen.md)&gt;): The shown screens, back to front.
- `scopes` ([`PageScopes`](PageScopes.md)): What the host scopes mean.
- `frame` ([`PageFrame`](PageFrame.md)): The frame.

### <a id="m-create-gpudevice-gpuformat-glyphatlas-artatlas"></a>Create(GpuDevice, GpuFormat, GlyphAtlas, ArtAtlas)

```csharp
public static PageRenderer Create(GpuDevice device, GpuFormat targetFormat, GlyphAtlas glyphs, ArtAtlas art);
```

Creates a renderer for targets of one format.

**Parameters**

- `device` ([`GpuDevice`](../../OneGame.Gpu/OneGame.Gpu/GpuDevice.md)): The device.
- `targetFormat` ([`GpuFormat`](../../OneGame.Gpu/OneGame.Gpu/GpuFormat.md)): The format of the targets pages are drawn onto; an sRGB format.
- `glyphs` ([`GlyphAtlas`](../../OneGame.Text/OneGame.Text/GlyphAtlas.md)): The game's glyph atlas; the renderer registers OneRender's font faces in it.
- `art` ([`ArtAtlas`](../../OneGame.Atlas/OneGame.Atlas/ArtAtlas.md)): The game's art atlas, which holds page images and SVG.

**Returns:** [`PageRenderer`](PageRenderer.md)

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Implements [`IDisposable.Dispose()`](https://learn.microsoft.com/dotnet/api/system.idisposable.dispose).
