using System.Numerics;
using System.Text.Json;

namespace OneGame.Gltf.Tests;

/// <summary>Poses at named times against values worked out from the keyframes in the file with the formulas of the glTF specification.</summary>
public class AnimationTests
{
    private const double Tolerance = 1e-5;

    /// <summary>The value of a sampler at a time, in double precision, straight from the specification text (Animations, Appendix C).</summary>
    internal static double[] Expected(string interpolation, double[] times, double[] output, int comps, bool rotation, double time)
    {
        var cubic = interpolation == "CUBICSPLINE";
        var stride = cubic ? comps * 3 : comps;
        var off = cubic ? comps : 0;
        double[] Value(int k) => output.AsSpan((k * stride) + off, comps).ToArray();
        var n = times.Length;
        if (time <= times[0])
        {
            return Value(0);
        }

        if (time >= times[n - 1])
        {
            return Value(n - 1);
        }

        var key = 0;
        while (times[key + 1] <= time)
        {
            key++;
        }

        var td = times[key + 1] - times[key];
        var t = (time - times[key]) / td;
        var a = Value(key);
        var b = Value(key + 1);
        double[] result;
        switch (interpolation)
        {
            case "STEP":
                return a;
            case "LINEAR":
                if (rotation)
                {
                    return Slerp(a, b, t);
                }

                result = new double[comps];
                for (var i = 0; i < comps; i++)
                {
                    result[i] = ((1 - t) * a[i]) + (t * b[i]);
                }

                return result;
            default:
                result = new double[comps];
                var t2 = t * t;
                var t3 = t2 * t;
                for (var i = 0; i < comps; i++)
                {
                    var outTangent = output[(key * stride) + (2 * comps) + i];
                    var inTangent = output[((key + 1) * stride) + i];
                    result[i] = (((2 * t3) - (3 * t2) + 1) * a[i]) + ((t3 - (2 * t2) + t) * td * outTangent) + (((-2 * t3) + (3 * t2)) * b[i]) + ((t3 - t2) * td * inTangent);
                }

                if (rotation)
                {
                    var len = Math.Sqrt(result.Sum(x => x * x));
                    result = [.. result.Select(x => x / len)];
                }

                return result;
        }
    }

    private static double[] Slerp(double[] a, double[] b, double t)
    {
        var dot = (a[0] * b[0]) + (a[1] * b[1]) + (a[2] * b[2]) + (a[3] * b[3]);
        var sign = dot < 0 ? -1.0 : 1.0;
        dot *= sign;
        var r = new double[4];
        var theta = Math.Acos(Math.Min(1, dot));
        double wa, wb;
        if (Math.Sin(theta) < 1e-9)
        {
            wa = 1 - t;
            wb = t;
        }
        else
        {
            wa = Math.Sin((1 - t) * theta) / Math.Sin(theta);
            wb = Math.Sin(t * theta) / Math.Sin(theta);
        }

        for (var i = 0; i < 4; i++)
        {
            r[i] = (wa * a[i]) + (wb * sign * b[i]);
        }

        var len = Math.Sqrt(r.Sum(x => x * x));
        return [.. r.Select(x => x / len)];
    }

    private static double[] Times(double[] keys)
    {
        var list = new List<double> { -1, 0, keys[^1], keys[^1] + 1 };
        for (var k = 0; k + 1 < keys.Length; k++)
        {
            foreach (var f in new[] { 0.0, 0.1, 0.25, 0.5, 0.9 })
            {
                list.Add(keys[k] + (f * (keys[k + 1] - keys[k])));
            }
        }

        return [.. list];
    }

    private static void AssertClose(double[] expected, float[] actual, string what, bool quaternion)
    {
        Assert.Equal(expected.Length, actual.Length);
        var sign = 1.0;
        if (quaternion)
        {
            // q and -q are the same rotation.
            var dot = 0.0;
            for (var i = 0; i < 4; i++)
            {
                dot += expected[i] * actual[i];
            }

            sign = dot < 0 ? -1 : 1;
        }

        for (var i = 0; i < expected.Length; i++)
        {
            Assert.True(Math.Abs((sign * expected[i]) - actual[i]) <= Tolerance + (Tolerance * Math.Abs(expected[i])), $"{what}[{i}]: expected {sign * expected[i]}, got {actual[i]}");
        }
    }

    private static float[] Read(NodePose pose, int node, AnimationPath path) => path switch
    {
        AnimationPath.Translation => [pose.Translation[node].X, pose.Translation[node].Y, pose.Translation[node].Z],
        AnimationPath.Scale => [pose.Scale[node].X, pose.Scale[node].Y, pose.Scale[node].Z],
        AnimationPath.Rotation => [pose.Rotation[node].X, pose.Rotation[node].Y, pose.Rotation[node].Z, pose.Rotation[node].W],
        _ => pose.Weights[node],
    };

    /// <summary>Applies every animation of a file at many times and compares every animated property with the formulas.</summary>
    private static int CheckAllAnimations(string relative, string? only = null)
    {
        var path = TestData.Path_(relative);
        var model = GltfLoader.LoadFile(path);
        using var raw = new RawGltf(path);
        var checkedValues = 0;
        var animations = raw.Root.GetProperty("animations");
        Assert.Equal(animations.GetArrayLength(), model.Animations.Length);
        for (var a = 0; a < model.Animations.Length; a++)
        {
            var json = animations[a];
            if (only is not null && json.GetProperty("name").GetString() != only)
            {
                continue;
            }

            var clip = model.Animations[a];
            foreach (var channel in json.GetProperty("channels").EnumerateArray())
            {
                var sampler = json.GetProperty("samplers")[channel.GetProperty("sampler").GetInt32()];
                var node = channel.GetProperty("target").GetProperty("node").GetInt32();
                var pathName = channel.GetProperty("target").GetProperty("path").GetString()!;
                var apath = pathName switch { "translation" => AnimationPath.Translation, "rotation" => AnimationPath.Rotation, "scale" => AnimationPath.Scale, _ => AnimationPath.Weights };
                var interpolation = sampler.TryGetProperty("interpolation", out var ip) ? ip.GetString()! : "LINEAR";
                var times = raw.Floats(sampler.GetProperty("input").GetInt32());
                var output = raw.Floats(sampler.GetProperty("output").GetInt32());
                var comps = apath switch { AnimationPath.Rotation => 4, AnimationPath.Weights => model.Meshes[model.Nodes[node].Mesh].MorphTargetCount, _ => 3 };
                foreach (var time in Times(times))
                {
                    var pose = model.CreateRestPose();
                    clip.Apply(pose, (float)time);
                    var expected = Expected(interpolation, times, output, comps, apath == AnimationPath.Rotation, (float)time);
                    AssertClose(expected, Read(pose, node, apath), $"{relative} '{json.GetProperty("name").GetString()}' node {node} {pathName} {interpolation} at {time}", apath == AnimationPath.Rotation);
                    checkedValues++;
                }
            }
        }

        return checkedValues;
    }

    [Fact]
    public void InterpolationTestMatchesTheSpecFormulas()
    {
        // Step, linear and cubic spline on translation, rotation and scale.
        var checkedValues = CheckAllAnimations("InterpolationTest/glTF/InterpolationTest.gltf");
        Assert.True(checkedValues > 100, checkedValues.ToString());
    }

    [Fact]
    public void CubicSplineAtAKeyframeIsTheKeyValueAndUsesTheTangents()
    {
        var model = TestData.Load("InterpolationTest/glTF/InterpolationTest.gltf");
        var clip = model.Animations.Single(c => c.Name == "CubicSpline Scale");
        var s = clip.Samplers[0];
        Assert.Equal(AnimationInterpolation.CubicSpline, s.Interpolation);
        Span<float> v = stackalloc float[3];
        // At key 1 the Hermite basis is (0, 0, 1, 0): exactly the stored value, ignoring the tangents.
        s.Evaluate(s.Input[1], v);
        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(s.Output[(1 * 9) + 3 + i], v[i]);
        }

        // Halfway, with t = 0.5: p = 0.5 v0 + 0.125 td b0 + 0.5 v1 - 0.125 td a1.
        var td = s.Input[2] - s.Input[1];
        var mid = s.Input[1] + (td / 2);
        s.Evaluate(mid, v);
        for (var i = 0; i < 3; i++)
        {
            var v0 = s.Output[(1 * 9) + 3 + i];
            var b0 = s.Output[(1 * 9) + 6 + i];
            var a1 = s.Output[(2 * 9) + i];
            var v1 = s.Output[(2 * 9) + 3 + i];
            var expected = (0.5 * v0) + (0.125 * td * b0) + (0.5 * v1) - (0.125 * td * a1);
            Assert.InRange(v[i], expected - 1e-5, expected + 1e-5);
        }
    }

    [Fact]
    public void MorphWeightsMatchTheSpecFormulasAtNamedTimes()
    {
        var path = TestData.Path_("AnimatedMorphCube/glTF/AnimatedMorphCube.gltf");
        var model = GltfLoader.LoadFile(path);
        Assert.Equal(2, model.Meshes[0].MorphTargetCount);
        Assert.Single(model.Animations);
        Assert.True(CheckAllAnimations("AnimatedMorphCube/glTF/AnimatedMorphCube.gltf") > 400);
        // Weights stay inside the clip's range and are written to the node that owns the mesh.
        var pose = model.CreateRestPose();
        model.Animations[0].Apply(pose, model.Animations[0].Duration * 0.5f);
        Assert.Equal(2, pose.Weights[0].Length);
    }

    [Fact]
    public void TheBinaryAndQuantizedMorphCubesAnimateTheSame()
    {
        var a = TestData.Load("AnimatedMorphCube/glTF/AnimatedMorphCube.gltf");
        var b = TestData.Load("AnimatedMorphCube/glTF-Binary/AnimatedMorphCube.glb");
        var pa = a.CreateRestPose();
        var pb = b.CreateRestPose();
        a.Animations[0].Apply(pa, 0.37f);
        b.Animations[0].Apply(pb, 0.37f);
        Assert.Equal(pa.Weights[0], pb.Weights[0]);
    }

    [Theory]
    [InlineData("Fox/glTF/Fox.gltf", "Run")]
    [InlineData("Fox/glTF-Binary/Fox.glb", "Walk")]
    [InlineData("Fox/glTF/Fox.gltf", "Survey")]
    public void FoxPosesMatchTheFormulas(string file, string clipName)
    {
        Assert.True(CheckAllAnimations(file, clipName) > 1000);
    }

    [Fact]
    public void FoxWorldAndJointMatricesFollowTheSkinningRules()
    {
        var path = TestData.Path_("Fox/glTF/Fox.gltf");
        var model = GltfLoader.LoadFile(path);
        using var raw = new RawGltf(path);
        var clip = model.Animations.Single(c => c.Name == "Run");
        var meshNode = Array.FindIndex(model.Nodes, n => n.Skin >= 0);
        var skin = model.Skins[model.Nodes[meshNode].Skin];
        var skinJson = raw.Root.GetProperty("skins")[model.Nodes[meshNode].Skin];
        var ibm = raw.Floats(skinJson.GetProperty("inverseBindMatrices").GetInt32());
        Assert.Equal(skin.Joints.Length * 16, ibm.Length);

        // At the rest pose the joint matrices are the identity when the file's nodes are in the bind pose, which Fox's are.
        var world = new Matrix4x4[model.Nodes.Length];
        var rest = model.CreateRestPose();
        rest.ComputeWorld(model, world);
        var joints = new Matrix4x4[skin.Joints.Length];
        skin.ComputeJointMatrices(world, meshNode, joints);
        for (var j = 0; j < joints.Length; j++)
        {
            Assert.True(D4.MaxDifference(D4.FromNumerics(joints[j]), D4.Identity()) < 2e-3, $"rest joint {j}");
        }

        // Posed: world = product of the posed local TRS up the parent chain, joint = inverse(meshWorld) * jointWorld * inverseBind.
        var pose = model.CreateRestPose();
        clip.Apply(pose, 0.4f);
        pose.ComputeWorld(model, world);
        double[,] ExpectedWorld(int node)
        {
            var m = D4.Trs(
                [pose.Translation[node].X, pose.Translation[node].Y, pose.Translation[node].Z],
                [pose.Rotation[node].X, pose.Rotation[node].Y, pose.Rotation[node].Z, pose.Rotation[node].W],
                [pose.Scale[node].X, pose.Scale[node].Y, pose.Scale[node].Z]);
            return model.Nodes[node].Parent < 0 ? m : D4.Mul(ExpectedWorld(model.Nodes[node].Parent), m);
        }

        skin.ComputeJointMatrices(world, meshNode, joints);
        var inverseMesh = D4.Inverse(ExpectedWorld(meshNode));
        for (var j = 0; j < joints.Length; j++)
        {
            Assert.True(D4.MaxDifference(D4.FromNumerics(world[skin.Joints[j]]), ExpectedWorld(skin.Joints[j])) < 1e-3, $"world of joint {j}");
            var expected = D4.Mul(D4.Mul(inverseMesh, ExpectedWorld(skin.Joints[j])), D4.FromColumnMajor(ibm.AsSpan(j * 16, 16)));
            Assert.True(D4.MaxDifference(D4.FromNumerics(joints[j]), expected) < 2e-3, $"joint matrix {j}");
        }
    }

    [Fact]
    public void TimesOutsideTheKeysAreClamped()
    {
        var model = TestData.Load("InterpolationTest/glTF/InterpolationTest.gltf");
        var clip = model.Animations.Single(c => c.Name == "Linear Scale");
        var s = clip.Samplers[0];
        var before = model.CreateRestPose();
        var after = model.CreateRestPose();
        clip.Apply(before, -5);
        clip.Apply(after, clip.Duration + 100);
        Assert.Equal(s.Output[0], before.Scale[1].X);
        Assert.Equal(s.Output[^3], after.Scale[1].X);
        clip.Apply(before, float.NaN);
        Assert.Equal(s.Output[0], before.Scale[1].X);
    }

    [Fact]
    public void ApplyingAClipLeavesOtherNodesAtRest()
    {
        var model = TestData.Load("InterpolationTest/glTF/InterpolationTest.gltf");
        var clip = model.Animations.Single(c => c.Name == "Step Scale");
        var rest = model.CreateRestPose();
        var pose = model.CreateRestPose();
        clip.Apply(pose, 1);
        Assert.Equal(rest.Scale[1], pose.Scale[1]);
        Assert.Equal(rest.Translation[0], pose.Translation[0]);
    }
}
