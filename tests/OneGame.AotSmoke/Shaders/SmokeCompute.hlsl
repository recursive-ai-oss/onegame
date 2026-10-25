// Writes index * 3 + 1 into a storage buffer; the smoke test checks the values after a direct and an indirect dispatch.

[[vk::binding(0, 0)]] RWStructuredBuffer<uint> Out;

[shader("compute")]
[numthreads(8, 1, 1)]
void CSMain(uint3 id : SV_DispatchThreadID)
{
    Out[id.x] = id.x * 3u + 1u;
}
