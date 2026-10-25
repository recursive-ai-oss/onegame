using OneGame.Gltf;

namespace OneGame.Render3D.Tests;

public class SmokeTests
{
    [Fact]
    public void BoxRendersWithValidationClean()
    {
        var path = Paths.Model("Box/glTF/Box.gltf");
        Assert.NotNull(path);
        using var rig = new Rig();
        var model = rig.Load(path);
        var scene = new Scene3D();
        scene.Add(model);
        var b = model.Source.ComputeWorldBounds()!.Value;
        scene.Camera = Camera3D.Frame(b.Min, b.Max, 30, 20, 320 / 240f);
        var pixels = rig.Capture(scene, "box");
        Assert.Contains(pixels, p => p != pixels[0]);
    }
}
