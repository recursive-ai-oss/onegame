[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# PluginInfo record

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`PluginInfo`](PluginInfo.md)&gt;

```csharp
public sealed record PluginInfo : IEquatable<PluginInfo>
```

A snapshot of one loaded plugin.

## Constructors

| Member | Description |
| --- | --- |
| [`PluginInfo(string, string, string, int, PluginState, uint[], string[], int, int, PluginFault?)`](#m-ctor-string-string-string-int-pluginstate-uint-string-int-int-pluginfault) | A snapshot of one loaded plugin. |

### <a id="m-ctor-string-string-string-int-pluginstate-uint-string-int-int-pluginfault"></a>PluginInfo(string, string, string, int, PluginState, uint\[\], string\[\], int, int, PluginFault?)

```csharp
public PluginInfo(string Name, string Path, string Build, int ApiVersion, PluginState State, uint[] Scopes, string[] EntityTypes, int Systems, int Reloads, PluginFault? Fault);
```

A snapshot of one loaded plugin.

**Parameters**

- `Name` (`string`): The plugin's name.
- `Path` (`string`): Where it was loaded from.
- `Build` (`string`): The loaded build's identity (assembly version and module id).
- `ApiVersion` (`int`): The API version it declared.
- `State` ([`PluginState`](PluginState.md)): Running or faulted.
- `Scopes` (`uint[]`): The scope ids it is bound to.
- `EntityTypes` (`string[]`): The entity type names it registered.
- `Systems` (`int`): How many systems it registered.
- `Reloads` (`int`): How many times it was hot-reloaded.
- `Fault` ([`PluginFault`](PluginFault.md)?): The last fault, or null.

## Properties

| Member | Description |
| --- | --- |
| [`ApiVersion`](#m-apiversion) | The API version it declared. |
| [`Build`](#m-build) | The loaded build's identity (assembly version and module id). |
| [`EntityTypes`](#m-entitytypes) | The entity type names it registered. |
| [`Fault`](#m-fault) | The last fault, or null. |
| [`Name`](#m-name) | The plugin's name. |
| [`Path`](#m-path) | Where it was loaded from. |
| [`Reloads`](#m-reloads) | How many times it was hot-reloaded. |
| [`Scopes`](#m-scopes) | The scope ids it is bound to. |
| [`State`](#m-state) | Running or faulted. |
| [`Systems`](#m-systems) | How many systems it registered. |

### <a id="m-apiversion"></a>ApiVersion

```csharp
public int ApiVersion { get; init; }
```

The API version it declared.

**Type:** `int`

### <a id="m-build"></a>Build

```csharp
public string Build { get; init; }
```

The loaded build's identity (assembly version and module id).

**Type:** `string`

### <a id="m-entitytypes"></a>EntityTypes

```csharp
public string[] EntityTypes { get; init; }
```

The entity type names it registered.

**Type:** `string[]`

### <a id="m-fault"></a>Fault

```csharp
public PluginFault? Fault { get; init; }
```

The last fault, or null.

**Type:** [`PluginFault`](PluginFault.md)?

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

The plugin's name.

**Type:** `string`

### <a id="m-path"></a>Path

```csharp
public string Path { get; init; }
```

Where it was loaded from.

**Type:** `string`

### <a id="m-reloads"></a>Reloads

```csharp
public int Reloads { get; init; }
```

How many times it was hot-reloaded.

**Type:** `int`

### <a id="m-scopes"></a>Scopes

```csharp
public uint[] Scopes { get; init; }
```

The scope ids it is bound to.

**Type:** `uint[]`

### <a id="m-state"></a>State

```csharp
public PluginState State { get; init; }
```

Running or faulted.

**Type:** [`PluginState`](PluginState.md)

### <a id="m-systems"></a>Systems

```csharp
public int Systems { get; init; }
```

How many systems it registered.

**Type:** `int`
