using System.Numerics;
using System.Text;
using OneGame.Gltf;

namespace OneGame.AotSmoke;

/// <summary>The glTF importer under the native publish: a binary skinned model with a PNG texture, an embedded text model, animation, and a refused file.</summary>
internal static class GltfSmoke
{
    public static string? Run(byte[] foxGlb, byte[] boxGltf)
    {
        var fox = GltfLoader.Load(foxGlb);
        if (fox.Nodes.Length != 26 || fox.Animations.Length != 3 || fox.Skins.Length != 1 || fox.Images[0].Status != ImageStatus.Decoded || fox.Images[0].Pixels is null)
        {
            return $"fox: nodes {fox.Nodes.Length}, animations {fox.Animations.Length}, skins {fox.Skins.Length}";
        }

        // Pose the Run clip, build the world matrices and the joint matrices a skinning shader would use.
        var run = fox.Animations[2];
        var pose = fox.CreateRestPose();
        run.Apply(pose, run.Duration * 0.4f);
        var world = new Matrix4x4[fox.Nodes.Length];
        pose.ComputeWorld(fox, world);
        var meshNode = Array.FindIndex(fox.Nodes, n => n.Skin >= 0);
        var joints = new Matrix4x4[fox.Skins[0].Joints.Length];
        fox.Skins[0].ComputeJointMatrices(world, meshNode, joints);
        if (!float.IsFinite(joints[3].M11) || fox.Meshes[0].Primitives[0].PackVertices().Length != 1728 * GltfPrimitive.PackedVertexSize || fox.ComputeWorldBounds() is null)
        {
            return "fox: skinning or packing";
        }

        var box = GltfLoader.Load(boxGltf);
        var prim = box.Meshes[0].Primitives[0];
        if (prim.VertexCount != 24 || prim.Indices.Length != 36 || box.Materials.Length != 1)
        {
            return $"box: {prim.VertexCount} vertices, {prim.Indices.Length} indices";
        }

        try
        {
            _ = GltfLoader.Load(foxGlb.AsMemory(0, foxGlb.Length / 2));
            return "a truncated file was accepted";
        }
        catch (GltfException ex) when (ex.Code is "glb_invalid" or "buffer_truncated")
        {
        }

        try
        {
            _ = GltfLoader.Load(Encoding.UTF8.GetBytes("{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"byteLength\":4,\"uri\":\"../x.bin\"}]}"));
            return "an escaping uri was accepted";
        }
        catch (GltfException ex) when (ex.Code == "uri_not_allowed")
        {
        }

        return null;
    }
}
