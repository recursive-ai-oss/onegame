using OneGame.Platform;

namespace OneGame.Control;

/// <summary>One haptic request in <see cref="HapticsResult"/>.</summary>
/// <param name="Sequence">Counts up from 1 and never repeats.</param>
/// <param name="Frame">The game frame of the call.</param>
/// <param name="Kind">The name of the <see cref="HapticKind"/>, such as <c>Tick</c>.</param>
/// <param name="Delivered">Whether a device took the request (false on desktop and the web).</param>
public sealed record HapticEventInfo(long Sequence, long Frame, string Kind, bool Delivered);

/// <summary>Result of <c>haptics</c>.</summary>
/// <param name="LastSequence">The sequence number of the newest request ever logged (0 before the first), also when none is returned.</param>
/// <param name="Frame">The game's frame counter now.</param>
/// <param name="Enabled">Whether <see cref="Haptics.Enabled"/> is on.</param>
/// <param name="Events">The requests after <c>since</c>, oldest first.</param>
public sealed record HapticsResult(long LastSequence, long Frame, bool Enabled, HapticEventInfo[] Events);

public sealed partial class ControlSurface
{
    private HapticsResult HapticsOp(Args a)
    {
        if (a.Str("play") is { } play)
        {
            if (!Enum.TryParse<HapticKind>(play, true, out var kind) || !Enum.IsDefined(kind))
            {
                throw new ControlException("bad_args", $"play is one of {string.Join(", ", Enum.GetNames<HapticKind>())}, got '{play}'.");
            }

            Haptics.Play(kind);
        }

        var since = (long)a.Double("since", 0);
        var log = Haptics.GetLog();
        var last = log.Count > 0 ? log[^1].Sequence : 0;
        var events = log.Where(e => e.Sequence > since).Select(e => new HapticEventInfo(e.Sequence, e.Frame, e.Kind.ToString(), e.Delivered)).ToArray();
        if (a.Bool("clear", false))
        {
            Haptics.ClearLog();
        }

        return new HapticsResult(last, Game.Frame, Haptics.Enabled, events);
    }
}
