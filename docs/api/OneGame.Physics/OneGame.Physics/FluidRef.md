[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# FluidRef struct

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)  
**Implements:** [`ISyncValue`](../../OneGame.Core/OneGame.Core/ISyncValue-1.md)&lt;[`FluidRef`](FluidRef.md)&gt;, [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`FluidRef`](FluidRef.md)&gt;

```csharp
public readonly struct FluidRef : ISyncValue<FluidRef>, IEquatable<FluidRef>
```

A reference to a fluid, a body-ref-like value that fits a synced property. The default value names nothing.

## Properties

| Member | Description |
| --- | --- |
| [`IsNull`](#m-isnull) | True for the default value, which names nothing (and, as a filter, means every fluid). |
| [`Slot`](#m-slot) | The fluid's slot in the space, 0 for the first fluid added. |

### <a id="m-isnull"></a>IsNull

```csharp
public bool IsNull { get; }
```

True for the default value, which names nothing (and, as a filter, means every fluid).

**Type:** `bool`

### <a id="m-slot"></a>Slot

```csharp
public int Slot { get; }
```

The fluid's slot in the space, 0 for the first fluid added. Particle reads carry the same number ([`FluidParticle.Fluid`](FluidParticle.md#m-fluid)).

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Equals(FluidRef)`](#m-equals-fluidref) |  |
| [`Equals(object?)`](#m-equals-object) |  |
| [`GetHashCode()`](#m-gethashcode) |  |
| [`Read(ref SyncReader)`](#m-read-syncreader) | Reads a value written by [`ISyncValue<TSelf>.Write()`](../../OneGame.Core/OneGame.Core/ISyncValue-1.md#m-write-syncwriter). |
| [`ToString()`](#m-tostring) |  |
| [`Write(SyncWriter)`](#m-write-syncwriter) | Writes the value in the sync format. |
| [`WriteJson(JsonOut)`](#m-writejson-jsonout) | Writes the value as JSON for inspection. |

### <a id="m-equals-fluidref"></a>Equals(FluidRef)

```csharp
public bool Equals(FluidRef other);
```

Implements [`IEquatable<FluidRef>.Equals()`](https://learn.microsoft.com/dotnet/api/system.iequatable-1.equals).

**Parameters**

- `other` ([`FluidRef`](FluidRef.md))

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
public static FluidRef Read(ref SyncReader reader);
```

Reads a value written by [`ISyncValue<TSelf>.Write()`](../../OneGame.Core/OneGame.Core/ISyncValue-1.md#m-write-syncwriter). Problems are recorded on the reader, and a default value is returned.

**Parameters**

- `reader` ([`SyncReader`](../../OneGame.Core/OneGame.Core/SyncReader.md))

**Returns:** [`FluidRef`](FluidRef.md)

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
| [`operator !=(FluidRef, FluidRef)`](#m-operator-fluidref-fluidref) | Two references differ when they name different fluids. |
| [`operator ==(FluidRef, FluidRef)`](#m-operator-fluidref-fluidref-2) | Two references are equal when they name the same fluid. |

### <a id="m-operator-fluidref-fluidref"></a>operator !=(FluidRef, FluidRef)

```csharp
public static bool operator !=(FluidRef left, FluidRef right);
```

Two references differ when they name different fluids.

**Parameters**

- `left` ([`FluidRef`](FluidRef.md))
- `right` ([`FluidRef`](FluidRef.md))

**Returns:** `bool`

### <a id="m-operator-fluidref-fluidref-2"></a>operator ==(FluidRef, FluidRef)

```csharp
public static bool operator ==(FluidRef left, FluidRef right);
```

Two references are equal when they name the same fluid.

**Parameters**

- `left` ([`FluidRef`](FluidRef.md))
- `right` ([`FluidRef`](FluidRef.md))

**Returns:** `bool`
