[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# ServerSetup class

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)

```csharp
public sealed class ServerSetup
```

What a game adds to a server that has no plugins, or in addition to them.

## Constructors

| Member | Description |
| --- | --- |
| [`ServerSetup()`](#m-ctor) |  |

### <a id="m-ctor"></a>ServerSetup()

```csharp
public ServerSetup();
```

## Properties

| Member | Description |
| --- | --- |
| [`AfterTick`](#m-aftertick) | Called after every tick, between ticks, so it may start entity transfers, save scopes or drive escrow. |
| [`BeforeTick`](#m-beforetick) | Called at the start of every tick, before arrived messages are handled. |
| [`BuildWorld`](#m-buildworld) | Called for every authority world the server creates, and for every replica world it creates for tests, so tables and systems are the same on both. |
| [`Pump`](#m-pump) | Called whenever the host moves network messages (every loop of [`ServerHost.Run()`](ServerHost.md#m-run-func-bool-action), and between ticks), so connections the game opened keep acknowledging. |
| [`RegisterEntities`](#m-registerentities) | Registers the game's own entity types in a scope's registry, usually `Generated.EntityTypes.Register`. |
| [`SeedWorld`](#m-seedworld) | Creates the starting entities of a hosted scope. |
| [`Started`](#m-started) | Called once at the end of [`ServerHost.Start()`](ServerHost.md#m-start), with the scopes built and the listeners open. |

### <a id="m-aftertick"></a>AfterTick

```csharp
public Action<ServerHost>? AfterTick { get; init; }
```

Called after every tick, between ticks, so it may start entity transfers, save scopes or drive escrow.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`ServerHost`](ServerHost.md)&gt;?

### <a id="m-beforetick"></a>BeforeTick

```csharp
public Action<ServerHost>? BeforeTick { get; init; }
```

Called at the start of every tick, before arrived messages are handled. Move the transports of the server's own outgoing connections here.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`ServerHost`](ServerHost.md)&gt;?

### <a id="m-buildworld"></a>BuildWorld

```csharp
public Action<World, ScopeId>? BuildWorld { get; init; }
```

Called for every authority world the server creates, and for every replica world it creates for tests, so tables and systems are the same on both.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-2)&lt;[`World`](../../OneGame.Core/OneGame.Core/World.md), [`ScopeId`](../../OneGame.Sync/OneGame.Sync/ScopeId.md)&gt;?

### <a id="m-pump"></a>Pump

```csharp
public Action<ServerHost>? Pump { get; init; }
```

Called whenever the host moves network messages (every loop of [`ServerHost.Run()`](ServerHost.md#m-run-func-bool-action), and between ticks), so connections the game opened keep acknowledging.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`ServerHost`](ServerHost.md)&gt;?

### <a id="m-registerentities"></a>RegisterEntities

```csharp
public Action<EntityRegistry>? RegisterEntities { get; init; }
```

Registers the game's own entity types in a scope's registry, usually `Generated.EntityTypes.Register`.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`EntityRegistry`](../../OneGame.Core/OneGame.Core/EntityRegistry.md)&gt;?

### <a id="m-seedworld"></a>SeedWorld

```csharp
public Action<World, ScopeId>? SeedWorld { get; init; }
```

Creates the starting entities of a hosted scope. Called once per authority world, after [`ServerSetup.BuildWorld`](ServerSetup.md#m-buildworld), and never for replica worlds (which fill in from the server's snapshot).

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-2)&lt;[`World`](../../OneGame.Core/OneGame.Core/World.md), [`ScopeId`](../../OneGame.Sync/OneGame.Sync/ScopeId.md)&gt;?

### <a id="m-started"></a>Started

```csharp
public Action<ServerHost>? Started { get; init; }
```

Called once at the end of [`ServerHost.Start()`](ServerHost.md#m-start), with the scopes built and the listeners open. Connect to other servers here, or restore saves.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`ServerHost`](ServerHost.md)&gt;?
