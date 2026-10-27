using OneGame.Core;
using OneGame.Sync;

namespace OneGame.Corpus;

/// <summary>
/// The parts of a run that are not frames: saves, recordings in segments, and late-joining subscribers whose first message is a snapshot.
/// Everything is taken through the engine's own calls (<c>Save</c>, <c>StartRecording</c>, a real subscription).
/// </summary>
internal sealed class Archivist
{
    /// <summary>One authority scope and how to subscribe to it from a stranger.</summary>
    public sealed record Target(AuthorityScope Scope, Func<World> NewReplicaWorld, Func<Rng, Trust> LateTrust, string Name);

    private readonly Cluster _cluster;
    private readonly Capture _capture;
    private readonly Rng _rng;
    private readonly List<Target> _targets;
    private readonly Dictionary<AuthorityScope, Recorder> _recorders = [];
    private readonly int _recordingTicks;

    public Archivist(Cluster cluster, Capture capture, Rng rng, List<Target> targets, int recordingTicks)
    {
        _cluster = cluster;
        _capture = capture;
        _rng = rng;
        _targets = targets;
        _recordingTicks = recordingTicks;
    }

    /// <summary>Files a save the run took (the schema label comes from the save's own header).</summary>
    public void StoreSave(byte[] bytes)
    {
        var label = _capture.Label(Kinds.Saves);
        if (SaveFile.TryReadHeader(bytes, out var header, out _, out _) == SaveStatus.Ok && header is not null)
        {
            var max = 0u;
            foreach (var s in header.Schemas)
            {
                max = Math.Max(max, s.Version);
            }

            label = "save" + header.Format.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".e" + max.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        _capture.Store(Kinds.Saves, bytes, label);
    }

    /// <summary>Starts a recording on each scope that has none and closes the ones that ran long enough. Call between ticks.</summary>
    public void Recordings()
    {
        foreach (var t in _targets)
        {
            if (_recorders.TryGetValue(t.Scope, out var open) && t.Scope.World.Tick - open.Recording.StartTick >= _recordingTicks)
            {
                _capture.Store(Kinds.Recordings, open.Stop().ToBytes());
                _recorders.Remove(t.Scope);
            }

            if (!_recorders.ContainsKey(t.Scope) && t.Scope.World.PendingCommandCount == 0 && !_capture.Sink.Full(_capture.Game, Kinds.Recordings))
            {
                _recorders[t.Scope] = t.Scope.StartRecording();
            }
        }
    }

    /// <summary>A stranger joins one scope, gets the snapshot, and leaves. The snapshot is what a late joiner or a reconnecting client receives.</summary>
    public void LateJoin()
    {
        var target = _targets[_rng.Range(0, _targets.Count)];
        var node = _cluster.AddNode("late");
        var (client, server) = Loopback.CreatePair();
        var link = node.ConnectTo(target.Scope.Node, target.LateTrust(_rng), client, _capture.ServerEnd(server));
        node.Subscribe(link, target.Scope.Id, target.NewReplicaWorld());
        _cluster.Settle();
        target.Scope.Node.Detach(link.Connection);
        _cluster.RemoveNode(node);
    }

    /// <summary>Closes open recordings and files them.</summary>
    public void Finish()
    {
        foreach (var (scope, recorder) in _recorders)
        {
            _ = scope;
            _capture.Store(Kinds.Recordings, recorder.Stop().ToBytes());
        }

        _recorders.Clear();
    }
}
