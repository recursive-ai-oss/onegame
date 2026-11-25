namespace OneGame.Control;

public static partial class ControlOps
{
    private static ControlOp[] HapticOps =>
    [
        new("haptics",
            "The touch feedback the game asked for (Haptics.Tick, Buzz, Play), as JSON: lastSequence, the current frame, enabled and events [{sequence, frame, kind, delivered}] oldest first (the last 1024); kind is Tick, Click, Buzz, Success, Warning or Error and delivered says the phone's system took it (false on desktop and the web). since=N returns only events after sequence N; clear=true empties the log afterwards. play=Tick (or another kind) makes the request first, so a device or emulator run can feel it. Works on every platform.",
            """{"type":"object","properties":{"since":{"type":"integer","minimum":0},"clear":{"type":"boolean"},"play":{"type":"string","enum":["Tick","Click","Buzz","Success","Warning","Error"]}}}""", false),
    ];
}
