using P = OneGame.UI.StyleProp;

namespace OneGame.UI;

// Layout follows CSS Flexible Box Layout Module Level 1 (section numbers below refer to it) and CSS Box Alignment for the
// distribution of free space. Sizes are border-box pixels; NaN means "not definite" or "auto".
internal sealed partial class FlexEngine(UiTree ui)
{
    private struct Frame
    {
        public float PL, PR, PT, PB, BL, BR, BT, BB;

        public readonly float PadW => PL + PR + BL + BR;

        public readonly float PadH => PT + PB + BT + BB;
    }

    private struct Item
    {
        public Element El;
        public float MainStartM, MainEndM, CrossStartM, CrossEndM; // logical margins; NaN = auto
        public float PadMain, PadCross;
        public float Base, Hyp, Target, Min, Max;
        public float Grow, Shrink;
        public float Cross;
        public float SpecCross;
        public int Violation;
        public float MinCross, MaxCross;
        public bool CrossAuto;
        public bool Frozen;
        public Align Self;
        public float MainPos, CrossPos;
    }

    private struct Line
    {
        public int Start, End;
        public float Cross, Pos;
    }

    private readonly List<Item[]> _itemPool = [];
    private readonly List<Line[]> _linePool = [];

    private float Scale => ui.Scale;

    // ---- units ---------------------------------------------------------------------------------------------------

    private float R(Length l, float basis) => l.Unit switch
    {
        LengthUnit.Px => l.Value,
        LengthUnit.Ui => l.Value * Scale,
        LengthUnit.Percent => float.IsNaN(basis) ? float.NaN : basis * l.Value / 100f,
        _ => float.NaN,
    };

    private float RZ(Length l, float basis)
    {
        var v = R(l, basis);
        return float.IsNaN(v) ? 0 : v;
    }

    private static float Pos(float v) => float.IsNaN(v) ? 0 : v;

    private Frame GetFrame(Element el, float cb)
    {
        var s = el.Computed;
        return new Frame
        {
            PL = MathF.Max(0, RZ(s.L(P.PaddingLeft), cb)),
            PR = MathF.Max(0, RZ(s.L(P.PaddingRight), cb)),
            PT = MathF.Max(0, RZ(s.L(P.PaddingTop), cb)),
            PB = MathF.Max(0, RZ(s.L(P.PaddingBottom), cb)),
            BL = MathF.Max(0, RZ(s.L(P.BorderLeftWidth), 0)),
            BR = MathF.Max(0, RZ(s.L(P.BorderRightWidth), 0)),
            BT = MathF.Max(0, RZ(s.L(P.BorderTopWidth), 0)),
            BB = MathF.Max(0, RZ(s.L(P.BorderBottomWidth), 0)),
        };
    }

    // Specified size converted to a border-box size; NaN when auto or a percentage of something indefinite.
    private float Spec(Element el, P prop, float basis, in Frame f, bool horizontal)
    {
        var v = R(el.Computed.L(prop), basis);
        if (float.IsNaN(v))
        {
            return v;
        }

        if (el.Computed.E(P.BoxSizing) == (int)BoxSizing.ContentBox)
        {
            v += horizontal ? f.PadW : f.PadH;
        }

        return MathF.Max(v, horizontal ? f.PadW : f.PadH);
    }

    private static float Clamp(float v, float min, float max)
    {
        if (!float.IsNaN(max))
        {
            v = MathF.Min(v, max);
        }

        if (!float.IsNaN(min))
        {
            v = MathF.Max(v, min);
        }

        return v;
    }

    // ---- text ----------------------------------------------------------------------------------------------------

    internal float FontSizePx(Element el)
    {
        var l = el.Computed.L(P.FontSize);
        if (l.Unit == LengthUnit.Percent)
        {
            var parentSize = el.Parent is { } p ? FontSizePx(p) : 16f * Scale;
            return parentSize * l.Value / 100f;
        }

        var v = R(l, float.NaN);
        return float.IsNaN(v) ? 16f * Scale : v;
    }

    internal TextStyle TextStyleOf(Element el)
    {
        var s = el.Computed;
        var size = FontSizePx(el);
        var lh = s.L(P.LineHeight);
        float lineHeight = lh.Unit switch
        {
            LengthUnit.Px => lh.Value,
            LengthUnit.Ui => lh.Value * Scale,
            LengthUnit.Percent => size * lh.Value / 100f,
            _ => 0,
        };
        return new TextStyle(s.T(P.FontFamily), size, (int)s.N(P.FontWeight), s.E(P.FontStyle) == (int)FontStyle.Italic, lineHeight,
            RZ(s.L(P.LetterSpacing), size), (WhiteSpace)s.E(P.WhiteSpace));
    }

    // ---- intrinsic sizes -----------------------------------------------------------------------------------------

    /// <summary>Border-box min-content (<paramref name="max"/> false) or max-content width. CSS Sizing 3 §4, flexbox §9.9.1.</summary>
    public float Intrinsic(Element el, bool max, float cb)
    {
        var kind = (byte)(max ? 1 : 0);
        if (el.TryGetCache(kind, 0, cb, out var cached))
        {
            return cached;
        }

        var v = ComputeIntrinsic(el, max, cb);
        el.PutCache(kind, 0, cb, v);
        return v;
    }

    private float ComputeIntrinsic(Element el, bool max, float cb)
    {
        var f = GetFrame(el, cb);
        if (el.Text is { } text)
        {
            return ui.Measurer.Measure(text, TextStyleOf(el), max ? float.PositiveInfinity : 0).Width + f.PadW;
        }

        if (el.ChildCount == 0)
        {
            return (el.IntrinsicSize.X > 0 ? el.IntrinsicSize.X : 0) + f.PadW;
        }

        var s = el.Computed;
        var row = s.E(P.FlexDirection) < 2;
        var wrap = s.E(P.FlexWrap) != (int)FlexWrap.NoWrap;
        var sum = 0f;
        var widest = 0f;
        var n = 0;
        for (var i = 0; i < el.ChildCount; i++)
        {
            var c = el.ChildAt(i);
            if (!c.InLayout || c.Computed.E(P.Position) != (int)PositionMode.Relative)
            {
                continue;
            }

            var w = Contribution(c, max);
            sum += w;
            widest = MathF.Max(widest, w);
            n++;
        }

        var gap = row ? RZ(s.L(P.ColumnGap), float.NaN) : 0;
        float content;
        if (!row || (!max && wrap))
        {
            content = widest;
        }
        else
        {
            content = sum + (gap * Math.Max(0, n - 1));
        }

        return content + f.PadW;
    }

    // Outer width a child asks for when its parent is sized by content.
    private float Contribution(Element c, bool max)
    {
        var f = GetFrame(c, float.NaN);
        var s = c.Computed;
        var w = Spec(c, P.Width, float.NaN, f, true);
        if (float.IsNaN(w))
        {
            w = Intrinsic(c, max, float.NaN);
        }

        w = Clamp(w, Spec(c, P.MinWidth, float.NaN, f, true), Spec(c, P.MaxWidth, float.NaN, f, true));
        w = MathF.Max(w, f.PadW);
        return w + RZ(s.L(P.MarginLeft), float.NaN) + RZ(s.L(P.MarginRight), float.NaN);
    }

    private float FitContent(Element el, float avail, float cb)
    {
        var minC = Intrinsic(el, false, cb);
        var maxC = Intrinsic(el, true, cb);
        return MathF.Min(maxC, MathF.Max(minC, avail));
    }

    /// <summary>Border-box height of an element laid out at the given border-box width with an auto height.</summary>
    public float ContentHeight(Element el, float w, float cb)
    {
        if (el.TryGetCache(2, w, cb, out var cached))
        {
            return cached;
        }

        var h = Run(el, w, float.NaN, cb, false).H;
        el.PutCache(2, w, cb, h);
        return h;
    }

    // ---- commit --------------------------------------------------------------------------------------------------

    /// <summary>Lays out <paramref name="el"/> at a definite border-box size and stores the boxes of its whole subtree.</summary>
    public void Commit(Element el, float w, float h, float cb)
    {
        if (el.HasCommit && !el.LayoutDirty && el.CommitW.Equals(w) && el.CommitH.Equals(h) && el.CommitCb.Equals(cb))
        {
            return;
        }

        Run(el, w, h, cb, true);
        el.LW = w;
        el.LH = h;
        el.CommitW = w;
        el.CommitH = h;
        el.CommitCb = cb;
        el.HasCommit = true;
        el.LayoutDirty = false;
    }

    private (float W, float H) Run(Element el, float w, float h, float cb, bool commit)
    {
        var s = el.Computed;
        var f = GetFrame(el, cb);
        var innerW = MathF.Max(0, w - f.PadW);
        var innerH = float.IsNaN(h) ? float.NaN : MathF.Max(0, h - f.PadH);
        if (commit)
        {
            el.PX = f.BL;
            el.PY = f.BT;
            el.BRw = f.BR;
            el.BBw = f.BB;
            el.CX = f.PL + f.BL;
            el.CY = f.PT + f.BT;
            el.CW = innerW;
        }

        if (el.Text is { } text)
        {
            var style = TextStyleOf(el);
            var size = ui.Measurer.Measure(text, style, innerW);
            var th = float.IsNaN(h) ? size.Height + f.PadH : h;
            if (commit)
            {
                var textLines = el.TextLines ??= [];
                textLines.Clear();
                ui.Measurer.Layout(text, style, innerW, textLines);
                el.PW = w - f.BL - f.BR;
                el.PH = th - f.BT - f.BB;
                el.SW = MathF.Max(el.PW, size.Width + f.PL + f.PR);
                el.SH = MathF.Max(el.PH, size.Height + f.PT + f.PB);
            }

            return (w, th);
        }

        if (el.ChildCount == 0)
        {
            var ch = float.IsNaN(h) ? ContentOnlyHeight(el, innerW, f) : h;
            if (commit)
            {
                el.PW = w - f.BL - f.BR;
                el.PH = ch - f.BT - f.BB;
                el.SW = el.PW;
                el.SH = el.PH;
            }

            return (w, ch);
        }

        var dir = s.E(P.FlexDirection);
        var row = dir < 2;
        var revMain = dir is (int)FlexDirection.RowReverse or (int)FlexDirection.ColumnReverse;
        var wrapMode = (FlexWrap)s.E(P.FlexWrap);
        var multi = wrapMode != FlexWrap.NoWrap;
        var mainGap = RZ(s.L(row ? P.ColumnGap : P.RowGap), row ? innerW : innerH);
        var crossGap = RZ(s.L(row ? P.RowGap : P.ColumnGap), row ? innerH : innerW);
        var mainInner = row ? innerW : innerH;
        var crossInner = row ? innerH : innerW;
        var containerAlign = (Align)s.E(P.AlignItems);

        // ---- collect items (flexbox §9.2 steps 1-2) ----
        var items = RentItems(el.ChildCount);
        var count = 0;
        var anyOrder = false;
        for (var i = 0; i < el.ChildCount; i++)
        {
            var c = el.ChildAt(i);
            if (!c.InLayout || c.Computed.E(P.Position) != (int)PositionMode.Relative)
            {
                continue;
            }

            ref var it = ref items[count++];
            it = default;
            it.El = c;
            anyOrder |= c.Computed.N(P.Order) != 0;
        }

        if (anyOrder)
        {
            SortByOrder(items, count);
        }

        for (var i = 0; i < count; i++)
        {
            PrepareItem(ref items[i], row, revMain, wrapMode == FlexWrap.WrapReverse, innerW, innerH, mainInner, containerAlign);
        }

        // ---- lines (§9.3) ----
        var lines = RentLines(count + 1);
        var lineCount = 0;
        if (count > 0)
        {
            var start = 0;
            var used = 0f;
            for (var i = 0; i < count; i++)
            {
                var outer = OuterMain(items[i].Hyp, items[i]);
                if (multi && i > start && !float.IsNaN(mainInner) && used + mainGap + outer > mainInner + 0.0001f)
                {
                    lines[lineCount++] = new Line { Start = start, End = i };
                    start = i;
                    used = 0;
                }

                used += (i > start ? mainGap : 0) + outer;
            }

            lines[lineCount++] = new Line { Start = start, End = count };
        }

        // Indefinite main size: content size is the longest line (§9.2 step 3 via max-content).
        if (float.IsNaN(mainInner))
        {
            var longest = 0f;
            for (var l = 0; l < lineCount; l++)
            {
                var sum = 0f;
                for (var i = lines[l].Start; i < lines[l].End; i++)
                {
                    sum += OuterMain(items[i].Hyp, items[i]) + (i > lines[l].Start ? mainGap : 0);
                }

                longest = MathF.Max(longest, sum);
            }

            mainInner = longest;
        }

        // ---- resolve flexible lengths (§9.7) and cross sizes (§9.4) ----
        for (var l = 0; l < lineCount; l++)
        {
            ref var line = ref lines[l];
            ResolveFlexible(items, line.Start, line.End, mainInner - (mainGap * (line.End - line.Start - 1)));
            var lineCross = 0f;
            for (var i = line.Start; i < line.End; i++)
            {
                ref var it = ref items[i];
                ComputeCross(ref it, row, innerW, innerH);
                lineCross = MathF.Max(lineCross, it.Cross + Pos(it.CrossStartM) + Pos(it.CrossEndM));
            }

            line.Cross = lineCross;
        }

        // §9.4 step 8: a single-line container with a definite cross size gives its line that whole size.
        if (!multi && lineCount == 1 && !float.IsNaN(crossInner))
        {
            lines[0].Cross = crossInner;
        }

        var linesTotal = 0f;
        for (var l = 0; l < lineCount; l++)
        {
            linesTotal += lines[l].Cross + (l > 0 ? crossGap : 0);
        }

        var contentCross = float.IsNaN(crossInner) ? linesTotal : crossInner;

        // ---- align-content (§9.4 step 15; Box Alignment) ----
        var lineOffset = 0f;
        var lineBetween = crossGap;
        if (multi && lineCount > 0)
        {
            var free = contentCross - linesTotal;
            var ac = (Align)s.E(P.AlignContent);
            if (ac is Align.Auto or Align.Stretch)
            {
                if (free > 0)
                {
                    for (var l = 0; l < lineCount; l++)
                    {
                        lines[l].Cross += free / lineCount;
                    }
                }
            }
            else
            {
                Distribute(ac, free, lineCount, out lineOffset, out var extra);
                lineBetween += extra;
            }
        }

        var cursor = lineOffset;
        for (var l = 0; l < lineCount; l++)
        {
            lines[l].Pos = cursor;
            cursor += lines[l].Cross + lineBetween;
        }

        // ---- main-axis placement (§9.5) and cross alignment (§9.6) ----
        var justify = (Align)s.E(P.JustifyContent);
        for (var l = 0; l < lineCount; l++)
        {
            ref var line = ref lines[l];
            var used = 0f;
            var autos = 0;
            for (var i = line.Start; i < line.End; i++)
            {
                ref var it = ref items[i];
                used += it.Target + Pos(it.MainStartM) + Pos(it.MainEndM);
                autos += (float.IsNaN(it.MainStartM) ? 1 : 0) + (float.IsNaN(it.MainEndM) ? 1 : 0);
            }

            var n = line.End - line.Start;
            var free = mainInner - used - (mainGap * (n - 1));
            var offset = 0f;
            var between = mainGap;
            var autoShare = 0f;
            if (autos > 0)
            {
                autoShare = MathF.Max(0, free) / autos;
            }
            else
            {
                Distribute(justify, free, n, out offset, out var extra);
                between += extra;
            }

            var pos = offset;
            for (var i = line.Start; i < line.End; i++)
            {
                ref var it = ref items[i];
                var ms = float.IsNaN(it.MainStartM) ? autoShare : it.MainStartM;
                var me = float.IsNaN(it.MainEndM) ? autoShare : it.MainEndM;
                pos += ms;
                it.MainPos = pos;
                pos += it.Target + me + between;
                AlignCross(ref it, line.Cross, containerAlign, innerW, innerH, row);
                it.CrossPos += line.Pos;
            }
        }

        // ---- output size ----
        var contentMain = mainInner;
        float outW = w;
        float outH = h;
        if (float.IsNaN(h))
        {
            outH = (row ? contentCross : contentMain) + f.PadH;
        }

        if (!commit)
        {
            ReturnItems(items);
            ReturnLines(lines);
            return (outW, outH);
        }

        // ---- commit children ----
        var totalMain = mainInner;
        var totalCross = contentCross;
        var ox = f.PL + f.BL;
        var oy = f.PT + f.BT;
        var maxRight = 0f;
        var maxBottom = 0f;
        for (var i = 0; i < count; i++)
        {
            ref var it = ref items[i];
            var mp = revMain ? totalMain - (it.MainPos + it.Target) : it.MainPos;
            var cp = wrapMode == FlexWrap.WrapReverse ? totalCross - (it.CrossPos + it.Cross) : it.CrossPos;
            var cw = row ? it.Target : it.Cross;
            var chh = row ? it.Cross : it.Target;
            var x = ox + (row ? mp : cp);
            var y = oy + (row ? cp : mp);
            var c = it.El;
            Commit(c, cw, chh, innerW);
            if (c.Computed.E(P.Position) == (int)PositionMode.Relative)
            {
                x += RelativeOffset(c.Computed.L(P.Left), c.Computed.L(P.Right), innerW);
                y += RelativeOffset(c.Computed.L(P.Top), c.Computed.L(P.Bottom), innerH);
            }

            c.LX = x;
            c.LY = y;
            var mR = row ? (revMain ? it.MainStartM : it.MainEndM) : (wrapMode == FlexWrap.WrapReverse ? it.CrossStartM : it.CrossEndM);
            var mB = !row ? (revMain ? it.MainStartM : it.MainEndM) : (wrapMode == FlexWrap.WrapReverse ? it.CrossStartM : it.CrossEndM);
            maxRight = MathF.Max(maxRight, x + cw + Pos(mR));
            maxBottom = MathF.Max(maxBottom, y + chh + Pos(mB));
        }

        ReturnItems(items);
        ReturnLines(lines);

        el.PW = w - f.BL - f.BR;
        el.PH = outH - f.BT - f.BB;
        LayoutAbsolute(el, w, outH, f, row, justify, containerAlign, ref maxRight, ref maxBottom);
        el.SW = MathF.Max(el.PW, maxRight - f.BL + f.PR);
        el.SH = MathF.Max(el.PH, maxBottom - f.BT + f.PB);
        el.ClampScroll();
        return (outW, outH);
    }

    private static float ContentOnlyHeight(Element el, float innerW, in Frame f)
    {
        if (el.IntrinsicSize.X > 0 && el.IntrinsicSize.Y > 0)
        {
            return (innerW * el.IntrinsicSize.Y / el.IntrinsicSize.X) + f.PadH;
        }

        return el.IntrinsicSize.Y + f.PadH;
    }

    private float RelativeOffset(Length a, Length b, float basis)
    {
        var av = R(a, basis);
        if (!float.IsNaN(av))
        {
            return av;
        }

        var bv = R(b, basis);
        return float.IsNaN(bv) ? 0 : -bv;
    }

    private static float OuterMain(float size, in Item it) => size + Pos(it.MainStartM) + Pos(it.MainEndM);

    private void PrepareItem(ref Item it, bool row, bool revMain, bool revCross, float innerW, float innerH, float mainInner, Align containerAlign)
    {
        var c = it.El;
        var s = c.Computed;
        var f = GetFrame(c, innerW);
        var mT = RM(s.L(P.MarginTop), innerW);
        var mR = RM(s.L(P.MarginRight), innerW);
        var mB = RM(s.L(P.MarginBottom), innerW);
        var mL = RM(s.L(P.MarginLeft), innerW);
        var (ms, me) = row ? (mL, mR) : (mT, mB);
        var (cs, ce) = row ? (mT, mB) : (mL, mR);
        it.MainStartM = revMain ? me : ms;
        it.MainEndM = revMain ? ms : me;
        it.CrossStartM = revCross ? ce : cs;
        it.CrossEndM = revCross ? cs : ce;
        it.PadMain = row ? f.PadW : f.PadH;
        it.PadCross = row ? f.PadH : f.PadW;
        it.Grow = s.N(P.FlexGrow);
        it.Shrink = s.N(P.FlexShrink);
        var self = (Align)s.E(P.AlignSelf);
        it.Self = self == Align.Auto ? containerAlign : self;
        if (it.Self is Align.Auto or Align.Baseline)
        {
            it.Self = it.Self == Align.Auto ? Align.Stretch : Align.Start;
        }

        var specW = Spec(c, P.Width, innerW, f, true);
        var specH = Spec(c, P.Height, innerH, f, false);
        var minW = Spec(c, P.MinWidth, innerW, f, true);
        var maxW = Spec(c, P.MaxWidth, innerW, f, true);
        var minH = Spec(c, P.MinHeight, innerH, f, false);
        var maxH = Spec(c, P.MaxHeight, innerH, f, false);
        var specMain = row ? specW : specH;
        var specCross = row ? specH : specW;
        var minMain = row ? minW : minH;
        var maxMain = row ? maxW : maxH;
        it.MinCross = row ? minH : minW;
        it.MaxCross = row ? maxH : maxW;
        it.CrossAuto = float.IsNaN(specCross);
        it.SpecCross = specCross;

        // For a column container the cross size is the width, and the content height depends on it.
        var itemW = float.NaN;
        if (!row)
        {
            itemW = specW;
            var crossMargins = Pos(it.CrossStartM) + Pos(it.CrossEndM);
            if (float.IsNaN(itemW))
            {
                var stretch = it.Self == Align.Stretch && !float.IsNaN(it.CrossStartM) && !float.IsNaN(it.CrossEndM);
                itemW = stretch ? innerW - crossMargins : FitContent(c, innerW - crossMargins, innerW);
            }

            itemW = MathF.Max(Clamp(itemW, minW, maxW), f.PadW);
            it.Cross = itemW;
        }

        // Flex base size (§9.2 step 3).
        float baseSize;
        var basisL = s.L(P.FlexBasis);
        var basis = basisL.IsAuto ? float.NaN : Spec(c, P.FlexBasis, mainInner, f, row);
        if (!float.IsNaN(basis))
        {
            baseSize = basis;
        }
        else if (!float.IsNaN(specMain))
        {
            baseSize = specMain;
        }
        else
        {
            baseSize = row ? Intrinsic(c, true, innerW) : ContentHeight(c, itemW, innerW);
        }

        baseSize = MathF.Max(baseSize, it.PadMain);

        // Automatic minimum size (§4.5).
        float min;
        if (!float.IsNaN(minMain))
        {
            min = minMain;
        }
        else if (c.Scrolls(row))
        {
            min = 0;
        }
        else
        {
            var contentSize = row ? Intrinsic(c, false, innerW) : ContentHeight(c, itemW, innerW);
            if (!float.IsNaN(maxMain))
            {
                contentSize = MathF.Min(contentSize, maxMain);
            }

            min = contentSize;
            if (!float.IsNaN(specMain))
            {
                min = MathF.Min(min, float.IsNaN(maxMain) ? specMain : MathF.Min(specMain, maxMain));
            }
        }

        min = MathF.Max(min, it.PadMain);
        it.Min = min;
        it.Max = float.IsNaN(maxMain) ? float.PositiveInfinity : maxMain;
        it.Base = baseSize;
        it.Hyp = MathF.Max(it.Min, MathF.Min(baseSize, it.Max));
        it.Target = it.Hyp;
    }

    // Margins: auto stays NaN; percentages of an indefinite basis count as zero.
    private float RM(Length l, float basis) => l.IsAuto ? float.NaN : RZ(l, basis);

    private static void SortByOrder(Item[] items, int count)
    {
        // Stable insertion sort: items with equal `order` keep document order (§5.4).
        for (var i = 1; i < count; i++)
        {
            var x = items[i];
            var key = x.El.Computed.N(P.Order);
            var j = i - 1;
            while (j >= 0 && items[j].El.Computed.N(P.Order) > key)
            {
                items[j + 1] = items[j];
                j--;
            }

            items[j + 1] = x;
        }
    }

    // §9.7 Resolving Flexible Lengths.
    private static void ResolveFlexible(Item[] items, int start, int end, float avail)
    {
        var sumHyp = 0f;
        for (var i = start; i < end; i++)
        {
            sumHyp += OuterMain(items[i].Hyp, items[i]);
        }

        var growing = sumHyp < avail;
        var allFrozen = true;
        for (var i = start; i < end; i++)
        {
            ref var it = ref items[i];
            var factor = growing ? it.Grow : it.Shrink;
            it.Frozen = factor == 0 || (growing ? it.Base > it.Hyp : it.Base < it.Hyp);
            if (it.Frozen)
            {
                it.Target = it.Hyp;
            }
            else
            {
                allFrozen = false;
            }
        }

        var initialFree = avail - SumOuter(items, start, end);
        while (!allFrozen)
        {
            var remaining = avail - SumOuter(items, start, end);
            var sumFactor = 0f;
            for (var i = start; i < end; i++)
            {
                if (!items[i].Frozen)
                {
                    sumFactor += growing ? items[i].Grow : items[i].Shrink;
                }
            }

            if (sumFactor < 1)
            {
                var scaled = initialFree * sumFactor;
                if (MathF.Abs(scaled) < MathF.Abs(remaining))
                {
                    remaining = scaled;
                }
            }

            if (remaining != 0)
            {
                if (growing)
                {
                    for (var i = start; i < end; i++)
                    {
                        ref var it = ref items[i];
                        if (!it.Frozen)
                        {
                            it.Target = it.Base + (remaining * it.Grow / sumFactor);
                        }
                    }
                }
                else
                {
                    var sumScaled = 0f;
                    for (var i = start; i < end; i++)
                    {
                        if (!items[i].Frozen)
                        {
                            sumScaled += items[i].Shrink * (items[i].Base - items[i].PadMain);
                        }
                    }

                    for (var i = start; i < end; i++)
                    {
                        ref var it = ref items[i];
                        if (!it.Frozen && sumScaled > 0)
                        {
                            it.Target = it.Base + (remaining * it.Shrink * (it.Base - it.PadMain) / sumScaled);
                        }
                    }
                }
            }

            var total = 0f;
            for (var i = start; i < end; i++)
            {
                ref var it = ref items[i];
                if (it.Frozen)
                {
                    continue;
                }

                var clamped = MathF.Max(it.Min, MathF.Min(it.Target, it.Max));
                it.Violation = clamped > it.Target ? 1 : clamped < it.Target ? -1 : 0;
                total += clamped - it.Target;
                it.Target = clamped;
            }

            // Freeze by the sign of the total violation (§9.7 step 4.e).
            allFrozen = true;
            for (var i = start; i < end; i++)
            {
                ref var it = ref items[i];
                if (it.Frozen)
                {
                    continue;
                }

                it.Frozen = total == 0 || (total > 0 ? it.Violation == 1 : it.Violation == -1);
                allFrozen &= it.Frozen;
            }
        }
    }

    private static float SumOuter(Item[] items, int start, int end)
    {
        var sum = 0f;
        for (var i = start; i < end; i++)
        {
            sum += OuterMain(items[i].Frozen ? items[i].Target : items[i].Base, items[i]);
        }

        return sum;
    }
}
