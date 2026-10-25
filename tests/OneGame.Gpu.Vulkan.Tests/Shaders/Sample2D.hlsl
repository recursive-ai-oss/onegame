// Samples the center of a texture at its first visible mip level.

[[vk::binding(0, 0)]] Texture2D<float4> Tex;
[[vk::binding(1, 0)]] SamplerState Smp;

[shader("vertex")]
float4 VSMain(uint id : SV_VertexID) : SV_Position
{
    float2 p = float2((id << 1) & 2, id & 2);
    return float4(p * 2.0 - 1.0, 0.0, 1.0);
}

[shader("pixel")]
float4 PSMain() : SV_Target
{
    return Tex.SampleLevel(Smp, float2(0.5, 0.5), 0);
}
