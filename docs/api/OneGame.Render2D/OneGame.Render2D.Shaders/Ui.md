[API reference](../../index.md) › [OneGame.Render2D](../index.md) › OneGame.Render2D.Shaders

# Ui static class

**Namespace:** `OneGame.Render2D.Shaders`  
**Package:** [`RecursiveAI.OneGame.Render2D`](../index.md)

```csharp
public static class Ui
```

## Nested types

| Type | Description |
| --- | --- |
| [`Ui.Bindings`](Ui.Bindings.md) |  |
| [`Ui.Frame`](Ui.Frame.md) |  |
| [`Ui.PSMain`](Ui.PSMain.md) |  |
| [`Ui.VSMain`](Ui.VSMain.md) |  |

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
