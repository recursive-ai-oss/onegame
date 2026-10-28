using System.Numerics;
using OneGame.Core;
using OneGame.Gltf;

namespace OneGame.AotSmoke;

/// <summary>The animation layer under NativeAOT: a skinned figure with a blend space, a state machine, a crossfade, root motion baked to fixed point, and IK.</summary>
internal static class AnimationSmoke
{
    public static string? Run(byte[] figure)
    {
        var model = GltfLoader.Load(figure);
        var rig = new Rig(model);
        var animator = new Animator(rig, rig.Node("Root"));
        animator.AddFloat("speed").AddTrigger("jump");
        animator.AddState("move", new BlendSpace1DMotion(new BlendSpace1D(0, 1, 2), rig.Clip("idle"), rig.Clip("walk"), rig.Clip("run")) { XParameter = "speed" });
        animator.AddState("jump", new ClipMotion(rig.Clip("jump")), loop: false);
        animator.Transition("move", "jump").WhenTrigger("jump").Fade(0.1f);
        animator.Transition("jump", "move").AtExit(0.9f).Fade(0.1f);

        var jumped = false;
        for (var f = 0; f < 240; f++)
        {
            animator.MoveFloat("speed", f < 120 ? 1.5f : 0.2f, 4f);
            if (f == 60)
            {
                animator.Trigger("jump");
            }

            animator.UpdateTo(f / 60.0);
            jumped |= animator.CurrentState == "jump";
        }

        if (!jumped || animator.CurrentState != "move")
        {
            return "the state machine did not go to the jump and back";
        }

        var space = new BlendSpace2D(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1));
        var weights = new float[4];
        space.Weights(new Vector2(0.25f, 0.75f), weights);
        if (MathF.Abs(weights.Sum() - 1f) > 1e-5f)
        {
            return "2D blend weights do not add up to one";
        }

        var track = RootMotion.Bake(rig.Clip("walk"), rig.Node("Root"), 60);
        if (track.Length != 60 || track.Total.Z.ToSingle() > -0.99f)
        {
            return $"walk root motion is wrong: {track.Length} ticks, total {track.Total}";
        }

        var moved = track.Step(Transform3.Identity, 0);
        if (moved.Position.Z >= Fixed.Zero)
        {
            return "root motion did not move the transform forward";
        }

        var pose = rig.CreatePose();
        rig.Sample(rig.Clip("walk"), 0.3f, pose);
        var target = new Vector3(0.2f, 0.15f, -0.2f);
        if (!TwoBoneIk.Solve(rig, pose, rig.Node("ThighR"), rig.Node("ShinR"), rig.Node("FootR"), target, new Vector3(0.1f, 0.5f, -1f))
            || Vector3.Distance(rig.ComputeWorld(pose)[rig.Node("FootR")].Translation, target) > 1e-3f)
        {
            return "two-bone IK missed its target";
        }

        return float.IsFinite(animator.Pose.Translation[rig.Node("Hips")].Y) ? null : "the pose is not finite";
    }
}
