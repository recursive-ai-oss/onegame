[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# ShadersResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ShadersResult`](ShadersResult.md)&gt;

```csharp
public sealed record ShadersResult : IEquatable<ShadersResult>
```

Result of the `shaders` operation.

## Constructors

| Member | Description |
| --- | --- |
| [`ShadersResult(ShaderStatus[], string[])`](#m-ctor-shaderstatus-string) | Result of the `shaders` operation. |

### <a id="m-ctor-shaderstatus-string"></a>ShadersResult(ShaderStatus\[\], string\[\])

```csharp
public ShadersResult(ShaderStatus[] Shaders, string[] Changed);
```

Result of the `shaders` operation.

**Parameters**

- `Shaders` ([`ShaderStatus`](ShaderStatus.md)\[\]): Every registered shader.
- `Changed` (`string[]`): The names whose file changed since the last check; each was recompiled.

## Properties

| Member | Description |
| --- | --- |
| [`Changed`](#m-changed) | The names whose file changed since the last check; each was recompiled. |
| [`Shaders`](#m-shaders) | Every registered shader. |

### <a id="m-changed"></a>Changed

```csharp
public string[] Changed { get; init; }
```

The names whose file changed since the last check; each was recompiled.

**Type:** `string[]`

### <a id="m-shaders"></a>Shaders

```csharp
public ShaderStatus[] Shaders { get; init; }
```

Every registered shader.

**Type:** [`ShaderStatus`](ShaderStatus.md)\[\]
