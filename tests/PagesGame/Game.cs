using System;
using System.IO;
using OneGame.Core;
using OneGame.Host;
using OneGame.Pages;
using OneGame.UI;
using OneRender.Interaction;

namespace PagesGame;

/// <summary>A game whose menu is a OneRender HTML page, drawn over and under a small OneGame.UI panel.</summary>
public sealed class PagesGameDefinition : GameDefinition
{
    private const string Html = """
        <body style="margin:0;font:16px Body;color:#e8ecf4">
        <div id="under" style="--host-scope:2;position:absolute;left:0px;top:20px;width:110px;height:70px;background:#c03030"></div>
        <div id="glow" style="--host-scope:1;position:absolute;left:10px;top:150px;width:90px;height:40px;background:#503010;border-radius:10px"></div>
        <h1 style="margin:10px;font-size:22px">Pages menu</h1>
        <button id="start" style="position:absolute;left:10px;top:105px;width:120px;height:36px;border:2px solid #6080c0;border-radius:6px;background:#203050;color:#fff;font:16px Body;transition:background-color 200ms linear">Start</button>
        <style>#start:hover{background:#4070c0 !important} #start:focus{outline:3px solid #ffd040}</style>
        <input id="name" type="text" value="" style="position:absolute;left:10px;top:200px;width:160px;height:28px;font:16px Body">
        <div id="over" style="position:absolute;left:70px;top:30px;width:80px;height:50px;background:rgba(40,160,80,.7)"></div>
        <p id="status" style="position:absolute;left:10px;top:235px;margin:0">idle</p>
        </body>
        """;

    public override string Name => "Pages";

    /// <summary>False leaves the page out, for checking that showing one changes no state.</summary>
    public bool ShowPages { get; init; } = true;

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.PagesGame.EntityTypes.Register(registry);

    public override void Setup(World world) => world.Add(new Meter());

    public override void Style(UiTree ui)
    {
        ArgumentNullException.ThrowIfNull(ui);
        ui.AddStyleSheet(new StyleSheet().Rule("Root", s => s.TextColor(Color.White)));
    }

    public override void DefineScreens(ScreenSet screens) => screens.Add("main", game =>
    {
        // The page lives as long as the game; screens may be rebuilt, so showing replaces by name.
        game.Pages.Scopes.Additive(1);
        game.Pages.Scopes.Layer(2, PageScopes.Under);
        var fonts = new PageFonts(game.Fonts);
        fonts.Add(FontBytes(), "Body");
        if (ShowPages)
        {
            game.Pages.Show("menu", PageScreen.FromHtml(Html, new PageOptions(fonts)
        {
            Setup = s =>
            {
                var page = s.Interactive!;
                page.Events.AddEventListener(EventTarget.Of(page.FindElement("start")), "click", _ => Meter.Of(game.World).Send.Add(1));
            },
        }));
        }

        return Ui.Column(new Element("Panel").Named("panel").Background(Color.Rgb(0x3060a0)).Size(120, 60)).Padding(0);
    });

    private static byte[] FontBytes()
    {
        using var stream = typeof(PagesGameDefinition).Assembly.GetManifestResourceStream("fonts/NotoSans-Regular.ttf") ?? throw new InvalidOperationException("The program has no embedded font.");
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }

    public override void DefineFonts(OneGame.Text.FontLibrary fonts)
    {
        ArgumentNullException.ThrowIfNull(fonts);
        fonts.Add(FontBytes());
    }
}
