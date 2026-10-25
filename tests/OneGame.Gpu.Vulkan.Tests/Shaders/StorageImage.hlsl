// CSMain writes a gradient into a storage image; PSMain reads it back texel by texel.

[[vk::binding(0, 0)]] [[vk::image_format("rgba8")]] RWTexture2D<float4> Out;
[[vk::binding(0, 1)]] Texture2D<float4> In;

[shader("compute")]
[numthreads(8, 8, 1)]
void CSMain(uint3 id : SV_DispatchThreadID)
{
    Out[id.xy] = float4(id.x / 15.0, id.y / 15.0, 0.5, 1.0);
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
    return In.Load(int3((int)pos.x, (int)pos.y, 0));
}
