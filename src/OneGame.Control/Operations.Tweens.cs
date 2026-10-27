namespace OneGame.Control;

public static partial class ControlOps
{
    private static ControlOp[] TweenOps =>
    [
        new("tweens",
            "List the running tweens and move view time, as JSON: {time, paused, timeScale, active, tweens [{id, kind, name, label, elapsed, duration, progress, paused, children}]}. Tweens are view-only (UI style transitions such as 'transition: opacity 150ms' and the ones game code starts): they never change simulation state or the state hash. duration is -1 for a tween that repeats forever; label names the element and property of a style transition. advance=N moves view time forward N seconds first (UI transitions included) without running ticks, so a capture can be taken at exact points of an animation; settle=true advances until every finite tween has finished. cancel_all=true stops every tween where it is. paused and time_scale set the clock (paused freezes all tweens; time_scale 0.5 is slow motion). A running game's own frames advance view time as well; a stepped run advances it by 1/60 s per tick.",
            """{"type":"object","properties":{"advance":{"type":"number","minimum":0,"maximum":600},"settle":{"type":"boolean"},"cancel_all":{"type":"boolean"},"paused":{"type":"boolean"},"time_scale":{"type":"number","minimum":0,"maximum":100}}}""", false),
    ];
}
