using System.Text;
using OneGame.Core;
using OneGame.Gltf;

namespace OneGame.RootMotionProbe;

/// <summary>Bakes each clip of the character figure and describes the result as raw fixed-point numbers.</summary>
internal static class Probe
{
    public static string Run(byte[] figure)
    {
        var rig = new Rig(GltfLoader.Load(figure));
        var root = rig.Node("Root");
        var text = new StringBuilder();
        foreach (var name in new[] { "idle", "walk", "run", "jump" })
        {
            var track = RootMotion.Bake(rig.Clip(name), root, 60);
            // FNV-1a over every step's raw numbers: a single-bit difference in any tick changes it.
            var h = 14695981039346656037UL;
            for (var i = 0; i < track.Length; i++)
            {
                var d = track.DeltaAt(i);
                foreach (var raw in new[] { d.X.Raw, d.Y.Raw, d.Z.Raw })
                {
                    h = (h ^ (ulong)raw) * 1099511628211UL;
                }
            }

            text.Append($"{name} ticks={track.Length} total={track.Total.X.Raw},{track.Total.Y.Raw},{track.Total.Z.Raw} hash={h:x16}\n");
        }

        return text.ToString();
    }
}
