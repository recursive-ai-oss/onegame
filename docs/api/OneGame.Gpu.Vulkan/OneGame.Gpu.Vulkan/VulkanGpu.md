[API reference](../../index.md) › [OneGame.Gpu.Vulkan](../index.md) › OneGame.Gpu.Vulkan

# VulkanGpu static class

**Namespace:** `OneGame.Gpu.Vulkan`  
**Package:** [`RecursiveAI.OneGame.Gpu.Vulkan`](../index.md)

```csharp
public static class VulkanGpu
```

Entry point of the Vulkan backend.

## Properties

| Member | Description |
| --- | --- |
| [`IsAvailable`](#m-isavailable) | Whether a Vulkan loader library is present on this machine. |

### <a id="m-isavailable"></a>IsAvailable

```csharp
public static bool IsAvailable { get; }
```

Whether a Vulkan loader library is present on this machine.

**Type:** `bool`

## Methods

| Member | Description |
| --- | --- |
| [`CreateDevice(GpuDeviceOptions?)`](#m-createdevice-gpudeviceoptions) | Creates a device on the best adapter, or the one chosen by [`GpuDeviceOptions.Adapter`](../../OneGame.Gpu/OneGame.Gpu/GpuDeviceOptions.md#m-adapter) or the ONEGAME\_GPU\_ADAPTER environment variable. |
| [`EnumerateAdapters()`](#m-enumerateadapters) | Lists the adapters that can run the backend (Vulkan 1.3 with a graphics and compute queue). |

### <a id="m-createdevice-gpudeviceoptions"></a>CreateDevice(GpuDeviceOptions?)

```csharp
public static GpuDevice CreateDevice(GpuDeviceOptions? options = null);
```

Creates a device on the best adapter, or the one chosen by [`GpuDeviceOptions.Adapter`](../../OneGame.Gpu/OneGame.Gpu/GpuDeviceOptions.md#m-adapter) or the ONEGAME\_GPU\_ADAPTER environment variable.

**Parameters**

- `options` ([`GpuDeviceOptions`](../../OneGame.Gpu/OneGame.Gpu/GpuDeviceOptions.md)?)

**Returns:** [`GpuDevice`](../../OneGame.Gpu/OneGame.Gpu/GpuDevice.md)

**Exceptions**

- [`GpuUnsupportedException`](../../OneGame.Gpu/OneGame.Gpu/GpuUnsupportedException.md): No Vulkan loader, no suitable adapter, or the validation layer was required but is missing.

### <a id="m-enumerateadapters"></a>EnumerateAdapters()

```csharp
public static IReadOnlyList<GpuAdapterInfo> EnumerateAdapters();
```

Lists the adapters that can run the backend (Vulkan 1.3 with a graphics and compute queue).

**Returns:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`GpuAdapterInfo`](../../OneGame.Gpu/OneGame.Gpu/GpuAdapterInfo.md)&gt;
