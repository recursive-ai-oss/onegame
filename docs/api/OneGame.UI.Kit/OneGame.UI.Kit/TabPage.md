[API reference](../../index.md) › [OneGame.UI.Kit](../index.md) › OneGame.UI.Kit

# TabPage record

**Namespace:** `OneGame.UI.Kit`  
**Package:** [`RecursiveAI.OneGame.UI.Kit`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`TabPage`](TabPage.md)&gt;

```csharp
public sealed record TabPage : IEquatable<TabPage>
```

A page of [`UiKit.Tabs()`](UiKit.md#m-tabs-ireadonlylist-tabpage-func-int-action-int-string): the tab's title and the content shown while it is selected.

## Constructors

| Member | Description |
| --- | --- |
| [`TabPage(string, Element, string?)`](#m-ctor-string-element-string) | A page of [`UiKit.Tabs()`](UiKit.md#m-tabs-ireadonlylist-tabpage-func-int-action-int-string): the tab's title and the content shown while it is selected. |

### <a id="m-ctor-string-element-string"></a>TabPage(string, Element, string?)

```csharp
public TabPage(string Title, Element Content, string? Id = null);
```

A page of [`UiKit.Tabs()`](UiKit.md#m-tabs-ireadonlylist-tabpage-func-int-action-int-string): the tab's title and the content shown while it is selected.

**Parameters**

- `Title` (`string`): The text on the tab.
- `Content` ([`Element`](../../OneGame.UI/OneGame.UI/Element.md)): The content, built once; it stays in the tree (hidden) while another tab is selected, so its state survives.
- `Id` (`string?`): The id of the tab (the content keeps its own id); null for none.

## Properties

| Member | Description |
| --- | --- |
| [`Content`](#m-content) | The content, built once; it stays in the tree (hidden) while another tab is selected, so its state survives. |
| [`Id`](#m-id) | The id of the tab (the content keeps its own id); null for none. |
| [`Title`](#m-title) | The text on the tab. |

### <a id="m-content"></a>Content

```csharp
public Element Content { get; init; }
```

The content, built once; it stays in the tree (hidden) while another tab is selected, so its state survives.

**Type:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md)

### <a id="m-id"></a>Id

```csharp
public string? Id { get; init; }
```

The id of the tab (the content keeps its own id); null for none.

**Type:** `string?`

### <a id="m-title"></a>Title

```csharp
public string Title { get; init; }
```

The text on the tab.

**Type:** `string`
