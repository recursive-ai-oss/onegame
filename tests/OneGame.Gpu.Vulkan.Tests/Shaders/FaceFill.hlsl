// Fills the target with one color from a constant buffer; the buffer range moves with a dynamic offset.

[[vk::binding(0, 0)]] cbuffer Face
{
    float4 Tint;
};

[shader("vertex")]
float4 VSMain(uint id : SV_VertexID) : SV_Position
{
    float2 p = float2((id << 1) & 2, id & 2);
    return float4(p * 2.0 - 1.0, 0.0, 1.0);
}

[shader("pixel")]
float4 PSMain() : SV_Target
{
    return Tint;
}
