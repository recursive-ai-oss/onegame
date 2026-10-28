[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# HotShader record

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`HotShader`](HotShader.md)&gt;

```csharp
public sealed record HotShader : IEquatable<HotShader>
```

One shader a game wants to see edited while it runs: the HLSL file, the entry point of its pixel stage, the interface the game was built against and what to do with a recompiled version. Register it with [`HotShaders.Register()`](HotShaders.md#m-register-hotshader); development builds watch the file, release builds ignore the registration.

## Constructors

| Member | Description |
| --- | --- |
| [`HotShader(string, string, string, ShaderReflection, Action<ReadOnlyMemory<byte>>)`](#m-ctor-string-string-string-shaderreflection-action-readonlymemory-byte) | One shader a game wants to see edited while it runs: the HLSL file, the entry point of its pixel stage, the interface the game was built against and what to do with a recompiled version. |

### <a id="m-ctor-string-string-string-shaderreflection-action-readonlymemory-byte"></a>HotShader(string, string, string, ShaderReflection, Action&lt;ReadOnlyMemory&lt;byte&gt;&gt;)

```csharp
public HotShader(string Name, string Path, string EntryPoint, ShaderReflection Baseline, Action<ReadOnlyMemory<byte>> Apply);
```

One shader a game wants to see edited while it runs: the HLSL file, the entry point of its pixel stage, the interface the game was built against and what to do with a recompiled version. Register it with [`HotShaders.Register()`](HotShaders.md#m-register-hotshader); development builds watch the file, release builds ignore the registration.

**Parameters**

- `Name` (`string`): A short name shown in reports, usually the material's.
- `Path` (`string`): The `.hlsl` file. Relative paths resolve against the working directory.
- `EntryPoint` (`string`): The pixel entry point to take from the compiled file.
- `Baseline` (`ShaderReflection`): The reflection the game was built with, for example `Ripple.Reflection` of the generated shader class.
- `Apply` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`ReadOnlyMemory`](https://learn.microsoft.com/dotnet/api/system.readonlymemory-1)`<byte>>`): Swaps the recompiled SPIR-V into the live pipeline, for example by calling `Renderer2D.ReplaceMaterialShader`.

## Properties

| Member | Description |
| --- | --- |
| [`Apply`](#m-apply) | Swaps the recompiled SPIR-V into the live pipeline, for example by calling `Renderer2D.ReplaceMaterialShader`. |
| [`Baseline`](#m-baseline) | The reflection the game was built with, for example `Ripple.Reflection` of the generated shader class. |
| [`EntryPoint`](#m-entrypoint) | The pixel entry point to take from the compiled file. |
| [`FileLength`](#m-filelength) | The file's length when it was registered. |
| [`FileTimeUtc`](#m-filetimeutc) | The file's last write time when it was registered; later edits are changes. |
| [`Name`](#m-name) | A short name shown in reports, usually the material's. |
| [`Path`](#m-path) | The `.hlsl` file. |

### <a id="m-apply"></a>Apply

```csharp
public Action<ReadOnlyMemory<byte>> Apply { get; init; }
```

Swaps the recompiled SPIR-V into the live pipeline, for example by calling `Renderer2D.ReplaceMaterialShader`.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`ReadOnlyMemory`](https://learn.microsoft.com/dotnet/api/system.readonlymemory-1)`<byte>>`

### <a id="m-baseline"></a>Baseline

```csharp
public ShaderReflection Baseline { get; init; }
```

The reflection the game was built with, for example `Ripple.Reflection` of the generated shader class.

**Type:** `ShaderReflection`

### <a id="m-entrypoint"></a>EntryPoint

```csharp
public string EntryPoint { get; init; }
```

The pixel entry point to take from the compiled file.

**Type:** `string`

### <a id="m-filelength"></a>FileLength

```csharp
public long FileLength { get; init; }
```

The file's length when it was registered. Set by [`HotShaders.Register()`](HotShaders.md#m-register-hotshader).

**Type:** `long`

### <a id="m-filetimeutc"></a>FileTimeUtc

```csharp
public DateTime FileTimeUtc { get; init; }
```

The file's last write time when it was registered; later edits are changes. Set by [`HotShaders.Register()`](HotShaders.md#m-register-hotshader).

**Type:** [`DateTime`](https://learn.microsoft.com/dotnet/api/system.datetime)

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

A short name shown in reports, usually the material's.

**Type:** `string`

### <a id="m-path"></a>Path

```csharp
public string Path { get; init; }
```

The `.hlsl` file. Relative paths resolve against the working directory.

**Type:** `string`
