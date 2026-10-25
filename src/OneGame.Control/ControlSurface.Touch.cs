using OneGame.Platform;

namespace OneGame.Control;

public sealed partial class ControlSurface
{
    private InputResult InputTouch(Args a)
    {
        var inj = Game.Platform.Injector;
        var window = Game.Window.Id;
        var id = (int)a.Double("id", 0);
        var x = (float)a.Double("x", 0);
        var y = (float)a.Double("y", 0);
        switch (a.Required("action"))
        {
            case "down":
                inj.TouchDown(window, id, x, y);
                break;
            case "move":
                inj.TouchMove(window, id, x, y);
                break;
            case "up":
                inj.TouchUp(window, id, x, y);
                break;
            case "cancel":
                inj.TouchCancel(window, id);
                break;
            case "tap":
                inj.Tap(window, x, y, id);
                break;
            case var other:
                throw new ControlException("bad_args", $"action is down, move, up, cancel or tap, got '{other}'.");
        }

        return Done();
    }

    private InputResult UiTap(Args a)
    {
        var e = FindElement(a);
        var b = e.AbsoluteBounds;
        var x = b.X + (b.Width / 2);
        var y = b.Y + (b.Height / 2);
        var hit = Game.Ui.HitTest(x, y);
        if (hit is null || !(hit == e || e.Contains(hit)))
        {
            throw new ControlException("obscured", $"{e} is not reachable at its center ({x:0.#}, {y:0.#}); the finger would hit {hit?.ToString() ?? "nothing"}. Open the right screen or scroll it into view.");
        }

        Game.Platform.Injector.Tap(Game.Window.Id, x, y);
        return Done();
    }

    private InputResult AppLifecycle(Args a)
    {
        Game.Platform.Injector.Lifecycle(a.Required("state") switch
        {
            "paused" => AppLifecycleState.Paused,
            "resumed" => AppLifecycleState.Resumed,
            "low_memory" => AppLifecycleState.LowMemory,
            var other => throw new ControlException("bad_args", $"state is paused, resumed or low_memory, got '{other}'."),
        });
        return Done();
    }

    private string PlatformNative(Args a)
    {
        string answer;
        try
        {
            answer = Game.Platform.NativeCommand(a.Required("action"), a.Str("text"));
        }
        catch (PlatformException ex)
        {
            throw new ControlException("unsupported", ex.Message);
        }

        // Text typed through the system reaches the game as platform events; let them take effect before answering.
        Game.DrainInput();
        return answer.StartsWith('{') ? answer : "{\"result\":\"" + answer + "\"}";
    }
}
