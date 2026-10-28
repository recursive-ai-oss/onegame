[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# PluginLoadFailure record

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`PluginLoadFailure`](PluginLoadFailure.md)&gt;

```csharp
public sealed record PluginLoadFailure : IEquatable<PluginLoadFailure>
```

A plugin that was configured but could not be loaded.

## Constructors

| Member | Description |
| --- | --- |
| [`PluginLoadFailure(string, string, string)`](#m-ctor-string-string-string) | A plugin that was configured but could not be loaded. |

### <a id="m-ctor-string-string-string"></a>PluginLoadFailure(string, string, string)

```csharp
public PluginLoadFailure(string Name, string Code, string Message);
```

A plugin that was configured but could not be loaded.

**Parameters**

- `Name` (`string`): The plugin's name.
- `Code` (`string`): The [`PluginException.Code`](PluginException.md#m-code).
- `Message` (`string`): What to change.

## Properties

| Member | Description |
| --- | --- |
| [`Code`](#m-code) | The [`PluginException.Code`](PluginException.md#m-code). |
| [`Message`](#m-message) | What to change. |
| [`Name`](#m-name) | The plugin's name. |

### <a id="m-code"></a>Code

```csharp
public string Code { get; init; }
```

The [`PluginException.Code`](PluginException.md#m-code).

**Type:** `string`

### <a id="m-message"></a>Message

```csharp
public string Message { get; init; }
```

What to change.

**Type:** `string`

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

The plugin's name.

**Type:** `string`
