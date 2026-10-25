namespace OneGame.Sync;

public sealed partial class AuthorityScope
{
    /// <summary>
    /// A relay's upstream replica just handled a message. A snapshot (first, or after a resync) is passed on as a fresh snapshot to every
    /// subscriber; a frame is passed on as one frame for the same tick, filtered per audience; a revoked or refused subscription empties the
    /// subscribers' view too.
    /// </summary>
    internal void OnUpstream(byte messageType)
    {
        var up = Upstream!;
        switch (messageType)
        {
            case Msg.Snapshot:
                if (up.State == ReplicaState.Live)
                {
                    _relayTick = up.LastTick;
                    ResendSnapshots();
                }

                break;
            case Msg.Frame:
                if (up.State == ReplicaState.Live && up.LastTick != _relayTick)
                {
                    _relayTick = up.LastTick;
                    Publish();
                }

                break;
            case Msg.Revoked:
            case Msg.SubscribeRefused:
                ResendSnapshots();
                break;
        }
    }
}
