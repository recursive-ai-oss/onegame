using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace OneGame.Server;

/// <summary>
/// A headless game server: one node hosting one or more authority scopes on a fixed-step driver, with no window, GPU or audio.
/// It starts from a <see cref="ServerConfig"/>, loads plugins through an <see cref="IPluginLoader"/> (when the host has one), keeps
/// ticking when a plugin fails, and can load, hot-reload and unload plugins while it runs. Everything runs on the thread that calls
/// <see cref="StepOnce"/>, <see cref="Advance"/> or <see cref="Run"/>: plugin operations are plain calls between ticks.
/// </summary>
public sealed class ServerHost
{
    private readonly ServerSetup _setup;
    private readonly IPluginLoader? _loader;
    private readonly FixedStepDriver _driver;
    private readonly List<ServerScope> _scopes = [];
    private readonly List<PluginRecord> _plugins = [];
    private readonly List<PluginLoadFailure> _failures = [];
    private readonly List<EntityRegistry> _replicaRegistries = [];
    private readonly Dictionary<uint, Func<Entity>> _factories = [];
    private readonly Dictionary<uint, string> _typeOwners = [];
    private int _localQueued;

    /// <summary>Creates a server. Nothing runs until <see cref="Start"/>.</summary>
    /// <param name="config">The configuration.</param>
    /// <param name="setup">The game's own entity types and world setup, or null for a server that only runs plugins.</param>
    /// <param name="loader">Loads plugins. Null for a host that cannot (a NativeAOT build): configured plugins are then reported as failures.</param>
    /// <param name="log">The log; defaults to one that keeps entries and writes nowhere.</param>
    public ServerHost(ServerConfig config, ServerSetup? setup = null, IPluginLoader? loader = null, ServerLog? log = null)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        _setup = setup ?? new ServerSetup();
        _loader = loader;
        Log = log ?? new ServerLog();
        _driver = new FixedStepDriver(config.TickRate, config.MaxCatchUp);
        Node = new Node(config.Name);
    }

    /// <summary>The configuration.</summary>
    public ServerConfig Config { get; }

    /// <summary>The log.</summary>
    public ServerLog Log { get; }

    /// <summary>The node that is authority for every hosted scope. Clients connect to it with <see cref="Node.ConnectTo"/>.</summary>
    public Node Node { get; }

    /// <summary>Where the server is in its life.</summary>
    public ServerState State { get; private set; } = ServerState.Created;

    /// <summary>Ticks run so far.</summary>
    public long Tick { get; private set; }

    /// <summary>The fixed-step driver that turns elapsed time into ticks.</summary>
    public FixedStepDriver Driver => _driver;

    /// <summary>The hosted scopes in id order of declaration.</summary>
    public IReadOnlyList<ServerScope> Scopes => _scopes;

    /// <summary>Plugins that were configured but failed to load.</summary>
    public IReadOnlyList<PluginLoadFailure> LoadFailures => _failures;

    /// <summary>
    /// Delivers pending messages to clients that live in this process (tests). A plugin's load context cannot be collected while a local
    /// replica still holds one of its entities, so reload and unload call this before they wait for the collector. Remote clients need nothing.
    /// </summary>
    public Action? DeliverLocalMessages { get; set; }

    /// <summary>True once a stop was requested (the control surface's <c>shutdown</c>).</summary>
    public bool StopRequested { get; private set; }

    /// <summary>Asks <see cref="Run"/> to finish its current tick and stop.</summary>
    public void RequestStop() => StopRequested = true;

    /// <summary>The scope with this id, or null.</summary>
    /// <param name="id">The scope id.</param>
    public ServerScope? FindScope(ScopeId id) => _scopes.Find(s => s.Id == id);

    /// <summary>Builds the scopes and loads the configured plugins. A plugin that fails to load is logged and listed in <see cref="LoadFailures"/>; the server starts without it.</summary>
    /// <exception cref="InvalidOperationException">The server was already started.</exception>
    public void Start()
    {
        if (State != ServerState.Created)
        {
            throw new InvalidOperationException($"The server is {State}; it can only be started once.");
        }

        foreach (var sc in Config.Scopes)
        {
            var registry = new EntityRegistry();
            _setup.RegisterEntities?.Invoke(registry);
            var world = new World(sc.Seed, registry);
            _setup.BuildWorld?.Invoke(world, new ScopeId(sc.Id));
            var systems = new PluginSystems(OnSystemFault);
            world.AddSystem(systems);
            var id = new ScopeId(sc.Id);
            _scopes.Add(new ServerScope(id, Node.AddScope(id, world), registry, systems));
        }

        State = ServerState.Running;
        Log.Info($"server '{Config.Name}' started: {_scopes.Count} scope(s), {Config.TickRate} ticks/s");
        foreach (var spec in Config.Plugins)
        {
            try
            {
                LoadPlugin(spec);
            }
            catch (PluginException ex)
            {
                _failures.Add(new PluginLoadFailure(spec.Name, ex.Code, ex.Message));
                Log.Error($"plugin '{spec.Name}' was not loaded ({ex.Code}): {ex.Message}");
            }
        }
    }

    /// <summary>Runs one tick: delivers arrived messages, steps and publishes every scope. A plugin that throws is contained.</summary>
    /// <exception cref="InvalidOperationException">The server is not running.</exception>
    public void StepOnce()
    {
        if (State != ServerState.Running)
        {
            throw new InvalidOperationException($"The server is {State}; start it before stepping.");
        }

        Node.Pump();
        _localQueued = 0;
        foreach (var scope in _scopes)
        {
            try
            {
                scope.Authority.StepAndPublish();
            }
            catch (Exception ex) when (ex is not OutOfMemoryException && Blame(ex) is not null)
            {
                Quarantine(Blame(ex)!, "tick", ex);
            }
        }

        Tick++;
        Log.Tick = Tick;
    }

    /// <summary>Runs <paramref name="ticks"/> ticks.</summary>
    /// <param name="ticks">How many.</param>
    public void Step(int ticks)
    {
        for (var i = 0; i < ticks; i++)
        {
            StepOnce();
        }
    }

    /// <summary>Feeds elapsed time to the fixed-step driver and runs the ticks that are due. Returns how many ran.</summary>
    /// <param name="elapsed">Time since the previous call.</param>
    public int Advance(TimeSpan elapsed)
    {
        var steps = _driver.Advance(elapsed);
        Step(steps);
        return steps;
    }

    /// <summary>
    /// Runs in real time until <see cref="RequestStop"/> or <paramref name="keepRunning"/> returns false. <paramref name="between"/> runs once per
    /// loop and is where live control requests are served; it should wait briefly (a millisecond or two) when it has nothing to do.
    /// </summary>
    /// <param name="keepRunning">Checked every loop; null runs until a stop is requested.</param>
    /// <param name="between">Called between ticks, or null to sleep a millisecond.</param>
    public void Run(Func<bool>? keepRunning = null, Action? between = null)
    {
        // Real time is the host's business; the simulation only ever sees whole ticks.
        var clock = Stopwatch.StartNew();
        var last = clock.Elapsed;
        while (!StopRequested && (keepRunning?.Invoke() ?? true))
        {
            var now = clock.Elapsed;
            Advance(now - last);
            last = now;
            if (between is null)
            {
                Thread.Sleep(1);
            }
            else
            {
                between();
            }
        }
    }

    /// <summary>Stops ticking, gives every plugin its <see cref="ServerPlugin.Unload"/> call (errors are logged) and releases it. The scopes' state stays readable.</summary>
    public void Stop()
    {
        if (State is ServerState.Draining or ServerState.Stopped)
        {
            return;
        }

        State = ServerState.Draining;
        Log.Info("server draining");
        Node.Pump();
        for (var i = _plugins.Count - 1; i >= 0; i--)
        {
            var rec = _plugins[i];
            RunUnloadHook(rec);
            rec.Image?.Release();
            rec.Image = null;
        }

        State = ServerState.Stopped;
        Log.Info("server stopped");
    }

    /// <summary>Snapshots of every scope.</summary>
    public IReadOnlyList<ScopeInfo> ScopeInfos()
    {
        var list = new List<ScopeInfo>(_scopes.Count);
        foreach (var s in _scopes)
        {
            list.Add(new ScopeInfo(s.Id.Value, s.World.Tick, s.World.ComputeHash().ToString("x16", CultureInfo.InvariantCulture), s.World.Entities.Count, s.Authority.Subscribers.Count));
        }

        return list;
    }

    /// <summary>Snapshots of every loaded plugin, in load order.</summary>
    public IReadOnlyList<PluginInfo> Plugins()
    {
        var list = new List<PluginInfo>(_plugins.Count);
        foreach (var p in _plugins)
        {
            list.Add(p.Info());
        }

        return list;
    }

    /// <summary>
    /// Creates a replica world for a scope with the same entity types, tables and systems as the authority, for in-process clients and tests.
    /// Plugin types are created through the host, so the replica follows reloads.
    /// </summary>
    /// <param name="scope">The scope id.</param>
    /// <exception cref="PluginException">No such scope.</exception>
    public World CreateReplicaWorld(ScopeId scope)
    {
        if (FindScope(scope) is null)
        {
            throw new PluginException("not_found", $"There is no {scope}.");
        }

        var registry = new EntityRegistry();
        _setup.RegisterEntities?.Invoke(registry);
        foreach (var kv in _typeOwners)
        {
            AddIndirect(registry, kv.Key, kv.Value);
        }

        _replicaRegistries.Add(registry);
        var world = new World(FindScope(scope)!.World.Seed, registry, WorldRole.Replica);
        _setup.BuildWorld?.Invoke(world, scope);
        return world;
    }

    /// <summary>Sends a command to an entity from the host, as trusted input: it is queued for the next tick. Arguments are text, as in scenarios.</summary>
    /// <param name="scope">The scope.</param>
    /// <param name="entity">The entity id.</param>
    /// <param name="command">The command name.</param>
    /// <param name="args">The arguments as text.</param>
    /// <exception cref="PluginException">No such scope or entity.</exception>
    public CommandStatus SendCommand(ScopeId scope, EntityId entity, string command, params string[] args)
    {
        var s = FindScope(scope) ?? throw new PluginException("not_found", $"There is no {scope}.");
        var e = s.World.Find(entity) ?? throw new PluginException("not_found", $"{scope} has no entity {entity}.");
        var status = e.SendFromText(command, args);
        if (status == CommandStatus.Queued)
        {
            _localQueued++;
        }

        return status;
    }

    // ---- Plugins ----

    /// <summary>
    /// Loads a plugin. Errors while loading (missing files, API mismatch, an exception in <see cref="ServerPlugin.Load"/>, clashing type ids)
    /// refuse the plugin and change nothing. An exception while seeding quarantines the plugin: it stays listed as faulted.
    /// </summary>
    /// <param name="spec">The name, path, scopes and settings.</param>
    /// <returns>The loaded plugin.</returns>
    /// <exception cref="PluginException">The plugin was refused.</exception>
    public PluginInfo LoadPlugin(PluginConfig spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        RequireRunning();
        var loader = _loader ?? throw new PluginException("unsupported", "This server was built without plugin support. Use the plugin-capable server host (OneGame.Server.Plugins).");
        if (FindPlugin(spec.Name) is not null)
        {
            throw new PluginException("duplicate", $"A plugin named '{spec.Name}' is already loaded. Reload or unload it, or pick another name.");
        }

        var scopeIds = ResolveScopes(spec);
        var rec = new PluginRecord(spec.Name, spec.Path, scopeIds, spec.Settings);
        var image = loader.Load(spec.Name, spec.Path);
        var context = new PluginContext(spec.Name, false, ToScopeIds(scopeIds), spec.Settings, m => Log.Info($"[{spec.Name}] {m}"));
        try
        {
            image.Plugin.Load(context);
            CheckConflicts(rec, context, null);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            DiscardImage(image, spec.Name);
            throw Refusal(spec.Name, "load", ex);
        }

        rec.Image = image;
        rec.Entities = [.. context.Entities];
        rec.Systems = [.. context.Systems];
        Install(rec);
        _plugins.Add(rec);
        Log.Info($"plugin '{rec.Name}' loaded ({rec.Entities.Length} entity type(s), {rec.Systems.Length} system(s), build {image.Build})");

        foreach (var id in scopeIds)
        {
            var scope = FindScope(new ScopeId(id))!;
            try
            {
                rec.Image!.Plugin.Seed(new PluginScope(scope.Id, scope.World));
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                Quarantine(rec, "seed", ex);
                break;
            }
        }

        return rec.Info();
    }

    /// <summary>
    /// Replaces a running plugin with a new build of it. The new build loads first (a failure leaves the old one running). Then each bound
    /// scope is saved, the new build's types and systems are swapped in, and the save is restored through the entity types' migrations, so
    /// entity ids and state stay and replicas get a fresh snapshot. If the restore fails the old build is put back.
    /// </summary>
    /// <param name="name">The plugin's name.</param>
    /// <param name="path">A new location, or null to load again from where it was.</param>
    /// <exception cref="PluginException">The reload was refused; the old build is still running.</exception>
    public ReloadResult ReloadPlugin(string name, string? path = null)
    {
        RequireRunning();
        var rec = FindPlugin(name) ?? throw new PluginException("not_found", $"No plugin named '{name}' is loaded.");
        var (result, old) = SwapBuild(rec, path ?? rec.Path);

        // The swap method has returned, so none of its locals can keep the old build reachable while we wait for the collector.
        DeliverLocalMessages?.Invoke();
        var collected = old.WaitUntilCollected();
        Log.Info($"plugin '{name}' reloaded; old build {(collected ? "collected" : "still referenced")}");
        return result with { Collected = collected };
    }

    /// <summary>Removes a plugin: its entities leave the scopes, its systems stop, <see cref="ServerPlugin.Unload"/> runs, and its load context is released and collected.</summary>
    /// <param name="name">The plugin's name.</param>
    /// <exception cref="PluginException">No such plugin.</exception>
    public UnloadResult UnloadPlugin(string name)
    {
        RequireRunning();
        var rec = FindPlugin(name) ?? throw new PluginException("not_found", $"No plugin named '{name}' is loaded.");
        RequireNoQueuedCommands();
        var (removed, image) = Detach(rec);
        DeliverLocalMessages?.Invoke();
        var collected = image.WaitUntilCollected();
        Log.Info($"plugin '{name}' unloaded; {removed} entit(ies) removed, context {(collected ? "collected" : "still referenced")}");
        return new UnloadResult(name, removed, collected);
    }

    // Kept out of line and not inlined so that the old build's objects are dead by the time the caller waits for the collector.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private (ReloadResult, PluginImage) SwapBuild(PluginRecord rec, string path)
    {
        RequireNoQueuedCommands();
        var loader = _loader ?? throw new PluginException("unsupported", "This server was built without plugin support.");
        var newImage = loader.Load(rec.Name, path);
        var context = new PluginContext(rec.Name, true, ToScopeIds(rec.ScopeIds), rec.Settings, m => Log.Info($"[{rec.Name}] {m}"));
        try
        {
            newImage.Plugin.Load(context);
            CheckConflicts(rec, context, rec);
            CheckReloadable(rec, context);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            DiscardImage(newImage, rec.Name);
            throw Refusal(rec.Name, "reload", ex);
        }

        var bound = new List<ServerScope>();
        foreach (var id in rec.ScopeIds)
        {
            bound.Add(FindScope(new ScopeId(id))!);
        }

        var saves = new List<byte[]>(bound.Count);
        foreach (var s in bound)
        {
            saves.Add(s.Authority.Save());
        }

        var oldImage = rec.Image!;
        var oldEntities = rec.Entities;
        var oldSystems = rec.Systems;
        var oldBuild = oldImage.Build;
        var wasFaulted = rec.State == PluginState.Faulted;

        Uninstall(rec, keepTypes: true);
        rec.Image = newImage;
        rec.Entities = [.. context.Entities];
        rec.Systems = [.. context.Systems];
        Install(rec);

        var migrated = false;
        string? failure = null;
        for (var i = 0; i < bound.Count && failure is null; i++)
        {
            var r = bound[i].Authority.Load(saves[i]);
            migrated |= r.Migrated;
            if (r.Status != SaveStatus.Ok)
            {
                failure = $"{bound[i].Id}: {r.Status} ({r.Detail})";
            }
        }

        if (failure is not null)
        {
            // Put the old build back and restore the saved state into it.
            Uninstall(rec, keepTypes: true);
            rec.Image = oldImage;
            rec.Entities = oldEntities;
            rec.Systems = oldSystems;
            Install(rec);
            for (var i = 0; i < bound.Count; i++)
            {
                var back = bound[i].Authority.Load(saves[i]);
                if (back.Status != SaveStatus.Ok)
                {
                    Log.Error($"plugin '{rec.Name}': rolling back {bound[i].Id} failed ({back.Status}); the scope may be inconsistent");
                }
            }

            DiscardImage(newImage, rec.Name);
            throw new PluginException("reload_failed", $"The new build of '{rec.Name}' could not restore the saved state ({failure}). The previous build is running again. If the entity layout changed, add a [Migrate(n)] method for the old schema.");
        }

        rec.Reloads++;
        rec.State = PluginState.Loaded;
        rec.Fault = null;
        _ = wasFaulted;
        RunUnloadHookOf(oldImage, rec.Name);
        oldImage.Release();
        return (new ReloadResult(rec.Name, oldBuild, newImage.Build, rec.ScopeIds, migrated, false), oldImage);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private (int Removed, PluginImage Image) Detach(PluginRecord rec)
    {
        var removed = RemoveEntities(rec);
        Uninstall(rec, keepTypes: false);

        // Subscribers learn about the removal now, with a fresh snapshot, rather than with the next tick's frame.
        foreach (var id in rec.ScopeIds)
        {
            var scope = FindScope(new ScopeId(id))!;
            if (scope.Authority.Subscribers.Count > 0 && scope.Authority.ActiveRecorder is null)
            {
                scope.Authority.Load(scope.Authority.Save());
            }
        }

        var image = rec.Image!;
        RunUnloadHookOf(image, rec.Name);
        image.Release();
        rec.Image = null;
        _plugins.Remove(rec);
        return (removed, image);
    }

    private void RunUnloadHook(PluginRecord rec)
    {
        if (rec.Image is not null)
        {
            RunUnloadHookOf(rec.Image, rec.Name);
        }
    }

    private void RunUnloadHookOf(PluginImage image, string name)
    {
        try
        {
            image.Plugin.Unload();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Contained: the plugin is going away either way.
            Log.Error($"plugin '{name}' threw in Unload: {Describe(ex).Message}");
        }
    }

    private void DiscardImage(PluginImage image, string name)
    {
        RunUnloadHookOf(image, name);
        image.Release();
    }

    private PluginException Refusal(string name, string phase, Exception ex)
    {
        if (ex is PluginException pe)
        {
            Log.Error($"plugin '{name}' refused ({pe.Code}): {pe.Message}");
            return new PluginException(pe.Code, pe.Message);
        }

        var fault = Describe(ex);
        Log.Error($"plugin '{name}' failed in {phase}: {fault.Type}: {fault.Message}");
        var code = phase == "reload" ? "reload_failed" : "load_failed";
        return new PluginException(code, $"Plugin '{name}' threw while loading: {fault.Type}: {fault.Message}");
    }

    // ---- Registration ----

    private void Install(PluginRecord rec)
    {
        foreach (var e in rec.Entities)
        {
            _factories[e.TypeId] = e.Factory;
            if (!_typeOwners.ContainsKey(e.TypeId))
            {
                _typeOwners[e.TypeId] = e.TypeName;
                foreach (var s in _scopes)
                {
                    AddIndirect(s.Registry, e.TypeId, e.TypeName);
                }

                foreach (var r in _replicaRegistries)
                {
                    AddIndirect(r, e.TypeId, e.TypeName);
                }
            }
        }

        foreach (var id in rec.ScopeIds)
        {
            FindScope(new ScopeId(id))!.Systems.Replace(rec.Name, rec.Systems);
        }
    }

    private void Uninstall(PluginRecord rec, bool keepTypes)
    {
        foreach (var e in rec.Entities)
        {
            _factories.Remove(e.TypeId);
        }

        foreach (var id in rec.ScopeIds)
        {
            FindScope(new ScopeId(id))!.Systems.Replace(rec.Name, []);
        }

        if (!keepTypes)
        {
            // The registries keep the type id (they cannot forget one), but with no factory behind it a snapshot that names it is refused.
            rec.Entities = [];
            rec.Systems = [];
        }
    }

    private void AddIndirect(EntityRegistry registry, uint typeId, string name)
    {
        // The registry never holds a plugin type directly: it asks the host's table, which a reload replaces.
        registry.Add(typeId, name, () => _factories.TryGetValue(typeId, out var f) ? f() : null!);
    }

    private void CheckConflicts(PluginRecord rec, PluginContext context, PluginRecord? replacing)
    {
        var probe = new EntityRegistry();
        _setup.RegisterEntities?.Invoke(probe);
        foreach (var kv in _typeOwners)
        {
            if (replacing is not null && replacing.Entities.Any(e => e.TypeId == kv.Key))
            {
                continue;
            }

            probe.Add(kv.Key, kv.Value, static () => null!);
        }

        foreach (var e in context.Entities)
        {
            try
            {
                probe.Add(e.TypeId, e.TypeName, static () => null!);
            }
            catch (DuplicateTypeException ex)
            {
                throw new PluginException("conflict", ex.Message);
            }

            if (_typeOwners.TryGetValue(e.TypeId, out var existing) && existing != e.TypeName)
            {
                throw new PluginException("conflict", $"Entity type '{e.TypeName}' has the same type id as '{existing}'. Give one a different [Entity(Id = ...)].");
            }

            var owner = _plugins.Find(p => p != replacing && p.Entities.Any(x => x.TypeId == e.TypeId));
            if (owner is not null)
            {
                throw new PluginException("conflict", $"Entity type '{e.TypeName}' is already provided by plugin '{owner.Name}'.");
            }
        }

        _ = rec;
    }

    private void CheckReloadable(PluginRecord rec, PluginContext context)
    {
        foreach (var id in rec.ScopeIds)
        {
            var scope = FindScope(new ScopeId(id))!;
            if (scope.Authority.ActiveRecorder is not null)
            {
                throw new PluginException("conflict", $"{scope.Id} is being recorded; a reload would make the recording unreplayable. Stop the recording first.");
            }

            foreach (var entity in scope.World.Entities)
            {
                var owned = rec.Entities.Any(e => e.TypeId == entity.TypeId);
                if (owned && !context.Entities.Any(e => e.TypeId == entity.TypeId))
                {
                    throw new PluginException("conflict", $"The new build no longer registers entity type '{entity.TypeName}' but {scope.Id} still holds entities of it. Keep the type, or unload the plugin to remove them.");
                }
            }
        }
    }

    private int RemoveEntities(PluginRecord rec)
    {
        var removed = 0;
        foreach (var id in rec.ScopeIds)
        {
            var world = FindScope(new ScopeId(id))!.World;
            var victims = new List<EntityId>();
            foreach (var e in world.Entities)
            {
                if (rec.Entities.Any(t => t.TypeId == e.TypeId))
                {
                    victims.Add(e.Id);
                }
            }

            foreach (var v in victims)
            {
                if (world.Remove(v))
                {
                    removed++;
                }
            }
        }

        return removed;
    }

    // ---- Faults ----

    private PluginRecord? Blame(Exception ex)
    {
        foreach (var p in _plugins)
        {
            if (p.State == PluginState.Loaded && p.Image is not null && p.Image.Owns(ex))
            {
                return p;
            }
        }

        return null;
    }

    private void OnSystemFault(string plugin, Exception ex)
    {
        if (FindPlugin(plugin) is { } rec)
        {
            Quarantine(rec, "tick", ex);
        }
    }

    private void Quarantine(PluginRecord rec, string phase, Exception ex)
    {
        var fault = Describe(ex);
        rec.State = PluginState.Faulted;
        rec.Fault = fault with { Phase = phase };
        foreach (var id in rec.ScopeIds)
        {
            FindScope(new ScopeId(id))!.Systems.Stop(rec.Name);
        }

        var removed = RemoveEntities(rec);
        Log.Error($"plugin '{rec.Name}' threw in {phase} and was stopped ({fault.Type}: {fault.Message}); {removed} entit(ies) removed");
    }

    private PluginFault Describe(Exception ex)
    {
        var detail = new StringBuilder();
        var trace = ex.StackTrace;
        if (trace is not null)
        {
            var lines = 0;
            foreach (var line in trace.Split('\n'))
            {
                if (lines++ >= 8)
                {
                    break;
                }

                detail.Append(line.TrimEnd()).Append('\n');
            }
        }

        return new PluginFault("", ex.GetType().FullName ?? ex.GetType().Name, ex.Message, detail.ToString().TrimEnd(), Tick);
    }

    // ---- Helpers ----

    private PluginRecord? FindPlugin(string name) => _plugins.Find(p => p.Name == name);

    // A save and restore starts the scope from its saved state, which does not include commands waiting for the next tick.
    private void RequireNoQueuedCommands()
    {
        if (_localQueued > 0)
        {
            throw new PluginException("conflict", $"{_localQueued} command(s) sent through the host are queued for the next tick and would be lost. Step the server once first.");
        }
    }

    private void RequireRunning()
    {
        if (State != ServerState.Running)
        {
            throw new PluginException("unsupported", $"The server is {State}; plugins can only change while it runs.");
        }
    }

    private uint[] ResolveScopes(PluginConfig spec)
    {
        if (spec.Scopes.Length == 0)
        {
            return [.. _scopes.Select(s => s.Id.Value)];
        }

        foreach (var id in spec.Scopes)
        {
            if (FindScope(new ScopeId(id)) is null)
            {
                throw new PluginException("not_found", $"Plugin '{spec.Name}' is bound to scope {id}, which this server does not host.");
            }
        }

        return spec.Scopes;
    }

    private static ScopeId[] ToScopeIds(uint[] ids)
    {
        var result = new ScopeId[ids.Length];
        for (var i = 0; i < ids.Length; i++)
        {
            result[i] = new ScopeId(ids[i]);
        }

        return result;
    }

    private sealed class PluginRecord(string name, string path, uint[] scopeIds, KeyValuePair<string, string>[] settings)
    {
        public string Name { get; } = name;

        public string Path { get; } = path;

        public uint[] ScopeIds { get; } = scopeIds;

        public KeyValuePair<string, string>[] Settings { get; } = settings;

        public PluginImage? Image { get; set; }

        public StagedEntity[] Entities { get; set; } = [];

        public ISimSystem[] Systems { get; set; } = [];

        public PluginState State { get; set; }

        public PluginFault? Fault { get; set; }

        public int Reloads { get; set; }

        public PluginInfo Info() => new(Name, Path, Image?.Build ?? "", Image?.ApiVersion ?? 0, State, ScopeIds, [.. Entities.Select(e => e.TypeName)], Systems.Length, Reloads, Fault);
    }
}
