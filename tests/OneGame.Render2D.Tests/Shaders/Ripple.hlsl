// A custom sprite material used by the tests: shifts the sprite's rows sideways in a sine wave and mixes in a colour.
// It follows the material contract: the vertex stage is the renderer's, so the input struct must match QuadOut in Quad.hlsl.

[[vk::binding(0, 1)]] Texture2D Tex;
[[vk::binding(1, 1)]] SamplerState Samp;

[[vk::binding(0, 2)]] cbuffer Params
{
    // P[0].x: wave amplitude in uv units, P[0].y: waves per sprite height, P[0].z: colour mix. P[1].rgb: colour (linear).
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
    float2 uv = i.Uv;
    uv.x += sin(uv.y * P[0].y * 6.2831853) * P[0].x;
    float4 t = Tex.Sample(Samp, uv) * i.Color;
    float3 mixed = lerp(t.rgb, P[1].rgb * t.a, P[0].z);
    return float4(mixed, t.a);
}
