[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# FluidDrainRef struct

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)  
**Implements:** [`ISyncValue`](../../OneGame.Core/OneGame.Core/ISyncValue-1.md)&lt;[`FluidDrainRef`](FluidDrainRef.md)&gt;, [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`FluidDrainRef`](FluidDrainRef.md)&gt;

```csharp
public readonly struct FluidDrainRef : ISyncValue<FluidDrainRef>, IEquatable<FluidDrainRef>
```

A reference to a drain of a fluid, checked like [`FluidEmitterRef`](FluidEmitterRef.md).

## Properties

| Member | Description |
| --- | --- |
| [`Fluid`](#m-fluid) | The fluid the drain belongs to. |
| [`Index`](#m-index) | The drain's slot in its fluid. |
| [`IsNull`](#m-isnull) | True for the default value, which names nothing. |

### <a id="m-fluid"></a>Fluid

```csharp
public FluidRef Fluid { get; }
```

The fluid the drain belongs to.

**Type:** [`FluidRef`](FluidRef.md)

### <a id="m-index"></a>Index

```csharp
public int Index { get; }
```

The drain's slot in its fluid.

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
| [`Equals(FluidDrainRef)`](#m-equals-fluiddrainref) |  |
| [`Equals(object?)`](#m-equals-object) |  |
| [`GetHashCode()`](#m-gethashcode) |  |
| [`Read(ref SyncReader)`](#m-read-syncreader) | Reads a value written by [`ISyncValue<TSelf>.Write()`](../../OneGame.Core/OneGame.Core/ISyncValue-1.md#m-write-syncwriter). |
| [`ToString()`](#m-tostring) |  |
| [`Write(SyncWriter)`](#m-write-syncwriter) | Writes the value in the sync format. |
| [`WriteJson(JsonOut)`](#m-writejson-jsonout) | Writes the value as JSON for inspection. |

### <a id="m-equals-fluiddrainref"></a>Equals(FluidDrainRef)

```csharp
public bool Equals(FluidDrainRef other);
```

Implements [`IEquatable<FluidDrainRef>.Equals()`](https://learn.microsoft.com/dotnet/api/system.iequatable-1.equals).

**Parameters**

- `other` ([`FluidDrainRef`](FluidDrainRef.md))

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
public static FluidDrainRef Read(ref SyncReader reader);
```

Reads a value written by [`ISyncValue<TSelf>.Write()`](../../OneGame.Core/OneGame.Core/ISyncValue-1.md#m-write-syncwriter). Problems are recorded on the reader, and a default value is returned.

**Parameters**

- `reader` ([`SyncReader`](../../OneGame.Core/OneGame.Core/SyncReader.md))

**Returns:** [`FluidDrainRef`](FluidDrainRef.md)

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
| [`operator !=(FluidDrainRef, FluidDrainRef)`](#m-operator-fluiddrainref-fluiddrainref) | Two references differ when they name different drains. |
| [`operator ==(FluidDrainRef, FluidDrainRef)`](#m-operator-fluiddrainref-fluiddrainref-2) | Two references are equal when they name the same drain. |

### <a id="m-operator-fluiddrainref-fluiddrainref"></a>operator !=(FluidDrainRef, FluidDrainRef)

```csharp
public static bool operator !=(FluidDrainRef left, FluidDrainRef right);
```

Two references differ when they name different drains.

**Parameters**

- `left` ([`FluidDrainRef`](FluidDrainRef.md))
- `right` ([`FluidDrainRef`](FluidDrainRef.md))

**Returns:** `bool`

### <a id="m-operator-fluiddrainref-fluiddrainref-2"></a>operator ==(FluidDrainRef, FluidDrainRef)

```csharp
public static bool operator ==(FluidDrainRef left, FluidDrainRef right);
```

Two references are equal when they name the same drain.

**Parameters**

- `left` ([`FluidDrainRef`](FluidDrainRef.md))
- `right` ([`FluidDrainRef`](FluidDrainRef.md))

**Returns:** `bool`
