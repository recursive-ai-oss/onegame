[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# ServerPluginAttribute attribute

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)  
**Inherits:** [`Attribute`](https://learn.microsoft.com/dotnet/api/system.attribute)  
**Applies to:** `Assembly`

```csharp
public sealed class ServerPluginAttribute : Attribute
```

Marks a plugin assembly and names its entry class. Put `[assembly: ServerPlugin(typeof(MyPlugin))]` in the plugin project. The API version is a default argument, so the compiler writes the version the plugin was built against into the assembly and the host can check it before any plugin code runs.

## Constructors

| Member | Description |
| --- | --- |
| [`ServerPluginAttribute(Type, int)`](#m-ctor-type-int) | Marks a plugin assembly and names its entry class. |

### <a id="m-ctor-type-int"></a>ServerPluginAttribute(Type, int)

```csharp
public ServerPluginAttribute(Type entryType, int apiVersion = 1);
```

Marks a plugin assembly and names its entry class. Put `[assembly: ServerPlugin(typeof(MyPlugin))]` in the plugin project. The API version is a default argument, so the compiler writes the version the plugin was built against into the assembly and the host can check it before any plugin code runs.

**Parameters**

- `entryType` ([`Type`](https://learn.microsoft.com/dotnet/api/system.type)): The [`ServerPlugin`](ServerPlugin.md) subclass to instantiate. It needs a public parameterless constructor.
- `apiVersion` (`int`): The plugin API version the plugin was built against. Leave the default.

## Properties

| Member | Description |
| --- | --- |
| [`ApiVersion`](#m-apiversion) | The API version the plugin was built against. |
| [`EntryType`](#m-entrytype) | The entry class. |

### <a id="m-apiversion"></a>ApiVersion

```csharp
public int ApiVersion { get; }
```

The API version the plugin was built against.

**Type:** `int`

### <a id="m-entrytype"></a>EntryType

```csharp
public Type EntryType { get; }
```

The entry class.

**Type:** [`Type`](https://learn.microsoft.com/dotnet/api/system.type)
