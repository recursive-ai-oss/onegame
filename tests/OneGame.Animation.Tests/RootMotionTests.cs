using System.Numerics;
using OneGame.Core;
using OneGame.Gltf;

namespace OneGame.Animation.Tests;

public class RootMotionTests
{
    private static readonly int Root = Figure.Rig.Node("Root");

    [Fact]
    public void BakingGivesWholeTicksThatAddUpExactlyToTheClipsMovement()
    {
        var walk = Figure.Walk;
        Assert.Equal(60, walk.Length);
        Assert.Equal(-1.0f, walk.Total.Z.ToSingle(), 5);
        Assert.Equal(0f, walk.Total.X.ToSingle());
        Assert.Equal(42, Figure.Run.Length);
        Assert.Equal(-2.114f, Figure.Run.Total.Z.ToSingle(), 3);
        Assert.Equal(48, Figure.Jump.Length);
        Assert.Equal(0, Figure.Jump.Total.Y.ToSingle()); // lands where it started
        Assert.Equal(-0.6f, Figure.Jump.Total.Z.ToSingle(), 5);
        Assert.Equal(0.5f, Enumerable.Range(0, 48).Select(i => Figure.Jump.Displacement(0, i).Y.ToSingle()).Max(), 2);

        // The steps add up exactly (no rounding drift), however many loops.
        var sum = FVec3.Zero;
        for (var i = 0; i < walk.Length; i++)
        {
            sum += walk.DeltaAt(i);
        }

        Assert.True(sum == walk.Total);
        Assert.True(walk.Displacement(0, 6000) == walk.Total * Fixed.FromInt(100));
        Assert.True(walk.Displacement(17, 203) == walk.Displacement(0, 203) - walk.Displacement(0, 17));
        Assert.True(walk.DeltaAt(61) == walk.DeltaAt(1));
        Assert.True(Figure.Jump.DeltaAt(48, loop: false) == FVec3.Zero);
        Assert.True(Figure.Jump.DeltaAt(-1) == FVec3.Zero);

        // A clip that does not move the root gives a track of zeros.
        var idle = RootMotion.Bake(Figure.Rig.Clip("idle"), Root, 60);
        Assert.Equal(120, idle.Length);
        Assert.True(idle.Total == FVec3.Zero);
    }

    [Fact]
    public void BakingTwiceGivesTheSameFixedPointNumbers()
    {
        var a = RootMotion.Bake(Figure.Rig.Clip("run"), Root, 60);
        var b = RootMotion.Bake(Figure.Rig.Clip("run"), Root, 60);
        for (var i = 0; i < a.Length; i++)
        {
            Assert.True(a.DeltaAt(i) == b.DeltaAt(i));
        }

        // A known answer for the walk's total: if the numbers ever differ between machines or runtimes this is the test that says so.
        Assert.Equal(-4294967296L, Figure.Walk.Total.Z.Raw);
    }

    [Fact]
    public void StepMovesATransformAlongItsHeading()
    {
        var turned = Transform3.At(FVec3.Zero, FQuat.FromAxisAngle(FVec3.UnitY, Fixed.HalfPi)); // a quarter turn to the left: forward is now -X
        var moved = Figure.Walk.Step(turned, 0);
        Assert.True(moved.Position.X.ToSingle() < 0f);
        Assert.Equal(0f, moved.Position.Z.ToSingle(), 3);
        Assert.Equal(Figure.Walk.DeltaAt(0).Z.ToSingle(), moved.Position.X.ToSingle(), 3);
    }

    // The ankle of the planted foot, in the world: its place in the model with the root's own movement taken out, plus the baked movement so far.
    private static (float Min, float Max, float Y) PlantedFoot(string clipName, RootMotionTrack track, string foot, int ticks)
    {
        var rig = Figure.Rig;
        var clip = rig.Clip(clipName);
        var pose = rig.CreatePose();
        var ankle = rig.Node(foot);
        float min = float.MaxValue, max = float.MinValue, y = 0f;
        for (var k = 0; k < ticks; k++)
        {
            rig.Sample(clip, k / 60f, pose);
            pose.Translation[Root] = rig.RestTranslation(Root);
            var world = rig.ComputeWorld(pose)[ankle].Translation;
            var z = world.Z + track.Displacement(0, k).Z.ToSingle();
            min = Math.Min(min, z);
            max = Math.Max(max, z);
            y = Math.Max(y, world.Y);
        }

        return (min, max, y);
    }

    [Fact]
    public void ThePlantedFootDoesNotSlideOnAStraightWalkOrRun()
    {
        // The right foot is planted for the first 55% of the walk cycle (33 ticks) and 35% of the run cycle (14 ticks).
        var walk = PlantedFoot("walk", Figure.Walk, "FootR", 33);
        var run = PlantedFoot("run", Figure.Run, "FootR", 14);
        Assert.True(walk.Max - walk.Min < 0.002f, $"walk foot slid {walk.Max - walk.Min:0.0000} m");
        Assert.True(run.Max - run.Min < 0.005f, $"run foot slid {run.Max - run.Min:0.0000} m");
        Assert.True(walk.Y < 0.06f && run.Y < 0.06f, $"the ankle stays at ground level while planted (walk {walk.Y}, run {run.Y})");

        // Without the root's movement the same foot travels far, so the test would notice a missing track.
        var none = PlantedFoot("walk", RootMotion.Bake(Figure.Rig.Clip("idle"), Root, 60), "FootR", 33);
        Assert.True(none.Max - none.Min > 0.4f, $"without root motion the foot moves {none.Max - none.Min}");

        // Planted in the other half of the cycle for the left foot.
        var left = PlantedFoot("walk", Figure.Walk, "FootL", 33);
        Assert.True(left.Max - left.Min > 0.1f, $"left {left.Max - left.Min}"); // the left foot is mostly swinging in these ticks
    }

    [Fact]
    public void TheClipItselfLoopsSeamlessly()
    {
        var rig = Figure.Rig;
        foreach (var name in new[] { "idle", "walk", "run" })
        {
            var clip = rig.Clip(name);
            var a = rig.CreatePose();
            var b = rig.CreatePose();
            rig.Sample(clip, 0f, a);
            rig.Sample(clip, clip.Duration, b);
            for (var i = 0; i < rig.NodeCount; i++)
            {
                if (i != Root)
                {
                    Assert.True(MathF.Abs(Quaternion.Dot(a.Rotation[i], b.Rotation[i])) > 0.99999f, $"{name}: node {i}");
                    Assert.True(Vector3.Distance(a.Translation[i], b.Translation[i]) < 1e-4f, $"{name}: node {i}");
                }
            }
        }
    }

    [Fact]
    public void TwoBoneIkReachesATargetKeepsBoneLengthsAndBendsTowardsThePole()
    {
        var rig = Figure.Rig;
        int thigh = rig.Node("ThighR"), shin = rig.Node("ShinR"), foot = rig.Node("FootR");
        var pose = rig.CreatePose();
        rig.Sample(rig.Clip("walk"), 0.3f, pose);
        var before = rig.ComputeWorld(pose);
        var hip = before[thigh].Translation;
        var l1 = Vector3.Distance(hip, before[shin].Translation);
        var l2 = Vector3.Distance(before[shin].Translation, before[foot].Translation);
        var target = new Vector3(0.2f, 0.15f, -0.2f);

        Assert.True(TwoBoneIk.Solve(rig, pose, thigh, shin, foot, target, new Vector3(0.1f, 0.5f, -1f)));
        var w = rig.ComputeWorld(pose);
        Assert.True(Vector3.Distance(w[foot].Translation, target) < 1e-3f, $"ankle {w[foot].Translation} target {target}");
        Assert.Equal(l1, Vector3.Distance(w[thigh].Translation, w[shin].Translation), 4);
        Assert.Equal(l2, Vector3.Distance(w[shin].Translation, w[foot].Translation), 4);
        var kneeForward = w[shin].Translation.Z;
        Assert.True(kneeForward < (hip.Z + target.Z) / 2f - 0.05f, "the knee bends towards -Z, where the pole is");

        Assert.True(TwoBoneIk.Solve(rig, pose, thigh, shin, foot, target, new Vector3(0.1f, 0.5f, 1f)));
        w = rig.ComputeWorld(pose);
        Assert.True(Vector3.Distance(w[foot].Translation, target) < 1e-3f);
        Assert.True(w[shin].Translation.Z > (hip.Z + target.Z) / 2f + 0.05f, "and towards +Z when the pole is there");

        // Out of reach: the leg straightens and points at the target.
        var far = new Vector3(0.1f, 0.05f, -3f);
        Assert.False(TwoBoneIk.Solve(rig, pose, thigh, shin, foot, far, new Vector3(0.1f, 0.5f, -1f)));
        w = rig.ComputeWorld(pose);
        Assert.Equal(l1 + l2, Vector3.Distance(w[thigh].Translation, w[foot].Translation), 3);
        Assert.True(Vector3.Dot(Vector3.Normalize(w[foot].Translation - hip), Vector3.Normalize(far - hip)) > 0.999f);

        Assert.Throws<ArgumentException>(() => TwoBoneIk.Solve(rig, pose, thigh, foot, shin, target, target));
    }
}
