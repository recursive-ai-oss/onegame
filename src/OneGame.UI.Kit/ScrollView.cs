namespace OneGame.UI.Kit;

/// <summary>
/// A scrolling container with scroll bars (kind <c>ScrollView</c>, role <c>scrollview</c>). Content larger than the view scrolls with the wheel, by dragging a bar's thumb,
/// by clicking the bar's track (one page toward the click) and when a focused descendant needs to be seen. The bars are drawn over the view, so they take no layout space,
/// and only show while there is something to scroll. Parts: <c>id/scroller</c> (the element that scrolls), <c>id/vbar</c>, <c>id/vthumb</c>, <c>id/hbar</c>, <c>id/hthumb</c>.
/// </summary>
public sealed class ScrollView : Element
{
    private readonly Bar _vertical;
    private readonly Bar? _horizontal;

    internal ScrollView(Element content, bool horizontal, string? id)
        : base("ScrollView")
    {
        ArgumentNullException.ThrowIfNull(content);
        Id = id;
        Role = "scrollview";
        Class("kit-scrollview");
        Scroller = UiKit.Part("Box", "kit-scroller", id, "scroller");
        Scroller.Style(s => s.Direction(FlexDirection.Column).Overflow(horizontal ? OverflowMode.Auto : OverflowMode.Hidden, OverflowMode.Auto).Grow(1).MinHeight(0).MinWidth(0));
        Content = content;
        content.Shrink(0);
        Scroller.Add(content);
        Add(Scroller);
        _vertical = new Bar(Scroller, vertical: true, id);
        Add(_vertical.Track);
        if (horizontal)
        {
            _horizontal = new Bar(Scroller, vertical: false, id);
            Add(_horizontal.Track);
        }

        On(UiEventKind.Wheel, (_, e) =>
        {
            var before = (Scroller.ScrollX, Scroller.ScrollY);
            Scroller.ScrollTo(Scroller.ScrollX + (horizontal ? e.DeltaX : 0), Scroller.ScrollY + e.DeltaY);
            return before != (Scroller.ScrollX, Scroller.ScrollY);
        });
        OnUpdate(_ =>
        {
            _vertical.Update();
            _horizontal?.Update();
        });
    }

    /// <summary>The element that scrolls; its <see cref="Element.ScrollX"/> and <see cref="Element.ScrollY"/> are the position.</summary>
    public Element Scroller { get; }

    /// <summary>The content that was passed in.</summary>
    public Element Content { get; }

    // One scroll bar: a track along an edge of the view with a draggable thumb whose size and place follow the scroll range.
    private sealed class Bar
    {
        private readonly Element _scroller;
        private readonly Element _thumb;
        private readonly bool _vertical;
        private float _lastLength = -1;
        private float _lastOffset = -1;
        private float _grab;

        public Bar(Element scroller, bool vertical, string? id)
        {
            _scroller = scroller;
            _vertical = vertical;
            var name = vertical ? "v" : "h";
            Track = UiKit.Part("Box", vertical ? "kit-scrollbar vertical" : "kit-scrollbar horizontal", id, name + "bar");
            Track.IsVisible = false;
            _thumb = UiKit.Part("Box", vertical ? "kit-scrollbar-thumb vertical" : "kit-scrollbar-thumb horizontal", id, name + "thumb");
            Track.Add(_thumb);
            Track.Role = "scrollbar";
            _thumb.On(UiEventKind.PointerDown, (t, e) =>
            {
                if (e.Button != 0)
                {
                    return false;
                }

                var b = t.AbsoluteBounds;
                _grab = (_vertical ? e.Y - b.Y : e.X - b.X);
                t.Tree?.CapturePointer(t);
                return true;
            });
            _thumb.On(UiEventKind.PointerMove, (t, e) =>
            {
                if (t.Tree?.PointerCaptor != t)
                {
                    return false;
                }

                var tb = Track.AbsoluteBounds;
                var trackLen = _vertical ? tb.Height : tb.Width;
                var thumbLen = _vertical ? t.Bounds.Height : t.Bounds.Width;
                var free = trackLen - thumbLen;
                if (free <= 0)
                {
                    return true;
                }

                var pos = (_vertical ? e.Y - tb.Y : e.X - tb.X) - _grab;
                var fraction = Math.Clamp(pos / free, 0f, 1f);
                var max = _scroller.MaxScroll;
                if (_vertical)
                {
                    _scroller.ScrollTo(_scroller.ScrollX, fraction * max.Y);
                }
                else
                {
                    _scroller.ScrollTo(fraction * max.X, _scroller.ScrollY);
                }

                return true;
            });
            Track.On(UiEventKind.PointerDown, (t, e) =>
            {
                // A click on the bare track pages toward the click, as a desktop scroll bar does.
                if (e.Button != 0)
                {
                    return false;
                }

                var thumb = _thumb.AbsoluteBounds;
                var before = _vertical ? e.Y < thumb.Y : e.X < thumb.X;
                var page = _vertical ? _scroller.Bounds.Height : _scroller.Bounds.Width;
                var sign = before ? -1 : 1;
                if (_vertical)
                {
                    _scroller.ScrollTo(_scroller.ScrollX, _scroller.ScrollY + (sign * page));
                }
                else
                {
                    _scroller.ScrollTo(_scroller.ScrollX + (sign * page), _scroller.ScrollY);
                }

                return true;
            });
        }

        public Element Track { get; }

        public void Update()
        {
            var max = _vertical ? _scroller.MaxScroll.Y : _scroller.MaxScroll.X;
            var show = max > 0.5f;
            if (Track.IsVisible != show)
            {
                Track.IsVisible = show;
            }

            if (!show)
            {
                return;
            }

            var view = _vertical ? _scroller.Bounds.Height : _scroller.Bounds.Width;
            var content = _vertical ? _scroller.ScrollSize.Y : _scroller.ScrollSize.X;
            var trackLen = _vertical ? Track.Bounds.Height : Track.Bounds.Width;
            if (trackLen <= 0 || content <= 0)
            {
                return;
            }

            var minThumb = UiKit.ToPx(Track, Length.Ui(24));
            var length = Math.Clamp(trackLen * view / content, Math.Min(minThumb, trackLen), trackLen);
            var scroll = _vertical ? _scroller.ScrollY : _scroller.ScrollX;
            var offset = (trackLen - length) * Math.Clamp(scroll / max, 0f, 1f);
            if (length != _lastLength)
            {
                _lastLength = length;
                if (_vertical)
                {
                    _thumb.Height(Length.Px(length));
                }
                else
                {
                    _thumb.Width(Length.Px(length));
                }
            }

            if (offset != _lastOffset)
            {
                _lastOffset = offset;
                if (_vertical)
                {
                    _thumb.Top(Length.Px(offset));
                }
                else
                {
                    _thumb.Left(Length.Px(offset));
                }
            }
        }
    }
}

public static partial class UiKit
{
    /// <summary>Wraps <paramref name="content"/> in a scroll view with scroll bars. See <see cref="ScrollView"/>.</summary>
    /// <param name="content">What scrolls.</param>
    /// <param name="horizontal">Also scroll sideways when the content is wider than the view; by default only vertically.</param>
    /// <param name="id">The id, or null.</param>
    public static ScrollView ScrollView(Element content, bool horizontal = false, string? id = null) => new(content, horizontal, id);
}
