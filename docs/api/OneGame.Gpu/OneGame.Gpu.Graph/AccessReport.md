[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu.Graph

# AccessReport record

**Namespace:** `OneGame.Gpu.Graph`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`AccessReport`](AccessReport.md)&gt;

```csharp
public sealed record AccessReport : IEquatable<AccessReport>
```

One thing a pass touches, as shown in a report.

## Constructors

| Member | Description |
| --- | --- |
| [`AccessReport(string, string, uint, uint)`](#m-ctor-string-string-uint-uint) | One thing a pass touches, as shown in a report. |

### <a id="m-ctor-string-string-uint-uint"></a>AccessReport(string, string, uint, uint)

```csharp
public AccessReport(string Resource, string Access, uint Mip, uint Layer);
```

One thing a pass touches, as shown in a report.

**Parameters**

- `Resource` (`string`): Resource name.
- `Access` (`string`): How: Sampled, StorageRead, StorageWrite, ColorTarget, DepthTarget, ResolveTarget, CopySource, CopyDestination, Uniform, Vertex, Index, Indirect, Write or ReadWrite.
- `Mip` (`uint`): Mip level for render targets; 0 otherwise.
- `Layer` (`uint`): Array layer or cube face for render targets; 0 otherwise.

## Properties

| Member | Description |
| --- | --- |
| [`Access`](#m-access) | How: Sampled, StorageRead, StorageWrite, ColorTarget, DepthTarget, ResolveTarget, CopySource, CopyDestination, Uniform, Vertex, Index, Indirect, Write or ReadWrite. |
| [`Layer`](#m-layer) | Array layer or cube face for render targets; 0 otherwise. |
| [`Mip`](#m-mip) | Mip level for render targets; 0 otherwise. |
| [`Resource`](#m-resource) | Resource name. |

### <a id="m-access"></a>Access

```csharp
public string Access { get; init; }
```

How: Sampled, StorageRead, StorageWrite, ColorTarget, DepthTarget, ResolveTarget, CopySource, CopyDestination, Uniform, Vertex, Index, Indirect, Write or ReadWrite.

**Type:** `string`

### <a id="m-layer"></a>Layer

```csharp
public uint Layer { get; init; }
```

Array layer or cube face for render targets; 0 otherwise.

**Type:** `uint`

### <a id="m-mip"></a>Mip

```csharp
public uint Mip { get; init; }
```

Mip level for render targets; 0 otherwise.

**Type:** `uint`

### <a id="m-resource"></a>Resource

```csharp
public string Resource { get; init; }
```

Resource name.

**Type:** `string`
