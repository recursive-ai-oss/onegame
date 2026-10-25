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
};
