using P = OneGame.UI.StyleProp;

namespace OneGame.UI;

internal sealed partial class FlexEngine
{
    private Item[] RentItems(int n)
    {
        for (var i = 0; i < _itemPool.Count; i++)
        {
            if (_itemPool[i].Length >= n)
            {
                var a = _itemPool[i];
                _itemPool.RemoveAt(i);
                return a;
            }
        }

        return new Item[Math.Max(n, 16)];
    }

    private void ReturnItems(Item[] a) => _itemPool.Add(a);

    private Line[] RentLines(int n)
    {
        for (var i = 0; i < _linePool.Count; i++)
        {
            if (_linePool[i].Length >= n)
            {
                var a = _linePool[i];
                _linePool.RemoveAt(i);
                return a;
            }
        }

        return new Line[Math.Max(n, 16)];
    }

    private void ReturnLines(Line[] a) => _linePool.Add(a);

    // Box Alignment: where the first item starts and how much extra goes between items, for a given free space.
    private static void Distribute(Align mode, float free, int n, out float offset, out float extra)
    {
        offset = 0;
        extra = 0;
        switch (mode)
        {
            case Align.End:
                offset = free;
                break;
            case Align.Center:
                offset = free / 2;
                break;
            case Align.SpaceBetween:
                if (free > 0 && n > 1)
                {
                    extra = free / (n - 1);
                }

                break;
            case Align.SpaceAround:
                if (free > 0)
                {
                    extra = free / n;
                    offset = extra / 2;
                }
                else
                {
                    offset = free / 2;
                }

                break;
            case Align.SpaceEvenly:
                if (free > 0)
                {
                    extra = free / (n + 1);
                    offset = extra;
                }
                else
                {
                    offset = free / 2;
                }

                break;
            default:
                break;
        }
    }

    // §9.4 step 7: the hypothetical cross size of a row item is its content height at the width it ended up with.
    private void ComputeCross(ref Item it, bool row, float innerW, float innerH)
    {
        if (!row)
        {
            return;
        }

        var cross = float.IsNaN(it.SpecCross) ? ContentHeight(it.El, it.Target, innerW) : it.SpecCross;
        it.Cross = MathF.Max(Clamp(cross, it.MinCross, it.MaxCross), it.PadCross);
    }

    // §9.6 Cross-Axis Alignment: auto margins first, then align-self.
    private static void AlignCross(ref Item it, float lineCross, Align containerAlign, float innerW, float innerH, bool row)
    {
        var cs = it.CrossStartM;
        var ce = it.CrossEndM;
        var free = lineCross - it.Cross - Pos(cs) - Pos(ce);
        if (float.IsNaN(cs) || float.IsNaN(ce))
        {
            free = MathF.Max(0, free);
            if (float.IsNaN(cs) && float.IsNaN(ce))
            {
                cs = free / 2;
            }
            else if (float.IsNaN(cs))
            {
                cs = free;
            }

            it.CrossPos = cs;
            return;
        }

        switch (it.Self)
        {
            case Align.End:
                it.CrossPos = lineCross - ce - it.Cross;
                break;
            case Align.Center:
                it.CrossPos = cs + (free / 2);
                break;
            case Align.Stretch:
                if (it.CrossAuto)
                {
                    it.Cross = MathF.Max(Clamp(lineCross - cs - ce, it.MinCross, it.MaxCross), it.PadCross);
                }

                it.CrossPos = cs;
                break;
            default:
                it.CrossPos = cs;
                break;
        }
    }

    // ---- out-of-flow boxes ---------------------------------------------------------------------------------------

    // Absolutely positioned children are placed against the parent's padding box (CSS Positioned Layout 3 §4, with the
    // parent always acting as the containing block).
    private void LayoutAbsolute(Element el, float w, float h, in Frame f, bool row, Align justify, Align containerAlign, ref float maxRight, ref float maxBottom)
    {
        for (var i = 0; i < el.ChildCount; i++)
        {
            var c = el.ChildAt(i);
            if (!c.InLayout || c.Computed.E(P.Position) != (int)PositionMode.Absolute)
            {
                continue;
            }

            var selfAlign = (Align)c.Computed.E(P.AlignSelf);
            if (selfAlign == Align.Auto)
            {
                selfAlign = containerAlign;
            }

            var alignX = row ? justify : selfAlign;
            var alignY = row ? selfAlign : justify;
            PlaceOutOfFlow(c, f.BL, f.BT, w - f.BL - f.BR, h - f.BT - f.BB, f.PL, f.PT, w - f.PadW, h - f.PadH, alignX, alignY);
            maxRight = MathF.Max(maxRight, c.LX + c.LW);
            maxBottom = MathF.Max(maxBottom, c.LY + c.LH);
        }
    }

    /// <summary>Lays out every <c>position: fixed</c> element in the tree against the screen.</summary>
    public void LayoutFixed(Element root)
    {
        var stack = new Stack<Element>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var e = stack.Pop();
            for (var i = 0; i < e.ChildCount; i++)
            {
                var c = e.ChildAt(i);
                if (!c.InLayout)
                {
                    continue;
                }

                if (c.Computed.E(P.Position) == (int)PositionMode.Fixed)
                {
                    PlaceOutOfFlow(c, 0, 0, ui.Width, ui.Height, 0, 0, ui.Width, ui.Height, Align.Start, Align.Start);
                }

                stack.Push(c);
            }
        }
    }

    // CSS 2.1 §10.3.7 and §10.6.4 for both axes: width/insets/margins, then height/insets/margins.
    private void PlaceOutOfFlow(Element c, float cbX, float cbY, float cbW, float cbH, float staticX, float staticY, float innerW, float innerH, Align alignX, Align alignY)
    {
        var s = c.Computed;
        var f = GetFrame(c, cbW);
        var left = R(s.L(P.Left), cbW);
        var right = R(s.L(P.Right), cbW);
        var top = R(s.L(P.Top), cbH);
        var bottom = R(s.L(P.Bottom), cbH);
        var mL = RM(s.L(P.MarginLeft), cbW);
        var mR = RM(s.L(P.MarginRight), cbW);
        var mT = RM(s.L(P.MarginTop), cbW);
        var mB = RM(s.L(P.MarginBottom), cbW);

        var w = Spec(c, P.Width, cbW, f, true);
        if (float.IsNaN(w))
        {
            w = !float.IsNaN(left) && !float.IsNaN(right)
                ? cbW - left - right - Pos(mL) - Pos(mR)
                : FitContent(c, cbW - Pos(left) - Pos(right) - Pos(mL) - Pos(mR), cbW);
        }

        w = MathF.Max(Clamp(w, Spec(c, P.MinWidth, cbW, f, true), Spec(c, P.MaxWidth, cbW, f, true)), f.PadW);

        var h = Spec(c, P.Height, cbH, f, false);
        if (float.IsNaN(h))
        {
            h = !float.IsNaN(top) && !float.IsNaN(bottom)
                ? cbH - top - bottom - Pos(mT) - Pos(mB)
                : ContentHeight(c, w, cbW);
        }

        h = MathF.Max(Clamp(h, Spec(c, P.MinHeight, cbH, f, false), Spec(c, P.MaxHeight, cbH, f, false)), f.PadH);

        var x = AbsPos(cbW, left, right, w, mL, mR, staticX + AlignOffset(alignX, innerW - w - Pos(mL) - Pos(mR)) + Pos(mL));
        var y = AbsPos(cbH, top, bottom, h, mT, mB, staticY + AlignOffset(alignY, innerH - h - Pos(mT) - Pos(mB)) + Pos(mT));
        Commit(c, w, h, cbW);
        c.LX = cbX + x;
        c.LY = cbY + y;
    }

    private static float AlignOffset(Align a, float free) => a switch
    {
        Align.End => free,
        Align.Center => free / 2,
        _ => 0,
    };

    // Returns the border-box start of the box, relative to the containing block's start edge.
    private static float AbsPos(float cb, float start, float end, float size, float mStart, float mEnd, float staticPos)
    {
        if (!float.IsNaN(start) && !float.IsNaN(end))
        {
            var free = cb - start - end - size - Pos(mStart) - Pos(mEnd);
            if (float.IsNaN(mStart) && float.IsNaN(mEnd))
            {
                return start + (free >= 0 ? free / 2 : 0);
            }

            return float.IsNaN(mStart) ? start + free : start + Pos(mStart);
        }

        if (!float.IsNaN(start))
        {
            return start + Pos(mStart);
        }

        if (!float.IsNaN(end))
        {
            return cb - end - size - Pos(mEnd);
        }

        return staticPos;
    }
}
