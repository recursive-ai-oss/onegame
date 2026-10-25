// Fills the target with one color taken from the push-constant block.

struct PushData
{
    float4 Tint;
};

[[vk::push_constant]] ConstantBuffer<PushData> Push;

[shader("vertex")]
float4 VSMain(uint id : SV_VertexID) : SV_Position
{
    float2 p = float2((id << 1) & 2, id & 2);
    return float4(p * 2.0 - 1.0, 0.0, 1.0);
}

[shader("pixel")]
float4 PSMain() : SV_Target
{
    return Push.Tint;
}
