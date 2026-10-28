[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Stores

# CloudFile struct

**Namespace:** `OneGame.Stores`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`CloudFile`](CloudFile.md)&gt;

```csharp
public readonly struct CloudFile : IEquatable<CloudFile>
```

One cloud file.

## Constructors

| Member | Description |
| --- | --- |
| [`CloudFile(string, int)`](#m-ctor-string-int) | One cloud file. |

### <a id="m-ctor-string-int"></a>CloudFile(string, int)

```csharp
public CloudFile(string Name, int Size);
```

One cloud file.

**Parameters**

- `Name` (`string`): Relative path.
- `Size` (`int`): Size in bytes.

## Properties

| Member | Description |
| --- | --- |
| [`Name`](#m-name) | Relative path. |
| [`Size`](#m-size) | Size in bytes. |

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

Relative path.

**Type:** `string`

### <a id="m-size"></a>Size

```csharp
public int Size { get; init; }
```

Size in bytes.

**Type:** `int`
