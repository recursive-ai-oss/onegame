// Two triangles from a vertex buffer, drawn red; and compute passes that write indirect arguments and then use them.

struct VsIn
{
    [[vk::location(0)]] float2 Pos : POSITION;
};

[shader("vertex")]
float4 VSMain(VsIn i) : SV_Position
{
    return float4(i.Pos, 0.0, 1.0);
}

[shader("pixel")]
float4 PSMain() : SV_Target
{
    return float4(1, 0, 0, 1);
}

[[vk::binding(0, 0)]] RWByteAddressBuffer Args;
[[vk::binding(1, 0)]] RWStructuredBuffer<uint> Result;

[shader("compute")]
[numthreads(1, 1, 1)]
void CSArgs()
{
    Args.Store3(0, uint3(3, 1, 1));
}

[shader("compute")]
[numthreads(4, 1, 1)]
void CSFill(uint3 id : SV_DispatchThreadID)
{
    Result[id.x] = id.x + 1;
}
