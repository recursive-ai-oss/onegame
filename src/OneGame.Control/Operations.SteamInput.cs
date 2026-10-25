namespace OneGame.Control;

public static partial class ControlOps
{
    private static ControlOp[] SteamInputOps =>
    [
        new("steam_input",
            "Steam Input as the game sees it, as JSON: whether it is enabled (or why not), the declared action sets and the screens they serve, the active set, the connected controllers (index, type, the set Steam says is active on each), the latest value of every action per controller, the on-screen keyboard (visible, the text field's rectangle, how often it was shown) and declared actions Steam does not know. The remaining arguments drive the harness's stand-in controller and fail with 'not_fake' when real Steam is behind the service: connect (input type number, default 3 = Xbox One) plugs in a controller; disconnect=INDEX unplugs; press=ACTION or release=ACTION sets a button action; analog=ACTION with x and y (up is positive) sets an analog one; controller (default 1) says which. Changes show in the game after the next frame. Fails with 'no_steam_input' when the game never started the service.",
            """{"type":"object","properties":{"connect":{"type":"integer","minimum":0,"maximum":255},"disconnect":{"type":"integer","minimum":1},"press":{"type":"string"},"release":{"type":"string"},"analog":{"type":"string"},"x":{"type":"number"},"y":{"type":"number"},"controller":{"type":"integer","minimum":1}}}""", true),
    ];
}
