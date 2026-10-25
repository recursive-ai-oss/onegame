// Cosine-weighted mean radiance around each direction (irradiance divided by pi), from cosine-distributed samples of a source mip.

#include "Common.hlsli"

[[vk::binding(0, 0)]] cbuffer Params
{
    float4 A;   // x face size, y sample count, z source face size
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
    float3 t;
    float3 b;
    Basis(n, t, b);
    uint count = (uint)A.y;
    float texelSolidAngle = 4.0 * PI / (6.0 * A.z * A.z);

    float3 sum = 0.0;
    for (uint k = 0u; k < count; k++)
    {
        float2 xi = Hammersley(k, count);
        float phi = 2.0 * PI * xi.x;
        float cosTheta = sqrt(1.0 - xi.y);
        float sinTheta = sqrt(xi.y);
        float3 l = normalize(t * (sinTheta * cos(phi)) + b * (sinTheta * sin(phi)) + n * cosTheta);
        float pdf = cosTheta / PI;
        float lod = max(0.5 * log2(1.0 / ((float)count * pdf + 1e-4) / texelSolidAngle) + 1.0, 0.0);
        sum += Source.SampleLevel(Samp, l, lod).rgb;
    }

    OutCube[id] = float4(sum / (float)count, 1.0);
}
