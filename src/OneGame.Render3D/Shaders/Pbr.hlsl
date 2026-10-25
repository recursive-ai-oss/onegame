// glTF 2.0 metallic-roughness shading (specification Appendix B) with clustered forward lighting and image-based lighting.
//
//   set 0: frame constants, lights, light clusters, joint matrices, environment textures, shadow atlas and records, ambient occlusion
//   set 1: per-object constants (dynamic offset)
//   set 2: material constants, five textures, five samplers

#include "Frame.hlsli"

[[vk::binding(1, 0)]] StructuredBuffer<LightData> Lights;
[[vk::binding(2, 0)]] StructuredBuffer<uint> Clusters;
[[vk::binding(3, 0)]] StructuredBuffer<float4> Joints;      // four rows per matrix
[[vk::binding(4, 0)]] TextureCube<float4> Irradiance;       // cosine-weighted mean radiance (irradiance / pi)
[[vk::binding(5, 0)]] TextureCube<float4> Specular;         // GGX-prefiltered radiance, roughness = mip / highest mip
[[vk::binding(6, 0)]] Texture2D<float2> BrdfLut;            // split-sum scale and bias by (n.v, roughness)
[[vk::binding(7, 0)]] SamplerState EnvSampler;

// One shadow-casting light. Matrices are stored as columns (four float4 per matrix) so clip = (dot(p, c0), dot(p, c1), dot(p, c2), dot(p, c3)).
struct ShadowData
{
    float4 Col[24];     // up to six view-projection matrices: cascades, one spot face, or the six faces of a point light
    float4 Rect[6];     // per tile: xy offset in the atlas (uv), zw size in the atlas (uv)
    float4 Splits;      // directional: far view depth of each cascade
    float4 Texels;      // directional: world size of one texel per cascade. spot and point: texel size per unit distance (x)
    float4 P0;          // x kind (0 directional, 1 spot, 2 point), y cascades, z normal offset in texels, w depth bias
    float4 P1;          // xy atlas texel size in uv, z filter radius in texels
};

[[vk::binding(8, 0)]] Texture2D<float> ShadowAtlas;
[[vk::binding(9, 0)]] StructuredBuffer<ShadowData> Shadows;
[[vk::binding(10, 0)]] Texture2D<float4> AoTex;
[[vk::binding(11, 0)]] SamplerComparisonState ShadowSamp;

[[vk::binding(0, 1)]] cbuffer Object
{
    row_major float4x4 Model;
    row_major float4x4 NormalMat;   // transpose(inverse(Model))
    uint4 ObjInfo;                  // x first joint, y 1 when skinned
};

[[vk::binding(0, 2)]] cbuffer Material
{
    float4 BaseColor;
    float4 Emissive;       // rgb already multiplied by emissive strength
    float4 P0;             // x metallic, y roughness, z normal scale, w occlusion strength
    float4 P1;             // x alpha cutoff, y alpha mode (0 opaque, 1 mask, 2 blend), z unlit, w dielectric f0
    uint4 Flags;           // x has normal map
    float4 Xf[10];         // per texture: row 0 = (m11, m21, m31, uv set), row 1 = (m12, m22, m32, 0)
    float4 StyleA;         // x style (0 pbr, 1 toon, 2 flat), y toon bands, z toon softness, w toon highlight
    float4 StyleB;         // x outline width in pixels
    float4 OutlineColor;
};

[[vk::binding(1, 2)]] Texture2D<float4> BaseTex;
[[vk::binding(2, 2)]] Texture2D<float4> MrTex;
[[vk::binding(3, 2)]] Texture2D<float4> NormalTex;
[[vk::binding(4, 2)]] Texture2D<float4> OccTex;
[[vk::binding(5, 2)]] Texture2D<float4> EmisTex;
[[vk::binding(6, 2)]] SamplerState BaseSamp;
[[vk::binding(7, 2)]] SamplerState MrSamp;
[[vk::binding(8, 2)]] SamplerState NormalSamp;
[[vk::binding(9, 2)]] SamplerState OccSamp;
[[vk::binding(10, 2)]] SamplerState EmisSamp;

struct VIn
{
    [[vk::location(0)]] float3 Pos : POSITION;
    [[vk::location(1)]] float3 Normal : NORMAL;
    [[vk::location(2)]] float4 Tangent : TANGENT;
    [[vk::location(3)]] float2 Uv0 : TEXCOORD0;
    [[vk::location(4)]] float2 Uv1 : TEXCOORD1;
    [[vk::location(5)]] float4 Color : COLOR0;
    [[vk::location(6)]] uint2 Joints : BLENDINDICES;   // four 16-bit joint indices
    [[vk::location(7)]] float4 Weights : BLENDWEIGHT;
};

struct VOut
{
    float4 Pos : SV_Position;
    [[vk::location(0)]] float3 WorldPos : TEXCOORD0;
    [[vk::location(1)]] float3 Normal : TEXCOORD1;
    [[vk::location(2)]] float4 Tangent : TEXCOORD2;
    [[vk::location(3)]] float2 Uv0 : TEXCOORD3;
    [[vk::location(4)]] float2 Uv1 : TEXCOORD4;
    [[vk::location(5)]] float4 Color : TEXCOORD5;
};

float4x4 JointMatrix(uint index)
{
    uint b = (ObjInfo.x + index) * 4u;
    return float4x4(Joints[b], Joints[b + 1u], Joints[b + 2u], Joints[b + 3u]);
}

// Per-instance data: the first three columns of the instance matrix as rows of a 3x4 (x' = dot((p, 1), Rx)), and a colour.
struct InstIn
{
    [[vk::location(8)]] float4 Rx : INSTROW0;
    [[vk::location(9)]] float4 Ry : INSTROW1;
    [[vk::location(10)]] float4 Rz : INSTROW2;
    [[vk::location(11)]] float4 Tint : INSTCOLOR;
};

float3 Inst(float3 v, float w, InstIn n)
{
    float4 h = float4(v, w);
    return float3(dot(h, n.Rx), dot(h, n.Ry), dot(h, n.Rz));
}

InstIn NoInstance()
{
    InstIn n;
    n.Rx = float4(1.0, 0.0, 0.0, 0.0);
    n.Ry = float4(0.0, 1.0, 0.0, 0.0);
    n.Rz = float4(0.0, 0.0, 1.0, 0.0);
    n.Tint = float4(1.0, 1.0, 1.0, 1.0);
    return n;
}

VOut Transform(VIn i, bool instanced, InstIn inst)
{
    float3 p = i.Pos;
    float3 n = i.Normal;
    float3 t = i.Tangent.xyz;
    if (ObjInfo.y != 0u)
    {
        // Linear blend skinning. Joint matrices already include the inverse bind matrix and the mesh node's inverse.
        uint j0 = i.Joints.x & 0xFFFFu;
        uint j1 = i.Joints.x >> 16;
        uint j2 = i.Joints.y & 0xFFFFu;
        uint j3 = i.Joints.y >> 16;
        float4x4 s = JointMatrix(j0) * i.Weights.x + JointMatrix(j1) * i.Weights.y + JointMatrix(j2) * i.Weights.z + JointMatrix(j3) * i.Weights.w;
        p = mul(float4(p, 1.0), s).xyz;
        n = mul(float4(n, 0.0), s).xyz;
        t = mul(float4(t, 0.0), s).xyz;
    }

    float4 world = mul(float4(p, 1.0), Model);
    float3 wn = mul(float4(n, 0.0), NormalMat).xyz;
    float3 wt = mul(float4(t, 0.0), Model).xyz;
    float4 color = i.Color;
    if (instanced)
    {
        world = float4(Inst(world.xyz, 1.0, inst), 1.0);
        wn = Inst(wn, 0.0, inst);
        wt = Inst(wt, 0.0, inst);
        color *= inst.Tint;
    }

    VOut o;
    o.Pos = mul(world, ViewProj);
    o.WorldPos = world.xyz;
    o.Normal = wn;
    o.Tangent = float4(wt, i.Tangent.w);
    o.Uv0 = i.Uv0;
    o.Uv1 = i.Uv1;
    o.Color = color;
    return o;
}

[shader("vertex")]
VOut VSMain(VIn i)
{
    InstIn none = NoInstance();
    return Transform(i, false, none);
}

[shader("vertex")]
VOut VSInst(VIn i, InstIn inst)
{
    return Transform(i, true, inst);
}

// Outline: the same mesh pushed out along its normal by a fixed number of pixels, drawn back faces only (the "inverted hull").
VOut Outline(VOut o)
{
    float3 nw = normalize(o.Normal);
    float2 nclip = mul(float4(nw, 0.0), ViewProj).xy;
    float2 dir = nclip * ViewportInfo.xy;
    float len = length(dir);
    dir = len > 1e-6 ? dir / len : float2(0.0, 0.0);
    o.Pos.xy += dir * (StyleB.x * 2.0) * ViewportInfo.zw * o.Pos.w;
    return o;
}

[shader("vertex")]
VOut VSOutline(VIn i)
{
    InstIn none = NoInstance();
    return Outline(Transform(i, false, none));
}

[shader("vertex")]
VOut VSOutlineInst(VIn i, InstIn inst)
{
    return Outline(Transform(i, true, inst));
}

float2 TexUv(uint slot, VOut i)
{
    float4 r0 = Xf[slot * 2u];
    float4 r1 = Xf[slot * 2u + 1u];
    float2 uv = r0.w > 0.5 ? i.Uv1 : i.Uv0;
    float3 h = float3(uv, 1.0);
    return float2(dot(h, r0.xyz), dot(h, r1.xyz));
}

// ---- BRDF pieces (glTF 2.0 Appendix B) ----

float3 FresnelSchlick(float3 f0, float vdoth)
{
    float m = saturate(1.0 - vdoth);
    float m2 = m * m;
    return f0 + (1.0 - f0) * (m2 * m2 * m);
}

float DistributionGgx(float ndoth, float a2)
{
    float d = ndoth * ndoth * (a2 - 1.0) + 1.0;
    return a2 / (PI * d * d);
}

// Height-correlated Smith visibility, already divided by 4 n.l n.v.
float VisibilitySmith(float ndotl, float ndotv, float a2)
{
    float gv = ndotl * sqrt(ndotv * ndotv * (1.0 - a2) + a2);
    float gl = ndotv * sqrt(ndotl * ndotl * (1.0 - a2) + a2);
    float s = gv + gl;
    return s > 0.0 ? 0.5 / s : 0.0;
}

// KHR_lights_punctual: inverse-square falloff, windowed to zero at the range; spots fall off between the cone angles.
float Attenuation(LightData l, float3 toLight, float dist2)
{
    float att = 1.0 / max(dist2, 1e-8);
    float range = l.PosRange.w;
    if (range > 0.0)
    {
        float r = dist2 / (range * range);
        float w = saturate(1.0 - r * r);
        att *= w;
    }

    if (l.DirType.w > 1.5)
    {
        float cd = dot(l.DirType.xyz, -toLight);
        float a = saturate(cd * l.Spot.x + l.Spot.y);
        att *= a * a;
    }

    return att;
}

// ---- shadows ----

float4 ShadowClip(uint index, uint tile, float3 p)
{
    float4 h = float4(p, 1.0);
    uint b = tile * 4u;
    return float4(dot(h, Shadows[index].Col[b]), dot(h, Shadows[index].Col[b + 1u]), dot(h, Shadows[index].Col[b + 2u]), dot(h, Shadows[index].Col[b + 3u]));
}

// Fraction of light that reaches the surface point: percentage-closer filtering over one tile of the atlas. The point is first moved
// along the normal by a few texels, which keeps a lit surface from shadowing itself.
float ShadowTile(uint index, uint tile, float3 pos, float3 n, float texelWorld)
{
    float3 p = pos + n * (texelWorld * Shadows[index].P0.z);
    float4 c = ShadowClip(index, tile, p);
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
    float2 texel = Shadows[index].P1.xy;
    int radius = (int)Shadows[index].P1.z;
    float2 pad = texel * (float)(radius + 1);
    a = clamp(a, rect.xy + pad, rect.xy + rect.zw - pad);
    float depth = ndc.z - Shadows[index].P0.w;
    float sum = 0.0;
    float taps = 0.0;
    [loop] for (int y = -radius; y <= radius; y++)
    {
        [loop] for (int x = -radius; x <= radius; x++)
        {
            sum += ShadowAtlas.SampleCmpLevelZero(ShadowSamp, a + float2(x, y) * texel, depth);
            taps += 1.0;
        }
    }

    return sum / taps;
}

float ShadowTerm(uint index, LightData l, float3 pos, float3 n, float viewZ)
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

        float s = ShadowTile(index, tile, pos, n, Shadows[index].Texels[tile]);
        return lerp(1.0, s, saturate((last - viewZ) / (0.1 * last)));
    }

    float3 d = pos - l.PosRange.xyz;
    float texelWorld = Shadows[index].Texels.x * length(d);
    uint face = 0u;
    if (kind == 2u)
    {
        float3 a = abs(d);
        face = (a.x >= a.y && a.x >= a.z) ? (d.x > 0.0 ? 0u : 1u) : ((a.y >= a.z) ? (d.y > 0.0 ? 2u : 3u) : (d.z > 0.0 ? 4u : 5u));
    }

    return ShadowTile(index, face, pos, n, texelWorld);
}

float3 Shade(LightData l, float3 pos, float3 n, float3 v, float ndotv, float3 diffuseColor, float3 f0, float f0Dielectric, float a2, float viewZ)
{
    float3 toLight;
    float att;
    if (l.DirType.w < 0.5)
    {
        toLight = -l.DirType.xyz;
        att = 1.0;
    }
    else
    {
        float3 d = l.PosRange.xyz - pos;
        float dist2 = dot(d, d);
        toLight = d * rsqrt(max(dist2, 1e-8));
        att = Attenuation(l, toLight, dist2);
    }

    float ndotl = dot(n, toLight);
    if (ndotl <= 0.0 || att <= 0.0)
    {
        return 0.0;
    }

    if (l.Spot.w > 0.5)
    {
        att *= ShadowTerm((uint)(l.Spot.w - 0.5), l, pos, n, viewZ);
    }

    float3 h = normalize(toLight + v);
    float ndoth = saturate(dot(n, h));
    float vdoth = saturate(dot(v, h));
    float3 f = FresnelSchlick(f0, vdoth);
    float fd = FresnelSchlick(float3(f0Dielectric, f0Dielectric, f0Dielectric), vdoth).x;
    float3 spec = f * (DistributionGgx(ndoth, a2) * VisibilitySmith(ndotl, ndotv, a2));
    float3 diff = (1.0 - fd) * diffuseColor / PI;
    return (diff + spec) * l.Color.rgb * (att * ndotl);
}

float3 RotateEnv(float3 d)
{
    return float3(EnvA.z * d.x + EnvA.w * d.z, d.y, -EnvA.w * d.x + EnvA.z * d.z);
}

// ---- stylized lighting ----

// Splits a 0..1 light amount into flat steps with a short smooth edge between them.
float Band(float x, float bands, float soft)
{
    float q = saturate(x) * bands;
    float f = floor(q);
    float w = max(soft * 0.5, 1e-3);
    return (f + smoothstep(0.5 - w, 0.5 + w, q - f)) / bands;
}

float Incident(LightData l, float3 pos, out float3 toLight)
{
    if (l.DirType.w < 0.5)
    {
        toLight = -l.DirType.xyz;
        return 1.0;
    }

    float3 d = l.PosRange.xyz - pos;
    float dist2 = dot(d, d);
    toLight = d * rsqrt(max(dist2, 1e-8));
    return Attenuation(l, toLight, dist2);
}

float3 ToonLight(LightData l, float3 pos, float3 n, float3 v, float3 baseColor, float viewZ)
{
    float3 toLight;
    float att = Incident(l, pos, toLight);
    float ndotl = dot(n, toLight);
    if (att <= 0.0 || ndotl <= 0.0)
    {
        return 0.0;
    }

    if (l.Spot.w > 0.5)
    {
        att *= ShadowTerm((uint)(l.Spot.w - 0.5), l, pos, n, viewZ);
    }

    float lit = Band(ndotl, StyleA.y, StyleA.z);
    float3 h = normalize(toLight + v);
    float highlight = smoothstep(0.965, 0.975, dot(n, h)) * StyleA.w;
    return l.Color.rgb * (att / PI) * (baseColor * lit + highlight);
}

float AmbientOcclusion(float4 fragCoord)
{
    if (AoInfo.x <= 0.0)
    {
        return 1.0;
    }

    float ao = AoTex.SampleLevel(EnvSampler, fragCoord.xy * ViewportInfo.zw, 0).r;
    return lerp(1.0, ao, AoInfo.x);
}

bool AlphaMasked(float4 base)
{
    return P1.y > 0.5 && P1.y < 1.5 && base.a < P1.x;
}

[shader("pixel")]
float4 PSMain(VOut i, bool front : SV_IsFrontFace) : SV_Target
{
    // Every texture is sampled before anything can discard, so screen-space derivatives stay defined.
    float4 base = BaseColor * i.Color * BaseTex.Sample(BaseSamp, TexUv(0u, i));
    float4 mr = MrTex.Sample(MrSamp, TexUv(1u, i));
    float3 tn = NormalTex.Sample(NormalSamp, TexUv(2u, i)).xyz * 2.0 - 1.0;
    float occ = OccTex.Sample(OccSamp, TexUv(3u, i)).r;
    float3 emis = Emissive.rgb * EmisTex.Sample(EmisSamp, TexUv(4u, i)).rgb;
    float2 nuv = TexUv(2u, i);

    if (AlphaMasked(base))
    {
        discard;
    }

    float alpha = P1.y > 1.5 ? base.a : 1.0;
    if (P1.z > 0.5 || StyleA.x > 1.5)
    {
        // KHR_materials_unlit, or the flat style
        return float4(base.rgb, alpha);
    }

    float3 v = normalize(CameraPos.w > 0.5 ? View._m02_m12_m22 : CameraPos.xyz - i.WorldPos);
    float s = front ? 1.0 : -1.0;
    float3 n = normalize(i.Normal) * s;
    if (Flags.x != 0u)
    {
        float3 t;
        float3 b;
        if (dot(i.Tangent.xyz, i.Tangent.xyz) > 1e-10)
        {
            t = normalize(i.Tangent.xyz) * s;
            t = normalize(t - n * dot(n, t));
            b = cross(n, t) * i.Tangent.w;
        }
        else
        {
            // No tangents: build the frame from screen-space derivatives of position and uv.
            float3 dp1 = ddx(i.WorldPos);
            float3 dp2 = ddy(i.WorldPos);
            float2 du1 = ddx(nuv);
            float2 du2 = ddy(nuv);
            float3 dp2perp = cross(dp2, n);
            float3 dp1perp = cross(n, dp1);
            float3 tt = dp2perp * du1.x + dp1perp * du2.x;
            float3 bb = dp2perp * du1.y + dp1perp * du2.y;
            float inv = rsqrt(max(max(dot(tt, tt), dot(bb, bb)), 1e-20));
            t = tt * inv;
            b = -bb * inv;
        }

        float3 sn = float3(tn.xy * P0.z, tn.z);
        n = normalize(t * sn.x + b * sn.y + n * sn.z);
    }

    // Clustered punctual lights: the slice comes from view depth, the tile from the pixel position.
    float viewZ = max(-mul(float4(i.WorldPos, 1.0), View).z, 1e-4);
    uint tile = (uint)Grid.w;
    uint2 t2 = uint2(i.Pos.xy) / tile;
    float sl = floor(log2(viewZ) * Slice.x + Slice.y);
    uint slice = (uint)clamp(sl, 0.0, Grid.z - 1.0);
    uint cluster = (slice * (uint)Grid.y + min(t2.y, (uint)Grid.y - 1u)) * (uint)Grid.x + min(t2.x, (uint)Grid.x - 1u);
    uint at = cluster * Counts.z;
    uint count = Clusters[at];
    float aoScreen = AmbientOcclusion(i.Pos);

    if (StyleA.x > 0.5)
    {
        // Cel shading: stepped light, a hard highlight, and the environment's average light as the shaded side.
        float3 toon = 0.0;
        for (uint d = 0u; d < Counts.x; d++)
        {
            toon += ToonLight(Lights[d], i.WorldPos, n, v, base.rgb, viewZ);
        }

        for (uint k = 0u; k < count; k++)
        {
            toon += ToonLight(Lights[Clusters[at + 1u + k]], i.WorldPos, n, v, base.rgb, viewZ);
        }

        float3 ambient = Irradiance.SampleLevel(EnvSampler, RotateEnv(n), 0).rgb * base.rgb * (EnvA.y * aoScreen);
        return float4(toon + ambient + emis, alpha);
    }

    float metallic = saturate(P0.x * mr.b);
    float roughness = clamp(P0.y * mr.g, 0.045, 1.0);
    float a2 = pow(roughness, 4.0);   // alpha = roughness^2, squared
    float f0d = P1.w;
    float3 diffuseColor = base.rgb * (1.0 - metallic);
    float3 f0 = lerp(float3(f0d, f0d, f0d), base.rgb, metallic);
    float ndotv = max(dot(n, v), 1e-4);

    float3 color = 0.0;
    for (uint d = 0u; d < Counts.x; d++)
    {
        color += Shade(Lights[d], i.WorldPos, n, v, ndotv, diffuseColor, f0, f0d, a2, viewZ);
    }

    for (uint k = 0u; k < count; k++)
    {
        color += Shade(Lights[Clusters[at + 1u + k]], i.WorldPos, n, v, ndotv, diffuseColor, f0, f0d, a2, viewZ);
    }

    // Image-based lighting: split-sum specular and cosine-convolved diffuse. Occlusion only dims indirect light.
    float ao = (1.0 + P0.w * (occ - 1.0)) * aoScreen;
    float3 r = reflect(-v, n);
    float2 ab = BrdfLut.SampleLevel(EnvSampler, float2(ndotv, roughness), 0);
    float3 spec = Specular.SampleLevel(EnvSampler, RotateEnv(r), roughness * EnvA.x).rgb;
    float3 diff = Irradiance.SampleLevel(EnvSampler, RotateEnv(n), 0).rgb;
    // Fresnel with a roughness-limited grazing term (Lagarde and de Rousiers, "Moving Frostbite to PBR") decides how much light the diffuse layer keeps.
    float fdIbl = f0d + (max(1.0 - roughness, f0d) - f0d) * pow(1.0 - ndotv, 5.0);
    float3 ibl = (diff * diffuseColor * (1.0 - fdIbl) + spec * (f0 * ab.x + ab.y)) * (EnvA.y * ao);
    color += ibl;
    color += emis;
    return float4(color, alpha);
}

// ---- depth-only passes ----

// Shadow maps for alpha-tested materials: only the cut-out matters. Opaque materials use a pipeline with no pixel shader.
[shader("pixel")]
void PSShadowMask(VOut i)
{
    float4 base = BaseColor * i.Color * BaseTex.Sample(BaseSamp, TexUv(0u, i));
    if (AlphaMasked(base))
    {
        discard;
    }
}

// View-space normal and distance along the view axis, for ambient occlusion. A distance of 0 means nothing was drawn.
[shader("pixel")]
float4 PSPrepass(VOut i, bool front : SV_IsFrontFace) : SV_Target
{
    float4 base = BaseColor * i.Color * BaseTex.Sample(BaseSamp, TexUv(0u, i));
    if (AlphaMasked(base))
    {
        discard;
    }

    float3 n = normalize(i.Normal) * (front ? 1.0 : -1.0);
    float3 vn = normalize(mul(float4(n, 0.0), View).xyz);
    float dist = -mul(float4(i.WorldPos, 1.0), View).z;
    return float4(vn, dist);
}

[shader("pixel")]
float4 PSOutline(VOut i) : SV_Target
{
    float4 base = BaseColor * i.Color * BaseTex.Sample(BaseSamp, TexUv(0u, i));
    if (AlphaMasked(base))
    {
        discard;
    }

    return float4(OutlineColor.rgb, 1.0);
}

// ---- environment backdrop ----

struct SkyOut
{
    float4 Pos : SV_Position;
    [[vk::location(0)]] float2 Ndc : TEXCOORD0;
};

[shader("vertex")]
SkyOut VSSky(uint id : SV_VertexID)
{
    float2 p = float2((id << 1) & 2, id & 2);
    SkyOut o;
    o.Ndc = p * 2.0 - 1.0;
    o.Pos = float4(o.Ndc, 1.0, 1.0);
    return o;
}

[shader("pixel")]
float4 PSSky(SkyOut i) : SV_Target
{
    float4 w = mul(float4(i.Ndc, 1.0, 1.0), InvViewProj);
    float3 dir = normalize(w.xyz / w.w - CameraPos.xyz);
    float3 c = Specular.SampleLevel(EnvSampler, RotateEnv(dir), Sky.x).rgb * Sky.y;
    return float4(c, 1.0);
}
