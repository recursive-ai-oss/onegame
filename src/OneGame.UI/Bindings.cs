namespace OneGame.UI;

// Bindings are lambdas evaluated once per UiTree.Frame, parents before children. Each one compares its result with the last and
// touches the element only on a change, so an unchanged frame does no work beyond calling the lambdas.
internal abstract class Binding
{
    public abstract void Update(Element el);
}

internal sealed class TextBinding(Func<string> source) : Binding
{
    public override void Update(Element el) => el.Text = source();
}

internal sealed class TextBinding<T>(Func<T> source, Func<T, string> format) : Binding
    where T : IEquatable<T>
{
    private T _last = default!;
    private bool _has;

    public override void Update(Element el)
    {
        var v = source();
        if (_has && _last.Equals(v))
        {
            return;
        }

        _last = v;
        _has = true;
        el.Text = format(v);
    }
}

internal sealed class ClassBinding(string name, Func<bool> source) : Binding
{
    public override void Update(Element el) => el.SetClass(name, source());
}

internal sealed class VisibleBinding(Func<bool> source) : Binding
{
    public override void Update(Element el) => el.IsVisible = source();
}

internal sealed class EnabledBinding(Func<bool> source) : Binding
{
    public override void Update(Element el) => el.IsEnabled = source();
}

internal sealed class StyleBinding(Action<StyleBuilder> apply) : Binding
{
    public override void Update(Element el) => apply(el.Inline);
}

internal sealed class UpdateBinding(Action<Element> action) : Binding
{
    public override void Update(Element el) => action(el);
}

// Keeps an element's children in step with a list of items: an item keeps its element for as long as an equal item stays in the
// list, so row state such as focus, scroll position and hover survives re-sorting and filtering.
internal sealed class RepeatBinding<T>(Func<IReadOnlyList<T>> source, Func<T, Element> build) : Binding
{
    private readonly List<T> _items = [];
    private readonly List<Element> _elements = [];
    private readonly List<T> _newItems = [];
    private readonly List<Element> _newElements = [];

    public override void Update(Element el)
    {
        var src = source();
        var same = src.Count == _items.Count;
        if (same)
        {
            for (var i = 0; i < src.Count; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(src[i], _items[i]))
                {
                    same = false;
                    break;
                }
            }
        }

        if (same)
        {
            return;
        }

        _newItems.Clear();
        _newElements.Clear();
        var cmp = EqualityComparer<T>.Default;
        var cursor = 0;
        for (var i = 0; i < src.Count; i++)
        {
            var item = src[i];
            Element? found = null;
            // Look from where the last match was found so in-order lists cost one comparison per item.
            for (var k = 0; k < _items.Count; k++)
            {
                var j = (cursor + k) % _items.Count;
                if (_elements[j] is { } cand && cmp.Equals(_items[j], item))
                {
                    found = cand;
                    _elements[j] = null!;
                    cursor = j + 1;
                    break;
                }
            }

            _newItems.Add(item);
            _newElements.Add(found ?? build(item));
        }

        _items.Clear();
        _items.AddRange(_newItems);
        _elements.Clear();
        _elements.AddRange(_newElements);
        el.SetChildren(_newElements);
    }
}
