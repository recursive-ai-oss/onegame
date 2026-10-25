using OneGame.Platform;
using OneGame.Steam.Input;
using OneGame.Stores;

internal static class SteamInputSmoke
{
    /// <summary>Declares action sets, writes the actions file, runs the stand-in controller through the service and checks the disabled path. Returns a problem or null.</summary>
    public static string? Run(IStorePlatform store)
    {
        var actions = new SteamInputActions()
            .Set("Map", "Map", s => s.Button("select", "Select", GamepadButton.South).Stick("move", "Move", SteamStickMode.JoystickMove, SteamStick.Left).OnScreens("map"))
            .Set("Menu", "Menu", s => s.Button("accept", "Accept", GamepadButton.South).Trigger("amount", "Amount", GamepadAxis.RightTrigger));
        if (!actions.ToVdf().Contains("\"In Game Actions\"", StringComparison.Ordinal) || !actions.Matches(actions.ToVdf(), out _))
        {
            return "the actions file was not written";
        }

        using var service = SteamInputService.StartFake(actions);
        var pad = service.Driver!.Connect(3);
        service.ActivateForScreen("map");
        service.Update();
        service.Driver.SetDigital(pad, "select", true);
        service.Driver.SetAnalog(pad, "move", 0.5f, 0.25f);
        service.Update();
        service.GetAnalog("move", out var x, out var y);
        if (!service.IsDown("select") || x != 0.5f || y != 0.25f || service.GetGlyphName("select") != "a" || service.Describe().Controllers.Count != 1)
        {
            return "the stand-in controller did not drive the service";
        }

        var off = SteamInputService.Start(store, actions);
        off.Update();
        if (store.IsAvailable == off.IsEnabled && !store.IsAvailable && off.ActivateSet("Map") != StoreResult.Unavailable)
        {
            return "a disabled service must report Unavailable";
        }

        return null;
    }
}
