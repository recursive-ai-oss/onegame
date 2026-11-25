namespace OneGame.Render3D.Tests;

/// <summary>The wall-with-slits scene of the light shaft tests, shared with the browser runner.</summary>
internal static class ShaftScene
{
    /// <summary>Four wall pieces leave three slits 0.8 wide, 2.8 apart, in a wall 30 high so the light cannot pass over it.</summary>
    public static Shapes SlitShapes()
    {
        var wall = Shapes.Matte(0.5f, 0.35f, 0.3f);
        return new Shapes()
            .Ground(0f, 40f, Shapes.Matte(0.55f, 0.55f, 0.5f))
            .Box(new Vector3(-13.2f, 15f, 0f), new Vector3(20f, 30f, 0.4f), wall)
            .Box(new Vector3(-1.4f, 15f, 0f), new Vector3(2f, 30f, 0.4f), wall)
            .Box(new Vector3(1.4f, 15f, 0f), new Vector3(2f, 30f, 0.4f), wall)
            .Box(new Vector3(13.2f, 15f, 0f), new Vector3(20f, 30f, 0.4f), wall);
    }

    /// <summary>The scene: a dark room, the sun behind the wall (casting shadows), shafts on or off.</summary>
    public static Scene3D Slit(Model3D model, bool shafts = true)
    {
        var scene = new Scene3D { Camera = Camera3D.LookAt(new Vector3(-1f, 2f, 9f), new Vector3(0f, 2.6f, 0f), Vector3.UnitY) };
        scene.Add(model);
        scene.Environment.Intensity = 0.15f;
        scene.Environment.Background = BackgroundMode.Color;
        scene.Environment.BackgroundColor = new Vector3(0.02f, 0.025f, 0.03f);
        scene.Post.Bloom = false;
        var sun = Light3D.Directional(new Vector3(0.2f, -0.45f, 0.87f), new Vector3(1f, 0.93f, 0.8f), 3f);
        sun.CastShadows = true;
        sun.Volumetric = shafts;
        scene.Lights.Add(sun);
        scene.Volumetric.Enabled = shafts;
        scene.Volumetric.Density = 0.03f;
        scene.Volumetric.Intensity = 3.5f;
        scene.Volumetric.MaxDistance = 40f;
        scene.Shadows.MaxDistance = 60f;
        return scene;
    }
}
