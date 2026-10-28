[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# HostResources record

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`HostResources`](HostResources.md)&gt;

```csharp
public sealed record HostResources : IEquatable<HostResources>
```

What the host has ready when it asks the game for a render stage.

## Constructors

| Member | Description |
| --- | --- |
| [`HostResources(FontLibrary?, FontTextMeasurer?, ArtAtlas, IShaderSource?)`](#m-ctor-fontlibrary-fonttextmeasurer-artatlas-ishadersource) | What the host has ready when it asks the game for a render stage. |

### <a id="m-ctor-fontlibrary-fonttextmeasurer-artatlas-ishadersource"></a>HostResources(FontLibrary?, FontTextMeasurer?, ArtAtlas, IShaderSource?)

```csharp
public HostResources(FontLibrary? Fonts, FontTextMeasurer? Measurer, ArtAtlas Art, IShaderSource? Shaders);
```

What the host has ready when it asks the game for a render stage.

**Parameters**

- `Fonts` ([`FontLibrary`](../../OneGame.Text/OneGame.Text/FontLibrary.md)?): The game's fonts, or null when it has none.
- `Measurer` ([`FontTextMeasurer`](../../OneGame.Text/OneGame.Text/FontTextMeasurer.md)?): Measures and shapes text with `Fonts`; null when there are no fonts.
- `Art` ([`ArtAtlas`](../../OneGame.Atlas/OneGame.Atlas/ArtAtlas.md)): The atlas of SVG and image art the UI draws.
- `Shaders` ([`IShaderSource`](../../OneGame.Render2D/OneGame.Render2D/IShaderSource.md)?): Where compiled renderer shaders come from; null when none are available.

## Properties

| Member | Description |
| --- | --- |
| [`Art`](#m-art) | The atlas of SVG and image art the UI draws. |
| [`Assets`](#m-assets) | The game's asset service; the 2D stage attaches its renderer so textures can be uploaded. |
| [`Fonts`](#m-fonts) | The game's fonts, or null when it has none. |
| [`HotShaders`](#m-hotshaders) | Where a stage registers materials whose shader files may be edited while the game runs. |
| [`Measurer`](#m-measurer) | Measures and shapes text with `Fonts`; null when there are no fonts. |
| [`Shaders`](#m-shaders) | Where compiled renderer shaders come from; null when none are available. |

### <a id="m-art"></a>Art

```csharp
public ArtAtlas Art { get; init; }
```

The atlas of SVG and image art the UI draws.

**Type:** [`ArtAtlas`](../../OneGame.Atlas/OneGame.Atlas/ArtAtlas.md)

### <a id="m-assets"></a>Assets

```csharp
public Assets? Assets { get; init; }
```

The game's asset service; the 2D stage attaches its renderer so textures can be uploaded.

**Type:** [`Assets`](Assets.md)?

### <a id="m-fonts"></a>Fonts

```csharp
public FontLibrary? Fonts { get; init; }
```

The game's fonts, or null when it has none.

**Type:** [`FontLibrary`](../../OneGame.Text/OneGame.Text/FontLibrary.md)?

### <a id="m-hotshaders"></a>HotShaders

```csharp
public HotShaders HotShaders { get; init; }
```

Where a stage registers materials whose shader files may be edited while the game runs.

**Type:** [`HotShaders`](HotShaders.md)

### <a id="m-measurer"></a>Measurer

```csharp
public FontTextMeasurer? Measurer { get; init; }
```

Measures and shapes text with `Fonts`; null when there are no fonts.

**Type:** [`FontTextMeasurer`](../../OneGame.Text/OneGame.Text/FontTextMeasurer.md)?

### <a id="m-shaders"></a>Shaders

```csharp
public IShaderSource? Shaders { get; init; }
```

Where compiled renderer shaders come from; null when none are available.

**Type:** [`IShaderSource`](../../OneGame.Render2D/OneGame.Render2D/IShaderSource.md)?
