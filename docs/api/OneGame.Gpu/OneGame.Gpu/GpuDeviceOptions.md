[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# GpuDeviceOptions class

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
public sealed class GpuDeviceOptions
```

Options for creating a device.

## Constructors

| Member | Description |
| --- | --- |
| [`GpuDeviceOptions()`](#m-ctor) |  |

### <a id="m-ctor"></a>GpuDeviceOptions()

```csharp
public GpuDeviceOptions();
```

## Properties

| Member | Description |
| --- | --- |
| [`Adapter`](#m-adapter) | Picks the adapter whose name contains this text (case-insensitive) or whose kind is named by it ("software", "discrete", "integrated"). |
| [`ApplicationName`](#m-applicationname) | Application name reported to the driver. |
| [`RequireValidation`](#m-requirevalidation) | When set with [`GpuDeviceOptions.Validation`](GpuDeviceOptions.md#m-validation), creation fails if the validation layer is missing instead of running without it. |
| [`Validation`](#m-validation) | Turns on the graphics API's validation layer when available. |

### <a id="m-adapter"></a>Adapter

```csharp
public string? Adapter { get; init; }
```

Picks the adapter whose name contains this text (case-insensitive) or whose kind is named by it ("software", "discrete", "integrated"). The environment variable ONEGAME\_GPU\_ADAPTER overrides it.

**Type:** `string?`

### <a id="m-applicationname"></a>ApplicationName

```csharp
public string ApplicationName { get; init; }
```

Application name reported to the driver.

**Type:** `string`

### <a id="m-requirevalidation"></a>RequireValidation

```csharp
public bool RequireValidation { get; init; }
```

When set with [`GpuDeviceOptions.Validation`](GpuDeviceOptions.md#m-validation), creation fails if the validation layer is missing instead of running without it.

**Type:** `bool`

### <a id="m-validation"></a>Validation

```csharp
public bool Validation { get; init; }
```

Turns on the graphics API's validation layer when available. The environment variable ONEGAME\_GPU\_VALIDATION=1 also turns it on.

**Type:** `bool`
