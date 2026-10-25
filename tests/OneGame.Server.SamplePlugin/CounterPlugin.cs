using System;
using OneGame.Core;
using OneGame.Server;

[assembly: ServerPlugin(typeof(Sample.CounterPlugin))]

namespace Sample;

/// <summary>A counter. The system adds the plugin's step to every counter each tick; the Add command adds an amount.</summary>
#if VARIANT3
[Entity(Schema = 2)]
#else
[Entity]
#endif
public partial class Counter : Entity
{
    [Synced] public partial int Value { get; set; }

    [Synced] public partial int Ticks { get; set; }

#if VARIANT3
    [Synced] public partial string Label { get; set; }
#endif

    [Command]
    public void Add(int amount) => Value += amount;

    [Command]
    public void Boom()
    {
        Value++;
        throw new InvalidOperationException("command exploded");
    }

    // Per-entity logic runs before the systems.
    protected override void OnTick()
    {
        Ticks++;
        if (CounterPlugin.FaultMode == "entity" && World!.Tick >= CounterPlugin.FaultTick)
        {
            throw new InvalidOperationException("entity logic exploded");
        }
    }

#if VARIANT3
    /// <summary>Schema 1 stored Value then Ticks.</summary>
    [Migrate(1)]
    private static void FromV1(ref SyncReader old, Counter counter)
    {
        counter.Value = old.ReadInt32();
        counter.Ticks = old.ReadInt32();
        counter.Label = "migrated";
    }
#endif
}

/// <summary>Adds the step (twice, in variant 2) to every counter each tick.</summary>
public sealed class CounterSystem(int step, string faultMode, long faultTick) : ISimSystem
{
    /// <inheritdoc />
    public void Tick(World world)
    {
        if (faultMode == "tick" && world.Tick >= faultTick)
        {
            throw new InvalidOperationException("system exploded");
        }

        foreach (var e in world.Entities)
        {
            if (e is Counter c)
            {
#if VARIANT2
                c.Value += step * 2;
#else
                c.Value += step;
#endif
            }
        }
    }
}

/// <summary>The entry point. Settings: <c>step</c> (default 1) and <c>fault</c> (<c>load</c>, <c>seed</c>, <c>tick</c>, <c>entity</c>, <c>unload</c>) with <c>faultTick</c>, to exercise error handling.</summary>
public sealed class CounterPlugin : ServerPlugin
{
    // Read by entity logic, which cannot see the context. Written only while the plugin loads.
    internal static string FaultMode = "";
    internal static long FaultTick;

    private string _fault = "";

    /// <inheritdoc />
    public override void Load(PluginContext context)
    {
        _fault = context.Setting("fault") ?? "";
        FaultMode = _fault;
        FaultTick = context.SettingInt("faultTick", 0);
        if (_fault == "load")
        {
            throw new InvalidOperationException("load exploded");
        }

        context.AddEntity<Counter>();
        context.AddSystem(new CounterSystem(context.SettingInt("step", 1), _fault, FaultTick));
        context.Log($"counter plugin ready (step {context.SettingInt("step", 1)})");
    }

    /// <inheritdoc />
    public override void Seed(PluginScope scope)
    {
        if (_fault == "seed")
        {
            throw new InvalidOperationException("seed exploded");
        }

        scope.World.Add(new Counter());
    }

    /// <inheritdoc />
    public override void Unload()
    {
        if (_fault == "unload")
        {
            throw new InvalidOperationException("unload exploded");
        }
    }
}
