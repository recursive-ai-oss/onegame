[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# PluginException class

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)  
**Inherits:** [`Exception`](https://learn.microsoft.com/dotnet/api/system.exception)

```csharp
public class PluginException : Exception
```

A plugin could not be loaded, reloaded or unloaded, or a setting was wrong. [`PluginException.Code`](PluginException.md#m-code) is stable and the message says what to change.

## Constructors

| Member | Description |
| --- | --- |
| [`PluginException(string, string)`](#m-ctor-string-string) | Creates the exception. |
| [`PluginException(string, string, Exception)`](#m-ctor-string-string-exception) | Creates the exception. |

### <a id="m-ctor-string-string"></a>PluginException(string, string)

```csharp
public PluginException(string code, string message);
```

Creates the exception.

**Parameters**

- `code` (`string`)
- `message` (`string`)

### <a id="m-ctor-string-string-exception"></a>PluginException(string, string, Exception)

```csharp
public PluginException(string code, string message, Exception inner);
```

Creates the exception.

**Parameters**

- `code` (`string`)
- `message` (`string`)
- `inner` ([`Exception`](https://learn.microsoft.com/dotnet/api/system.exception))

## Properties

| Member | Description |
| --- | --- |
| [`Code`](#m-code) | A short stable identifier: `api_mismatch`, `no_entry`, `not_found`, `duplicate`, `load_failed`, `conflict`, `reload_failed`, `bad_setting`, `unsupported`. |

### <a id="m-code"></a>Code

```csharp
public string Code { get; }
```

A short stable identifier: `api_mismatch`, `no_entry`, `not_found`, `duplicate`, `load_failed`, `conflict`, `reload_failed`, `bad_setting`, `unsupported`.

**Type:** `string`
