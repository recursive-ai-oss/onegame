using OneGame.Core;
using OneGame.Host;
using OneGame.Tweens;
using OneGame.UI;

namespace TweenExample;

/// <summary>Two ways to animate the UI: a tween you start from code, and a CSS transition you only declare. Both run on view
/// time, never on the simulation, so a replay or a state hash is the same with or without them.</summary>
public sealed class TweenGame : GameDefinition
{
    public override string Name => "Tween";

    // The game has no synced state, so one empty entity is all the world holds.
    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.TweenExample.EntityTypes.Register(registry);

    public override void Setup(World world) => world.Add(new Empty());

    // A transition: when `lit` is added or removed, the background blends over 300 ms instead of jumping.
    public override void Style(UiTree ui) => ui.AddStyleSheet(new StyleSheet()
        .Rule(".lamp", s => s.Size(120, 40).Background(Color.Rgb(0x203040)).Transition("background-color 300ms ease-out"))
        .Rule(".lamp.lit", s => s.Background(Color.Rgb(0xf2c14e))));

    public override void DefineScreens(ScreenSet screens) => screens.Add("main", game =>
    {
        var card = Ui.Label("Hello").Named("card").Padding(Length.Px(12)).Background(Color.Rgb(0x35506b));
        var lamp = Ui.Label("").Named("lamp").Class("lamp");
        return Ui.Column(
            Ui.Button("Slide in", () => game.Tweens
                .Number(-300, 0, 0.4f, x => card.Style(s => s.Translate(x, 0)))   // from, to, seconds, what to do with each value
                .Ease(Easing.OutCubic)
                .OnComplete(() => card.Class("landed"))).Named("slide"),
            Ui.Button("Toggle lamp", () => lamp.SetClass("lit", !lamp.HasClass("lit"))).Named("toggle"),
            card, lamp)
            .Padding(Length.Px(16)).Gap(Length.Px(12)).TextColor(Color.White);
    });
}

/// <summary>An entity with no state.</summary>
[Entity]
public partial class Empty : Entity
{
}
