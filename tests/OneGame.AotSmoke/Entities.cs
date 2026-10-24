using OneGame.Core;

namespace OneGame.AotSmoke;

/// <summary>A synced value used by the smoke entity.</summary>
[SyncedStruct]
public partial struct Coin
{
    public int Kind { get; set; }

    public Fixed Weight { get; set; }
}

/// <summary>An entity that exercises the generated code under NativeAOT: tracking, codecs, deltas, snapshots and commands.</summary>
[Entity]
public partial class Purse : Entity
{
    [Synced] public partial string Owner { get; set; }

    [Synced] public partial Fixed Total { get; private set; }

    [Synced] public SyncList<Coin> Coins { get; } = new();

    [Command]
    public bool Add(int kind, Fixed weight)
    {
        Coins.Add(new Coin { Kind = kind, Weight = weight });
        Total += weight;
        return true;
    }

    protected override void OnTick() => Total += Fixed.FromRatio(1, 4);
}

/// <summary>One row of the hot table.</summary>
[HotData]
public struct Spark
{
    public Fixed X { get; set; }

    public int Life { get; set; }
}
