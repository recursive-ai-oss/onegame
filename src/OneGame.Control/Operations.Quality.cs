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

/// <summary>Result of <c>render_scale</c>: the share of the window the 3D scene is drawn at.</summary>
/// <param name="Scale">The scale in effect, 0.25 to 1.</param>
/// <param name="RenderWidth">Width in pixels of the image the last frame drew the scene at (0 before the first frame).</param>
/// <param name="RenderHeight">Height in pixels of that image.</param>
public sealed record RenderScaleResult(double Scale, int RenderWidth, int RenderHeight);

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
public sealed record EnvironmentResult(bool FogEnabled, double[] FogColor, double FogStart, double FogEnd, double FogMaxDensity, bool HasNextMap, double Blend, double Intensity, double Yaw)
{
    /// <summary>The newest asynchronous environment load the stage was given; null when there was none.</summary>
    public EnvironmentLoadResult? Load { get; init; }
}

/// <summary>Progress of an asynchronous environment load started with the <c>environment</c> operation's <c>load</c> or <c>sky</c> argument.</summary>
/// <param name="State">converting, uploading, filtering, ready, failed or canceled. Ready means the map has been (or is about to be) swapped in.</param>
/// <param name="Progress">0 to 1.</param>
/// <param name="Slot">map or next: the scene environment the result replaces.</param>
/// <param name="Error">Why a failed load failed.</param>
public sealed record EnvironmentLoadResult(string State, double Progress, string Slot, string? Error);

/// <summary>One light as <c>volumetric</c> reports it.</summary>
/// <param name="Index">Position in the scene's light list.</param>
/// <param name="Kind">directional, point or spot.</param>
/// <param name="Volumetric">Whether the light opts in to light shafts.</param>
/// <param name="Intensity">The light's own shaft multiplier.</param>
/// <param name="CastShadows">Whether the light has shadow maps (shafts are broken up by them).</param>
public sealed record VolumetricLightInfo(int Index, string Kind, bool Volumetric, double Intensity, bool CastShadows);

/// <summary>Result of <c>volumetric</c>: the scene's light shaft settings and what the quality level and the last frame did with them.</summary>
/// <param name="Enabled">Whether the scene asks for light shafts.</param>
/// <param name="Density">Scattering per world unit.</param>
/// <param name="Intensity">Scene-wide multiplier on the scattered light.</param>
/// <param name="Anisotropy">Henyey-Greenstein g, -0.95 to 0.95.</param>
/// <param name="MaxDistance">Longest marched stretch of a view ray.</param>
/// <param name="LinkToFog">Whether shafts fade with the scene's fog.</param>
/// <param name="Allowed">Whether the current quality level allows shafts (Low does not).</param>
/// <param name="Steps">Samples per ray at the current level.</param>
/// <param name="Scale">Size of the marched image as a share of the frame at the current level.</param>
/// <param name="Active">Whether the last frame ran the shaft passes.</param>
/// <param name="Lights">Every light of the scene.</param>
public sealed record VolumetricResult(
    bool Enabled, double Density, double Intensity, double Anisotropy, double MaxDistance, bool LinkToFog, bool Allowed, int Steps, double Scale, bool Active, VolumetricLightInfo[] Lights);

public static partial class ControlOps
{
    private static ControlOp[] QualityOps =>
    [
        new("environment",
            "Read or set the distance fog and the environment crossfade of a game that draws a 3D scene, to tune them live. fog true or false turns fog on or off; fogColor is linear [r,g,b]; fogStart and fogEnd are distances from the camera (fogEnd above fogStart); fogMaxDensity is 0..1. blend (0..1) moves the picture from the scene's first environment map to its second (it has no effect while the game set no second map); intensity and yaw (radians) scale and turn the environment light. Changes apply from the next frame and last until the game sets them again. load (a Radiance .hdr file in the output folder) or sky (a procedural panorama of that pixel width) starts an asynchronous environment load of face size size (0 follows quality): the game keeps drawing its current map, the filtering is spread over frames, and when it is ready the result replaces the scene map named by into (next, the default, to fade to with blend; or map). cancel true stops a running load. Returns the current values, with the latest load's state and progress.",
            """{"type":"object","properties":{"fog":{"type":"boolean"},"fogColor":{"type":"array","items":{"type":"number","minimum":0},"minItems":3,"maxItems":3},"fogStart":{"type":"number","minimum":0},"fogEnd":{"type":"number","exclusiveMinimum":0},"fogMaxDensity":{"type":"number","minimum":0,"maximum":1},"blend":{"type":"number","minimum":0,"maximum":1},"intensity":{"type":"number","minimum":0},"yaw":{"type":"number"},"load":{"type":"string"},"sky":{"type":"integer","minimum":64,"maximum":8192},"size":{"type":"integer","minimum":0,"maximum":1024},"into":{"enum":["next","map"]},"cancel":{"type":"boolean"}}}""", true),
        new("render_scale",
            "Read or set the share of the window a game's 3D scene is drawn at, the cheapest lever on frame time. scale is 0.25 to 1 (1 is full size): the 3D frame (shadows, lighting, bloom, tone mapping) runs on a smaller image that is stretched over the window before the 2D and UI passes, so the UI stays sharp; cost follows the pixel count. Picking, projection and captures keep using window pixels. Applies from the next frame. Returns the scale and the pixel size the last frame drew the scene at.",
            """{"type":"object","properties":{"scale":{"type":"number","minimum":0.25,"maximum":1}}}""", true),
        new("volumetric",
            "Read or set the light shafts (volumetric light) of a game that draws a 3D scene. enabled turns them on for the scene; density is scattering per world unit (about 0.01 to 0.1); intensity multiplies the scattered light; anisotropy (-0.95..0.95) favours scattering toward the light's direction of travel when positive; maxDistance is the longest marched stretch of a view ray (keep it at or below the shadow distance); linkToFog fades shafts with the scene's fog. light picks a light by its index in the scene's list: lightEnabled opts it in or out and lightIntensity sets its own multiplier. The quality level bounds the cost and Low has no shafts. Changes apply from the next frame; a game that sets these itself in UpdateScene overwrites them every frame. Returns the settings, whether the level allows shafts, whether the last frame ran them, and every light.",
            """{"type":"object","properties":{"enabled":{"type":"boolean"},"density":{"type":"number","minimum":0},"intensity":{"type":"number","minimum":0},"anisotropy":{"type":"number","minimum":-0.95,"maximum":0.95},"maxDistance":{"type":"number","exclusiveMinimum":0},"linkToFog":{"type":"boolean"},"light":{"type":"integer","minimum":0},"lightEnabled":{"type":"boolean"},"lightIntensity":{"type":"number","minimum":0}}}""", true),
        new("quality",
            "Read or set the quality level of a game that draws a 3D scene. level is low, medium, high or ultra: it bounds shadow map size and cascades, ambient occlusion, multisampling, bloom and environment size, and applies from the next frame. ssao true or false asks the scene for screen-space ambient occlusion (a level may still disallow it). Returns the level's settings, what the last frame did (shadow tiles, instances drawn) and the passes it ran.",
            """{"type":"object","properties":{"level":{"type":"string","enum":["low","medium","high","ultra"]},"ssao":{"type":"boolean"},"ssaoRadius":{"type":"number","exclusiveMinimum":0}}}""", true),
    ];
}
