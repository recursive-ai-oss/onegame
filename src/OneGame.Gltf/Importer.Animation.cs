namespace OneGame.Gltf;

internal sealed partial class Importer
{
    private AnimationClip[] ReadAnimations()
    {
        var count = _root.ArrayLength("animations", _limits.MaxAnimations);
        var clips = new AnimationClip[count];
        for (var i = 0; i < count; i++)
        {
            clips[i] = ReadClip(_root.Item("animations", i), i);
        }

        return clips;
    }

    private AnimationClip ReadClip(JObj a, int index)
    {
        var samplerCount = a.ArrayLength("samplers", _limits.MaxAccessors);
        var channelCount = a.ArrayLength("channels", _limits.MaxAccessors);
        var samplers = new AnimationSampler?[samplerCount];
        var channels = new List<AnimationChannel>(channelCount);
        var duration = 0f;
        for (var c = 0; c < channelCount; c++)
        {
            var ch = a.Item("channels", c);
            var sampler = ch.ReqRef("sampler", samplerCount);
            var target = ch.Obj("target") ?? throw ch.Error("schema", "target", "is required.");
            var pathName = target.Str("path");
            if (pathName is not ("translation" or "rotation" or "scale" or "weights"))
            {
                // KHR_animation_pointer and other targets are not part of the supported set.
                Warn("ignored_animation_channel", $"animations[{N(index)}].channels[{N(c)}] animates '{GltfUri.Show(pathName)}', which is not supported.");
                continue;
            }

            var node = target.Ref("node", _model.Nodes.Length);
            if (node < 0)
            {
                continue;
            }

            var path = pathName switch
            {
                "translation" => AnimationPath.Translation,
                "rotation" => AnimationPath.Rotation,
                "scale" => AnimationPath.Scale,
                _ => AnimationPath.Weights,
            };
            int components;
            if (path == AnimationPath.Weights)
            {
                var mesh = _model.Nodes[node].Mesh;
                components = mesh >= 0 ? _model.Meshes[mesh].MorphTargetCount : 0;
                if (components == 0)
                {
                    Warn("ignored_animation_channel", $"animations[{N(index)}].channels[{N(c)}] animates weights of a node without morph targets.");
                    continue;
                }
            }
            else
            {
                components = path == AnimationPath.Rotation ? 4 : 3;
            }

            if (_model.Nodes[node].FromMatrix)
            {
                Warn("animated_matrix_node", $"animations[{N(index)}] animates node {N(node)}, which is given by a matrix; the spec forbids this and the channel was ignored.");
                continue;
            }

            if (samplers[sampler] is null)
            {
                samplers[sampler] = ReadSampler(a.Item("samplers", sampler), components, path == AnimationPath.Rotation);
            }
            else if (samplers[sampler]!.Components != components)
            {
                throw ch.Error("animation_invalid", "sampler", "is used for properties with different value sizes.");
            }

            var used = samplers[sampler]!;
            duration = Math.Max(duration, used.Input.Length == 0 ? 0 : used.Input[^1]);
            channels.Add(new AnimationChannel { Sampler = sampler, Node = node, Path = path });
        }

        var all = new AnimationSampler[samplerCount];
        for (var s = 0; s < samplerCount; s++)
        {
            all[s] = samplers[s] ?? new AnimationSampler();
        }

        return new AnimationClip { Name = a.Str("name"), Channels = [.. channels], Samplers = all, Duration = duration };
    }

    private AnimationSampler ReadSampler(JObj s, int components, bool rotation)
    {
        var inputAccessor = s.ReqRef("input", _acc.Count);
        var outputAccessor = s.ReqRef("output", _acc.Count);
        var mode = s.Str("interpolation", "LINEAR") switch
        {
            "LINEAR" => AnimationInterpolation.Linear,
            "STEP" => AnimationInterpolation.Step,
            "CUBICSPLINE" => AnimationInterpolation.CubicSpline,
            _ => throw s.Error("schema", "interpolation", "must be LINEAR, STEP or CUBICSPLINE."),
        };
        var keys = _acc.ElementCount(inputAccessor);
        if (keys > _limits.MaxKeyframes)
        {
            throw s.Error("limit_exceeded", "input", $"has {N(keys)} keyframes; the limit is {N(_limits.MaxKeyframes)}.");
        }

        if (_acc.Describe(inputAccessor).Components != 1)
        {
            throw s.Error("animation_invalid", "input", "must be a SCALAR accessor of times.");
        }

        var perKey = mode == AnimationInterpolation.CubicSpline ? 3L * components : components;
        var outAcc = _acc.Describe(outputAccessor);
        if ((long)_acc.ElementCount(outputAccessor) * outAcc.Components != keys * perKey)
        {
            throw s.Error("animation_invalid", "output", $"holds {N(_acc.ElementCount(outputAccessor))} elements, which does not match {N(keys)} keyframes of {N((int)perKey)} values.");
        }

        if (mode == AnimationInterpolation.CubicSpline && keys < 2)
        {
            throw s.Error("animation_invalid", "input", "needs at least two keyframes for cubic spline interpolation.");
        }

        var input = _acc.ReadFloats(inputAccessor, s.Path("input"));
        for (var k = 0; k < input.Length; k++)
        {
            // glTF: times are non-negative and increase.
            if (!float.IsFinite(input[k]) || input[k] < 0 || (k > 0 && input[k] < input[k - 1]))
            {
                throw s.Error("animation_invalid", "input", $"keyframe {N(k)} has a negative, non-finite or decreasing time.");
            }
        }

        var output = _acc.ReadFloats(outputAccessor, s.Path("output"));
        return new AnimationSampler { Input = input, Output = output, Components = components, Interpolation = mode, IsRotation = rotation };
    }
}
