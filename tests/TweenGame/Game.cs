using System;
using System.IO;
using OneGame.Core;
using OneGame.Host;
using OneGame.Tweens;
using OneGame.UI;

namespace TweenGame;

/// <summary>A panel that changes look when toggled (a style transition) and a score label that pops when the score changes (a tween started from view code).</summary>
public sealed class TweenGameDefinition : GameDefinition
{
    public override string Name => "Tweens";

    /// <summary>False turns every animation off (no style transitions, no pulse), for checking that animation never changes the simulation.</summary>
    public bool Animate { get; init; } = true;

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.TweenGame.EntityTypes.Register(registry);

    public override void Setup(World world) => world.Add(new Meter());

    public override void Style(UiTree ui)
    {
        ArgumentNullException.ThrowIfNull(ui);
        ui.AddStyleSheet(new StyleSheet()
            .Rule("Root", s => s.Background(Color.Rgb(0x14181f)).TextColor(Color.White))
            .Rule(".panel", s => s.Size(200, 80).Background(Color.Rgb(0x3060a0)).Radius(8)
                .Transition(Animate ? "background-color 200ms linear, opacity 200ms linear, width 200ms ease-out" : "none"))
            .Rule(".panel.on", s => s.Background(Color.Rgb(0xe0a030)).Opacity(0.5f).Width(320)));
    }

    public override void DefineScreens(ScreenSet screens) => screens.Add("main", game =>
    {
        var panel = new Element("Panel").Named("panel").Class("panel");
        var shown = 0;
        var score = new Element("Label").Named("score").Padding(8)
            .BindText(() => "Score " + Meter.Of(game.World).Score)
            .OnUpdate(el =>
            {
                var now = Meter.Of(game.World).Score;
                if (now != shown)
                {
                    shown = now;
                    if (Animate)
                    {
                        game.Tweens.Number(1.4f, 1f, 0.3f, v => el.Style(st => st.Scale(v))).Ease(Easing.OutBack).Named("pulse");
                    }
                }
            });
        return Ui.Column(panel, Ui.Button("Toggle", () => panel.SetClass("on", !panel.HasClass("on"))).Named("toggle"), score).Gap(8).Padding(16);
    });

    public override void DefineFonts(OneGame.Text.FontLibrary fonts)
    {
        ArgumentNullException.ThrowIfNull(fonts);
        using var stream = typeof(TweenGameDefinition).Assembly.GetManifestResourceStream("fonts/NotoSans-Regular.ttf") ?? throw new InvalidOperationException("The program has no embedded font.");
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        fonts.Add(copy.ToArray());
    }
}
