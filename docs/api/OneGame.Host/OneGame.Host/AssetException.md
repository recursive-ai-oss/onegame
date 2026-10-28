[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# AssetException class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Inherits:** [`Exception`](https://learn.microsoft.com/dotnet/api/system.exception)

```csharp
public sealed class AssetException : Exception
```

A file could not be loaded or reloaded.

## Constructors

| Member | Description |
| --- | --- |
| [`AssetException(string, string, string, Exception?)`](#m-ctor-string-string-string-exception) | Creates the exception. |

### <a id="m-ctor-string-string-string-exception"></a>AssetException(string, string, string, Exception?)

```csharp
public AssetException(string code, string path, string message, Exception? inner = null);
```

Creates the exception.

**Parameters**

- `code` (`string`): `not_found`, `bad_path`, `too_large`, `bad_format`, `no_renderer` or `no_audio`.
- `path` (`string`): The asset path.
- `message` (`string`): What is wrong, with the fix where there is one.
- `inner` ([`Exception`](https://learn.microsoft.com/dotnet/api/system.exception)?): The decoder's exception, if any.

## Properties

| Member | Description |
| --- | --- |
| [`AssetPath`](#m-assetpath) | The asset path. |
| [`Code`](#m-code) | The short reason code. |

### <a id="m-assetpath"></a>AssetPath

```csharp
public string AssetPath { get; }
```

The asset path.

**Type:** `string`

### <a id="m-code"></a>Code

```csharp
public string Code { get; }
```

The short reason code.

**Type:** `string`
