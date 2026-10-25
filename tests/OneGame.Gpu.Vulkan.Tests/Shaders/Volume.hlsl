// Pixel x of a four-pixel-wide target samples slice x of a 4-deep volume.

[[vk::binding(0, 0)]] Texture3D<float4> Vol;
[[vk::binding(1, 0)]] SamplerState Smp;

[shader("vertex")]
float4 VSMain(uint id : SV_VertexID) : SV_Position
{
    float2 p = float2((id << 1) & 2, id & 2);
    return float4(p * 2.0 - 1.0, 0.0, 1.0);
}

[shader("pixel")]
float4 PSMain(float4 pos : SV_Position) : SV_Target
{
    return Vol.SampleLevel(Smp, float3(0.5, 0.5, pos.x / 4.0), 0);
}
