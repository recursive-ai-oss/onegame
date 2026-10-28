[API reference](../../index.md) › [OneGame.Server.Plugins](../index.md) › OneGame.Server.Plugins

# PluginLoader class

**Namespace:** `OneGame.Server.Plugins`  
**Package:** [`RecursiveAI.OneGame.Server.Plugins`](../index.md)  
**Implements:** [`IPluginLoader`](../../OneGame.Server/OneGame.Server/IPluginLoader.md)

```csharp
public sealed class PluginLoader : IPluginLoader
```

Loads server plugins into collectible [`AssemblyLoadContext`](https://learn.microsoft.com/dotnet/api/system.runtime.loader.assemblyloadcontext)s. The plugin's dll is read into memory first, so the file on disk is free to be rebuilt while the old copy runs (a hot reload loads the new bytes). Engine assemblies (`OneGame.*`) and the framework are shared with the host, so a plugin's entity types are the same `Entity` the host's worlds work with; everything else is loaded from the plugin's own folder.

This is the only assembly that uses runtime reflection; it is CoreCLR-only and is never part of a NativeAOT publish.

## Constructors

| Member | Description |
| --- | --- |
| [`PluginLoader()`](#m-ctor) |  |

### <a id="m-ctor"></a>PluginLoader()

```csharp
public PluginLoader();
```

## Methods

| Member | Description |
| --- | --- |
| [`Load(string, string)`](#m-load-string-string) | Loads the plugin at `path` (a folder holding `<name>.dll`, or the dll itself) and creates its entry object. |

### <a id="m-load-string-string"></a>Load(string, string)

```csharp
public PluginImage Load(string name, string path);
```

Loads the plugin at `path` (a folder holding `<name>.dll`, or the dll itself) and creates its entry object. No plugin code runs before the API version is checked.

**Parameters**

- `name` (`string`): The configured plugin name.
- `path` (`string`): The folder or dll.

**Returns:** [`PluginImage`](../../OneGame.Server/OneGame.Server/PluginImage.md)

**Exceptions**

- [`PluginException`](../../OneGame.Server/OneGame.Server/PluginException.md): The files are missing, the API version is refused, or there is no entry class.
