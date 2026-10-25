using OneGame.Steam.Input;

namespace OneGame.Control;

public sealed partial class ControlSurface
{
    private static SteamInputReport SteamInputOp(Args a)
    {
        var svc = SteamInputService.Active ?? throw new ControlException("no_steam_input", "The game has not started Steam Input (SteamInputService.Start).");
        var drives = a.Has("connect") || a.Has("disconnect") || a.Has("press") || a.Has("release") || a.Has("analog");
        if (drives)
        {
            var d = svc.Driver ?? throw new ControlException("not_fake", "Real Steam is behind Steam Input; only the harness's stand-in controller can be driven. Start the game with ONEGAME_STEAM_INPUT=fake and Steam not running.");
            var pad = a.Int("controller", 1);
            try
            {
                if (a.Has("connect"))
                {
                    d.Connect(a.Int("connect", 3));
                }

                if (a.Has("disconnect"))
                {
                    d.Disconnect(a.Int("disconnect", 1));
                }

                if (a.Str("press") is { } press)
                {
                    d.SetDigital(pad, press, true);
                }

                if (a.Str("release") is { } release)
                {
                    d.SetDigital(pad, release, false);
                }

                if (a.Str("analog") is { } analog)
                {
                    d.SetAnalog(pad, analog, (float)a.Double("x", 0), (float)a.Double("y", 0));
                }
            }
            catch (ArgumentException e)
            {
                throw new ControlException("bad_args", e.Message);
            }
        }

        return svc.Describe();
    }
}
