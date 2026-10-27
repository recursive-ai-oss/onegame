// Camera-facing quads for particles and other sprites in the 3D scene. One instance per quad.
//
//   set 0: frame constants (binding 0)
//   set 1: the quad texture (binding 0, premultiplied alpha) and its sampler (binding 1)
//
// The colour arrives premultiplied and in linear light, possibly above 1 (it is drawn into the HDR target). Blending is
// One, OneMinusSrcAlpha: a quad with Additive set writes alpha 0, so its colour is added instead of blended over.

#include "Frame.hlsli"

[[vk::binding(0, 1)]] Texture2D<float4> Tex;
[[vk::binding(1, 1)]] SamplerState Samp;

struct BillIn
{
    [[vk::location(0)]] float4 PosRot : POSROT;     // xyz world position, w rotation in radians
    [[vk::location(1)]] float4 SizeAdd : SIZEADD;   // xy size in world units, z 1 for additive
    [[vk::location(2)]] float4 Uv : UVRECT;         // xy origin, zw extent
    [[vk::location(3)]] float4 Color : TINT;        // premultiplied linear colour
    uint VertexId : SV_VertexID;
};

struct BillOut
{
    float4 Pos : SV_Position;
    [[vk::location(0)]] float2 Uv : TEXCOORD0;
    [[vk::location(1)]] float4 Color : TEXCOORD1;
    [[vk::location(2)]] float Add : TEXCOORD2;
};

[shader("vertex")]
BillOut VSBillboard(BillIn i)
{
    // Two triangles per instance from the vertex index: corners 0,1,2 and 1,3,2 of a unit square, like the 2D renderer's quads.
    uint k = (0x231210u >> (i.VertexId * 4u)) & 0xFu;
    float2 c = float2(k & 1u, k >> 1u);
    float2 local = float2(c.x - 0.5, 0.5 - c.y) * i.SizeAdd.xy;
    float s, co;
    sincos(i.PosRot.w, s, co);
    local = float2(local.x * co - local.y * s, local.x * s + local.y * co);

    // Row vectors: view-space x is dot(p, first column), so the camera's right and up in the world are the first two columns.
    float3 right = float3(View[0][0], View[1][0], View[2][0]);
    float3 up = float3(View[0][1], View[1][1], View[2][1]);
    float3 world = i.PosRot.xyz + right * local.x + up * local.y;

    BillOut o;
    o.Pos = mul(float4(world, 1.0), ViewProj);
    o.Uv = i.Uv.xy + i.Uv.zw * c;
    o.Color = i.Color;
    o.Add = i.SizeAdd.z;
    return o;
}

[shader("pixel")]
float4 PSBillboard(BillOut i) : SV_Target
{
    float4 c = Tex.Sample(Samp, i.Uv) * i.Color;
    return float4(c.rgb, c.a * (1.0 - i.Add));
}
