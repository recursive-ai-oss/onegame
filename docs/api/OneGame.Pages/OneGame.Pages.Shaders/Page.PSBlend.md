[API reference](../../index.md) › [OneGame.Pages](../index.md) › OneGame.Pages.Shaders

# Page.PSBlend static class

**Namespace:** `OneGame.Pages.Shaders`  
**Package:** [`RecursiveAI.OneGame.Pages`](../index.md)  
**Declared in:** [`Page`](Page.md)

```csharp
public static class Page.PSBlend
```

## Fields

| Member | Description |
| --- | --- |
| [`EntryPoint`](#m-entrypoint) |  |
| [`MslFunctionName`](#m-mslfunctionname) | The function to look up in the compiled library (differs from [`Page.PSBlend.EntryPoint`](Page.PSBlend.md#m-entrypoint) when MSL reserves the name). |
| [`Stage`](#m-stage) |  |
| [`WgslEntryPoint`](#m-wgslentrypoint) | The entry point name in the WGSL source (differs from [`Page.PSBlend.EntryPoint`](Page.PSBlend.md#m-entrypoint) when WGSL reserves the name). |

### <a id="m-entrypoint"></a>EntryPoint

```csharp
public const string EntryPoint = "PSBlend";
```

**Type:** `string`

### <a id="m-mslfunctionname"></a>MslFunctionName

```csharp
public const string MslFunctionName = "PSBlend";
```

The function to look up in the compiled library (differs from [`Page.PSBlend.EntryPoint`](Page.PSBlend.md#m-entrypoint) when MSL reserves the name).

**Type:** `string`

### <a id="m-stage"></a>Stage

```csharp
public const ShaderStage Stage = 1;
```

**Type:** `ShaderStage`

### <a id="m-wgslentrypoint"></a>WgslEntryPoint

```csharp
public const string WgslEntryPoint = "PSBlend";
```

The entry point name in the WGSL source (differs from [`Page.PSBlend.EntryPoint`](Page.PSBlend.md#m-entrypoint) when WGSL reserves the name).

**Type:** `string`

## Properties

| Member | Description |
| --- | --- |
| [`Layout`](#m-layout) | The resources, push-constant block and vertex input of this entry point as plain data; combine entry points with `PipelineLayout`. |
| [`Msl`](#m-msl) | UTF-8 Metal Shading Language source. |
| [`SpirV`](#m-spirv) |  |
| [`Wgsl`](#m-wgsl) | UTF-8 WGSL source. |

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
