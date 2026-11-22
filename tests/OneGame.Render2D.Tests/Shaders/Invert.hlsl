// A full-screen pass written for the tests, the way a game writes its own: inverts the colour of the image so far.
// P[0].x is how much (0 leaves it, 1 inverts it).

[[vk::binding(0, 1)]] Texture2D Tex;
[[vk::binding(1, 1)]] SamplerState Samp;

[[vk::binding(0, 2)]] cbuffer Params
{
    float4 P[1];
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
    float4 t = Tex.Sample(Samp, i.Uv);
    float3 inverted = (float3(1.0, 1.0, 1.0) - t.rgb) * t.a;
    return float4(lerp(t.rgb, inverted, P[0].x), t.a);
}
