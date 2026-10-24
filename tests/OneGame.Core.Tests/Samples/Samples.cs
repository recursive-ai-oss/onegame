namespace OneGame.Core.Tests.Samples;

public enum Mood : byte
{
    Calm,
    Eager,
    Angry,
}

[SyncedStruct]
public partial struct ItemSlot
{
    public int ItemId { get; set; }
    public int Count { get; set; }
    public Mood Tag { get; set; }
}

/// <summary>A small shop: money, stock and a list of items. Schema 2 added Stock and turned Gold into fixed-point.</summary>
[Entity(Schema = 2)]
public partial class Shop : Entity
{
    [Synced] public partial string Name { get; set; }

    [Synced] public partial Fixed Gold { get; private set; }

    [Synced] public partial int Stock { get; set; }

    [Synced] public partial bool Open { get; set; }

    [Synced] public partial Mood Mood { get; set; }

    [Synced] public partial Timed<Fixed> Price { get; set; }

    [Synced] public SyncList<ItemSlot> Items { get; } = new();

    [Command]
    public bool Buy(int itemId, int count)
    {
        for (var i = 0; i < Items.Count; i++)
        {
            var stack = Items[i];
            if (stack.ItemId != itemId || stack.Count < count || count <= 0)
            {
                continue;
            }

            stack.Count -= count;
            Items[i] = stack;
            Stock -= count;
            Gold += Price.To * count;
            return true;
        }

        return false;
    }

    [Command]
    public void Restock(ItemSlot stack)
    {
        Items.Add(stack);
        Stock += stack.Count;
    }

    [Command]
    public void Rename(string name) => Name = name;

    /// <summary>Reads the layout of schema 1: name, whole gold, items.</summary>
    [Migrate(1)]
    private static void FromV1(ref SyncReader old, Shop shop)
    {
        shop.Name = old.ReadString();
        shop.Gold = Fixed.FromInt(old.ReadInt32());
        var n = old.ReadCount(1000);
        var items = new ItemSlot[n];
        for (var i = 0; i < n && old.Ok; i++)
        {
            items[i] = new ItemSlot { ItemId = old.ReadInt32(), Count = old.ReadInt32(), Tag = (Mood)old.ReadByte() };
        }

        if (old.Ok)
        {
            shop.Items.ReplaceWith(items);
        }
    }

    [View]
    public string Label => $"{Name}: {Items.Count} stacks";

    protected override void OnTick()
    {
        if (Price.EndTick > 0 && World!.Tick >= Price.EndTick)
        {
            Price = new Timed<Fixed>(Price.To, Price.To, World.Tick, World.Tick);
        }
    }
}

/// <summary>A wandering bot kept as an ordinary entity.</summary>
[Entity]
public partial class Bot : Entity
{
    [Synced] public partial FVec2 Position { get; set; }

    [Synced] public partial FVec2 Velocity { get; set; }

    [Synced] public partial int Hp { get; set; }

    protected override void OnTick()
    {
        var next = Position + Velocity;
        if (next.X < Fixed.Zero || next.X > 100)
        {
            Velocity = new FVec2(-Velocity.X, Velocity.Y);
        }

        if (next.Y < Fixed.Zero || next.Y > 100)
        {
            Velocity = new FVec2(Velocity.X, -Velocity.Y);
        }

        Position += Velocity;
        var rng = World!.Rng["bots"];
        if (rng.NextInt(50) == 0)
        {
            Hp--;
            Velocity = new FVec2(Fixed.FromRatio(rng.NextInt(-8, 9), 8), Fixed.FromRatio(rng.NextInt(-8, 9), 8));
        }
    }

    [Command]
    public void Heal(int amount) => Hp += amount;
}

/// <summary>Row of the hot bot table.</summary>
[HotData(Name = "bots")]
public struct BotRow
{
    public Fixed X { get; set; }
    public Fixed Y { get; set; }
    public Fixed Vx { get; set; }
    public Fixed Vy { get; set; }
    public int Hp { get; set; }
}

/// <summary>Moves every row of the hot table; allocates nothing per tick.</summary>
public sealed class BotSystem : ISimSystem
{
    private readonly BotRowTable _table;

    public BotSystem(BotRowTable table) => _table = table;

    public void Tick(World world)
    {
        var x = _table.X;
        var y = _table.Y;
        var vx = _table.Vx;
        var vy = _table.Vy;
        var hp = _table.Hp;
        var rng = world.Rng["hotbots"];
        for (var i = 0; i < x.Length; i++)
        {
            var nx = x[i] + vx[i];
            var ny = y[i] + vy[i];
            if (nx < Fixed.Zero || nx > 100)
            {
                vx[i] = -vx[i];
            }

            if (ny < Fixed.Zero || ny > 100)
            {
                vy[i] = -vy[i];
            }

            x[i] += vx[i];
            y[i] += vy[i];
            if (rng.NextInt(50) == 0)
            {
                hp[i]--;
                vx[i] = Fixed.FromRatio(rng.NextInt(-8, 9), 8);
                vy[i] = Fixed.FromRatio(rng.NextInt(-8, 9), 8);
            }
        }
    }
}
