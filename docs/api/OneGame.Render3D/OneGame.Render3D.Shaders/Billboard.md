[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D.Shaders

# Billboard static class

**Namespace:** `OneGame.Render3D.Shaders`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public static class Billboard
```

## Nested types

| Type | Description |
| --- | --- |
| [`Billboard.Bindings`](Billboard.Bindings.md) |  |
| [`Billboard.Frame`](Billboard.Frame.md) |  |
| [`Billboard.PSBillboard`](Billboard.PSBillboard.md) |  |
| [`Billboard.VSBillboard`](Billboard.VSBillboard.md) |  |

## Properties

| Member | Description |
| --- | --- |
| [`Pipeline`](#m-pipeline) | The pipeline layout of this file's vertex and pixel entry points as plain data; convert it with `ToVulkan`, `TryToMetal` or `TryToWebGpu`. |
| [`Reflection`](#m-reflection) | The reflection this class was generated from; parsed on first use. |
| [`ReflectionData`](#m-reflectiondata) |  |

### <a id="m-pipeline"></a>Pipeline

```csharp
public static PipelineLayout Pipeline { get; }
```

The pipeline layout of this file's vertex and pixel entry points as plain data; convert it with `ToVulkan`, `TryToMetal` or `TryToWebGpu`.

**Type:** `PipelineLayout`

### <a id="m-reflection"></a>Reflection

```csharp
public static ShaderReflection Reflection { get; }
```

The reflection this class was generated from; parsed on first use.

**Type:** `ShaderReflection`

### <a id="m-reflectiondata"></a>ReflectionData

```csharp
public static ReadOnlySpan<byte> ReflectionData { get; }
```

**Type:** [`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`
