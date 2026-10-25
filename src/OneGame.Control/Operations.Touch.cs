namespace OneGame.Control;

public static partial class ControlOps
{
    private static ControlOp[] TouchOps =>
    [
        new("input_touch",
            "Inject touch input through the platform event queue, as a finger would. action: down, move, up, cancel, tap (down then up). id is the finger (default 0); several fingers can be down at once. Positions are window pixels. The first finger also drives the pointer (move, left button), the way a real touch screen does.",
            """{"type":"object","properties":{"action":{"type":"string","enum":["down","move","up","cancel","tap"]},"id":{"type":"integer","minimum":0,"maximum":31},"x":{"type":"number"},"y":{"type":"number"}},"required":["action"]}""", true),
        new("ui_tap",
            "Tap the center of a UI element with a finger (touch down and up), by id or selector (first match). Like ui_click, but the input is touch.",
            """{"type":"object","properties":{"id":{"type":"string"},"selector":{"type":"string"}}}""", true),
        new("app_lifecycle",
            "Tell the game the app went to the background (paused), came back (resumed) or is short of memory (low_memory), as a phone does. A paused game stops simulating in a realtime run; state is kept.",
            """{"type":"object","properties":{"state":{"type":"string","enum":["paused","resumed","low_memory"]}},"required":["state"]}""", true),
        new("platform_native",
            "A command only the running platform backend understands. On iOS: describe (window size, scale, safe area, orientation, application state, whether the keyboard is up); orientation with text = portrait, landscape-left, landscape-right, landscape or all (asks the system to rotate the scene); text_insert / text_mark with text, text_unmark, text_delete, text_probe (drive the iOS text input object the way its keyboard does, so committed and marked text take the real path).",
            """{"type":"object","properties":{"action":{"type":"string"},"text":{"type":"string"}},"required":["action"]}""", true),
    ];
}
