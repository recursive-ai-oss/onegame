using OneGame.Platform;
using OneGame.Steam.Input;

internal static class ActionSmoke
{
    /// <summary>Declares actions, drives them through the platform queue, a Steam stand-in controller and the harness source, and round-trips the bindings text. Returns a problem or null.</summary>
    public static string? Run()
    {
        var map = new ActionMap().Context("play", c => c
            .Button("jump", Bind.Key(Key.Space), Bind.Pad(GamepadButton.South))
            .Vector("move", Bind.Wasd, Bind.LeftStick)
            .Axis("zoom", Bind.Wheel(), Bind.Pair(Key.Q, Key.E)));
        using var app = PlatformApp.Create(new PlatformOptions { Backend = PlatformBackendKind.Headless, EnableGamepadDevices = false });
        var window = app.CreateWindow().Id;
        var pad = app.Injector.GamepadConnect("smoke");

        void Frame()
        {
            app.PumpEvents();
            while (app.TryGetEvent(out var e))
            {
                map.Observe(e);
            }

            map.Update(app.Input, 64, 64);
        }

        app.Injector.KeyPress(window, Key.Space);
        app.Injector.Key(window, Key.D, true);
        Frame();
        if (!map.Pressed("jump") || map.Vector("move").X != 1f)
        {
            return "keyboard did not drive the actions";
        }

        app.Injector.Key(window, Key.D, false);
        app.Injector.GamepadAxis(pad, GamepadAxis.LeftY, 1f);
        Frame();
        if (map.Vector("move").Y != 1f)
        {
            return "the pad stick did not drive the vector";
        }

        map.Harness.Pulse("jump");
        Frame();
        if (!map.Pressed("jump"))
        {
            return "the harness source did not press the button";
        }

        // Steam Input as a source, with the declaration built from the map.
        using var service = SteamInputService.StartFake(SteamActionSource.CreateActions(map));
        using var steam = new SteamActionSource(service, map);
        var controller = service.Driver!.Connect(3);
        service.ActivateForScreen("anything");
        service.Driver.SetDigital(controller, "jump", true);
        service.Update();
        app.Injector.GamepadAxis(pad, GamepadAxis.LeftY, 0f);
        Frame();
        if (steam.FedActions != 3 || !map.Held("jump"))
        {
            return "Steam Input did not feed the action";
        }

        // Rebinding and the text form.
        map.BeginRebind("jump");
        map.Observe(new PlatformEvent { Kind = PlatformEventKind.KeyDown, Key = Key.J });
        var text = map.ToText();
        map.ResetBindings();
        var loaded = map.LoadText(text);
        if (loaded.Applied != 1 || loaded.Problems.Count != 0 || map.ToText() != text || map.LoadText("jump = key:Nope").Problems.Count != 1)
        {
            return "bindings did not round-trip through text";
        }

        return null;
    }
}
