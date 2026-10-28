[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# ServerPlugin class

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)

```csharp
public abstract class ServerPlugin
```

A server plugin. The host creates one instance per load, calls [`ServerPlugin.Load()`](ServerPlugin.md#m-load-plugincontext) (every load and reload), [`ServerPlugin.Seed()`](ServerPlugin.md#m-seed-pluginscope) (first load only) and finally [`ServerPlugin.Unload()`](ServerPlugin.md#m-unload). State belongs in entities, so it survives a reload; keep nothing in static fields, threads, timers or event subscriptions, because the assembly can only be unloaded once nothing outside it refers to it.

**Example**

A plugin that adds an entity type to a running server. Build it as a class library (`OneGameKind` set to `Plugin` in the SDK), then point a server configuration's `plugins:` list at the dll; rebuild it and use the `plugin_reload` operation to hot-reload it.

```csharp
[assembly: ServerPlugin(typeof(Pulse.PulsePlugin))]

namespace Pulse;

public sealed class PulsePlugin : ServerPlugin
{
    public override void Load(PluginContext context)
    {
        context.AddEntity<Counter>();
        context.Log("step is " + context.SettingInt("step", 1));
    }

    public override void Seed(PluginScope scope) => scope.World.Add(new Counter());
}
```

## Constructors

| Member | Description |
| --- | --- |
| [`ServerPlugin()`](#m-ctor) |  |

### <a id="m-ctor"></a>ServerPlugin()

```csharp
protected ServerPlugin();
```

## Methods

| Member | Description |
| --- | --- |
| [`Load(PluginContext)`](#m-load-plugincontext) | Registers the plugin's entity types and systems and reads its settings. |
| [`Seed(PluginScope)`](#m-seed-pluginscope) | Creates the plugin's initial entities in one scope. |
| [`Unload()`](#m-unload) | Releases anything held outside the world. |

### <a id="m-load-plugincontext"></a>Load(PluginContext)

```csharp
public abstract void Load(PluginContext context);
```

Registers the plugin's entity types and systems and reads its settings. Do not touch scopes here: this also runs on reload, and a failure here leaves the world untouched.

**Parameters**

- `context` ([`PluginContext`](PluginContext.md)): The registration surface.

### <a id="m-seed-pluginscope"></a>Seed(PluginScope)

```csharp
public virtual void Seed(PluginScope scope);
```

Creates the plugin's initial entities in one scope. Called once per bound scope on the first load only; a reload keeps the existing state.

**Parameters**

- `scope` ([`PluginScope`](PluginScope.md)): The scope and its authority world.

### <a id="m-unload"></a>Unload()

```csharp
public virtual void Unload();
```

Releases anything held outside the world. Called before the plugin is unloaded or replaced; an exception here is reported and does not stop the unload.
