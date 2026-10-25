// Screen-space ambient occlusion from the view-space normals and distances of the depth prepass (Gbuf: xyz normal, w distance along the
// view axis, 0 where nothing was drawn), then a depth-aware blur. Both passes draw a full-screen triangle with Post.VSFull.
//   set 0: binding 0 parameters, binding 1 Gbuf, binding 2 the raw occlusion (blur only)

[[vk::binding(0, 0)]] cbuffer Params
{
    float4 A;   // x, y projection scale (matrix m11, m22), z 1 for an orthographic camera, w sampling radius in view units
    float4 B;   // x intensity, y depth bias as a fraction of the radius, z sample count
    float4 C;   // xy Gbuf size, zw occlusion target size (they differ at half resolution)
};

[[vk::binding(1, 0)]] Texture2D<float4> Gbuf;
[[vk::binding(2, 0)]] Texture2D<float4> Raw;

struct FullIn
{
    float4 Pos : SV_Position;
    [[vk::location(0)]] float2 Uv : TEXCOORD0;
};

float4 LoadG(int2 p)
{
    int2 hi = int2(C.xy) - 1;
    return Gbuf.Load(int3(clamp(p, int2(0, 0), hi), 0));
}

float3 ViewPos(float2 uv, float dist)
{
    float2 ndc = float2(uv.x * 2.0 - 1.0, 1.0 - uv.y * 2.0);
    float2 xy = A.z > 0.5 ? ndc / A.xy : ndc * dist / A.xy;
    return float3(xy, -dist);
}

float2 ProjectUv(float3 p)
{
    float d = max(-p.z, 1e-4);
    float2 ndc = A.z > 0.5 ? p.xy * A.xy : p.xy * A.xy / d;
    return float2(ndc.x * 0.5 + 0.5, 0.5 - ndc.y * 0.5);
}

[shader("pixel")]
float4 PSSsao(FullIn i) : SV_Target
{
    int2 px = int2(i.Pos.xy);
    float2 scale = C.xy / C.zw;
    int2 gp = int2((float2(px) + 0.5) * scale);
    float4 g = LoadG(gp);
    if (g.w <= 0.0)
    {
        return float4(1.0, 1.0, 1.0, 1.0);
    }

    float3 n = g.xyz;
    float3 p = ViewPos((float2(gp) + 0.5) / C.xy, g.w);

    // Interleaved gradient noise (Jimenez, "Next Generation Post Processing in Call of Duty: Advanced Warfare") turns the pattern per pixel;
    // the blur removes the grain.
    float noise = frac(52.9829189 * frac(dot(i.Pos.xy, float2(0.06711056, 0.00583715))));
    float noise2 = frac(noise * 7.13 + 0.37);
    float3 up = abs(n.z) < 0.99 ? float3(0.0, 0.0, 1.0) : float3(1.0, 0.0, 0.0);
    float3 t = normalize(cross(up, n));
    float3 b = cross(n, t);
    float radius = A.w;
    float bias = B.y * radius + 0.002 * g.w;
    int count = (int)B.z;
    float occ = 0.0;
    [loop] for (int k = 0; k < count; k++)
    {
        // Cosine-weighted directions in the hemisphere around the normal, spread over the radius.
        float u = ((float)k + 0.5) / (float)count;
        float phi = (float)k * 2.39996323 + noise * 6.2831853;
        float sinT = sqrt(u);
        float cosT = sqrt(1.0 - u);
        float3 dir = t * (cos(phi) * sinT) + b * (sin(phi) * sinT) + n * cosT;
        float s = lerp(0.1, 1.0, frac(u * 3.7 + noise2));
        float3 sp = p + dir * (radius * s * s);
        float2 suv = ProjectUv(sp);
        if (any(suv < 0.0) || any(suv > 1.0))
        {
            continue;
        }

        float4 sg = LoadG(int2(suv * C.xy));
        if (sg.w <= 0.0)
        {
            continue;
        }

        // The sample is occluded when the surface seen through its pixel is in front of it; surfaces far in front belong to another object.
        float diff = -sp.z - sg.w;
        float range = smoothstep(0.0, 1.0, radius / max(abs(g.w - sg.w), 1e-4));
        occ += (diff > bias ? 1.0 : 0.0) * range;
    }

    float ao = saturate(1.0 - B.x * occ / (float)count);
    return float4(ao, ao, ao, 1.0);
}

[shader("pixel")]
float4 PSBlur(FullIn i) : SV_Target
{
    int2 px = int2(i.Pos.xy);
    float2 scale = C.xy / C.zw;
    float center = LoadG(int2((float2(px) + 0.5) * scale)).w;
    if (center <= 0.0)
    {
        return float4(1.0, 1.0, 1.0, 1.0);
    }

    int2 hi = int2(C.zw) - 1;
    float sum = 0.0;
    float weights = 0.0;
    [loop] for (int y = -2; y <= 2; y++)
    {
        [loop] for (int x = -2; x <= 2; x++)
        {
            int2 q = clamp(px + int2(x, y), int2(0, 0), hi);
            float d = LoadG(int2((float2(q) + 0.5) * scale)).w;
            float w = d > 0.0 ? saturate(1.0 - abs(d - center) / (0.05 * center + 0.01)) : 0.0;
            sum += Raw.Load(int3(q, 0)).r * w;
            weights += w;
        }
    }

    float ao = weights > 0.0 ? sum / weights : 1.0;
    return float4(ao, ao, ao, 1.0);
}
