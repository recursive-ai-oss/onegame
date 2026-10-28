[API reference](../../index.md) › [OneGame.Render2D](../index.md) › OneGame.Render2D

# GeneratedShaders class

**Namespace:** `OneGame.Render2D`  
**Package:** [`RecursiveAI.OneGame.Render2D`](../index.md)  
**Implements:** [`IShaderSource`](IShaderSource.md)

```csharp
public sealed class GeneratedShaders : IShaderSource
```

The renderer's own shaders, compiled from `Shaders/*.hlsl` during the build. This is the default [`IShaderSource`](IShaderSource.md); the SPIR-V is embedded in the assembly, so nothing is read from disk at run time.

## Constructors

| Member | Description |
| --- | --- |
| [`GeneratedShaders()`](#m-ctor) |  |

### <a id="m-ctor"></a>GeneratedShaders()

```csharp
public GeneratedShaders();
```

## Properties

| Member | Description |
| --- | --- |
| [`Instance`](#m-instance) | The shared instance. |

### <a id="m-instance"></a>Instance

```csharp
public static GeneratedShaders Instance { get; }
```

The shared instance.

**Type:** [`GeneratedShaders`](GeneratedShaders.md)

## Methods

| Member | Description |
| --- | --- |
| [`Load(string, string)`](#m-load-string-string) | Returns the compiled entry point `entryPoint` of the program `program`. |

### <a id="m-load-string-string"></a>Load(string, string)

```csharp
public ShaderCode Load(string program, string entryPoint);
```

Returns the compiled entry point `entryPoint` of the program `program`.

**Parameters**

- `program` (`string`): One of [`ShaderNames`](ShaderNames.md): the HLSL file name without its extension.
- `entryPoint` (`string`): One of the entry point names in [`ShaderNames`](ShaderNames.md).

**Returns:** [`ShaderCode`](ShaderCode.md) — The compiled stage.
