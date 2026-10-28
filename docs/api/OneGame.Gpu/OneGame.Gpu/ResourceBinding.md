[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# ResourceBinding struct

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ResourceBinding`](ResourceBinding.md)&gt;

```csharp
public readonly struct ResourceBinding : IEquatable<ResourceBinding>
```

Describes one shader resource binding. Shader tooling emits these; pipeline layouts are built from them.

## Constructors

| Member | Description |
| --- | --- |
| [`ResourceBinding(uint, uint, uint, ResourceKind, ShaderStage)`](#m-ctor-uint-uint-uint-resourcekind-shaderstage) | Describes one shader resource binding. |

### <a id="m-ctor-uint-uint-uint-resourcekind-shaderstage"></a>ResourceBinding(uint, uint, uint, ResourceKind, ShaderStage)

```csharp
public ResourceBinding(uint Set, uint Binding, uint Count, ResourceKind Kind, ShaderStage Stages);
```

Describes one shader resource binding. Shader tooling emits these; pipeline layouts are built from them.

**Parameters**

- `Set` (`uint`): Bind group index.
- `Binding` (`uint`): Binding index inside the group.
- `Count` (`uint`): Array size (1 for a single resource).
- `Kind` ([`ResourceKind`](ResourceKind.md)): Resource kind.
- `Stages` ([`ShaderStage`](ShaderStage.md)): Stages that use the resource.

## Properties

| Member | Description |
| --- | --- |
| [`Binding`](#m-binding) | Binding index inside the group. |
| [`Count`](#m-count) | Array size (1 for a single resource). |
| [`Dynamic`](#m-dynamic) | For constant and storage buffers: the buffer range is moved at bind time by an offset given to `SetBindGroup`, so one bind group serves many objects. |
| [`Kind`](#m-kind) | Resource kind. |
| [`Set`](#m-set) | Bind group index. |
| [`Stages`](#m-stages) | Stages that use the resource. |

### <a id="m-binding"></a>Binding

```csharp
public uint Binding { get; init; }
```

Binding index inside the group.

**Type:** `uint`

### <a id="m-count"></a>Count

```csharp
public uint Count { get; init; }
```

Array size (1 for a single resource).

**Type:** `uint`

### <a id="m-dynamic"></a>Dynamic

```csharp
public bool Dynamic { get; init; }
```

For constant and storage buffers: the buffer range is moved at bind time by an offset given to `SetBindGroup`, so one bind group serves many objects. Offsets are given in binding order and are multiples of [`GpuDevice.DynamicOffsetAlignment`](GpuDevice.md#m-dynamicoffsetalignment).

**Type:** `bool`

### <a id="m-kind"></a>Kind

```csharp
public ResourceKind Kind { get; init; }
```

Resource kind.

**Type:** [`ResourceKind`](ResourceKind.md)

### <a id="m-set"></a>Set

```csharp
public uint Set { get; init; }
```

Bind group index.

**Type:** `uint`

### <a id="m-stages"></a>Stages

```csharp
public ShaderStage Stages { get; init; }
```

Stages that use the resource.

**Type:** [`ShaderStage`](ShaderStage.md)
