using System.Numerics;

namespace Lanterns.Views;

/// <summary>What the view remembers about one walker between frames. View state only: nothing here is synced or recorded.</summary>
internal sealed class Track
{
    /// <summary>Where the walker was drawn one tick ago, in metres on the ground (x, z).</summary>
    public Vector2 Prev;

    /// <summary>Where it is drawn as of the current tick.</summary>
    public Vector2 Shown;

    /// <summary>Where the state says it is.</summary>
    public Vector2 Target;

    /// <summary>Heading in radians (the way the model faces).</summary>
    public float Yaw;

    /// <summary>Metres walked, for the stride.</summary>
    public float Phase;

    /// <summary>Metres moved in the last tick.</summary>
    public float Speed;

    /// <summary>True while the walker is gliding over a gap bigger than a step (a hand-over to the other island).</summary>
    public bool Glide;

    /// <summary>The island frame the target is in, to notice a hand-over.</summary>
    public uint Frame;

    /// <summary>Which pass over the frame last saw it.</summary>
    public int Seen;

    /// <summary>Where to draw it between two ticks.</summary>
    public Vector2 At(float alpha) => Vector2.Lerp(Prev, Shown, Math.Clamp(alpha, 0f, 1f));
}

/// <summary>
/// Turns the positions the state reports into the positions the screen shows. A predicted walker whose correction arrives is not moved by
/// the whole difference in one frame: the drawn position chases the true one, a little each tick, so a correction is a short bend in the
/// path instead of a pop. Steady walking trails the true position by a fraction of a step. The same follows the camera. Time here is the
/// tick, never the clock, so a capture after N ticks always looks the same.
/// </summary>
internal sealed class Motion
{
    /// <summary>Share of the remaining gap closed each tick while walking.</summary>
    public const float Chase = 0.4f;

    /// <summary>Share the camera closes each tick.</summary>
    public const float CameraChase = 0.12f;

    /// <summary>The camera never moves further than this in one tick, so a hand-over to the other island is a sweep, not a cut.</summary>
    public const float MaxCameraStep = 0.5f;

    /// <summary>Metres per tick covered while gliding across the bridge hand-over.</summary>
    public const float GlideStep = 0.3f;

    // After this many ticks without a look (a paused window, a long scenario step) the picture snaps rather than replaying them.
    private const int MaxReplay = 8;

    // A walker that is in neither island's copy for a moment (the hand-over takes it out of one and into the other) keeps its place here
    // for this many ticks, so it glides on from where it was instead of appearing out of nowhere.
    private const int Linger = 120;

    private readonly Dictionary<uint, Track> _tracks = [];
    private readonly List<uint> _gone = [];
    private readonly Track _camera = new();
    private long _tick = long.MinValue;
    private int _pass;
    private int _steps;
    private bool _snap;
    private bool _cameraSeen;

    /// <summary>The camera focus, which trails the walker it follows.</summary>
    public Track Camera => _camera;

    /// <summary>True when the current frame moved on by at least one tick.</summary>
    public bool Advancing => _steps > 0;

    /// <summary>Forgets everything: the next frame shows every walker and the camera exactly where the state puts them.</summary>
    public void Reset()
    {
        _tracks.Clear();
        _tick = long.MinValue;
        _cameraSeen = false;
        _steps = 0;
    }

    /// <summary>Starts a frame. Calls with the same tick change nothing, so asking twice in a frame is harmless.</summary>
    public void Begin(long tick)
    {
        if (tick == _tick)
        {
            _steps = 0;
            _snap = false;
            return;
        }

        var d = tick - _tick;
        _snap = _tick == long.MinValue || d < 0 || d > MaxReplay;
        _steps = _snap ? 1 : (int)d;
        _tick = tick;
        _pass++;
    }

    /// <summary>Notes a walker at its true position this frame and returns what to draw.</summary>
    public Track Touch(uint who, Vector2 target, uint frame, int dx, int dy)
    {
        if (!_tracks.TryGetValue(who, out var t))
        {
            t = new Track { Prev = target, Shown = target, Target = target, Frame = frame };
            _tracks[who] = t;
            t.Yaw = Heading(dx, dy, 0f);
        }

        t.Seen = _pass;
        t.Target = target;
        if (_steps == 0)
        {
            return t;
        }

        if (t.Frame != frame)
        {
            t.Frame = frame;
            t.Glide = !_snap;
        }

        var yawTarget = Heading(dx, dy, t.Yaw);
        for (var i = 0; i < _steps; i++)
        {
            t.Prev = t.Shown;
            var before = t.Shown;
            if (_snap)
            {
                t.Shown = target;
                t.Prev = target;
            }
            else if (t.Glide)
            {
                var to = target - t.Shown;
                var dist = to.Length();
                t.Shown = dist <= GlideStep ? target : t.Shown + (to / dist * GlideStep);
                t.Glide = Vector2.Distance(t.Shown, target) > 0.05f;
            }
            else
            {
                t.Shown += (target - t.Shown) * Chase;
            }

            var moved = Vector2.Distance(before, t.Shown);
            t.Speed = moved;
            t.Phase += moved;
            t.Yaw += Wrap(yawTarget - t.Yaw) * (_snap ? 1f : 0.35f);
        }

        return t;
    }

    /// <summary>Moves the camera focus towards a point (in the same frame of reference as the walkers).</summary>
    public Track Follow(Vector2 target)
    {
        if (!_cameraSeen)
        {
            _cameraSeen = true;
            _camera.Prev = _camera.Shown = target;
        }

        _camera.Target = target;
        for (var i = 0; i < _steps; i++)
        {
            _camera.Prev = _snap ? target : _camera.Shown;
            if (_snap)
            {
                _camera.Shown = target;
            }
            else
            {
                var step = (target - _camera.Shown) * CameraChase;
                var length = step.Length();
                _camera.Shown += length > MaxCameraStep ? step / length * MaxCameraStep : step;
            }
        }

        return _camera;
    }

    /// <summary>Ends a frame: forgets walkers that have not been seen for a while.</summary>
    public void End()
    {
        if (_steps == 0)
        {
            return;
        }

        _gone.Clear();
        foreach (var (who, t) in _tracks)
        {
            if (_pass - t.Seen > Linger)
            {
                _gone.Add(who);
            }
        }

        foreach (var who in _gone)
        {
            _tracks.Remove(who);
        }
    }

    /// <summary>The track of a walker, if it has been seen.</summary>
    public Track? Get(uint who) => _tracks.TryGetValue(who, out var t) ? t : null;

    private static float Heading(int dx, int dy, float keep) =>
        dx == 0 && dy == 0 ? keep : MathF.Atan2(-dx, -dy);

    private static float Wrap(float a)
    {
        while (a > MathF.PI)
        {
            a -= MathF.Tau;
        }

        while (a < -MathF.PI)
        {
            a += MathF.Tau;
        }

        return a;
    }
}
