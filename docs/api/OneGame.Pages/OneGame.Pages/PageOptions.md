[API reference](../../index.md) › [OneGame.Pages](../index.md) › OneGame.Pages

# PageOptions class

**Namespace:** `OneGame.Pages`  
**Package:** [`RecursiveAI.OneGame.Pages`](../index.md)

```csharp
public sealed class PageOptions
```

What a page is made from.

## Constructors

| Member | Description |
| --- | --- |
| [`PageOptions(PageFonts)`](#m-ctor-pagefonts) | Creates options. |

### <a id="m-ctor-pagefonts"></a>PageOptions(PageFonts)

```csharp
public PageOptions(PageFonts fonts);
```

Creates options.

**Parameters**

- `fonts` ([`PageFonts`](PageFonts.md)): The fonts the page may use.

## Properties

| Member | Description |
| --- | --- |
| [`Fonts`](#m-fonts) | The fonts the page may use. |
| [`Images`](#m-images) | The images the page can name; none when `null`. |
| [`Setup`](#m-setup) | Runs every time the page is (re)created, which happens when the window size or UI scale changes (a OneRender HTML page cannot resize). |

### <a id="m-fonts"></a>Fonts

```csharp
public PageFonts Fonts { get; }
```

The fonts the page may use.

**Type:** [`PageFonts`](PageFonts.md)

### <a id="m-images"></a>Images

```csharp
public PageImages? Images { get; init; }
```

The images the page can name; none when `null`.

**Type:** [`PageImages`](PageImages.md)?

### <a id="m-setup"></a>Setup

```csharp
public Action<PageScreen>? Setup { get; init; }
```

Runs every time the page is (re)created, which happens when the window size or UI scale changes (a OneRender HTML page cannot resize). Register DOM listeners here so they survive: `s.Interactive!.Events.AddEventListener(...)`.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`PageScreen`](PageScreen.md)&gt;?
