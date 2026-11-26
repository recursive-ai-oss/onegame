namespace OneGame.Control;

public static partial class ControlOps
{
    private static ControlOp[] GestureOps =>
    [
        new("system_gestures",
            "Read or set the regions of the window the game claims from the system's own gestures (the back swipe from a screen edge on Android). With no arguments it returns the claim. regions is a list of 'x,y,w,h' rectangles in window pixels and replaces the claim; clear releases it. The result also has the edges the claim touches and limit, the most height per edge the system honours (0 = no limit, as on desktop).",
            """{"type":"object","properties":{"regions":{"type":"array","items":{"type":"string"}},"clear":{"type":"boolean"}}}""", false),
        new("input_back", "Inject the system's back button or gesture (a BackRequested event). The game's OnBack sees it first; unhandled, the UI gets a Cancel and, with nothing to close, the app asks to quit.",
            """{"type":"object","properties":{}}""", true),
    ];
}
