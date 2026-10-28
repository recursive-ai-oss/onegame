[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D.Shaders

# Pbr static class

**Namespace:** `OneGame.Render3D.Shaders`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public static class Pbr
```

## Nested types

| Type | Description |
| --- | --- |
| [`Pbr.Bindings`](Pbr.Bindings.md) |  |
| [`Pbr.Frame`](Pbr.Frame.md) |  |
| [`Pbr.LightData`](Pbr.LightData.md) |  |
| [`Pbr.Material`](Pbr.Material.md) |  |
| [`Pbr.Object`](Pbr.Object.md) |  |
| [`Pbr.PSMain`](Pbr.PSMain.md) |  |
| [`Pbr.PSOutline`](Pbr.PSOutline.md) |  |
| [`Pbr.PSPrepass`](Pbr.PSPrepass.md) |  |
| [`Pbr.PSShadowMask`](Pbr.PSShadowMask.md) |  |
| [`Pbr.PSSky`](Pbr.PSSky.md) |  |
| [`Pbr.ShadowData`](Pbr.ShadowData.md) |  |
| [`Pbr.VSInst`](Pbr.VSInst.md) |  |
| [`Pbr.VSMain`](Pbr.VSMain.md) |  |
| [`Pbr.VSOutline`](Pbr.VSOutline.md) |  |
| [`Pbr.VSOutlineInst`](Pbr.VSOutlineInst.md) |  |
| [`Pbr.VSSky`](Pbr.VSSky.md) |  |

## Properties

| Member | Description |
| --- | --- |
| [`Reflection`](#m-reflection) | The reflection this class was generated from; parsed on first use. |
| [`ReflectionData`](#m-reflectiondata) |  |

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
