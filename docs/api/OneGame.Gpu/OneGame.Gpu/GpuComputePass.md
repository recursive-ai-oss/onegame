[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# GpuComputePass class

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
public abstract class GpuComputePass
```

Records compute dispatches.

## Constructors

| Member | Description |
| --- | --- |
| [`GpuComputePass()`](#m-ctor) |  |

### <a id="m-ctor"></a>GpuComputePass()

```csharp
protected GpuComputePass();
```

## Methods

| Member | Description |
| --- | --- |
| [`Dispatch(uint, uint, uint)`](#m-dispatch-uint-uint-uint) | Runs the compute shader over a grid of workgroups. |
| [`DispatchIndirect(GpuBuffer, ulong)`](#m-dispatchindirect-gpubuffer-ulong) | Runs the compute shader over a grid of workgroups read from a buffer ([`DispatchIndirectArgs`](DispatchIndirectArgs.md)). |
| [`EndPass()`](#m-endpass) | Ends the pass. |
| [`SetBindGroup(uint, GpuBindGroup)`](#m-setbindgroup-uint-gpubindgroup) | Binds a bind group at a set index of the current pipeline layout. |
| [`SetBindGroup(uint, GpuBindGroup, ReadOnlySpan<uint>)`](#m-setbindgroup-uint-gpubindgroup-readonlyspan-uint) | Binds a bind group whose layout has dynamic bindings; see [`GpuRenderPass.SetBindGroup()`](GpuRenderPass.md#m-setbindgroup-uint-gpubindgroup-readonlyspan-uint). |
| [`SetPipeline(GpuComputePipeline)`](#m-setpipeline-gpucomputepipeline) | Sets the pipeline for the following dispatches. |
| [`SetPushConstants(uint, ReadOnlySpan<byte>)`](#m-setpushconstants-uint-readonlyspan-byte) | Writes push constants visible to the following dispatches. |

### <a id="m-dispatch-uint-uint-uint"></a>Dispatch(uint, uint, uint)

```csharp
public abstract void Dispatch(uint x, uint y = 1, uint z = 1);
```

Runs the compute shader over a grid of workgroups.

**Parameters**

- `x` (`uint`)
- `y` (`uint`)
- `z` (`uint`)

### <a id="m-dispatchindirect-gpubuffer-ulong"></a>DispatchIndirect(GpuBuffer, ulong)

```csharp
public abstract void DispatchIndirect(GpuBuffer arguments, ulong offset);
```

Runs the compute shader over a grid of workgroups read from a buffer ([`DispatchIndirectArgs`](DispatchIndirectArgs.md)). The buffer needs [`BufferUsage.Indirect`](BufferUsage.md#m-indirect).

**Parameters**

- `arguments` ([`GpuBuffer`](GpuBuffer.md))
- `offset` (`ulong`)

### <a id="m-endpass"></a>EndPass()

```csharp
public abstract void EndPass();
```

Ends the pass.

### <a id="m-setbindgroup-uint-gpubindgroup"></a>SetBindGroup(uint, GpuBindGroup)

```csharp
public void SetBindGroup(uint setIndex, GpuBindGroup group);
```

Binds a bind group at a set index of the current pipeline layout.

**Parameters**

- `setIndex` (`uint`)
- `group` ([`GpuBindGroup`](GpuBindGroup.md))

### <a id="m-setbindgroup-uint-gpubindgroup-readonlyspan-uint"></a>SetBindGroup(uint, GpuBindGroup, ReadOnlySpan&lt;uint&gt;)

```csharp
public abstract void SetBindGroup(uint setIndex, GpuBindGroup group, ReadOnlySpan<uint> dynamicOffsets);
```

Binds a bind group whose layout has dynamic bindings; see [`GpuRenderPass.SetBindGroup()`](GpuRenderPass.md#m-setbindgroup-uint-gpubindgroup-readonlyspan-uint).

**Parameters**

- `setIndex` (`uint`)
- `group` ([`GpuBindGroup`](GpuBindGroup.md))
- `dynamicOffsets` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<uint>`)

### <a id="m-setpipeline-gpucomputepipeline"></a>SetPipeline(GpuComputePipeline)

```csharp
public abstract void SetPipeline(GpuComputePipeline pipeline);
```

Sets the pipeline for the following dispatches.

**Parameters**

- `pipeline` ([`GpuComputePipeline`](GpuComputePipeline.md))

### <a id="m-setpushconstants-uint-readonlyspan-byte"></a>SetPushConstants(uint, ReadOnlySpan&lt;byte&gt;)

```csharp
public abstract void SetPushConstants(uint offset, ReadOnlySpan<byte> data);
```

Writes push constants visible to the following dispatches.

**Parameters**

- `offset` (`uint`)
- `data` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)
