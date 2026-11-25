// Light shafts: single scattering of the lights that opt in, marched along each view ray through the shadow maps. Two passes draw a
// full-screen triangle with Post.VSFull:
//   PSMarch     at a fraction of the frame, writes the in-scattered light (rgb) and the view distance of the pixel it stands for (a)
//   PSComposite at full size, upsamples that image with weights from the depth difference and adds it onto the lit scene
//
//   set 0: binding 0 frame constants, 1 parameters, 2 prepass image (xyz normal, w view distance, 0 = no surface), 3 shadow atlas,
//          4 shadow records, 5 shadow comparison sampler, 6 the marched image (composite only)

#include "Frame.hlsli"

[[vk::binding(1, 0)]] cbuffer ShaftParams
{
    float4 ShaftA;      // x steps, y longest march in world units, z scattering per world unit, w Henyey-Greenstein anisotropy
    float4 ShaftB;      // x intensity, y 1 to fade shafts with the fog, zw size of the marched image
    float4 ShaftC;      // xy size of the full frame, z lights
    float4 Lt[16];      // per light, four float4 as a LightData: position and range, direction and type, colour times intensity, spot terms (w = shadow record + 1, 0 = none)
};

[[vk::binding(2, 0)]] Texture2D<float4> Gbuf;

struct ShadowData
{
    float4 Col[24];
    float4 Rect[6];
    float4 Splits;
    float4 Texels;
    float4 P0;          // x kind (0 directional, 1 spot, 2 point), y cascades
    float4 P1;          // xy atlas texel size in uv
};

[[vk::binding(3, 0)]] Texture2D<float> ShadowAtlas;
[[vk::binding(4, 0)]] StructuredBuffer<ShadowData> Shadows;
[[vk::binding(5, 0)]] SamplerComparisonState ShadowSamp;
[[vk::binding(6, 0)]] Texture2D<float4> Scatter;

struct FullIn
{
    float4 Pos : SV_Position;
    [[vk::location(0)]] float2 Uv : TEXCOORD0;
};

// ---- shadow lookup: one comparison tap (the hardware filters 2x2) of the tile the point falls in; no normal, the point is in air ----

float ShadowTap(uint index, uint tile, float3 pos)
{
    float4 h = float4(pos, 1.0);
    uint b = tile * 4u;
    float4 c = float4(dot(h, Shadows[index].Col[b]), dot(h, Shadows[index].Col[b + 1u]), dot(h, Shadows[index].Col[b + 2u]), dot(h, Shadows[index].Col[b + 3u]));
    if (c.w <= 0.0)
    {
        return 1.0;
    }

    float3 ndc = c.xyz / c.w;
    if (any(abs(ndc.xy) > 1.0) || ndc.z < 0.0 || ndc.z > 1.0)
    {
        return 1.0;
    }

    float4 rect = Shadows[index].Rect[tile];
    float2 uv = float2(ndc.x * 0.5 + 0.5, 0.5 - ndc.y * 0.5);
    float2 a = rect.xy + uv * rect.zw;
    float2 pad = Shadows[index].P1.xy;
    a = clamp(a, rect.xy + pad, rect.xy + rect.zw - pad);
    return ShadowAtlas.SampleCmpLevelZero(ShadowSamp, a, ndc.z);
}

float ShadowAt(uint index, float3 lightPos, float3 pos, float viewZ)
{
    uint kind = (uint)Shadows[index].P0.x;
    if (kind == 0u)
    {
        uint count = (uint)Shadows[index].P0.y;
        float last = Shadows[index].Splits[count - 1u];
        if (viewZ > last)
        {
            return 1.0;
        }

        uint tile = 0u;
        [loop] while (tile + 1u < count && viewZ > Shadows[index].Splits[tile])
        {
            tile++;
        }

        return lerp(1.0, ShadowTap(index, tile, pos), saturate((last - viewZ) / (0.1 * last)));
    }

    uint face = 0u;
    if (kind == 2u)
    {
        float3 d = pos - lightPos;
        float3 a = abs(d);
        face = (a.x >= a.y && a.x >= a.z) ? (d.x > 0.0 ? 0u : 1u) : ((a.y >= a.z) ? (d.y > 0.0 ? 2u : 3u) : (d.z > 0.0 ? 4u : 5u));
    }

    return ShadowTap(index, face, pos);
}

// Henyey-Greenstein phase function, normalised over the sphere. c is the cosine between the view ray and the direction to the light:
// 1 looks straight into the light, where positive g scatters most.
float Phase(float c, float g)
{
    float d = 1.0 + g * g - 2.0 * g * c;
    return (1.0 - g * g) / (4.0 * PI * d * sqrt(d));
}

// What a light delivers at a point in the air: its colour times falloff, cone and shadow. Same falloff as the surface shader
// (KHR_lights_punctual: inverse square, smooth window at the range, spot cone squared).
float3 LightAt(uint j, float3 pos, float3 dir, float viewZ)
{
    float3 posRange = Lt[j * 4u].xyz;
    float range = Lt[j * 4u].w;
    float4 dirType = Lt[j * 4u + 1u];
    float4 colour = Lt[j * 4u + 2u];
    float4 spot = Lt[j * 4u + 3u];
    float3 toLight;
    float att = 1.0;
    if (dirType.w < 0.5)
    {
        toLight = -dirType.xyz;
    }
    else
    {
        float3 d = posRange - pos;
        float dist2 = dot(d, d);
        toLight = d * rsqrt(max(dist2, 1e-8));
        att = 1.0 / max(dist2, 1.0);       // the lamp counts as one unit wide, so the air at its very surface does not blow up
        if (range > 0.0)
        {
            float r = dist2 / (range * range);
            float w = saturate(1.0 - r * r);
            att *= w;
        }

        if (dirType.w > 1.5)
        {
            float a = saturate(dot(dirType.xyz, -toLight) * spot.x + spot.y);
            att *= a * a;
        }
    }

    if (att <= 0.0)
    {
        return 0.0;
    }

    if (spot.w > 0.5)
    {
        att *= ShadowAt((uint)(spot.w - 0.5), posRange, pos, viewZ);
    }

    return colour.rgb * (att * Phase(dot(dir, toLight), ShaftA.w));
}

[shader("pixel")]
float4 PSMarch(FullIn i) : SV_Target
{
    float2 uv = i.Pos.xy / ShaftB.zw;
    int2 hi = int2(ShaftC.xy) - 1;
    int2 gp = clamp(int2(uv * ShaftC.xy), int2(0, 0), hi);
    float surface = Gbuf.Load(int3(gp, 0)).w;

    // The ray through this pixel: the near and far plane points give its direction; a perspective ray starts at the camera.
    float2 ndc = float2(uv.x * 2.0 - 1.0, 1.0 - uv.y * 2.0);
    float4 n0 = mul(float4(ndc, 0.0, 1.0), InvViewProj);
    float4 n1 = mul(float4(ndc, 1.0, 1.0), InvViewProj);
    float3 p0 = n0.xyz / n0.w;
    float3 dir = normalize(n1.xyz / n1.w - p0);
    float3 origin = CameraPos.w > 0.5 ? p0 : CameraPos.xyz;
    float originDepth = -mul(float4(origin, 1.0), View).z;
    float along = max(-dot(dir, View._m02_m12_m22), 1e-3);        // view depth gained per unit along the ray

    float end = ShaftA.y;
    if (surface > 0.0)
    {
        end = min(end, max(surface - originDepth, 0.0) / along);
    }

    int steps = (int)ShaftA.x;
    float dt = end / (float)steps;

    // Interleaved gradient noise (Jimenez, "Next Generation Post Processing in Call of Duty: Advanced Warfare") moves each ray's samples
    // by a fraction of a step, trading banding for fine grain that the upsample smooths.
    float noise = frac(52.9829189 * frac(dot(i.Pos.xy, float2(0.06711056, 0.00583715))));
    uint lights = (uint)ShaftC.z;
    float3 sum = 0.0;
    [loop] for (int k = 0; k < steps; k++)
    {
        float s = ((float)k + noise) * dt;
        float3 p = origin + dir * s;
        float viewZ = max(-mul(float4(p, 1.0), View).z, 1e-4);
        float transmittance = exp(-ShaftA.z * s);
        if (ShaftB.y > 0.5)
        {
            transmittance *= 1.0 - FogAmount(p);
        }

        float3 inLight = 0.0;
        for (uint j = 0u; j < lights; j++)
        {
            inLight += LightAt(j, p, dir, viewZ);
        }

        sum += inLight * (transmittance * ShaftA.z * dt);
    }

    return float4(min(sum * ShaftB.x, 60000.0), surface);
}

float DepthWeight(float a, float b)
{
    return max(saturate(1.0 - abs(a - b) / (0.05 * max(a, b) + 0.01)), 1e-4);
}

// The marched image is grainy by design (each ray starts its samples at a different offset), so the upsample reads a 4x4 block of its
// texels with a tent filter two texels wide, and weights each by how close its surface distance is to the full-size pixel's: a shaft never bleeds
// across the edge of a nearer object.
[shader("pixel")]
float4 PSComposite(FullIn i) : SV_Target
{
    int2 px = int2(i.Pos.xy);
    float depth = Gbuf.Load(int3(px, 0)).w;
    int2 lowHi = int2(ShaftB.zw) - 1;
    float2 pos = i.Pos.xy * ShaftB.zw / ShaftC.xy - 0.5;    // in texel centres of the marched image
    int2 base = int2(floor(pos));
    float3 sum = 0.0;
    float weights = 0.0;
    [unroll] for (int y = -1; y <= 2; y++)
    {
        [unroll] for (int x = -1; x <= 2; x++)
        {
            int2 q = base + int2(x, y);
            float2 d = abs(float2(q) - pos);
            float spatial = max(2.0 - d.x, 0.0) * max(2.0 - d.y, 0.0);
            float4 t = Scatter.Load(int3(clamp(q, int2(0, 0), lowHi), 0));
            float w = spatial * DepthWeight(depth, t.a);
            sum += t.rgb * w;
            weights += w;
        }
    }

    return float4(sum / max(weights, 1e-6), 0.0);
}
