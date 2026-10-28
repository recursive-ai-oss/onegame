[API reference](../../index.md) › [OneGame.Gpu.Metal](../index.md) › OneGame.Gpu.Metal

# MetalGpu static class

**Namespace:** `OneGame.Gpu.Metal`  
**Package:** [`RecursiveAI.OneGame.Gpu.Metal`](../index.md)

```csharp
public static class MetalGpu
```

Entry point of the Metal backend.

## Properties

| Member | Description |
| --- | --- |
| [`IsAvailable`](#m-isavailable) | Whether this machine can run the backend: an Apple arm64 platform with a Metal device. |

### <a id="m-isavailable"></a>IsAvailable

```csharp
public static bool IsAvailable { get; }
```

Whether this machine can run the backend: an Apple arm64 platform with a Metal device.

**Type:** `bool`

## Methods

| Member | Description |
| --- | --- |
| [`CreateDevice(GpuDeviceOptions?)`](#m-createdevice-gpudeviceoptions) | Creates a device on the best Metal device (discrete first, then the one driving the display), or the one chosen by [`GpuDeviceOptions.Adapter`](../../OneGame.Gpu/OneGame.Gpu/GpuDeviceOptions.md#m-adapter) or the ONEGAME\_GPU\_ADAPTER environment variable. |
| [`EnumerateAdapters()`](#m-enumerateadapters) | Lists the Metal devices of this machine. |

### <a id="m-createdevice-gpudeviceoptions"></a>CreateDevice(GpuDeviceOptions?)

```csharp
public static GpuDevice CreateDevice(GpuDeviceOptions? options = null);
```

Creates a device on the best Metal device (discrete first, then the one driving the display), or the one chosen by [`GpuDeviceOptions.Adapter`](../../OneGame.Gpu/OneGame.Gpu/GpuDeviceOptions.md#m-adapter) or the ONEGAME\_GPU\_ADAPTER environment variable.

[`GpuDeviceOptions.Validation`](../../OneGame.Gpu/OneGame.Gpu/GpuDeviceOptions.md#m-validation) turns on Metal's API validation (the debug layer), which only takes effect when it is enabled before the first Metal call of the process; set `MTL_DEBUG_LAYER=1` in the environment to be sure. With validation on, a Metal error ends the process with the message, so tests cannot pass over one.

**Parameters**

- `options` ([`GpuDeviceOptions`](../../OneGame.Gpu/OneGame.Gpu/GpuDeviceOptions.md)?)

**Returns:** [`GpuDevice`](../../OneGame.Gpu/OneGame.Gpu/GpuDevice.md)

**Exceptions**

- [`GpuUnsupportedException`](../../OneGame.Gpu/OneGame.Gpu/GpuUnsupportedException.md): No Metal device, not an Apple arm64 platform, or validation was required but is off.

### <a id="m-enumerateadapters"></a>EnumerateAdapters()

```csharp
public static IReadOnlyList<GpuAdapterInfo> EnumerateAdapters();
```

Lists the Metal devices of this machine.

**Returns:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`GpuAdapterInfo`](../../OneGame.Gpu/OneGame.Gpu/GpuAdapterInfo.md)&gt;
