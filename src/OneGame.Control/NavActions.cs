using OneGame.Platform;

namespace OneGame.Control;

/// <summary>
/// The names the <c>input_nav</c> operation (and the scenario <c>nav</c> step) accepts for a virtual gamepad button: the menu actions
/// (<c>activate</c>, <c>cancel</c>...) and every <see cref="GamepadButton"/> by name. Case, <c>_</c>, <c>-</c> and spaces are ignored.
/// </summary>
public static class NavActions
{
    private static readonly (string Name, GamepadButton Button)[] Canonical =
    [
        ("up", GamepadButton.DPadUp),
        ("down", GamepadButton.DPadDown),
        ("left", GamepadButton.DPadLeft),
        ("right", GamepadButton.DPadRight),
        ("next", GamepadButton.RightShoulder),
        ("previous", GamepadButton.LeftShoulder),
        ("activate", GamepadButton.South),
        ("cancel", GamepadButton.East),
        ("south", GamepadButton.South),
        ("east", GamepadButton.East),
        ("west", GamepadButton.West),
        ("north", GamepadButton.North),
        ("left_shoulder", GamepadButton.LeftShoulder),
        ("right_shoulder", GamepadButton.RightShoulder),
        ("back", GamepadButton.Back),
        ("start", GamepadButton.Start),
        ("guide", GamepadButton.Guide),
        ("left_stick", GamepadButton.LeftStick),
        ("right_stick", GamepadButton.RightStick),
        ("dpad_up", GamepadButton.DPadUp),
        ("dpad_down", GamepadButton.DPadDown),
        ("dpad_left", GamepadButton.DPadLeft),
        ("dpad_right", GamepadButton.DPadRight),
        ("misc1", GamepadButton.Misc1),
        ("touchpad", GamepadButton.Touchpad),
        ("paddle1", GamepadButton.Paddle1),
        ("paddle2", GamepadButton.Paddle2),
        ("paddle3", GamepadButton.Paddle3),
        ("paddle4", GamepadButton.Paddle4),
    ];

    // Face buttons by their Xbox letters, and the other spellings people reach for.
    private static readonly (string Name, GamepadButton Button)[] Aliases =
    [
        ("a", GamepadButton.South),
        ("b", GamepadButton.East),
        ("x", GamepadButton.West),
        ("y", GamepadButton.North),
        ("lb", GamepadButton.LeftShoulder),
        ("rb", GamepadButton.RightShoulder),
        ("select", GamepadButton.Back),
        ("menu", GamepadButton.Start),
        ("l3", GamepadButton.LeftStick),
        ("r3", GamepadButton.RightStick),
    ];

    /// <summary>The names listed in the operation's schema, in a stable order.</summary>
    public static IReadOnlyList<string> Names { get; } = [.. Canonical.Select(c => c.Name)];

    /// <summary>Finds the button a name stands for.</summary>
    /// <param name="name">An action or button name.</param>
    /// <param name="button">The button.</param>
    /// <returns>False when the name is unknown.</returns>
    public static bool TryParse(string name, out GamepadButton button)
    {
        var key = Normalize(name);
        foreach (var (n, b) in Canonical)
        {
            if (Normalize(n) == key)
            {
                button = b;
                return true;
            }
        }

        foreach (var (n, b) in Aliases)
        {
            if (n == key)
            {
                button = b;
                return true;
            }
        }

        button = default;
        return false;
    }

    private static string Normalize(string s) =>
        string.Concat(s.Where(c => c is not ('_' or '-' or ' '))).ToLowerInvariant();
}
