[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# IPluginLoader interface

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)

```csharp
public interface IPluginLoader
```

Loads plugin assemblies. The CoreCLR implementation lives in `OneGame.Server.Plugins`; a host built without it cannot load plugins.

## Methods

| Member | Description |
| --- | --- |
| [`Load(string, string)`](#m-load-string-string) | Loads the plugin at `path` (a folder holding `<name>.dll`, or the dll itself) and creates its entry object. |

### <a id="m-load-string-string"></a>Load(string, string)

```csharp
PluginImage Load(string name, string path);
```

Loads the plugin at `path` (a folder holding `<name>.dll`, or the dll itself) and creates its entry object. No plugin code runs before the API version is checked.

**Parameters**

- `name` (`string`): The configured plugin name.
- `path` (`string`): The folder or dll.

**Returns:** [`PluginImage`](PluginImage.md)

**Exceptions**

- [`PluginException`](PluginException.md): The files are missing, the API version is refused, or there is no entry class.
