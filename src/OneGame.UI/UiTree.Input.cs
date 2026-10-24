using System.Numerics;
using P = OneGame.UI.StyleProp;

namespace OneGame.UI;

public sealed partial class UiTree
{
    private readonly List<Element> _focusList = [];
    private readonly Stack<Element> _walk = new();
    private Element? _hover;
    private Element? _pressed;
    private Element? _focused;
    private float _pointerX;
    private float _pointerY;

    /// <summary>The element with keyboard focus, or null.</summary>
    public Element? Focused => _focused;

    /// <summary>The deepest element under the pointer as of the last pointer event, or null.</summary>
    public Element? Hovered => _hover;

    /// <summary>The cursor the platform layer should show for the hovered element.</summary>
    public CursorShape Cursor => _hover is null ? CursorShape.Default : (CursorShape)_hover.Computed.E(P.Cursor);

    /// <summary>When true (the default), unhandled arrow keys move focus like the d-pad.</summary>
    public bool ArrowKeysNavigate { get; set; } = true;

    // ---- dispatch ------------------------------------------------------------------------------------------------

    /// <summary>
    /// Feeds one input event to the UI. Pointer events are hit-tested against the current layout, key and text events go to the focused element,
    /// and what no handler takes gets the default action: click on release, Tab and arrow focus moves, Enter or Space to click, wheel to scroll.
    /// Returns true when a handler or a default action consumed the event.
    /// </summary>
    public bool Dispatch(in UiEvent e)
    {
        Update();
        switch (e.Kind)
        {
            case UiEventKind.PointerMove:
                _pointerX = e.X;
                _pointerY = e.Y;
                var moved = HitTest(e.X, e.Y);
                SetHover(moved);
                return moved is not null && Bubble(moved, e);
            case UiEventKind.PointerLeave:
                SetHover(null);
                return false;
            case UiEventKind.PointerDown:
                return PointerDown(e);
            case UiEventKind.PointerUp:
                return PointerUp(e);
            case UiEventKind.Wheel:
                var over = HitTest(e.X, e.Y);
                return (over is not null && Bubble(over, e)) || ScrollAt(over, e.DeltaX, e.DeltaY);
            case UiEventKind.KeyDown:
                return KeyDown(e);
            case UiEventKind.KeyUp:
            case UiEventKind.TextInput:
                return Bubble(_focused ?? Root, e);
            case UiEventKind.Navigate:
                return Navigate(e);
            case UiEventKind.Activate:
                return _focused is not null && Invoke(_focused);
            case UiEventKind.Cancel:
                return Bubble(_focused ?? Root, e);
            default:
                return false;
        }
    }

    /// <summary>Clicks an element as a pointer or the confirm button would, without hit testing. Returns false when it is disabled or nothing handled the click.</summary>
    public bool Invoke(Element element)
    {
        ArgumentNullException.ThrowIfNull(element);
        Update();
        return Bubble(element, new UiEvent { Kind = UiEventKind.Click, X = _pointerX, Y = _pointerY });
    }

    private static bool Bubble(Element from, in UiEvent e)
    {
        if (!from.IsEffectivelyEnabled)
        {
            return false;
        }

        for (var el = from; el is not null; el = el.Parent)
        {
            if (el.Raise(e))
            {
                return true;
            }
        }

        return false;
    }

    private bool PointerDown(in UiEvent e)
    {
        _pointerX = e.X;
        _pointerY = e.Y;
        var target = HitTest(e.X, e.Y);
        SetHover(target);
        if (e.Button == 0)
        {
            _pressed = target;
            SetActive(target);
            Element? focusTarget = null;
            for (var a = target; a is not null; a = a.Parent)
            {
                if (IsFocusableNow(a))
                {
                    focusTarget = a;
                    break;
                }
            }

            Focus(focusTarget);
        }

        return target is not null && Bubble(target, e);
    }

    private bool PointerUp(in UiEvent e)
    {
        _pointerX = e.X;
        _pointerY = e.Y;
        var target = HitTest(e.X, e.Y);
        var handled = target is not null && Bubble(target, e);
        if (e.Button == 0)
        {
            var down = _pressed;
            _pressed = null;
            SetActive(null);
            if (down is not null && target is not null && Root.Contains(down))
            {
                // CSS UI Events: a click goes to the nearest common ancestor of the press and release targets.
                for (var a = down; a is not null; a = a.Parent)
                {
                    if (a.Contains(target))
                    {
                        handled |= Bubble(a, new UiEvent { Kind = UiEventKind.Click, X = e.X, Y = e.Y, Button = e.Button });
                        break;
                    }
                }
            }
        }

        return handled;
    }

    private bool KeyDown(in UiEvent e)
    {
        if (Bubble(_focused ?? Root, e))
        {
            return true;
        }

        var shift = (e.Modifiers & KeyModifiers.Shift) != 0;
        switch (e.Key)
        {
            case "Tab":
                return Navigate(UiEvent.Navigate(shift ? NavDirection.Previous : NavDirection.Next));
            case "Enter":
            case " ":
                return _focused is not null && Invoke(_focused);
            case "ArrowUp" when ArrowKeysNavigate:
                return Navigate(UiEvent.Navigate(NavDirection.Up));
            case "ArrowDown" when ArrowKeysNavigate:
                return Navigate(UiEvent.Navigate(NavDirection.Down));
            case "ArrowLeft" when ArrowKeysNavigate:
                return Navigate(UiEvent.Navigate(NavDirection.Left));
            case "ArrowRight" when ArrowKeysNavigate:
                return Navigate(UiEvent.Navigate(NavDirection.Right));
            case "Escape":
                return Bubble(_focused ?? Root, UiEvent.Cancel());
            default:
                return false;
        }
    }

    private bool Navigate(in UiEvent e) => Bubble(_focused ?? Root, e) || MoveFocus(e.Direction);

    private static bool ScrollAt(Element? target, float dx, float dy)
    {
        for (var a = target; a is not null; a = a.Parent)
        {
            var max = a.MaxScroll;
            var canY = dy != 0 && a.Scrolls(false) && (dy > 0 ? a.ScrollY < max.Y : a.ScrollY > 0);
            var canX = dx != 0 && a.Scrolls(true) && (dx > 0 ? a.ScrollX < max.X : a.ScrollX > 0);
            if (canX || canY)
            {
                a.ScrollTo(a.ScrollX + (canX ? dx : 0), a.ScrollY + (canY ? dy : 0));
                return true;
            }
        }

        return false;
    }

    // ---- hover, press, focus state -------------------------------------------------------------------------------

    private void SetHover(Element? target)
    {
        if (_hover == target)
        {
            return;
        }

        var old = _hover;
        _hover = target;
        for (var e = old; e is not null; e = e.Parent)
        {
            if (!e.Contains(target) && e.HoverChain)
            {
                e.HoverChain = false;
                e.MarkStyle(AncestorSelectors);
                e.Raise(new UiEvent { Kind = UiEventKind.PointerLeave, X = _pointerX, Y = _pointerY });
            }
        }

        for (var e = target; e is not null; e = e.Parent)
        {
            if (!e.HoverChain)
            {
                e.HoverChain = true;
                e.MarkStyle(AncestorSelectors);
                e.Raise(new UiEvent { Kind = UiEventKind.PointerEnter, X = _pointerX, Y = _pointerY });
            }
        }
    }

    private Element? _active;

    private void SetActive(Element? target)
    {
        if (_active == target)
        {
            return;
        }

        for (var e = _active; e is not null; e = e.Parent)
        {
            if (e.ActiveChain)
            {
                e.ActiveChain = false;
                e.MarkStyle(AncestorSelectors);
            }
        }

        _active = target;
        for (var e = target; e is not null; e = e.Parent)
        {
            if (!e.ActiveChain)
            {
                e.ActiveChain = true;
                e.MarkStyle(AncestorSelectors);
            }
        }
    }

    // Elements removed from the tree while hovered, pressed or focused must not keep the state.
    private void DropStaleReferences()
    {
        if (_focused is not null && !Root.Contains(_focused))
        {
            _focused.FocusFlag = false;
            _focused = null;
        }

        if (_hover is not null && !Root.Contains(_hover))
        {
            SetHover(null);
        }

        if (_active is not null && !Root.Contains(_active))
        {
            SetActive(null);
        }

        if (_pressed is not null && !Root.Contains(_pressed))
        {
            _pressed = null;
        }
    }

    // ---- focus ---------------------------------------------------------------------------------------------------

    private static bool IsFocusableNow(Element e)
    {
        if (!e.IsFocusable || !e.IsEffectivelyEnabled || e.Computed.E(P.Visibility) != (int)VisibilityMode.Visible)
        {
            return false;
        }

        for (var a = e; a is not null; a = a.Parent)
        {
            if (!a.InLayout)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Moves keyboard focus to <paramref name="element"/>, or clears it with null. Returns false when the element cannot take focus (not focusable, disabled, hidden or not in this tree).</summary>
    public bool Focus(Element? element)
    {
        Update();
        if (element is not null && (!Root.Contains(element) || !IsFocusableNow(element)))
        {
            return false;
        }

        if (_focused == element)
        {
            return true;
        }

        var old = _focused;
        _focused = element;
        if (old is not null)
        {
            old.FocusFlag = false;
            old.MarkStyle(AncestorSelectors);
            old.Raise(new UiEvent { Kind = UiEventKind.Blur });
        }

        if (element is not null)
        {
            element.FocusFlag = true;
            element.MarkStyle(AncestorSelectors);
            element.Raise(new UiEvent { Kind = UiEventKind.Focus });
            ScrollIntoView(element);
        }

        return true;
    }

    /// <summary>Scrolls the ancestors of <paramref name="element"/> that scroll so that it is inside their padding boxes, as far as it fits.</summary>
    public static void ScrollIntoView(Element element)
    {
        ArgumentNullException.ThrowIfNull(element);
        for (var a = element.Parent; a is not null; a = a.Parent)
        {
            if (!a.Scrolls(true) && !a.Scrolls(false))
            {
                continue;
            }

            var ar = a.AbsoluteBounds;
            var er = element.AbsoluteBounds;
            var left = ar.X + a.PX;
            var top = ar.Y + a.PY;
            var dx = 0f;
            var dy = 0f;
            if (er.X < left)
            {
                dx = er.X - left;
            }
            else if (er.Right > left + a.PW)
            {
                dx = MathF.Min(er.Right - (left + a.PW), er.X - left);
            }

            if (er.Y < top)
            {
                dy = er.Y - top;
            }
            else if (er.Bottom > top + a.PH)
            {
                dy = MathF.Min(er.Bottom - (top + a.PH), er.Y - top);
            }

            if (dx != 0 || dy != 0)
            {
                a.ScrollTo(a.ScrollX + dx, a.ScrollY + dy);
            }
        }
    }

    /// <summary>
    /// Moves focus in a direction. Next and Previous follow tab order (positive <see cref="Element.TabIndex"/> first, then document order, wrapping).
    /// Up, Down, Left and Right pick the focusable element nearest in that direction by edge distance, preferring ones that overlap the current element's row or column.
    /// With nothing focused, the first focusable element takes focus. Focus stays inside a shown <see cref="Element.TrapsFocus"/> element. Returns true when focus moved.
    /// </summary>
    public bool MoveFocus(NavDirection direction)
    {
        Update();
        var scope = FindTrap() ?? Root;
        CollectFocusable(scope, _focusList);
        if (_focusList.Count == 0)
        {
            return false;
        }

        var cur = _focused;
        var idx = cur is null ? -1 : _focusList.IndexOf(cur);
        Element? next;
        if (idx < 0)
        {
            next = _focusList[0];
        }
        else if (direction == NavDirection.Next)
        {
            next = _focusList[(idx + 1) % _focusList.Count];
        }
        else if (direction == NavDirection.Previous)
        {
            next = _focusList[(idx + _focusList.Count - 1) % _focusList.Count];
        }
        else
        {
            next = Directional(cur!, direction, _focusList);
        }

        return next is not null && next != cur && Focus(next);
    }

    private Element? FindTrap()
    {
        Element? last = null;
        _walk.Clear();
        _walk.Push(Root);
        while (_walk.Count > 0)
        {
            var e = _walk.Pop();
            if (!e.InLayout)
            {
                continue;
            }

            if (e.TrapsFocus)
            {
                last = e;
            }

            for (var i = e.ChildCount - 1; i >= 0; i--)
            {
                _walk.Push(e.ChildAt(i));
            }
        }

        return last;
    }

    private void CollectFocusable(Element scope, List<Element> output)
    {
        output.Clear();
        _walk.Clear();
        _walk.Push(scope);
        while (_walk.Count > 0)
        {
            var e = _walk.Pop();
            if (!e.InLayout || !e.IsEnabled)
            {
                continue;
            }

            if (e.IsFocusable && e.Computed.E(P.Visibility) == (int)VisibilityMode.Visible)
            {
                output.Add(e);
            }

            for (var i = e.ChildCount - 1; i >= 0; i--)
            {
                _walk.Push(e.ChildAt(i));
            }
        }

        // HTML tab order: positive tab indexes ascending first, then the rest in document order. The insertion sort is stable.
        for (var i = 1; i < output.Count; i++)
        {
            var x = output[i];
            var key = TabKey(x);
            var j = i - 1;
            while (j >= 0 && TabKey(output[j]) > key)
            {
                output[j + 1] = output[j];
                j--;
            }

            output[j + 1] = x;
        }
    }

    private static int TabKey(Element e) => e.TabIndex > 0 ? e.TabIndex : int.MaxValue;

    private static Element? Directional(Element from, NavDirection d, List<Element> candidates)
    {
        var r0 = from.AbsoluteBounds;
        var c0 = r0.Center;
        Element? best = null;
        var bestScore = float.PositiveInfinity;
        foreach (var c in candidates)
        {
            if (c == from)
            {
                continue;
            }

            var r = c.AbsoluteBounds;
            var cc = r.Center;
            float primary;
            float orthGap;
            float orthCenter;
            bool ahead;
            switch (d)
            {
                case NavDirection.Right:
                    ahead = cc.X > c0.X;
                    primary = MathF.Max(0, r.X - r0.Right);
                    orthGap = Gap(r0.Y, r0.Bottom, r.Y, r.Bottom);
                    orthCenter = MathF.Abs(cc.Y - c0.Y);
                    break;
                case NavDirection.Left:
                    ahead = cc.X < c0.X;
                    primary = MathF.Max(0, r0.X - r.Right);
                    orthGap = Gap(r0.Y, r0.Bottom, r.Y, r.Bottom);
                    orthCenter = MathF.Abs(cc.Y - c0.Y);
                    break;
                case NavDirection.Down:
                    ahead = cc.Y > c0.Y;
                    primary = MathF.Max(0, r.Y - r0.Bottom);
                    orthGap = Gap(r0.X, r0.Right, r.X, r.Right);
                    orthCenter = MathF.Abs(cc.X - c0.X);
                    break;
                default:
                    ahead = cc.Y < c0.Y;
                    primary = MathF.Max(0, r0.Y - r.Bottom);
                    orthGap = Gap(r0.X, r0.Right, r.X, r.Right);
                    orthCenter = MathF.Abs(cc.X - c0.X);
                    break;
            }

            if (!ahead)
            {
                continue;
            }

            var score = primary + (3 * orthGap) + (0.1f * orthCenter) + (0.001f * Vector2.Distance(c0, cc));
            if (score < bestScore)
            {
                bestScore = score;
                best = c;
            }
        }

        return best;
    }

    // Distance between two intervals on one axis; zero when they overlap.
    private static float Gap(float a0, float a1, float b0, float b1) => MathF.Max(0, MathF.Max(b0 - a1, a0 - b1));

    // ---- hit testing ---------------------------------------------------------------------------------------------

    /// <summary>The topmost element at a screen position, honoring clipping, scroll offsets, transforms, z-index, <c>visibility</c> and <c>pointer-events</c>; null when nothing is there.</summary>
    public Element? HitTest(float x, float y)
    {
        Update();
        return Hit(Root, x, y, x, y);
    }

    private Element? Hit(Element e, float px, float py, float sx, float sy)
    {
        if (!e.InLayout)
        {
            return null;
        }

        var s = e.Computed;
        var fixedPos = s.E(P.Position) == (int)PositionMode.Fixed;
        var lx = (fixedPos ? sx : px) - e.LX;
        var ly = (fixedPos ? sy : py) - e.LY;
        if (_painter.TransformOf(e) is { } m && Matrix3x2.Invert(m, out var inv))
        {
            var p = Vector2.Transform(new Vector2(lx, ly), inv);
            lx = p.X;
            ly = p.Y;
        }

        var inside = lx >= 0 && ly >= 0 && lx < e.LW && ly < e.LH;
        if (!inside && (e.Scrolls(true) || e.Scrolls(false)))
        {
            return null;
        }

        var n = e.ChildCount;
        if (n > 0)
        {
            var cx = lx + e.ScrollX;
            var cy = ly + e.ScrollY;
            var order = _painter.Order(e);
            Element? found = null;
            if (order is null)
            {
                for (var i = n - 1; i >= 0 && found is null; i--)
                {
                    found = Hit(e.ChildAt(i), cx, cy, sx, sy);
                }
            }
            else
            {
                for (var i = order.Count - 1; i >= 0 && found is null; i--)
                {
                    found = Hit(order[i], cx, cy, sx, sy);
                }

                _painter.Release(order);
            }

            if (found is not null)
            {
                return found;
            }
        }

        return inside && s.E(P.Visibility) == (int)VisibilityMode.Visible && s.E(P.PointerEvents) == (int)PointerEvents.Auto ? e : null;
    }
}
