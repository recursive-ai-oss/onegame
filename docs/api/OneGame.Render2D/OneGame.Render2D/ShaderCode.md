[API reference](../../index.md) › [OneGame.Render2D](../index.md) › OneGame.Render2D

# ShaderCode struct

**Namespace:** `OneGame.Render2D`  
**Package:** [`RecursiveAI.OneGame.Render2D`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ShaderCode`](ShaderCode.md)&gt;

```csharp
public readonly struct ShaderCode : IEquatable<ShaderCode>
```

One compiled shader stage: SPIR-V (and, for Metal, Metal Shading Language) and the name of its entry point.

## Constructors

| Member | Description |
| --- | --- |
| [`ShaderCode(ShaderStage, string, ReadOnlyMemory<byte>, ReadOnlyMemory<byte>, string?, EntryLayout?, ReadOnlyMemory<byte>, string?)`](#m-ctor-shaderstage-string-readonlymemory-byte-readonlymemory-byte-string-entrylayout-readonlymemory-byte-string) | One compiled shader stage: SPIR-V (and, for Metal, Metal Shading Language) and the name of its entry point. |

### <a id="m-ctor-shaderstage-string-readonlymemory-byte-readonlymemory-byte-string-entrylayout-readonlymemory-byte-string"></a>ShaderCode(ShaderStage, string, ReadOnlyMemory&lt;byte&gt;, ReadOnlyMemory&lt;byte&gt;, string?, EntryLayout?, ReadOnlyMemory&lt;byte&gt;, string?)

```csharp
public ShaderCode(ShaderStage Stage, string EntryPoint, ReadOnlyMemory<byte> SpirV, ReadOnlyMemory<byte> Msl = default, string? MslFunctionName = null, EntryLayout? Layout = null, ReadOnlyMemory<byte> Wgsl = default, string? WgslEntryPoint = null);
```

One compiled shader stage: SPIR-V (and, for Metal, Metal Shading Language) and the name of its entry point.

**Parameters**

- `Stage` ([`ShaderStage`](../../OneGame.Gpu/OneGame.Gpu/ShaderStage.md)): The pipeline stage.
- `EntryPoint` (`string`): The entry point function name in the SPIR-V module.
- `SpirV` ([`ReadOnlyMemory`](https://learn.microsoft.com/dotnet/api/system.readonlymemory-1)`<byte>`): The SPIR-V words as bytes. The renderer reads it while creating pipelines and does not keep it.
- `Msl` ([`ReadOnlyMemory`](https://learn.microsoft.com/dotnet/api/system.readonlymemory-1)`<byte>`): UTF-8 Metal Shading Language source, needed on Metal. Empty for shaders that only run on Vulkan.
- `MslFunctionName` (`string?`): The function to look up in `Msl`; defaults to the entry point.
- `Layout` (`EntryLayout?`): The entry point's layout (`Shader.Entry.Layout` of the generated class); needed on Metal.
- `Wgsl` ([`ReadOnlyMemory`](https://learn.microsoft.com/dotnet/api/system.readonlymemory-1)`<byte>`): UTF-8 WGSL source, needed on browser WebGPU. Empty for shaders compiled without it.
- `WgslEntryPoint` (`string?`): The entry point name in the WGSL; defaults to the entry point.

## Properties

| Member | Description |
| --- | --- |
| [`EntryPoint`](#m-entrypoint) | The entry point function name in the SPIR-V module. |
| [`Layout`](#m-layout) | The entry point's layout (`Shader.Entry.Layout` of the generated class); needed on Metal. |
| [`Msl`](#m-msl) | UTF-8 Metal Shading Language source, needed on Metal. |
| [`MslFunctionName`](#m-mslfunctionname) | The function to look up in `Msl`; defaults to the entry point. |
| [`SpirV`](#m-spirv) | The SPIR-V words as bytes. |
| [`Stage`](#m-stage) | The pipeline stage. |
| [`Wgsl`](#m-wgsl) | UTF-8 WGSL source, needed on browser WebGPU. |
| [`WgslEntryPoint`](#m-wgslentrypoint) | The entry point name in the WGSL; defaults to the entry point. |

### <a id="m-entrypoint"></a>EntryPoint

```csharp
public string EntryPoint { get; init; }
```

The entry point function name in the SPIR-V module.

**Type:** `string`

### <a id="m-layout"></a>Layout

```csharp
public EntryLayout? Layout { get; init; }
```

The entry point's layout (`Shader.Entry.Layout` of the generated class); needed on Metal.

**Type:** `EntryLayout?`

### <a id="m-msl"></a>Msl

```csharp
public ReadOnlyMemory<byte> Msl { get; init; }
```

UTF-8 Metal Shading Language source, needed on Metal. Empty for shaders that only run on Vulkan.

**Type:** [`ReadOnlyMemory`](https://learn.microsoft.com/dotnet/api/system.readonlymemory-1)`<byte>`

### <a id="m-mslfunctionname"></a>MslFunctionName

```csharp
public string? MslFunctionName { get; init; }
```

The function to look up in `Msl`; defaults to the entry point.

**Type:** `string?`

### <a id="m-spirv"></a>SpirV

```csharp
public ReadOnlyMemory<byte> SpirV { get; init; }
```

The SPIR-V words as bytes. The renderer reads it while creating pipelines and does not keep it.

**Type:** [`ReadOnlyMemory`](https://learn.microsoft.com/dotnet/api/system.readonlymemory-1)`<byte>`

### <a id="m-stage"></a>Stage

```csharp
public ShaderStage Stage { get; init; }
```

The pipeline stage.

**Type:** [`ShaderStage`](../../OneGame.Gpu/OneGame.Gpu/ShaderStage.md)

### <a id="m-wgsl"></a>Wgsl

```csharp
public ReadOnlyMemory<byte> Wgsl { get; init; }
```

UTF-8 WGSL source, needed on browser WebGPU. Empty for shaders compiled without it.

**Type:** [`ReadOnlyMemory`](https://learn.microsoft.com/dotnet/api/system.readonlymemory-1)`<byte>`

### <a id="m-wgslentrypoint"></a>WgslEntryPoint

```csharp
public string? WgslEntryPoint { get; init; }
```

The entry point name in the WGSL; defaults to the entry point.

**Type:** `string?`

## Methods

| Member | Description |
| --- | --- |
| [`ToSource()`](#m-tosource) | The form the graphics layer takes to create a shader on any backend. |

### <a id="m-tosource"></a>ToSource()

```csharp
public ShaderSource ToSource();
```

The form the graphics layer takes to create a shader on any backend.

**Returns:** [`ShaderSource`](../../OneGame.Gpu/OneGame.Gpu/ShaderSource.md)
