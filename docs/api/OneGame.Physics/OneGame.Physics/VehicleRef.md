[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# VehicleRef struct

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)  
**Implements:** [`ISyncValue`](../../OneGame.Core/OneGame.Core/ISyncValue-1.md)&lt;[`VehicleRef`](VehicleRef.md)&gt;, [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`VehicleRef`](VehicleRef.md)&gt;

```csharp
public readonly struct VehicleRef : ISyncValue<VehicleRef>, IEquatable<VehicleRef>
```

Names a vehicle in a [`PhysicsSpace3D`](PhysicsSpace3D.md), like [`BodyRef`](BodyRef.md) names a body. Keep it in a synced property.

## Properties

| Member | Description |
| --- | --- |
| [`Generation`](#m-generation) | How many vehicles have used the slot; 0 only in the null reference. |
| [`Index`](#m-index) | The vehicle slot in the space (separate from its chassis body slot). |
| [`IsNull`](#m-isnull) | True for the default value, which names nothing. |

### <a id="m-generation"></a>Generation

```csharp
public int Generation { get; }
```

How many vehicles have used the slot; 0 only in the null reference.

**Type:** `int`

### <a id="m-index"></a>Index

```csharp
public int Index { get; }
```

The vehicle slot in the space (separate from its chassis body slot).

**Type:** `int`

### <a id="m-isnull"></a>IsNull

```csharp
public bool IsNull { get; }
```

True for the default value, which names nothing.

**Type:** `bool`

## Methods

| Member | Description |
| --- | --- |
| [`Equals(VehicleRef)`](#m-equals-vehicleref) |  |
| [`Equals(object?)`](#m-equals-object) |  |
| [`GetHashCode()`](#m-gethashcode) |  |
| [`Read(ref SyncReader)`](#m-read-syncreader) | Reads a value written by [`ISyncValue<TSelf>.Write()`](../../OneGame.Core/OneGame.Core/ISyncValue-1.md#m-write-syncwriter). |
| [`ToString()`](#m-tostring) |  |
| [`Write(SyncWriter)`](#m-write-syncwriter) | Writes the value in the sync format. |
| [`WriteJson(JsonOut)`](#m-writejson-jsonout) | Writes the value as JSON for inspection. |

### <a id="m-equals-vehicleref"></a>Equals(VehicleRef)

```csharp
public bool Equals(VehicleRef other);
```

Implements [`IEquatable<VehicleRef>.Equals()`](https://learn.microsoft.com/dotnet/api/system.iequatable-1.equals).

**Parameters**

- `other` ([`VehicleRef`](VehicleRef.md))

**Returns:** `bool`

### <a id="m-equals-object"></a>Equals(object?)

```csharp
public override bool Equals(object? obj);
```

Overrides [`ValueType.Equals()`](https://learn.microsoft.com/dotnet/api/system.valuetype.equals).

**Parameters**

- `obj` (`object?`)

**Returns:** `bool`

### <a id="m-gethashcode"></a>GetHashCode()

```csharp
public override int GetHashCode();
```

Overrides [`ValueType.GetHashCode()`](https://learn.microsoft.com/dotnet/api/system.valuetype.gethashcode).

**Returns:** `int`

### <a id="m-read-syncreader"></a>Read(ref SyncReader)

```csharp
public static VehicleRef Read(ref SyncReader reader);
```

Reads a value written by [`ISyncValue<TSelf>.Write()`](../../OneGame.Core/OneGame.Core/ISyncValue-1.md#m-write-syncwriter). Problems are recorded on the reader, and a default value is returned.

**Parameters**

- `reader` ([`SyncReader`](../../OneGame.Core/OneGame.Core/SyncReader.md))

**Returns:** [`VehicleRef`](VehicleRef.md)

### <a id="m-tostring"></a>ToString()

```csharp
public override string ToString();
```

Overrides [`ValueType.ToString()`](https://learn.microsoft.com/dotnet/api/system.valuetype.tostring).

**Returns:** `string`

### <a id="m-write-syncwriter"></a>Write(SyncWriter)

```csharp
public void Write(SyncWriter writer);
```

Writes the value in the sync format.

**Parameters**

- `writer` ([`SyncWriter`](../../OneGame.Core/OneGame.Core/SyncWriter.md))

### <a id="m-writejson-jsonout"></a>WriteJson(JsonOut)

```csharp
public void WriteJson(JsonOut json);
```

Writes the value as JSON for inspection.

**Parameters**

- `json` ([`JsonOut`](../../OneGame.Core/OneGame.Core/JsonOut.md))

## Operators

| Member | Description |
| --- | --- |
| [`operator !=(VehicleRef, VehicleRef)`](#m-operator-vehicleref-vehicleref) | Two references differ when they name different vehicles. |
| [`operator ==(VehicleRef, VehicleRef)`](#m-operator-vehicleref-vehicleref-2) | Two references are equal when they name the same vehicle. |

### <a id="m-operator-vehicleref-vehicleref"></a>operator !=(VehicleRef, VehicleRef)

```csharp
public static bool operator !=(VehicleRef left, VehicleRef right);
```

Two references differ when they name different vehicles.

**Parameters**

- `left` ([`VehicleRef`](VehicleRef.md))
- `right` ([`VehicleRef`](VehicleRef.md))

**Returns:** `bool`

### <a id="m-operator-vehicleref-vehicleref-2"></a>operator ==(VehicleRef, VehicleRef)

```csharp
public static bool operator ==(VehicleRef left, VehicleRef right);
```

Two references are equal when they name the same vehicle.

**Parameters**

- `left` ([`VehicleRef`](VehicleRef.md))
- `right` ([`VehicleRef`](VehicleRef.md))

**Returns:** `bool`
