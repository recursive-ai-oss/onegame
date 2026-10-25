// Three hard-coded vertices with red, green and blue corners.

struct VsOut
{
    float4 Pos : SV_Position;
    [[vk::location(0)]] float3 Color : TEXCOORD0;
};

[shader("vertex")]
VsOut VSMain(uint id : SV_VertexID)
{
    static const float2 pos[3] = { float2(-0.8, -0.8), float2(0.8, -0.8), float2(0.0, 0.8) };
    static const float3 col[3] = { float3(1, 0, 0), float3(0, 1, 0), float3(0, 0, 1) };
    VsOut o;
    o.Pos = float4(pos[id], 0.0, 1.0);
    o.Color = col[id];
    return o;
}

[shader("pixel")]
float4 PSMain(VsOut i) : SV_Target
{
    return float4(i.Color, 1.0);
}
