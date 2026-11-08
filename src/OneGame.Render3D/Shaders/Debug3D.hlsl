// Portable thick primitives use triangles, never implementation-dependent native line widths.
#include "Frame.hlsli"
struct DebugIn
{
    [[vk::location(0)]] float4 FromSize : FROMSIZE;
    [[vk::location(1)]] float4 ToUnits : TOUNITS;
    [[vk::location(2)]] float4 Color : TINT;
    [[vk::location(3)]] float4 Kind : KIND;
    uint VertexId : SV_VertexID;
};
struct DebugOut
{
    float4 Pos : SV_Position;
    [[vk::location(0)]] float4 Color : TEXCOORD0;
};
[shader("vertex")]
DebugOut VSDebug(DebugIn i)
{
    float4 a = mul(float4(i.FromSize.xyz, 1), ViewProj);
    float4 b = mul(float4(i.ToUnits.xyz, 1), ViewProj);
    bool point = i.Kind.x == 1;
    bool hidden = point ? a.z < 0 : (a.z < 0 && b.z < 0);
    // Clip the centreline against Vulkan's z=0 near plane before dividing by w.
    if (a.z < 0 && b.z >= 0) a = lerp(a, b, -a.z / (b.z - a.z));
    if (b.z < 0 && a.z >= 0) b = lerp(b, a, -b.z / (a.z - b.z));
    float2 viewport = ViewportInfo.xy;
    float2 ap = a.xy / max(a.w, 0.000001) * viewport * 0.5;
    float2 bp = b.xy / max(b.w, 0.000001) * viewport * 0.5;
    float len = length(bp - ap);
    float2 dir = (bp - ap) / max(len, 0.000001);
    float2 normal = float2(-dir.y, dir.x);
    // Vertical projection scale gives a camera-facing world width, also for orthographic cameras.
    float wa = i.FromSize.w * (i.ToUnits.w == 1 ? abs(Proj[1][1]) * viewport.y * 0.5 / max(a.w, 0.000001) : 1);
    float wb = i.FromSize.w * (i.ToUnits.w == 1 ? abs(Proj[1][1]) * viewport.y * 0.5 / max(b.w, 0.000001) : 1);
    uint v = i.VertexId;
    float4 pos = a;
    float2 offset = 0;
    float head = min(wb * 6, len * 0.5);
    if (v < 6)
    {
        uint k = (0x231210u >> (v * 4u)) & 0xFu;
        float2 c = float2(k & 1u, k >> 1u);
        if (point) offset = (c - 0.5) * wa;
        else
        {
            pos = c.x == 0 ? a : b;
            offset = normal * (c.y - 0.5) * (c.x == 0 ? wa : wb);
            if (i.Kind.x == 2 && c.x != 0) offset -= dir * head;
        }
    }
    else
    {
        pos = b;
        if (i.Kind.x != 2 || point) hidden = true;
        if (v != 6) offset = -dir * head + normal * (v == 7 ? -1 : 1) * head * 0.5;
    }
    pos.xy += offset * 2 / viewport * pos.w;
    if (hidden || (!point && len < 0.00001)) pos = float4(0, 0, -1, 1);
    DebugOut o;
    o.Pos = pos;
    o.Color = i.Color;
    return o;
}
[shader("pixel")]
float4 PSDebug(DebugOut i) : SV_Target { return i.Color; }
