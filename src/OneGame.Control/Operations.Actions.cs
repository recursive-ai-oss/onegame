namespace OneGame.Control;

public static partial class ControlOps
{
    private static ControlOp[] ActionOps =>
    [
        new("actions",
            "The game's input actions (GameInstance.Actions) as JSON: every context (active or not, the screens that switch it on) and every action with its kind, held/pressed/released this frame, value (x, y), and current bindings. " +
            "With 'name' it also drives the action as the harness source: state tap (default; a button is pressed for exactly one frame), down (held until released), up; for axes and vectors give x (and y, down is positive) and the value is held until state=up. " +
            "'contexts' switches exactly those contexts on. 'reset' puts every binding back to its default. The change shows after the next frame. Fails with 'no_actions' when the game declares none and 'unknown_action' for a name it does not have.",
            """{"type":"object","properties":{"name":{"type":"string"},"state":{"type":"string","enum":["tap","down","up"]},"x":{"type":"number"},"y":{"type":"number"},"contexts":{"type":"array","items":{"type":"string"}},"reset":{"type":"boolean"}}}""", true),
        new("input_pad",
            "Drive the harness's virtual gamepad (the one input_nav uses) with held state. button: a name input_nav takes (south, east, west, north, left_shoulder, dpad_up, start, ...) with state press (default: down then up), down or up. " +
            "axis: leftx, lefty, rightx, righty (down and right are positive), lefttrigger or righttrigger (0..1) with value; the value is final (no dead zone) and stays until changed.",
            """{"type":"object","properties":{"button":{"type":"string"},"state":{"type":"string","enum":["press","down","up"]},"axis":{"type":"string","enum":["leftx","lefty","rightx","righty","lefttrigger","righttrigger"]},"value":{"type":"number","minimum":-1,"maximum":1}}}""", true),
    ];
}
