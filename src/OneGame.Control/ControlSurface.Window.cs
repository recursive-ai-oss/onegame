using OneGame.Platform;

namespace OneGame.Control;

/// <summary>A screen's usable rectangle in <see cref="WindowResult"/>.</summary>
/// <param name="X">Left edge in screen pixels.</param>
/// <param name="Y">Top edge in screen pixels.</param>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
public sealed record WorkAreaInfo(int X, int Y, int Width, int Height);

/// <summary>Result of <c>window</c>: the window's placement.</summary>
/// <param name="X">Left edge of the normal client rectangle, or null where the window system places windows itself.</param>
/// <param name="Y">Top edge of the normal client rectangle, or null where the window system places windows itself.</param>
/// <param name="Width">Width of the normal (restored) client rectangle in pixels.</param>
/// <param name="Height">Height of the normal (restored) client rectangle in pixels.</param>
/// <param name="State"><c>normal</c>, <c>maximized</c> or <c>fullscreen</c>.</param>
/// <param name="ClientWidth">The client width right now, in pixels (the screen size while maximized).</param>
/// <param name="ClientHeight">The client height right now, in pixels.</param>
/// <param name="CanPlace">Whether this window system lets the game read and set the position.</param>
/// <param name="Text">The placement as a settings string that <c>text</c> accepts back.</param>
/// <param name="WorkAreas">The usable rectangle of each connected screen, primary first; empty where unknown.</param>
public sealed record WindowResult(int? X, int? Y, int Width, int Height, string State, int ClientWidth, int ClientHeight, bool CanPlace, string Text, WorkAreaInfo[] WorkAreas);

public sealed partial class ControlSurface
{
    private WindowResult WindowOp(Args a)
    {
        var window = Game.Window;
        var wanted = window.Placement;
        var change = false;
        if (a.Str("text") is { } text)
        {
            if (!WindowPlacement.TryParse(text, out wanted))
            {
                throw new ControlException("bad_args", $"'{text}' is not a placement. Use x,y,WxH,state such as 100,80,1280x720,maximized.");
            }

            change = true;
        }

        if (a.Str("x") is not null || a.Str("y") is not null)
        {
            wanted = wanted with { X = a.Int("x", wanted.X), Y = a.Int("y", wanted.Y), HasPosition = window.CanPlace };
            change = true;
        }

        if (a.Str("width") is not null || a.Str("height") is not null)
        {
            wanted = wanted with { Width = a.Int("width", wanted.Width), Height = a.Int("height", wanted.Height) };
            change = true;
        }

        if (a.Str("state") is { } state)
        {
            if (!Enum.TryParse<WindowState>(state, true, out var parsed) || !Enum.IsDefined(parsed))
            {
                throw new ControlException("bad_args", $"'{state}' is not a window state. Use normal, maximized or fullscreen.");
            }

            wanted = wanted with { State = parsed };
            change = true;
        }

        if (change)
        {
            if (wanted.Width < 1 || wanted.Height < 1)
            {
                throw new ControlException("bad_args", "width and height must be at least 1.");
            }

            window.ApplyPlacement(wanted);
            Game.DrainInput();
        }

        var now = window.Placement;
        var areas = Game.Platform.GetWorkAreas().Select(r => new WorkAreaInfo(r.X, r.Y, r.Width, r.Height)).ToArray();
        return new WindowResult(
            now.HasPosition ? now.X : null, now.HasPosition ? now.Y : null, now.Width, now.Height, now.State.ToString().ToLowerInvariant(),
            window.Width, window.Height, window.CanPlace, now.Format(), areas);
    }
}
