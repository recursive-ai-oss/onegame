namespace OneGame.Server;

/// <summary>Where a server is in its life.</summary>
public enum ServerState
{
    /// <summary>Built, not started.</summary>
    Created,

    /// <summary>Started and ticking.</summary>
    Running,

    /// <summary>Stopping: no more ticks, plugins being released.</summary>
    Draining,

    /// <summary>Stopped.</summary>
    Stopped,
}

/// <summary>Whether a loaded plugin is running.</summary>
public enum PluginState
{
    /// <summary>Running normally.</summary>
    Loaded,

    /// <summary>It threw while ticking or seeding. Its entities were removed and its systems stopped; it stays loaded so it can be inspected, reloaded or unloaded.</summary>
    Faulted,
}

/// <summary>What went wrong inside a plugin. Text only, so a record never keeps a plugin's types alive.</summary>
/// <param name="Phase"><c>load</c>, <c>seed</c>, <c>tick</c> (also commands and entity logic), <c>unload</c> or <c>reload</c>.</param>
/// <param name="Type">The exception type name.</param>
/// <param name="Message">The exception message.</param>
/// <param name="Detail">The first lines of the stack trace.</param>
/// <param name="Tick">The server tick when it happened.</param>
public sealed record PluginFault(string Phase, string Type, string Message, string Detail, long Tick);

/// <summary>A snapshot of one loaded plugin.</summary>
/// <param name="Name">The plugin's name.</param>
/// <param name="Path">Where it was loaded from.</param>
/// <param name="Build">The loaded build's identity (assembly version and module id).</param>
/// <param name="ApiVersion">The API version it declared.</param>
/// <param name="State">Running or faulted.</param>
/// <param name="Scopes">The scope ids it is bound to.</param>
/// <param name="EntityTypes">The entity type names it registered.</param>
/// <param name="Systems">How many systems it registered.</param>
/// <param name="Reloads">How many times it was hot-reloaded.</param>
/// <param name="Fault">The last fault, or null.</param>
public sealed record PluginInfo(string Name, string Path, string Build, int ApiVersion, PluginState State, uint[] Scopes, string[] EntityTypes, int Systems, int Reloads, PluginFault? Fault);

/// <summary>A plugin that was configured but could not be loaded.</summary>
/// <param name="Name">The plugin's name.</param>
/// <param name="Code">The <see cref="PluginException.Code"/>.</param>
/// <param name="Message">What to change.</param>
public sealed record PluginLoadFailure(string Name, string Code, string Message);

/// <summary>The result of a hot reload.</summary>
/// <param name="Name">The plugin's name.</param>
/// <param name="OldBuild">The build that was replaced.</param>
/// <param name="NewBuild">The build now running.</param>
/// <param name="Scopes">The scopes whose state went through a save and restore.</param>
/// <param name="Migrated">True when a stored entity went through a schema migration.</param>
/// <param name="Collected">True when the replaced build's load context was garbage collected.</param>
public sealed record ReloadResult(string Name, string OldBuild, string NewBuild, uint[] Scopes, bool Migrated, bool Collected);

/// <summary>The result of an unload.</summary>
/// <param name="Name">The plugin's name.</param>
/// <param name="EntitiesRemoved">How many of its entities were removed from the scopes.</param>
/// <param name="Collected">True when its load context was garbage collected.</param>
public sealed record UnloadResult(string Name, int EntitiesRemoved, bool Collected);

/// <summary>A snapshot of one scope.</summary>
/// <param name="Id">The scope id.</param>
/// <param name="Tick">The scope's world tick.</param>
/// <param name="Hash">The world hash, as 16 hex digits.</param>
/// <param name="Entities">The entity count.</param>
/// <param name="Subscribers">How many connections are subscribed.</param>
public sealed record ScopeInfo(uint Id, long Tick, string Hash, int Entities, int Subscribers);

/// <summary>What a game adds to a server that has no plugins, or in addition to them.</summary>
public sealed class ServerSetup
{
    /// <summary>Registers the game's own entity types in a scope's registry, usually <c>Generated.EntityTypes.Register</c>.</summary>
    public Action<EntityRegistry>? RegisterEntities { get; init; }

    /// <summary>Called for every authority world the server creates, and for every replica world it creates for tests, so tables and systems are the same on both.</summary>
    public Action<World, ScopeId>? BuildWorld { get; init; }
}

/// <summary>One hosted scope.</summary>
public sealed class ServerScope
{
    internal ServerScope(ScopeId id, AuthorityScope authority, EntityRegistry registry, PluginSystems systems)
    {
        Id = id;
        Authority = authority;
        Registry = registry;
        Systems = systems;
    }

    /// <summary>The scope id.</summary>
    public ScopeId Id { get; }

    /// <summary>The authority half: world, subscribers, outcomes.</summary>
    public AuthorityScope Authority { get; }

    /// <summary>The authority world.</summary>
    public World World => Authority.World;

    internal EntityRegistry Registry { get; }

    internal PluginSystems Systems { get; }
}

/// <summary>
/// Runs a plugin's systems inside a scope's world. The world's system list is append-only, so the host adds this one adapter and swaps
/// the systems behind it when a plugin is reloaded; the position in the tick order stays the same.
/// </summary>
internal sealed class PluginSystems : ISimSystem
{
    private readonly List<Slot> _slots = [];
    private readonly Action<string, Exception> _onFault;

    public PluginSystems(Action<string, Exception> onFault)
    {
        _onFault = onFault;
    }

    public void Tick(World world)
    {
        // Index loop: a fault nulls the slot instead of removing it, so the walk is never disturbed.
        for (var i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (slot.System is null)
            {
                continue;
            }

            try
            {
                slot.System.Tick(world);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                _onFault(slot.Plugin, ex);
            }
        }
    }

    public void Replace(string plugin, IReadOnlyList<ISimSystem> systems)
    {
        var first = _slots.FindIndex(s => s.Plugin == plugin);
        _slots.RemoveAll(s => s.Plugin == plugin);
        var at = first < 0 ? _slots.Count : first;
        foreach (var s in systems)
        {
            _slots.Insert(at++, new Slot(plugin, s));
        }
    }

    public void Stop(string plugin)
    {
        for (var i = 0; i < _slots.Count; i++)
        {
            if (_slots[i].Plugin == plugin)
            {
                _slots[i] = new Slot(plugin, null);
            }
        }
    }

    private readonly record struct Slot(string Plugin, ISimSystem? System);
}
