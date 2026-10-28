[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# GltfWarning struct

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`GltfWarning`](GltfWarning.md)&gt;

```csharp
public readonly struct GltfWarning : IEquatable<GltfWarning>
```

Something that loaded but was degraded or ignored.

## Constructors

| Member | Description |
| --- | --- |
| [`GltfWarning(string, string)`](#m-ctor-string-string) | Something that loaded but was degraded or ignored. |

### <a id="m-ctor-string-string"></a>GltfWarning(string, string)

```csharp
public GltfWarning(string Code, string Message);
```

Something that loaded but was degraded or ignored.

**Parameters**

- `Code` (`string`): A short stable code.
- `Message` (`string`): What happened and where.

## Properties

| Member | Description |
| --- | --- |
| [`Code`](#m-code) | A short stable code. |
| [`Message`](#m-message) | What happened and where. |

### <a id="m-code"></a>Code

```csharp
public string Code { get; init; }
```

A short stable code.

**Type:** `string`

### <a id="m-message"></a>Message

```csharp
public string Message { get; init; }
```

What happened and where.

**Type:** `string`
