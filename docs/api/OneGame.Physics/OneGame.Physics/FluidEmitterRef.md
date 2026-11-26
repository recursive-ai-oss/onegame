[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# FluidEmitterRef struct

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)  
**Implements:** [`ISyncValue`](../../OneGame.Core/OneGame.Core/ISyncValue-1.md)&lt;[`FluidEmitterRef`](FluidEmitterRef.md)&gt;, [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`FluidEmitterRef`](FluidEmitterRef.md)&gt;

```csharp
public readonly struct FluidEmitterRef : ISyncValue<FluidEmitterRef>, IEquatable<FluidEmitterRef>
```

A reference to an emitter of a fluid. It is checked: after [`PhysicsSpace2D.Remove()`](PhysicsSpace2D.md#m-remove-fluidemitterref) it names nothing, even if the slot is used again. An emitter made in this process stays reachable through it across restores into the same space; a space that was only loaded from a save cannot reach emitters it did not create (see `docs/physics.md`).

## Properties

| Member | Description |
| --- | --- |
| [`Fluid`](#m-fluid) | The fluid the emitter belongs to. |
| [`Index`](#m-index) | The emitter's slot in its fluid. |
| [`IsNull`](#m-isnull) | True for the default value, which names nothing. |

### <a id="m-fluid"></a>Fluid

```csharp
public FluidRef Fluid { get; }
```

The fluid the emitter belongs to.

**Type:** [`FluidRef`](FluidRef.md)

### <a id="m-index"></a>Index

```csharp
public int Index { get; }
```

The emitter's slot in its fluid.

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
| [`Equals(FluidEmitterRef)`](#m-equals-fluidemitterref) |  |
| [`Equals(object?)`](#m-equals-object) |  |
| [`GetHashCode()`](#m-gethashcode) |  |
| [`Read(ref SyncReader)`](#m-read-syncreader) | Reads a value written by [`ISyncValue<TSelf>.Write()`](../../OneGame.Core/OneGame.Core/ISyncValue-1.md#m-write-syncwriter). |
| [`ToString()`](#m-tostring) |  |
| [`Write(SyncWriter)`](#m-write-syncwriter) | Writes the value in the sync format. |
| [`WriteJson(JsonOut)`](#m-writejson-jsonout) | Writes the value as JSON for inspection. |

### <a id="m-equals-fluidemitterref"></a>Equals(FluidEmitterRef)

```csharp
public bool Equals(FluidEmitterRef other);
```

Implements [`IEquatable<FluidEmitterRef>.Equals()`](https://learn.microsoft.com/dotnet/api/system.iequatable-1.equals).

**Parameters**

- `other` ([`FluidEmitterRef`](FluidEmitterRef.md))

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
public static FluidEmitterRef Read(ref SyncReader reader);
```

Reads a value written by [`ISyncValue<TSelf>.Write()`](../../OneGame.Core/OneGame.Core/ISyncValue-1.md#m-write-syncwriter). Problems are recorded on the reader, and a default value is returned.

**Parameters**

- `reader` ([`SyncReader`](../../OneGame.Core/OneGame.Core/SyncReader.md))

**Returns:** [`FluidEmitterRef`](FluidEmitterRef.md)

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
| [`operator !=(FluidEmitterRef, FluidEmitterRef)`](#m-operator-fluidemitterref-fluidemitterref) | Two references differ when they name different emitters. |
| [`operator ==(FluidEmitterRef, FluidEmitterRef)`](#m-operator-fluidemitterref-fluidemitterref-2) | Two references are equal when they name the same emitter. |

### <a id="m-operator-fluidemitterref-fluidemitterref"></a>operator !=(FluidEmitterRef, FluidEmitterRef)

```csharp
public static bool operator !=(FluidEmitterRef left, FluidEmitterRef right);
```

Two references differ when they name different emitters.

**Parameters**

- `left` ([`FluidEmitterRef`](FluidEmitterRef.md))
- `right` ([`FluidEmitterRef`](FluidEmitterRef.md))

**Returns:** `bool`

### <a id="m-operator-fluidemitterref-fluidemitterref-2"></a>operator ==(FluidEmitterRef, FluidEmitterRef)

```csharp
public static bool operator ==(FluidEmitterRef left, FluidEmitterRef right);
```

Two references are equal when they name the same emitter.

**Parameters**

- `left` ([`FluidEmitterRef`](FluidEmitterRef.md))
- `right` ([`FluidEmitterRef`](FluidEmitterRef.md))

**Returns:** `bool`
