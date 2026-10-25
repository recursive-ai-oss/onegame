namespace OneGame.Control;

public static partial class ControlOps
{
    private static ControlOp[] AudioEventOps =>
    [
        new("audio_events",
            "The sounds the game asked the audio service to play, as JSON: lastSequence, the current frame and events [{sequence, frame, sound, bus, volume, loop, music, started}] oldest first (the last 4096), where sound is the name the clip was loaded under. since=N returns only events after sequence N; clear=true empties the log afterwards. Works with or without a sound device. Fails with 'no_audio' when the game runs no audio service.",
            """{"type":"object","properties":{"since":{"type":"integer","minimum":0},"clear":{"type":"boolean"}}}""", false),
    ];
}
