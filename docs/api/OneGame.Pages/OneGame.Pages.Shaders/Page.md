[API reference](../../index.md) › [OneGame.Pages](../index.md) › OneGame.Pages.Shaders

# Page static class

**Namespace:** `OneGame.Pages.Shaders`  
**Package:** [`RecursiveAI.OneGame.Pages`](../index.md)

```csharp
public static class Page
```

## Nested types

| Type | Description |
| --- | --- |
| [`Page.Bindings`](Page.Bindings.md) |  |
| [`Page.Frame`](Page.Frame.md) |  |
| [`Page.PSBackdrop`](Page.PSBackdrop.md) |  |
| [`Page.PSBlend`](Page.PSBlend.md) |  |
| [`Page.PSBlur`](Page.PSBlur.md) |  |
| [`Page.PSColor`](Page.PSColor.md) |  |
| [`Page.PSDropTint`](Page.PSDropTint.md) |  |
| [`Page.PSEncode`](Page.PSEncode.md) |  |
| [`Page.PSLayer`](Page.PSLayer.md) |  |
| [`Page.PSMain`](Page.PSMain.md) |  |
| [`Page.PSMaskApply`](Page.PSMaskApply.md) |  |
| [`Page.PSMaskCombine`](Page.PSMaskCombine.md) |  |
| [`Page.PSOver`](Page.PSOver.md) |  |
| [`Page.PSPresent`](Page.PSPresent.md) |  |
| [`Page.PSProject`](Page.PSProject.md) |  |
| [`Page.VSMain`](Page.VSMain.md) |  |

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
