// VSDepth writes a flat depth of 0.5 over the target. The lookup compares 0.4 and 0.6 against it.

[[vk::binding(0, 0)]] Texture2D<float> DepthMap;
[[vk::binding(1, 0)]] SamplerComparisonState Cmp;

[shader("vertex")]
float4 VSDepth(uint id : SV_VertexID) : SV_Position
{
    float2 p = float2((id << 1) & 2, id & 2);
    return float4(p * 2.0 - 1.0, 0.5, 1.0);
}

// Beyond the far plane: only depth clamping keeps it.
[shader("vertex")]
float4 VSFar(uint id : SV_VertexID) : SV_Position
{
    float2 p = float2((id << 1) & 2, id & 2);
    return float4(p * 2.0 - 1.0, 1.5, 1.0);
}

[shader("vertex")]
float4 VSMain(uint id : SV_VertexID) : SV_Position
{
    float2 p = float2((id << 1) & 2, id & 2);
    return float4(p * 2.0 - 1.0, 0.0, 1.0);
}

[shader("pixel")]
float4 PSMain(float4 pos : SV_Position) : SV_Target
{
    float reference = 0.4 + 0.2 * floor(pos.x);
    float lit = DepthMap.SampleCmpLevelZero(Cmp, float2(0.5, 0.5), reference);
    return float4(lit, lit, lit, 1.0);
}
