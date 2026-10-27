namespace OneGame.Particles;

/// <summary>A range of values; each particle draws one uniformly between <see cref="Min"/> and <see cref="Max"/>. A plain number converts to a range of one value.</summary>
/// <param name="Min">Lowest value.</param>
/// <param name="Max">Highest value.</param>
public readonly record struct FloatRange(float Min, float Max)
{
    /// <summary>A range that always gives <paramref name="value"/>.</summary>
    /// <param name="value">The value.</param>
    public static implicit operator FloatRange(float value) => new(value, value);

    /// <summary>A range that always gives <paramref name="value"/>.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The range.</returns>
    public static FloatRange FromSingle(float value) => new(value, value);
}

/// <summary>
/// A number that changes over a particle's life, as straight lines between stops. <c>t</c> runs from 0 (born) to 1 (gone). Before the first stop
/// the first value holds, after the last stop the last. Immutable: make one when the emitter is set up and share it.
/// </summary>
public sealed class ParticleCurve
{
    private readonly float[] _t;
    private readonly float[] _v;

    private ParticleCurve(float[] t, float[] v)
    {
        _t = t;
        _v = v;
    }

    /// <summary>The same value for the whole life.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The curve.</returns>
    public static ParticleCurve Constant(float value) => new([0f], [value]);

    /// <summary>A straight line from <paramref name="from"/> at birth to <paramref name="to"/> at the end.</summary>
    /// <param name="from">Value at t = 0.</param>
    /// <param name="to">Value at t = 1.</param>
    /// <returns>The curve.</returns>
    public static ParticleCurve Linear(float from, float to) => new([0f, 1f], [from, to]);

    /// <summary>A curve through the given stops, which must be in increasing order of <c>T</c>.</summary>
    /// <param name="stops">Pairs of life fraction and value; at least one.</param>
    /// <returns>The curve.</returns>
    /// <exception cref="ArgumentException">There are no stops, or they are not in increasing order.</exception>
    public static ParticleCurve Of(params (float T, float Value)[] stops)
    {
        ArgumentNullException.ThrowIfNull(stops);
        if (stops.Length == 0)
        {
            throw new ArgumentException("a curve needs at least one stop", nameof(stops));
        }

        var t = new float[stops.Length];
        var v = new float[stops.Length];
        for (var i = 0; i < stops.Length; i++)
        {
            if (i > 0 && !(stops[i].T > stops[i - 1].T))
            {
                throw new ArgumentException("the stops of a curve must be in increasing order of T", nameof(stops));
            }

            t[i] = stops[i].T;
            v[i] = stops[i].Value;
        }

        return new ParticleCurve(t, v);
    }

    /// <summary>The value at a point of the life.</summary>
    /// <param name="t">Fraction of the life, 0 to 1.</param>
    /// <returns>The value.</returns>
    public float Evaluate(float t)
    {
        var n = _t.Length;
        if (t <= _t[0])
        {
            return _v[0];
        }

        for (var i = 1; i < n; i++)
        {
            if (t < _t[i])
            {
                var f = (t - _t[i - 1]) / (_t[i] - _t[i - 1]);
                return _v[i - 1] + ((_v[i] - _v[i - 1]) * f);
            }
        }

        return _v[n - 1];
    }
}

/// <summary>
/// A colour that changes over a particle's life, as straight lines between stops. Colours are sRGB with straight alpha, components 0 to 1.
/// Values above 1 are allowed in 3D, where they glow (the scene is high dynamic range); 2D clamps. Immutable.
/// </summary>
public sealed class ParticleGradient
{
    private readonly float[] _t;
    private readonly Vector4[] _v;

    private ParticleGradient(float[] t, Vector4[] v)
    {
        _t = t;
        _v = v;
    }

    /// <summary>Makes a colour from a 0xRRGGBB value.</summary>
    /// <param name="rgb">Red in the third byte, green in the second, blue in the first.</param>
    /// <param name="alpha">Opacity, 0 to 1.</param>
    /// <returns>The colour.</returns>
    public static Vector4 Rgb(uint rgb, float alpha = 1f) =>
        new(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);

    /// <summary>The same colour for the whole life.</summary>
    /// <param name="color">sRGB with straight alpha.</param>
    /// <returns>The gradient.</returns>
    public static ParticleGradient Constant(Vector4 color) => new([0f], [color]);

    /// <summary>A straight line between two colours.</summary>
    /// <param name="from">Colour at birth.</param>
    /// <param name="to">Colour at the end.</param>
    /// <returns>The gradient.</returns>
    public static ParticleGradient Between(Vector4 from, Vector4 to) => new([0f, 1f], [from, to]);

    /// <summary>The colour fading out: opaque at birth, transparent at the end.</summary>
    /// <param name="color">The colour (its alpha is the opacity at birth).</param>
    /// <returns>The gradient.</returns>
    public static ParticleGradient FadeOut(Vector4 color) => new([0f, 1f], [color, new Vector4(color.X, color.Y, color.Z, 0f)]);

    /// <summary>A gradient through the given stops, which must be in increasing order of <c>T</c>.</summary>
    /// <param name="stops">Pairs of life fraction and colour; at least one.</param>
    /// <returns>The gradient.</returns>
    /// <exception cref="ArgumentException">There are no stops, or they are not in increasing order.</exception>
    public static ParticleGradient Of(params (float T, Vector4 Color)[] stops)
    {
        ArgumentNullException.ThrowIfNull(stops);
        if (stops.Length == 0)
        {
            throw new ArgumentException("a gradient needs at least one stop", nameof(stops));
        }

        var t = new float[stops.Length];
        var v = new Vector4[stops.Length];
        for (var i = 0; i < stops.Length; i++)
        {
            if (i > 0 && !(stops[i].T > stops[i - 1].T))
            {
                throw new ArgumentException("the stops of a gradient must be in increasing order of T", nameof(stops));
            }

            t[i] = stops[i].T;
            v[i] = stops[i].Color;
        }

        return new ParticleGradient(t, v);
    }

    /// <summary>The colour at a point of the life.</summary>
    /// <param name="t">Fraction of the life, 0 to 1.</param>
    /// <returns>sRGB with straight alpha.</returns>
    public Vector4 Evaluate(float t)
    {
        var n = _t.Length;
        if (t <= _t[0])
        {
            return _v[0];
        }

        for (var i = 1; i < n; i++)
        {
            if (t < _t[i])
            {
                return Vector4.Lerp(_v[i - 1], _v[i], (t - _t[i - 1]) / (_t[i] - _t[i - 1]));
            }
        }

        return _v[n - 1];
    }
}
