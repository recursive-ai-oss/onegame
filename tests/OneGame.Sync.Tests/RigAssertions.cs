namespace OneGame.Sync.Tests;

internal static class RigAssertions
{
    /// <summary>Steps the cluster and, every tick, checks that each player's replica holds exactly the view the authority computes for it.</summary>
    public static void StepChecked(this Rig rig, int ticks, params Player[] players)
    {
        for (var i = 0; i < ticks; i++)
        {
            rig.Cluster.Step();
            foreach (var p in players)
            {
                if (p.Replica.State == ReplicaState.Live)
                {
                    Assert.Equal(rig.Scope.ViewHashFor(p.Link.Connection), p.Replica.ViewHash);
                }
            }
        }
    }
}
