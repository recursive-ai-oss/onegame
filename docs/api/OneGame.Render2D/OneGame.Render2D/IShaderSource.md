[API reference](../../index.md) › [OneGame.Render2D](../index.md) › OneGame.Render2D

# IShaderSource interface

**Namespace:** `OneGame.Render2D`  
**Package:** [`RecursiveAI.OneGame.Render2D`](../index.md)

```csharp
public interface IShaderSource
```

Supplies the renderer's built-in shaders by program and entry point name. A game passes the source generated from the shader build output; a test or tool can pass its own.

## Methods

| Member | Description |
| --- | --- |
| [`Load(string, string)`](#m-load-string-string) | Returns the compiled entry point `entryPoint` of the program `program`. |

### <a id="m-load-string-string"></a>Load(string, string)

```csharp
ShaderCode Load(string program, string entryPoint);
```

Returns the compiled entry point `entryPoint` of the program `program`.

**Parameters**

- `program` (`string`): One of [`ShaderNames`](ShaderNames.md): the HLSL file name without its extension.
- `entryPoint` (`string`): One of the entry point names in [`ShaderNames`](ShaderNames.md).

**Returns:** [`ShaderCode`](ShaderCode.md) — The compiled stage.
