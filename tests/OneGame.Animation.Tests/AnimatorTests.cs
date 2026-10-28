using OneGame.Gltf;

namespace OneGame.Animation.Tests;

public class AnimatorTests
{
    private static Animator Make(Action<Animator>? more = null)
    {
        var rig = Figure.Rig;
        var a = new Animator(rig);
        a.AddFloat("speed").AddBool("ready").AddTrigger("go").AddTrigger("j");
        a.AddState("idle", new ClipMotion(rig.Clip("idle")));
        a.AddState("walk", new ClipMotion(rig.Clip("walk")));
        a.AddState("run", new ClipMotion(rig.Clip("run")));
        a.AddState("jump", new ClipMotion(rig.Clip("jump")), loop: false);
        more?.Invoke(a);
        return a;
    }

    [Fact]
    public void AFloatConditionStartsATransitionWhenItHolds()
    {
        var a = Make(x => x.Transition("idle", "walk").WhenFloat("speed", Compare.Greater, 0.5f));
        a.SetFloat("speed", 0.4f);
        for (var i = 0; i < 10; i++)
        {
            a.Update(0.016f);
        }

        Assert.Equal("idle", a.CurrentState);
        a.SetFloat("speed", 0.6f);
        a.Update(0.016f);
        Assert.Equal("walk", a.CurrentState);
        Assert.False(a.IsFading); // no fade time was given
    }

    [Fact]
    public void ACrossfadeBlendsTheOutgoingAndIncomingPoses()
    {
        var rig = Figure.Rig;
        var a = Make(x => x.Transition("idle", "walk").WhenTrigger("go").Fade(0.2f));
        a.Update(0f);
        a.Trigger("go");
        a.Update(0f);
        Assert.Equal("walk", a.CurrentState);
        Assert.True(a.IsFading);
        a.Update(0.1f);

        // Half way: idle has played 0.1 s, walk has played 0.1 s, each with weight one half.
        var idle = rig.CreatePose();
        var walk = rig.CreatePose();
        rig.Sample(rig.Clip("idle"), 0.1f, idle);
        rig.Sample(rig.Clip("walk"), 0.1f, walk);
        var hips = rig.Node("Hips");
        Assert.True(a.IsFading);
        Assert.Equal((idle.Translation[hips].Y + walk.Translation[hips].Y) / 2f, a.Pose.Translation[hips].Y, 4);
        a.Update(0.11f);
        Assert.False(a.IsFading);
        var after = rig.CreatePose();
        rig.Sample(rig.Clip("walk"), 0.21f, after);
        Assert.Equal(after.Translation[hips].Y, a.Pose.Translation[hips].Y, 4);
    }

    [Fact]
    public void ATriggerIsConsumedByTheTransitionThatFires()
    {
        var a = Make(x =>
        {
            x.Transition("idle", "walk").WhenTrigger("go");
            x.Transition("walk", "run").WhenTrigger("go");
        });
        a.Update(0f);
        a.Trigger("go");
        a.Update(0.016f);
        Assert.Equal("walk", a.CurrentState);
        Assert.False(a.GetBool("go"));
        a.Update(0.016f);
        a.Update(0.016f);
        Assert.Equal("walk", a.CurrentState); // one trigger starts one transition
        a.Trigger("go");
        a.Update(0.016f);
        Assert.Equal("run", a.CurrentState);
    }

    [Fact]
    public void ATriggerStaysSetUntilItsTransitionCanFire()
    {
        var a = Make(x => x.Transition("idle", "walk").WhenTrigger("go").WhenBool("ready", true));
        a.Trigger("go");
        a.Update(0.016f);
        Assert.Equal("idle", a.CurrentState);
        Assert.True(a.GetBool("go"));
        a.SetBool("ready", true);
        a.Update(0.016f);
        Assert.Equal("walk", a.CurrentState);
        Assert.False(a.GetBool("go"));
    }

    [Fact]
    public void ExitTimeWaitsForTheStateToHavePlayedThatMuch()
    {
        // The jump clip is 0.8 s long; leaving at half way means 0.4 s.
        var a = Make(x =>
        {
            x.Transition("idle", "jump").WhenTrigger("j");
            x.Transition("jump", "idle").AtExit(0.5f);
        });
        a.Update(0f);
        a.Trigger("j");
        a.Update(0f);
        Assert.Equal("jump", a.CurrentState);
        for (var i = 0; i < 39; i++)
        {
            a.Update(0.01f);
        }

        Assert.Equal("jump", a.CurrentState);
        for (var i = 0; i < 3; i++)
        {
            a.Update(0.01f);
        }

        Assert.Equal("idle", a.CurrentState);
    }

    [Fact]
    public void ANonLoopingStateHoldsItsLastPoseAndALoopingOneWraps()
    {
        var rig = Figure.Rig;
        var a = Make();
        a.Play("jump");
        a.Update(5f);
        var last = rig.CreatePose();
        rig.Sample(rig.Clip("jump"), rig.Clip("jump").Duration, last);
        Assert.Equal(last.Rotation[rig.Node("ThighR")], a.Pose.Rotation[rig.Node("ThighR")]);
        a.Play("walk", 3.25f);
        var quarter = rig.CreatePose();
        rig.Sample(rig.Clip("walk"), 0.25f * rig.Clip("walk").Duration, quarter);
        Assert.Equal(quarter.Rotation[rig.Node("ThighR")], a.Pose.Rotation[rig.Node("ThighR")]);
    }

    [Fact]
    public void ATransitionFromAnyStateAppliesEverywhereButIntoTheCurrentState()
    {
        var a = Make(x => x.Transition(null, "jump").WhenTrigger("j").Fade(0f));
        a.Update(0f);
        a.Play("run");
        a.Trigger("j");
        a.Update(0.016f);
        Assert.Equal("jump", a.CurrentState);
        a.Trigger("j");
        a.Update(0.016f);
        Assert.Equal("jump", a.CurrentState);
    }

    [Fact]
    public void ABlendSpaceStateFollowsItsParameter()
    {
        var rig = Figure.Rig;
        var a = new Animator(rig);
        a.AddFloat("speed");
        a.AddState("move", new BlendSpace1DMotion(new BlendSpace1D(0, 1, 2), rig.Clip("idle"), rig.Clip("walk"), rig.Clip("run")) { XParameter = "speed" });
        a.SetFloat("speed", 0f);
        a.Update(0f);
        var idle = rig.CreatePose();
        rig.Sample(rig.Clip("idle"), 0f, idle);
        Assert.Equal(idle.Translation[rig.Node("Hips")].Y, a.Pose.Translation[rig.Node("Hips")].Y, 5);
        a.SetFloat("speed", 2f);
        a.Update(0f);
        var run = rig.CreatePose();
        rig.Sample(rig.Clip("run"), 0f, run);
        Assert.Equal(run.Translation[rig.Node("Hips")].Y, a.Pose.Translation[rig.Node("Hips")].Y, 5);
        a.SetFloat("speed", 1.5f);
        a.Update(0f);
        var walk = rig.CreatePose();
        rig.Sample(rig.Clip("walk"), 0f, walk);
        Assert.Equal((walk.Translation[rig.Node("Hips")].Y + run.Translation[rig.Node("Hips")].Y) / 2f, a.Pose.Translation[rig.Node("Hips")].Y, 5);
    }

    [Fact]
    public void RootMotionIsHeldAtRestUnlessAskedForAndGoodErrorsNameWhatExists()
    {
        var rig = Figure.Rig;
        var root = rig.Node("Root");
        var a = new Animator(rig, root);
        a.AddState("walk", new ClipMotion(rig.Clip("walk")));
        a.Update(0.5f);
        Assert.Equal(rig.RestTranslation(root), a.Pose.Translation[root]);
        a.KeepRootMotion = true;
        a.Update(0f);
        Assert.True(a.Pose.Translation[root].Z < -0.4f);

        var ex = Assert.Throws<UnknownAnimatorNameException>(() => a.SetFloat("nope", 1f));
        Assert.Contains("nope", ex.Message);
        Assert.Throws<UnknownAnimatorNameException>(() => { _ = a.Transition("walk", "missing"); });
        Assert.Throws<KeyNotFoundException>(() => { _ = rig.Clip("flying"); });
        Assert.Throws<InvalidOperationException>(() => { new Animator(rig).Update(0.1f); });
    }

    [Fact]
    public void UpdateToPlaysShortGapsInFixedStepsAndSnapsAcrossLongOnes()
    {
        var rig = Figure.Rig;
        AnimatorState Run(double[] times)
        {
            var a = new Animator(rig);
            a.AddFloat("speed");
            a.AddState("walk", new ClipMotion(rig.Clip("walk")));
            foreach (var t in times)
            {
                a.UpdateTo(t);
            }

            return new AnimatorState(a.StateProgress, a.Pose.Rotation[rig.Node("ThighR")]);
        }

        // Frames at any rate that add up to the same time give the same result.
        var coarse = Run([0.0, 0.1, 0.2]);
        var fine = Run([0.0, 0.05, 0.1, 0.15, 0.2]);
        Assert.Equal(coarse.Progress, fine.Progress, 4);
        Assert.Equal(coarse.Rotation, fine.Rotation);

        // The same time again changes nothing; a long jump in time does not replay what was missed.
        var again = Run([0.0, 0.2, 0.2]);
        Assert.Equal(coarse.Progress, again.Progress, 4);
        var seek = Run([0.0, 100.0]);
        Assert.Equal(0f, seek.Progress);
    }

    [Fact]
    public void AMovedParameterGlidesAtItsRateAndSettlesAfterAGap()
    {
        var rig = Figure.Rig;
        var a = new Animator(rig);
        a.AddFloat("speed");
        a.AddState("walk", new ClipMotion(rig.Clip("walk")));
        a.MoveFloat("speed", 2f, 4f);
        a.Update(0.25f);
        Assert.Equal(1f, a.GetFloat("speed"), 5);
        a.Update(1f);
        Assert.Equal(2f, a.GetFloat("speed"), 5);
        a.MoveFloat("speed", 0f, 4f);
        a.UpdateTo(0);
        a.UpdateTo(50);
        Assert.Equal(0f, a.GetFloat("speed"));
    }

    private readonly record struct AnimatorState(float Progress, System.Numerics.Quaternion Rotation);

    [Fact]
    public void SteadyStateFramesAllocateNothing()
    {
        var a = Figure.CreateAnimator();
        var ik = Figure.Rig;
        long Window(int frames, int from)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var f = from; f < from + frames; f++)
            {
                a.MoveFloat("speed", (f / 90) % 3, 4f);
                if (f % 100 == 0)
                {
                    a.Trigger("jump");
                }

                a.UpdateTo(f / 60.0);
                TwoBoneIk.Solve(ik, a.Pose, ik.Node("ThighR"), ik.Node("ShinR"), ik.Node("FootR"), new System.Numerics.Vector3(0.1f, 0.1f, -0.2f), new System.Numerics.Vector3(0.1f, 0.5f, -1f));
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        // Warm up (jitting allocates), then take the best of three windows that cover blends, jumps and fades.
        Window(600, 0);
        var best = long.MaxValue;
        for (var w = 0; w < 3; w++)
        {
            best = Math.Min(best, Window(600, 600 + (w * 600)));
        }

        Assert.Equal(0, best);
    }
}
