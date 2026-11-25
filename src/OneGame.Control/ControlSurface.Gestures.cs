using System.Globalization;
using OneGame.Platform;

namespace OneGame.Control;

/// <summary>Result of <c>system_gestures</c>: what the game currently claims from the system's gestures.</summary>
/// <param name="Regions">The claimed rectangles as <c>x,y,w,h</c> in window pixels.</param>
/// <param name="Edges">The screen edges the claim touches (<c>left</c>, <c>top</c>, <c>right</c>, <c>bottom</c>), empty when none.</param>
/// <param name="Limit">The most height in pixels the system honours along one edge; 0 where there is no limit.</param>
public sealed record GestureClaimResult(string[] Regions, string[] Edges, int Limit);

public sealed partial class ControlSurface
{
    private GestureClaimResult GestureOp(Args a)
    {
        var window = Game.Window;
        if (a.Bool("clear", false))
        {
            window.ClaimSystemGestures([]);
        }

        var given = a.Strings("regions");
        if (given.Length > 0)
        {
            var parsed = new GestureRegion[given.Length];
            for (var i = 0; i < given.Length; i++)
            {
                var parts = given[i].Split(',');
                if (parts.Length != 4
                    || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var x)
                    || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var y)
                    || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var w)
                    || !int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var h))
                {
                    throw new ControlException("bad_args", $"'{given[i]}' is not a region. Use x,y,w,h in window pixels, such as 1070,200,10,800.");
                }

                parsed[i] = new GestureRegion(x, y, w, h);
            }

            window.ClaimSystemGestures(parsed);
        }

        var edges = window.ClaimedEdges;
        var names = new List<string>();
        foreach (var (flag, name) in new[] { (ScreenEdges.Left, "left"), (ScreenEdges.Top, "top"), (ScreenEdges.Right, "right"), (ScreenEdges.Bottom, "bottom") })
        {
            if ((edges & flag) != 0)
            {
                names.Add(name);
            }
        }

        return new GestureClaimResult(
            [.. window.ClaimedGestureRegions.Select(r => string.Create(CultureInfo.InvariantCulture, $"{r.X},{r.Y},{r.Width},{r.Height}"))],
            [.. names], window.GestureClaimLimit);
    }

    private InputResult InputBack()
    {
        Game.Platform.Injector.Back(Game.Window.Id);
        return Done();
    }
}
