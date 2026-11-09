namespace OneGame.Control;

/// <summary>Result of <c>quality</c>: the level of the 3D renderer and what it means, with what the last frame did.</summary>
/// <param name="Level">Current level: low, medium, high or ultra.</param>
/// <param name="Levels">All level names.</param>
/// <param name="ShadowTileSize">Shadow map tile edge in texels.</param>
/// <param name="MaxCascades">Most cascades per directional light.</param>
/// <param name="ShadowFilterRadius">Shadow filter radius in texels.</param>
/// <param name="MaxSamples">Most multisampling samples.</param>
/// <param name="Bloom">Whether bloom may run at this level.</param>
/// <param name="EnvironmentSize">Specular cube face size of environments that follow the level.</param>
/// <param name="SsaoAllowed">Whether this level allows ambient occlusion.</param>
/// <param name="SsaoSamples">Occlusion samples per pixel.</param>
/// <param name="SsaoEnabled">Whether the scene asks for ambient occlusion.</param>
/// <param name="ShadowTiles">Shadow map tiles the last frame drew.</param>
/// <param name="ShadowLights">Lights that cast shadows in the last frame.</param>
/// <param name="Instances">Instanced copies in the last frame.</param>
/// <param name="InstancesDrawn">Instanced copies that survived culling in the last frame.</param>
/// <param name="Passes">Names of the passes the last frame ran, in order (the same list <c>render_graph</c> reports).</param>
public sealed record QualityResult(
    string Level, string[] Levels, int ShadowTileSize, int MaxCascades, int ShadowFilterRadius, int MaxSamples, bool Bloom, int EnvironmentSize,
    bool SsaoAllowed, int SsaoSamples, bool SsaoEnabled, int ShadowTiles, int ShadowLights, int Instances, int InstancesDrawn, string[] Passes);

/// <summary>Result of <c>environment</c>: the scene's distance fog and environment crossfade.</summary>
/// <param name="FogEnabled">Whether fog is applied.</param>
/// <param name="FogColor">Linear fog colour (r, g, b).</param>
/// <param name="FogStart">Distance where fog begins.</param>
/// <param name="FogEnd">Distance where fog reaches its maximum.</param>
/// <param name="FogMaxDensity">Largest share of the fog colour a surface shows.</param>
/// <param name="HasNextMap">Whether the scene has a second environment to fade to.</param>
/// <param name="Blend">Crossfade weight, 0 (first map) to 1 (second map).</param>
/// <param name="Intensity">Image-based light scale.</param>
/// <param name="Yaw">Environment turn about +Y in radians.</param>
public sealed record EnvironmentResult(bool FogEnabled, double[] FogColor, double FogStart, double FogEnd, double FogMaxDensity, bool HasNextMap, double Blend, double Intensity, double Yaw);

public static partial class ControlOps
{
    private static ControlOp[] QualityOps =>
    [
        new("environment",
            "Read or set the distance fog and the environment crossfade of a game that draws a 3D scene, to tune them live. fog true or false turns fog on or off; fogColor is linear [r,g,b]; fogStart and fogEnd are distances from the camera (fogEnd above fogStart); fogMaxDensity is 0..1. blend (0..1) moves the picture from the scene's first environment map to its second (it has no effect while the game set no second map); intensity and yaw (radians) scale and turn the environment light. Changes apply from the next frame and last until the game sets them again. Returns the current values.",
            """{"type":"object","properties":{"fog":{"type":"boolean"},"fogColor":{"type":"array","items":{"type":"number","minimum":0},"minItems":3,"maxItems":3},"fogStart":{"type":"number","minimum":0},"fogEnd":{"type":"number","exclusiveMinimum":0},"fogMaxDensity":{"type":"number","minimum":0,"maximum":1},"blend":{"type":"number","minimum":0,"maximum":1},"intensity":{"type":"number","minimum":0},"yaw":{"type":"number"}}}""", true),
        new("quality",
            "Read or set the quality level of a game that draws a 3D scene. level is low, medium, high or ultra: it bounds shadow map size and cascades, ambient occlusion, multisampling, bloom and environment size, and applies from the next frame. ssao true or false asks the scene for screen-space ambient occlusion (a level may still disallow it). Returns the level's settings, what the last frame did (shadow tiles, instances drawn) and the passes it ran.",
            """{"type":"object","properties":{"level":{"type":"string","enum":["low","medium","high","ultra"]},"ssao":{"type":"boolean"},"ssaoRadius":{"type":"number","exclusiveMinimum":0}}}""", true),
    ];
}
