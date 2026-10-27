using System.Globalization;

namespace OneGame.Tweens;

/// <summary>The shape family of a named easing curve (see <see cref="Easing.Of"/>).</summary>
public enum EasingFamily : byte
{
    /// <summary>A quarter sine wave: the gentlest curve.</summary>
    Sine,

    /// <summary>t squared.</summary>
    Quad,

    /// <summary>t cubed.</summary>
    Cubic,

    /// <summary>t to the fourth.</summary>
    Quart,

    /// <summary>t to the fifth.</summary>
    Quint,

    /// <summary>An exponential: almost flat, then a sharp rise (or the reverse).</summary>
    Expo,

    /// <summary>A circle's arc.</summary>
    Circ,

    /// <summary>Pulls back past the start (or overshoots the end) before settling.</summary>
    Back,

    /// <summary>A damped spring: overshoots and rings.</summary>
    Elastic,

    /// <summary>Bounces off the end like a dropped ball.</summary>
    Bounce,
}

/// <summary>Which end of a named easing curve is eased.</summary>
public enum EasingMode : byte
{
    /// <summary>Starts slowly, ends at full speed.</summary>
    In,

    /// <summary>Starts at full speed, ends slowly.</summary>
    Out,

    /// <summary><see cref="In"/> for the first half and <see cref="Out"/> for the second.</summary>
    InOut,
}

/// <summary>Where a <see cref="Easing.Steps"/> function jumps (CSS Easing Functions Level 1, §2.3).</summary>
public enum StepJump : byte
{
    /// <summary>The last rise happens at the very end (CSS <c>jump-end</c>, the default).</summary>
    End,

    /// <summary>The first rise happens at the very start (CSS <c>jump-start</c>).</summary>
    Start,

    /// <summary>All rises happen strictly inside the range (CSS <c>jump-none</c>); needs at least 2 steps.</summary>
    None,

    /// <summary>A rise at both the start and the end (CSS <c>jump-both</c>).</summary>
    Both,
}

/// <summary>
/// An easing function: maps progress through a tween (0 at the start, 1 at the end) to how far along the value is. Linear, the CSS
/// timing functions (<c>ease</c>, <c>cubic-bezier()</c>, <c>steps()</c>; CSS Easing Functions Level 1) and the common named in/out/in-out
/// families are all values of this one small struct, so evaluating one never allocates. <c>default(Easing)</c> is <see cref="Linear"/>.
/// </summary>
public readonly struct Easing : IEquatable<Easing>
{
    private enum Shape : byte
    {
        Linear,
        Bezier,
        Steps,
        Family,
    }

    private readonly Shape _shape;
    private readonly byte _a;
    private readonly byte _b;
    private readonly int _steps;
    private readonly float _x1;
    private readonly float _y1;
    private readonly float _x2;
    private readonly float _y2;

    private Easing(Shape shape, byte a, byte b, int steps, float x1, float y1, float x2, float y2)
    {
        _shape = shape;
        _a = a;
        _b = b;
        _steps = steps;
        _x1 = x1;
        _y1 = y1;
        _x2 = x2;
        _y2 = y2;
    }

    /// <summary>Constant speed (CSS <c>linear</c>).</summary>
    public static Easing Linear => default;

    /// <summary>CSS <c>ease</c>: <c>cubic-bezier(0.25, 0.1, 0.25, 1)</c>, the default of CSS transitions.</summary>
    public static Easing Ease { get; } = CubicBezier(0.25f, 0.1f, 0.25f, 1f);

    /// <summary>CSS <c>ease-in</c>: <c>cubic-bezier(0.42, 0, 1, 1)</c>.</summary>
    public static Easing EaseIn { get; } = CubicBezier(0.42f, 0f, 1f, 1f);

    /// <summary>CSS <c>ease-out</c>: <c>cubic-bezier(0, 0, 0.58, 1)</c>.</summary>
    public static Easing EaseOut { get; } = CubicBezier(0f, 0f, 0.58f, 1f);

    /// <summary>CSS <c>ease-in-out</c>: <c>cubic-bezier(0.42, 0, 0.58, 1)</c>.</summary>
    public static Easing EaseInOut { get; } = CubicBezier(0.42f, 0f, 0.58f, 1f);

    /// <summary>CSS <c>step-start</c>: <c>steps(1, jump-start)</c>.</summary>
    public static Easing StepStart => Steps(1, StepJump.Start);

    /// <summary>CSS <c>step-end</c>: <c>steps(1, jump-end)</c>.</summary>
    public static Easing StepEnd => Steps(1, StepJump.End);

    /// <summary>A named curve, such as <c>Easing.Of(EasingFamily.Cubic, EasingMode.Out)</c>.</summary>
    /// <param name="family">The curve's shape.</param>
    /// <param name="mode">Which end is eased.</param>
    /// <exception cref="ArgumentOutOfRangeException">The family or mode is not a defined value.</exception>
    public static Easing Of(EasingFamily family, EasingMode mode)
    {
        if (family > EasingFamily.Bounce)
        {
            throw new ArgumentOutOfRangeException(nameof(family), family, "Not a known easing family.");
        }

        if (mode > EasingMode.InOut)
        {
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Not a known easing mode.");
        }

        return new Easing(Shape.Family, (byte)family, (byte)mode, 0, 0, 0, 0, 0);
    }

    /// <summary>Sine, eased at the start.</summary>
    public static Easing InSine => Of(EasingFamily.Sine, EasingMode.In);

    /// <summary>Sine, eased at the end.</summary>
    public static Easing OutSine => Of(EasingFamily.Sine, EasingMode.Out);

    /// <summary>Sine, eased at both ends.</summary>
    public static Easing InOutSine => Of(EasingFamily.Sine, EasingMode.InOut);

    /// <summary>Quadratic, eased at the start.</summary>
    public static Easing InQuad => Of(EasingFamily.Quad, EasingMode.In);

    /// <summary>Quadratic, eased at the end.</summary>
    public static Easing OutQuad => Of(EasingFamily.Quad, EasingMode.Out);

    /// <summary>Quadratic, eased at both ends.</summary>
    public static Easing InOutQuad => Of(EasingFamily.Quad, EasingMode.InOut);

    /// <summary>Cubic, eased at the start.</summary>
    public static Easing InCubic => Of(EasingFamily.Cubic, EasingMode.In);

    /// <summary>Cubic, eased at the end: the usual choice for things that slide into place.</summary>
    public static Easing OutCubic => Of(EasingFamily.Cubic, EasingMode.Out);

    /// <summary>Cubic, eased at both ends.</summary>
    public static Easing InOutCubic => Of(EasingFamily.Cubic, EasingMode.InOut);

    /// <summary>Quartic, eased at the start.</summary>
    public static Easing InQuart => Of(EasingFamily.Quart, EasingMode.In);

    /// <summary>Quartic, eased at the end.</summary>
    public static Easing OutQuart => Of(EasingFamily.Quart, EasingMode.Out);

    /// <summary>Quartic, eased at both ends.</summary>
    public static Easing InOutQuart => Of(EasingFamily.Quart, EasingMode.InOut);

    /// <summary>Quintic, eased at the start.</summary>
    public static Easing InQuint => Of(EasingFamily.Quint, EasingMode.In);

    /// <summary>Quintic, eased at the end.</summary>
    public static Easing OutQuint => Of(EasingFamily.Quint, EasingMode.Out);

    /// <summary>Quintic, eased at both ends.</summary>
    public static Easing InOutQuint => Of(EasingFamily.Quint, EasingMode.InOut);

    /// <summary>Exponential, eased at the start.</summary>
    public static Easing InExpo => Of(EasingFamily.Expo, EasingMode.In);

    /// <summary>Exponential, eased at the end.</summary>
    public static Easing OutExpo => Of(EasingFamily.Expo, EasingMode.Out);

    /// <summary>Exponential, eased at both ends.</summary>
    public static Easing InOutExpo => Of(EasingFamily.Expo, EasingMode.InOut);

    /// <summary>Circular, eased at the start.</summary>
    public static Easing InCirc => Of(EasingFamily.Circ, EasingMode.In);

    /// <summary>Circular, eased at the end.</summary>
    public static Easing OutCirc => Of(EasingFamily.Circ, EasingMode.Out);

    /// <summary>Circular, eased at both ends.</summary>
    public static Easing InOutCirc => Of(EasingFamily.Circ, EasingMode.InOut);

    /// <summary>Pulls back before starting.</summary>
    public static Easing InBack => Of(EasingFamily.Back, EasingMode.In);

    /// <summary>Overshoots the end and settles back: a good pop for a card landing.</summary>
    public static Easing OutBack => Of(EasingFamily.Back, EasingMode.Out);

    /// <summary>Pulls back at the start and overshoots at the end.</summary>
    public static Easing InOutBack => Of(EasingFamily.Back, EasingMode.InOut);

    /// <summary>A spring winding up at the start.</summary>
    public static Easing InElastic => Of(EasingFamily.Elastic, EasingMode.In);

    /// <summary>A spring that rings around the end value.</summary>
    public static Easing OutElastic => Of(EasingFamily.Elastic, EasingMode.Out);

    /// <summary>A spring at both ends.</summary>
    public static Easing InOutElastic => Of(EasingFamily.Elastic, EasingMode.InOut);

    /// <summary>Bounces away from the start.</summary>
    public static Easing InBounce => Of(EasingFamily.Bounce, EasingMode.In);

    /// <summary>Bounces on the end like a dropped ball.</summary>
    public static Easing OutBounce => Of(EasingFamily.Bounce, EasingMode.Out);

    /// <summary>Bounces at both ends.</summary>
    public static Easing InOutBounce => Of(EasingFamily.Bounce, EasingMode.InOut);

    /// <summary>A cubic Bézier curve through (0,0) and (1,1) with control points (x1,y1) and (x2,y2), as CSS <c>cubic-bezier()</c>.</summary>
    /// <param name="x1">First control point x, from 0 to 1.</param>
    /// <param name="y1">First control point y; may leave 0..1 for overshoot.</param>
    /// <param name="x2">Second control point x, from 0 to 1.</param>
    /// <param name="y2">Second control point y; may leave 0..1 for overshoot.</param>
    /// <exception cref="ArgumentOutOfRangeException">An x value is outside 0..1 (CSS makes such a curve invalid) or a value is not finite.</exception>
    public static Easing CubicBezier(float x1, float y1, float x2, float y2)
    {
        if (!(x1 >= 0 && x1 <= 1))
        {
            throw new ArgumentOutOfRangeException(nameof(x1), x1, "A cubic-bezier x value must be between 0 and 1.");
        }

        if (!(x2 >= 0 && x2 <= 1))
        {
            throw new ArgumentOutOfRangeException(nameof(x2), x2, "A cubic-bezier x value must be between 0 and 1.");
        }

        if (!float.IsFinite(y1) || !float.IsFinite(y2))
        {
            throw new ArgumentOutOfRangeException(nameof(y1), "A cubic-bezier y value must be finite.");
        }

        return new Easing(Shape.Bezier, 0, 0, 0, x1, y1, x2, y2);
    }

    /// <summary>A staircase of <paramref name="count"/> equal steps, as CSS <c>steps()</c>.</summary>
    /// <param name="count">The number of intervals; at least 1, or 2 with <see cref="StepJump.None"/>.</param>
    /// <param name="jump">Where the jumps fall.</param>
    /// <exception cref="ArgumentOutOfRangeException">The count is too small or too large (above 4096), or the jump is not defined.</exception>
    public static Easing Steps(int count, StepJump jump = StepJump.End)
    {
        if (jump > StepJump.Both)
        {
            throw new ArgumentOutOfRangeException(nameof(jump), jump, "Not a known step position.");
        }

        if (count < (jump == StepJump.None ? 2 : 1) || count > 4096)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Steps needs a count from 1 (2 for StepJump.None) to 4096.");
        }

        return new Easing(Shape.Steps, (byte)jump, 0, count, 0, 0, 0, 0);
    }

    /// <summary>
    /// Maps progress to eased progress. Input outside 0..1 is allowed: Bézier curves continue along their end tangents (CSS Easing §2.2.1),
    /// named curves and steps hold their end values. Output may leave 0..1 for curves that overshoot.
    /// </summary>
    /// <param name="t">Progress through the tween, normally 0 to 1.</param>
    public float Evaluate(float t) => _shape switch
    {
        Shape.Bezier => (float)Bezier(t),
        Shape.Steps => StepsAt(t),
        Shape.Family => t <= 0 ? 0 : t >= 1 ? 1 : (float)Family((EasingFamily)_a, (EasingMode)_b, t),
        _ => t,
    };

    // CSS Easing §2.3.1, with the before flag clear.
    private float StepsAt(float t)
    {
        var jump = (StepJump)_a;
        var step = Math.Floor(t * (double)_steps);
        if (jump is StepJump.Start or StepJump.Both)
        {
            step += 1;
        }

        if (t >= 0 && step < 0)
        {
            step = 0;
        }

        var jumps = jump switch
        {
            StepJump.None => _steps - 1,
            StepJump.Both => _steps + 1,
            _ => _steps,
        };
        if (t <= 1 && step > jumps)
        {
            step = jumps;
        }

        return (float)(step / jumps);
    }

    // CSS Easing §2.2.1: solve x(s) = t for the curve parameter, return y(s); beyond [0,1] follow the end tangents.
    private double Bezier(double t)
    {
        double x1 = _x1, y1 = _y1, x2 = _x2, y2 = _y2;
        if (t < 0)
        {
            return x1 > 0 ? t * y1 / x1 : x2 > 0 ? t * y2 / x2 : 0;
        }

        if (t > 1)
        {
            return x2 < 1 ? 1 + ((t - 1) * (y2 - 1) / (x2 - 1)) : x1 < 1 ? 1 + ((t - 1) * (y1 - 1) / (x1 - 1)) : 1;
        }

        if (t == 0 || t == 1)
        {
            return t;
        }

        // x(s) = 3(1-s)^2 s x1 + 3(1-s) s^2 x2 + s^3, written as a polynomial in s.
        var cx = 3 * x1;
        var bx = (3 * (x2 - x1)) - cx;
        var ax = 1 - cx - bx;
        var s = t;
        for (var i = 0; i < 8; i++)
        {
            var err = (((((ax * s) + bx) * s) + cx) * s) - t;
            if (Math.Abs(err) < 1e-9)
            {
                return Y(s);
            }

            var d = (((3 * ax * s) + (2 * bx)) * s) + cx;
            if (Math.Abs(d) < 1e-7)
            {
                break;
            }

            s -= err / d;
        }

        // Newton failed to settle (a nearly flat part of the curve): bisection always converges because x(s) is monotonic for x1, x2 in [0,1].
        double lo = 0, hi = 1;
        s = t;
        for (var i = 0; i < 60; i++)
        {
            var x = (((((ax * s) + bx) * s) + cx) * s);
            if (Math.Abs(x - t) < 1e-9)
            {
                break;
            }

            if (x < t)
            {
                lo = s;
            }
            else
            {
                hi = s;
            }

            s = (lo + hi) / 2;
        }

        return Y(s);

        double Y(double p)
        {
            var cy = 3 * y1;
            var by = (3 * (y2 - y1)) - cy;
            var ay = 1 - cy - by;
            return (((((ay * p) + by) * p) + cy) * p);
        }
    }

    // The widely used closed forms of the in/out/in-out families (t is strictly inside 0..1 here). Out and in-out are the mirror images of In,
    // except for Back and Elastic, whose usual in-out forms use their own constants.
    private static double Family(EasingFamily family, EasingMode mode, double t)
    {
        const double C1 = 1.70158;
        const double C3 = C1 + 1;
        if (family == EasingFamily.Back && mode == EasingMode.Out)
        {
            return 1 + (C3 * Math.Pow(t - 1, 3)) + (C1 * Math.Pow(t - 1, 2));
        }

        if (family == EasingFamily.Back && mode == EasingMode.InOut)
        {
            const double C2 = C1 * 1.525;
            return t < 0.5
                ? Math.Pow(2 * t, 2) * (((C2 + 1) * 2 * t) - C2) / 2
                : ((Math.Pow((2 * t) - 2, 2) * (((C2 + 1) * ((t * 2) - 2)) + C2)) + 2) / 2;
        }

        if (family == EasingFamily.Elastic && mode == EasingMode.Out)
        {
            return (Math.Pow(2, -10 * t) * Math.Sin(((10 * t) - 0.75) * (2 * Math.PI / 3))) + 1;
        }

        if (family == EasingFamily.Elastic && mode == EasingMode.InOut)
        {
            const double C5 = 2 * Math.PI / 4.5;
            var s = Math.Sin(((20 * t) - 11.125) * C5);
            return t < 0.5 ? -(Math.Pow(2, (20 * t) - 10) * s) / 2 : (Math.Pow(2, (-20 * t) + 10) * s / 2) + 1;
        }

        switch (mode)
        {
            case EasingMode.In:
                return In(family, t);
            case EasingMode.Out:
                return 1 - In(family, 1 - t);
            default:
                return t < 0.5 ? In(family, 2 * t) / 2 : 1 - (In(family, 2 - (2 * t)) / 2);
        }
    }

    private static double In(EasingFamily family, double t)
    {
        const double C1 = 1.70158;
        const double C3 = C1 + 1;
        switch (family)
        {
            case EasingFamily.Sine:
                return 1 - Math.Cos(t * Math.PI / 2);
            case EasingFamily.Quad:
                return t * t;
            case EasingFamily.Cubic:
                return t * t * t;
            case EasingFamily.Quart:
                return t * t * t * t;
            case EasingFamily.Quint:
                return t * t * t * t * t;
            case EasingFamily.Expo:
                return t <= 0 ? 0 : Math.Pow(2, (10 * t) - 10);
            case EasingFamily.Circ:
                return 1 - Math.Sqrt(1 - (t * t));
            case EasingFamily.Back:
                return (C3 * t * t * t) - (C1 * t * t);
            case EasingFamily.Elastic:
                return t <= 0 ? 0 : t >= 1 ? 1 : -Math.Pow(2, (10 * t) - 10) * Math.Sin(((10 * t) - 10.75) * (2 * Math.PI / 3));
            default:
                return 1 - BounceOut(1 - t);
        }
    }

    private static double BounceOut(double t)
    {
        const double N = 7.5625;
        const double D = 2.75;
        if (t < 1 / D)
        {
            return N * t * t;
        }

        if (t < 2 / D)
        {
            t -= 1.5 / D;
            return (N * t * t) + 0.75;
        }

        if (t < 2.5 / D)
        {
            t -= 2.25 / D;
            return (N * t * t) + 0.9375;
        }

        t -= 2.625 / D;
        return (N * t * t) + 0.984375;
    }

    // ---- text ----------------------------------------------------------------------------------------------------

    /// <summary>
    /// Reads an easing from CSS spelling: <c>linear</c>, <c>ease</c>, <c>ease-in</c>, <c>ease-out</c>, <c>ease-in-out</c>, <c>step-start</c>, <c>step-end</c>,
    /// <c>cubic-bezier(x1, y1, x2, y2)</c>, <c>steps(n[, jump-start|jump-end|jump-none|jump-both|start|end])</c>, plus the named curves as
    /// <c>in-quad</c>, <c>out-cubic</c>, <c>in-out-back</c> and so on. Keywords are not case sensitive.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="easing">The easing, when the text is valid.</param>
    /// <returns>False when the text is not an easing function or its arguments are out of range.</returns>
    public static bool TryParse(ReadOnlySpan<char> text, out Easing easing)
    {
        text = text.Trim();
        easing = default;
        if (text.IsEmpty)
        {
            return false;
        }

        var open = text.IndexOf('(');
        if (open >= 0)
        {
            if (text[^1] != ')')
            {
                return false;
            }

            var name = text[..open].Trim();
            var args = text[(open + 1)..^1];
            if (name.Equals("cubic-bezier", StringComparison.OrdinalIgnoreCase))
            {
                Span<float> v = stackalloc float[4];
                if (!ReadNumbers(args, v))
                {
                    return false;
                }

                if (!(v[0] >= 0 && v[0] <= 1) || !(v[2] >= 0 && v[2] <= 1))
                {
                    return false;
                }

                easing = CubicBezier(v[0], v[1], v[2], v[3]);
                return true;
            }

            if (name.Equals("steps", StringComparison.OrdinalIgnoreCase))
            {
                var comma = args.IndexOf(',');
                var countText = (comma < 0 ? args : args[..comma]).Trim();
                if (!int.TryParse(countText, NumberStyles.None, CultureInfo.InvariantCulture, out var count))
                {
                    return false;
                }

                var jump = StepJump.End;
                if (comma >= 0)
                {
                    var j = args[(comma + 1)..].Trim();
                    if (j.Equals("jump-start", StringComparison.OrdinalIgnoreCase) || j.Equals("start", StringComparison.OrdinalIgnoreCase))
                    {
                        jump = StepJump.Start;
                    }
                    else if (j.Equals("jump-end", StringComparison.OrdinalIgnoreCase) || j.Equals("end", StringComparison.OrdinalIgnoreCase))
                    {
                        jump = StepJump.End;
                    }
                    else if (j.Equals("jump-none", StringComparison.OrdinalIgnoreCase))
                    {
                        jump = StepJump.None;
                    }
                    else if (j.Equals("jump-both", StringComparison.OrdinalIgnoreCase))
                    {
                        jump = StepJump.Both;
                    }
                    else
                    {
                        return false;
                    }
                }

                if (count < (jump == StepJump.None ? 2 : 1) || count > 4096)
                {
                    return false;
                }

                easing = Steps(count, jump);
                return true;
            }

            return false;
        }

        return TryKeyword(text, out easing);
    }

    private static bool ReadNumbers(ReadOnlySpan<char> text, Span<float> into)
    {
        for (var i = 0; i < into.Length; i++)
        {
            var comma = text.IndexOf(',');
            var part = i == into.Length - 1 ? text : comma < 0 ? default : text[..comma];
            if ((i < into.Length - 1 && comma < 0) || !float.TryParse(part.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out into[i]) || !float.IsFinite(into[i]))
            {
                return false;
            }

            if (i < into.Length - 1)
            {
                text = text[(comma + 1)..];
            }
        }

        return true;
    }

    private static bool TryKeyword(ReadOnlySpan<char> text, out Easing easing)
    {
        easing = default;
        if (text.Equals("linear", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (text.Equals("ease", StringComparison.OrdinalIgnoreCase))
        {
            easing = Ease;
        }
        else if (text.Equals("ease-in", StringComparison.OrdinalIgnoreCase))
        {
            easing = EaseIn;
        }
        else if (text.Equals("ease-out", StringComparison.OrdinalIgnoreCase))
        {
            easing = EaseOut;
        }
        else if (text.Equals("ease-in-out", StringComparison.OrdinalIgnoreCase))
        {
            easing = EaseInOut;
        }
        else if (text.Equals("step-start", StringComparison.OrdinalIgnoreCase))
        {
            easing = StepStart;
        }
        else if (text.Equals("step-end", StringComparison.OrdinalIgnoreCase))
        {
            easing = StepEnd;
        }
        else
        {
            EasingMode mode;
            ReadOnlySpan<char> rest;
            if (text.StartsWith("in-out-", StringComparison.OrdinalIgnoreCase))
            {
                mode = EasingMode.InOut;
                rest = text[7..];
            }
            else if (text.StartsWith("in-", StringComparison.OrdinalIgnoreCase))
            {
                mode = EasingMode.In;
                rest = text[3..];
            }
            else if (text.StartsWith("out-", StringComparison.OrdinalIgnoreCase))
            {
                mode = EasingMode.Out;
                rest = text[4..];
            }
            else
            {
                return false;
            }

            for (var f = EasingFamily.Sine; f <= EasingFamily.Bounce; f++)
            {
                if (rest.Equals(f.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    easing = Of(f, mode);
                    return true;
                }
            }

            return false;
        }

        return true;
    }

    /// <summary>Reads an easing from CSS spelling; see <see cref="TryParse"/>.</summary>
    /// <param name="text">The text.</param>
    /// <exception cref="FormatException">The text is not an easing function.</exception>
    public static Easing Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return TryParse(text, out var e) ? e : throw new FormatException($"'{text}' is not an easing function. Use linear, ease, ease-in, ease-out, ease-in-out, step-start, step-end, cubic-bezier(x1, y1, x2, y2), steps(n[, jump]) or a named curve such as out-cubic.");
    }

    /// <summary>The CSS spelling, which <see cref="TryParse"/> reads back.</summary>
    public override string ToString()
    {
        switch (_shape)
        {
            case Shape.Bezier:
                if (this == Ease)
                {
                    return "ease";
                }

                if (this == EaseIn)
                {
                    return "ease-in";
                }

                if (this == EaseOut)
                {
                    return "ease-out";
                }

                if (this == EaseInOut)
                {
                    return "ease-in-out";
                }

                return string.Create(CultureInfo.InvariantCulture, $"cubic-bezier({_x1}, {_y1}, {_x2}, {_y2})");
            case Shape.Steps:
                var jump = (StepJump)_a;
                return jump == StepJump.End
                    ? string.Create(CultureInfo.InvariantCulture, $"steps({_steps})")
                    : string.Create(CultureInfo.InvariantCulture, $"steps({_steps}, {jump switch { StepJump.Start => "jump-start", StepJump.None => "jump-none", _ => "jump-both" }})");
            case Shape.Family:
                var mode = (EasingMode)_b switch { EasingMode.In => "in", EasingMode.Out => "out", _ => "in-out" };
                return mode + "-" + ((EasingFamily)_a).ToString().ToLowerInvariant();
            default:
                return "linear";
        }
    }

    /// <inheritdoc/>
    public bool Equals(Easing other) =>
        _shape == other._shape && _a == other._a && _b == other._b && _steps == other._steps
        && _x1 == other._x1 && _y1 == other._y1 && _x2 == other._x2 && _y2 == other._y2;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Easing e && Equals(e);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(_shape, _a, _b, _steps, _x1, _y1, _x2, _y2);

    /// <summary>Equality.</summary>
    public static bool operator ==(Easing left, Easing right) => left.Equals(right);

    /// <summary>Inequality.</summary>
    public static bool operator !=(Easing left, Easing right) => !left.Equals(right);
}
