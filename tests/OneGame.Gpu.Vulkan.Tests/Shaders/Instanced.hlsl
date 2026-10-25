// One quad per instance on a 100 x 100 grid; each instance covers exactly one pixel of a 100 x 100 target.

struct VsIn
{
    [[vk::location(0)]] float2 Corner : POSITION;
    [[vk::location(1)]] float2 Cell : TEXCOORD0;
    [[vk::location(2)]] [[oneshader::format("unorm8x4")]] float4 Color : COLOR0;
};

struct VsOut
{
    float4 Pos : SV_Position;
    [[vk::location(0)]] float4 Color : TEXCOORD0;
};

[shader("vertex")]
VsOut VSMain(VsIn i)
{
    VsOut o;
    o.Pos = float4((i.Cell + i.Corner) / 100.0 * 2.0 - 1.0, 0.0, 1.0);
    o.Color = i.Color;
    return o;
}

[shader("pixel")]
float4 PSMain(VsOut i) : SV_Target
{
    return i.Color;
}
