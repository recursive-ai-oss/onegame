[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# PluginScope record

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`PluginScope`](PluginScope.md)&gt;

```csharp
public sealed record PluginScope : IEquatable<PluginScope>
```

One scope a plugin is bound to.

## Constructors

| Member | Description |
| --- | --- |
| [`PluginScope(ScopeId, World)`](#m-ctor-scopeid-world) | One scope a plugin is bound to. |

### <a id="m-ctor-scopeid-world"></a>PluginScope(ScopeId, World)

```csharp
public PluginScope(ScopeId Id, World World);
```

One scope a plugin is bound to.

**Parameters**

- `Id` ([`ScopeId`](../../OneGame.Sync/OneGame.Sync/ScopeId.md)): The scope id.
- `World` ([`World`](../../OneGame.Core/OneGame.Core/World.md)): The authority world. Only touch it from [`ServerPlugin.Seed()`](ServerPlugin.md#m-seed-pluginscope) and from sim code the host calls.

## Properties

| Member | Description |
| --- | --- |
| [`Id`](#m-id) | The scope id. |
| [`World`](#m-world) | The authority world. |

### <a id="m-id"></a>Id

```csharp
public ScopeId Id { get; init; }
```

The scope id.

**Type:** [`ScopeId`](../../OneGame.Sync/OneGame.Sync/ScopeId.md)

### <a id="m-world"></a>World

```csharp
public World World { get; init; }
```

The authority world. Only touch it from [`ServerPlugin.Seed()`](ServerPlugin.md#m-seed-pluginscope) and from sim code the host calls.

**Type:** [`World`](../../OneGame.Core/OneGame.Core/World.md)
