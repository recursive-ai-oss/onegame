[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu.Graph

# GraphStats struct

**Namespace:** `OneGame.Gpu.Graph`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
public struct GraphStats
```

Counts and sizes from one executed frame of a [`RenderGraph`](RenderGraph.md).

## Properties

| Member | Description |
| --- | --- |
| [`Created`](#m-created) | Physical resources created this frame. |
| [`CulledPasses`](#m-culledpasses) | Passes dropped because nothing used their results. |
| [`LivePasses`](#m-livepasses) | Passes that ran. |
| [`Passes`](#m-passes) | Passes added. |
| [`PhysicalBuffers`](#m-physicalbuffers) | Physical buffers behind the transient ones. |
| [`PhysicalTextures`](#m-physicaltextures) | Physical textures behind the transient ones (fewer when lifetimes do not overlap). |
| [`Reused`](#m-reused) | Physical resources taken from the pool this frame. |
| [`TransientBytes`](#m-transientbytes) | Bytes of transient memory in use. |
| [`TransientResources`](#m-transientresources) | Transient textures and buffers that were used by a surviving pass. |
| [`UnaliasedBytes`](#m-unaliasedbytes) | Bytes the same resources would need without sharing memory. |

### <a id="m-created"></a>Created

```csharp
public int Created { get; }
```

Physical resources created this frame.

**Type:** `int`

### <a id="m-culledpasses"></a>CulledPasses

```csharp
public int CulledPasses { get; }
```

Passes dropped because nothing used their results.

**Type:** `int`

### <a id="m-livepasses"></a>LivePasses

```csharp
public int LivePasses { get; }
```

Passes that ran.

**Type:** `int`

### <a id="m-passes"></a>Passes

```csharp
public int Passes { get; }
```

Passes added.

**Type:** `int`

### <a id="m-physicalbuffers"></a>PhysicalBuffers

```csharp
public int PhysicalBuffers { get; }
```

Physical buffers behind the transient ones.

**Type:** `int`

### <a id="m-physicaltextures"></a>PhysicalTextures

```csharp
public int PhysicalTextures { get; }
```

Physical textures behind the transient ones (fewer when lifetimes do not overlap).

**Type:** `int`

### <a id="m-reused"></a>Reused

```csharp
public int Reused { get; }
```

Physical resources taken from the pool this frame.

**Type:** `int`

### <a id="m-transientbytes"></a>TransientBytes

```csharp
public ulong TransientBytes { get; }
```

Bytes of transient memory in use.

**Type:** `ulong`

### <a id="m-transientresources"></a>TransientResources

```csharp
public int TransientResources { get; }
```

Transient textures and buffers that were used by a surviving pass.

**Type:** `int`

### <a id="m-unaliasedbytes"></a>UnaliasedBytes

```csharp
public ulong UnaliasedBytes { get; }
```

Bytes the same resources would need without sharing memory.

**Type:** `ulong`
