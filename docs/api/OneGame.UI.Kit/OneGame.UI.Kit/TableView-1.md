[API reference](../../index.md) › [OneGame.UI.Kit](../index.md) › OneGame.UI.Kit

# TableView&lt;T&gt; class

**Namespace:** `OneGame.UI.Kit`  
**Package:** [`RecursiveAI.OneGame.UI.Kit`](../index.md)  
**Inherits:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md)

```csharp
public sealed class TableView<T> : Element
```

A table (kind `Table`, role `table`) with a header row and a virtualized body, so 5,000 rows cost about as much as 30. The header cells are kind `HeaderCell`, role `columnheader`, ids `id/header/0`...; a sortable one is focusable, and a click or Enter asks the game to sort (first ascending, then flipping) through the callback: the game sorts its list, and the table rebinds the rows and keeps the same item selected. The sorted header has the classes `sorted` and `asc` or `desc`. The body is a [`ListView<T>`](ListView-1.md) ([`TableView<T>.Body`](TableView-1.md#m-body)): it takes focus, Up/Down/PageUp/PageDown/Home/End move the selection, and Enter or the confirm button activates the selected row. Rows are role `row`, ids `id/row/n`; the body is `id/body`.

**Type parameters**

- `T`: The row item type.

## Properties

| Member | Description |
| --- | --- |
| [`Body`](#m-body) | The body: selection, scrolling and the virtualization counts live here. |
| [`SortColumn`](#m-sortcolumn) | The index of the column the table is sorted by, or -1 when no sort was chosen. |
| [`SortDescending`](#m-sortdescending) | True when the sort is descending. |

### <a id="m-body"></a>Body

```csharp
public ListView<T> Body { get; }
```

The body: selection, scrolling and the virtualization counts live here.

**Type:** [`ListView`](ListView-1.md)`<T>`

### <a id="m-sortcolumn"></a>SortColumn

```csharp
public int SortColumn { get; }
```

The index of the column the table is sorted by, or -1 when no sort was chosen.

**Type:** `int`

### <a id="m-sortdescending"></a>SortDescending

```csharp
public bool SortDescending { get; }
```

True when the sort is descending.

**Type:** `bool`

## Methods

| Member | Description |
| --- | --- |
| [`SetSort(int, bool)`](#m-setsort-int-bool) | Sets the sort indicator without calling the sort callback, for a table that starts out sorted. |

### <a id="m-setsort-int-bool"></a>SetSort(int, bool)

```csharp
public void SetSort(int column, bool descending);
```

Sets the sort indicator without calling the sort callback, for a table that starts out sorted.

**Parameters**

- `column` (`int`): The sorted column, or -1 for none.
- `descending` (`bool`): True for descending.

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): `column` is not a column index.
