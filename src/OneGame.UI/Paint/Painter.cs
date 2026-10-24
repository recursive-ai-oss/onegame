using System.Numerics;
using P = OneGame.UI.StyleProp;

namespace OneGame.UI;

// Turns laid-out elements into draw commands. Paint order follows CSS 2.1 Appendix E in a simplified form: an element paints its
// shadow, background, image, border and text, then its children ordered by z-index (ties keep document order), then its outline.
internal sealed class Painter(UiTree ui)
{
    private readonly List<List<Element>> _orderPool = [];
    private DisplayList _list = null!;

    public void Paint(Element root, DisplayList list)
    {
        _list = list;
        list.Clear();
        Node(root, 0, 0);
    }

    private void Node(Element e, float px, float py)
    {
        if (!e.InLayout)
        {
            return;
        }

        var s = e.Computed;
        var isFixed = s.E(P.Position) == (int)PositionMode.Fixed;
        var x = isFixed ? e.LX : px + e.LX;
        var y = isFixed ? e.LY : py + e.LY;
        var w = e.LW;
        var h = e.LH;
        var opacity = s.N(P.Opacity);
        if (opacity <= 0)
        {
            return;
        }

        var visible = s.E(P.Visibility) == (int)VisibilityMode.Visible;
        var group = opacity < 1;
        if (group)
        {
            _list.Add(new DrawCommand { Kind = DrawKind.PushOpacity, Value = opacity, Source = e });
        }

        var hasTransform = TryTransform(e, x, y, w, h, out var m);
        if (hasTransform)
        {
            _list.Add(new DrawCommand { Kind = DrawKind.PushTransform, Transform = m, Source = e });
        }

        var box = new Rect(x, y, w, h);
        var radii = Radii(e, w, h);
        if (visible)
        {
            PaintBox(e, s, box, radii);
        }

        var clip = e.Scrolls(true) || e.Scrolls(false);
        if (clip)
        {
            var pad = new Rect(x + e.PX, y + e.PY, e.PW, e.PH);
            _list.Add(new DrawCommand { Kind = DrawKind.PushClip, Bounds = pad, Radii = Inner(radii, e), Source = e });
        }

        if (visible && e.Text is { } text && e.TextLines is { Count: > 0 } lines)
        {
            PaintText(e, s, text, lines, x, y);
        }

        var ox = x - e.ScrollX;
        var oy = y - e.ScrollY;
        var n = e.ChildCount;
        if (n > 0)
        {
            var order = Order(e);
            if (order is null)
            {
                for (var i = 0; i < n; i++)
                {
                    Node(e.ChildAt(i), ox, oy);
                }
            }
            else
            {
                for (var i = 0; i < order.Count; i++)
                {
                    Node(order[i], ox, oy);
                }

                Release(order);
            }
        }

        if (clip)
        {
            _list.Add(new DrawCommand { Kind = DrawKind.PopClip });
        }

        if (visible && s.L(P.OutlineWidth) is { IsAuto: false } ow && ui.Px(ow) > 0 && s.C(P.OutlineColor).A > 0)
        {
            var wpx = ui.Px(ow);
            var grow = ui.Px(s.L(P.OutlineOffset)) + wpx;
            _list.Add(new DrawCommand
            {
                Kind = DrawKind.Border,
                Bounds = new Rect(x - grow, y - grow, w + (2 * grow), h + (2 * grow)),
                Color = s.C(P.OutlineColor),
                Widths = new Edges(wpx, wpx, wpx, wpx),
                Radii = Grow(radii, grow),
                Source = e,
            });
        }

        if (hasTransform)
        {
            _list.Add(new DrawCommand { Kind = DrawKind.PopTransform });
        }

        if (group)
        {
            _list.Add(new DrawCommand { Kind = DrawKind.PopOpacity });
        }
    }

    private void PaintBox(Element e, ComputedStyle s, Rect box, CornerRadii radii)
    {
        var shadow = s.C(P.ShadowColor);
        if (shadow.A > 0)
        {
            _list.Add(new DrawCommand
            {
                Kind = DrawKind.Shadow,
                Bounds = box,
                Color = shadow,
                Radii = radii,
                Offset = new Vector2(ui.Px(s.L(P.ShadowX)), ui.Px(s.L(P.ShadowY))),
                Value = ui.Px(s.L(P.ShadowBlur)),
                Spread = ui.Px(s.L(P.ShadowSpread)),
                Source = e,
            });
        }

        var bg = s.C(P.BackgroundColor);
        if (bg.A > 0)
        {
            _list.Add(new DrawCommand { Kind = DrawKind.Rect, Bounds = box, Color = bg, Radii = radii, Source = e });
        }

        var img = s.T(P.BackgroundImage);
        if (img.Length > 0)
        {
            var sl = ui.Px(s.L(P.BackgroundSlice));
            _list.Add(new DrawCommand { Kind = DrawKind.Image, Bounds = box, Resource = img, Color = Color.White, Radii = radii, Slice = new Edges(sl, sl, sl, sl), Source = e });
        }

        var bl = ui.Px(s.L(P.BorderLeftWidth));
        var bt = ui.Px(s.L(P.BorderTopWidth));
        var br = ui.Px(s.L(P.BorderRightWidth));
        var bb = ui.Px(s.L(P.BorderBottomWidth));
        var bc = s.C(P.BorderColor);
        if ((bl > 0 || bt > 0 || br > 0 || bb > 0) && bc.A > 0)
        {
            _list.Add(new DrawCommand { Kind = DrawKind.Border, Bounds = box, Color = bc, Widths = new Edges(bl, bt, br, bb), Radii = radii, Source = e });
        }
    }

    private void PaintText(Element e, ComputedStyle s, string text, List<TextLine> lines, float x, float y)
    {
        var align = (TextAlignment)s.E(P.TextAlign);
        var first = _list.Lines.Length;
        var left = x + e.CX;
        var top = y + e.CY;
        foreach (var l in lines)
        {
            var dx = align switch
            {
                TextAlignment.Center => (e.CW - l.Width) / 2,
                TextAlignment.End => e.CW - l.Width,
                _ => 0,
            };
            _list.AddLine(new PlacedLine(l.Start, l.Length, left + dx, top + l.Y, l.Width, l.Height));
        }

        var run = _list.AddRun(new TextRun(text, ui.Engine.TextStyleOf(e), s.C(P.Color), first, lines.Count));
        _list.Add(new DrawCommand { Kind = DrawKind.Text, Bounds = new Rect(left, top, e.CW, e.LH - e.CY - e.BBw), Color = s.C(P.Color), TextRun = run, Source = e });
    }

    // CSS Transforms 2: the individual translate, scale and rotate properties compose about the box center (transform-origin: 50% 50%).
    private bool TryTransform(Element e, float x, float y, float w, float h, out Matrix3x2 m)
    {
        var s = e.Computed;
        var tx = ui.Resolve(s.L(P.TranslateX), w);
        var ty = ui.Resolve(s.L(P.TranslateY), h);
        var sc = s.N(P.Scale);
        var rot = s.N(P.Rotate);
        if (tx == 0 && ty == 0 && sc == 1 && rot == 0)
        {
            m = Matrix3x2.Identity;
            return false;
        }

        var pivot = new Vector2(x + (w / 2), y + (h / 2));
        m = Matrix3x2.CreateTranslation(-pivot)
            * Matrix3x2.CreateScale(sc)
            * Matrix3x2.CreateRotation(rot * MathF.PI / 180f)
            * Matrix3x2.CreateTranslation(pivot + new Vector2(tx, ty));
        return true;
    }

    internal CornerRadii Radii(Element e, float w, float h)
    {
        var s = e.Computed;
        var basis = MathF.Min(w, h);
        var tl = MathF.Max(0, ui.Resolve(s.L(P.RadiusTopLeft), basis));
        var tr = MathF.Max(0, ui.Resolve(s.L(P.RadiusTopRight), basis));
        var br = MathF.Max(0, ui.Resolve(s.L(P.RadiusBottomRight), basis));
        var bl = MathF.Max(0, ui.Resolve(s.L(P.RadiusBottomLeft), basis));
        if (tl + tr + br + bl == 0)
        {
            return default;
        }

        // CSS Backgrounds 3 §5.5: shrink all radii together when two neighbours would overlap.
        var f = 1f;
        Fit(ref f, w, tl + tr);
        Fit(ref f, w, bl + br);
        Fit(ref f, h, tl + bl);
        Fit(ref f, h, tr + br);
        return new CornerRadii(tl * f, tr * f, br * f, bl * f);
    }

    private static void Fit(ref float f, float side, float sum)
    {
        if (sum > side && sum > 0)
        {
            f = MathF.Min(f, side / sum);
        }
    }

    private static CornerRadii Inner(CornerRadii r, Element e) => new(
        MathF.Max(0, r.TopLeft - MathF.Max(e.PX, e.PY)),
        MathF.Max(0, r.TopRight - MathF.Max(e.BRw, e.PY)),
        MathF.Max(0, r.BottomRight - MathF.Max(e.BRw, e.BBw)),
        MathF.Max(0, r.BottomLeft - MathF.Max(e.PX, e.BBw)));

    private static CornerRadii Grow(CornerRadii r, float d) => new(
        r.TopLeft > 0 ? r.TopLeft + d : 0, r.TopRight > 0 ? r.TopRight + d : 0, r.BottomRight > 0 ? r.BottomRight + d : 0, r.BottomLeft > 0 ? r.BottomLeft + d : 0);

    // Children sorted by z-index, or null when none sets one (the common case, no allocation).
    internal List<Element>? Order(Element e)
    {
        var any = false;
        for (var i = 0; i < e.ChildCount; i++)
        {
            if (e.ChildAt(i).Computed.N(P.ZIndex) != 0)
            {
                any = true;
                break;
            }
        }

        if (!any)
        {
            return null;
        }

        List<Element> list;
        if (_orderPool.Count > 0)
        {
            list = _orderPool[^1];
            _orderPool.RemoveAt(_orderPool.Count - 1);
            list.Clear();
        }
        else
        {
            list = [];
        }

        for (var i = 0; i < e.ChildCount; i++)
        {
            var c = e.ChildAt(i);
            var z = c.Computed.N(P.ZIndex);
            var j = list.Count;
            while (j > 0 && list[j - 1].Computed.N(P.ZIndex) > z)
            {
                j--;
            }

            list.Insert(j, c);
        }

        return list;
    }

    internal void Release(List<Element> order) => _orderPool.Add(order);

    internal Matrix3x2? TransformOf(Element e) => TryTransform(e, 0, 0, e.LW, e.LH, out var m) ? m : null;
}
