using System.Numerics;
using OneGame.UI;

namespace OneGame.Tweens;

/// <summary>Something a tween drives that is not a plain value: it is told the eased progress each time the tween moves.</summary>
public interface ITweenTarget
{
    /// <summary>Called when the tween's eased progress changes: 0 at the start and 1 at the end, outside that range for easings that overshoot.</summary>
    /// <param name="progress">The eased progress.</param>
    void Apply(float progress);

    /// <summary>A short description for the harness's tween listing, or null.</summary>
    string? Label => null;
}

/// <summary>Thrown when a tween handle is used wrongly: after the tween ended, on a tween that is part of a group, or with an unusable shape.</summary>
public sealed class TweenException : InvalidOperationException
{
    /// <summary>Creates the exception.</summary>
    /// <param name="message">What was wrong and what to do instead.</param>
    public TweenException(string message)
        : base(message)
    {
    }
}

/// <summary>A running tween as the harness lists it (see <see cref="TweenSet.Snapshot"/>).</summary>
/// <param name="Id">A number that counts up from 1 in creation order.</param>
/// <param name="Kind"><c>value</c>, <c>call</c>, <c>delay</c>, <c>sequence</c> or <c>parallel</c>.</param>
/// <param name="Name">The name given with <see cref="Tween.Named"/>, if any.</param>
/// <param name="Label">What the tween drives, when it says (a style transition names its element and property).</param>
/// <param name="Elapsed">Seconds since it started, delay included.</param>
/// <param name="Duration">Seconds from start to finish, delay and repeats included; -1 when it repeats forever.</param>
/// <param name="Progress">0 to 1 through <paramref name="Duration"/>; through the current repeat when it runs forever.</param>
/// <param name="Paused">True while <see cref="Tween.Pause"/> holds it.</param>
/// <param name="Children">How many tweens are inside it (groups only).</param>
public readonly record struct TweenInfo(long Id, string Kind, string? Name, string? Label, double Elapsed, double Duration, double Progress, bool Paused, int Children);

internal enum NodeKind : byte
{
    Leaf,
    Call,
    Delay,
    Sequence,
    Parallel,
}

internal enum Sink : byte
{
    None,
    Float,
    Vec2,
    Vec3,
    Vec4,
    Color,
    Target,
}

internal sealed class TweenNode
{
    public int Gen;
    public bool Dead;
    public NodeKind Kind;
    public Sink Sink;
    public object? Target;
    public Vector4 From;
    public Vector4 To;
    public double Duration;
    public double Delay;
    public Easing Ease;
    public int Plays = 1;
    public bool Yoyo;
    public Action? OnComplete;
    public string? Name;
    public long Serial;
    public TweenNode? Parent;
    public List<TweenNode>? Children;
    public double Offset;
    public double Elapsed;
    public bool Paused;
    public bool Applied;
    public bool Fired;
    public double LastEff = double.NaN;
    public double LastCycle;
    public double IterCache;
    public bool IterValid;
    public int Depth;
}

/// <summary>
/// A set of tweens that all run on one clock. Games and the UI share one: <c>game.Tweens</c> is the set the UI's style transitions run in, and the host
/// advances it once per frame by the view time that passed. A tween changes values and calls back; it never touches the simulation, so replays and
/// state hashes are the same with or without tweens.
/// </summary>
/// <remarks>
/// Tweens are pooled: starting and running them allocates nothing once the pool is warm (the delegate you pass is yours to keep alive).
/// Every tween starts on the next <see cref="Advance"/>. A tween's handle is a small struct that stops working when the tween ends or is cancelled.
/// </remarks>
public sealed class TweenSet
{
    private const int MaxDepth = 8;
    private const int MaxChildren = 4096;

    private readonly List<TweenNode> _roots = [];
    private readonly Stack<TweenNode> _free = new();
    private readonly List<Action> _pending = [];
    private long _serial;
    private bool _seeking;
    private bool _draining;

    /// <summary>Seconds of view time this set has been advanced by (scaled by <see cref="TimeScale"/>, not counting paused time).</summary>
    public double Time { get; private set; }

    /// <summary>How many top-level tweens are running or waiting to start.</summary>
    public int ActiveCount => _roots.Count;

    /// <summary>Multiplies every <see cref="Advance"/>: 0.5 is slow motion. Not negative.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or not finite.</exception>
    public float TimeScale
    {
        get;
        set
        {
            if (!(value >= 0 && float.IsFinite(value)))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "The time scale must be zero or more.");
            }

            field = value;
        }
    } = 1;

    /// <summary>True stops the whole set: <see cref="Advance"/> does nothing until it is false again.</summary>
    public bool Paused { get; set; }

    // ---- starting ------------------------------------------------------------------------------------------------

    /// <summary>Moves a number from <paramref name="from"/> to <paramref name="to"/>, calling <paramref name="apply"/> with each value.</summary>
    /// <param name="from">The value at the start.</param>
    /// <param name="to">The value at the end.</param>
    /// <param name="seconds">How long it takes; zero jumps to the end on the first advance.</param>
    /// <param name="apply">Receives the value. Also called once with <paramref name="from"/> when the tween starts, and with exactly <paramref name="to"/> at the end.</param>
    /// <exception cref="ArgumentOutOfRangeException">The duration is negative or not finite.</exception>
    public Tween Number(float from, float to, float seconds, Action<float> apply)
    {
        ArgumentNullException.ThrowIfNull(apply);
        var n = Leaf(Sink.Float, seconds, apply);
        n.From = new Vector4(from, 0, 0, 0);
        n.To = new Vector4(to, 0, 0, 0);
        return Start(n);
    }

    /// <summary>Moves a 2D vector (a position, a size) between two values.</summary>
    /// <param name="from">The value at the start.</param>
    /// <param name="to">The value at the end.</param>
    /// <param name="seconds">How long it takes.</param>
    /// <param name="apply">Receives the value.</param>
    public Tween Vector2(Vector2 from, Vector2 to, float seconds, Action<Vector2> apply)
    {
        ArgumentNullException.ThrowIfNull(apply);
        var n = Leaf(Sink.Vec2, seconds, apply);
        n.From = new Vector4(from, 0, 0);
        n.To = new Vector4(to, 0, 0);
        return Start(n);
    }

    /// <summary>Moves a 3D vector between two values.</summary>
    /// <param name="from">The value at the start.</param>
    /// <param name="to">The value at the end.</param>
    /// <param name="seconds">How long it takes.</param>
    /// <param name="apply">Receives the value.</param>
    public Tween Vector3(Vector3 from, Vector3 to, float seconds, Action<Vector3> apply)
    {
        ArgumentNullException.ThrowIfNull(apply);
        var n = Leaf(Sink.Vec3, seconds, apply);
        n.From = new Vector4(from, 0);
        n.To = new Vector4(to, 0);
        return Start(n);
    }

    /// <summary>Moves a 4D vector between two values.</summary>
    /// <param name="from">The value at the start.</param>
    /// <param name="to">The value at the end.</param>
    /// <param name="seconds">How long it takes.</param>
    /// <param name="apply">Receives the value.</param>
    public Tween Vector4(Vector4 from, Vector4 to, float seconds, Action<Vector4> apply)
    {
        ArgumentNullException.ThrowIfNull(apply);
        var n = Leaf(Sink.Vec4, seconds, apply);
        n.From = from;
        n.To = to;
        return Start(n);
    }

    /// <summary>Moves a color between two values, blending the way CSS does (premultiplied alpha, in sRGB).</summary>
    /// <param name="from">The value at the start.</param>
    /// <param name="to">The value at the end.</param>
    /// <param name="seconds">How long it takes.</param>
    /// <param name="apply">Receives the color.</param>
    public Tween Color(Color from, Color to, float seconds, Action<Color> apply)
    {
        ArgumentNullException.ThrowIfNull(apply);
        var n = Leaf(Sink.Color, seconds, apply);
        n.From = new Vector4(from.R, from.G, from.B, from.A);
        n.To = new Vector4(to.R, to.G, to.B, to.A);
        return Start(n);
    }

    /// <summary>Drives a <see cref="ITweenTarget"/> with eased progress from 0 to 1.</summary>
    /// <param name="target">Told the progress.</param>
    /// <param name="seconds">How long it takes.</param>
    public Tween Custom(ITweenTarget target, float seconds)
    {
        ArgumentNullException.ThrowIfNull(target);
        return Start(Leaf(Sink.Target, seconds, target));
    }

    /// <summary>Does nothing for <paramref name="seconds"/>: a pause inside a <see cref="Sequence"/>.</summary>
    /// <param name="seconds">How long to wait.</param>
    public Tween Delay(float seconds)
    {
        var n = Leaf(Sink.None, seconds, null);
        n.Kind = NodeKind.Delay;
        return Start(n);
    }

    /// <summary>Calls <paramref name="action"/> when its turn comes: at once on the next advance, or in the middle of a <see cref="Sequence"/>.</summary>
    /// <param name="action">What to run. It runs after the frame's values are applied, so it may start or cancel tweens.</param>
    public Tween Call(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var n = Rent(NodeKind.Call);
        n.OnComplete = action;
        return Start(n);
    }

    /// <summary>Plays tweens one after another. Each child starts when the one before it has finished (its delay and repeats included).</summary>
    /// <param name="children">Tweens made but not yet advanced, such as <c>tweens.Number(...)</c> results. They become part of the group.</param>
    /// <exception cref="TweenException">A child already started, belongs to a group or another set, or repeats forever.</exception>
    public Tween Sequence(params ReadOnlySpan<Tween> children) => Group(NodeKind.Sequence, children);

    /// <summary>Plays tweens at the same time. It ends when the longest child ends.</summary>
    /// <param name="children">Tweens made but not yet advanced. They become part of the group.</param>
    /// <exception cref="TweenException">A child already started, belongs to a group or another set, or repeats forever.</exception>
    public Tween Parallel(params ReadOnlySpan<Tween> children) => Group(NodeKind.Parallel, children);

    private Tween Group(NodeKind kind, ReadOnlySpan<Tween> children)
    {
        var g = Rent(kind);
        g.Children ??= [];
        var tween = Start(g);
        for (var i = 0; i < children.Length; i++)
        {
            try
            {
                tween.Add(children[i]);
            }
            catch (TweenException)
            {
                // A group that failed halfway is not left running.
                tween.Cancel();
                throw;
            }
        }

        return tween;
    }

    // Durations are kept as the decimal the caller wrote (0.2f is 0.2, not 0.20000000298...), so a step of exactly that long finishes the tween.
    internal static double Exact(float seconds) => Math.Round((double)seconds, 6);

    private TweenNode Leaf(Sink sink, float seconds, object? target)
    {
        if (!(seconds >= 0 && float.IsFinite(seconds)))
        {
            throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "A tween's duration must be zero or more.");
        }

        var n = Rent(NodeKind.Leaf);
        n.Sink = sink;
        n.Target = target;
        n.Duration = Exact(seconds);
        return n;
    }

    private TweenNode Rent(NodeKind kind)
    {
        var n = _free.Count > 0 ? _free.Pop() : new TweenNode();
        n.Kind = kind;
        n.Dead = false;
        n.Serial = ++_serial;
        return n;
    }

    private Tween Start(TweenNode n)
    {
        _roots.Add(n);
        return new Tween(this, n, n.Gen);
    }

    // ---- running -------------------------------------------------------------------------------------------------

    /// <summary>
    /// Moves every tween forward by <paramref name="seconds"/> (times <see cref="TimeScale"/>), applies their values, then runs the completion callbacks of the
    /// tweens that finished. The host calls this once per frame; a test or the harness may call it with exact steps.
    /// </summary>
    /// <param name="seconds">View time that passed; zero or more.</param>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or not finite.</exception>
    /// <exception cref="TweenException">Called from inside a value callback of this set.</exception>
    public void Advance(double seconds)
    {
        if (!(seconds >= 0 && double.IsFinite(seconds)))
        {
            throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "Time can only move forward.");
        }

        if (_seeking || _draining)
        {
            throw new TweenException("Advance was called from a tween callback. Let the host advance the set.");
        }

        if (Paused)
        {
            return;
        }

        var dt = seconds * TimeScale;
        Time += dt;
        var count = _roots.Count;
        _seeking = true;
        try
        {
            for (var i = 0; i < count; i++)
            {
                var r = _roots[i];
                if (r.Dead || r.Paused)
                {
                    continue;
                }

                r.Elapsed += dt;
                Step(r);
            }
        }
        finally
        {
            _seeking = false;
            Sweep();
        }

        Drain();
    }

    // Runs one root to its current time and retires it when it has finished.
    private void Step(TweenNode r)
    {
        Seek(r, r.Elapsed);
        var total = Total(r);
        if (r.Elapsed >= total)
        {
            r.Dead = true;
            r.Gen++;
        }
    }

    private void Sweep()
    {
        var w = 0;
        for (var i = 0; i < _roots.Count; i++)
        {
            var r = _roots[i];
            if (r.Dead)
            {
                Release(r);
            }
            else
            {
                _roots[w++] = r;
            }
        }

        _roots.RemoveRange(w, _roots.Count - w);
    }

    private void Drain()
    {
        _draining = true;
        var head = 0;
        try
        {
            // Callbacks may start tweens or cancel others, but not advance; what they append waits for the next frame.
            var n = _pending.Count;
            for (; head < n;)
            {
                var a = _pending[head++];
                a();
            }
        }
        finally
        {
            _draining = false;
            _pending.RemoveRange(0, head);
        }
    }

    private void Release(TweenNode n)
    {
        if (n.Children is { } kids)
        {
            foreach (var c in kids)
            {
                Release(c);
            }

            kids.Clear();
        }

        n.Gen++;
        n.Target = null;
        n.OnComplete = null;
        n.Name = null;
        n.Parent = null;
        n.Sink = Sink.None;
        n.Plays = 1;
        n.Yoyo = false;
        n.Delay = 0;
        n.Duration = 0;
        n.Ease = default;
        n.Elapsed = 0;
        n.Paused = false;
        n.Applied = false;
        n.Fired = false;
        n.LastEff = double.NaN;
        n.LastCycle = 0;
        n.IterValid = false;
        n.Offset = 0;
        n.Depth = 0;
        n.Dead = true;
        _free.Push(n);
    }

    // Length of one play of a node, in seconds; groups derive it from their children (and set each child's offset).
    private static double Iter(TweenNode n)
    {
        if (n.IterValid)
        {
            return n.IterCache;
        }

        double v;
        switch (n.Kind)
        {
            case NodeKind.Call:
                v = 0;
                break;
            case NodeKind.Sequence:
                v = 0;
                foreach (var c in n.Children!)
                {
                    c.Offset = v;
                    v += Total(c);
                }

                break;
            case NodeKind.Parallel:
                v = 0;
                foreach (var c in n.Children!)
                {
                    c.Offset = 0;
                    v = Math.Max(v, Total(c));
                }

                break;
            default:
                v = n.Duration;
                break;
        }

        n.IterCache = v;
        n.IterValid = true;
        return v;
    }

    internal static double Total(TweenNode n) => n.Plays < 0 ? double.PositiveInfinity : n.Delay + (Iter(n) * n.Plays);

    // Puts a node at local time t (seconds since it started). Values are a pure function of t, so scrubbing and seeking agree with playing.
    private void Seek(TweenNode n, double t, bool reset = false)
    {
        var u = t - n.Delay;
        var iter = Iter(n);
        double eff;
        var reversing = false;
        if (u < 0)
        {
            // During a delay nothing is touched, unless the node was already shown (a repeat or a reversal brought it back to the start).
            if (!n.Applied)
            {
                return;
            }

            eff = 0;
        }
        else
        {
            double cycle = 0;
            double phase = 0;
            if (iter > 0)
            {
                cycle = Math.Floor(u / iter);
                if (n.Plays > 0 && cycle >= n.Plays)
                {
                    cycle = n.Plays - 1;
                    phase = iter;
                }
                else
                {
                    phase = u - (cycle * iter);
                }
            }

            reversing = n.Yoyo && (((long)cycle) & 1) == 1;
            eff = reversing ? iter - phase : phase;

            // A step that crosses into a later play first shows the end of the play it left, so a call at the very end of a repeated
            // group still runs. (A step that spans several plays runs that end once, not once per play.)
            var left = n.Applied ? n.LastCycle : 0;
            if (cycle > left && iter > 0)
            {
                var leftEnd = n.Yoyo && (((long)left) & 1) == 1 ? 0 : iter;
                if (!n.Applied || leftEnd != n.LastEff)
                {
                    var before = n.LastEff;
                    n.Applied = true;
                    n.LastEff = leftEnd;
                    Evaluate(n, leftEnd, iter, leftEnd < before, reset);
                }
            }

            n.LastCycle = cycle;
        }

        if (!n.Applied || eff != n.LastEff)
        {
            var prev = n.LastEff;
            n.Applied = true;
            n.LastEff = eff;
            // Time going back that is not half of a yoyo is a restart (a repeat, or a seek): completions may fire again.
            Evaluate(n, eff, iter, eff < prev && !reversing, reset);
        }

        var done = !reset && u >= 0 && t >= Total(n);
        if (done)
        {
            if (!n.Fired)
            {
                n.Fired = true;
                if (n.OnComplete is { } cb)
                {
                    _pending.Add(cb);
                }
            }
        }
        else
        {
            n.Fired = false;
        }
    }

    private void Evaluate(TweenNode n, double eff, double iter, bool restart, bool reset)
    {
        switch (n.Kind)
        {
            case NodeKind.Leaf:
                var p = iter <= 0 ? 1f : (float)Math.Clamp(eff / iter, 0, 1);
                ApplyValue(n, n.Ease.Evaluate(p));
                break;
            case NodeKind.Sequence:
            case NodeKind.Parallel:
                var kids = n.Children!;
                // Children that are not started yet but were shown go back to their start first, last child first, so a shared property ends on the earliest value.
                for (var i = kids.Count - 1; i >= 0; i--)
                {
                    var c = kids[i];
                    if (eff - c.Offset < 0 && c.Applied)
                    {
                        Seek(c, 0, true);
                    }
                    else if (restart)
                    {
                        ClearFired(c);
                    }
                }

                for (var i = 0; i < kids.Count; i++)
                {
                    var c = kids[i];
                    var ct = eff - c.Offset;
                    if (ct >= 0)
                    {
                        Seek(c, Math.Min(ct, Total(c)), reset);
                    }
                }

                break;
        }
    }

    private static void ClearFired(TweenNode n)
    {
        n.Fired = false;
        if (n.Children is { } kids)
        {
            foreach (var k in kids)
            {
                ClearFired(k);
            }
        }
    }

    private static void ApplyValue(TweenNode n, float e)
    {
        var a = n.From;
        var b = n.To;
        switch (n.Sink)
        {
            case Sink.Float:
                ((Action<float>)n.Target!)(Mix(a.X, b.X, e));
                break;
            case Sink.Vec2:
                ((Action<Vector2>)n.Target!)(new Vector2(Mix(a.X, b.X, e), Mix(a.Y, b.Y, e)));
                break;
            case Sink.Vec3:
                ((Action<Vector3>)n.Target!)(new Vector3(Mix(a.X, b.X, e), Mix(a.Y, b.Y, e), Mix(a.Z, b.Z, e)));
                break;
            case Sink.Vec4:
                ((Action<Vector4>)n.Target!)(new Vector4(Mix(a.X, b.X, e), Mix(a.Y, b.Y, e), Mix(a.Z, b.Z, e), Mix(a.W, b.W, e)));
                break;
            case Sink.Color:
                ((Action<Color>)n.Target!)(Interp.Mix(new Color((byte)a.X, (byte)a.Y, (byte)a.Z, (byte)a.W), new Color((byte)b.X, (byte)b.Y, (byte)b.Z, (byte)b.W), e));
                break;
            case Sink.Target:
                ((ITweenTarget)n.Target!).Apply(e);
                break;
        }
    }

    // from*(1-e) + to*e hits `to` exactly at e = 1, which a + (b-a)*e does not always.
    internal static float Mix(float a, float b, float e) => (a * (1 - e)) + (b * e);

    // ---- handle operations ---------------------------------------------------------------------------------------

    internal static TweenNode Resolve(TweenNode? node, int gen)
    {
        if (node is null || node.Gen != gen || node.Dead)
        {
            throw new TweenException("This tween has ended or was cancelled, so its handle no longer does anything. Check Tween.IsActive before using a handle you kept.");
        }

        return node;
    }

    internal static void Invalidate(TweenNode n)
    {
        for (var x = n; x is not null; x = x.Parent)
        {
            x.IterValid = false;
        }
    }

    internal void AddChild(TweenNode group, Tween child)
    {
        if (group.Kind is not (NodeKind.Sequence or NodeKind.Parallel))
        {
            throw new TweenException("Only a sequence or a parallel group can hold other tweens.");
        }

        if (!ReferenceEquals(child.Set, this))
        {
            throw new TweenException("A tween can only join a group from the same TweenSet.");
        }

        var c = Resolve(child.Node, child.Gen);
        if (group.Parent is not null)
        {
            throw new TweenException("This group is already inside another group; add its children before nesting it.");
        }

        if (c.Parent is not null || ReferenceEquals(c, group))
        {
            throw new TweenException("This tween is already part of a group.");
        }

        if (c.Elapsed > 0 || c.Applied)
        {
            throw new TweenException("This tween has already started. Build groups from tweens made in the same frame.");
        }

        if (c.Plays < 0)
        {
            throw new TweenException("A tween that repeats forever cannot be inside a group, because the group would never finish. Give it a number of repeats.");
        }

        if (group.Elapsed > 0 || group.Applied)
        {
            throw new TweenException("The group has already started; add its children first.");
        }

        if (group.Children!.Count >= MaxChildren)
        {
            throw new TweenException($"A group holds at most {MaxChildren} tweens.");
        }

        if (c.Depth + 1 >= MaxDepth)
        {
            throw new TweenException($"Groups can be nested at most {MaxDepth - 1} deep.");
        }

        _roots.Remove(c);
        c.Parent = group;
        group.Children.Add(c);
        group.Depth = Math.Max(group.Depth, c.Depth + 1);
        Invalidate(group);
    }

    internal void Cancel(TweenNode n)
    {
        if (n.Parent is not null)
        {
            throw new TweenException("This tween is part of a group. Cancel the group instead.");
        }

        n.Dead = true;
        n.Gen++;
        if (!_seeking)
        {
            Sweep();
        }
    }

    /// <summary>Stops every tween where it is, without callbacks. Handles stop working.</summary>
    public void CancelAll()
    {
        foreach (var r in _roots)
        {
            r.Dead = true;
            r.Gen++;
        }

        if (!_seeking)
        {
            Sweep();
        }
    }

    internal void Finish(TweenNode n)
    {
        if (n.Parent is not null)
        {
            throw new TweenException("This tween is part of a group. Complete the group instead.");
        }

        if (n.Plays < 0)
        {
            throw new TweenException("A tween that repeats forever never finishes. Cancel it instead.");
        }

        MoveTo(n, Total(n));
    }

    internal void MoveTo(TweenNode n, double seconds)
    {
        if (n.Parent is not null)
        {
            throw new TweenException("This tween is part of a group. Move the group instead.");
        }

        if (_seeking || _draining)
        {
            throw new TweenException("A tween cannot be moved from inside a tween callback.");
        }

        n.Elapsed = Math.Max(0, seconds);
        _seeking = true;
        try
        {
            Step(n);
        }
        finally
        {
            _seeking = false;
            Sweep();
        }

        Drain();
    }

    /// <summary>Lists the running top-level tweens, oldest first, into <paramref name="into"/> (which is cleared first).</summary>
    /// <param name="into">The list to fill; reuse one to avoid allocating.</param>
    public void Snapshot(List<TweenInfo> into)
    {
        ArgumentNullException.ThrowIfNull(into);
        into.Clear();
        foreach (var r in _roots)
        {
            if (r.Dead)
            {
                continue;
            }

            into.Add(Describe(r));
        }
    }

    internal static TweenInfo Describe(TweenNode r)
    {
        var total = Total(r);
        var iter = Iter(r);
        var progress = double.IsInfinity(total)
            ? (iter > 0 ? ((r.Elapsed - r.Delay) <= 0 ? 0 : ((r.Elapsed - r.Delay) / iter) % 1) : 0)
            : total <= 0 ? 1 : Math.Clamp(r.Elapsed / total, 0, 1);
        var kind = r.Kind switch
        {
            NodeKind.Leaf => "value",
            NodeKind.Call => "call",
            NodeKind.Delay => "delay",
            NodeKind.Sequence => "sequence",
            _ => "parallel",
        };
        return new TweenInfo(r.Serial, kind, r.Name, (r.Target as ITweenTarget)?.Label, r.Elapsed, double.IsInfinity(total) ? -1 : total, progress, r.Paused, CountChildren(r));
    }

    private static int CountChildren(TweenNode n)
    {
        var c = 0;
        if (n.Children is { } kids)
        {
            foreach (var k in kids)
            {
                c += 1 + CountChildren(k);
            }
        }

        return c;
    }
}
