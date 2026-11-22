// A full-screen pass of the game's own for a post chain: splits the red and blue channels sideways and darkens every other row.
// The vertex stage is the renderer's, so the input struct matches QuadOut of the quad stage. Set 0 holds the size of the image being
// drawn, set 1 the image so far, set 2 the material's parameters.

[[vk::binding(0, 0)]] cbuffer Frame
{
    float2 Viewport;
    float2 FramePad;
};

[[vk::binding(0, 1)]] Texture2D Tex;
[[vk::binding(1, 1)]] SamplerState Samp;

[[vk::binding(0, 2)]] cbuffer Params
{
    // P[0].x: channel split in pixels, P[0].y: how dark the dim rows are (0 to 1).
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
    float2 shift = float2(P[0].x / Viewport.x, 0.0);
    float4 mid = Tex.Sample(Samp, i.Uv);
    float red = Tex.Sample(Samp, i.Uv + shift).r;
    float blue = Tex.Sample(Samp, i.Uv - shift).b;
    // cos(pi * y) at a pixel centre alternates between -1 and 1, so every second row is dimmed.
    float dim = 1.0 - P[0].y * (0.5 - 0.5 * cos(3.14159265 * i.Uv.y * Viewport.y));
    return float4(float3(red, mid.g, blue) * dim, mid.a);
}
