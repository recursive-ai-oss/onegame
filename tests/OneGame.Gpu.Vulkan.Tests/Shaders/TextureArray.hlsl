// Pixel x of a four-pixel-wide target samples element x of a texture array, with an index that differs between pixels.

[[vk::binding(0, 0)]] Texture2D<float4> Tex[4];
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
    uint i = (uint)pos.x;
    return Tex[NonUniformResourceIndex(i)].SampleLevel(Smp, float2(0.5, 0.5), 0);
}
