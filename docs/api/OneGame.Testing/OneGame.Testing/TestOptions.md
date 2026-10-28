[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# TestOptions record

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`TestOptions`](TestOptions.md)&gt;

```csharp
public sealed record TestOptions : IEquatable<TestOptions>
```

Options for [`TestCluster`](TestCluster.md).

## Constructors

| Member | Description |
| --- | --- |
| [`TestOptions()`](#m-ctor) |  |

### <a id="m-ctor"></a>TestOptions()

```csharp
public TestOptions();
```

## Properties

| Member | Description |
| --- | --- |
| [`Gpu`](#m-gpu) | Graphics device policy for captures. |
| [`Height`](#m-height) | Window height in pixels. |
| [`OutputDirectory`](#m-outputdirectory) | Folder for files that operations write (captures, snapshots). |
| [`Scale`](#m-scale) | UI scale. |
| [`Seed`](#m-seed) | World seed; the game's own when null. |
| [`Shaders`](#m-shaders) | Where the 2D renderer gets compiled shaders; the renderer's own shaders when null. |
| [`Storage`](#m-storage) | Where the game's saves and settings live. |
| [`Width`](#m-width) | Window width in pixels. |

### <a id="m-gpu"></a>Gpu

```csharp
public GpuMode Gpu { get; init; }
```

Graphics device policy for captures. Auto uses a device when there is one; Off always renders on the CPU.

**Type:** [`GpuMode`](../../OneGame.Host/OneGame.Host/GpuMode.md)

### <a id="m-height"></a>Height

```csharp
public int Height { get; init; }
```

Window height in pixels.

**Type:** `int`

### <a id="m-outputdirectory"></a>OutputDirectory

```csharp
public string? OutputDirectory { get; init; }
```

Folder for files that operations write (captures, snapshots). A fresh temporary folder when null.

**Type:** `string?`

### <a id="m-scale"></a>Scale

```csharp
public float Scale { get; init; }
```

UI scale.

**Type:** `float`

### <a id="m-seed"></a>Seed

```csharp
public ulong? Seed { get; init; }
```

World seed; the game's own when null.

**Type:** `ulong?`

### <a id="m-shaders"></a>Shaders

```csharp
public IShaderSource? Shaders { get; init; }
```

Where the 2D renderer gets compiled shaders; the renderer's own shaders when null.

**Type:** [`IShaderSource`](../../OneGame.Render2D/OneGame.Render2D/IShaderSource.md)?

### <a id="m-storage"></a>Storage

```csharp
public IByteStore? Storage { get; init; }
```

Where the game's saves and settings live. A test starts with memory (never the player's files); give a shared store, or a folder, to model a restart.

**Type:** [`IByteStore`](../../OneGame.Core/OneGame.Storage/IByteStore.md)?

### <a id="m-width"></a>Width

```csharp
public int Width { get; init; }
```

Window width in pixels.

**Type:** `int`
