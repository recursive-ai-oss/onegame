namespace OneGame.Gltf;

/// <summary>Two-bone inverse kinematics for a leg or an arm: turns the upper and middle joints so the end joint reaches a target.</summary>
public static class TwoBoneIk
{
    /// <summary>
    /// Changes the local rotations of <paramref name="upper"/> and <paramref name="mid"/> so that <paramref name="end"/> sits at
    /// <paramref name="target"/>, or as close as the bones' lengths allow (the limb then points straight at it). The bend goes towards
    /// <paramref name="pole"/>. Call it after the clips and blends, on the final pose. Bones should have uniform scale.
    /// </summary>
    /// <param name="rig">The rig of the pose.</param>
    /// <param name="pose">The pose to change.</param>
    /// <param name="upper">The first joint (hip or shoulder).</param>
    /// <param name="mid">The middle joint (knee or elbow), a child of <paramref name="upper"/>.</param>
    /// <param name="end">The end joint (ankle or wrist), a child of <paramref name="mid"/>.</param>
    /// <param name="target">Where the end joint should be, in the model's root space.</param>
    /// <param name="pole">A point in the model's root space the middle joint bends towards (a knee goes forward).</param>
    /// <returns>True when the target was within reach.</returns>
    /// <exception cref="ArgumentException">The joints are not a chain.</exception>
    public static bool Solve(Rig rig, NodePose pose, int upper, int mid, int end, Vector3 target, Vector3 pole)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(pose);
        var nodes = rig.Model.Nodes;
        if (nodes[mid].Parent != upper || nodes[end].Parent != mid)
        {
            throw new ArgumentException("The joints must be a chain: mid is a child of upper and end is a child of mid.");
        }

        var w = rig.ComputeWorld(pose);
        var a = w[upper].Translation;
        var b = w[mid].Translation;
        var c = w[end].Translation;
        var l1 = Vector3.Distance(a, b);
        var l2 = Vector3.Distance(b, c);
        var toTarget = target - a;
        var dist = toTarget.Length();
        if (dist < 1e-6f || l1 < 1e-6f || l2 < 1e-6f)
        {
            return false;
        }

        var dir = toTarget / dist;
        var reach = Math.Clamp(dist, MathF.Abs(l1 - l2) + 1e-4f, l1 + l2 - 1e-4f);
        var reachable = dist <= l1 + l2 + 1e-4f;

        // Law of cosines: the middle joint sits `along` the line to the target and `off` it, on the pole's side.
        var along = ((l1 * l1) - (l2 * l2) + (reach * reach)) / (2f * reach);
        var off = MathF.Sqrt(Math.Max(0f, (l1 * l1) - (along * along)));
        var bend = pole - a;
        bend -= dir * Vector3.Dot(bend, dir);
        if (bend.LengthSquared() < 1e-10f)
        {
            bend = (b - a) - (dir * Vector3.Dot(b - a, dir));
        }

        bend = bend.LengthSquared() < 1e-10f ? PerpendicularTo(dir) : Vector3.Normalize(bend);
        var newB = a + (dir * along) + (bend * off);

        Rotate(rig, pose, upper, b - a, newB - a);
        w = rig.ComputeWorld(pose);
        var newC = w[end].Translation;
        var endGoal = reachable ? target : newB + (Vector3.Normalize(target - newB) * l2);
        Rotate(rig, pose, mid, newC - w[mid].Translation, endGoal - w[mid].Translation);
        return reachable;
    }

    // Turns a node so that the world direction `from` becomes `to`: the new world rotation is the old one followed by the shortest turn.
    private static void Rotate(Rig rig, NodePose pose, int node, Vector3 from, Vector3 to)
    {
        var turn = FromTo(Vector3.Normalize(from), Vector3.Normalize(to));
        var w = rig.ComputeWorld(pose);
        var nodes = rig.Model.Nodes;
        var worldRot = RotationOf(w[node]);
        var parentRot = nodes[node].Parent < 0 ? Matrix4x4.Identity : RotationOf(w[nodes[node].Parent]);
        Matrix4x4.Invert(parentRot, out var parentInverse);

        // Row vectors: world = local * parent, so local = world * inverse(parent).
        var local = worldRot * Matrix4x4.CreateFromQuaternion(turn) * parentInverse;
        pose.Rotation[node] = Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(local));
    }

    private static Matrix4x4 RotationOf(Matrix4x4 m)
    {
        var x = Vector3.Normalize(new Vector3(m.M11, m.M12, m.M13));
        var y = Vector3.Normalize(new Vector3(m.M21, m.M22, m.M23));
        var z = Vector3.Normalize(new Vector3(m.M31, m.M32, m.M33));
        return new Matrix4x4(x.X, x.Y, x.Z, 0, y.X, y.Y, y.Z, 0, z.X, z.Y, z.Z, 0, 0, 0, 0, 1);
    }

    private static Quaternion FromTo(Vector3 u, Vector3 v)
    {
        var d = Vector3.Dot(u, v);
        if (d > 0.999999f)
        {
            return Quaternion.Identity;
        }

        if (d < -0.999999f)
        {
            var axis = PerpendicularTo(u);
            return new Quaternion(axis, 0f);
        }

        var cross = Vector3.Cross(u, v);
        return Quaternion.Normalize(new Quaternion(cross, 1f + d));
    }

    private static Vector3 PerpendicularTo(Vector3 v)
        => Vector3.Normalize(MathF.Abs(v.X) < 0.9f ? Vector3.Cross(v, Vector3.UnitX) : Vector3.Cross(v, Vector3.UnitY));
}
