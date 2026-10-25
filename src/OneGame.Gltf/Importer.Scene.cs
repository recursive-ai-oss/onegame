namespace OneGame.Gltf;

internal sealed partial class Importer
{
    private void ReadNodes()
    {
        var count = _root.ArrayLength("nodes", _limits.MaxNodes);
        var nodes = new GltfNode[count];
        var lightCount = LightCount();
        var cameraCount = _root.ArrayLength("cameras", _limits.MaxSmallLists);
        for (var i = 0; i < count; i++)
        {
            var n = _root.Item("nodes", i);
            var node = new GltfNode
            {
                Name = n.Str("name"),
                Children = n.Refs("children", count, count),
                Mesh = n.Ref("mesh", _model.Meshes.Length),
                Camera = n.Ref("camera", cameraCount),
                Light = Ext(n, "KHR_lights_punctual") is { } lp ? lp.Ref("light", lightCount) : -1,
            };
            node.Skin = n.TryGet("skin", out _) ? n.ReqInt("skin", 0) : -1;
            var matrix = n.Floats("matrix", 16);
            var t = n.Floats("translation", 3);
            var r = n.Floats("rotation", 4);
            var s = n.Floats("scale", 3);
            if (matrix is not null)
            {
                var m = new Matrix4x4(
                    matrix[0], matrix[1], matrix[2], matrix[3], matrix[4], matrix[5], matrix[6], matrix[7],
                    matrix[8], matrix[9], matrix[10], matrix[11], matrix[12], matrix[13], matrix[14], matrix[15]);
                node.Local = m;
                node.FromMatrix = true;
                if (Matrix4x4.Decompose(m, out var scale, out var rotation, out var translation))
                {
                    node.Scale = scale;
                    node.Rotation = rotation;
                    node.Translation = translation;
                }
            }
            else
            {
                node.Translation = t is null ? Vector3.Zero : new Vector3(t[0], t[1], t[2]);
                node.Scale = s is null ? Vector3.One : new Vector3(s[0], s[1], s[2]);
                var q = r is null ? Quaternion.Identity : new Quaternion(r[0], r[1], r[2], r[3]);
                var len = q.Length();
                node.Rotation = len > 1e-12f && float.IsFinite(len) ? Quaternion.Normalize(q) : Quaternion.Identity;
                node.Local = Matrix4x4.CreateScale(node.Scale) * Matrix4x4.CreateFromQuaternion(node.Rotation) * Matrix4x4.CreateTranslation(node.Translation);
            }

            node.Weights = n.Floats("weights", node.Mesh >= 0 ? _model.Meshes[node.Mesh].MorphTargetCount : 0);
            nodes[i] = node;
        }

        // Each node has at most one parent, and walking down from the roots must reach every node: a node left over sits in a cycle.
        for (var i = 0; i < count; i++)
        {
            foreach (var c in nodes[i].Children)
            {
                if (c == i || nodes[c].Parent >= 0)
                {
                    throw new GltfException("node_graph", $"nodes[{N(c)}] is the child of more than one node (or of itself).");
                }

                nodes[c].Parent = i;
            }
        }

        var order = new int[count];
        var filled = 0;
        for (var i = 0; i < count; i++)
        {
            if (nodes[i].Parent < 0)
            {
                order[filled++] = i;
            }
        }

        for (var head = 0; head < filled; head++)
        {
            var node = nodes[order[head]];
            foreach (var c in node.Children)
            {
                nodes[c].Depth = node.Depth + 1;
                if (nodes[c].Depth > _limits.MaxNodeDepth)
                {
                    throw new GltfException("limit_exceeded", $"The node tree is deeper than {N(_limits.MaxNodeDepth)}.");
                }

                order[filled++] = c;
            }
        }

        if (filled != count)
        {
            throw new GltfException("node_graph", "The node hierarchy contains a cycle.");
        }

        foreach (var i in order)
        {
            var node = nodes[i];
            node.World = node.Parent < 0 ? node.Local : node.Local * nodes[node.Parent].World;
        }

        _model.Nodes = nodes;
        _model.ParentFirst = order;
    }

    private int LightCount()
    {
        var lights = Ext(_root, "KHR_lights_punctual");
        return lights is { } l ? l.ArrayLength("lights", _limits.MaxSmallLists) : 0;
    }

    private GltfSkin[] ReadSkins()
    {
        var count = _root.ArrayLength("skins", _limits.MaxSkins);
        var skins = new GltfSkin[count];
        for (var i = 0; i < count; i++)
        {
            var s = _root.Item("skins", i);
            var joints = s.Refs("joints", _model.Nodes.Length, _limits.MaxJointsPerSkin);
            if (joints.Length == 0)
            {
                throw s.Error("schema", "joints", "must hold at least one joint.");
            }

            var ibm = new Matrix4x4[joints.Length];
            var ia = s.Ref("inverseBindMatrices", _acc.Count);
            if (ia >= 0)
            {
                var comps = _acc.Describe(ia).Components;
                if (comps != 16 || _acc.ElementCount(ia) < joints.Length)
                {
                    throw s.Error("skin_invalid", "inverseBindMatrices", $"must be a MAT4 accessor with at least {N(joints.Length)} matrices.");
                }

                var f = _acc.ReadFloats(ia, s.Path("inverseBindMatrices"));
                for (var j = 0; j < joints.Length; j++)
                {
                    var o = j * 16;
                    ibm[j] = new Matrix4x4(
                        f[o], f[o + 1], f[o + 2], f[o + 3], f[o + 4], f[o + 5], f[o + 6], f[o + 7],
                        f[o + 8], f[o + 9], f[o + 10], f[o + 11], f[o + 12], f[o + 13], f[o + 14], f[o + 15]);
                }
            }
            else
            {
                Array.Fill(ibm, Matrix4x4.Identity);
            }

            skins[i] = new GltfSkin { Name = s.Str("name"), Joints = joints, InverseBindMatrices = ibm, Skeleton = s.Ref("skeleton", _model.Nodes.Length) };
        }

        foreach (var node in _model.Nodes)
        {
            if (node.Skin >= skins.Length)
            {
                throw new GltfException("reference", $"A node names skin {N(node.Skin)}, but only {N(skins.Length)} exist.");
            }
        }

        return skins;
    }

    /// <summary>Joint indices in vertices must stay inside the joint list of the skin of every node that draws the mesh.</summary>
    private void ValidateSkinUse()
    {
        foreach (var node in _model.Nodes)
        {
            if (node.Skin < 0 || node.Mesh < 0)
            {
                continue;
            }

            var jointCount = _model.Skins[node.Skin].Joints.Length;
            foreach (var prim in _model.Meshes[node.Mesh].Primitives)
            {
                if (prim.Joints is null)
                {
                    continue;
                }

                foreach (var j in prim.Joints)
                {
                    if (j.A >= jointCount || j.B >= jointCount || j.C >= jointCount || j.D >= jointCount)
                    {
                        throw new GltfException("skin_invalid", $"A vertex names a joint beyond the {N(jointCount)} joints of its skin.");
                    }
                }
            }
        }
    }

    private GltfCamera[] ReadCameras()
    {
        var count = _root.ArrayLength("cameras", _limits.MaxSmallLists);
        var cameras = new GltfCamera[count];
        for (var i = 0; i < count; i++)
        {
            var c = _root.Item("cameras", i);
            var cam = new GltfCamera { Name = c.Str("name") };
            switch (c.Str("type"))
            {
                case "perspective":
                    var p = c.Obj("perspective") ?? throw c.Error("schema", "perspective", "is required.");
                    cam.Perspective = true;
                    cam.YFov = p.ReqFloat("yfov");
                    cam.AspectRatio = p.FloatOpt("aspectRatio");
                    cam.ZNear = p.ReqFloat("znear");
                    cam.ZFar = p.FloatOpt("zfar");
                    break;
                case "orthographic":
                    var o = c.Obj("orthographic") ?? throw c.Error("schema", "orthographic", "is required.");
                    cam.XMag = o.ReqFloat("xmag");
                    cam.YMag = o.ReqFloat("ymag");
                    cam.ZNear = o.ReqFloat("znear");
                    cam.ZFar = o.ReqFloat("zfar");
                    break;
                default:
                    throw c.Error("schema", "type", "must be perspective or orthographic.");
            }

            cameras[i] = cam;
        }

        return cameras;
    }

    private GltfLight[] ReadLights()
    {
        if (Ext(_root, "KHR_lights_punctual") is not { } ext)
        {
            return [];
        }

        var count = ext.ArrayLength("lights", _limits.MaxSmallLists);
        var lights = new GltfLight[count];
        for (var i = 0; i < count; i++)
        {
            var l = ext.Item("lights", i);
            var light = new GltfLight
            {
                Name = l.Str("name"),
                Color = Vec3(l.Floats("color", 3), Vector3.One),
                Intensity = l.Float("intensity", 1),
                Range = l.FloatOpt("range"),
                Type = l.Str("type") switch
                {
                    "directional" => GltfLightType.Directional,
                    "point" => GltfLightType.Point,
                    "spot" => GltfLightType.Spot,
                    _ => throw l.Error("schema", "type", "must be directional, point or spot."),
                },
            };
            if (l.Obj("spot") is { } spot)
            {
                light.InnerConeAngle = spot.Float("innerConeAngle", 0);
                light.OuterConeAngle = spot.Float("outerConeAngle", MathF.PI / 4);
            }

            lights[i] = light;
        }

        return lights;
    }

    private void ReadScenes()
    {
        var count = _root.ArrayLength("scenes", _limits.MaxSmallLists);
        var scenes = new GltfScene[count];
        for (var i = 0; i < count; i++)
        {
            var s = _root.Item("scenes", i);
            var roots = s.Refs("nodes", _model.Nodes.Length, _model.Nodes.Length);
            foreach (var r in roots)
            {
                if (_model.Nodes[r].Parent >= 0)
                {
                    throw s.Error("node_graph", "nodes", $"lists node {N(r)}, which is not a root.");
                }
            }

            scenes[i] = new GltfScene { Name = s.Str("name"), Roots = roots };
        }

        _model.Scenes = scenes;
        _model.DefaultScene = count == 0 ? -1 : Math.Max(_root.Ref("scene", count), 0);
    }
}
