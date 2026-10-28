[API reference](../../index.md) › [OneGame.Gpu.Backends](../index.md) › OneGame.Gpu

# GpuBackends static class

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu.Backends`](../index.md)

```csharp
public static class GpuBackends
```

Chooses and creates a graphics device. Metal is the default on macOS and iOS, WebGPU in a browser and Vulkan everywhere else; the `ONEGAME_GPU_BACKEND` environment variable (`vulkan`, `metal` or `auto`) overrides it, for example to run the Vulkan backend through a loader on a Mac.

**Example**

```csharp
if (!GpuBackends.IsAvailable)
{
    Console.WriteLine(GpuBackends.UnavailableReason);   // for example no Vulkan driver
    return;
}

using var device = GpuBackends.CreateDevice(new GpuDeviceOptions { ApplicationName = "my tool" });
Console.WriteLine($"{device.Backend} on {device.Adapter.Name}");
```

## Properties

| Member | Description |
| --- | --- |
| [`IsAvailable`](#m-isavailable) | Whether the selected backend can run on this machine. |
| [`Selected`](#m-selected) | The backend this process uses: `ONEGAME_GPU_BACKEND` when set, otherwise the platform's own. |
| [`UnavailableReason`](#m-unavailablereason) | Why the selected backend is unavailable, for an error message. |

### <a id="m-isavailable"></a>IsAvailable

```csharp
public static bool IsAvailable { get; }
```

Whether the selected backend can run on this machine.

**Type:** `bool`

### <a id="m-selected"></a>Selected

```csharp
public static GpuBackend Selected { get; }
```

The backend this process uses: `ONEGAME_GPU_BACKEND` when set, otherwise the platform's own.

**Type:** [`GpuBackend`](../../OneGame.Gpu/OneGame.Gpu/GpuBackend.md)

### <a id="m-unavailablereason"></a>UnavailableReason

```csharp
public static string UnavailableReason { get; }
```

Why the selected backend is unavailable, for an error message.

**Type:** `string`

## Methods

| Member | Description |
| --- | --- |
| [`CreateDevice(GpuDeviceOptions?)`](#m-createdevice-gpudeviceoptions) | Creates a device on the selected backend. |
| [`CreateDeviceAsync(GpuDeviceOptions?)`](#m-createdeviceasync-gpudeviceoptions) | Creates a device on the selected backend. |
| [`EnumerateAdapters()`](#m-enumerateadapters) | Lists the adapters of the selected backend. |

### <a id="m-createdevice-gpudeviceoptions"></a>CreateDevice(GpuDeviceOptions?)

```csharp
public static GpuDevice CreateDevice(GpuDeviceOptions? options = null);
```

Creates a device on the selected backend.

**Parameters**

- `options` ([`GpuDeviceOptions`](../../OneGame.Gpu/OneGame.Gpu/GpuDeviceOptions.md)?): Device options; [`GpuDeviceOptions.Adapter`](../../OneGame.Gpu/OneGame.Gpu/GpuDeviceOptions.md#m-adapter) and `ONEGAME_GPU_ADAPTER` choose the adapter.

**Returns:** [`GpuDevice`](../../OneGame.Gpu/OneGame.Gpu/GpuDevice.md)

**Exceptions**

- [`GpuUnsupportedException`](../../OneGame.Gpu/OneGame.Gpu/GpuUnsupportedException.md): The backend is not available on this machine, or it is WebGPU, which can only be created asynchronously: use [`GpuBackends.CreateDeviceAsync()`](GpuBackends.md#m-createdeviceasync-gpudeviceoptions).

### <a id="m-createdeviceasync-gpudeviceoptions"></a>CreateDeviceAsync(GpuDeviceOptions?)

```csharp
public static Task<GpuDevice> CreateDeviceAsync(GpuDeviceOptions? options = null);
```

Creates a device on the selected backend. Works on every backend and is the only way to get one in a browser.

**Parameters**

- `options` ([`GpuDeviceOptions`](../../OneGame.Gpu/OneGame.Gpu/GpuDeviceOptions.md)?): Device options.

**Returns:** [`Task`](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task-1)&lt;[`GpuDevice`](../../OneGame.Gpu/OneGame.Gpu/GpuDevice.md)&gt;

**Exceptions**

- [`GpuUnsupportedException`](../../OneGame.Gpu/OneGame.Gpu/GpuUnsupportedException.md): The backend is not available on this machine.

### <a id="m-enumerateadapters"></a>EnumerateAdapters()

```csharp
public static IReadOnlyList<GpuAdapterInfo> EnumerateAdapters();
```

Lists the adapters of the selected backend.

**Returns:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`GpuAdapterInfo`](../../OneGame.Gpu/OneGame.Gpu/GpuAdapterInfo.md)&gt;
