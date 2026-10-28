[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# GltfException class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)  
**Inherits:** [`Exception`](https://learn.microsoft.com/dotnet/api/system.exception)

```csharp
public sealed class GltfException : Exception
```

The file cannot be imported: it is damaged, hostile, over a limit or uses something the importer does not support.

## Constructors

| Member | Description |
| --- | --- |
| [`GltfException(string, string)`](#m-ctor-string-string) | Creates the exception. |

### <a id="m-ctor-string-string"></a>GltfException(string, string)

```csharp
public GltfException(string code, string message);
```

Creates the exception.

**Parameters**

- `code` (`string`): The stable error code; see the interface document for the list.
- `message` (`string`): What is wrong and where.

## Properties

| Member | Description |
| --- | --- |
| [`Code`](#m-code) | A short stable code a program can act on, such as `buffer_range` or `limit_exceeded`. |

### <a id="m-code"></a>Code

```csharp
public string Code { get; }
```

A short stable code a program can act on, such as `buffer_range` or `limit_exceeded`.

**Type:** `string`
