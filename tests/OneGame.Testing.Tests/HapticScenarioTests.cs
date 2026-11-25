using System.Text.Json;
using OneGame.Core;
using OneGame.Host;
using OneGame.Platform;
using OneGame.UI;

namespace OneGame.Testing.Tests;

public class HapticScenarioTests
{
    // Space ticks, Enter buzzes: a game giving feedback from its input handler.
    private sealed class FeelGame : GameDefinition
    {
        public override string Name => "FeelGame";

        public override void RegisterEntities(EntityRegistry registry)
        {
        }

        public override void Setup(World world)
        {
        }

        public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => Ui.Column(Ui.Label("x")));

        public override void OnInput(GameInstance game, in PlatformEvent input)
        {
            if (input.Kind != PlatformEventKind.KeyDown)
            {
                return;
            }

            if (input.Key == Key.Space)
            {
                Haptics.Tick();
            }
            else if (input.Key == Key.Enter)
            {
                Haptics.Buzz();
            }
        }
    }

    private static string Run(string yaml, out bool passed)
    {
        using var cluster = TestCluster.Start(new FeelGame(), new TestOptions { Width = 320, Height = 200, Gpu = GpuMode.Off });
        var result = cluster.Run(ScenarioParser.Parse(yaml));
        passed = result.Passed;
        return JsonSerializer.Serialize(result, ScenarioJson.Default.ScenarioResult);
    }

    [Fact]
    public void AScenarioAssertsWhichHapticsWereRequestedAndHowOften()
    {
        Run("""
            name: feel
            steps:
              - press: Space
              - step: 5
              - press: Space
              - press: Enter
              - step: 1
            expect:
              - haptic: { kind: tick, count: 2 }
              - haptic: { kind: buzz }                    # at least once
              - haptic: { count: 3 }                      # any kind
              - haptic: { kind: tick, count: 1, frames: 3 }
              - haptic: { kind: error, count: 0 }
            """, out var passed);
        Assert.True(passed);
    }

    [Fact]
    public void AMissingHapticFailsWithTheCountItSaw()
    {
        var json = Run("""
            name: nofeel
            steps:
              - press: Space
              - step: 1
            expect:
              - haptic: { kind: buzz }
            """, out var passed);
        Assert.False(passed);
        Assert.Contains("haptic buzz was requested 0 times", json);
    }

    [Fact]
    public void DesktopRecordsTheRequestAsNotDelivered()
    {
        Haptics.ClearLog();
        Haptics.Play(HapticKind.Success);
        var entry = Assert.Single(Haptics.GetLog());
        Assert.Equal(HapticKind.Success, entry.Kind);
        Assert.False(entry.Delivered);
        Assert.Throws<ArgumentOutOfRangeException>(() => Haptics.Play((HapticKind)99));
    }
}
