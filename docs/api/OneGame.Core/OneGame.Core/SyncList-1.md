[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# SyncList&lt;T&gt; class

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Implements:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<T>`, [`IEnumerable`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable-1)`<T>`, [`IEnumerable`](https://learn.microsoft.com/dotnet/api/system.collections.ienumerable), [`IReadOnlyCollection`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlycollection-1)`<T>`

```csharp
public sealed class SyncList<T> : IReadOnlyList<T>, IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>
```

A list that is part of an entity's synced state. It tracks its own changes, so an entity sends it when it has changed and not otherwise. Declare it as a get-only property with an initializer: `[Synced] public SyncList<int> Items { get; } = new();`. Only sim code may change it; views get the read-only members.

**Type parameters**

- `T`: A synced element type: a number, bool, string, enum, or a synced value.

## Constructors

| Member | Description |
| --- | --- |
| [`SyncList()`](#m-ctor) |  |

### <a id="m-ctor"></a>SyncList()

```csharp
public SyncList();
```

## Properties

| Member | Description |
| --- | --- |
| [`Count`](#m-count) |  |
| [`IsDirty`](#m-isdirty) | True when the list changed since the last [`SyncList<T>.ClearDirty()`](SyncList-1.md#m-cleardirty). |
| [`this[int]`](#m-item-int) | Gets or sets an element. |

### <a id="m-count"></a>Count

```csharp
public int Count { get; }
```

Implements [`IReadOnlyCollection<T>.Count`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlycollection-1.count).

**Type:** `int`

### <a id="m-isdirty"></a>IsDirty

```csharp
public bool IsDirty { get; }
```

True when the list changed since the last [`SyncList<T>.ClearDirty()`](SyncList-1.md#m-cleardirty).

**Type:** `bool`

### <a id="m-item-int"></a>this\[int\]

```csharp
public T this[int index] { get; set; }
```

Gets or sets an element.

**Parameters**

- `index` (`int`)

**Type:** `T`

## Methods

| Member | Description |
| --- | --- |
| [`Add(T)`](#m-add-t) | Adds an element at the end. |
| [`Clear()`](#m-clear) | Removes every element. |
| [`ClearDirty()`](#m-cleardirty) | Marks the list as unchanged; the engine calls it after sending or saving. |
| [`Contains(T)`](#m-contains-t) | True if the list contains `item`. |
| [`ContentEquals(SyncList<T>)`](#m-contentequals-synclist-t) | True if both lists hold equal elements in the same order. |
| [`CopyFrom(SyncList<T>)`](#m-copyfrom-synclist-t) | Makes this list hold the elements of `other`, reusing its storage. |
| [`GetEnumerator()`](#m-getenumerator) | Enumerates the elements without allocating. |
| [`IndexOf(T)`](#m-indexof-t) | The index of the first element equal to `item`, or -1. |
| [`Insert(int, T)`](#m-insert-int-t) | Inserts an element. |
| [`Remove(T)`](#m-remove-t) | Removes the first element equal to `item`. |
| [`RemoveAt(int)`](#m-removeat-int) | Removes the element at `index`. |
| [`ReplaceWith(T[])`](#m-replacewith-t) | Replaces the contents; the engine uses it when applying received state. |
| [`ToArray()`](#m-toarray) | Copies the elements into a new array. |

### <a id="m-add-t"></a>Add(T)

```csharp
public void Add(T item);
```

Adds an element at the end.

**Parameters**

- `item` (`T`): The element.

### <a id="m-clear"></a>Clear()

```csharp
public void Clear();
```

Removes every element.

### <a id="m-cleardirty"></a>ClearDirty()

```csharp
public void ClearDirty();
```

Marks the list as unchanged; the engine calls it after sending or saving.

### <a id="m-contains-t"></a>Contains(T)

```csharp
public bool Contains(T item);
```

True if the list contains `item`.

**Parameters**

- `item` (`T`)

**Returns:** `bool`

### <a id="m-contentequals-synclist-t"></a>ContentEquals(SyncList&lt;T&gt;)

```csharp
public bool ContentEquals(SyncList<T> other);
```

True if both lists hold equal elements in the same order.

**Parameters**

- `other` ([`SyncList`](SyncList-1.md)`<T>`): The list to compare with.

**Returns:** `bool`

### <a id="m-copyfrom-synclist-t"></a>CopyFrom(SyncList&lt;T&gt;)

```csharp
public void CopyFrom(SyncList<T> other);
```

Makes this list hold the elements of `other`, reusing its storage. Prediction uses it to copy confirmed state.

**Parameters**

- `other` ([`SyncList`](SyncList-1.md)`<T>`)

### <a id="m-getenumerator"></a>GetEnumerator()

```csharp
public List<T>.Enumerator GetEnumerator();
```

Enumerates the elements without allocating.

**Returns:** `List<T>.`[`Enumerator`](https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1.enumerator)

### <a id="m-indexof-t"></a>IndexOf(T)

```csharp
public int IndexOf(T item);
```

The index of the first element equal to `item`, or -1.

**Parameters**

- `item` (`T`)

**Returns:** `int`

### <a id="m-insert-int-t"></a>Insert(int, T)

```csharp
public void Insert(int index, T item);
```

Inserts an element.

**Parameters**

- `index` (`int`): Where the element goes, 0 to [`SyncList<T>.Count`](SyncList-1.md#m-count).
- `item` (`T`): The element.

### <a id="m-remove-t"></a>Remove(T)

```csharp
public bool Remove(T item);
```

Removes the first element equal to `item`.

**Parameters**

- `item` (`T`)

**Returns:** `bool` — True if one was removed.

### <a id="m-removeat-int"></a>RemoveAt(int)

```csharp
public void RemoveAt(int index);
```

Removes the element at `index`.

**Parameters**

- `index` (`int`)

### <a id="m-replacewith-t"></a>ReplaceWith(T\[\])

```csharp
public void ReplaceWith(T[] items);
```

Replaces the contents; the engine uses it when applying received state.

**Parameters**

- `items` (`T[]`): The new contents.

### <a id="m-toarray"></a>ToArray()

```csharp
public T[] ToArray();
```

Copies the elements into a new array.

**Returns:** `T[]`
