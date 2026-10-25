// Resamples an equirectangular panorama into one mip of a cube map (faces are array layers).
// Panorama convention: v = 0 is straight up, u = 0.5 is the +Z direction, u grows towards +X.

#include "Common.hlsli"

[[vk::binding(0, 0)]] cbuffer Params
{
    float4 A;   // x face size
};
[[vk::binding(1, 0)]] Texture2D<float4> Equirect;
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
    float3 d = CubeDir(id.z, u, v);
    float2 uv = float2(atan2(d.x, -d.z) / (2.0 * PI) + 1.0, acos(clamp(d.y, -1.0, 1.0)) / PI);
    OutCube[id] = float4(Equirect.SampleLevel(Samp, uv, 0).rgb, 1.0);
}
