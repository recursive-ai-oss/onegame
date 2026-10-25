using OneGame.Gltf.Tests.Fuzzing;
namespace OneGame.Gltf.Tests;
public class DumpTests
{
    [Fact]
    public void Dump()
    {
        var dir = Environment.GetEnvironmentVariable("DUMP_DIR");
        if (dir is null) { return; }
        foreach (var f in Directory.GetFiles(dir))
        {
            var t = GltfFuzzTargets.All.Single(x => Path.GetFileName(f).StartsWith(x.Name + "-", StringComparison.Ordinal));
            try { t.Run(File.ReadAllBytes(f)); Console.WriteLine("ok " + f); }
            catch (Exception e) { Console.WriteLine("FAIL " + f + "\n" + e); }
        }
    }
}
