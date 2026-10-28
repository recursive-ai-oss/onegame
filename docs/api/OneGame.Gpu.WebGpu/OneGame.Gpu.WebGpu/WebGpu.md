[API reference](../../index.md) › [OneGame.Gpu.WebGpu](../index.md) › OneGame.Gpu.WebGpu

# WebGpu static class

**Namespace:** `OneGame.Gpu.WebGpu`  
**Package:** [`RecursiveAI.OneGame.Gpu.WebGpu`](../index.md)

```csharp
public static class WebGpu
```

Creates WebGPU devices in the browser.

## Properties

| Member | Description |
| --- | --- |
| [`IsAvailable`](#m-isavailable) | True when the page has a WebGPU implementation (navigator.gpu exists). |

### <a id="m-isavailable"></a>IsAvailable

```csharp
public static bool IsAvailable { get; }
```

True when the page has a WebGPU implementation (navigator.gpu exists).

**Type:** `bool`

## Methods

| Member | Description |
| --- | --- |
| [`CreateDeviceAsync(GpuDeviceOptions?)`](#m-createdeviceasync-gpudeviceoptions) | Asks the browser for an adapter and a device. |
| [`SettleAsync(GpuDevice)`](#m-settleasync-gpudevice) | Runs everything recorded so far and waits until the GPU has finished it, so errors it caused have been reported. |

### <a id="m-createdeviceasync-gpudeviceoptions"></a>CreateDeviceAsync(GpuDeviceOptions?)

```csharp
public static Task<GpuDevice> CreateDeviceAsync(GpuDeviceOptions? options = null);
```

Asks the browser for an adapter and a device. The browser answers asynchronously, so a game awaits this before it creates its instance and hands the device to it.

**Parameters**

- `options` ([`GpuDeviceOptions`](../../OneGame.Gpu/OneGame.Gpu/GpuDeviceOptions.md)?)

**Returns:** [`Task`](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task-1)&lt;[`GpuDevice`](../../OneGame.Gpu/OneGame.Gpu/GpuDevice.md)&gt;

**Exceptions**

- [`GpuUnsupportedException`](../../OneGame.Gpu/OneGame.Gpu/GpuUnsupportedException.md): The browser has no WebGPU, or no adapter.

### <a id="m-settleasync-gpudevice"></a>SettleAsync(GpuDevice)

```csharp
public static Task SettleAsync(GpuDevice device);
```

Runs everything recorded so far and waits until the GPU has finished it, so errors it caused have been reported.

**Parameters**

- `device` ([`GpuDevice`](../../OneGame.Gpu/OneGame.Gpu/GpuDevice.md))

**Returns:** [`Task`](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)
