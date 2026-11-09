namespace OneGame.Control;

public static partial class ControlOps
{
    private static ControlOp[] WindowOps =>
    [
        new("window",
            "Read or set the game window's placement. With no arguments it returns the placement: x, y, width, height (the normal, restored client rectangle in screen pixels; x and y are absent where the window system places windows itself, as on Wayland), state (normal, maximized or fullscreen), the current client size, canPlace, and the screens' workAreas. Setting any of x, y, width, height or state applies the changed fields to the current placement (a position that no screen would show is moved onto one) and returns the result after the platform has handled it. text is the same placement as a settings string, e.g. '100,80,1280x720,maximized'; pass it back as text to restore. Headless runs keep the placement in memory.",
            """{"type":"object","properties":{"x":{"type":"integer"},"y":{"type":"integer"},"width":{"type":"integer","minimum":1},"height":{"type":"integer","minimum":1},"state":{"type":"string","enum":["normal","maximized","fullscreen"]},"text":{"type":"string"}}}""", false),
    ];
}
