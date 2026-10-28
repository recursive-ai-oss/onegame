using System.Numerics;
using OneGame.Gltf;

namespace OneGame.Animation.Tests;

public class BlendTests
{
    [Fact]
    public void ACrossfadeRunsFromZeroToOneInTheGivenTime()
    {
        var fade = new Crossfade(0.2f);
        Assert.Equal(0f, fade.Weight);
        Assert.False(fade.Done);
        fade.Advance(0.05f);
        Assert.Equal(0.25f, fade.Weight, 4);
        fade.Advance(-1f); // time never runs backwards
        Assert.Equal(0.25f, fade.Weight, 4);
        fade.Advance(0.15f);
        Assert.Equal(1f, fade.Weight);
        Assert.True(fade.Done);
        fade.Advance(5f);
        Assert.Equal(1f, fade.Weight);
        Assert.True(new Crossfade(0f).Done);
        Assert.Equal(1f, new Crossfade(0f).Weight);
    }

    [Theory]
    [InlineData(-5f, 1f, 0f, 0f)]
    [InlineData(0f, 1f, 0f, 0f)]
    [InlineData(0.5f, 0.5f, 0.5f, 0f)]
    [InlineData(1f, 0f, 1f, 0f)]
    [InlineData(1.25f, 0f, 0.75f, 0.25f)]
    [InlineData(2f, 0f, 0f, 1f)]
    [InlineData(9f, 0f, 0f, 1f)]
    [InlineData(float.NaN, 1f, 0f, 0f)]
    public void ABlendSpace1DWeighsTheTwoNeighboursAndClampsAtTheEnds(float value, float w0, float w1, float w2)
    {
        var space = new BlendSpace1D(0f, 1f, 2f);
        Span<float> w = stackalloc float[3];
        space.Weights(value, w);
        Assert.Equal(w0, w[0], 5);
        Assert.Equal(w1, w[1], 5);
        Assert.Equal(w2, w[2], 5);
        Assert.Equal(1f, w[0] + w[1] + w[2], 5);
    }

    [Fact]
    public void ABlendSpace1DNeedsIncreasingPositions()
    {
        Assert.Throws<ArgumentException>(() => { _ = new BlendSpace1D(); });
        Assert.Throws<ArgumentException>(() => { _ = new BlendSpace1D(0f, 0f); });
        Assert.Throws<ArgumentException>(() => { _ = new BlendSpace1D(1f, 0f); });
    }

    private static Vector2 Mix(BlendSpace2D space, Vector2[] points, Vector2 at, out float sum)
    {
        var w = new float[points.Length];
        space.Weights(at, w);
        sum = w.Sum();
        var p = Vector2.Zero;
        for (var i = 0; i < w.Length; i++)
        {
            Assert.True(w[i] >= 0f);
            p += points[i] * w[i];
        }

        return p;
    }

    [Fact]
    public void ABlendSpace2DReproducesPointsInsideAndTheNearestEdgePointOutside()
    {
        Vector2[] points = [new(0, 0), new(1, 0), new(0, 1), new(1, 1)];
        var space = new BlendSpace2D(points);
        Assert.Equal(2, space.TriangleCount);

        // Corners count alone.
        for (var i = 0; i < points.Length; i++)
        {
            var w = new float[4];
            space.Weights(points[i], w);
            Assert.Equal(1f, w[i], 5);
        }

        // Inside: the weights add up to the point (barycentric).
        foreach (var at in new[] { new Vector2(0.25f, 0.25f), new Vector2(0.5f, 0.5f), new Vector2(0.9f, 0.3f) })
        {
            var got = Mix(space, points, at, out var sum);
            Assert.Equal(1f, sum, 5);
            Assert.True(Vector2.Distance(got, at) < 1e-5f, $"{at} -> {got}");
        }

        // Outside: the closest point of the outline.
        Assert.True(Vector2.Distance(Mix(space, points, new Vector2(2f, 0.5f), out var s1), new Vector2(1f, 0.5f)) < 1e-5f);
        Assert.Equal(1f, s1, 5);
        Assert.True(Vector2.Distance(Mix(space, points, new Vector2(-3f, -3f), out _), Vector2.Zero) < 1e-5f);
    }

    [Fact]
    public void ABlendSpace2DOfARingAroundACentreUsesTheCentreInside()
    {
        Vector2[] points = [new(0, 0), new(1, 0), new(0, 1), new(-1, 0), new(0, -1)];
        var space = new BlendSpace2D(points);
        Assert.Equal(4, space.TriangleCount);
        var w = new float[5];
        space.Weights(new Vector2(0.5f, 0.0f), w);
        Assert.Equal(0.5f, w[0], 5);
        Assert.Equal(0.5f, w[1], 5);
        Assert.Equal(1f, w.Sum(), 5);
        space.Weights(new Vector2(0f, 4f), w);
        Assert.Equal(1f, w[2], 5);
        Assert.Throws<ArgumentException>(() => { _ = new BlendSpace2D(new Vector2(0, 0), new Vector2(1, 1), new Vector2(2, 2)); });
        Assert.Throws<ArgumentException>(() => { _ = new BlendSpace2D(new Vector2(0, 0), new Vector2(1, 1)); });
    }

    [Fact]
    public void PoseLerpBlendsLinearlyAndAMaskKeepsTheRestOfTheBodyAsItWas()
    {
        var rig = Figure.Rig;
        var a = rig.CreatePose();
        var b = rig.CreatePose();
        rig.Sample(rig.Clip("idle"), 0f, a);
        rig.Sample(rig.Clip("walk"), 0.3f, b);
        var hips = rig.Node("Hips");
        var thigh = rig.Node("ThighR");
        var shin = rig.Node("ShinR");
        var dst = rig.CreatePose();
        PoseBlend.Lerp(dst, a, b, 0.5f);
        Assert.Equal((a.Translation[hips].Y + b.Translation[hips].Y) / 2f, dst.Translation[hips].Y, 5);
        PoseBlend.Lerp(dst, a, b, 0f);
        Assert.Equal(a.Rotation[thigh], dst.Rotation[thigh]);
        PoseBlend.Lerp(dst, a, b, 1f);
        Assert.True(MathF.Abs(Quaternion.Dot(b.Rotation[thigh], dst.Rotation[thigh])) > 0.99999f);

        var mask = PoseMask.Subtree(rig.Model, thigh);
        PoseBlend.Lerp(dst, a, b, 1f, mask);
        Assert.True(MathF.Abs(Quaternion.Dot(b.Rotation[shin], dst.Rotation[shin])) > 0.99999f, "below the masked node follows b");
        Assert.Equal(a.Translation[hips], dst.Translation[hips]); // outside the mask stays a
        Assert.Equal(a.Rotation[rig.Node("ThighL")], dst.Rotation[rig.Node("ThighL")]);

        // Blending in place gives the same answer as into another pose.
        var inPlace = rig.CreatePose();
        inPlace.CopyFrom(a);
        PoseBlend.Lerp(inPlace, inPlace, b, 0.37f);
        PoseBlend.Lerp(dst, a, b, 0.37f);
        for (var i = 0; i < rig.NodeCount; i++)
        {
            Assert.Equal(dst.Rotation[i], inPlace.Rotation[i]);
            Assert.Equal(dst.Translation[i], inPlace.Translation[i]);
        }
    }

    [Fact]
    public void SamplingAClipNeverDisturbsTheRigsRestPose()
    {
        var rig = Figure.Rig;
        var pose = rig.CreatePose();
        rig.Sample(rig.Clip("run"), 0.2f, pose);
        var fresh = rig.CreatePose();
        var restHips = Figure.Model.Nodes[rig.Node("Hips")].Translation;
        Assert.Equal(restHips, fresh.Translation[rig.Node("Hips")]);
        Assert.NotEqual(restHips, pose.Translation[rig.Node("Hips")]);
        rig.Reset(pose);
        Assert.Equal(restHips, pose.Translation[rig.Node("Hips")]);
    }

    [Fact]
    public void ATwoDimensionalMotionBlendsClipsByItsTwoInputs()
    {
        var rig = Figure.Rig;
        var space = new BlendSpace2D(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1));
        var motion = new BlendSpace2DMotion(space, rig.Clip("idle"), rig.Clip("walk"), rig.Clip("run"), rig.Clip("jump"));
        var dst = rig.CreatePose();
        var scratch = rig.CreatePose();
        motion.X = 0f;
        motion.Y = 0f;
        Assert.Equal(rig.Clip("idle").Duration, motion.Duration, 5);
        motion.Sample(rig, 0.25f, dst, scratch);
        var alone = rig.CreatePose();
        rig.Sample(rig.Clip("idle"), 0.25f * rig.Clip("idle").Duration, alone);
        Assert.Equal(alone.Translation[rig.Node("Hips")], dst.Translation[rig.Node("Hips")]);
        motion.X = 0.5f;
        Assert.Equal((rig.Clip("idle").Duration + rig.Clip("walk").Duration) / 2f, motion.Duration, 4);
        motion.Sample(rig, 0.25f, dst, scratch);
        Assert.True(float.IsFinite(dst.Translation[rig.Node("Hips")].Y));
        Assert.Throws<ArgumentException>(() => { _ = new BlendSpace2DMotion(space, rig.Clip("idle")); });
    }
}
