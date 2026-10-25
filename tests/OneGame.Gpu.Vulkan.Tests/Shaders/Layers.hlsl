// Pixel x of a four-pixel-wide target samples layer x of a 2D array.

[[vk::binding(0, 0)]] Texture2DArray<float4> Layers;
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
    return Layers.SampleLevel(Smp, float3(0.5, 0.5, floor(pos.x)), 0);
}
