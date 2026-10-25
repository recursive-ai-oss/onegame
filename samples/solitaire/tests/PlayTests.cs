using System.Globalization;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using OneGame.Audio;
using OneGame.Live;
using OneGame.Stores;
using OneGame.Testing;

namespace Solitaire.Tests;

/// <summary>A cloud store kept in memory, so the cloud-backed file store can be tested without a store client.</summary>
internal sealed class FakeCloud : ICloudSaves
{
    public readonly Dictionary<string, byte[]> Files = [];

    public bool Enabled { get; set; } = true;

    public bool FailWrites { get; set; }

    public bool IsEnabled => Enabled;

    public StoreResult Write(string name, ReadOnlySpan<byte> data)
    {
        if (FailWrites)
        {
            return StoreResult.Failed;
        }

        Files[name] = data.ToArray();
        return StoreResult.Ok;
    }

    public StoreResult Read(string name, out byte[] data, int maxBytes = ICloudSaves.MaxFileBytes)
    {
        data = [];
        if (!Files.TryGetValue(name, out var bytes))
        {
            return StoreResult.NotFound;
        }

        if (bytes.Length > maxBytes)
        {
            return StoreResult.TooLarge;
        }

        data = bytes;
        return StoreResult.Ok;
    }

    public bool Exists(string name) => Files.ContainsKey(name);

    public StoreResult Delete(string name) => Files.Remove(name) ? StoreResult.Ok : StoreResult.NotFound;

    public IReadOnlyList<CloudFile> List() => [.. Files.Select(f => new CloudFile(f.Key, f.Value.Length))];

    public StoreResult TryGetQuota(out CloudQuota quota)
    {
        quota = default;
        return StoreResult.Unavailable;
    }
}

/// <summary>A store with working cloud saves and nothing else. The other services are the no-op ones of the null store.</summary>
internal sealed class FakeStore(FakeCloud cloud, bool available) : IStorePlatform
{
    private readonly NullStorePlatform _none = new("test");

    public string Name => "fake";

    public bool IsAvailable => available;

    public string? UnavailableReason => available ? null : "off";

    public bool RelaunchRequested => false;

    public void Pump()
    {
    }

    public IStoreIdentity Identity => _none.Identity;

    public IAchievements Achievements => _none.Achievements;

    public IStats Stats => _none.Stats;

    public ICloudSaves Cloud => cloud;

    public IRichPresence Presence => _none.Presence;

    public IOverlay Overlay => _none.Overlay;

    public IOwnership Ownership => _none.Ownership;

    public IStoreEnvironment Environment => _none.Environment;

    public void Dispose()
    {
    }
}

public class ReplayTests
{
    private static TestOptions Options => new() { Width = 1280, Height = 720 };

    // One command of the winning line per frame, then idle frames.
    private static void Drive(TestClient client, int frame)
    {
        var line = Fixtures.Winnable().Line;
        if (frame >= line.Count)
        {
            return;
        }

        var m = line[frame];
        if (m.IsDraw)
        {
            client.Send("Table", "Draw");
        }
        else
        {
            client.Send("Table", "Move", m.From.ToString(CultureInfo.InvariantCulture), m.To.ToString(CultureInfo.InvariantCulture), m.Count.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static int Frames => Fixtures.Winnable().Line.Count + 10;

    [Fact]
    public void FullGameRunsTwiceWithTheSameHashesEveryTick() =>
        Determinism.RunTwice(() => new SolitaireGame(), Drive, Frames, Options).ThrowIfDifferent();

    [Fact]
    public void SnapshotHalfwayAndRestoreGivesTheSameHashesAfterwards() =>
        Determinism.SnapshotRestore(() => new SolitaireGame(), Drive, Frames, Frames / 2, Options).ThrowIfDifferent();

    [Fact]
    public void RecordedFullGameReplaysWithIdenticalHashes() =>
        Determinism.Replay(() => new SolitaireGame(), Drive, Frames, Options).ThrowIfDifferent();

    [Fact]
    public void ReplicaNeverDriftsFromTheAuthorityAcrossAFullGame()
    {
        using var cluster = TestCluster.Start(new SolitaireGame(), Options);
        for (var f = 0; f < Frames; f++)
        {
            Drive(cluster.Client, f);
            cluster.Client.Step(1);
        }

        Assert.Equal("1", cluster.Client.Get("Table", "Phase"));
        Assert.Empty(cluster.Client.Game.DriftReports);
    }
}

public class InputTests
{
    private static string Num(int v) => v.ToString(CultureInfo.InvariantCulture);

    private static (float X, float Y) Center(TestClient client, string id)
    {
        var b = client.Find(id)!.AbsoluteBounds;
        return (b.X + (b.Width / 2), b.Y + (b.Height / 2));
    }

    // The hash includes the tick, so "nothing changed" compares the cards and the counters.
    private static string Layout(Table t) => string.Join('|', Enumerable.Range(0, Piles.Count).Select(p => string.Join(',', t.Pile(p)))) + $";{t.Score};{t.Moves};{t.Phase}";

    private static void Pointer(TestClient client, string action, float x, float y) =>
        client.Call("input_pointer", $"{{\"action\":\"{action}\",\"x\":{x.ToString(CultureInfo.InvariantCulture)},\"y\":{y.ToString(CultureInfo.InvariantCulture)}}}");

    [Fact]
    public void DraggingACardToAPileMovesIt()
    {
        using var cluster = TestCluster.Start(new SolitaireGame(), new TestOptions { Seed = 1 });
        var client = cluster.Client;
        var table = (Table)client.Game.World.Entities[0];
        client.Call("screen_open", "{\"name\":\"table\"}");
        client.Step(5);

        // Find the first single-card move in the solver's line, playing draws before it through commands.
        var line = Solver.Solve(Rig.TableOf(Rig.NewWorld(1)), 2_000_000)!;
        var index = 0;
        while (line[index].IsDraw || line[index].Count != 1)
        {
            client.Send("Table", "Draw");
            client.Step(1);
            index++;
        }

        // Drawing is the only thing played above, so the solver line's draws and these agree only while they are the same draws.
        var move = line[index];
        var cardId = table.Pile(move.From)[^1];
        client.Step(40); // let the cards finish moving
        var from = Center(client, "card-" + Cards.Name(cardId));
        var to = Center(client, move.To switch { >= Piles.Tableau => "col" + Num(move.To - Piles.Tableau + 1), _ => "found" + Num(move.To - Piles.Foundation + 1) });
        var before = table.Moves;

        Pointer(client, "down", from.X, from.Y);
        client.Step(1);
        Pointer(client, "move", (from.X + to.X) / 2, (from.Y + to.Y) / 2);
        client.Step(1);
        Pointer(client, "move", to.X, to.Y);
        client.Step(1);
        Pointer(client, "up", to.X, to.Y);
        client.Step(3);

        Assert.Equal(before + 1, table.Moves);
        Assert.Contains(cardId, table.Pile(move.To));
    }

    [Fact]
    public void ADragDroppedOnNothingChangesNothing()
    {
        using var cluster = TestCluster.Start(new SolitaireGame(), new TestOptions { Seed = 1 });
        var client = cluster.Client;
        var table = (Table)client.Game.World.Entities[0];
        client.Call("screen_open", "{\"name\":\"table\"}");
        client.Step(40);
        var before = Layout(table);
        var from = Center(client, "card-" + Cards.Name(table.Pile(Piles.Tableau + 3)[^1]));
        Pointer(client, "down", from.X, from.Y);
        client.Step(1);
        Pointer(client, "move", from.X + 3, from.Y + 200);
        client.Step(1);
        Pointer(client, "up", from.X + 3, from.Y + 200);
        client.Step(3);
        Assert.Equal(before, Layout(table));
    }

    [Fact]
    public void KeyboardDrawsUndoesAndPauses()
    {
        using var cluster = TestCluster.Start(new SolitaireGame(), new TestOptions { Seed = 1 });
        var client = cluster.Client;
        client.Call("screen_open", "{\"name\":\"table\"}");
        client.Step(2);
        var table = (Table)client.Game.World.Entities[0];
        var start = Layout(table);
        client.Press("d").Step(2);
        Assert.Equal("1", client.Get("Table", "Moves"));
        Assert.NotEqual(start, Layout(table));
        client.Press("u").Step(2);
        Assert.Equal("0", client.Get("Table", "Moves"));
        Assert.Equal(start, Layout(table));

        client.Press("p").Step(2);
        Assert.Equal("true", client.Get("Table", "Paused"), ignoreCase: true);
        Assert.True(client.Find("pause")?.IsVisible ?? false);
    }

    [Fact]
    public void KeyboardNavigationPicksUpAndPlacesACard()
    {
        // Menu to a new game with Enter, then play by keyboard only: the cursor starts on a pile; moves are made with Space presses.
        using var cluster = TestCluster.Start(new SolitaireGame(), new TestOptions { Seed = 1 });
        var client = cluster.Client;
        client.Step(2);
        client.Click("new-draw1").Step(3);
        Assert.Equal("table", client.Game.CurrentScreen);
        client.Press("d").Step(1);
        Assert.Equal("1", client.Get("Table", "Moves"));
        client.Press("Escape").Step(2);
        Assert.Equal("true", client.Get("Table", "Paused"), ignoreCase: true);
    }
}

public class ProfileTests
{
    [Fact]
    public void RoundTripKeepsSettingsAndStatistics()
    {
        var p = new Profile { DrawMode = 3, AutoFinish = false, MusicVolume = 2, EffectsVolume = 9, Glyphs = GlyphStyle.Gamepad };
        p.Add(new GameRecord(1, 3, true, 1200, 90, 4000));
        p.Add(new GameRecord(2, 1, false, 10, 4, 100));
        Assert.True(Profile.TryRead(p.Write(), out var read));
        Assert.Equal(3, read.DrawMode);
        Assert.False(read.AutoFinish);
        Assert.Equal(2, read.MusicVolume);
        Assert.Equal(9, read.EffectsVolume);
        Assert.Equal(GlyphStyle.Gamepad, read.Glyphs);
        Assert.Equal(p.Wins, read.Wins);
        Assert.Equal(p.BestScore, read.BestScore);
        Assert.Equal(p.History, read.History);
        Assert.True(read.Games >= read.Wins);
    }

    [Fact]
    public void GarbageAndDamagedFilesAreRefusedWithoutThrowing()
    {
        var good = new Profile();
        good.Add(new GameRecord(1, 1, true, 500, 80, 3000));
        var bytes = good.Write();
        var rng = new Random(12345);
        for (var i = 0; i < 20_000; i++)
        {
            var copy = (byte[])bytes.Clone();
            switch (i % 4)
            {
                case 0:
                    copy[rng.Next(copy.Length)] ^= (byte)(1 << rng.Next(8));
                    break;
                case 1:
                    copy = copy[..rng.Next(copy.Length)];
                    break;
                case 2:
                    copy = [.. copy, .. new byte[rng.Next(1, 40)]];
                    break;
                default:
                    copy = new byte[rng.Next(0, 300)];
                    rng.NextBytes(copy);
                    break;
            }

            _ = Profile.TryRead(copy, out var p);
            Assert.InRange(p.History.Count, 0, Profile.HistoryLimit);
        }

        Assert.False(Profile.TryRead([], out _));
        Assert.False(Profile.TryRead(new byte[20_000], out _));
    }

    [Fact]
    public void RecordsPastTheLimitDropTheOldest()
    {
        var p = new Profile();
        for (var i = 0; i < Profile.HistoryLimit + 5; i++)
        {
            p.Add(new GameRecord(i, 1, false, 0, 1, 1));
        }

        Assert.Equal(Profile.HistoryLimit, p.History.Count);
        Assert.Equal(5, p.History[0].Deal);
    }
}

public class StoreTests
{
    [Fact]
    public void ChoosesTheCloudOnlyWhenTheStoreIsRunningAndCloudIsOn()
    {
        var dir = Path.Combine(Path.GetTempPath(), "solitaire-test-" + Guid.NewGuid().ToString("N"));
        var cloud = new FakeCloud();
        Assert.IsType<CloudStore>(DataStores.Choose(new FakeStore(cloud, true), dir));
        Assert.IsType<LocalFileStore>(DataStores.Choose(new FakeStore(cloud, false), dir));
        cloud.Enabled = false;
        Assert.IsType<LocalFileStore>(DataStores.Choose(new FakeStore(cloud, true), dir));
        Assert.IsType<LocalFileStore>(DataStores.Choose(null, dir));
    }

    [Fact]
    public void CloudStoreReadsWritesAndDeletes()
    {
        var cloud = new FakeCloud();
        var store = new CloudStore(new FakeStore(cloud, true));
        Assert.False(store.TryRead("a", 100, out _));
        Assert.True(store.Write("a", [1, 2, 3]));
        Assert.True(store.TryRead("a", 100, out var data));
        Assert.Equal(new byte[] { 1, 2, 3 }, data);
        Assert.False(store.TryRead("a", 2, out _));
        store.Delete("a");
        Assert.False(store.TryRead("a", 100, out _));
        cloud.FailWrites = true;
        Assert.False(store.Write("a", [9]));
    }

    [Fact]
    public void LocalFileStoreFallbackKeepsTheProfileBetweenRuns()
    {
        var dir = Path.Combine(Path.GetTempPath(), "solitaire-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = DataStores.Choose(new FakeStore(new FakeCloud { Enabled = false }, true), dir);
            var p = new Profile { DrawMode = 3 };
            Assert.True(store.Write(Profile.FileName, p.Write()));
            var again = new LocalFileStore(dir);
            Assert.True(again.TryRead(Profile.FileName, 16 * 1024, out var bytes));
            Assert.True(Profile.TryRead(bytes, out var read));
            Assert.Equal(3, read.DrawMode);
            Assert.False(again.TryRead("missing", 100, out _));
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }

    [Fact]
    public void AGameWithACloudStoreKeepsItsProfileThere()
    {
        var cloud = new FakeCloud();
        var services = new GameServices { CreateStore = _ => new CloudStore(new FakeStore(cloud, true)) };
        using var cluster = TestCluster.Start(new SolitaireGame(services), new TestOptions { Seed = 1 });
        var client = cluster.Client;
        client.Call("screen_open", "{\"name\":\"table\"}");
        client.Send("Table", "Draw").Step(3);
        client.Call("screen_open", "{\"name\":\"main\"}");
        client.Step(3);
        Assert.True(cloud.Files.Count > 0, "the game wrote nothing to the cloud store");
    }
}

public class SoundTests
{
    [Fact]
    public void MovesAndRefusalsMakeTheirSounds()
    {
        var sounds = new RecordingSounds();
        using var cluster = TestCluster.Start(new SolitaireGame(new GameServices { Sounds = sounds }), new TestOptions { Seed = 1 });
        var client = cluster.Client;
        client.Call("screen_open", "{\"name\":\"table\"}");
        client.Press("d").Step(3);
        Assert.Contains(sounds.Played, p => p.Sound == SoundEvent.Draw);
        client.Send("Table", "Move", "6", "7", "1").Step(3);
        Assert.Contains(sounds.Played, p => p.Sound == SoundEvent.Refuse);
        Assert.All(sounds.Played, p => Assert.InRange(p.Volume, 0f, 1f));
    }

    [Fact]
    public void TheAudioServiceTakesEverySoundOnACaptureSink()
    {
        using var sounds = AudioSounds.TryCreate(new AudioServiceOptions { Capture = true });
        Assert.NotNull(sounds);
        foreach (var e in Enum.GetValues<SoundEvent>())
        {
            sounds.Play(e, 0.5f);
        }

        sounds.SetVolumes(0.7f, 0.8f);
        Assert.NotNull(sounds.Service.Capture);

        // The clips are decoded by the real mixer and reach the recording as sound, not silence.
        sounds.Service.Advance(24000);
        Assert.InRange(sounds.Service.Capture.Peak, 0.05f, 1f);
    }

    [Fact]
    public void EverySynthesizedClipIsAValidWave()
    {
        foreach (var e in Enum.GetValues<SoundEvent>())
        {
            var wav = AudioSounds.Synthesize(e);
            Assert.Equal("RIFF", Encoding.ASCII.GetString(wav, 0, 4));
            Assert.Equal("WAVE", Encoding.ASCII.GetString(wav, 8, 4));
            Assert.True(wav.Length > 1000);
        }
    }
}

public class LiveSessionTests
{
    [Fact]
    public void ClientStartsAGameMakesAMoveReadsTheScoreAndCapturesAFrameOverMcp()
    {
        using var cluster = TestCluster.Start(new SolitaireGame(), new TestOptions { Seed = 1 });
        using var server = new LiveServer(cluster.Client.Control);
        var socketPath = Path.Combine(cluster.OutputDirectory, "live.sock");
        server.Start(socketPath);
        var replies = new List<JsonDocument>();
        Exception? failure = null;
        var done = new ManualResetEventSlim();
        var driver = new Thread(() =>
        {
            try
            {
                using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                socket.Connect(new UnixDomainSocketEndPoint(socketPath));
                using var stream = new NetworkStream(socket);
                using var reader = new StreamReader(stream, Encoding.UTF8);
                using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };
                var id = 0;
                void Call(string tool, string args)
                {
                    writer.WriteLine($"{{\"jsonrpc\":\"2.0\",\"id\":{++id},\"method\":\"tools/call\",\"params\":{{\"name\":\"{tool}\",\"arguments\":{args}}}}}");
                    replies.Add(JsonDocument.Parse(reader.ReadLine()!));
                }

                writer.WriteLine("""{"jsonrpc":"2.0","id":100,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"client","version":"1"}}}""");
                _ = reader.ReadLine();
                writer.WriteLine("""{"jsonrpc":"2.0","method":"notifications/initialized"}""");
                Call("screen_open", """{"name":"main"}""");
                Call("ui_click", """{"id":"new-draw1"}""");
                Call("step", """{"frames":5}""");
                Call("command_send", """{"entity":"Table","command":"Draw"}""");
                Call("step", """{"frames":5}""");
                Call("state_query", """{"entity":"Table","property":"Moves"}""");
                Call("state_query", """{"entity":"Table","property":"Score"}""");
                Call("capture_frame", """{"size":"640x360"}""");
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                done.Set();
            }
        });
        driver.Start();
        var deadline = Environment.TickCount64 + 60000;
        while (!done.IsSet && Environment.TickCount64 < deadline)
        {
            server.Pump();
            server.WaitForWork(5);
        }

        driver.Join();
        Assert.Null(failure);
        Assert.Equal(8, replies.Count);
        string Text(int i) => replies[i].RootElement.GetProperty("result").GetProperty("content")[0].GetProperty("text").GetString()!;
        Assert.Equal("1", Text(5).Trim('"'));
        Assert.Equal("0", Text(6).Trim('"'));
        Assert.Contains(replies[7].RootElement.GetProperty("result").GetProperty("content").EnumerateArray(), c => c.GetProperty("type").GetString() == "image");
        Assert.Equal("table", cluster.Client.Game.CurrentScreen);
        foreach (var r in replies)
        {
            r.Dispose();
        }
    }
}
