// A sprite material of the game's own: multiplies the sprite by a colour from its parameters. The vertex stage is the renderer's, so the
// input struct matches the quad stage's output.

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
