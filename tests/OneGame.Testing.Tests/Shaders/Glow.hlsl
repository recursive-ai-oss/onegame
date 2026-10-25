// A material for the hot reload tests: the sprite's texture times a tint from the material parameters.

[[vk::binding(0, 1)]] Texture2D Tex;
[[vk::binding(1, 1)]] SamplerState Samp;

[[vk::binding(0, 2)]] cbuffer Params
{
    float4 P[2];
};

struct QuadOut
{
    float4 Pos : SV_Position;
    [[vk::location(0)]] float2 Uv : TEXCOORD0;
    [[vk::location(1)]] float4 Color : TEXCOORD1;
};

[shader("pixel")]
float4 PSMain(QuadOut i) : SV_Target
{
    float4 t = Tex.Sample(Samp, i.Uv) * i.Color;
    return float4(t.rgb * P[0].rgb, t.a);
}
