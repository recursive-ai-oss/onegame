// Position and texture coordinate come from a vertex buffer; a constant buffer offsets the quad and tints the texture.

[[vk::binding(0, 0)]] cbuffer Params
{
    float4 Tint;
    float2 Offset;
};

[[vk::binding(1, 0)]] Texture2D Tex;
[[vk::binding(2, 0)]] SamplerState Smp;

struct VsIn
{
    [[vk::location(0)]] float2 Pos : POSITION;
    [[vk::location(1)]] float2 Uv : TEXCOORD0;
};

struct VsOut
{
    float4 Pos : SV_Position;
    [[vk::location(0)]] float2 Uv : TEXCOORD0;
};

[shader("vertex")]
VsOut VSMain(VsIn i)
{
    VsOut o;
    o.Pos = float4(i.Pos + Offset, 0.0, 1.0);
    o.Uv = i.Uv;
    return o;
}

[shader("pixel")]
float4 PSMain(VsOut i) : SV_Target
{
    return Tex.Sample(Smp, i.Uv) * Tint;
}
