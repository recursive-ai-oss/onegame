// Assigns punctual lights to a grid of view-space clusters (screen tiles x exponential depth slices).
// One thread per cluster: build its bounding box, then keep every light whose culling sphere touches it.

#include "Frame.hlsli"

[[vk::binding(1, 0)]] StructuredBuffer<LightData> Lights;
[[vk::binding(2, 0)]] RWStructuredBuffer<uint> Clusters;

[shader("compute")]
[numthreads(64, 1, 1)]
void CSMain(uint3 id : SV_DispatchThreadID)
{
    uint tx = (uint)Grid.x;
    uint ty = (uint)Grid.y;
    uint tz = (uint)Grid.z;
    if (id.x >= tx * ty * tz)
    {
        return;
    }

    uint x = id.x % tx;
    uint y = (id.x / tx) % ty;
    uint z = id.x / (tx * ty);
    float near = Slice.z;
    float far = Slice.w;
    float zn = near * pow(far / near, (float)z / (float)tz);
    float zf = near * pow(far / near, (float)(z + 1u) / (float)tz);

    float2 px0 = float2(x, y) * Grid.w;
    float2 px1 = float2(x + 1u, y + 1u) * Grid.w;
    float2 n0 = float2(px0.x / ViewportInfo.x * 2.0 - 1.0, 1.0 - px0.y / ViewportInfo.y * 2.0);
    float2 n1 = float2(px1.x / ViewportInfo.x * 2.0 - 1.0, 1.0 - px1.y / ViewportInfo.y * 2.0);

    float3 mn = float3(1e30, 1e30, 1e30);
    float3 mx = float3(-1e30, -1e30, -1e30);
    for (uint c = 0u; c < 4u; c++)
    {
        float2 ndc = float2((c & 1u) != 0u ? n1.x : n0.x, (c & 2u) != 0u ? n1.y : n0.y);
        float4 q = mul(float4(ndc, 0.0, 1.0), InvProj);
        float3 p = q.xyz / q.w;
        for (uint e = 0u; e < 2u; e++)
        {
            float d = e == 0u ? zn : zf;
            float3 w;
            if (CameraPos.w > 0.5)
            {
                w = float3(p.xy, -d);
            }
            else
            {
                w = p * (d / -p.z);
            }

            mn = min(mn, w);
            mx = max(mx, w);
        }
    }

    uint stride = Counts.z;
    uint at = id.x * stride;
    uint count = 0u;
    for (uint i = Counts.x; i < Counts.y && count + 1u < stride; i++)
    {
        LightData l = Lights[i];
        float3 center = mul(float4(l.PosRange.xyz, 1.0), View).xyz;
        float r = l.Spot.z;
        float3 dv = max(max(mn - center, 0.0), center - mx);
        if (dot(dv, dv) <= r * r)
        {
            Clusters[at + 1u + count] = i;
            count++;
        }
    }

    Clusters[at] = count;
}
