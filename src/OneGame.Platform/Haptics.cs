namespace OneGame.Platform;

/// <summary>The kinds of touch feedback a game can ask for. Each maps to the nearest system effect on the platform.</summary>
public enum HapticKind
{
    /// <summary>A very light tick: a piece snaps into place, a slider crosses a step, a selection changes.</summary>
    Tick,

    /// <summary>A medium click: a button press.</summary>
    Click,

    /// <summary>A strong, longer pulse: a hit, an explosion, a big moment.</summary>
    Buzz,

    /// <summary>The action worked.</summary>
    Success,

    /// <summary>The action went through but something needs attention.</summary>
    Warning,

    /// <summary>The action failed or was refused.</summary>
    Error,
}

/// <summary>One haptic request, as <see cref="Haptics.GetLog"/> keeps it.</summary>
/// <param name="Sequence">Counts up from 1 for the life of the process and never repeats, even after old entries were dropped.</param>
/// <param name="Frame">The game frame the call was made in (from <see cref="Haptics.FrameClock"/>), or -1 when no game clock is attached.</param>
/// <param name="Kind">What was asked for.</param>
/// <param name="Delivered">True when the request was handed to the phone's system (which still drops it when the user turned touch feedback off); false on desktop and the web, on Android calls from a thread other than the game thread, and while <see cref="Haptics.Enabled"/> is off.</param>
public sealed record HapticEvent(long Sequence, long Frame, HapticKind Kind, bool Delivered);

/// <summary>
/// Touch feedback for view and host code. Call <see cref="Tick"/> when a piece snaps and <see cref="Buzz"/> for a bigger moment.
/// </summary>
/// <remarks>
/// <para>Android plays the request through <c>View.performHapticFeedback</c> on the game's window, which needs no permission and follows
/// the user's "touch feedback" setting by itself. iOS uses the feedback generators (<c>UIImpactFeedbackGenerator</c>,
/// <c>UISelectionFeedbackGenerator</c>, <c>UINotificationFeedbackGenerator</c>), which the system silences when haptics are off. The
/// simulator has no haptic engine, so the call runs and nothing is felt. Desktop and the web do nothing; a gamepad's rumble is
/// <see cref="GamepadManager.Rumble"/>.</para>
/// <para>Every call is recorded on every platform, so a scenario can assert on <see cref="GetLog"/> (the <c>haptic:</c> expectation and the
/// <c>haptics</c> control operation read it). Call it from views, input handlers and the host, never from sim code: the sim runs
/// during replays and on servers, where nobody holds a phone.</para>
/// </remarks>
public static class Haptics
{
    // A scenario reads the last few calls; this keeps a long-running game bounded.
    private const int LogCapacity = 1024;

    private static readonly Queue<HapticEvent> s_log = new();
    private static readonly object s_lock = new();
    private static long s_sequence;

    /// <summary>Whether requests reach the device. A game's "vibration" setting sets this; requests made while it is off are still logged, as not delivered.</summary>
    public static bool Enabled { get; set; } = true;

    [ThreadStatic]
    private static Func<long>? t_frameClock;

    /// <summary>Tells the log which game frame it is, for the calling thread. The game instance binds it to its frame counter on the thread that runs its frames (so two games in one process, as in tests, each stamp their own); a game never needs to. Requests made on a thread with no clock are stamped -1.</summary>
    public static Func<long>? FrameClock
    {
        get => t_frameClock;
        set => t_frameClock = value;
    }

    /// <summary>A very light tick.</summary>
    public static void Tick() => Play(HapticKind.Tick);

    /// <summary>A strong pulse.</summary>
    public static void Buzz() => Play(HapticKind.Buzz);

    /// <summary>Plays one kind of feedback.</summary>
    /// <param name="kind">The effect to play.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not a defined <see cref="HapticKind"/>.</exception>
    public static void Play(HapticKind kind)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Not a HapticKind.");
        }

        var delivered = Enabled && Native(kind);
        var frame = FrameClock?.Invoke() ?? -1;
        lock (s_lock)
        {
            if (s_log.Count >= LogCapacity)
            {
                s_log.Dequeue();
            }

            s_log.Enqueue(new HapticEvent(++s_sequence, frame, kind, delivered));
        }
    }

    /// <summary>The requests so far, oldest first, up to the last 1024. Works on every platform.</summary>
    /// <returns>A copy of the log.</returns>
    public static IReadOnlyList<HapticEvent> GetLog()
    {
        lock (s_lock)
        {
            return [.. s_log];
        }
    }

    /// <summary>Empties the log. Sequence numbers keep counting.</summary>
    public static void ClearLog()
    {
        lock (s_lock)
        {
            s_log.Clear();
        }
    }

    private static bool Native(HapticKind kind)
    {
        if (AndroidActivityHost.Current is { } host)
        {
            return AndroidHaptics.Play(host, kind);
        }

        if (OperatingSystem.IsIOS() && IosAppHost.Current is not null)
        {
            return IosHaptics.Play(kind);
        }

        return false;
    }
}
