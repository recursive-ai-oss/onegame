using System.Linq;

namespace OneGame.Gltf;

/// <summary>How a float parameter is compared in a transition condition.</summary>
public enum Compare
{
    /// <summary>The parameter is greater than the value.</summary>
    Greater,

    /// <summary>The parameter is less than the value.</summary>
    Less,
}

/// <summary>A parameter, state or clip name an <see cref="Animator"/> does not have.</summary>
public sealed class UnknownAnimatorNameException : ArgumentException
{
    /// <summary>Creates the exception.</summary>
    /// <param name="kind">What was looked up: parameter, state or trigger.</param>
    /// <param name="name">The name that was asked for.</param>
    /// <param name="known">The names that exist.</param>
    public UnknownAnimatorNameException(string kind, string name, IEnumerable<string> known)
        : base($"The animator has no {kind} named '{name}'. Known: {string.Join(", ", known)}.")
    {
    }
}

/// <summary>One state of an <see cref="Animator"/>: a motion and how it plays.</summary>
public sealed class AnimState
{
    internal readonly List<AnimTransition> Transitions = [];

    internal AnimState(string name, Motion motion, bool loop, float speed, int xParameter, int yParameter)
    {
        Name = name;
        Motion = motion;
        Loop = loop;
        Speed = speed;
        XParameter = xParameter;
        YParameter = yParameter;
    }

    /// <summary>The state's name.</summary>
    public string Name { get; }

    /// <summary>What the state plays.</summary>
    public Motion Motion { get; }

    /// <summary>Whether the motion repeats; if not it holds its last pose.</summary>
    public bool Loop { get; }

    /// <summary>Playback speed factor.</summary>
    public float Speed { get; }

    internal int XParameter { get; }

    internal int YParameter { get; }

    /// <summary>Progress through the motion in passes (1 = one full pass), not wrapped, reset when the state is entered.</summary>
    public float Progress { get; internal set; }

    internal float Phase => Loop ? Progress - MathF.Floor(Progress) : Math.Min(Progress, 1f);
}

/// <summary>A rule for leaving one state for another. Build it with the <c>When</c>/<c>AtExit</c>/<c>Fade</c> calls, which return it.</summary>
public sealed class AnimTransition
{
    internal readonly List<Condition> Conditions = [];
    private readonly Animator _owner;

    internal AnimTransition(Animator owner, AnimState? from, AnimState to)
    {
        _owner = owner;
        From = from;
        To = to;
    }

    /// <summary>The state it leaves, or null for a transition that applies from every state.</summary>
    public AnimState? From { get; }

    /// <summary>The state it enters.</summary>
    public AnimState To { get; }

    /// <summary>The crossfade length in seconds.</summary>
    public float FadeSeconds { get; private set; }

    /// <summary>The progress the source state must have reached, or a negative number for none.</summary>
    public float ExitTime { get; private set; } = -1f;

    /// <summary>Also requires a float parameter to compare true.</summary>
    public AnimTransition WhenFloat(string parameter, Compare compare, float value)
    {
        Conditions.Add(new Condition(_owner.Index(parameter), compare == Compare.Greater ? Op.Greater : Op.Less, value));
        return this;
    }

    /// <summary>Also requires a bool parameter to have a value.</summary>
    public AnimTransition WhenBool(string parameter, bool value)
    {
        Conditions.Add(new Condition(_owner.Index(parameter), value ? Op.True : Op.False, 0f));
        return this;
    }

    /// <summary>Also requires a trigger to be set. The transition that fires clears it, so one trigger starts one transition.</summary>
    public AnimTransition WhenTrigger(string trigger)
    {
        Conditions.Add(new Condition(_owner.Index(trigger), Op.Trigger, 0f));
        return this;
    }

    /// <summary>Waits until the source state has played this much (1 = one full pass). With no other condition the transition fires then.</summary>
    public AnimTransition AtExit(float progress = 1f)
    {
        ExitTime = progress;
        return this;
    }

    /// <summary>Sets the crossfade length.</summary>
    public AnimTransition Fade(float seconds)
    {
        FadeSeconds = Math.Max(0f, seconds);
        return this;
    }

    internal enum Op
    {
        Greater,
        Less,
        True,
        False,
        Trigger,
    }

    internal readonly record struct Condition(int Parameter, Op Op, float Value);
}

/// <summary>
/// The happy path for animating a character: states that play clips or blends, parameters that steer them, transitions with crossfades,
/// and the finished <see cref="Pose"/> to hand to the scene. A view sets parameters from synced state and calls <see cref="UpdateTo"/> once per frame.
/// It reads state only; it must not be used from sim code (rule OG0023). After construction an update allocates nothing.
/// </summary>
/// <example>
/// <code>
/// var anim = new Animator(rig, rootNode: rig.Node("Root"));
/// anim.AddFloat("speed").AddTrigger("jump");
/// anim.AddState("move", new BlendSpace1DMotion(new BlendSpace1D(0, 1, 2), idle, walk, run) { XParameter = "speed" });
/// anim.AddState("jump", new ClipMotion(jump), loop: false);
/// anim.Transition("move", "jump").WhenTrigger("jump").Fade(0.1f);
/// anim.Transition("jump", "move").AtExit().Fade(0.15f);
/// // each frame:  anim.SetFloat("speed", gait); anim.UpdateTo(time); scene.Model("hero", transform, anim.Pose);
/// </code>
/// </example>
public sealed class Animator
{
    private const float Step = 1f / 60f;
    private const double MaxReplay = 0.25;
    private readonly Rig _rig;
    private readonly List<string> _names = [];
    private readonly Dictionary<string, int> _index = new(StringComparer.Ordinal);
    private readonly List<byte> _kinds = [];
    private float[] _values = new float[8];
    private float[] _goal = new float[8];
    private float[] _rate = new float[8];
    private readonly List<AnimState> _states = [];
    private readonly List<AnimTransition> _any = [];
    private readonly NodePose _from;
    private readonly NodePose _scratch;
    private AnimState? _current;
    private AnimState? _previous;
    private Crossfade _fade;
    private double _clock = double.NaN;

    /// <summary>Creates an animator for a rig.</summary>
    /// <param name="rig">The skeleton and clips.</param>
    /// <param name="rootNode">The node whose translation carries root motion, or -1 for none. Unless <see cref="KeepRootMotion"/> is set, its translation is held at rest so the character stays under its entity while the sim moves it.</param>
    public Animator(Rig rig, int rootNode = -1)
    {
        ArgumentNullException.ThrowIfNull(rig);
        _rig = rig;
        RootNode = rootNode;
        Pose = rig.CreatePose();
        _from = rig.CreatePose();
        _scratch = rig.CreatePose();
    }

    /// <summary>The finished pose of the last update: pass it to <c>SceneWriter.Model</c> or copy it with <see cref="NodePose.CopyFrom"/>.</summary>
    public NodePose Pose { get; }

    /// <summary>The root node, or -1.</summary>
    public int RootNode { get; }

    /// <summary>True to leave the root node's own movement in the pose (for a character that has no sim moving it).</summary>
    public bool KeepRootMotion { get; set; }

    /// <summary>The name of the state being played (the one being entered during a crossfade), or empty before the first update.</summary>
    public string CurrentState => _current?.Name ?? string.Empty;

    /// <summary>True while a crossfade is running.</summary>
    public bool IsFading => _previous is not null;

    /// <summary>Progress of the current state (see <see cref="AnimState.Progress"/>).</summary>
    public float StateProgress => _current?.Progress ?? 0f;

    /// <summary>Declares a float parameter (initially 0).</summary>
    public Animator AddFloat(string name) => Declare(name, 0);

    /// <summary>Declares a bool parameter (initially false).</summary>
    public Animator AddBool(string name) => Declare(name, 1);

    /// <summary>Declares a trigger: a flag that stays set until a transition that waits for it fires.</summary>
    public Animator AddTrigger(string name) => Declare(name, 2);

    /// <summary>Adds a state. The first state added is where the animator starts.</summary>
    /// <param name="name">A unique name.</param>
    /// <param name="motion">What it plays. Parameters it names must be declared first.</param>
    /// <param name="loop">Whether it repeats.</param>
    /// <param name="speed">Playback speed factor.</param>
    public AnimState AddState(string name, Motion motion, bool loop = true, float speed = 1f)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(motion);
        if (_states.Exists(s => s.Name == name))
        {
            throw new ArgumentException($"A state named '{name}' already exists.", nameof(name));
        }

        var x = motion.XParameter is null ? -1 : Index(motion.XParameter);
        var y = motion.YParameter is null ? -1 : Index(motion.YParameter);
        var state = new AnimState(name, motion, loop, speed, x, y);
        _states.Add(state);
        return state;
    }

    /// <summary>Adds a transition between two states, or from every state when <paramref name="from"/> is null. The first transition whose conditions hold fires.</summary>
    /// <param name="from">The state to leave, or null.</param>
    /// <param name="to">The state to enter.</param>
    /// <exception cref="UnknownAnimatorNameException">A state name does not exist.</exception>
    public AnimTransition Transition(string? from, string to)
    {
        var t = new AnimTransition(this, from is null ? null : State(from), State(to));
        (t.From is null ? _any : t.From.Transitions).Add(t);
        return t;
    }

    /// <summary>Sets a float parameter at once.</summary>
    /// <exception cref="UnknownAnimatorNameException">There is no such parameter.</exception>
    public void SetFloat(string name, float value)
    {
        var i = Index(name);
        _values[i] = value;
        _rate[i] = -1f;
    }

    /// <summary>Moves a float parameter towards a goal at a steady rate while the animator updates, so a gait that changes from 1 to 2 blends over time instead of jumping. Call it every frame with the current goal.</summary>
    /// <param name="name">The parameter.</param>
    /// <param name="goal">The value to reach.</param>
    /// <param name="perSecond">The most the value changes in a second.</param>
    public void MoveFloat(string name, float goal, float perSecond)
    {
        var i = Index(name);
        _goal[i] = goal;
        _rate[i] = Math.Max(0f, perSecond);
    }

    /// <summary>Sets a bool parameter.</summary>
    /// <exception cref="UnknownAnimatorNameException">There is no such parameter.</exception>
    public void SetBool(string name, bool value) => _values[Index(name)] = value ? 1f : 0f;

    /// <summary>Sets a trigger.</summary>
    /// <exception cref="UnknownAnimatorNameException">There is no such trigger.</exception>
    public void Trigger(string name) => _values[Index(name)] = 1f;

    /// <summary>Reads a float parameter.</summary>
    public float GetFloat(string name) => _values[Index(name)];

    /// <summary>Reads a bool parameter or whether a trigger is set.</summary>
    public bool GetBool(string name) => _values[Index(name)] != 0f;

    /// <summary>Starts a crossfade into a state at once, whatever the transitions say.</summary>
    /// <param name="state">The state's name.</param>
    /// <param name="seconds">Fade length.</param>
    public void CrossFade(string state, float seconds)
    {
        EnsureStarted();
        Enter(State(state), seconds);
    }

    /// <summary>
    /// Advances to a running time. Short gaps (up to a quarter second) are played in fixed 1/60 s steps, so the result depends on the time and
    /// the parameters, not on the frame rate. A longer gap (a capture after many ticks, a paused window), a time that goes backwards (a
    /// restore) or the first call does not replay what was missed: the clock moves, parameters being moved by <see cref="MoveFloat"/> settle on their goals,
    /// and the pose is made from where the animator stands. To show an exact moment of a state after such a gap, call <see cref="Play"/>.
    /// </summary>
    /// <param name="seconds">A running time, for example <c>(scene.Tick + scene.Alpha) / 60.0</c>; the same value again changes nothing.</param>
    public void UpdateTo(double seconds)
    {
        if (double.IsNaN(_clock) || seconds < _clock || seconds - _clock > MaxReplay)
        {
            _clock = seconds;
            for (var i = 0; i < _names.Count; i++)
            {
                if (_rate[i] >= 0f)
                {
                    _values[i] = _goal[i];
                }
            }

            Update(0f);
            return;
        }

        var steps = (int)((seconds - _clock) / Step);
        for (var i = 0; i < steps; i++)
        {
            Update(Step);
        }

        _clock += steps * (double)Step;
        if (steps == 0)
        {
            Update(0f);
        }
    }

    /// <summary>Cuts to a state at a point in it, with no crossfade, and recomputes <see cref="Pose"/>.</summary>
    /// <param name="state">The state's name.</param>
    /// <param name="progress">Passes through the state's motion so far (0.5 = halfway through the first pass).</param>
    /// <exception cref="UnknownAnimatorNameException">There is no such state.</exception>
    public void Play(string state, float progress = 0f)
    {
        EnsureStarted();
        _current = State(state);
        _current.Progress = Math.Max(0f, progress);
        _previous = null;
        Compose();
    }

    /// <summary>Advances by a time step and recomputes <see cref="Pose"/>.</summary>
    /// <param name="dt">Seconds, not negative.</param>
    public void Update(float dt)
    {
        EnsureStarted();
        dt = Math.Max(0f, dt);
        for (var i = 0; i < _names.Count; i++)
        {
            if (_rate[i] >= 0f)
            {
                var step = _rate[i] * dt;
                _values[i] = Math.Abs(_goal[i] - _values[i]) <= step ? _goal[i] : _values[i] + (MathF.Sign(_goal[i] - _values[i]) * step);
            }
        }

        var current = _current!;
        Feed(current);
        if (_previous is { } previous)
        {
            Feed(previous);
            Advance(previous, dt);
            _fade.Advance(dt);
            if (_fade.Done)
            {
                _previous = null;
            }
        }

        Advance(current, dt);
        if (_previous is null)
        {
            Evaluate(current);
        }

        Compose();
    }

    /// <summary>The state names in the order they were added.</summary>
    public IEnumerable<string> States => _states.Select(s => s.Name);

    internal int Index(string name)
        => _index.TryGetValue(name, out var i) ? i : throw new UnknownAnimatorNameException("parameter", name, _names);

    private Animator Declare(string name, byte kind)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        if (_index.ContainsKey(name))
        {
            throw new ArgumentException($"A parameter named '{name}' already exists.", nameof(name));
        }

        _index[name] = _names.Count;
        _names.Add(name);
        _kinds.Add(kind);
        if (_names.Count > _values.Length)
        {
            Array.Resize(ref _values, _values.Length * 2);
            Array.Resize(ref _goal, _values.Length);
            Array.Resize(ref _rate, _values.Length);
        }

        _rate[_names.Count - 1] = -1f;

        return this;
    }

    private AnimState State(string name)
    {
        for (var i = 0; i < _states.Count; i++)
        {
            if (_states[i].Name == name)
            {
                return _states[i];
            }
        }

        throw new UnknownAnimatorNameException("state", name, States);
    }

    private void EnsureStarted()
    {
        if (_current is null)
        {
            if (_states.Count == 0)
            {
                throw new InvalidOperationException("The animator has no states. Add one with AddState before updating.");
            }

            _current = _states[0];
            _current.Progress = 0f;
        }
    }

    private void Feed(AnimState s)
    {
        if (s.XParameter >= 0)
        {
            s.Motion.X = _values[s.XParameter];
        }

        if (s.YParameter >= 0)
        {
            s.Motion.Y = _values[s.YParameter];
        }
    }

    private static void Advance(AnimState s, float dt)
        => s.Progress += dt * s.Speed / s.Motion.Duration;

    private void Evaluate(AnimState current)
    {
        for (var pass = 0; pass < 2; pass++)
        {
            var list = pass == 0 ? current.Transitions : _any;
            for (var i = 0; i < list.Count; i++)
            {
                var t = list[i];
                if (ReferenceEquals(t.To, current) || !Holds(t, current))
                {
                    continue;
                }

                for (var c = 0; c < t.Conditions.Count; c++)
                {
                    if (t.Conditions[c].Op == AnimTransition.Op.Trigger)
                    {
                        _values[t.Conditions[c].Parameter] = 0f;
                    }
                }

                Enter(t.To, t.FadeSeconds);
                return;
            }
        }
    }

    private bool Holds(AnimTransition t, AnimState current)
    {
        if (t.ExitTime >= 0f && current.Progress < t.ExitTime)
        {
            return false;
        }

        for (var c = 0; c < t.Conditions.Count; c++)
        {
            var cond = t.Conditions[c];
            var v = _values[cond.Parameter];
            var ok = cond.Op switch
            {
                AnimTransition.Op.Greater => v > cond.Value,
                AnimTransition.Op.Less => v < cond.Value,
                AnimTransition.Op.False => v == 0f,
                _ => v != 0f,
            };
            if (!ok)
            {
                return false;
            }
        }

        return true;
    }

    private void Enter(AnimState next, float fadeSeconds)
    {
        if (ReferenceEquals(next, _current))
        {
            return;
        }

        _previous = fadeSeconds > 0f ? _current : null;
        _current = next;
        next.Progress = 0f;
        _fade = new Crossfade(fadeSeconds);
    }

    private void Compose()
    {
        var current = _current!;
        Feed(current);
        current.Motion.Sample(_rig, current.Phase, Pose, _scratch);
        if (_previous is { } previous)
        {
            previous.Motion.Sample(_rig, previous.Phase, _from, _scratch);
            PoseBlend.Lerp(Pose, _from, Pose, _fade.Weight);
        }

        if (RootNode >= 0 && !KeepRootMotion)
        {
            Pose.Translation[RootNode] = _rig.RestTranslation(RootNode);
        }
    }
}
