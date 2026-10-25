using OneGame.Platform;
using OneGame.Testing;
using OneGame.UI.Kit;
using Bricks.Views;

namespace Bricks.Tests;

/// <summary>The game as a player meets it: input, the harness's determinism checks, and what survives a restart.</summary>
public class PlayTests
{
    private static BricksGame Make() => new(new GameServices { CreateStore = _ => new MemoryStore() });

    private static TestOptions Options => new() { Width = 540, Height = 960 };

    // A scripted game: start, launch, then swing the paddle about with pointer-style steering and a few key pushes.
    private static void Script(TestClient client, int frame)
    {
        switch (frame)
        {
            case 0:
                client.Send("Court", "NewGame", "1");
                break;
            case 3:
                client.Send("Court", "Launch");
                break;
            case 200:
                client.Send("Court", "Push", "-100");
                break;
            case 230:
                client.Send("Court", "Push", "0");
                break;
        }

        if (frame is > 3 and < 600 && frame % 25 == 0)
        {
            client.Send("Court", "Steer", ((frame * 7 % 400) + 40).ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    [Fact]
    public void TheSameScriptTwiceGivesTheSameHashEveryFrame() =>
        Determinism.RunTwice(Make, Script, 700, Options).ThrowIfDifferent();

    [Fact]
    public void ASnapshotRestoredHalfWayFinishesOnTheSameHashes() =>
        Determinism.SnapshotRestore(Make, Script, 700, 350, Options).ThrowIfDifferent();

    [Fact]
    public void ARecordedGameReplaysToTheSameHashes() =>
        Determinism.Replay(Make, Script, 700, Options).ThrowIfDifferent();

    [Fact]
    public void AHeldKeyDrivesThePaddleAndReleasingItStopsIt()
    {
        using var cluster = TestCluster.Start(Make(), Options);
        var client = cluster.Client;
        client.Click("play").Step(2);
        var injector = client.Game.Platform.Injector;
        var start = Paddle(client);
        injector.Key(1, Key.ArrowRight, true);
        client.Step(10);
        var moved = Paddle(client);
        Assert.True(moved > start + 50, $"{start} -> {moved}");
        injector.Key(1, Key.ArrowRight, false);
        client.Step(10);
        var stopped = Paddle(client);
        Assert.True(stopped - moved < 10, "the paddle keeps going after the key is released");
        client.Step(10);
        Assert.Equal(stopped, Paddle(client));
    }

    [Fact]
    public void AControllerStickAndDpadDriveThePaddleToo()
    {
        using var cluster = TestCluster.Start(Make(), Options);
        var client = cluster.Client;
        client.Click("play").Step(2);
        var injector = client.Game.Platform.Injector;
        var pad = injector.GamepadConnect();
        client.Step(1);
        var start = Paddle(client);
        injector.GamepadAxis(pad, GamepadAxis.RightX, -1f);
        client.Step(10);
        var left = Paddle(client);
        Assert.True(left < start - 50, $"{start} -> {left}");
        injector.GamepadAxis(pad, GamepadAxis.RightX, 0f);
        injector.GamepadButton(pad, GamepadButton.DPadRight, true);
        client.Step(10);
        Assert.True(Paddle(client) > left + 50);
    }

    [Fact]
    public void TheMouseSteersWithoutAClickAndAClickLaunches()
    {
        using var cluster = TestCluster.Start(Make(), Options);
        var client = cluster.Client;
        client.Click("play").Step(2);
        var injector = client.Game.Platform.Injector;
        injector.PointerMove(1, 100, 600);
        client.Step(30);
        Assert.True(Paddle(client) < 120);
        Assert.Equal("1", client.Get("Court", "Phase"));
        client.ClickAt(300, 400);
        client.Step(2);
        Assert.Equal("2", client.Get("Court", "Phase"));
    }

    [Fact]
    public void LeavingTheForegroundPausesAGameInProgress()
    {
        using var cluster = TestCluster.Start(Make(), Options);
        var client = cluster.Client;
        client.Click("play").Step(2);
        client.Press("Space").Step(5);
        client.Game.Platform.Injector.Lifecycle(AppLifecycleState.Paused);
        client.Step(2);
        Assert.Equal("true", client.Get("Court", "Paused"));
    }

    [Fact]
    public void ATypedNameIsKeptOnceAndTheTableSurvivesStartingTheProgramAgain()
    {
        var dir = Path.Combine(Path.GetTempPath(), "bricks-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            BricksGame OnDisk() => new(new GameServices { CreateStore = _ => new LocalFileStore(dir) });
            using (var first = TestCluster.Start(OnDisk(), Options))
            {
                var client = first.Client;
                client.Game.LoadFixture("last-life");
                client.Game.OpenScreen("play");
                client.Step(80);
                Assert.Equal("4", client.Get("Court", "Phase"));
                client.Click("name").Type("Ada").Step(5);
                Assert.Equal("Ada", ((TextInputView)client.Find("name")!).FieldText);
                client.Click("save-score").Step(2);
                Assert.Equal(["Ada"], Session.For(client.Game).Profile.Scores.Select(s => s.Name));
            }

            Assert.True(File.Exists(Path.Combine(dir, Profile.FileName)));
            using var second = TestCluster.Start(OnDisk(), Options);
            var scores = Session.For(second.Client.Game).Profile.Scores;
            Assert.Single(scores);
            Assert.Equal(new ScoreEntry("Ada", 1230, 3, false), scores[0]);
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }

    [Fact]
    public void AScoreThatDoesNotMakeTheTableOffersNoNameField()
    {
        using var cluster = TestCluster.Start(Make(), Options);
        var client = cluster.Client;
        client.Game.LoadFixture("last-life");
        client.Game.OpenScreen("play");
        var profile = Session.For(client.Game).Profile;
        for (var i = 0; i < Profile.TableSize; i++)
        {
            profile.Add("P" + i, 5000 - i, 5, false);
        }

        client.Step(80);
        Assert.Equal("4", client.Get("Court", "Phase"));
        Assert.False(Session.For(client.Game).CanEnterScore);
        Assert.False(client.Find("save-score")!.IsVisible);
    }

    private static double Paddle(TestClient client) => double.Parse(client.Get("Court", "PaddleX"), System.Globalization.CultureInfo.InvariantCulture);
}
