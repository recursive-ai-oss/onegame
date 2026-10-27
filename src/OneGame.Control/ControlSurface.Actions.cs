using System.Globalization;
using System.Text.Json;
using OneGame.Platform;

namespace OneGame.Control;

public sealed partial class ControlSurface
{
    private string ActionsOp(Args a)
    {
        var map = Game.Actions;
        if (map.Actions.Count == 0)
        {
            throw new ControlException("no_actions", "The game declares no input actions. Override GameDefinition.DefineActions.");
        }

        try
        {
            if (a.Has("reset") && a.Bool("reset", false))
            {
                map.ResetBindings();
            }

            var contexts = a.Strings("contexts");
            if (contexts.Length > 0)
            {
                map.SetActive([.. contexts]);
            }

            if (a.Str("name") is { } name)
            {
                DriveAction(map, name, a.Str("state") ?? "tap", a);
            }
        }
        catch (ArgumentException e)
        {
            throw new ControlException(map.TryGet(a.Str("name") ?? "", out _) ? "bad_args" : "unknown_action", e.Message);
        }

        return ActionReport(map);
    }

    private InputResult InputPad(Args a)
    {
        var inj = Game.Platform.Injector;
        if (_gamepad < 0)
        {
            _gamepad = inj.GamepadConnect("Harness Gamepad");
        }

        if (a.Str("axis") is { } axisName)
        {
            if (!Enum.TryParse<GamepadAxis>(axisName, true, out var axis) || !Enum.IsDefined(axis))
            {
                throw new ControlException("bad_args", $"'{axisName}' is not an axis: leftx, lefty, rightx, righty, lefttrigger or righttrigger.");
            }

            inj.GamepadAxis(_gamepad, axis, (float)Math.Clamp(a.Double("value", 0), -1, 1));
            return Done();
        }

        var name = a.Required("button");
        if (!NavActions.TryParse(name, out var button))
        {
            throw new ControlException("bad_args", $"'{name}' is not a pad button. Use one of: {string.Join(", ", NavActions.Names)} (or a, b, x, y).");
        }

        switch (a.Str("state") ?? "press")
        {
            case "press":
                inj.GamepadButton(_gamepad, button, true);
                inj.GamepadButton(_gamepad, button, false);
                break;
            case "down":
                inj.GamepadButton(_gamepad, button, true);
                break;
            case "up":
                inj.GamepadButton(_gamepad, button, false);
                break;
            case var other:
                throw new ControlException("bad_args", $"state is press, down or up, got '{other}'.");
        }

        return Done();
    }

    private static void DriveAction(ActionMap map, string name, string state, Args a)
    {
        var action = map[name];
        var src = map.Harness;
        switch (state)
        {
            case "tap" when action.Kind == ActionKind.Button:
                src.Pulse(name);
                break;
            case "up":
                src.Release(name);
                break;
            case "tap" or "down":
                if (action.Kind == ActionKind.Button)
                {
                    src.Set(name, true);
                }
                else
                {
                    src.Set(name, (float)a.Double("x", 0), (float)a.Double("y", 0));
                }

                break;
            default:
                throw new ControlException("bad_args", $"state is tap, down or up, got '{state}'.");
        }
    }

    internal static string ActionReport(ActionMap map)
    {
        using var ms = new MemoryStream();
        using (var w = new Utf8JsonWriter(ms))
        {
            w.WriteStartObject();
            w.WriteStartArray("contexts");
            foreach (var c in map.Contexts)
            {
                w.WriteStartObject();
                w.WriteString("name", c.Name);
                w.WriteBoolean("active", map.IsContextActive(c.Name));
                w.WriteStartArray("screens");
                foreach (var s in c.Screens)
                {
                    w.WriteStringValue(s);
                }

                w.WriteEndArray();
                w.WriteEndObject();
            }

            w.WriteEndArray();
            w.WriteStartArray("actions");
            foreach (var a in map.Actions)
            {
                w.WriteStartObject();
                w.WriteString("name", a.Name);
                w.WriteString("kind", a.Kind.ToString().ToLowerInvariant());
                w.WriteString("context", a.Context);
                w.WriteBoolean("active", a.IsActive);
                w.WriteBoolean("held", a.Held);
                w.WriteBoolean("pressed", a.Pressed);
                w.WriteBoolean("released", a.Released);
                w.WriteNumber("x", Math.Round(a.X, 4));
                w.WriteNumber("y", Math.Round(a.Y, 4));
                w.WriteBoolean("default", a.IsDefault);
                w.WriteStartArray("bindings");
                foreach (var b in a.Bindings)
                {
                    w.WriteStringValue(b.ToString());
                }

                w.WriteEndArray();
                w.WriteEndObject();
            }

            w.WriteEndArray();
            w.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(ms.ToArray());
    }
}
