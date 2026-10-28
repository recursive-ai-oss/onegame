[API reference](../../index.md) › [OneGame.Render2D](../index.md) › OneGame.Render2D

# Material class

**Namespace:** `OneGame.Render2D`  
**Package:** [`RecursiveAI.OneGame.Render2D`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class Material : IDisposable
```

A pixel shader with parameters, usable on sprites. Make one with [`Renderer2D.CreateMaterial()`](Renderer2D.md#m-creatematerial-shadercode-string). The shader takes the renderer's quad vertex output (see the material contract in the renderer's documentation) and up to eight float4 parameters; a parameter change takes effect on every sprite that uses the material in the frame being built.

## Properties

| Member | Description |
| --- | --- |
| [`ParameterCount`](#m-parametercount) | The number of parameter slots. |

### <a id="m-parametercount"></a>ParameterCount

```csharp
public int ParameterCount { get; }
```

The number of parameter slots.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Dispose()`](#m-dispose) | Releases the GPU objects. |
| [`GetParameter(int)`](#m-getparameter-int) | Reads back parameter `slot`. |
| [`SetParameter(int, Vector4)`](#m-setparameter-int-vector4) | Sets parameter `slot`, which the shader reads as `P[slot]`. |

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Releases the GPU objects. Do not draw with the material afterwards.

### <a id="m-getparameter-int"></a>GetParameter(int)

```csharp
public Vector4 GetParameter(int slot);
```

Reads back parameter `slot`.

**Parameters**

- `slot` (`int`): 0 to 7.

**Returns:** [`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4) — The current value.

### <a id="m-setparameter-int-vector4"></a>SetParameter(int, Vector4)

```csharp
public void SetParameter(int slot, Vector4 value);
```

Sets parameter `slot`, which the shader reads as `P[slot]`.

**Parameters**

- `slot` (`int`): 0 to 7.
- `value` ([`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4)): The value.
