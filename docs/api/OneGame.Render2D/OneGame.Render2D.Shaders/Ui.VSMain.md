[API reference](../../index.md) › [OneGame.Render2D](../index.md) › OneGame.Render2D.Shaders

# Ui.VSMain static class

**Namespace:** `OneGame.Render2D.Shaders`  
**Package:** [`RecursiveAI.OneGame.Render2D`](../index.md)  
**Declared in:** [`Ui`](Ui.md)

```csharp
public static class Ui.VSMain
```

## Nested types

| Type | Description |
| --- | --- |
| [`Ui.VSMain.Vertex`](Ui.VSMain.Vertex.md) |  |

## Fields

| Member | Description |
| --- | --- |
| [`EntryPoint`](#m-entrypoint) |  |
| [`MslFunctionName`](#m-mslfunctionname) | The function to look up in the compiled library (differs from [`Ui.VSMain.EntryPoint`](Ui.VSMain.md#m-entrypoint) when MSL reserves the name). |
| [`MslVertexBufferIndex`](#m-mslvertexbufferindex) | Metal buffer index of the interleaved vertex buffer (layout: `Stride` and `Inputs`). |
| [`Stage`](#m-stage) |  |
| [`Stride`](#m-stride) |  |
| [`WgslEntryPoint`](#m-wgslentrypoint) | The entry point name in the WGSL source (differs from [`Ui.VSMain.EntryPoint`](Ui.VSMain.md#m-entrypoint) when WGSL reserves the name). |

### <a id="m-entrypoint"></a>EntryPoint

```csharp
public const string EntryPoint = "VSMain";
```

**Type:** `string`

### <a id="m-mslfunctionname"></a>MslFunctionName

```csharp
public const string MslFunctionName = "VSMain";
```

The function to look up in the compiled library (differs from [`Ui.VSMain.EntryPoint`](Ui.VSMain.md#m-entrypoint) when MSL reserves the name).

**Type:** `string`

### <a id="m-mslvertexbufferindex"></a>MslVertexBufferIndex

```csharp
public const uint MslVertexBufferIndex = 30;
```

Metal buffer index of the interleaved vertex buffer (layout: `Stride` and `Inputs`).

**Type:** `uint`

### <a id="m-stage"></a>Stage

```csharp
public const ShaderStage Stage = 0;
```

**Type:** `ShaderStage`

### <a id="m-stride"></a>Stride

```csharp
public const uint Stride = 152;
```

**Type:** `uint`

### <a id="m-wgslentrypoint"></a>WgslEntryPoint

```csharp
public const string WgslEntryPoint = "VSMain";
```

The entry point name in the WGSL source (differs from [`Ui.VSMain.EntryPoint`](Ui.VSMain.md#m-entrypoint) when WGSL reserves the name).

**Type:** `string`

## Properties

| Member | Description |
| --- | --- |
| [`Inputs`](#m-inputs) |  |
| [`Layout`](#m-layout) | The resources, push-constant block and vertex input of this entry point as plain data; combine entry points with `PipelineLayout`. |
| [`Msl`](#m-msl) | UTF-8 Metal Shading Language source. |
| [`SpirV`](#m-spirv) |  |
| [`Wgsl`](#m-wgsl) | UTF-8 WGSL source. |

### <a id="m-inputs"></a>Inputs

```csharp
public static ReadOnlySpan<VertexAttribute> Inputs { get; }
```

**Type:** [`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<VertexAttribute>`

### <a id="m-layout"></a>Layout

```csharp
public static EntryLayout Layout { get; }
```

The resources, push-constant block and vertex input of this entry point as plain data; combine entry points with `PipelineLayout`.

**Type:** `EntryLayout`

### <a id="m-msl"></a>Msl

```csharp
public static ReadOnlySpan<byte> Msl { get; }
```

UTF-8 Metal Shading Language source.

**Type:** [`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`

### <a id="m-spirv"></a>SpirV

```csharp
public static ReadOnlySpan<byte> SpirV { get; }
```

**Type:** [`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`

### <a id="m-wgsl"></a>Wgsl

```csharp
public static ReadOnlySpan<byte> Wgsl { get; }
```

UTF-8 WGSL source.

**Type:** [`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`
