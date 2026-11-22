using OneGame.Core;

namespace OneGame.Control;

public sealed partial class ControlSurface
{
    private ControlResult SimEventsOp(Args a)
    {
        var since = (long)a.Double("since", 0);
        var max = (int)a.Double("max", 256);
        if (since < 0 || max < 1 || max > EventLog.Capacity)
        {
            throw new ControlException("bad_args", $"since must be at least 0 and max between 1 and {EventLog.Capacity}.");
        }

        var world = Game.World;
        var log = world.Events;
        var j = new JsonOut();
        j.BeginObject();
        j.Name("world");
        j.Value(world.Role == WorldRole.Authority ? "authority" : "replica");
        j.Name("tick");
        j.Value(world.Tick);
        j.Name("next");
        j.Value(log.Count);
        j.Name("first");
        j.Value(log.FirstRetained);
        j.Name("dropped");
        j.Value(log.Dropped);
        j.Name("hash");
        j.Value(log.StreamHash.ToString("x16", System.Globalization.CultureInfo.InvariantCulture));
        j.Name("events");
        log.WriteJson(j, since, max);
        j.EndObject();
        return new ControlResult(j.ToString());
    }
}
