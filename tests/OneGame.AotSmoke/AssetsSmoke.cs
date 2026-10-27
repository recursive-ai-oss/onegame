using OneGame.Control;
using OneGame.Host;
using OneYaml;

namespace OneGame.AotSmoke;

internal sealed record SmokeData(string Name, int Count);

[YamlSerializable(typeof(SmokeData))]
internal sealed partial class SmokeYaml : YamlSerializerContext;

/// <summary>Loads a texture and a data file, edits them and lets the development reloader pick the edits up, so the native publish compiles the asset path.</summary>
internal static class AssetsSmoke
{
    public static void Run()
    {
        var dir = Path.Combine(Path.GetTempPath(), "aot-assets-" + Environment.ProcessId);
        Directory.CreateDirectory(Path.Combine(dir, "data"));
        Directory.CreateDirectory(Path.Combine(dir, "art"));
        try
        {
            var yaml = Path.Combine(dir, "data", "s.yaml");
            var png = Path.Combine(dir, "art", "s.png");
            File.WriteAllText(yaml, "name: a\ncount: 1\n");
            File.WriteAllBytes(png, Png.Encode(new CapturedFrame(2, 2, new byte[16], "smoke")));
            using var assets = new Assets(new FileAssetSource([dir]));
            var data = assets.LoadData("data/s.yaml", SmokeYaml.Default.SmokeData);
            var tex = assets.LoadTexture("art/s.png");
            File.WriteAllText(yaml, "name: b\ncount: 22\n");
            File.SetLastWriteTimeUtc(yaml, DateTime.UtcNow.AddSeconds(5));
            var changed = new AssetReloader(assets).Check();
            if (!tex.IsReady || changed.Count != 1 || !data.IsPending || data.Value.Count != 1 || assets.ApplyPendingData() != 1 || data.Value.Count != 22)
            {
                throw new InvalidOperationException("The asset smoke failed.");
            }
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
