using System.Runtime.CompilerServices;
using System.Text.Json;
using OneGame.Audio;
using OneGame.Core;
using OneGame.Host;
using OneGame.Platform;
using OneGame.UI;

namespace OneGame.Testing.Tests;

public class SoundScenarioTests
{
    private static string Clip([CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "testdata", "audio", "burst-silence.ogg"));

    // Plays the clip when the space bar reaches the game, as a game plays a click for an action of its own.
    private sealed class ClickGame(AudioService audio) : GameDefinition
    {
        private readonly Sound _click = audio.LoadSound(File.OpenRead(Clip()), "click.ogg");

        public override string Name => "ClickGame";

        public override void RegisterEntities(EntityRegistry registry)
        {
        }

        public override void Setup(World world)
        {
        }

        public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => Ui.Column(Ui.Label("x")));

        public override void OnInput(GameInstance game, in PlatformEvent input)
        {
            if (input.Kind == PlatformEventKind.KeyDown && input.Key == Key.Space)
            {
                audio.Play(_click);
            }
        }
    }

    private static AudioService Audio() => AudioService.Create(new AudioServiceOptions { Capture = true, Device = new AudioDeviceOptions { SampleRate = 48_000, Channels = 2 } });

    [Fact]
    public void AScenarioAssertsWhichSoundsPlayedAndHowOften()
    {
        const string yaml = """
            name: clicks
            steps:
              - press: Space
              - step: 5
              - press: Space
              - step: 1
            expect:
              - sound: { name: click }                       # at least once
              - sound: { name: click, count: 2 }
              - sound: { name: click.ogg, min: 1, max: 2 }   # the file name works too
              - sound: { name: click, count: 1, frames: 3 }  # only the second press is within the last 3 frames
              - sound: { name: coin, count: 0 }
            """;
        using var audio = Audio();
        using var cluster = TestCluster.Start(new ClickGame(audio), new TestOptions { Width = 320, Height = 200, Gpu = GpuMode.Off });
        var result = cluster.Run(ScenarioParser.Parse(yaml));
        var json = JsonSerializer.Serialize(result, ScenarioJson.Default.ScenarioResult);
        Assert.True(result.Passed, json);

        // The audio event log is part of the result.
        Assert.NotNull(result.Sounds);
        Assert.Equal(2, result.Sounds!.Length);
        Assert.All(result.Sounds, s => Assert.Equal("click.ogg", s.Sound));
        Assert.True(result.Sounds[1].Frame > result.Sounds[0].Frame);
        Assert.Contains("\"sounds\":[{\"sequence\":", json);
    }

    [Fact]
    public void AMissingSoundFailsWithTheCountItSaw()
    {
        const string yaml = """
            name: no-clicks
            steps:
              - step: 2
            expect:
              - sound: { name: click }
              - sound: { name: click, count: 3 }
            """;
        using var audio = Audio();
        using var cluster = TestCluster.Start(new ClickGame(audio), new TestOptions { Width = 320, Height = 200, Gpu = GpuMode.Off });
        var result = cluster.Run(ScenarioParser.Parse(yaml));
        Assert.False(result.Passed);
        var json = JsonSerializer.Serialize(result, ScenarioJson.Default.ScenarioResult);
        Assert.Contains("sound click was played 0 times, expected at least 1", json);
        Assert.Contains("sound click was played 0 times, expected exactly 3", json);
    }

    [Fact]
    public void SoundsBeforeTheScenarioDoNotCount()
    {
        using var audio = Audio();
        using var cluster = TestCluster.Start(new ClickGame(audio), new TestOptions { Width = 320, Height = 200, Gpu = GpuMode.Off });
        cluster.Client.Call("input_key", """{"key":"Space"}""");
        cluster.Client.Step(1);
        var result = cluster.Run(ScenarioParser.Parse("name: later\nsteps:\n  - step: 1\nexpect:\n  - sound: { name: click, count: 0 }\n"));
        Assert.True(result.Passed, JsonSerializer.Serialize(result, ScenarioJson.Default.ScenarioResult));
    }
}
