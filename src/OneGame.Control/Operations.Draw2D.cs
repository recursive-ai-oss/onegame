namespace OneGame.Control;

/// <summary>Result of <c>draw2d</c>: what the 2D happy path drew in the last frame and where its camera looks.</summary>
/// <param name="Stage">The render stage's name.</param>
/// <param name="Sprites">Sprites and shapes queued by the last drawn frame.</param>
/// <param name="Texts">Text lines queued by the last drawn frame.</param>
/// <param name="Tick">The tick the last drawn frame showed.</param>
/// <param name="Time">The view time of that frame in seconds.</param>
/// <param name="Width">Target width of that frame.</param>
/// <param name="Height">Target height of that frame.</param>
/// <param name="Position">The camera's world position [x, y].</param>
/// <param name="Zoom">Physical pixels per world unit.</param>
/// <param name="Rotation">Camera rotation in radians.</param>
/// <param name="World">The world point under the <c>screen</c> pixel that was asked about, or null.</param>
/// <param name="Screen">The pixel of the <c>world</c> point that was asked about, or null.</param>
public sealed record Draw2DReport(string Stage, int Sprites, int Texts, long Tick, double Time, int Width, int Height, double[] Position, double Zoom, double Rotation, double[]? World, double[]? Screen);

public static partial class ControlOps
{
    private static ControlOp[] Draw2DOps =>
    [
        new("draw_view",
            "What the 2D happy path (entities implementing IView2D, drawing through Draw2D) showed in the last drawn frame: sprites and shapes queued, text lines, the tick and view time, and the camera (position, zoom, rotation). screen [x,y] answers which world point is under that pixel; world [x,y] answers which pixel a world point lands on. Fails with 'no_draw2d' when the game's stage is not the 2D renderer's.",
            """{"type":"object","properties":{"screen":{"type":"array","items":{"type":"number"},"minItems":2,"maxItems":2},"world":{"type":"array","items":{"type":"number"},"minItems":2,"maxItems":2}}}""", false),
    ];
}
