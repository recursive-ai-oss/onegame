[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# GpuShader class

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Inherits:** [`GpuObject`](GpuObject.md)

```csharp
public abstract class GpuShader : GpuObject
```

A compiled shader entry point.

## Constructors

| Member | Description |
| --- | --- |
| [`GpuShader()`](#m-ctor) |  |

### <a id="m-ctor"></a>GpuShader()

```csharp
protected GpuShader();
```

## Properties

| Member | Description |
| --- | --- |
| [`EntryPoint`](#m-entrypoint) | Entry point name. |
| [`Stage`](#m-stage) | Stage the shader runs in. |

### <a id="m-entrypoint"></a>EntryPoint

```csharp
public string EntryPoint { get; protected init; }
```

Entry point name.

**Type:** `string`

### <a id="m-stage"></a>Stage

```csharp
public ShaderStage Stage { get; protected init; }
```

Stage the shader runs in.

**Type:** [`ShaderStage`](ShaderStage.md)
