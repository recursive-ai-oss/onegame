[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# Timed&lt;T&gt; struct

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Implements:** [`ISyncValue`](ISyncValue-1.md)&lt;[`Timed`](Timed-1.md)`<T>>`, [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`Timed`](Timed-1.md)`<T>>`

```csharp
public readonly struct Timed<T> : ISyncValue<Timed<T>>, IEquatable<Timed<T>> where T : ISyncValue<T>, ILerp<T>
```

A value that moves from one value to another over a span of ticks. The authority writes it once; every node evaluates [`Timed<T>.ValueAt()`](Timed-1.md#m-valueat-long) for the tick it is showing, so nothing is sent per tick. Evaluation is integer-only and identical everywhere.

**Type parameters**

- `T`: The value type: [`Fixed`](Fixed.md), [`FVec2`](FVec2.md) or another interpolatable synced value.

## Constructors

| Member | Description |
| --- | --- |
| [`Timed(T, T, long, long)`](#m-ctor-t-t-long-long) | Creates a timed value. |

### <a id="m-ctor-t-t-long-long"></a>Timed(T, T, long, long)

```csharp
public Timed(T from, T to, long startTick, long endTick);
```

Creates a timed value.

**Parameters**

- `from` (`T`): The value at `startTick` and before.
- `to` (`T`): The value at `endTick` and after.
- `startTick` (`long`): The tick the move starts.
- `endTick` (`long`): The tick the move ends.

## Properties

| Member | Description |
| --- | --- |
| [`EndTick`](#m-endtick) | The tick the move ends. |
| [`From`](#m-from) | The start value. |
| [`StartTick`](#m-starttick) | The tick the move starts. |
| [`To`](#m-to) | The end value. |

### <a id="m-endtick"></a>EndTick

```csharp
public long EndTick { get; }
```

The tick the move ends.

**Type:** `long`

### <a id="m-from"></a>From

```csharp
public T From { get; }
```

The start value.

**Type:** `T`

### <a id="m-starttick"></a>StartTick

```csharp
public long StartTick { get; }
```

The tick the move starts.

**Type:** `long`

### <a id="m-to"></a>To

```csharp
public T To { get; }
```

The end value.

**Type:** `T`

## Methods

| Member | Description |
| --- | --- |
| [`Equals(Timed<T>)`](#m-equals-timed-t) |  |
| [`Equals(object?)`](#m-equals-object) |  |
| [`GetHashCode()`](#m-gethashcode) |  |
| [`ValueAt(long)`](#m-valueat-long) | The value at `tick`: [`Timed<T>.From`](Timed-1.md#m-from) before the start, [`Timed<T>.To`](Timed-1.md#m-to) from the end, a linear blend between. |
| [`ValueAt(long, Fixed)`](#m-valueat-long-fixed) | The value `fraction` of the way from `tick` to the next tick, for a view that draws between ticks (pass the frame driver's alpha as a [`Fixed`](Fixed.md)). |
| [`Write(SyncWriter)`](#m-write-syncwriter) | Writes the value in the sync format. |
| [`WriteJson(JsonOut)`](#m-writejson-jsonout) | Writes the value as JSON for inspection. |

### <a id="m-equals-timed-t"></a>Equals(Timed&lt;T&gt;)

```csharp
public bool Equals(Timed<T> other);
```

**Parameters**

- `other` ([`Timed`](Timed-1.md)`<T>`)

**Returns:** `bool`

### <a id="m-equals-object"></a>Equals(object?)

```csharp
public override bool Equals(object? obj);
```

**Parameters**

- `obj` (`object?`)

**Returns:** `bool`

### <a id="m-gethashcode"></a>GetHashCode()

```csharp
public override int GetHashCode();
```

**Returns:** `int`

### <a id="m-valueat-long"></a>ValueAt(long)

```csharp
public T ValueAt(long tick);
```

The value at `tick`: [`Timed<T>.From`](Timed-1.md#m-from) before the start, [`Timed<T>.To`](Timed-1.md#m-to) from the end, a linear blend between.

**Parameters**

- `tick` (`long`)

**Returns:** `T`

### <a id="m-valueat-long-fixed"></a>ValueAt(long, Fixed)

```csharp
public T ValueAt(long tick, Fixed fraction);
```

The value `fraction` of the way from `tick` to the next tick, for a view that draws between ticks (pass the frame driver's alpha as a [`Fixed`](Fixed.md)). Remote replicas use it to move smoothly while nothing is sent per tick.

**Parameters**

- `tick` (`long`): The last tick that ran.
- `fraction` ([`Fixed`](Fixed.md)): From 0 to just under 1.

**Returns:** `T`

### <a id="m-write-syncwriter"></a>Write(SyncWriter)

```csharp
public void Write(SyncWriter writer);
```

Writes the value in the sync format.

**Parameters**

- `writer` ([`SyncWriter`](SyncWriter.md))

### <a id="m-writejson-jsonout"></a>WriteJson(JsonOut)

```csharp
public void WriteJson(JsonOut json);
```

Writes the value as JSON for inspection.

**Parameters**

- `json` ([`JsonOut`](JsonOut.md))

## Operators

| Member | Description |
| --- | --- |
| [`operator !=(Timed<T>, Timed<T>)`](#m-operator-timed-t-timed-t) | Inequality of any part. |
| [`operator ==(Timed<T>, Timed<T>)`](#m-operator-timed-t-timed-t-2) | Equality of all four parts. |

### <a id="m-operator-timed-t-timed-t"></a>operator !=(Timed&lt;T&gt;, Timed&lt;T&gt;)

```csharp
public static bool operator !=(Timed<T> a, Timed<T> b);
```

Inequality of any part.

**Parameters**

- `a` ([`Timed`](Timed-1.md)`<T>`)
- `b` ([`Timed`](Timed-1.md)`<T>`)

**Returns:** `bool`

### <a id="m-operator-timed-t-timed-t-2"></a>operator ==(Timed&lt;T&gt;, Timed&lt;T&gt;)

```csharp
public static bool operator ==(Timed<T> a, Timed<T> b);
```

Equality of all four parts.

**Parameters**

- `a` ([`Timed`](Timed-1.md)`<T>`)
- `b` ([`Timed`](Timed-1.md)`<T>`)

**Returns:** `bool`
