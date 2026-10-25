using System.Collections;

namespace OneGame.Core;

/// <summary>
/// A list that is part of an entity's synced state. It tracks its own changes, so an entity sends it when it has changed and
/// not otherwise. Declare it as a get-only property with an initializer: <c>[Synced] public SyncList&lt;int&gt; Items { get; } = new();</c>.
/// Only sim code may change it; views get the read-only members.
/// </summary>
/// <typeparam name="T">A synced element type: a number, bool, string, enum, or a synced value.</typeparam>
public sealed class SyncList<T> : IReadOnlyList<T>
{
    private readonly List<T> _items = [];

    /// <summary>True when the list changed since the last <see cref="ClearDirty"/>.</summary>
    public bool IsDirty { get; private set; }

    /// <inheritdoc />
    public int Count => _items.Count;

    /// <summary>Gets or sets an element.</summary>
    public T this[int index]
    {
        get => _items[index];
        [MutatesState]
        set
        {
            _items[index] = value;
            IsDirty = true;
        }
    }

    /// <summary>Adds an element at the end.</summary>
    [MutatesState]
    public void Add(T item)
    {
        _items.Add(item);
        IsDirty = true;
    }

    /// <summary>Inserts an element.</summary>
    [MutatesState]
    public void Insert(int index, T item)
    {
        _items.Insert(index, item);
        IsDirty = true;
    }

    /// <summary>Removes the element at <paramref name="index"/>.</summary>
    [MutatesState]
    public void RemoveAt(int index)
    {
        _items.RemoveAt(index);
        IsDirty = true;
    }

    /// <summary>Removes the first element equal to <paramref name="item"/>.</summary>
    /// <returns>True if one was removed.</returns>
    [MutatesState]
    public bool Remove(T item)
    {
        var removed = _items.Remove(item);
        IsDirty |= removed;
        return removed;
    }

    /// <summary>Removes every element.</summary>
    [MutatesState]
    public void Clear()
    {
        if (_items.Count > 0)
        {
            _items.Clear();
            IsDirty = true;
        }
    }

    /// <summary>Replaces the contents; the engine uses it when applying received state.</summary>
    [MutatesState]
    public void ReplaceWith(T[] items)
    {
        _items.Clear();
        _items.AddRange(items);
        IsDirty = true;
    }

    /// <summary>Makes this list hold the elements of <paramref name="other"/>, reusing its storage. Prediction uses it to copy confirmed state.</summary>
    [MutatesState]
    public void CopyFrom(SyncList<T> other)
    {
        _items.Clear();
        _items.AddRange(other._items);
    }

    /// <summary>True if the list contains <paramref name="item"/>.</summary>
    public bool Contains(T item) => _items.Contains(item);

    /// <summary>The index of the first element equal to <paramref name="item"/>, or -1.</summary>
    public int IndexOf(T item) => _items.IndexOf(item);

    /// <summary>Copies the elements into a new array.</summary>
    public T[] ToArray() => _items.ToArray();

    /// <summary>True if both lists hold equal elements in the same order.</summary>
    public bool ContentEquals(SyncList<T> other)
    {
        if (_items.Count != other._items.Count)
        {
            return false;
        }

        var comparer = EqualityComparer<T>.Default;
        for (var i = 0; i < _items.Count; i++)
        {
            if (!comparer.Equals(_items[i], other._items[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Marks the list as unchanged; the engine calls it after sending or saving.</summary>
    public void ClearDirty() => IsDirty = false;

    /// <summary>Enumerates the elements without allocating.</summary>
    public List<T>.Enumerator GetEnumerator() => _items.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
}
