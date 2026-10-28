[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# InProcessPluginImage class

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)  
**Inherits:** [`PluginImage`](PluginImage.md)

```csharp
public sealed class InProcessPluginImage : PluginImage
```

An image for plugins already linked into the process (a game that bundles its own, or tests). It cannot be unloaded.

## Constructors

| Member | Description |
| --- | --- |
| [`InProcessPluginImage(ServerPlugin)`](#m-ctor-serverplugin) | An image for plugins already linked into the process (a game that bundles its own, or tests). |

### <a id="m-ctor-serverplugin"></a>InProcessPluginImage(ServerPlugin)

```csharp
public InProcessPluginImage(ServerPlugin plugin);
```

An image for plugins already linked into the process (a game that bundles its own, or tests). It cannot be unloaded.

**Parameters**

- `plugin` ([`ServerPlugin`](ServerPlugin.md)): The plugin object.

## Properties

| Member | Description |
| --- | --- |
| [`ApiVersion`](#m-apiversion) | The API version the plugin declared. |
| [`Build`](#m-build) | A short identity of the loaded build, such as the assembly version and module id. |
| [`Plugin`](#m-plugin) | The entry object. |

### <a id="m-apiversion"></a>ApiVersion

```csharp
public override int ApiVersion { get; }
```

The API version the plugin declared.

**Type:** `int`

### <a id="m-build"></a>Build

```csharp
public override string Build { get; }
```

A short identity of the loaded build, such as the assembly version and module id.

**Type:** `string`

### <a id="m-plugin"></a>Plugin

```csharp
public override ServerPlugin Plugin { get; }
```

The entry object.

**Type:** [`ServerPlugin`](ServerPlugin.md)

## Methods

| Member | Description |
| --- | --- |
| [`Owns(Exception)`](#m-owns-exception) | Whether any frame of `exception` belongs to this plugin's code. |
| [`Release()`](#m-release) | Starts unloading. |
| [`WaitUntilCollected()`](#m-waituntilcollected) | Forces collections until the plugin's load context is gone or the attempts run out, and reports whether it is. |

### <a id="m-owns-exception"></a>Owns(Exception)

```csharp
public override bool Owns(Exception exception);
```

Whether any frame of `exception` belongs to this plugin's code.

**Parameters**

- `exception` ([`Exception`](https://learn.microsoft.com/dotnet/api/system.exception)): The exception.

**Returns:** `bool`

### <a id="m-release"></a>Release()

```csharp
public override void Release();
```

Starts unloading. Afterwards the caller must drop every reference to the plugin and its types.

### <a id="m-waituntilcollected"></a>WaitUntilCollected()

```csharp
public override bool WaitUntilCollected();
```

Forces collections until the plugin's load context is gone or the attempts run out, and reports whether it is.

**Returns:** `bool`
