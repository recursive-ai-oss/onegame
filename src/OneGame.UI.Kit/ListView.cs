namespace OneGame.UI.Kit;

/// <summary>
/// A virtualized list (kind <c>List</c>, role <c>listbox</c>): however many items the source holds, only the rows in view (plus a spare) exist as elements, and they are recycled as the list scrolls.
/// All rows have the same height. One item at a time can be selected; the list itself takes focus, and Up/Down (arrow keys or d-pad), PageUp/PageDown, Home and End move the selection,
/// which keeps the selected row in view. At the first or last item Up or Down is not used, so focus moves on to the neighbor.
/// Rows are kind <c>ListRow</c>, role <c>row</c>, with the class <c>selected</c> on the selected one and <c>odd</c> on odd indexes; with an id, row <c>n</c> has the id <c>id/row/n</c> and <see cref="Element.Value"/> <c>n</c>.
/// </summary>
/// <typeparam name="T">The item type. Equality (<see cref="EqualityComparer{T}.Default"/>) is how the selection follows an item when the order changes.</typeparam>
public class ListView<T> : Element
{
    private readonly Func<IReadOnlyList<T>> _items;
    private readonly Func<int>? _version;
    private readonly Func<Element> _createRow;
    private readonly Action<Element, T, int> _bindRow;
    private readonly Length _rowHeight;
    private readonly Action<T, int>? _onSelect;
    private readonly Action<T, int>? _onActivate;
    private readonly ScrollView _view;
    private readonly Element _layer;
    private readonly List<Element> _pool = [];
    private IReadOnlyList<T>? _lastItems;
    private int _lastCount = -1;
    private int _lastVersion = int.MinValue;
    private int _first = -1;
    private int _realized = -1;
    private float _lastRowPx = -1;
    private float _lastLayerHeight = -1;
    private bool _invalid = true;
    private int _selected = -1;
    private T? _selectedItem;
    private readonly string? _rowIdPrefix;

    internal ListView(
        Func<IReadOnlyList<T>> items,
        Func<Element> createRow,
        Action<Element, T, int> bindRow,
        Length rowHeight,
        Func<int>? version,
        Action<T, int>? onSelect,
        Action<T, int>? onActivate,
        string? id,
        string kind = "List",
        string? rowIdPrefix = null)
        : base(kind)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _createRow = createRow ?? throw new ArgumentNullException(nameof(createRow));
        _bindRow = bindRow ?? throw new ArgumentNullException(nameof(bindRow));
        _rowHeight = rowHeight;
        _version = version;
        _onSelect = onSelect;
        _onActivate = onActivate;
        if (rowHeight.Unit is not (LengthUnit.Px or LengthUnit.Ui) || !(rowHeight.Value > 0))
        {
            throw new ArgumentException("The row height must be a positive length in pixels or UI units.", nameof(rowHeight));
        }

        Id = id;
        _rowIdPrefix = rowIdPrefix ?? id;
        Role = "listbox";
        Class("kit-list " + UiKit.FocusableClass);
        IsFocusable = true;
        _layer = UiKit.Part("Box", "kit-list-rows", id, "rows");
        _view = UiKit.ScrollView(_layer, false, id is null ? null : id + "/view");
        _view.Class("kit-list-view");
        Add(_view);

        On(UiEventKind.PointerDown, (_, e) =>
        {
            if (e.Button != 0)
            {
                return false;
            }

            var at = IndexAt(e.Y);
            if (at >= 0)
            {
                SelectCore(at, scroll: false);
            }

            return true;
        });
        On(UiEventKind.Click, (_, e) =>
        {
            // A click from the pointer already selected the row on press; one from Enter, Space or the confirm button activates it.
            if (e.Button != -1 || _selected < 0)
            {
                return false;
            }

            _onActivate?.Invoke(_selectedItem!, _selected);
            return _onActivate is not null;
        });
        On(UiEventKind.Focus, (t, _) =>
        {
            // Keyboard or controller focus lands on a visible selection; a pointer press selects the row under the pointer instead.
            if (_selected < 0 && Count > 0 && !(t.Tree?.Hovered is { } h && t.Contains(h)))
            {
                SelectCore(0, scroll: true);
            }

            return false;
        });
        NavigationKeys.OnStep(this, false, d => Move(d));
        On(UiEventKind.KeyDown, (_, e) =>
        {
            var page = Math.Max(1, (int)(_view.Scroller.Bounds.Height / Math.Max(1, UiKit.ToPx(this, _rowHeight))) - 1);
            switch (e.Key)
            {
                case "PageDown": return Move(page);
                case "PageUp": return Move(-page);
                case "Home": return Count > 0 && SelectCore(0, scroll: true);
                case "End": return Count > 0 && SelectCore(Count - 1, scroll: true);
                default: return false;
            }
        });
        OnUpdate(_ => Sync());
    }

    /// <summary>The index of the selected item, or -1 for none.</summary>
    public int SelectedIndex => _selected;

    /// <summary>The selected item, or default when nothing is selected.</summary>
    public T? SelectedItem => _selected < 0 ? default : _selectedItem;

    /// <summary>The number of items in the source as of the last update.</summary>
    public int Count => _lastCount < 0 ? _items().Count : _lastCount;

    /// <summary>How many row elements currently show items (not the number of items).</summary>
    public int RealizedRowCount => _realized < 0 ? 0 : _realized;

    /// <summary>The index of the item the first realized row shows.</summary>
    public int FirstRealizedIndex => _first < 0 ? 0 : _first;

    /// <summary>The scroll view inside, for its scroll position and bars.</summary>
    public ScrollView View => _view;

    /// <summary>Selects an item and scrolls it into view; calls the selection callback when the selection changes.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not an index of the source.</exception>
    public void Select(int index)
    {
        if ((uint)index >= (uint)_items().Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "The index is outside the list.");
        }

        SelectCore(index, scroll: true);
    }

    /// <summary>Tells the list that items changed in place (a price, a sort): rows are rebound on the next update and the selection follows its item.</summary>
    public void Invalidate() => _invalid = true;

    /// <summary>Scrolls so the item is inside the view, as little as possible.</summary>
    public void ScrollToIndex(int index)
    {
        var rowPx = UiKit.ToPx(this, _rowHeight);
        var scroller = _view.Scroller;
        var top = index * rowPx;
        var height = scroller.Bounds.Height;
        if (top < scroller.ScrollY)
        {
            scroller.ScrollTo(0, top);
        }
        else if (top + rowPx > scroller.ScrollY + height)
        {
            scroller.ScrollTo(0, top + rowPx - height);
        }
    }

    private bool Move(int delta)
    {
        var count = _items().Count;
        if (count == 0)
        {
            return false;
        }

        if (_selected < 0)
        {
            return SelectCore(delta > 0 ? 0 : count - 1, scroll: true);
        }

        var raw = _selected + delta;
        if (Math.Abs(delta) == 1 && (raw < 0 || raw >= count))
        {
            // One step past either end is not used, so focus can move on to the neighbor.
            return false;
        }

        var target = Math.Clamp(raw, 0, count - 1);
        return target != _selected && SelectCore(target, scroll: true);
    }

    private bool SelectCore(int index, bool scroll)
    {
        var items = _items();
        if ((uint)index >= (uint)items.Count)
        {
            return false;
        }

        if (scroll)
        {
            ScrollToIndex(index);
        }

        if (index == _selected && EqualityComparer<T>.Default.Equals(items[index], _selectedItem))
        {
            return true;
        }

        _selected = index;
        _selectedItem = items[index];
        Value = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
        MarkSelection();
        _onSelect?.Invoke(_selectedItem, index);
        return true;
    }

    private int IndexAt(float screenY)
    {
        var rowPx = UiKit.ToPx(this, _rowHeight);
        var scroller = _view.Scroller;
        var y = screenY - scroller.AbsoluteBounds.Y + scroller.ScrollY;
        var i = (int)MathF.Floor(y / rowPx);
        return i >= 0 && i < _items().Count ? i : -1;
    }

    private void MarkSelection()
    {
        for (var k = 0; k < _realized && k < _pool.Count; k++)
        {
            _pool[k].SetClass("selected", _first + k == _selected);
        }
    }

    // Brings the realized rows in step with the source, the scroll position and the view size. Nothing here allocates when nothing changed.
    private void Sync()
    {
        var items = _items();
        var count = items.Count;
        var version = _version?.Invoke() ?? 0;
        var rowPx = UiKit.ToPx(this, _rowHeight);
        var scroller = _view.Scroller;
        var viewHeight = scroller.Bounds.Height;
        if (viewHeight <= 0)
        {
            viewHeight = Tree?.Height ?? 0;
        }

        var sourceChanged = _invalid || !ReferenceEquals(items, _lastItems) || count != _lastCount || version != _lastVersion;
        if (sourceChanged)
        {
            Refind(items);
        }

        var needed = count == 0 ? 0 : Math.Min(count, (int)MathF.Ceiling(viewHeight / rowPx) + 2);
        var first = Math.Clamp((int)MathF.Floor(scroller.ScrollY / rowPx), 0, Math.Max(0, count - needed));
        if (!sourceChanged && first == _first && needed == _realized && rowPx == _lastRowPx)
        {
            return;
        }

        var layerHeight = count * rowPx;
        if (layerHeight != _lastLayerHeight)
        {
            _lastLayerHeight = layerHeight;
            _layer.Height(Length.Px(layerHeight));
        }

        while (_pool.Count < needed)
        {
            var row = _createRow();
            row.Role ??= "row";
            row.Class("kit-row");
            row.Style(s => s.Position(PositionMode.Absolute).Left(0f).Right(0f));
            _layer.Add(row);
            _pool.Add(row);
        }

        var prefix = _rowIdPrefix;
        for (var k = 0; k < _pool.Count; k++)
        {
            var row = _pool[k];
            if (k >= needed)
            {
                row.IsVisible = false;
                continue;
            }

            var index = first + k;
            row.IsVisible = true;
            row.Top(Length.Px(index * rowPx)).Height(Length.Px(rowPx));
            _bindRow(row, items[index], index);
            row.SetClass("selected", index == _selected);
            row.SetClass("odd", (index & 1) == 1);
            row.Value = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (prefix is not null)
            {
                row.Id = prefix + "/row/" + row.Value;
            }
        }

        _lastItems = items;
        _lastCount = count;
        _lastVersion = version;
        _first = first;
        _realized = needed;
        _lastRowPx = rowPx;
        _invalid = false;
    }

    // After the source changed, the selection follows its item to the new index, or clears when the item is gone.
    private void Refind(IReadOnlyList<T> items)
    {
        if (_selected < 0)
        {
            return;
        }

        var cmp = EqualityComparer<T>.Default;
        if (_selected < items.Count && cmp.Equals(items[_selected], _selectedItem))
        {
            return;
        }

        for (var i = 0; i < items.Count; i++)
        {
            if (cmp.Equals(items[i], _selectedItem))
            {
                _selected = i;
                Value = i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                return;
            }
        }

        _selected = -1;
        _selectedItem = default;
        Value = null;
    }
}

public static partial class UiKit
{
    /// <summary>
    /// A virtualized list: see <see cref="ListView{T}"/>. <paramref name="createRow"/> builds one row element and runs once per recycled row;
    /// <paramref name="bindRow"/> fills a row for an item and runs only when the row is assigned a different item (scrolling, a changed source), never on a frame where nothing changed.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="items">Returns the items, every frame. Return the same list instance while it is unchanged; a new instance, a different count or a different <paramref name="version"/> refreshes the rows.</param>
    /// <param name="createRow">Builds an empty row.</param>
    /// <param name="bindRow">Shows an item in a row: the row, the item and its index.</param>
    /// <param name="rowHeight">The height of every row, in UI units or pixels.</param>
    /// <param name="onSelect">Called with the item and index when the selection changes by the player or by <see cref="ListView{T}.Select"/>.</param>
    /// <param name="onActivate">Called when Enter, Space or the confirm button is pressed on the selected item.</param>
    /// <param name="id">The id, or null.</param>
    /// <param name="version">Optional counter the game bumps when items change in place; a change refreshes the rows.</param>
    public static ListView<T> VirtualList<T>(
        Func<IReadOnlyList<T>> items,
        Func<Element> createRow,
        Action<Element, T, int> bindRow,
        Length rowHeight,
        Action<T, int>? onSelect = null,
        Action<T, int>? onActivate = null,
        string? id = null,
        Func<int>? version = null) =>
        new(items, createRow, bindRow, rowHeight, version, onSelect, onActivate, id);

    /// <summary>A virtualized list of text rows, one line per item: see <see cref="VirtualList{T}"/>.</summary>
    /// <param name="items">Returns the items, every frame.</param>
    /// <param name="text">Gives the line of text for an item; it runs when a row is bound, not every frame.</param>
    /// <param name="rowHeight">The height of every row.</param>
    /// <param name="onSelect">Called when the selection changes.</param>
    /// <param name="onActivate">Called when the selected item is activated.</param>
    /// <param name="id">The id, or null.</param>
    /// <typeparam name="T">The item type.</typeparam>
    public static ListView<T> TextList<T>(Func<IReadOnlyList<T>> items, Func<T, string> text, Length rowHeight, Action<T, int>? onSelect = null, Action<T, int>? onActivate = null, string? id = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        return VirtualList(
            items,
            () => new Element("ListRow").Class("kit-list-row").With(Ui.Label("").Class("kit-cell")),
            (row, item, _) => row.ChildAt(0).Text = text(item),
            rowHeight,
            onSelect,
            onActivate,
            id);
    }
}
