// The split-sum environment BRDF table (Karis 2013): scale and bias applied to F0, by n.v (x) and roughness (y).
// Uses the same height-correlated Smith visibility as the direct lighting.

#include "Common.hlsli"

[[vk::binding(0, 0)]] cbuffer Params
{
    float4 A;   // x table size, y sample count
};
[[vk::binding(1, 0)]] [[vk::image_format("rg16f")]] RWTexture2D<float2> Out;

[shader("compute")]
[numthreads(8, 8, 1)]
void CSMain(uint3 id : SV_DispatchThreadID)
{
    uint size = (uint)A.x;
    if (id.x >= size || id.y >= size)
    {
        return;
    }

    float ndotv = max((id.x + 0.5) / size, 1e-3);
    float roughness = (id.y + 0.5) / size;
    float alpha = roughness * roughness;
    float a2 = alpha * alpha;
    float3 v = float3(sqrt(1.0 - ndotv * ndotv), 0.0, ndotv);
    float3 n = float3(0.0, 0.0, 1.0);
    uint count = (uint)A.y;

    float sa = 0.0;
    float sb = 0.0;
    for (uint k = 0u; k < count; k++)
    {
        float3 h = ImportanceGgx(Hammersley(k, count), n, alpha);
        float3 l = 2.0 * dot(v, h) * h - v;
        float ndotl = saturate(l.z);
        float ndoth = saturate(h.z);
        float vdoth = saturate(dot(v, h));
        if (ndotl > 0.0)
        {
            float gv = ndotl * sqrt(ndotv * ndotv * (1.0 - a2) + a2);
            float gl = ndotv * sqrt(ndotl * ndotl * (1.0 - a2) + a2);
            float vis = 0.5 / max(gv + gl, 1e-8);
            // pdf of h is D n.h; the estimator is brdf n.l / pdf(l) with pdf(l) = D n.h / (4 v.h).
            float gVis = vis * 4.0 * ndotl * vdoth / max(ndoth, 1e-8);
            float fc = pow(1.0 - vdoth, 5.0);
            sa += (1.0 - fc) * gVis;
            sb += fc * gVis;
        }
    }

    Out[id.xy] = float2(sa, sb) / (float)count;
}
