[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# PluginImage class

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)

```csharp
public abstract class PluginImage
```

One loaded copy of a plugin: the entry object plus the means to find which exceptions came from it and to release it.

## Constructors

| Member | Description |
| --- | --- |
| [`PluginImage()`](#m-ctor) |  |

### <a id="m-ctor"></a>PluginImage()

```csharp
protected PluginImage();
```

## Properties

| Member | Description |
| --- | --- |
| [`ApiVersion`](#m-apiversion) | The API version the plugin declared. |
| [`Build`](#m-build) | A short identity of the loaded build, such as the assembly version and module id. |
| [`Plugin`](#m-plugin) | The entry object. |

### <a id="m-apiversion"></a>ApiVersion

```csharp
public abstract int ApiVersion { get; }
```

The API version the plugin declared.

**Type:** `int`

### <a id="m-build"></a>Build

```csharp
public abstract string Build { get; }
```

A short identity of the loaded build, such as the assembly version and module id.

**Type:** `string`

### <a id="m-plugin"></a>Plugin

```csharp
public abstract ServerPlugin Plugin { get; }
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
public abstract bool Owns(Exception exception);
```

Whether any frame of `exception` belongs to this plugin's code.

**Parameters**

- `exception` ([`Exception`](https://learn.microsoft.com/dotnet/api/system.exception)): The exception.

**Returns:** `bool`

### <a id="m-release"></a>Release()

```csharp
public abstract void Release();
```

Starts unloading. Afterwards the caller must drop every reference to the plugin and its types.

### <a id="m-waituntilcollected"></a>WaitUntilCollected()

```csharp
public abstract bool WaitUntilCollected();
```

Forces collections until the plugin's load context is gone or the attempts run out, and reports whether it is.

**Returns:** `bool`
