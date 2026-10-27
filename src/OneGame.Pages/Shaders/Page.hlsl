// Page primitives and layer effects, drawn in encoded sRGB the way CSS blends.
//
// Every value here is an sRGB-encoded colour with premultiplied alpha, stored in Rgba8Unorm layers; nothing is decoded to linear light until
// PSPresent lays the finished page over the game's sRGB target. Each instance is a rectangle in its own (local, CSS px) coordinates plus a 2x3
// matrix to device pixels. Edges are anti-aliased from the signed distance to the shape, so they stay sharp under any transform.
//
// Primitive kinds (Flags.x), drawn by PSMain:
//   0 fill      solid colour (C0) or a gradient (Flags.y: 1 linear, 2 radial, 3 conic) looked up in the ramp texture bound as Tex
//   1 border    one side's part of a ring (CSS Backgrounds 3 §4, §5.3), optionally dashed or dotted
//   2 shadow    box shadow, outer or inset; the falloff is a logistic curve of the signed distance with sigma = blur / 2
//   3 image     the texture region in Uv over Rect
//   4 coverage  an R8 glyph or path coverage bitmap times colour C0
//   5 tiles     an image repeated over Rect with a gap
//
// Layer effects have their own pixel entry points and draw a quad over the destination rectangle with the same vertex shader.
//
// Clips are a table of rounded boxes (set 0, binding 1; five float4 each: rect, horizontal radii, vertical radii, inverse matrix, inverse
// translation plus parent). An instance names its innermost clip, each entry names its parent, up to four deep.

[[vk::binding(0, 0)]] cbuffer Frame
{
    float2 Viewport;
    float2 FramePad;
};

[[vk::binding(1, 0)]] StructuredBuffer<float4> Clips;

[[vk::binding(0, 1)]] Texture2D Tex;
[[vk::binding(1, 1)]] SamplerState Samp;
[[vk::binding(2, 1)]] Texture2D Tex2;

struct PageIn
{
    [[vk::location(0)]] float4 Rect : RECT;
    [[vk::location(1)]] float4 M : MATRIX;
    [[vk::location(2)]] float2 T : TRANSLATE;
    [[vk::location(3)]] float4 Radii : RADII;
    [[vk::location(4)]] float4 RadiiY : RADIIY;
    [[vk::location(5)]] float4 Widths : WIDTHS;
    [[vk::location(6)]] float4 Uv : UVRECT;
    [[vk::location(7)]] float4 C0 : COLOR0;
    [[vk::location(8)]] float4 C1 : COLOR1;
    [[vk::location(9)]] float4 Grad : GRADIENT;
    [[vk::location(10)]] uint4 Flags : FLAGS;
    uint VertexId : SV_VertexID;
};

struct PageOut
{
    float4 Pos : SV_Position;
    [[vk::location(0)]] float2 Local : TEXCOORD0;
    [[vk::location(1)]] float2 Dev : TEXCOORD1;
    [[vk::location(2)]] nointerpolation float4 Rect : TEXCOORD2;
    [[vk::location(3)]] nointerpolation float4 Radii : TEXCOORD3;
    [[vk::location(4)]] nointerpolation float4 RadiiY : TEXCOORD4;
    [[vk::location(5)]] nointerpolation float4 Widths : TEXCOORD5;
    [[vk::location(6)]] nointerpolation float4 Uv : TEXCOORD6;
    [[vk::location(7)]] nointerpolation float4 C0 : TEXCOORD7;
    [[vk::location(8)]] nointerpolation float4 C1 : TEXCOORD8;
    [[vk::location(9)]] nointerpolation float4 Grad : TEXCOORD9;
    [[vk::location(10)]] nointerpolation uint3 Flags : TEXCOORD10;
};

[shader("vertex")]
PageOut VSMain(PageIn i)
{
    uint k = (0x231210u >> (i.VertexId * 4u)) & 0xFu;
    float2 c = float2(k & 1u, k >> 1u);
    float pad = asfloat(i.Flags.w);
    float2 local = i.Rect.xy - pad + c * (i.Rect.zw + 2.0 * pad);
    // Row vectors: x' = x * m11 + y * m21 + tx, y' = x * m12 + y * m22 + ty.
    float2 p = float2(local.x * i.M.x + local.y * i.M.z + i.T.x, local.x * i.M.y + local.y * i.M.w + i.T.y);

    PageOut o;
    o.Pos = float4(p.x / Viewport.x * 2.0 - 1.0, 1.0 - p.y / Viewport.y * 2.0, 0.0, 1.0);
    o.Local = local;
    o.Dev = p;
    o.Rect = i.Rect;
    o.Radii = i.Radii;
    o.RadiiY = i.RadiiY;
    o.Widths = i.Widths;
    o.Uv = i.Uv;
    o.C0 = i.C0;
    o.C1 = i.C1;
    o.Grad = i.Grad;
    o.Flags = i.Flags.xyz;
    return o;
}

// ---- shapes ----

// Signed distance to a box whose corners are quarter ellipses. Radii order: top-left, top-right, bottom-right, bottom-left. Along the
// straight sides the distance is exact; inside a corner it is the first-order ellipse approximation k0 (k0 - 1) / k1, which is exact for circles.
float RoundedBox(float2 p, float4 rect, float4 rx, float4 ry)
{
    float2 hext = rect.zw * 0.5;
    float2 q = p - (rect.xy + hext);
    float2 r = q.x < 0.0 ? (q.y < 0.0 ? float2(rx.x, ry.x) : float2(rx.w, ry.w)) : (q.y < 0.0 ? float2(rx.y, ry.y) : float2(rx.z, ry.z));
    r = min(r, hext);
    float2 aq = abs(q);
    float2 d = aq - (hext - r);
    if (d.x <= 0.0 || d.y <= 0.0 || r.x <= 0.0 || r.y <= 0.0)
    {
        float2 dd = aq - hext;
        return length(max(dd, 0.0)) + min(max(dd.x, dd.y), 0.0);
    }

    float2 e = d / r;
    float k0 = length(e);
    float k1 = length(d / (r * r));
    return k0 * (k0 - 1.0) / max(k1, 1e-6);
}

// Coverage of a pixel from the distance at its centre: one pixel of linear ramp.
float Cover(float d)
{
    float w = max(fwidth(d), 1e-5);
    return saturate(0.5 - d / w);
}

float ClipCover(float2 dev, uint id)
{
    float c = 1.0;
    for (uint n = 0u; n < 4u; n++)
    {
        if (id == 0u)
        {
            break;
        }

        uint b = (id - 1u) * 5u;
        float4 rect = Clips[b];
        float4 rx = Clips[b + 1u];
        float4 ry = Clips[b + 2u];
        float4 m = Clips[b + 3u];
        float4 t = Clips[b + 4u];
        float2 local = float2(dev.x * m.x + dev.y * m.z + t.x, dev.x * m.y + dev.y * m.w + t.y);
        c *= Cover(RoundedBox(local, rect, rx, ry));
        id = (uint)t.z;
    }

    return c;
}

float4 Premul(float4 c)
{
    return float4(c.rgb * c.a, c.a);
}

// ---- gradients ----

float GradientT(PageOut i)
{
    uint gk = i.Flags.y;
    float t = 0.0;
    if (gk == 1u)
    {
        float2 axis = i.Grad.zw - i.Grad.xy;
        t = dot(i.Local - i.Grad.xy, axis) / max(dot(axis, axis), 1e-12);
    }
    else if (gk == 2u)
    {
        float2 v = (i.Local - i.Grad.xy) / max(i.Grad.zw, 1e-6);
        t = length(v);
    }
    else
    {
        // Conic: clockwise from up (CSS Images 4 §3.3), in turns.
        float2 v = i.Local - i.Grad.xy;
        float a = atan2(v.x, -v.y) * 0.15915494309;
        t = a - i.Grad.z * 0.15915494309;
        t = t - floor(t);
    }

    return t;
}

float4 GradientColor(PageOut i)
{
    // Uv.x is the ramp row's v coordinate, Uv.y and Uv.z the first and last stop offsets, Uv.w nonzero when the gradient repeats.
    float t = GradientT(i);
    float range = max(i.Uv.z - i.Uv.y, 1e-6);
    float u = (t - i.Uv.y) / range;
    u = i.Uv.w > 0.5 ? (u - floor(u)) : saturate(u);
    return Tex.Sample(Samp, float2(u, i.Uv.x));
}

// ---- borders ----

// Signed distance (positive inside) to the line through a and b, on the side that contains `inside`.
float Side(float2 p, float2 a, float2 b, float2 inside)
{
    float2 e = b - a;
    float len = length(e);
    if (len < 1e-5)
    {
        return 1e5;
    }

    float2 n = float2(-e.y, e.x) / len;
    float s = dot(inside - a, n) < 0.0 ? -1.0 : 1.0;
    return dot(p - a, n) * s;
}

float WedgeCover(PageOut i, uint side)
{
    float4 r = i.Rect;
    float wT = i.Widths.x, wR = i.Widths.y, wB = i.Widths.z, wL = i.Widths.w;
    float x0 = r.x, y0 = r.y, x1 = r.x + r.z, y1 = r.y + r.w;
    float ix0 = x0 + wL, ix1 = x1 - wR, iy0 = y0 + wT, iy1 = y1 - wB;
    if (ix0 > ix1)
    {
        float f = (wL + wR) > 0.0 ? wL / (wL + wR) : 0.5;
        ix0 = x0 + r.z * f;
        ix1 = ix0;
    }

    if (iy0 > iy1)
    {
        float f = (wT + wB) > 0.0 ? wT / (wT + wB) : 0.5;
        iy0 = y0 + r.w * f;
        iy1 = iy0;
    }

    float2 p = i.Local;
    float2 mid = float2((x0 + x1) * 0.5, (y0 + y1) * 0.5);
    float d0, d1;
    float2 in0;
    if (side == 0u)
    {
        in0 = float2(mid.x, y0 + 0.01);
        d0 = Side(p, float2(x0, y0), float2(ix0, iy0), in0);
        d1 = Side(p, float2(x1, y0), float2(ix1, iy0), in0);
    }
    else if (side == 1u)
    {
        in0 = float2(x1 - 0.01, mid.y);
        d0 = Side(p, float2(x1, y0), float2(ix1, iy0), in0);
        d1 = Side(p, float2(x1, y1), float2(ix1, iy1), in0);
    }
    else if (side == 2u)
    {
        in0 = float2(mid.x, y1 - 0.01);
        d0 = Side(p, float2(x1, y1), float2(ix1, iy1), in0);
        d1 = Side(p, float2(x0, y1), float2(ix0, iy1), in0);
    }
    else
    {
        in0 = float2(x0 + 0.01, mid.y);
        d0 = Side(p, float2(x0, y1), float2(ix0, iy1), in0);
        d1 = Side(p, float2(x0, y0), float2(ix0, iy0), in0);
    }

    // The diagonals are straight lines, so the distance gradient is one local unit per unit; convert to a pixel ramp with the screen derivative.
    float w0 = max(fwidth(d0), 1e-5);
    float w1 = max(fwidth(d1), 1e-5);
    return saturate(0.5 + d0 / w0) * saturate(0.5 + d1 / w1);
}

float4 BorderInset(PageOut i, float k, out float4 rx, out float4 ry)
{
    // The border box shrunk by k times each side's width, with every radius reduced by the width it meets (CSS Backgrounds 3 §5.2).
    float wT = i.Widths.x * k, wR = i.Widths.y * k, wB = i.Widths.z * k, wL = i.Widths.w * k;
    float4 r = float4(i.Rect.x + wL, i.Rect.y + wT, max(i.Rect.z - wL - wR, 0.0), max(i.Rect.w - wT - wB, 0.0));
    rx = max(i.Radii - float4(wL, wR, wR, wL), 0.0);
    ry = max(i.RadiiY - float4(wT, wT, wB, wB), 0.0);
    return r;
}

float4 BorderPixel(PageOut i)
{
    uint side = i.Flags.y;
    float a = i.Grad.x;
    float b = i.Grad.y;
    float4 orx, ory, irx, iry;
    float4 outer = BorderInset(i, a, orx, ory);
    float4 inner = BorderInset(i, b, irx, iry);
    float cov = Cover(RoundedBox(i.Local, outer, orx, ory));
    bool hasHole = inner.z > 0.0 && inner.w > 0.0;
    if (hasHole)
    {
        cov *= 1.0 - Cover(RoundedBox(i.Local, inner, irx, iry));
    }

    if (side < 4u)
    {
        cov *= WedgeCover(i, side);
    }

    // Dashes and dots run along the side's axis (Uv.x: 1 dashed, 2 dotted; Uv.y: the border width of the side).
    uint pattern = (uint)(i.Uv.x + 0.5);
    if (pattern != 0u)
    {
        float w = max(i.Uv.y, 1e-3);
        bool horizontal = side == 0u || side == 2u;
        float s = horizontal ? (i.Local.x - i.Rect.x) : (i.Local.y - i.Rect.y);
        float across = horizontal ? (i.Local.y - i.Rect.y) : (i.Local.x - i.Rect.x);
        if (pattern == 1u)
        {
            float period = 3.0 * w;
            float f = s - floor(s / period) * period;
            float edge = fwidth(s);
            cov *= saturate((2.0 * w - f) / max(edge, 1e-5) + 0.5) * saturate(f / max(edge, 1e-5) + 0.5);
        }
        else
        {
            float period = 2.0 * w;
            float f = s - floor(s / period) * period;
            float2 c = float2(f - 0.5 * w, 0.0);
            float lineY = horizontal ? (side == 0u ? i.Rect.y + 0.5 * w : i.Rect.y + i.Rect.w - 0.5 * w) : 0.0;
            float lineX = horizontal ? 0.0 : (side == 3u ? i.Rect.x + 0.5 * w : i.Rect.x + i.Rect.z - 0.5 * w);
            float cross = horizontal ? (i.Local.y - lineY) : (i.Local.x - lineX);
            float dist = length(float2(f - 0.5 * w, cross)) - 0.5 * w;
            cov *= Cover(dist);
        }
    }

    return Premul(i.C0) * cov;
}

// ---- shadows ----

float4 ShadowPixel(PageOut i)
{
    float sigma = max(i.Grad.x, 1e-3);
    bool inset = i.Grad.y > 0.5;
    float4 color = Premul(i.C0);
    // Widths is the element's own box (the hole of an outer shadow, the padding box of an inset one); Uv its circular radii.
    float4 box = i.Widths;
    float4 brx = i.Uv;
    float boxCover = Cover(RoundedBox(i.Local, box, brx, brx));
    float d = RoundedBox(i.Local, i.Rect, i.Radii, i.RadiiY);
    float blurred = 1.0 / (1.0 + exp(1.702 * d / sigma));
    if (inset)
    {
        return color * ((1.0 - blurred) * boxCover);
    }

    return color * (blurred * (1.0 - boxCover));
}

// ---- tiles ----

float4 TilePixel(PageOut i)
{
    float4 tile = i.Widths;
    float2 gap = i.Grad.zw;
    float2 period = tile.zw + gap;
    float2 rel = i.Local - tile.xy;
    float2 cell = floor(rel / period);
    float2 inCell = rel - cell * period;
    if (inCell.x >= tile.z || inCell.y >= tile.w)
    {
        return float4(0, 0, 0, 0);
    }

    float2 f = inCell / tile.zw;
    float2 uv = i.Uv.xy + i.Uv.zw * f;
    uv = clamp(uv, i.RadiiY.xy, i.RadiiY.zw);
    return Tex.Sample(Samp, uv);
}

[shader("pixel")]
float4 PSMain(PageOut i) : SV_Target
{
    uint kind = i.Flags.x;
    uint clipId = i.Flags.z;
    float clip = ClipCover(i.Dev, clipId);
    float4 outc;
    if (kind == 0u)
    {
        float cov = Cover(RoundedBox(i.Local, i.Rect, i.Radii, i.RadiiY));
        float4 col = i.Flags.y == 0u ? Premul(i.C0) : GradientColor(i);
        outc = col * cov;
    }
    else if (kind == 1u)
    {
        outc = BorderPixel(i);
    }
    else if (kind == 2u)
    {
        outc = ShadowPixel(i);
    }
    else if (kind == 3u)
    {
        float2 f = saturate((i.Local - i.Rect.xy) / max(i.Rect.zw, 1e-5));
        float2 uv = clamp(i.Uv.xy + i.Uv.zw * f, i.RadiiY.xy, i.RadiiY.zw);
        float cov = Cover(RoundedBox(i.Local, i.Rect, float4(0, 0, 0, 0), float4(0, 0, 0, 0)));
        outc = Tex.Sample(Samp, uv) * cov;
    }
    else if (kind == 4u)
    {
        float2 uv = i.Uv.xy + i.Uv.zw * saturate((i.Local - i.Rect.xy) / max(i.Rect.zw, 1e-5));
        outc = Premul(i.C0) * Tex.Sample(Samp, uv).r;
    }
    else
    {
        outc = TilePixel(i);
    }

    return outc * clip;
}

// ---- layer effects ----

float2 LayerUv(PageOut i)
{
    return i.Dev / Viewport;
}

// A layer laid over its parent with an opacity. Grad.x is the opacity.
[shader("pixel")]
float4 PSLayer(PageOut i) : SV_Target
{
    return Tex.Sample(Samp, LayerUv(i)) * (i.Grad.x * ClipCover(i.Dev, i.Flags.z));
}

float Lum(float3 c)
{
    return dot(c, float3(0.3, 0.59, 0.11));
}

float3 ClipColor(float3 c)
{
    float l = Lum(c);
    float n = min(min(c.r, c.g), c.b);
    float x = max(max(c.r, c.g), c.b);
    if (n < 0.0)
    {
        c = l + (c - l) * l / max(l - n, 1e-6);
    }

    if (x > 1.0)
    {
        c = l + (c - l) * (1.0 - l) / max(x - l, 1e-6);
    }

    return c;
}

float3 SetLum(float3 c, float l)
{
    return ClipColor(c + (l - Lum(c)));
}

float Sat(float3 c)
{
    return max(max(c.r, c.g), c.b) - min(min(c.r, c.g), c.b);
}

float3 SetSat(float3 c, float s)
{
    float mx = max(max(c.r, c.g), c.b);
    float mn = min(min(c.r, c.g), c.b);
    if (mx <= mn)
    {
        return float3(0, 0, 0);
    }

    return (c - mn) * s / (mx - mn);
}

float Dodge(float cb, float cs)
{
    return cb <= 0.0 ? 0.0 : (cs >= 1.0 ? 1.0 : min(1.0, cb / (1.0 - cs)));
}

float Burn(float cb, float cs)
{
    return cb >= 1.0 ? 1.0 : (cs <= 0.0 ? 0.0 : 1.0 - min(1.0, (1.0 - cb) / cs));
}

float3 BlendChannel(uint mode, float3 cb, float3 cs)
{
    if (mode == 1u) { return cb * cs; }
    if (mode == 2u) { return cb + cs - cb * cs; }
    if (mode == 3u) { return lerp(2.0 * cb * cs, 1.0 - 2.0 * (1.0 - cb) * (1.0 - cs), step(0.5, cb)); }
    if (mode == 4u) { return min(cb, cs); }
    if (mode == 5u) { return max(cb, cs); }
    if (mode == 6u) { return float3(Dodge(cb.r, cs.r), Dodge(cb.g, cs.g), Dodge(cb.b, cs.b)); }
    if (mode == 7u) { return float3(Burn(cb.r, cs.r), Burn(cb.g, cs.g), Burn(cb.b, cs.b)); }
    if (mode == 8u) { return lerp(2.0 * cb * cs, 1.0 - 2.0 * (1.0 - cb) * (1.0 - cs), step(0.5, cs)); }
    if (mode == 9u)
    {
        float3 d = lerp(((16.0 * cb - 12.0) * cb + 4.0) * cb, sqrt(cb), step(0.25, cb));
        return lerp(cb - (1.0 - 2.0 * cs) * cb * (1.0 - cb), cb + (2.0 * cs - 1.0) * (d - cb), step(0.5, cs));
    }
    if (mode == 10u) { return abs(cb - cs); }
    if (mode == 11u) { return cb + cs - 2.0 * cb * cs; }
    if (mode == 12u) { return SetLum(SetSat(cs, Sat(cb)), Lum(cb)); }
    if (mode == 13u) { return SetLum(SetSat(cb, Sat(cs)), Lum(cb)); }
    if (mode == 14u) { return SetLum(cs, Lum(cb)); }
    if (mode == 15u) { return SetLum(cb, Lum(cs)); }
    return cs;
}

// A group with a non-normal blend mode (Compositing and Blending 1 §9, §10). Tex is the group's layer, Tex2 the backdrop, and the result
// replaces the whole parent layer, so the pass covers the target. Flags.y is the mode, Grad.x the group opacity.
[shader("pixel")]
float4 PSBlend(PageOut i) : SV_Target
{
    float2 uv = LayerUv(i);
    float4 src = Tex.Sample(Samp, uv);
    float4 back = Tex2.Sample(Samp, uv);
    float clip = ClipCover(i.Dev, i.Flags.z);
    float as = src.a * i.Grad.x;
    if (as <= 0.0 || clip <= 0.0)
    {
        return back;
    }

    float ab = back.a;
    float3 cs = src.a > 0.0 ? src.rgb / src.a : float3(0, 0, 0);
    float3 cb = ab > 0.0 ? back.rgb / ab : float3(0, 0, 0);
    float3 mixed = BlendChannel(i.Flags.y, cb, cs);
    float3 rgb = (1.0 - as) * ab * cb + as * ((1.0 - ab) * cs + ab * mixed);
    float4 blended = float4(rgb, as + ab * (1.0 - as));
    return lerp(back, blended, clip);
}

// One layer of a mask folded into the coverage so far. Tex is the layer, Tex2 the coverage before it (alpha holds it). Flags.y is the
// composite operator (0 add, 1 subtract, 2 intersect, 3 exclude), Grad.x is 1 when the layer counts by luminance.
[shader("pixel")]
float4 PSMaskCombine(PageOut i) : SV_Target
{
    float2 uv = LayerUv(i);
    float4 s = Tex.Sample(Samp, uv);
    float cs = s.a;
    if (i.Grad.x > 0.5)
    {
        float3 straight = s.a > 0.0 ? s.rgb / s.a : float3(0, 0, 0);
        cs = (0.2126 * straight.r + 0.7152 * straight.g + 0.0722 * straight.b) * s.a;
    }

    float r = Tex2.Sample(Samp, uv).a;
    uint op = i.Flags.y;
    float o;
    if (op == 0u) { o = cs + r * (1.0 - cs); }
    else if (op == 1u) { o = cs * (1.0 - r); }
    else if (op == 2u) { o = cs * r; }
    else { o = cs * (1.0 - r) + r * (1.0 - cs); }
    return float4(0, 0, 0, o);
}

// The masked content: Tex premultiplied content, Tex2 the coverage.
[shader("pixel")]
float4 PSMaskApply(PageOut i) : SV_Target
{
    float2 uv = LayerUv(i);
    // A path's coverage is an R8 bitmap over its own rectangle (Widths); a mask layer carries its coverage in alpha.
    float cover = i.Grad.y > 0.5 ? Tex2.Sample(Samp, (i.Dev - i.Widths.xy) / i.Widths.zw).r : Tex2.Sample(Samp, uv).a;
    return Tex.Sample(Samp, uv) * (cover * ClipCover(i.Dev, i.Flags.z));
}

// Colour filter functions (Filter Effects 1 §5) on premultiplied values, up to four in order. Radii holds the kinds (-1 for none) and Widths
// the amounts. Kinds: 1 brightness, 2 contrast, 3 grayscale, 4 hue-rotate, 5 invert, 6 opacity, 7 saturate, 8 sepia.
float4 ColorOp(float4 p, float kind, float amount)
{
    float a = p.a;
    if (kind < 0.5)
    {
        return p;
    }

    if (kind < 1.5)
    {
        return float4(clamp(p.rgb * amount, 0.0, a), a);
    }

    if (kind < 2.5)
    {
        return float4(clamp(amount * p.rgb + a * (0.5 - 0.5 * amount), 0.0, a), a);
    }

    if (kind < 3.5)
    {
        float s = 1.0 - saturate(amount);
        float3 r0 = float3(0.2126 + 0.7874 * s, 0.7152 - 0.7152 * s, 0.0722 - 0.0722 * s);
        float3 r1 = float3(0.2126 - 0.2126 * s, 0.7152 + 0.2848 * s, 0.0722 - 0.0722 * s);
        float3 r2 = float3(0.2126 - 0.2126 * s, 0.7152 - 0.7152 * s, 0.0722 + 0.9278 * s);
        return float4(clamp(float3(dot(r0, p.rgb), dot(r1, p.rgb), dot(r2, p.rgb)), 0.0, a), a);
    }

    if (kind < 4.5)
    {
        float cs = cos(amount);
        float sn = sin(amount);
        float3 r0 = float3(0.213 + cs * 0.787 - sn * 0.213, 0.715 - cs * 0.715 - sn * 0.715, 0.072 - cs * 0.072 + sn * 0.928);
        float3 r1 = float3(0.213 - cs * 0.213 + sn * 0.143, 0.715 + cs * 0.285 + sn * 0.140, 0.072 - cs * 0.072 - sn * 0.283);
        float3 r2 = float3(0.213 - cs * 0.213 - sn * 0.787, 0.715 - cs * 0.715 + sn * 0.715, 0.072 + cs * 0.928 + sn * 0.072);
        return float4(clamp(float3(dot(r0, p.rgb), dot(r1, p.rgb), dot(r2, p.rgb)), 0.0, a), a);
    }

    if (kind < 5.5)
    {
        float k = saturate(amount);
        return float4(clamp((1.0 - 2.0 * k) * p.rgb + k * a, 0.0, a), a);
    }

    if (kind < 6.5)
    {
        return p * saturate(amount);
    }

    if (kind < 7.5)
    {
        float s = amount;
        float3 r0 = float3(0.213 + 0.787 * s, 0.715 - 0.715 * s, 0.072 - 0.072 * s);
        float3 r1 = float3(0.213 - 0.213 * s, 0.715 + 0.285 * s, 0.072 - 0.072 * s);
        float3 r2 = float3(0.213 - 0.213 * s, 0.715 - 0.715 * s, 0.072 + 0.928 * s);
        return float4(clamp(float3(dot(r0, p.rgb), dot(r1, p.rgb), dot(r2, p.rgb)), 0.0, a), a);
    }

    float s = 1.0 - saturate(amount);
    float3 r0 = float3(0.393 + 0.607 * s, 0.769 - 0.769 * s, 0.189 - 0.189 * s);
    float3 r1 = float3(0.349 - 0.349 * s, 0.686 + 0.314 * s, 0.168 - 0.168 * s);
    float3 r2 = float3(0.272 - 0.272 * s, 0.534 - 0.534 * s, 0.131 + 0.869 * s);
    return float4(clamp(float3(dot(r0, p.rgb), dot(r1, p.rgb), dot(r2, p.rgb)), 0.0, a), a);
}

[shader("pixel")]
float4 PSColor(PageOut i) : SV_Target
{
    float4 p = Tex.Sample(Samp, LayerUv(i));
    p = ColorOp(p, i.Radii.x, i.Widths.x);
    p = ColorOp(p, i.Radii.y, i.Widths.y);
    p = ColorOp(p, i.Radii.z, i.Widths.z);
    p = ColorOp(p, i.Radii.w, i.Widths.w);
    return p;
}

// One direction of a Gaussian blur. Grad.xy is the step between taps in uv units, Grad.z the sigma in device pixels, Grad.w the tap count
// on each side and Radii.x the distance between taps in device pixels (above one for a large sigma). Taps beyond the layer are transparent.
[shader("pixel")]
float4 PSBlur(PageOut i) : SV_Target
{
    float2 uv = LayerUv(i);
    float sigma = max(i.Grad.z, 1e-3);
    int taps = (int)i.Grad.w;
    float stride = max(i.Radii.x, 1e-3);
    float4 sum = float4(0, 0, 0, 0);
    float norm = 0.0;
    for (int n = -taps; n <= taps; n++)
    {
        float x = (float)n * stride;
        float w = exp(-0.5 * x * x / (sigma * sigma));
        float2 q = uv + i.Grad.xy * (float)n;
        float inside = (q.x >= 0.0 && q.x <= 1.0 && q.y >= 0.0 && q.y <= 1.0) ? 1.0 : 0.0;
        sum += Tex.SampleLevel(Samp, saturate(q), 0) * (w * inside);
        norm += w;
    }

    return sum / max(norm, 1e-6);
}

// The shadow of a drop-shadow filter before it is blurred: the layer's alpha, moved by Grad.xy device pixels and tinted by C0.
[shader("pixel")]
float4 PSDropTint(PageOut i) : SV_Target
{
    float2 uv = (i.Dev - i.Grad.xy) / Viewport;
    float inside = (uv.x >= 0.0 && uv.x <= 1.0 && uv.y >= 0.0 && uv.y <= 1.0) ? 1.0 : 0.0;
    float a = Tex.SampleLevel(Samp, saturate(uv), 0).a * i.C0.a * inside;
    return float4(i.C0.rgb * a, a);
}

// Tex with Tex2 behind it.
[shader("pixel")]
float4 PSOver(PageOut i) : SV_Target
{
    float2 uv = LayerUv(i);
    float4 top = Tex.Sample(Samp, uv);
    return top + Tex2.Sample(Samp, uv) * (1.0 - top.a);
}

// The parent layer with the filtered copy (Tex) replacing it inside a rounded rectangle; Tex2 is the parent.
[shader("pixel")]
float4 PSBackdrop(PageOut i) : SV_Target
{
    float2 uv = LayerUv(i);
    float cov = Cover(RoundedBox(i.Local, i.Rect, i.Radii, i.RadiiY)) * ClipCover(i.Dev, i.Flags.z);
    return lerp(Tex2.Sample(Samp, uv), Tex.Sample(Samp, uv), cov);
}

// A layer under a projective transform. Radii, RadiiY and Widths hold the rows of the inverse homography from screen pixels to layer pixels
// (x, y, w). Pixels that map outside the layer are transparent.
[shader("pixel")]
float4 PSProject(PageOut i) : SV_Target
{
    float3 s = float3(i.Dev, 1.0);
    float3 q = float3(dot(s, i.Radii.xyz), dot(s, i.RadiiY.xyz), dot(s, i.Widths.xyz));
    if (q.z <= 1e-6)
    {
        return float4(0, 0, 0, 0);
    }

    float2 p = q.xy / q.z;
    float2 uv = p / Viewport;
    if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
    {
        return float4(0, 0, 0, 0);
    }

    return Tex.Sample(Samp, uv) * (i.Grad.x * ClipCover(i.Dev, i.Flags.z));
}

float3 SrgbToLinear(float3 c)
{
    return lerp(c / 12.92, pow(max((c + 0.055) / 1.055, 0.0), 2.4), step(0.04045, c));
}

float3 LinearToSrgb(float3 c)
{
    return lerp(c * 12.92, 1.055 * pow(max(c, 0.0), 1.0 / 2.4) - 0.055, step(0.0031308, c));
}

// The finished page (encoded, premultiplied) over the game's sRGB target: premultiplied linear light, so the hardware blend is exact.
[shader("pixel")]
float4 PSPresent(PageOut i) : SV_Target
{
    float4 p = Tex.Sample(Samp, LayerUv(i));
    float3 straight = p.a > 0.0 ? p.rgb / p.a : float3(0, 0, 0);
    return float4(SrgbToLinear(saturate(straight)) * p.a, p.a);
}

// The other way: a linear premultiplied sRGB texture (a material's output) back into an encoded layer.
[shader("pixel")]
float4 PSEncode(PageOut i) : SV_Target
{
    float4 p = Tex.Sample(Samp, LayerUv(i));
    float3 straight = p.a > 0.0 ? p.rgb / p.a : float3(0, 0, 0);
    return float4(LinearToSrgb(saturate(straight)) * p.a, p.a) * ClipCover(i.Dev, i.Flags.z);
}
