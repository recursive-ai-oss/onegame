[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.UI

# Ui static class

**Namespace:** `OneGame.UI`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)

```csharp
public static class Ui
```

Factory methods that read like a layout: `Ui.Column(Ui.Label("Hello"), Ui.Button("OK", Accept))`. Each returns an [`Element`](Element.md); chain its builder methods ([`Element.Padding()`](Element.md#m-padding-length), [`Element.Gap()`](Element.md#m-gap-length), [`Element.Named()`](Element.md#m-named-string)) to style it, and return the root from a screen builder passed to `ScreenSet.Add`. Text and visibility follow game state through bindings ([`Element.BindText()`](Element.md#m-bindtext-func-string)), which are lambdas re-read every frame: read synced state in them, never write it.

**Example**

A screen with a live counter and a button that sends a command:

```csharp
screens.Add("main", game => Ui.Column(
        Ui.Label(() => $"Coins: {((Jar)game.World.Entities[0]).Coins}").Named("coins"),
        Ui.Button("Add a coin", () => ((Jar)game.World.Entities[0]).Send.Add(1)).Named("add"))
    .Gap(Length.Px(8))
    .Padding(Length.Px(16)));
```

## Methods

| Member | Description |
| --- | --- |
| [`Box(Element[])`](#m-box-element) | A plain container (kind `Box`) with children. |
| [`Button(string, Action)`](#m-button-string-action) | A focusable container that runs `onClick` when clicked or activated (kind `Button`), containing a label. |
| [`Column(Element[])`](#m-column-element) | A container that lays children out top to bottom. |
| [`Each<T>(Func<IReadOnlyList<T>>, Func<T, Element>)`](#m-each-t-func-ireadonlylist-t-func-t-element) | A container with children chosen by a list that is re-read every frame; see [`Element.BindChildren()`](Element.md#m-bindchildren-t-func-ireadonlylist-t-func-t-element). |
| [`Image(string, float, float)`](#m-image-string-float-float) | An element that shows the image or SVG with this asset id (kind `Image`). |
| [`Label(Func<string>)`](#m-label-func-string) | A text element whose text follows a lambda. |
| [`Label(string)`](#m-label-string) | A text element (kind `Label`). |
| [`Row(Element[])`](#m-row-element) | A container that lays children out left to right. |
| [`Spacer()`](#m-spacer) | An empty element that grows to fill free space in its parent. |

### <a id="m-box-element"></a>Box(Element\[\])

```csharp
public static Element Box(params Element[] children);
```

A plain container (kind `Box`) with children.

**Parameters**

- `children` ([`Element`](Element.md)\[\])

**Returns:** [`Element`](Element.md)

### <a id="m-button-string-action"></a>Button(string, Action)

```csharp
public static Element Button(string text, Action onClick);
```

A focusable container that runs `onClick` when clicked or activated (kind `Button`), containing a label.

**Parameters**

- `text` (`string`)
- `onClick` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action))

**Returns:** [`Element`](Element.md)

### <a id="m-column-element"></a>Column(Element\[\])

```csharp
public static Element Column(params Element[] children);
```

A container that lays children out top to bottom.

**Parameters**

- `children` ([`Element`](Element.md)\[\])

**Returns:** [`Element`](Element.md)

### <a id="m-each-t-func-ireadonlylist-t-func-t-element"></a>Each&lt;T&gt;(Func&lt;IReadOnlyList&lt;T&gt;&gt;, Func&lt;T, Element&gt;)

```csharp
public static Element Each<T>(Func<IReadOnlyList<T>> items, Func<T, Element> build);
```

A container with children chosen by a list that is re-read every frame; see [`Element.BindChildren()`](Element.md#m-bindchildren-t-func-ireadonlylist-t-func-t-element).

**Parameters**

- `items` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)&lt;[`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<T>>`)
- `build` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-2)`<T,` [`Element`](Element.md)&gt;)

**Returns:** [`Element`](Element.md)

### <a id="m-image-string-float-float"></a>Image(string, float, float)

```csharp
public static Element Image(string assetId, float naturalWidth = 0f, float naturalHeight = 0f);
```

An element that shows the image or SVG with this asset id (kind `Image`). `naturalWidth` and `naturalHeight` size it when no width or height is set.

**Parameters**

- `assetId` (`string`)
- `naturalWidth` (`float`)
- `naturalHeight` (`float`)

**Returns:** [`Element`](Element.md)

### <a id="m-label-func-string"></a>Label(Func&lt;string&gt;)

```csharp
public static Element Label(Func<string> text);
```

A text element whose text follows a lambda.

**Parameters**

- `text` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)`<string>`)

**Returns:** [`Element`](Element.md)

### <a id="m-label-string"></a>Label(string)

```csharp
public static Element Label(string text);
```

A text element (kind `Label`).

**Parameters**

- `text` (`string`)

**Returns:** [`Element`](Element.md)

### <a id="m-row-element"></a>Row(Element\[\])

```csharp
public static Element Row(params Element[] children);
```

A container that lays children out left to right.

**Parameters**

- `children` ([`Element`](Element.md)\[\])

**Returns:** [`Element`](Element.md)

### <a id="m-spacer"></a>Spacer()

```csharp
public static Element Spacer();
```

An empty element that grows to fill free space in its parent.

**Returns:** [`Element`](Element.md)
