[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# ShaderSource struct

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
public readonly ref struct ShaderSource
```

> **Obsolete.** Types with embedded references are not supported in this version of your compiler.

One compiled shader entry point in every form a backend may need. Fill it from the members the shader build generates: `SpirV` for Vulkan, `Msl` and `MslFunctionName` for Metal, `Wgsl` and `WgslEntryPoint` for the browser, and the entry point's `Layout` so a backend that numbers resources per entry point (Metal) can place each (set, binding) pair.

A backend reads only the parts it needs and throws [`GpuUnsupportedException`](GpuUnsupportedException.md) when its part is empty.

## Constructors

| Member | Description |
| --- | --- |
| [`ShaderSource(ShaderStage, string)`](#m-ctor-shaderstage-string) | Creates a source for an entry point. |

### <a id="m-ctor-shaderstage-string"></a>ShaderSource(ShaderStage, string)

```csharp
public ShaderSource(ShaderStage stage, string entryPoint);
```

Creates a source for an entry point.

**Parameters**

- `stage` ([`ShaderStage`](ShaderStage.md)): Stage the entry point runs in.
- `entryPoint` (`string`): Entry point name as written in the HLSL.

## Properties

| Member | Description |
| --- | --- |
| [`EntryPoint`](#m-entrypoint) | Entry point name as written in the HLSL. |
| [`Layout`](#m-layout) | The entry point's layout from the shader build (`Shader.Entry.Layout` of the generated class, or `EntryLayout.TryCreate` for a shader compiled at run time): its resources with their Metal argument indices, the push-constant block with its Metal index and WGSL group and binding, and the compute thread-group size. |
| [`Msl`](#m-msl) | UTF-8 Metal Shading Language source, for Metal. |
| [`MslFunctionName`](#m-mslfunctionname) | The function to look up in the MSL source. |
| [`SpirV`](#m-spirv) | SPIR-V code (4-byte aligned words, little-endian), for Vulkan. |
| [`Stage`](#m-stage) | Stage the entry point runs in. |
| [`Wgsl`](#m-wgsl) | UTF-8 WGSL source, for browser WebGPU. |
| [`WgslEntryPoint`](#m-wgslentrypoint) | The entry point name in the WGSL. |

### <a id="m-entrypoint"></a>EntryPoint

```csharp
public string EntryPoint { get; }
```

Entry point name as written in the HLSL.

**Type:** `string`

### <a id="m-layout"></a>Layout

```csharp
public EntryLayout? Layout { get; init; }
```

The entry point's layout from the shader build (`Shader.Entry.Layout` of the generated class, or `EntryLayout.TryCreate` for a shader compiled at run time): its resources with their Metal argument indices, the push-constant block with its Metal index and WGSL group and binding, and the compute thread-group size. Metal needs it to bind resources, push constants and dispatch; WebGPU needs it for push constants; Vulkan ignores it.

**Type:** `EntryLayout?`

### <a id="m-msl"></a>Msl

```csharp
public ReadOnlySpan<byte> Msl { get; init; }
```

UTF-8 Metal Shading Language source, for Metal.

**Type:** [`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`

### <a id="m-mslfunctionname"></a>MslFunctionName

```csharp
public string? MslFunctionName { get; init; }
```

The function to look up in the MSL source. Defaults to [`ShaderSource.EntryPoint`](ShaderSource.md#m-entrypoint).

**Type:** `string?`

### <a id="m-spirv"></a>SpirV

```csharp
public ReadOnlySpan<byte> SpirV { get; init; }
```

SPIR-V code (4-byte aligned words, little-endian), for Vulkan.

**Type:** [`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`

### <a id="m-stage"></a>Stage

```csharp
public ShaderStage Stage { get; }
```

Stage the entry point runs in.

**Type:** [`ShaderStage`](ShaderStage.md)

### <a id="m-wgsl"></a>Wgsl

```csharp
public ReadOnlySpan<byte> Wgsl { get; init; }
```

UTF-8 WGSL source, for browser WebGPU.

**Type:** [`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`

### <a id="m-wgslentrypoint"></a>WgslEntryPoint

```csharp
public string? WgslEntryPoint { get; init; }
```

The entry point name in the WGSL. Defaults to [`ShaderSource.EntryPoint`](ShaderSource.md#m-entrypoint).

**Type:** `string?`
