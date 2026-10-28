[API reference](../../index.md) › [OneGame.UI.Kit](../index.md) › OneGame.UI.Kit

# TableColumn&lt;T&gt; class

**Namespace:** `OneGame.UI.Kit`  
**Package:** [`RecursiveAI.OneGame.UI.Kit`](../index.md)

```csharp
public sealed class TableColumn<T>
```

One column of a [`TableView<T>`](TableView-1.md): a title, a width, and how a cell shows an item.

**Type parameters**

- `T`: The row item type.

## Constructors

| Member | Description |
| --- | --- |
| [`TableColumn(string, Func<Element>, Action<Element, T>, ColumnWidth?, bool)`](#m-ctor-string-func-element-action-element-t-columnwidth-bool) | Creates a column with its own cell element, for icons, bars or buttons. |
| [`TableColumn(string, Func<T, string>, ColumnWidth?, bool, bool)`](#m-ctor-string-func-t-string-columnwidth-bool-bool) | Creates a column whose cells show text. |

### <a id="m-ctor-string-func-element-action-element-t-columnwidth-bool"></a>TableColumn(string, Func&lt;Element&gt;, Action&lt;Element, T&gt;, ColumnWidth?, bool)

```csharp
public TableColumn(string title, Func<Element> createCell, Action<Element, T> bindCell, ColumnWidth? width = default, bool sortable = false);
```

Creates a column with its own cell element, for icons, bars or buttons.

**Parameters**

- `title` (`string`): The header text.
- `createCell` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)&lt;[`Element`](../../OneGame.UI/OneGame.UI/Element.md)&gt;): Builds an empty cell; it runs once per recycled row.
- `bindCell` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-2)&lt;[`Element`](../../OneGame.UI/OneGame.UI/Element.md)`, T>`): Shows an item in a cell; it runs when the row is bound.
- `width` ([`ColumnWidth`](ColumnWidth.md)?): The width; flexible with a share of 1 by default.
- `sortable` (`bool`): True when clicking the header asks the game to sort by this column.

### <a id="m-ctor-string-func-t-string-columnwidth-bool-bool"></a>TableColumn(string, Func&lt;T, string&gt;, ColumnWidth?, bool, bool)

```csharp
public TableColumn(string title, Func<T, string> text, ColumnWidth? width = default, bool sortable = true, bool alignEnd = false);
```

Creates a column whose cells show text.

**Parameters**

- `title` (`string`): The header text.
- `text` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-2)`<T, string>`): Gives a cell's text for an item; it runs when a row is bound, not every frame.
- `width` ([`ColumnWidth`](ColumnWidth.md)?): The width; flexible with a share of 1 by default.
- `sortable` (`bool`): True when clicking the header asks the game to sort by this column.
- `alignEnd` (`bool`): Right-align the cells (numbers).

## Properties

| Member | Description |
| --- | --- |
| [`AlignEnd`](#m-alignend) | True when text cells are right-aligned. |
| [`Sortable`](#m-sortable) | True when the header can be clicked to sort. |
| [`Title`](#m-title) | The header text. |
| [`Width`](#m-width) | The column width. |

### <a id="m-alignend"></a>AlignEnd

```csharp
public bool AlignEnd { get; }
```

True when text cells are right-aligned.

**Type:** `bool`

### <a id="m-sortable"></a>Sortable

```csharp
public bool Sortable { get; }
```

True when the header can be clicked to sort.

**Type:** `bool`

### <a id="m-title"></a>Title

```csharp
public string Title { get; }
```

The header text.

**Type:** `string`

### <a id="m-width"></a>Width

```csharp
public ColumnWidth Width { get; }
```

The column width.

**Type:** [`ColumnWidth`](ColumnWidth.md)
