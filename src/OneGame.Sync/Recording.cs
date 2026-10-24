using System.Globalization;

namespace OneGame.Sync;

/// <summary>A command that entered an authority from outside its tick.</summary>
/// <param name="Tick">The tick it ran in (the world's completed tick count when it was submitted).</param>
/// <param name="Entity">The target entity id.</param>
/// <param name="CommandId">The command id.</param>
/// <param name="Payload">The encoded arguments.</param>
public sealed record LoggedCommand(long Tick, ulong Entity, uint CommandId, byte[] Payload);

/// <summary>A hash of the whole world after a tick, with the per-part digest when it was small enough to keep.</summary>
/// <param name="Tick">The completed tick count.</param>
/// <param name="Hash">The world hash.</param>
/// <param name="Detail">Per-entity, per-table digest, or null.</param>
public sealed record Checkpoint(long Tick, ulong Hash, StateDigest? Detail);

/// <summary>How a recording samples the run.</summary>
public sealed class RecordingOptions
{
    /// <summary>Ticks between hash checkpoints. 1 pins any divergence to its exact tick; larger values name the first checkpoint after it. The last tick always gets one.</summary>
    public int CheckpointInterval { get; init; } = 30;

    /// <summary>Checkpoints keep a per-entity digest only for worlds with at most this many entities.</summary>
    public int DetailLimit { get; init; } = 4096;
}

/// <summary>
/// A run that can be replayed: the world at the start, every command that entered from outside, and hash checkpoints to verify
/// against. Saved as one file so it carries everything replay needs.
/// </summary>
public sealed class Recording
{
    private const uint Magic = 0x5052474F; // "OGRP" little-endian
    private const uint Format = 1;

    /// <summary>The seed of the recorded world (informational; the snapshot holds the random streams).</summary>
    public ulong Seed { get; set; }

    /// <summary>The tick of the start snapshot.</summary>
    public long StartTick { get; set; }

    /// <summary>The world hash right after loading the start snapshot.</summary>
    public ulong StartHash { get; set; }

    /// <summary>The tick the recording stopped at.</summary>
    public long EndTick { get; set; }

    /// <summary>The configured checkpoint interval.</summary>
    public int CheckpointInterval { get; set; }

    /// <summary>The world snapshot to start replay from.</summary>
    public byte[] StartSnapshot { get; set; } = [];

    /// <summary>Commands in submission order. Editable, so tests can corrupt a log on purpose.</summary>
    public List<LoggedCommand> Commands { get; } = [];

    /// <summary>Hash checkpoints in tick order.</summary>
    public List<Checkpoint> Checkpoints { get; } = [];

    /// <summary>Serializes the recording.</summary>
    public byte[] ToBytes()
    {
        var w = new SyncWriter(StartSnapshot.Length + 1024);
        w.WriteFixed32(Magic);
        w.WriteVarUInt(Format);
        w.WriteFixed64(Seed);
        w.WriteVarInt(StartTick);
        w.WriteFixed64(StartHash);
        w.WriteVarInt(EndTick);
        w.WriteVarUInt((ulong)CheckpointInterval);
        w.WriteBytes(StartSnapshot);
        w.WriteVarUInt((ulong)Commands.Count);
        var previous = StartTick;
        foreach (var c in Commands)
        {
            w.WriteVarInt(c.Tick - previous);
            previous = c.Tick;
            w.WriteVarUInt(c.Entity);
            w.WriteVarUInt(c.CommandId);
            w.WriteBytes(c.Payload);
        }

        w.WriteVarUInt((ulong)Checkpoints.Count);
        previous = StartTick;
        foreach (var cp in Checkpoints)
        {
            w.WriteVarInt(cp.Tick - previous);
            previous = cp.Tick;
            w.WriteFixed64(cp.Hash);
            w.WriteBool(cp.Detail is not null);
            cp.Detail?.Write(w);
        }

        return w.ToArray();
    }

    /// <summary>Decodes a recording from untrusted bytes.</summary>
    /// <param name="bytes">The file.</param>
    /// <param name="recording">The recording, or null on error.</param>
    /// <returns>None, or why decoding failed.</returns>
    public static SyncError TryRead(ReadOnlySpan<byte> bytes, out Recording? recording)
    {
        recording = null;
        var r = new SyncReader(bytes);
        if (r.ReadFixed32() != Magic || r.ReadVarUInt() != Format)
        {
            return r.Ok ? SyncError.BadHeader : r.Error;
        }

        var rec = new Recording
        {
            Seed = r.ReadFixed64(),
            StartTick = r.ReadVarInt(),
            StartHash = r.ReadFixed64(),
            EndTick = r.ReadVarInt(),
        };
        var interval = r.ReadVarUInt();
        rec.StartSnapshot = r.ReadBytes(int.MaxValue).ToArray();
        if (!r.Ok)
        {
            return r.Error;
        }

        if (rec.StartTick < 0 || rec.EndTick < rec.StartTick || interval > int.MaxValue)
        {
            return SyncError.InvalidValue;
        }

        rec.CheckpointInterval = (int)interval;
        var count = r.ReadCount(SyncLimits.MaxEntities);
        var tick = rec.StartTick;
        for (var i = 0; i < count && r.Ok; i++)
        {
            var delta = r.ReadVarInt();
            var entity = r.ReadVarUInt();
            var command = r.ReadVarUInt();
            var payload = r.ReadBytes(SyncLimits.MaxCommandBytes);
            if (!r.Ok)
            {
                break;
            }

            // Ticks never go backwards and never pass the end: replay submits commands by tick.
            if (delta < 0 || delta > rec.EndTick - tick || command > uint.MaxValue)
            {
                r.Fail(SyncError.InvalidValue);
                break;
            }

            tick += delta;
            rec.Commands.Add(new LoggedCommand(tick, entity, (uint)command, payload.ToArray()));
        }

        var checkpoints = r.ReadCount(SyncLimits.MaxEntities);
        tick = rec.StartTick;
        for (var i = 0; i < checkpoints && r.Ok; i++)
        {
            var delta = r.ReadVarInt();
            var hash = r.ReadFixed64();
            var hasDetail = r.ReadBool();
            if (!r.Ok)
            {
                break;
            }

            if (delta <= 0 || delta > rec.EndTick - tick)
            {
                r.Fail(SyncError.InvalidValue);
                break;
            }

            tick += delta;
            StateDigest? detail = null;
            if (hasDetail)
            {
                detail = new StateDigest();
                detail.Read(ref r);
            }

            if (r.Ok)
            {
                rec.Checkpoints.Add(new Checkpoint(tick, hash, detail));
            }
        }

        r.ExpectEnd();
        if (!r.Ok)
        {
            return r.Error;
        }

        recording = rec;
        return SyncError.None;
    }
}

/// <summary>Captures a run on an <see cref="AuthorityScope"/>. Create one with <see cref="AuthorityScope.StartRecording"/>.</summary>
public sealed class Recorder
{
    private readonly AuthorityScope _scope;
    private readonly RecordingOptions _options;
    private readonly SyncWriter _scratch = new(1024);
    private bool _stopped;

    internal Recorder(AuthorityScope scope, RecordingOptions options)
    {
        if (options.CheckpointInterval < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "CheckpointInterval must be at least 1.");
        }

        _scope = scope;
        _options = options;
        var world = scope.World;
        if (world.PendingCommandCount > 0)
        {
            throw new InvalidOperationException("Commands are queued for the next tick, and a snapshot does not hold them. Step once before starting a recording.");
        }

        var snapshot = new SyncWriter(4096);
        world.WriteSnapshot(snapshot);
        Recording = new Recording
        {
            Seed = world.Seed,
            StartTick = world.Tick,
            StartHash = world.ComputeHash(),
            EndTick = world.Tick,
            CheckpointInterval = options.CheckpointInterval,
            StartSnapshot = snapshot.ToArray(),
        };
    }

    /// <summary>The recording so far. Complete once <see cref="Stop"/> has been called.</summary>
    public Recording Recording { get; }

    internal void AddCommand(long tick, EntityId entity, uint commandId, ReadOnlySpan<byte> payload)
        => Recording.Commands.Add(new LoggedCommand(tick, entity.Value, commandId, payload.ToArray()));

    internal void OnTickEnd()
    {
        var world = _scope.World;
        Recording.EndTick = world.Tick;
        if ((world.Tick - Recording.StartTick) % _options.CheckpointInterval == 0)
        {
            AddCheckpoint(world);
        }
    }

    private void AddCheckpoint(World world)
    {
        if (Recording.Checkpoints.Count > 0 && Recording.Checkpoints[^1].Tick == world.Tick)
        {
            return;
        }

        var digest = new StateDigest();
        StateDigest.Capture(world, digest, _scratch, _options.DetailLimit);
        Recording.Checkpoints.Add(new Checkpoint(world.Tick, digest.WorldHash, digest.Truncated ? null : digest));
    }

    /// <summary>Ends the recording (adding a final checkpoint) and returns it.</summary>
    public Recording Stop()
    {
        if (!_stopped)
        {
            _stopped = true;
            Recording.EndTick = _scope.World.Tick;
            if (Recording.EndTick > Recording.StartTick)
            {
                AddCheckpoint(_scope.World);
            }

            _scope.RecorderStopped();
        }

        return Recording;
    }
}

/// <summary>Why a replay stopped.</summary>
public enum DivergenceReason
{
    /// <summary>The start snapshot could not be loaded into the replay world.</summary>
    StartSnapshot,

    /// <summary>The world loaded from the start snapshot does not hash to the recorded start hash.</summary>
    StartHash,

    /// <summary>A checkpoint hash differs from the recorded one.</summary>
    Checkpoint,

    /// <summary>The replay world is not an authority world, or the recording is inconsistent.</summary>
    Unusable,
}

/// <summary>The first place a replay stopped matching the recording.</summary>
/// <param name="Reason">What kind of mismatch.</param>
/// <param name="Tick">The completed tick count at which the mismatch was seen.</param>
/// <param name="LastGoodTick">The last tick at which the replay matched; the real divergence is in (LastGoodTick, Tick].</param>
/// <param name="ExpectedHash">The recorded hash.</param>
/// <param name="ActualHash">The replayed hash.</param>
/// <param name="Differences">The entities, tables or streams that differ; empty if the checkpoint kept no digest.</param>
/// <param name="Detail">A sentence on what happened.</param>
public sealed record Divergence(DivergenceReason Reason, long Tick, long LastGoodTick, ulong ExpectedHash, ulong ActualHash, IReadOnlyList<StateDifference> Differences, string Detail)
{
    /// <summary>The divergence as JSON.</summary>
    public string ToJson()
    {
        var j = new JsonOut();
        WriteJson(j);
        return j.ToString();
    }

    internal void WriteJson(JsonOut j)
    {
        j.BeginObject();
        j.Name("reason");
        j.Value(Reason.ToString());
        j.Name("tick");
        j.Value(Tick);
        j.Name("lastGoodTick");
        j.Value(LastGoodTick);
        j.Name("expectedHash");
        j.Value(ExpectedHash.ToString("x16", CultureInfo.InvariantCulture));
        j.Name("actualHash");
        j.Value(ActualHash.ToString("x16", CultureInfo.InvariantCulture));
        j.Name("detail");
        j.Value(Detail);
        j.Name("differences");
        StateDifference.WriteJsonArray(j, Differences);
        j.EndObject();
    }

    /// <inheritdoc />
    public override string ToString() => $"{Reason} at tick {Tick} (last good {LastGoodTick}): {Detail}";
}

/// <summary>The outcome of <see cref="Replayer.Replay"/>.</summary>
public sealed class ReplayResult
{
    internal ReplayResult(bool ok, long ticksRun, int checkpointsVerified, ulong finalHash, Divergence? divergence)
    {
        Ok = ok;
        TicksRun = ticksRun;
        CheckpointsVerified = checkpointsVerified;
        FinalHash = finalHash;
        Divergence = divergence;
    }

    /// <summary>True when every checkpoint matched.</summary>
    public bool Ok { get; }

    /// <summary>Ticks simulated.</summary>
    public long TicksRun { get; }

    /// <summary>Checkpoints that matched.</summary>
    public int CheckpointsVerified { get; }

    /// <summary>The replay world's hash when it stopped.</summary>
    public ulong FinalHash { get; }

    /// <summary>The first mismatch, or null.</summary>
    public Divergence? Divergence { get; }

    /// <summary>The result as JSON.</summary>
    public string ToJson()
    {
        var j = new JsonOut();
        j.BeginObject();
        j.Name("ok");
        j.Value(Ok);
        j.Name("ticksRun");
        j.Value(TicksRun);
        j.Name("checkpointsVerified");
        j.Value(CheckpointsVerified);
        j.Name("finalHash");
        j.Value(FinalHash.ToString("x16", CultureInfo.InvariantCulture));
        if (Divergence is not null)
        {
            j.Name("divergence");
            Divergence.WriteJson(j);
        }

        j.EndObject();
        return j.ToString();
    }
}

/// <summary>Options for <see cref="Replayer.Replay"/>.</summary>
public sealed class ReplayOptions
{
    /// <summary>Called after every replayed tick, for captures and assertions.</summary>
    public Action<World>? OnTick { get; init; }
}

/// <summary>Re-runs a <see cref="Recording"/> and checks the hashes.</summary>
public static class Replayer
{
    /// <summary>
    /// Loads the start snapshot into <paramref name="world"/>, then submits the logged commands before the tick they ran in and
    /// steps, comparing the world hash at every checkpoint. Stops at the first mismatch.
    /// </summary>
    /// <param name="recording">The recording.</param>
    /// <param name="world">A fresh authority world built like the recorded one: same registry, tables and systems, added in the same order.</param>
    /// <param name="options">Optional hooks.</param>
    public static ReplayResult Replay(Recording recording, World world, ReplayOptions? options = null)
    {
        if (world.Role != WorldRole.Authority)
        {
            return Fail(DivergenceReason.Unusable, recording.StartTick, recording.StartTick, 0, 0, [], "The replay world must be an authority world.", 0, 0, 0);
        }

        var error = world.ReadSnapshot(recording.StartSnapshot);
        if (error != SyncError.None)
        {
            return Fail(DivergenceReason.StartSnapshot, recording.StartTick, recording.StartTick, recording.StartHash, 0, [], "The start snapshot could not be loaded: " + error + ".", 0, 0, 0);
        }

        if (world.Tick != recording.StartTick || world.ComputeHash() != recording.StartHash)
        {
            return Fail(DivergenceReason.StartHash, world.Tick, recording.StartTick, recording.StartHash, world.ComputeHash(), [], "The loaded start state does not hash to the recorded start hash. The world was built differently from the recorded one.", 0, 0, 0);
        }

        var scratch = new SyncWriter(1024);
        var commandIndex = 0;
        var checkpointIndex = 0;
        var verified = 0;
        var lastGood = recording.StartTick;
        long ran = 0;
        while (world.Tick < recording.EndTick)
        {
            while (commandIndex < recording.Commands.Count && recording.Commands[commandIndex].Tick <= world.Tick)
            {
                var c = recording.Commands[commandIndex++];
                world.Submit(new EntityId(c.Entity), c.CommandId, c.Payload);
            }

            world.Step();
            ran++;
            options?.OnTick?.Invoke(world);
            while (checkpointIndex < recording.Checkpoints.Count && recording.Checkpoints[checkpointIndex].Tick <= world.Tick)
            {
                var cp = recording.Checkpoints[checkpointIndex++];
                if (cp.Tick != world.Tick)
                {
                    continue;
                }

                var actual = world.ComputeHash();
                if (actual == cp.Hash)
                {
                    verified++;
                    lastGood = cp.Tick;
                    continue;
                }

                var differences = new List<StateDifference>();
                var detail = "The state hash differs from the recording.";
                if (cp.Detail is not null)
                {
                    var now = new StateDigest();
                    StateDigest.Capture(world, now, scratch);
                    StateDigest.Diff(cp.Detail, now, null, world, differences);
                    detail = differences.Count == 0 ? "The state hash differs but no entity, table or random stream does; the tick counter differs." : $"{differences.Count} part(s) of the state differ.";
                }
                else
                {
                    detail += " The checkpoint kept no per-entity digest.";
                }

                return Fail(DivergenceReason.Checkpoint, cp.Tick, lastGood, cp.Hash, actual, differences, detail, ran, verified, actual);
            }
        }

        return new ReplayResult(true, ran, verified, world.ComputeHash(), null);
    }

    private static ReplayResult Fail(DivergenceReason reason, long tick, long lastGood, ulong expected, ulong actual, IReadOnlyList<StateDifference> differences, string detail, long ran, int verified, ulong finalHash)
        => new(false, ran, verified, finalHash, new Divergence(reason, tick, lastGood, expected, actual, differences, detail));
}
