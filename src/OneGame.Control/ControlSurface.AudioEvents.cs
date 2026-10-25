using OneGame.Audio;

namespace OneGame.Control;

/// <summary>Result of <c>audio_events</c>.</summary>
/// <param name="LastSequence">The sequence number of the newest event ever logged (0 before the first), also when none is returned.</param>
/// <param name="Frame">The game's frame counter now.</param>
/// <param name="Events">The sounds requested after <c>since</c>, oldest first.</param>
public sealed record AudioEventsResult(long LastSequence, long Frame, PlayedSound[] Events);

public sealed partial class ControlSurface
{
    private AudioEventsResult AudioEvents(Args a)
    {
        var audio = AudioOrThrow();
        var since = (long)a.Double("since", 0);
        var log = audio.GetPlayLog();
        var last = log.Count > 0 ? log[^1].Sequence : 0;
        var events = log.Where(e => e.Sequence > since).ToArray();
        if (a.Bool("clear", false))
        {
            audio.ClearPlayLog();
        }

        return new AudioEventsResult(last, Game.Frame, events);
    }
}
