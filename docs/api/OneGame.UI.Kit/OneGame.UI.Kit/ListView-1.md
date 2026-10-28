[API reference](../../index.md) › [OneGame.UI.Kit](../index.md) › OneGame.UI.Kit

# ListView&lt;T&gt; class

**Namespace:** `OneGame.UI.Kit`  
**Package:** [`RecursiveAI.OneGame.UI.Kit`](../index.md)  
**Inherits:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md)

```csharp
public class ListView<T> : Element
```

A virtualized list (kind `List`, role `listbox`): however many items the source holds, only the rows in view (plus a spare) exist as elements, and they are recycled as the list scrolls. All rows have the same height. One item at a time can be selected; the list itself takes focus, and Up/Down (arrow keys or d-pad), PageUp/PageDown, Home and End move the selection, which keeps the selected row in view. At the first or last item Up or Down is not used, so focus moves on to the neighbor. Rows are kind `ListRow`, role `row`, with the class `selected` on the selected one and `odd` on odd indexes; with an id, row `n` has the id `id/row/n` and [`Element.Value`](../../OneGame.UI/OneGame.UI/Element.md#m-value)`n`.

**Type parameters**

- `T`: The item type. Equality ([`EqualityComparer.Default`](https://learn.microsoft.com/dotnet/api/system.collections.generic.equalitycomparer-1)) is how the selection follows an item when the order changes.

## Properties

| Member | Description |
| --- | --- |
| [`Count`](#m-count) | The number of items in the source as of the last update. |
| [`FirstRealizedIndex`](#m-firstrealizedindex) | The index of the item the first realized row shows. |
| [`RealizedRowCount`](#m-realizedrowcount) | How many row elements currently show items (not the number of items). |
| [`SelectedIndex`](#m-selectedindex) | The index of the selected item, or -1 for none. |
| [`SelectedItem`](#m-selecteditem) | The selected item, or default when nothing is selected. |
| [`View`](#m-view) | The scroll view inside, for its scroll position and bars. |

### <a id="m-count"></a>Count

```csharp
public int Count { get; }
```

The number of items in the source as of the last update.

**Type:** `int`

### <a id="m-firstrealizedindex"></a>FirstRealizedIndex

```csharp
public int FirstRealizedIndex { get; }
```

The index of the item the first realized row shows.

**Type:** `int`

### <a id="m-realizedrowcount"></a>RealizedRowCount

```csharp
public int RealizedRowCount { get; }
```

How many row elements currently show items (not the number of items).

**Type:** `int`

### <a id="m-selectedindex"></a>SelectedIndex

```csharp
public int SelectedIndex { get; }
```

The index of the selected item, or -1 for none.

**Type:** `int`

### <a id="m-selecteditem"></a>SelectedItem

```csharp
public T? SelectedItem { get; }
```

The selected item, or default when nothing is selected.

**Type:** `T?`

### <a id="m-view"></a>View

```csharp
public ScrollView View { get; }
```

The scroll view inside, for its scroll position and bars.

**Type:** [`ScrollView`](ScrollView.md)

## Methods

| Member | Description |
| --- | --- |
| [`Invalidate()`](#m-invalidate) | Tells the list that items changed in place (a price, a sort): rows are rebound on the next update and the selection follows its item. |
| [`ScrollToIndex(int)`](#m-scrolltoindex-int) | Scrolls so the item is inside the view, as little as possible. |
| [`Select(int)`](#m-select-int) | Selects an item and scrolls it into view; calls the selection callback when the selection changes. |

### <a id="m-invalidate"></a>Invalidate()

```csharp
public void Invalidate();
```

Tells the list that items changed in place (a price, a sort): rows are rebound on the next update and the selection follows its item.

### <a id="m-scrolltoindex-int"></a>ScrollToIndex(int)

```csharp
public void ScrollToIndex(int index);
```

Scrolls so the item is inside the view, as little as possible.

**Parameters**

- `index` (`int`)

### <a id="m-select-int"></a>Select(int)

```csharp
public void Select(int index);
```

Selects an item and scrolls it into view; calls the selection callback when the selection changes.

**Parameters**

- `index` (`int`)

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): `index` is not an index of the source.
