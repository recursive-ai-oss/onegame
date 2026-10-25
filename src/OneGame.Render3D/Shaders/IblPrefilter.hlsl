// Prefilters the environment for one roughness: the radiance a GGX lobe gathers when the view and the normal both point along the
// output direction. Samples read a mip of the source chosen from each sample's solid angle, which keeps bright small lights from
// leaving speckles (Colbert and Krivanek, GPU Gems 3 chapter 20).

#include "Common.hlsli"

[[vk::binding(0, 0)]] cbuffer Params
{
    float4 A;   // x face size, y roughness, z sample count, w source face size
};
[[vk::binding(1, 0)]] TextureCube<float4> Source;
[[vk::binding(2, 0)]] SamplerState Samp;
[[vk::binding(3, 0)]] [[vk::image_format("rgba16f")]] RWTexture2DArray<float4> OutCube;

[shader("compute")]
[numthreads(8, 8, 1)]
void CSMain(uint3 id : SV_DispatchThreadID)
{
    uint size = (uint)A.x;
    if (id.x >= size || id.y >= size)
    {
        return;
    }

    float u = (id.x + 0.5) / size * 2.0 - 1.0;
    float v = (id.y + 0.5) / size * 2.0 - 1.0;
    float3 n = CubeDir(id.z, u, v);
    float alpha = A.y * A.y;
    float a2 = alpha * alpha;
    uint count = (uint)A.z;
    float texelSolidAngle = 4.0 * PI / (6.0 * A.w * A.w);

    float3 sum = 0.0;
    float weight = 0.0;
    for (uint k = 0u; k < count; k++)
    {
        float3 h = ImportanceGgx(Hammersley(k, count), n, alpha);
        float3 l = 2.0 * dot(n, h) * h - n;
        float ndotl = dot(n, l);
        if (ndotl > 0.0)
        {
            float ndoth = saturate(dot(n, h));
            float pdf = DistributionGgx(ndoth, a2) * 0.25;   // n = v makes n.h = v.h
            float sampleSolidAngle = 1.0 / ((float)count * pdf + 1e-4);
            float lod = A.y <= 0.0 ? 0.0 : max(0.5 * log2(sampleSolidAngle / texelSolidAngle) + 1.0, 0.0);
            sum += Source.SampleLevel(Samp, l, lod).rgb * ndotl;
            weight += ndotl;
        }
    }

    OutCube[id] = float4(sum / max(weight, 1e-4), 1.0);
}
