// Per-frame constants and the light record shared by the cluster compute shader and the forward shaders.
// Matrices are written by the CPU in System.Numerics layout (row-major, row vectors): use mul(v, M).

#define PI 3.14159265359

struct LightData
{
    float4 PosRange;   // xyz position, w range (0 = unbounded)
    float4 DirType;    // xyz direction the light shines in (towards the surface), w type: 0 directional, 1 point, 2 spot
    float4 Color;      // rgb colour times intensity (lux for directional, candela otherwise)
    float4 Spot;       // x scale, y offset (angular attenuation = saturate(cos * scale + offset)), z culling radius
};

[[vk::binding(0, 0)]] cbuffer Frame
{
    row_major float4x4 View;
    row_major float4x4 Proj;
    row_major float4x4 ViewProj;
    row_major float4x4 InvProj;
    row_major float4x4 InvViewProj;
    float4 CameraPos;   // xyz position, w = 1 for an orthographic camera
    float4 Grid;        // x tiles across, y tiles down, z depth slices, w tile size in pixels
    float4 Slice;       // x scale, y bias (slice = floor(log2(z) * x + y)), z near, w far
    float4 EnvA;        // x highest specular mip, y intensity, z cos(yaw), w sin(yaw)
    float4 ViewportInfo; // x width, y height
    uint4 Counts;       // x directional lights, y all lights, z uints per cluster, w unused
    float4 Sky;         // x background mip, y background intensity, z unused, w unused
    float4 ShadowInfo;  // x shadow records, y unused
    float4 AoInfo;      // x ambient occlusion strength (0 = none), y radius in world units
    float4 FogColor;    // rgb linear fog colour, w maximum density
    float4 FogRange;    // x start distance, y 1 / (end - start), z 1 when fog is on
    float4 EnvB;        // x highest specular mip of the second environment, y blend weight towards it, z backdrop mip of the second environment
    row_major float4x4 ReflViewProj;   // the main view mirrored about the reflection plane (planar reflection lookup)
    float4 Refl;        // x 1 while drawing the reflection itself (clip below the plane), y plane height, z 1 when a reflection image is bound, w scene time in seconds
    float4 MainZ;       // the third column of the main view, so shadow cascades pick by the main camera's depth in the reflection too
};

// Distance fog: linear in the distance from the camera (view depth for an orthographic camera) between start and end, scaled by the maximum density.
float FogAmount(float3 worldPos)
{
    float d = CameraPos.w > 0.5 ? -mul(float4(worldPos, 1.0), View).z : length(worldPos - CameraPos.xyz);
    return FogRange.z > 0.5 ? saturate((d - FogRange.x) * FogRange.y) * FogColor.w : 0.0;
}

// Blends a lit colour towards the fog colour. weight is the share of the fog colour a fully fogged pixel shows (1 for opaque colour,
// the pixel's coverage for premultiplied colour, 0 for light that is only added).
float3 ApplyFog(float3 color, float amount, float weight)
{
    return color * (1.0 - amount) + FogColor.rgb * (amount * weight);
}
