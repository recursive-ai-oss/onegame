namespace OneGame.UI.Kit;

/// <summary>How wide a table column is: a fixed length, or a share of the space the fixed columns leave.</summary>
/// <param name="Size">The fixed width in pixels or UI units; ignored for a flexible column.</param>
/// <param name="Grow">Zero for a fixed column; otherwise the column's share of the remaining width (a column with 2 gets twice a column with 1).</param>
public readonly record struct ColumnWidth(Length Size, float Grow)
{
    /// <summary>A column of exactly this width.</summary>
    public static ColumnWidth Fixed(Length size) => new(size, 0);

    /// <summary>A column that shares the remaining width in proportion to <paramref name="grow"/>.</summary>
    public static ColumnWidth Flex(float grow = 1) => new(Length.Auto, grow);
}

/// <summary>One column of a <see cref="TableView{T}"/>: a title, a width, and how a cell shows an item.</summary>
/// <typeparam name="T">The row item type.</typeparam>
public sealed class TableColumn<T>
{
    /// <summary>Creates a column whose cells show text.</summary>
    /// <param name="title">The header text.</param>
    /// <param name="text">Gives a cell's text for an item; it runs when a row is bound, not every frame.</param>
    /// <param name="width">The width; flexible with a share of 1 by default.</param>
    /// <param name="sortable">True when clicking the header asks the game to sort by this column.</param>
    /// <param name="alignEnd">Right-align the cells (numbers).</param>
    public TableColumn(string title, Func<T, string> text, ColumnWidth? width = null, bool sortable = true, bool alignEnd = false)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Width = width ?? ColumnWidth.Flex();
        Sortable = sortable;
        AlignEnd = alignEnd;
    }

    /// <summary>Creates a column with its own cell element, for icons, bars or buttons.</summary>
    /// <param name="title">The header text.</param>
    /// <param name="createCell">Builds an empty cell; it runs once per recycled row.</param>
    /// <param name="bindCell">Shows an item in a cell; it runs when the row is bound.</param>
    /// <param name="width">The width; flexible with a share of 1 by default.</param>
    /// <param name="sortable">True when clicking the header asks the game to sort by this column.</param>
    public TableColumn(string title, Func<Element> createCell, Action<Element, T> bindCell, ColumnWidth? width = null, bool sortable = false)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        CreateCell = createCell ?? throw new ArgumentNullException(nameof(createCell));
        BindCell = bindCell ?? throw new ArgumentNullException(nameof(bindCell));
        Width = width ?? ColumnWidth.Flex();
        Sortable = sortable;
    }

    /// <summary>The header text.</summary>
    public string Title { get; }

    /// <summary>The column width.</summary>
    public ColumnWidth Width { get; }

    /// <summary>True when the header can be clicked to sort.</summary>
    public bool Sortable { get; }

    /// <summary>True when text cells are right-aligned.</summary>
    public bool AlignEnd { get; }

    internal Func<T, string>? Text { get; }

    internal Func<Element>? CreateCell { get; }

    internal Action<Element, T>? BindCell { get; }
}

/// <summary>
/// A table (kind <c>Table</c>, role <c>table</c>) with a header row and a virtualized body, so 5,000 rows cost about as much as 30.
/// The header cells are kind <c>HeaderCell</c>, role <c>columnheader</c>, ids <c>id/header/0</c>...; a sortable one is focusable, and a click or Enter asks the game to sort
/// (first ascending, then flipping) through the callback: the game sorts its list, and the table rebinds the rows and keeps the same item selected.
/// The sorted header has the classes <c>sorted</c> and <c>asc</c> or <c>desc</c>. The body is a <see cref="ListView{T}"/> (<see cref="Body"/>): it takes focus,
/// Up/Down/PageUp/PageDown/Home/End move the selection, and Enter or the confirm button activates the selected row. Rows are role <c>row</c>, ids <c>id/row/n</c>.
/// </summary>
/// <typeparam name="T">The row item type.</typeparam>
public sealed class TableView<T> : Element
{
    private readonly TableColumn<T>[] _columns;
    private readonly Element[] _headers;
    private readonly Action<int, bool>? _onSort;
    private int _sortColumn = -1;
    private bool _descending;

    internal TableView(
        IReadOnlyList<TableColumn<T>> columns,
        Func<IReadOnlyList<T>> rows,
        Length rowHeight,
        Action<int, bool>? onSort,
        Action<T, int>? onSelect,
        Action<T, int>? onActivate,
        string? id,
        Func<int>? version)
        : base("Table")
    {
        ArgumentNullException.ThrowIfNull(columns);
        if (columns.Count == 0)
        {
            throw new ArgumentException("A table needs at least one column.", nameof(columns));
        }

        _columns = [.. columns];
        _onSort = onSort;
        Id = id;
        Role = "table";
        Class("kit-table");
        var header = UiKit.Part("Box", "kit-table-header", id, "header");
        header.Role = "row";
        _headers = new Element[_columns.Length];
        for (var c = 0; c < _columns.Length; c++)
        {
            var col = _columns[c];
            var column = c;
            var cell = UiKit.Make("HeaderCell", "columnheader", "kit-header-cell", id is null ? null : id + "/header/" + c.ToString(System.Globalization.CultureInfo.InvariantCulture), focusable: col.Sortable);
            cell.Value = col.Title;
            ApplyWidth(cell, col.Width);
            if (col.AlignEnd)
            {
                cell.SetClass("end", true);
            }

            cell.Add(Ui.Label(col.Title).Class("kit-header-title"));
            cell.Add(UiKit.Part("Box", "kit-sort", cell.Id, "sort"));
            if (col.Sortable)
            {
                cell.OnClick(() => ClickHeader(column));
            }

            _headers[c] = cell;
            header.Add(cell);
        }

        Body = new ListView<T>(
            rows,
            CreateRow,
            BindRow,
            rowHeight,
            version,
            onSelect,
            onActivate,
            id,
            "TableBody");
        Body.Class("kit-table-body");
        this.With(header, Body);
    }

    /// <summary>The body: selection, scrolling and the virtualization counts live here.</summary>
    public ListView<T> Body { get; }

    /// <summary>The index of the column the table is sorted by, or -1 when no sort was chosen.</summary>
    public int SortColumn => _sortColumn;

    /// <summary>True when the sort is descending.</summary>
    public bool SortDescending => _descending;

    /// <summary>Sets the sort indicator without calling the sort callback, for a table that starts out sorted.</summary>
    /// <param name="column">The sorted column, or -1 for none.</param>
    /// <param name="descending">True for descending.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="column"/> is not a column index.</exception>
    public void SetSort(int column, bool descending)
    {
        if (column < -1 || column >= _columns.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(column), column, "There is no such column.");
        }

        _sortColumn = column;
        _descending = descending;
        for (var i = 0; i < _headers.Length; i++)
        {
            var on = i == column;
            _headers[i].SetClass("sorted", on);
            _headers[i].SetClass("asc", on && !descending);
            _headers[i].SetClass("desc", on && descending);
            _headers[i].ChildAt(1).SetClass("asc", on && !descending);
            _headers[i].ChildAt(1).SetClass("desc", on && descending);
            _headers[i].ChildAt(1).SetClass("sorted", on);
        }
    }

    private static void ApplyWidth(Element cell, ColumnWidth w)
    {
        if (w.Grow > 0)
        {
            cell.Style(s => s.Flex(w.Grow, 1f, Length.Px(0)).MinWidth(0f));
        }
        else
        {
            cell.Style(s => s.Flex(0f, 0f, w.Size));
        }
    }

    private void ClickHeader(int column)
    {
        var descending = _sortColumn == column && !_descending;
        SetSort(column, descending);
        _onSort?.Invoke(column, descending);
        Body.Invalidate();
    }

    private Element CreateRow()
    {
        var row = new Element("TableRow").Class("kit-table-row");
        for (var c = 0; c < _columns.Length; c++)
        {
            var col = _columns[c];
            Element cell;
            if (col.CreateCell is not null)
            {
                cell = col.CreateCell();
                cell.Class("kit-cell");
            }
            else
            {
                cell = Ui.Label("").Class("kit-cell");
                if (col.AlignEnd)
                {
                    cell.SetClass("end", true);
                }
            }

            cell.Role ??= "cell";
            ApplyWidth(cell, col.Width);
            row.Add(cell);
        }

        return row;
    }

    private void BindRow(Element row, T item, int index)
    {
        for (var c = 0; c < _columns.Length; c++)
        {
            var col = _columns[c];
            var cell = row.ChildAt(c);
            if (col.BindCell is not null)
            {
                col.BindCell(cell, item);
            }
            else
            {
                cell.Text = col.Text!(item);
            }
        }
    }
}

public static partial class UiKit
{
    /// <summary>
    /// A sortable, virtualized table: see <see cref="TableView{T}"/>.
    /// </summary>
    /// <typeparam name="T">The row item type.</typeparam>
    /// <param name="columns">The columns, left to right.</param>
    /// <param name="rows">Returns the rows, every frame. Return the same list instance while it is unchanged; a new instance (for example after filtering), a different count or a different <paramref name="version"/> refreshes the rows.</param>
    /// <param name="rowHeight">The height of every row, in UI units or pixels.</param>
    /// <param name="onSort">Called when a sortable header is clicked: the column index and whether the order is descending. Sort the list you return from <paramref name="rows"/>.</param>
    /// <param name="onSelect">Called with the item and its index when the selection changes.</param>
    /// <param name="onActivate">Called when Enter, Space or the confirm button is pressed on the selected row.</param>
    /// <param name="id">The id, or null.</param>
    /// <param name="version">Optional counter the game bumps when rows change in place.</param>
    public static TableView<T> Table<T>(
        IReadOnlyList<TableColumn<T>> columns,
        Func<IReadOnlyList<T>> rows,
        Length rowHeight,
        Action<int, bool>? onSort = null,
        Action<T, int>? onSelect = null,
        Action<T, int>? onActivate = null,
        string? id = null,
        Func<int>? version = null) =>
        new(columns, rows, rowHeight, onSort, onSelect, onActivate, id, version);
}
