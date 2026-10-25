// Bloom and tone mapping.
//   set 0: binding 0 parameters, binding 1 colour, binding 2 bloom (tone mapping only), binding 3 sampler

[[vk::binding(0, 0)]] cbuffer Params
{
    float4 P;   // down: x,y source texel size, z 1 on the first level. up: x,y source texel size. tone map: x exposure, y bloom strength, z operator, w 1 to encode sRGB
};
[[vk::binding(1, 0)]] Texture2D<float4> Color;
[[vk::binding(2, 0)]] Texture2D<float4> Bloom;
[[vk::binding(3, 0)]] SamplerState Samp;

struct FullOut
{
    float4 Pos : SV_Position;
    [[vk::location(0)]] float2 Uv : TEXCOORD0;
};

[shader("vertex")]
FullOut VSFull(uint id : SV_VertexID)
{
    float2 p = float2((id << 1) & 2, id & 2);
    FullOut o;
    o.Pos = float4(p * 2.0 - 1.0, 0.0, 1.0);
    // Clip +Y is up and the texture origin is the top-left corner.
    o.Uv = float2(p.x, 1.0 - p.y);
    return o;
}

float3 Tap(float2 uv, float2 o)
{
    return Color.SampleLevel(Samp, uv + o * P.xy, 0).rgb;
}

float Karis(float3 c)
{
    float l = dot(c, float3(0.2126, 0.7152, 0.0722));
    return 1.0 / (1.0 + l);
}

// Thirteen taps over a 4x4 texel footprint (Jimenez, "Next Generation Post Processing in Call of Duty: Advanced Warfare").
// The first level weights each group by 1 / (1 + luma) so single very bright pixels do not flicker.
[shader("pixel")]
float4 PSDown(FullOut i) : SV_Target
{
    float3 a = Tap(i.Uv, float2(-2, -2));
    float3 b = Tap(i.Uv, float2(0, -2));
    float3 c = Tap(i.Uv, float2(2, -2));
    float3 d = Tap(i.Uv, float2(-2, 0));
    float3 e = Tap(i.Uv, float2(0, 0));
    float3 f = Tap(i.Uv, float2(2, 0));
    float3 g = Tap(i.Uv, float2(-2, 2));
    float3 h = Tap(i.Uv, float2(0, 2));
    float3 k = Tap(i.Uv, float2(2, 2));
    float3 j1 = Tap(i.Uv, float2(-1, -1));
    float3 j2 = Tap(i.Uv, float2(1, -1));
    float3 j3 = Tap(i.Uv, float2(-1, 1));
    float3 j4 = Tap(i.Uv, float2(1, 1));
    float3 r;
    if (P.z > 0.5)
    {
        float3 g0 = (a + b + d + e) * 0.25;
        float3 g1 = (b + c + e + f) * 0.25;
        float3 g2 = (d + e + g + h) * 0.25;
        float3 g3 = (e + f + h + k) * 0.25;
        float3 g4 = (j1 + j2 + j3 + j4) * 0.25;
        float w0 = Karis(g0) * 0.125;
        float w1 = Karis(g1) * 0.125;
        float w2 = Karis(g2) * 0.125;
        float w3 = Karis(g3) * 0.125;
        float w4 = Karis(g4) * 0.5;
        r = (g0 * w0 + g1 * w1 + g2 * w2 + g3 * w3 + g4 * w4) / (w0 + w1 + w2 + w3 + w4);
    }
    else
    {
        r = e * 0.125 + (a + c + g + k) * 0.03125 + (b + d + f + h) * 0.0625 + (j1 + j2 + j3 + j4) * 0.125;
    }

    return float4(r, 1.0);
}

// 3x3 tent filter; the pass adds its result onto the next larger level.
[shader("pixel")]
float4 PSUp(FullOut i) : SV_Target
{
    float3 r = Tap(i.Uv, float2(0, 0)) * 4.0;
    r += (Tap(i.Uv, float2(-1, 0)) + Tap(i.Uv, float2(1, 0)) + Tap(i.Uv, float2(0, -1)) + Tap(i.Uv, float2(0, 1))) * 2.0;
    r += Tap(i.Uv, float2(-1, -1)) + Tap(i.Uv, float2(1, -1)) + Tap(i.Uv, float2(-1, 1)) + Tap(i.Uv, float2(1, 1));
    return float4(r / 16.0, 1.0);
}

// Khronos PBR Neutral tone mapper, as specified in the Khronos ToneMapping repository (PBR_Neutral).
float3 PbrNeutral(float3 c)
{
    const float f90 = 0.04;
    const float ks = 0.8 - f90;
    const float kd = 0.15;
    float x = min(c.r, min(c.g, c.b));
    float f = x <= 2.0 * f90 ? x - x * x / (4.0 * f90) : f90;
    float p = max(c.r - f, max(c.g - f, c.b - f));
    if (p <= ks)
    {
        return c - f;
    }

    float pn = 1.0 - (1.0 - ks) * (1.0 - ks) / (p + 1.0 - 2.0 * ks);
    float g = 1.0 / (kd * (p - pn) + 1.0);
    return (c - f) * (pn / p) * g + pn * (1.0 - g);
}

// ACES filmic curve, rational fit by Narkowicz (2016), applied per channel.
float3 AcesFilmic(float3 c)
{
    c *= 0.6;
    return saturate((c * (2.51 * c + 0.03)) / (c * (2.43 * c + 0.59) + 0.14));
}

float3 LinearToSrgb(float3 c)
{
    c = saturate(c);
    return lerp(c * 12.92, 1.055 * pow(c, 1.0 / 2.4) - 0.055, step(0.0031308, c));
}

[shader("pixel")]
float4 PSTonemap(FullOut i) : SV_Target
{
    float3 c = Color.SampleLevel(Samp, i.Uv, 0).rgb;
    c += Bloom.SampleLevel(Samp, i.Uv, 0).rgb * P.y;
    c *= P.x;
    if (P.z > 1.5)
    {
        c = saturate(c);
    }
    else if (P.z > 0.5)
    {
        c = AcesFilmic(c);
    }
    else
    {
        c = PbrNeutral(c);
    }

    if (P.w > 0.5)
    {
        c = LinearToSrgb(c);
    }

    return float4(c, 1.0);
}
