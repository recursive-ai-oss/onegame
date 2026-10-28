[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# PluginConfig record

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`PluginConfig`](PluginConfig.md)&gt;

```csharp
public sealed record PluginConfig : IEquatable<PluginConfig>
```

One plugin the server loads at start.

## Constructors

| Member | Description |
| --- | --- |
| [`PluginConfig(string, string, uint[], KeyValuePair<string, string>[])`](#m-ctor-string-string-uint-keyvaluepair-string-string) | One plugin the server loads at start. |

### <a id="m-ctor-string-string-uint-keyvaluepair-string-string"></a>PluginConfig(string, string, uint\[\], KeyValuePair&lt;string, string&gt;\[\])

```csharp
public PluginConfig(string Name, string Path, uint[] Scopes, KeyValuePair<string, string>[] Settings);
```

One plugin the server loads at start.

**Parameters**

- `Name` (`string`): The plugin's name; unique, and used by every plugin operation.
- `Path` (`string`): A folder holding `<name>.dll`, or the dll. Relative paths resolve against the configuration file.
- `Scopes` (`uint[]`): The scopes it is bound to. Empty means all of them.
- `Settings` ([`KeyValuePair`](https://learn.microsoft.com/dotnet/api/system.collections.generic.keyvaluepair-2)`<string, string>[]`): Free-form text settings the plugin reads through [`PluginContext.Setting()`](PluginContext.md#m-setting-string).

## Properties

| Member | Description |
| --- | --- |
| [`Name`](#m-name) | The plugin's name; unique, and used by every plugin operation. |
| [`Path`](#m-path) | A folder holding `<name>.dll`, or the dll. |
| [`Scopes`](#m-scopes) | The scopes it is bound to. |
| [`Settings`](#m-settings) | Free-form text settings the plugin reads through [`PluginContext.Setting()`](PluginContext.md#m-setting-string). |

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

The plugin's name; unique, and used by every plugin operation.

**Type:** `string`

### <a id="m-path"></a>Path

```csharp
public string Path { get; init; }
```

A folder holding `<name>.dll`, or the dll. Relative paths resolve against the configuration file.

**Type:** `string`

### <a id="m-scopes"></a>Scopes

```csharp
public uint[] Scopes { get; init; }
```

The scopes it is bound to. Empty means all of them.

**Type:** `uint[]`

### <a id="m-settings"></a>Settings

```csharp
public KeyValuePair<string, string>[] Settings { get; init; }
```

Free-form text settings the plugin reads through [`PluginContext.Setting()`](PluginContext.md#m-setting-string).

**Type:** [`KeyValuePair`](https://learn.microsoft.com/dotnet/api/system.collections.generic.keyvaluepair-2)`<string, string>[]`
