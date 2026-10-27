using OneGame.Platform;
using OneGame.UI;
using OneGame.UI.Kit;

namespace OneGame.Host;

/// <summary>
/// Ties the action layer to the UI kit: glyph tables that follow the current bindings, and a ready-made rebinding list.
/// </summary>
public static class ActionUi
{
    /// <summary>
    /// Points an <see cref="InputGlyphs"/> table at the actions' current bindings, so <c>UiKit.InputGlyph(glyphs, "jump")</c> shows the key or button the player really uses.
    /// Each action gets its first keyboard key, mouse button and pad control; a vector shows its up key and the stick or d-pad. Call again after bindings change (<see cref="ActionMap.Version"/> tells).
    /// </summary>
    /// <param name="map">The game's actions.</param>
    /// <param name="glyphs">The glyph table.</param>
    public static void SyncGlyphs(ActionMap map, InputGlyphs glyphs)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(glyphs);
        foreach (var a in map.Actions)
        {
            string? key = null, mouse = null, pad = null;
            foreach (var b in a.Bindings)
            {
                // For a quad the glyph shows the "up" source; for a pair, the positive one.
                var s = b.Shape switch { BindingShape.Quad => b.Part(2), BindingShape.Pair => b.Part(1), _ => b.Part(0) };
                switch (s.Kind)
                {
                    case InputSourceKind.Key when key is null:
                        key = KeyNames.ToUiKey((Key)s.Code);
                        break;
                    case InputSourceKind.Mouse when mouse is null:
                        mouse = (MouseButton)s.Code switch { MouseButton.Right => "right", MouseButton.Middle => "middle", MouseButton.Left => "left", _ => null };
                        break;
                    case InputSourceKind.PadButton when pad is null:
                        pad = PadGlyph((GamepadButton)s.Code);
                        break;
                    case InputSourceKind.PadAxis when pad is null:
                        pad = (GamepadAxis)s.Code switch { GamepadAxis.LeftTrigger => "lt", GamepadAxis.RightTrigger => "rt", GamepadAxis.RightX or GamepadAxis.RightY => "rs", _ => "ls" };
                        break;
                    default:
                        break;
                }

                if (b.Shape == BindingShape.Stick && pad is null)
                {
                    pad = b.Part(0).Code == (int)GamepadAxis.RightX ? "rs" : "ls";
                }
            }

            glyphs.Bind(a.Name, key, mouse, pad);
        }
    }

    private static string? PadGlyph(GamepadButton b) => b switch
    {
        GamepadButton.South => "a",
        GamepadButton.East => "b",
        GamepadButton.West => "x",
        GamepadButton.North => "y",
        GamepadButton.LeftShoulder => "lb",
        GamepadButton.RightShoulder => "rb",
        GamepadButton.Back => "select",
        GamepadButton.Start => "start",
        GamepadButton.LeftStick => "ls",
        GamepadButton.RightStick => "rs",
        GamepadButton.DPadUp => "dpad-up",
        GamepadButton.DPadDown => "dpad-down",
        GamepadButton.DPadLeft => "dpad-left",
        GamepadButton.DPadRight => "dpad-right",
        _ => null,
    };

    /// <summary>
    /// A list of the actions with their current bindings, a glyph for each, a Rebind button and a Reset button. Rebind waits for the player's next key, mouse button, pad button or wheel turn
    /// (Escape cancels) and replaces the first binding, taking the source from other actions in the same context. Vectors and pair axes (WASD, sticks, Q/E) show their bindings and can be reset but not recaptured here;
    /// build their rows with <see cref="ActionMap.BeginRebind"/> if the game wants that. Ids: <c>rebind-NAME</c>, <c>reset-NAME</c>, <c>binding-NAME</c>; the list is <c>action-bindings</c>.
    /// </summary>
    /// <param name="game">The running game; its <see cref="GameInstance.Actions"/> are listed.</param>
    /// <param name="glyphs">The glyph table to show pictures from, or null for text only. The list keeps it in step with the bindings.</param>
    /// <param name="context">Only this context's actions, or null for all.</param>
    /// <returns>The list element.</returns>
    public static Element BindingList(GameInstance game, InputGlyphs? glyphs = null, string? context = null)
    {
        ArgumentNullException.ThrowIfNull(game);
        var map = game.Actions;
        var list = Ui.Column().Named("action-bindings").Gap(Length.Px(6));
        var last = -1;
        list.OnUpdate(_ =>
        {
            if (glyphs is not null && map.Version != last)
            {
                last = map.Version;
                SyncGlyphs(map, glyphs);
            }
        });
        foreach (var a in map.Actions)
        {
            if (context is not null && a.Context != context)
            {
                continue;
            }

            var name = a.Name;
            var canCapture = a.Kind == ActionKind.Button || (a.Kind == ActionKind.Axis && a.Bindings.All(b => b.Shape == BindingShape.One));
            var row = Ui.Row().Gap(Length.Px(8));
            row.Add(Ui.Label(a.Title).Width(Length.Px(160)));
            if (glyphs is not null)
            {
                row.Add(UiKit.InputGlyph(glyphs, name));
            }

            row.Add(Ui.Label(() => map.Rebinding?.Action == name ? "Press a key or button…" : Describe(map[name])).Named("binding-" + name).Grow());
            if (canCapture)
            {
                row.Add(UiKit.Button(() => map.Rebinding?.Action == name ? "Cancel" : "Rebind", () =>
                {
                    if (map.Rebinding?.Action == name)
                    {
                        map.CancelRebind();
                    }
                    else
                    {
                        map.BeginRebind(name, 0, 0, ConflictPolicy.Replace);
                    }
                }, "rebind-" + name));
            }

            row.Add(UiKit.Button("Reset", () => map.ResetBindings(name), "reset-" + name, ButtonKind.Flat));
            list.Add(row);
        }

        return list;
    }

    private static string Describe(InputAction a) => a.Bindings.Count == 0 ? "(unbound)" : string.Join(", ", a.Bindings.Select(b => b.Describe()));
}
