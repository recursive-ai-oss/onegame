namespace OneGame.Tweens;

/// <summary>
/// A handle to one tween in a <see cref="TweenSet"/>. Configure it right after you start it (<c>tweens.Number(...).Ease(Easing.OutCubic).Delay(0.1f)</c>),
/// then keep it only if you may want to cancel or inspect it. A handle stops working when its tween ends or is cancelled: <see cref="IsActive"/> turns false,
/// and members that need a live tween throw <see cref="TweenException"/>.
/// </summary>
public readonly struct Tween : IEquatable<Tween>
{
    private readonly TweenSet? _set;
    private readonly TweenNode? _node;
    private readonly int _gen;

    internal Tween(TweenSet set, TweenNode node, int gen)
    {
        _set = set;
        _node = node;
        _gen = gen;
    }

    internal TweenSet? Set => _set;

    internal TweenNode? Node => _node;

    internal int Gen => _gen;

    /// <summary>True while the tween is running or waiting to start. False for <c>default(Tween)</c>, and after it ended or was cancelled.</summary>
    public bool IsActive => _node is not null && _node.Gen == _gen && !_node.Dead;

    private TweenNode Live => TweenSet.Resolve(_node, _gen);

    /// <summary>Sets the easing curve. The default is <see cref="Easing.Linear"/>.</summary>
    /// <param name="easing">The curve.</param>
    public Tween Ease(Easing easing)
    {
        Live.Ease = easing;
        return this;
    }

    /// <summary>Sets the easing curve from CSS spelling, such as <c>"ease-out"</c> or <c>"cubic-bezier(0.2, 0, 0, 1)"</c>.</summary>
    /// <param name="easing">The text; see <see cref="Easing.TryParse"/>.</param>
    /// <exception cref="FormatException">The text is not an easing function.</exception>
    public Tween Ease(string easing) => Ease(Easing.Parse(easing));

    /// <summary>Waits before starting. Nothing is changed during the wait. Inside a sequence the delay is part of this tween's slot.</summary>
    /// <param name="seconds">Seconds to wait; zero or more.</param>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or not finite.</exception>
    public Tween Delay(float seconds)
    {
        if (!(seconds >= 0 && float.IsFinite(seconds)))
        {
            throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "A delay must be zero or more.");
        }

        var n = Live;
        n.Delay = TweenSet.Exact(seconds);
        TweenSet.Invalidate(n);
        return this;
    }

    /// <summary>Plays the tween this many times in total (1 is the default: no repeat).</summary>
    /// <param name="plays">How many times to play; at least 1.</param>
    /// <param name="yoyo">True plays every second repeat backwards, so a pulse goes there and back.</param>
    /// <exception cref="ArgumentOutOfRangeException">The count is below 1.</exception>
    public Tween Repeat(int plays, bool yoyo = false)
    {
        if (plays < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(plays), plays, "A tween plays at least once. Use RepeatForever for an endless loop.");
        }

        var n = Live;
        n.Plays = plays;
        n.Yoyo = yoyo;
        TweenSet.Invalidate(n);
        return this;
    }

    /// <summary>Plays until cancelled.</summary>
    /// <param name="yoyo">True plays every second repeat backwards.</param>
    /// <exception cref="TweenException">The tween is inside a group.</exception>
    public Tween RepeatForever(bool yoyo = false)
    {
        var n = Live;
        if (n.Parent is not null)
        {
            throw new TweenException("A tween inside a group cannot repeat forever. Repeat the group instead.");
        }

        n.Plays = -1;
        n.Yoyo = yoyo;
        return this;
    }

    /// <summary>Runs <paramref name="action"/> when the tween finishes (after its last repeat). It runs after that frame's values are applied.</summary>
    /// <param name="action">The callback; replaces an earlier one. It may start or cancel tweens. A tween that is cancelled never calls it.</param>
    public Tween OnComplete(Action? action)
    {
        Live.OnComplete = action;
        return this;
    }

    /// <summary>Gives the tween a name that shows in the harness's tween listing.</summary>
    /// <param name="name">A short name.</param>
    public Tween Named(string? name)
    {
        Live.Name = name;
        return this;
    }

    /// <summary>Appends a tween to a sequence, or adds it to a parallel group. The child must be fresh: made in the same frame and not yet advanced.</summary>
    /// <param name="child">The tween to add.</param>
    /// <exception cref="TweenException">This is not a group, or the child started, is in a group, or repeats forever.</exception>
    public Tween Add(Tween child)
    {
        _set!.AddChild(Live, child);
        return this;
    }

    /// <summary>Holds the tween where it is until <see cref="Resume"/>.</summary>
    /// <exception cref="TweenException">The tween is inside a group.</exception>
    public Tween Pause()
    {
        var n = Live;
        if (n.Parent is not null)
        {
            throw new TweenException("A tween inside a group cannot be paused on its own. Pause the group instead.");
        }

        n.Paused = true;
        return this;
    }

    /// <summary>Lets a paused tween run again.</summary>
    public Tween Resume()
    {
        Live.Paused = false;
        return this;
    }

    /// <summary>Stops the tween where it is. Its completion callback does not run. The handle stops working.</summary>
    /// <exception cref="TweenException">The tween is inside a group; cancel the group.</exception>
    public void Cancel() => _set!.Cancel(Live);

    /// <summary>Jumps to the end now: applies the final value and runs the completion callback, then the tween is over.</summary>
    /// <exception cref="TweenException">The tween repeats forever, is inside a group, or this was called from inside a tween callback.</exception>
    public void Complete() => _set!.Finish(Live);

    /// <summary>Moves the tween to a time (seconds since its start, delay included) and applies the values there. Scrubbing back replays callbacks.</summary>
    /// <param name="seconds">The time; zero or more.</param>
    /// <exception cref="TweenException">The tween is inside a group, or this was called from inside a tween callback.</exception>
    public void Seek(double seconds) => _set!.MoveTo(Live, seconds);

    /// <summary>Seconds since the tween started (paused time does not count), or 0 when it is not active.</summary>
    public double Elapsed => IsActive ? _node!.Elapsed : 0;

    /// <summary>Seconds from start to finish including delay and repeats; infinity when it repeats forever; 0 when not active.</summary>
    public double TotalDuration => IsActive ? TweenSet.Total(_node!) : 0;

    /// <summary>How far along the tween is, 0 to 1 of its <see cref="TotalDuration"/>. 0 when it is not active or never ends.</summary>
    public double Progress
    {
        get
        {
            if (!IsActive)
            {
                return 0;
            }

            var total = TweenSet.Total(_node!);
            return double.IsInfinity(total) ? 0 : total <= 0 ? 1 : Math.Clamp(_node!.Elapsed / total, 0, 1);
        }
    }

    /// <inheritdoc/>
    public bool Equals(Tween other) => ReferenceEquals(_node, other._node) && _gen == other._gen;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Tween t && Equals(t);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(_node, _gen);

    /// <summary>Equality: the same tween.</summary>
    public static bool operator ==(Tween left, Tween right) => left.Equals(right);

    /// <summary>Inequality.</summary>
    public static bool operator !=(Tween left, Tween right) => !left.Equals(right);
}
