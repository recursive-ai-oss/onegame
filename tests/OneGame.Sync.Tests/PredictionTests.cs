namespace OneGame.Sync.Tests;

/// <summary>An authority node and a player node joined by a link with a fixed round trip, stepped by hand so tests control every tick.</summary>
internal sealed class PredictionRig
{
    public PredictionRig(int lagEachWay, int lead, Trust? trust = null, PredictionOptions? options = null, uint player = 1, int runners = 4, int markers = 50, bool predict = true, bool autoLead = false)
    {
        var clusterOptions = new ClusterOptions { DriftCheckInterval = 1 };
        AuthorityNode = new Node("host", clusterOptions);
        PlayerNode = new Node("player", clusterOptions);
        Scope = AuthorityNode.AddScope(new ScopeId(1), Track.NewAuthority(runners, markers));
        if (lagEachWay < 0)
        {
            Link = PlayerNode.ConnectTo(AuthorityNode, trust ?? Trust.Owner(Scope.Id).As(player), "player");
        }
        else
        {
            var (client, server) = Line.Create(lagEachWay);
            Link = PlayerNode.ConnectTo(AuthorityNode, trust ?? Trust.Owner(Scope.Id).As(player), client, server, "player");
        }

        View = PlayerNode.Subscribe(Link, Scope.Id, Track.NewWorld(WorldRole.Replica));
        Prediction = predict ? View.EnablePrediction(Track.NewWorld(WorldRole.Predicted), options ?? new PredictionOptions { LeadTicks = lead, AutoLead = autoLead }) : null!;
        AuthorityHashes.Add(Scope.ViewHashFor(Link.Connection));
    }

    public LagLine Line { get; } = new();

    public Node AuthorityNode { get; }

    public Node PlayerNode { get; }

    public AuthorityScope Scope { get; }

    public Link Link { get; }

    public ReplicaScope View { get; }

    public Prediction Prediction { get; }

    /// <summary>The view hash the authority had for this player after each tick, by tick.</summary>
    public List<ulong> AuthorityHashes { get; } = [];

    /// <summary>Confirmed ticks compared with the authority's hash so far.</summary>
    public int ConfirmedChecks { get; private set; }

    /// <summary>The player's runner, as the predicted world has it.</summary>
    public Runner Mine => (Runner)Prediction.World.Find(new EntityId(1))!;

    /// <summary>The same runner as the confirmed replica has it.</summary>
    public Runner Confirmed => (Runner)View.World.Find(new EntityId(1))!;

    /// <summary>A rig on plain loopback channels (no delay, no copying), for allocation and timing measurements.</summary>
    public static PredictionRig Plain(int lead, int runners = 4, int markers = 50) => new(-1, lead, runners: runners, markers: markers);

    public Runner OnAuthority => (Runner)Scope.World.Find(new EntityId(1))!;

    /// <summary>One step: deliver what arrived, check the confirmed state against the authority, let the player act, run the predicted tick, run the authority tick.</summary>
    public void Step(Action<PredictionRig>? input = null, bool advance = true)
    {
        Line.Now++;
        AuthorityNode.Pump();
        PlayerNode.Pump();
        var tick = (int)View.World.Tick;
        if (View.State == ReplicaState.Live && tick > 0 && tick < AuthorityHashes.Count)
        {
            Assert.True(AuthorityHashes[tick] == View.World.ComputeViewHash(), $"the confirmed replica differs from the authority at tick {tick}");
            ConfirmedChecks++;
        }

        input?.Invoke(this);
        if (advance)
        {
            Prediction.Advance();
        }

        Scope.StepAndPublish();
        AuthorityHashes.Add(Scope.ViewHashFor(Link.Connection));
    }

    public void Run(int steps, Action<PredictionRig>? input = null, bool advance = true)
    {
        for (var i = 0; i < steps; i++)
        {
            Step(input, advance);
        }
    }
}

public class PredictionTests
{
    private static void SteerEveryTick(PredictionRig rig)
    {
        var t = (int)(rig.Scope.World.Tick % 40);
        var dx = t < 20 ? 1 : -1;
        var dy = (t % 7) - 3;
        var expected = new FVec2(dx, dy);
        rig.Mine.Send.Steer(dx, dy);
        Assert.True(rig.Mine.Velocity == expected, "the predicted state does not show the command on the tick it was sent");
    }

    [Fact]
    public void PredictedCommandsShowAtOnceWhileConfirmedStateStaysExact()
    {
        var rig = new PredictionRig(lagEachWay: 4, lead: 8);
        rig.Run(20); // the snapshot arrives
        Assert.Equal(ReplicaState.Live, rig.View.State);
        rig.Run(300, SteerEveryTick);
        Assert.True(rig.ConfirmedChecks > 250, $"only {rig.ConfirmedChecks} confirmed ticks were compared");
        Assert.Empty(rig.PlayerNode.DriftReports);
        Assert.Equal(300, rig.Prediction.CommandsPredicted);
        Assert.True(rig.Prediction.Unconfirmed > 0, "commands should be in flight over a link with a round trip");

        // Stop sending: everything is confirmed and the prediction is the confirmed state run forward by its lead.
        rig.Run(40);
        Assert.Equal(0, rig.Prediction.Unconfirmed);
        var ahead = rig.Prediction.Tick - rig.View.World.Tick;
        Assert.InRange(ahead, 8, 9);
        var p = rig.Mine;
        var c = rig.Confirmed;
        Assert.True(p.Position == c.Position + (c.Velocity * Fixed.FromInt(ahead)), "no command is in flight, so the prediction is the confirmed state moving on its velocity");
    }

    [Fact]
    public void ALeadMatchingTheRoundTripRemovesTheVisibleCorrections()
    {
        // Each way takes 4 steps plus one for the pump, so a command sent now runs on the authority 9 ticks ahead of the confirmed tick the replica sees.
        var none = new PredictionRig(lagEachWay: 4, lead: 0);
        var fixedLead = new PredictionRig(lagEachWay: 4, lead: 9);
        var measured = new PredictionRig(lagEachWay: 4, lead: 0, autoLead: true);
        foreach (var rig in new[] { none, fixedLead, measured })
        {
            rig.Run(20);
            rig.Run(300, SteerEveryTick);
            Assert.Empty(rig.PlayerNode.DriftReports);
        }

        Console.WriteLine($"rollbacks over 300 steered ticks (round trip 8 ticks): lead 0 -> {none.Prediction.Rollbacks} of {none.Prediction.Reconciliations} reconciliations; lead 9 -> {fixedLead.Prediction.Rollbacks}; measured (lead {measured.Prediction.LeadTicks}) -> {measured.Prediction.Rollbacks}");
        Assert.True(none.Prediction.Rollbacks > 100, $"lead 0 had {none.Prediction.Rollbacks} rollbacks");
        Assert.True(fixedLead.Prediction.Rollbacks <= 2, $"a lead of one round trip should predict what the authority does; {fixedLead.Prediction.Rollbacks} rollbacks");
        Assert.Equal(9, measured.Prediction.LeadTicks); // 4 steps each way, plus a step each for the receiving pump, less the tick the command runs in
        Assert.True(measured.Prediction.Rollbacks <= 20, $"the measured lead settled slowly: {measured.Prediction.Rollbacks} rollbacks");
    }

    [Fact]
    public void ARejectedPredictedCommandIsRolledBackAndTheReplicaConverges()
    {
        var rig = new PredictionRig(lagEachWay: 3, lead: 0);
        rig.Run(30);
        var shown = new List<FVec2>();
        var corrected = new List<FVec2>();
        FVec2 before = default;
        rig.Prediction.Reconciling += _ => before = rig.Prediction.World.Find(new EntityId(1)) is Runner r ? r.Position : default;
        rig.Prediction.Reconciled += p => { if (before != ((Runner)p.World.Find(new EntityId(1))!).Position) { shown.Add(before); corrected.Add(((Runner)p.World.Find(new EntityId(1))!).Position); } };

        // Someone else spent the energy on the authority; this replica has not heard.
        rig.Step(r =>
        {
            r.OnAuthority.Energy = 0;
            var start = r.Mine.Position;
            r.Mine.Send.Dash(5);
            Assert.True(r.Mine.Position == start + new FVec2(5, 0), "the dash shows at once");
            Assert.True(r.Mine.Energy >= 40);
        });
        var sequence = rig.View.LastSequence;
        rig.Run(40);

        var answer = rig.View.Results.Single(x => x.Sequence == sequence);
        Assert.Equal(CommandVerdict.Rejected, answer.Verdict);
        Assert.Equal("too tired", answer.Reason);
        Assert.True(rig.Prediction.Rollbacks >= 1);
        Assert.Equal(0, rig.Prediction.Unconfirmed);
        Assert.NotEmpty(shown);
        Assert.True(shown.Any(s => s.X - corrected[shown.IndexOf(s)].X == 5), "the hooks show the five-unit correction a view would blend away");

        // Let the confirmed state pass the prediction: the two are then the same world.
        rig.Run(20, advance: false);
        Assert.Equal(rig.View.World.ComputeHash(), rig.Prediction.World.ComputeHash());
        Assert.True(rig.Confirmed.Energy < 20, "the authority's energy is what the replica ends up with");
        Assert.Empty(rig.PlayerNode.DriftReports);
    }

    [Fact]
    public void ARefusedPredictedCommandIsRolledBack()
    {
        var rig = new PredictionRig(lagEachWay: 3, lead: 0, trust: Trust.Player(new ScopeId(1)).As(1));
        rig.Run(30);
        var start = rig.Mine.Position;
        rig.Step(r =>
        {
            r.Mine.Send.Teleport(500, 500);
            Assert.True(r.Mine.Position == new FVec2(500, 500));
        });
        var sequence = rig.View.LastSequence;
        rig.Run(20);
        var answer = rig.View.Results.Single(x => x.Sequence == sequence);
        Assert.Equal(CommandVerdict.Refused, answer.Verdict);
        Assert.Equal(RefusalReason.NotTrustedToOwn, answer.Refusal);
        Assert.Equal(0, rig.Prediction.Unconfirmed);
        rig.Run(20, advance: false);
        Assert.Equal(rig.View.World.ComputeHash(), rig.Prediction.World.ComputeHash());
        Assert.True(rig.Mine.Position == rig.Confirmed.Position && rig.Mine.Position.X < 400);
        Assert.True(rig.Prediction.Rollbacks >= 1);
    }

    [Fact]
    public void ACommandThatIsNotPredictedChangesOnlyConfirmedState()
    {
        var rig = new PredictionRig(lagEachWay: 3, lead: 0);
        rig.Run(30);
        var energy = rig.Mine.Energy;
        rig.Step(r => r.Mine.Send.Rest());
        Assert.Equal(0, rig.Prediction.CommandsPredicted);
        Assert.InRange(rig.Mine.Energy, energy, energy + 1); // only the tick rule moves it
        rig.Run(20);
        Assert.True(rig.Mine.Energy >= energy + 50);
    }

    [Fact]
    public void ASnapshotInTheMiddleOfPredictionSettlesOnTheAuthority()
    {
        var rig = new PredictionRig(lagEachWay: 4, lead: 0);
        rig.Run(20);
        rig.Run(30, SteerEveryTick);
        var loaded = rig.Scope.Load(rig.Scope.Save()); // every subscriber gets a fresh snapshot; queued commands are dropped
        Assert.Equal(SaveStatus.Ok, loaded.Status);
        rig.Run(60, SteerEveryTick);
        rig.Run(60);
        rig.Run(40, advance: false);
        Assert.Equal(0, rig.Prediction.Unconfirmed);
        Assert.Equal(rig.View.World.ComputeHash(), rig.Prediction.World.ComputeHash());
        Assert.Empty(rig.PlayerNode.DriftReports);
    }

    private static List<(ulong Confirmed, ulong Predicted)> Scenario(int lead)
    {
        var rig = new PredictionRig(lagEachWay: 5, lead: lead);
        var hashes = new List<(ulong, ulong)>();
        for (var i = 0; i < 400; i++)
        {
            rig.Step(r =>
            {
                if (i > 20)
                {
                    SteerEveryTick(r);
                    if (i % 37 == 0)
                    {
                        r.Mine.Send.Dash(3);
                    }

                    if (i % 91 == 0)
                    {
                        r.OnAuthority.Energy = 0;
                    }
                }
            });
            hashes.Add((rig.View.World.ComputeHash(), rig.Prediction.World.ComputeHash()));
        }

        hashes.Add(((ulong)rig.Prediction.Rollbacks, (ulong)rig.Prediction.Reconciliations));
        return hashes;
    }

    [Fact]
    public void TwoRunsOfTheSameInputGiveTheSameHashesTickForTick()
    {
        var first = Scenario(10);
        var second = Scenario(10);
        Assert.Equal(first, second);
        Assert.NotEqual(first[100].Confirmed, first[101].Confirmed);
    }

    [Fact]
    public void PredictionDoesNotChangeWhatTheAuthorityRecordsOrReplays()
    {
        var withPrediction = new PredictionRig(lagEachWay: 3, lead: 6);
        var without = new PredictionRig(lagEachWay: 3, lead: 6, predict: false);
        var recorderA = withPrediction.Scope.StartRecording(new RecordingOptions { CheckpointInterval = 1 });
        var recorderB = without.Scope.StartRecording(new RecordingOptions { CheckpointInterval = 1 });
        for (var i = 0; i < 200; i++)
        {
            withPrediction.Step(r => { if (i > 10) { SteerEveryTick(r); } });
            without.Step(r =>
            {
                if (i > 10)
                {
                    var t = (int)(r.Scope.World.Tick % 40);
                    r.Confirmed.Send.Steer(t < 20 ? 1 : -1, (t % 7) - 3);
                }
            }, advance: false);
        }

        var a = recorderA.Stop();
        var b = recorderB.Stop();
        Assert.Equal(withPrediction.Scope.World.ComputeHash(), without.Scope.World.ComputeHash());
        Assert.Equal(a.ToBytes(), b.ToBytes());
        var replay = Replayer.Replay(a, Track.NewAuthority());
        Assert.True(replay.Ok, replay.Divergence?.ToString());
    }

    [Fact]
    public void AMemberThatMovesOnATimedValueInterpolatesWithNothingSentPerTick()
    {
        var rig = new PredictionRig(lagEachWay: 2, lead: 0, runners: 0, markers: 0);
        var drifter = rig.Scope.World.Add(new Drifter());
        var sizes = new List<int>();
        rig.Line.OnClientReceive = n => sizes.Add(n);
        rig.Run(80);
        var idle = sizes.TakeLast(10).Max();
        sizes.Clear();

        drifter.Send.GlideTo(100, 40, 60);
        rig.Run(1);
        sizes.Clear();
        var replicaDrifter = (Drifter)rig.View.World.Find(drifter.Id)!;
        rig.Run(10);
        var started = replicaDrifter.Glide;
        Assert.Equal(60, started.EndTick - started.StartTick);

        // From here the replica holds start, end and the two ticks. Every following frame is as small as an idle one.
        sizes.Clear();
        var samples = new List<Fixed>();
        var half = Fixed.FromRatio(1, 2);
        for (var i = 0; i < 40; i++)
        {
            rig.Run(1);
            var g = ((Drifter)rig.View.World.Find(drifter.Id)!).Glide;
            var tick = rig.View.World.Tick;
            samples.Add(g.ValueAt(tick).X);
            samples.Add(g.ValueAt(tick, half).X);
        }

        Assert.True(sizes.Count >= 40);
        Assert.True(sizes.Max() <= idle + 1, $"a frame during the glide was {sizes.Max()} bytes; an idle frame is {idle}");
        for (var i = 1; i < samples.Count; i++)
        {
            Assert.True(samples[i] > samples[i - 1], $"sample {i} did not move forward: {samples[i - 1]} then {samples[i]}");
        }

        Assert.True(samples[^1].ToDouble() < 100 && samples[0].ToDouble() > 0);
    }

    /// <summary>
    /// Runs the player's side only (pump, steer, advance), counting the bytes that side allocates and the time it takes. The authority's
    /// own work is outside the measured calls.
    /// </summary>
    private static (long Bytes, double MicrosecondsPerStep, long Rollbacks) Measure(int lead, bool commands, int runners = 4, int markers = 50)
    {
        var rig = PredictionRig.Plain(lead, runners, markers);
        foreach (var e in rig.Scope.World.Entities)
        {
            ((Runner)e).Tracking = false;
        }

        for (var i = 0; i < 300; i++)
        {
            rig.Step(commands ? SteerEveryTick : null);
        }

        const int steps = 600;
        long bytes = 0;
        long ticks = 0;
        for (var i = 0; i < steps; i++)
        {
            rig.AuthorityNode.Pump();
            var before = GC.GetAllocatedBytesForCurrentThread();
            var t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            rig.PlayerNode.Pump();
            if (commands)
            {
                SteerEveryTick(rig);
            }

            rig.Prediction.Advance();
            ticks += System.Diagnostics.Stopwatch.GetTimestamp() - t0;
            bytes += GC.GetAllocatedBytesForCurrentThread() - before;
            rig.Scope.StepAndPublish();
            rig.AuthorityHashes.Add(0);
        }

        return (bytes, ticks * 1_000_000.0 / System.Diagnostics.Stopwatch.Frequency / steps, rig.Prediction.Rollbacks);
    }

    [Fact]
    public void APredictedReplicaAllocatesNothingPerTickInSteadyState()
    {
        var idle = Measure(lead: 10, commands: false);
        Assert.Equal(0, idle.Bytes);
        Console.WriteLine($"idle replica, lead 10: {idle.Bytes} bytes over 600 ticks, {idle.MicrosecondsPerStep:F1} us per tick");
    }

    [Fact]
    public void APredictedReplicaSendingEveryTickAllocatesNothingPerTick()
    {
        var busy = Measure(lead: 10, commands: true);
        Console.WriteLine($"replica steering every tick, lead 10: {busy.Bytes} bytes over 600 ticks, {busy.MicrosecondsPerStep:F1} us per tick, {busy.Rollbacks} rollbacks");
        Assert.Equal(0, busy.Bytes);
    }

    [Fact]
    public void ReportTheCostOfResimulatingTenTicks()
    {
        foreach (var (runners, markers) in new[] { (4, 50), (200, 2000) })
        {
            var zero = Measure(lead: 0, commands: false, runners, markers);
            var ten = Measure(lead: 10, commands: false, runners, markers);
            Console.WriteLine($"{runners} runners + {markers} marker rows: reconcile with no resimulation {zero.MicrosecondsPerStep:F1} us per tick; with 10 resimulated ticks {ten.MicrosecondsPerStep:F1} us per tick -> {(ten.MicrosecondsPerStep - zero.MicrosecondsPerStep):F1} us for the ten ticks");
            Assert.True(ten.MicrosecondsPerStep > 0);
        }
    }
}
