// Pixel x of a six-pixel-wide target picks one axis direction and samples the cube map there.

[[vk::binding(0, 0)]] TextureCube<float4> Cube;
[[vk::binding(1, 0)]] SamplerState Smp;

[shader("vertex")]
float4 VSMain(uint id : SV_VertexID) : SV_Position
{
    float2 p = float2((id << 1) & 2, id & 2);
    return float4(p * 2.0 - 1.0, 0.0, 1.0);
}

float3 Axis(uint i)
{
    float3 d = float3(0, 0, 0);
    float s = (i & 1) == 0 ? 1.0 : -1.0;
    uint a = i >> 1;
    if (a == 0) { d.x = s; }
    else if (a == 1) { d.y = s; }
    else { d.z = s; }
    return d;
}

[shader("pixel")]
float4 PSMain(float4 pos : SV_Position) : SV_Target
{
    return Cube.SampleLevel(Smp, Axis((uint)pos.x), 0);
}
