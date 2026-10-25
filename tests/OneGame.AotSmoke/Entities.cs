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

    [Command(Predicted = true)]
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

/// <summary>An entity with owner-only and hidden members and an owner-only command, to exercise filtered replicas under NativeAOT.</summary>
[Entity]
public partial class Locker : Entity
{
    [Synced, OwnerKey] public partial uint Player { get; set; }

    [Synced] public partial int Opened { get; set; }

    [Synced(Visibility = Visibility.Owner)] public partial string Combination { get; set; }

    [Synced(Visibility = Visibility.Hidden)] public partial int Alarm { get; set; }

    [Command]
    public CommandReply Open(int code)
    {
        if (code != 1234)
        {
            return CommandReply.Reject("wrong code");
        }

        Opened++;
        return CommandReply.Ok;
    }

    [Command(OwnerOnly = true)]
    public void Reset(string combination) => Combination = combination;

    protected override void OnTick() => Alarm++;
}
