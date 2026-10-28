[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# GpuAdapterInfo record

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`GpuAdapterInfo`](GpuAdapterInfo.md)&gt;

```csharp
public sealed record GpuAdapterInfo : IEquatable<GpuAdapterInfo>
```

Information about a graphics adapter.

## Constructors

| Member | Description |
| --- | --- |
| [`GpuAdapterInfo(string, AdapterKind, uint, uint, string)`](#m-ctor-string-adapterkind-uint-uint-string) | Information about a graphics adapter. |

### <a id="m-ctor-string-adapterkind-uint-uint-string"></a>GpuAdapterInfo(string, AdapterKind, uint, uint, string)

```csharp
public GpuAdapterInfo(string Name, AdapterKind Kind, uint VendorId, uint DeviceId, string ApiVersion);
```

Information about a graphics adapter.

**Parameters**

- `Name` (`string`): Driver-reported device name.
- `Kind` ([`AdapterKind`](AdapterKind.md)): Adapter kind.
- `VendorId` (`uint`): PCI vendor id.
- `DeviceId` (`uint`): PCI device id.
- `ApiVersion` (`string`): Highest graphics API version supported, as "major.minor.patch".

## Properties

| Member | Description |
| --- | --- |
| [`ApiVersion`](#m-apiversion) | Highest graphics API version supported, as "major.minor.patch". |
| [`DeviceId`](#m-deviceid) | PCI device id. |
| [`Kind`](#m-kind) | Adapter kind. |
| [`Name`](#m-name) | Driver-reported device name. |
| [`VendorId`](#m-vendorid) | PCI vendor id. |

### <a id="m-apiversion"></a>ApiVersion

```csharp
public string ApiVersion { get; init; }
```

Highest graphics API version supported, as "major.minor.patch".

**Type:** `string`

### <a id="m-deviceid"></a>DeviceId

```csharp
public uint DeviceId { get; init; }
```

PCI device id.

**Type:** `uint`

### <a id="m-kind"></a>Kind

```csharp
public AdapterKind Kind { get; init; }
```

Adapter kind.

**Type:** [`AdapterKind`](AdapterKind.md)

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

Driver-reported device name.

**Type:** `string`

### <a id="m-vendorid"></a>VendorId

```csharp
public uint VendorId { get; init; }
```

PCI vendor id.

**Type:** `uint`
