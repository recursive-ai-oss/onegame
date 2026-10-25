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

public static partial class ControlOps
{
    private static ControlOp[] QualityOps =>
    [
        new("quality",
            "Read or set the quality level of a game that draws a 3D scene. level is low, medium, high or ultra: it bounds shadow map size and cascades, ambient occlusion, multisampling, bloom and environment size, and applies from the next frame. ssao true or false asks the scene for screen-space ambient occlusion (a level may still disallow it). Returns the level's settings, what the last frame did (shadow tiles, instances drawn) and the passes it ran.",
            """{"type":"object","properties":{"level":{"type":"string","enum":["low","medium","high","ultra"]},"ssao":{"type":"boolean"},"ssaoRadius":{"type":"number","exclusiveMinimum":0}}}""", true),
    ];
}
