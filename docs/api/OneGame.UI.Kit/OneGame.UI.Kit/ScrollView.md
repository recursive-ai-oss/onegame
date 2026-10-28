[API reference](../../index.md) › [OneGame.UI.Kit](../index.md) › OneGame.UI.Kit

# ScrollView class

**Namespace:** `OneGame.UI.Kit`  
**Package:** [`RecursiveAI.OneGame.UI.Kit`](../index.md)  
**Inherits:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md)

```csharp
public sealed class ScrollView : Element
```

A scrolling container with scroll bars (kind `ScrollView`, role `scrollview`). Content larger than the view scrolls with the wheel, by dragging a bar's thumb, by clicking the bar's track (one page toward the click) and when a focused descendant needs to be seen. The bars are drawn over the view, so they take no layout space, and only show while there is something to scroll. Parts: `id/scroller` (the element that scrolls), `id/vbar`, `id/vthumb`, `id/hbar`, `id/hthumb`.

## Properties

| Member | Description |
| --- | --- |
| [`Content`](#m-content) | The content that was passed in. |
| [`Scroller`](#m-scroller) | The element that scrolls; its [`Element.ScrollX`](../../OneGame.UI/OneGame.UI/Element.md#m-scrollx) and [`Element.ScrollY`](../../OneGame.UI/OneGame.UI/Element.md#m-scrolly) are the position. |

### <a id="m-content"></a>Content

```csharp
public Element Content { get; }
```

The content that was passed in.

**Type:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md)

### <a id="m-scroller"></a>Scroller

```csharp
public Element Scroller { get; }
```

The element that scrolls; its [`Element.ScrollX`](../../OneGame.UI/OneGame.UI/Element.md#m-scrollx) and [`Element.ScrollY`](../../OneGame.UI/OneGame.UI/Element.md#m-scrolly) are the position.

**Type:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md)
