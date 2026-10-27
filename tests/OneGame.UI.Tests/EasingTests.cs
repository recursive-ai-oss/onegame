using OneGame.Tweens;

namespace OneGame.UI.Tests;

/// <summary>Easing functions against their definitions: CSS Easing Functions Level 1 for cubic-bezier, steps and linear, and the usual closed forms for the named curves.</summary>
public class EasingTests
{
    // Reference values for the CSS keywords were solved independently (bisection on x(s) = t with 200 steps), not with the code under test.
    public static TheoryData<string, float, float> BezierPoints() => new()
    {
        { "ease", 0.1f, 0.094796f }, { "ease", 0.25f, 0.408511f }, { "ease", 0.5f, 0.802403f }, { "ease", 0.75f, 0.960459f }, { "ease", 0.9f, 0.994316f },
        { "ease-in", 0.1f, 0.017027f }, { "ease-in", 0.25f, 0.093465f }, { "ease-in", 0.5f, 0.315357f }, { "ease-in", 0.75f, 0.621862f }, { "ease-in", 0.9f, 0.839428f },
        { "ease-out", 0.1f, 0.160572f }, { "ease-out", 0.25f, 0.378138f }, { "ease-out", 0.5f, 0.684643f }, { "ease-out", 0.75f, 0.906535f }, { "ease-out", 0.9f, 0.982973f },
        { "ease-in-out", 0.1f, 0.019722f }, { "ease-in-out", 0.25f, 0.129162f }, { "ease-in-out", 0.5f, 0.5f }, { "ease-in-out", 0.75f, 0.870838f }, { "ease-in-out", 0.9f, 0.980278f },
        { "cubic-bezier(0.1, 0.7, 0.9, -0.4)", 0.1f, 0.218508f }, { "cubic-bezier(0.1, 0.7, 0.9, -0.4)", 0.5f, 0.2375f }, { "cubic-bezier(0.1, 0.7, 0.9, -0.4)", 0.9f, 0.492779f },
    };

    [Theory]
    [MemberData(nameof(BezierPoints))]
    public void CubicBezierMatchesTheCurve(string text, float t, float expected) =>
        Assert.Equal(expected, Easing.Parse(text).Evaluate(t), 1e-5f);

    [Fact]
    public void CubicBezierContinuesAlongItsEndTangentsOutsideZeroToOne()
    {
        // CSS Easing 2.2.1: below 0 follow the line through P0 and P1 (slope y1/x1); above 1 follow the line through P2 and P3.
        var e = Easing.CubicBezier(0.25f, 0.1f, 0.25f, 1f);
        Assert.Equal(-0.5f * 0.1f / 0.25f, e.Evaluate(-0.5f), 1e-6f);
        Assert.Equal(1 + (0.5f * (1f - 1f) / (0.25f - 1f)), e.Evaluate(1.5f), 1e-6f);
        // With x1 = 0 the first tangent is skipped and P2's is used; with x1 = x2 = 0 everything below 0 is 0.
        Assert.Equal(-1f * 0.5f / 0.5f, Easing.CubicBezier(0, 0.2f, 0.5f, 0.5f).Evaluate(-1f), 1e-6f);
        Assert.Equal(0f, Easing.CubicBezier(0, 0.2f, 0, 0.5f).Evaluate(-3f));
        // With x2 = 1 the second tangent is skipped and the line through P1 and P3 is used: y = 1 + (t - 1) * (0.2 - 1) / (0.2 - 1).
        Assert.Equal(4f, Easing.CubicBezier(0.2f, 0.2f, 1, 1).Evaluate(4f), 1e-5f);
    }

    [Fact]
    public void CubicBezierWithControlXOutsideZeroToOneIsInvalid()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Easing.CubicBezier(-0.1f, 0, 0.5f, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Easing.CubicBezier(0.1f, 0, 1.5f, 1));
        Assert.False(Easing.TryParse("cubic-bezier(1.1, 0, 0.5, 1)", out _));
    }

    [Fact]
    public void StepsFollowTheSpecAlgorithmForEveryJump()
    {
        // steps(4): rises at .25, .5, .75 and 1 (end); jump-start rises at 0, .25, .5 and .75.
        var end = Easing.Steps(4);
        Assert.Equal(0f, end.Evaluate(0.2499f));
        Assert.Equal(0.25f, end.Evaluate(0.25f));
        Assert.Equal(0.75f, end.Evaluate(0.99f));
        Assert.Equal(1f, end.Evaluate(1f));

        var start = Easing.Steps(4, StepJump.Start);
        Assert.Equal(0.25f, start.Evaluate(0f));
        Assert.Equal(0.25f, start.Evaluate(0.2499f));
        Assert.Equal(0.5f, start.Evaluate(0.25f));
        Assert.Equal(1f, start.Evaluate(0.99f));
        Assert.Equal(1f, start.Evaluate(1f));

        // jump-none(3): three levels 0, 1/2, 1, reached at 1/3 and 2/3; jump-both(2): levels 1/3, 2/3, 1 and 0 never shows.
        var none = Easing.Steps(3, StepJump.None);
        float[] noneIn = [0f, 0.33f, 0.34f, 0.66f, 0.67f, 1f];
        Assert.Equal([0f, 0f, 0.5f, 0.5f, 1f, 1f], noneIn.Select(none.Evaluate).ToArray());
        var both = Easing.Steps(2, StepJump.Both);
        float[] bothIn = [0f, 0.49f, 0.5f, 1f];
        Assert.Equal([1f / 3f, 1f / 3f, 2f / 3f, 1f], bothIn.Select(both.Evaluate).ToArray());

        Assert.Equal(1f, Easing.StepStart.Evaluate(0f));
        Assert.Equal(0f, Easing.StepEnd.Evaluate(0.99f));
        Assert.Equal(1f, Easing.StepEnd.Evaluate(1f));
    }

    [Fact]
    public void StepsRejectCountsTheSpecForbids()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Easing.Steps(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Easing.Steps(1, StepJump.None));
        Assert.False(Easing.TryParse("steps(0)", out _));
        Assert.False(Easing.TryParse("steps(1, jump-none)", out _));
        Assert.False(Easing.TryParse("steps(3, middle)", out _));
    }

    [Fact]
    public void LinearIsTheIdentityAndTheDefault()
    {
        Assert.Equal(default, Easing.Linear);
        foreach (var t in new[] { -1f, 0f, 0.3f, 1f, 2f })
        {
            Assert.Equal(t, default(Easing).Evaluate(t));
        }
    }

    [Fact]
    public void NamedCurvesMatchTheirClosedForms()
    {
        Assert.Equal(0.75f, Easing.OutQuad.Evaluate(0.5f), 1e-6f);
        Assert.Equal(0.25f, Easing.InQuad.Evaluate(0.5f), 1e-6f);
        Assert.Equal(0.0625f, Easing.InOutCubic.Evaluate(0.25f), 1e-6f);
        Assert.Equal(0.875f, Easing.OutCubic.Evaluate(0.5f), 1e-6f);
        Assert.Equal(1f / 16f, Easing.InQuart.Evaluate(0.5f), 1e-6f);
        Assert.Equal(1f / 32f, Easing.InQuint.Evaluate(0.5f), 1e-6f);
        Assert.Equal(0.1464466f, Easing.InOutSine.Evaluate(0.25f), 1e-6f);
        Assert.Equal(0.96875f, Easing.OutExpo.Evaluate(0.5f), 1e-6f);
        Assert.Equal(0.8660254f, Easing.OutCirc.Evaluate(0.5f), 1e-6f);
        Assert.Equal(1.0877f, Easing.OutBack.Evaluate(0.5f), 1e-4f);
        Assert.Equal(-0.0996818f, Easing.InOutBack.Evaluate(0.25f), 1e-5f);
        Assert.Equal(1.015625f, Easing.OutElastic.Evaluate(0.5f), 1e-5f);
        Assert.Equal(-0.015625f, Easing.InElastic.Evaluate(0.5f), 1e-5f);
        Assert.Equal(0.0119694f, Easing.InOutElastic.Evaluate(0.25f), 1e-5f);
        Assert.Equal(0.765625f, Easing.OutBounce.Evaluate(0.5f), 1e-6f);
        Assert.Equal(1 - 0.765625f, Easing.InBounce.Evaluate(0.5f), 1e-6f);
    }

    [Fact]
    public void EveryNamedCurveStartsAtZeroEndsAtOneAndInOutPassesThroughTheMiddle()
    {
        for (var f = EasingFamily.Sine; f <= EasingFamily.Bounce; f++)
        {
            foreach (var m in new[] { EasingMode.In, EasingMode.Out, EasingMode.InOut })
            {
                var e = Easing.Of(f, m);
                Assert.Equal(0f, e.Evaluate(0f));
                Assert.Equal(1f, e.Evaluate(1f));
                Assert.Equal(0f, e.Evaluate(-0.5f));
                Assert.Equal(1f, e.Evaluate(1.5f));
                if (m == EasingMode.InOut && f != EasingFamily.Bounce)
                {
                    Assert.Equal(0.5f, e.Evaluate(0.5f), 1e-5f);
                }
            }
        }
    }

    [Fact]
    public void OutIsTheMirrorOfInForTheMirroredFamilies()
    {
        foreach (var f in new[] { EasingFamily.Sine, EasingFamily.Quad, EasingFamily.Cubic, EasingFamily.Quart, EasingFamily.Quint, EasingFamily.Expo, EasingFamily.Circ, EasingFamily.Bounce, EasingFamily.Back })
        {
            for (var t = 0.05f; t < 1; t += 0.1f)
            {
                Assert.Equal(1 - Easing.Of(f, EasingMode.In).Evaluate(1 - t), Easing.Of(f, EasingMode.Out).Evaluate(t), 1e-5f);
            }
        }
    }

    [Fact]
    public void TextRoundTrips()
    {
        foreach (var text in new[] { "linear", "ease", "ease-in", "ease-out", "ease-in-out", "steps(4)", "steps(3, jump-start)", "steps(3, jump-none)", "steps(2, jump-both)", "cubic-bezier(0.1, 0.7, 0.9, -0.4)", "in-quad", "out-back", "in-out-elastic" })
        {
            var e = Easing.Parse(text);
            Assert.Equal(text, e.ToString());
            Assert.Equal(e, Easing.Parse(e.ToString()));
        }

        Assert.Equal(Easing.StepStart, Easing.Parse("STEP-START"));
        Assert.Equal(Easing.Steps(3, StepJump.Start), Easing.Parse("steps(3, start)"));
        Assert.Equal(Easing.Steps(3), Easing.Parse("steps(3, end)"));
        Assert.Equal(Easing.Ease, Easing.Parse("  Ease  "));
        Assert.Equal(Easing.CubicBezier(0, 0, 0.58f, 1), Easing.Parse("cubic-bezier( 0 ,0, .58 , 1 )"));
        Assert.Throws<FormatException>(() => Easing.Parse("bounce"));
        Assert.False(Easing.TryParse("", out _));
        Assert.False(Easing.TryParse("cubic-bezier(1,2,3)", out _));
        Assert.False(Easing.TryParse("cubic-bezier(1,2,3,4,5)", out _));
        Assert.False(Easing.TryParse("in-nothing", out _));
    }
}
