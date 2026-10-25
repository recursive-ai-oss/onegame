// Shared helpers of the image-based lighting kernels.

#define PI 3.14159265359

// Direction of texel (u, v in -1..1, v growing downwards) of a cube face, inverse of the face selection table in the Vulkan
// specification (section 16.5.4, cube map face selection). Faces are +X, -X, +Y, -Y, +Z, -Z.
float3 CubeDir(uint face, float u, float v)
{
    float3 d;
    if (face == 0u) { d = float3(1.0, -v, -u); }
    else if (face == 1u) { d = float3(-1.0, -v, u); }
    else if (face == 2u) { d = float3(u, 1.0, v); }
    else if (face == 3u) { d = float3(u, -1.0, -v); }
    else if (face == 4u) { d = float3(u, -v, 1.0); }
    else { d = float3(-u, -v, -1.0); }
    return normalize(d);
}

float RadicalInverse(uint bits)
{
    bits = (bits << 16u) | (bits >> 16u);
    bits = ((bits & 0x55555555u) << 1u) | ((bits & 0xAAAAAAAAu) >> 1u);
    bits = ((bits & 0x33333333u) << 2u) | ((bits & 0xCCCCCCCCu) >> 2u);
    bits = ((bits & 0x0F0F0F0Fu) << 4u) | ((bits & 0xF0F0F0F0u) >> 4u);
    bits = ((bits & 0x00FF00FFu) << 8u) | ((bits & 0xFF00FF00u) >> 8u);
    return (float)bits * 2.3283064365386963e-10;
}

float2 Hammersley(uint i, uint n)
{
    return float2((float)i / (float)n, RadicalInverse(i));
}

// A frame around n for tangent-space sampling.
void Basis(float3 n, out float3 t, out float3 b)
{
    float3 up = abs(n.y) < 0.999 ? float3(0.0, 1.0, 0.0) : float3(1.0, 0.0, 0.0);
    t = normalize(cross(up, n));
    b = cross(n, t);
}

// GGX half vector (Karis 2013, "Real Shading in Unreal Engine 4"), alpha = roughness^2.
float3 ImportanceGgx(float2 xi, float3 n, float alpha)
{
    float phi = 2.0 * PI * xi.x;
    float cosTheta = sqrt((1.0 - xi.y) / (1.0 + (alpha * alpha - 1.0) * xi.y));
    float sinTheta = sqrt(max(1.0 - cosTheta * cosTheta, 0.0));
    float3 h = float3(sinTheta * cos(phi), sinTheta * sin(phi), cosTheta);
    float3 t;
    float3 b;
    Basis(n, t, b);
    return normalize(t * h.x + b * h.y + n * h.z);
}

float DistributionGgx(float ndoth, float a2)
{
    float d = ndoth * ndoth * (a2 - 1.0) + 1.0;
    return a2 / (PI * d * d);
}
