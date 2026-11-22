using System;
using System.Numerics;
using OneGame.Core;
using OneGame.Host;
using OneGame.Render2D;
using OneGame.UI;
using GlowExample.Shaders;

namespace GlowExample;

/// <summary>
/// Which picture the gallery shows. The simulation only holds the number; everything below is view code.
/// </summary>
[Entity]
public partial class Gallery : Entity, IView2D
{
    /// <summary>0 a render texture drawn as sprites, 1 bloom on the frame, 2 a pass of the game's own, 3 bloom on one layer under a sharp HUD.</summary>
    [Synced] public partial int Mode { get; private set; }

    /// <summary>Switches picture.</summary>
    [Command]
    public void Show(int mode) => Mode = mode;

    private RenderTexture2D? _map;
    private PostChain? _bloom;
    private PostChain? _scan;
    private Material? _scanMaterial;

    [View]
    public void Draw(Draw2D d)
    {
        // A render texture, a layer that draws into it, and two chains are made once and kept. Layers draw in the order they were made, so the
        // layer that fills the texture comes before the one that shows it, and the HUD layer comes last.
        _map ??= d.RenderTexture("map", 96, 96);
        var mapLayer = d.RenderLayer("map", _map, new Color(14, 30, 24));
        var world = d.GetLayer("world");
        var hud = d.ScreenLayer;
        _bloom ??= d.CreatePostChain().AddBloom(threshold: 0.55f, intensity: 1.4f, radius: 1f, blurPasses: 4);
        _scan ??= MakeScan(d);

        d.Post = Mode switch
        {
            1 => _bloom,
            2 => _scan,
            _ => null,
        };
        world.Post = Mode == 3 ? _bloom : null;

        // Everything the map texture shows: pixel coordinates of the texture, drawn only while it is on screen.
        if (Mode == 0)
        {
            d.Layer = mapLayer;
            d.StrokeRect(new Rect(1, 1, 94, 94), Color.Rgb(0x58c88c), 2);
            d.FillCircle(new Vector2(30, 36), 9, Color.Rgb(0xf2c14e));
            d.FillCircle(new Vector2(66, 58), 6, Color.Rgb(0xe8648c));
            d.Line(new Vector2(30, 36), new Vector2(66, 58), Color.Rgb(0xdddddd), 2);
            d.Text("MAP", new Vector2(48, 80), 14, Color.White, new Vector2(0.5f, 0.5f));
        }

        d.Layer = world;
        d.FillRect(new Rect(-300, -200, 600, 400), Color.Rgb(0x0b1020), z: -10);
        if (Mode == 0)
        {
            // The same texture three times: as it is, twice as large and turned, and small and tinted.
            d.Sprite(_map.Texture, new Vector2(-150, 0));
            d.Sprite(_map.Texture, new Vector2(0, 0), rotation: 0.4f, scale: 1.8f);
            d.Sprite(_map.Texture, new Vector2(150, 0), scale: 0.6f, tint: Color.Rgb(0x9ad0ff));
        }
        else
        {
            Neon(d);
        }

        d.Layer = hud;
        d.ScreenText(Mode switch { 0 => "render texture", 1 => "bloom on the frame", 2 => "a pass of our own", _ => "bloom on one layer" }, new Vector2(8, 8), 14, Color.White);
        d.StrokeRect(new Rect(6, 28, 190, 2), Color.White, 2);
    }

    // Bright shapes on a dark ground, and one dim shape that stays below the bloom threshold.
    [View]
    private static void Neon(Draw2D d)
    {
        d.StrokeCircle(new Vector2(-110, -10), 46, Color.Rgb(0x30e0ff), 5);
        d.FillCircle(new Vector2(40, -10), 28, Color.Rgb(0xff4fd8));
        d.Line(new Vector2(-190, 70), new Vector2(190, 70), Color.Rgb(0xfff060), 4);
        d.FillRect(new Rect(110, -40, 60, 60), Color.Rgb(0x2a3552));
        d.Text("GLOW", new Vector2(0, -80), 40, Color.White, new Vector2(0.5f, 0.5f));
    }

    [View]
    private PostChain MakeScan(Draw2D d)
    {
        _scanMaterial = d.Renderer!.CreateMaterial(new ShaderCode(RecursiveAI.OneGpu.ShaderStage.Pixel, Scan.PSMain.EntryPoint, Scan.PSMain.SpirV.ToArray(), Scan.PSMain.Msl.ToArray(), Scan.PSMain.MslFunctionName, Scan.PSMain.Layout, Scan.PSMain.Wgsl.ToArray(), Scan.PSMain.WgslEntryPoint), "scan");
        _scanMaterial.SetParameter(0, new Vector4(3, 0.35f, 0, 0));
        return d.CreatePostChain().AddMaterial(_scanMaterial);
    }
}

public sealed class GlowGame : GameDefinition
{
    public override string Name => "Glow";

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.GlowExample.EntityTypes.Register(registry);

    public override void Setup(World world) => world.Add(new Gallery());

    public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => Ui.Column());
}
