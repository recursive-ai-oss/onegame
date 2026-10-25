// Each invocation writes a value derived from its index; the test reads the buffer back.

[[vk::binding(0, 0)]] RWStructuredBuffer<uint> Out;
[[vk::binding(1, 0)]] StructuredBuffer<uint> In;

[shader("compute")]
[numthreads(64, 1, 1)]
void CSMain(uint3 id : SV_DispatchThreadID)
{
    uint i = id.x;
    Out[i] = In[i] * 3u + i * i + 7u;
}
