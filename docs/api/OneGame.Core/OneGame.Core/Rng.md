[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# Rng class

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public sealed class Rng
```

A named, seedable random stream (xoshiro256\*\*). Sim code draws randomness only from streams obtained from [`RngStreams`](RngStreams.md), so the draws are part of the world's state: they are in snapshots and in the state hash, and replaying the same commands redraws the same numbers.

## Properties

| Member | Description |
| --- | --- |
| [`Name`](#m-name) | The stream's name. |

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The stream's name.

**Type:** `string`

## Methods

| Member | Description |
| --- | --- |
| [`Chance(Fixed)`](#m-chance-fixed) | True with the given probability; 0 is never and 1 is always. |
| [`NextBool()`](#m-nextbool) | A random bool. |
| [`NextFixed()`](#m-nextfixed) | A random [`Fixed`](Fixed.md) in \[0, 1). |
| [`NextFixed(Fixed, Fixed)`](#m-nextfixed-fixed-fixed) | A random [`Fixed`](Fixed.md) in \[`min`, `max`). |
| [`NextInt(int)`](#m-nextint-int) | A random int in \[0, `maxExclusive`), without modulo bias. |
| [`NextInt(int, int)`](#m-nextint-int-int) | A random int in \[`min`, `maxExclusive`). |
| [`NextUInt64()`](#m-nextuint64) | The next 64 random bits. |

### <a id="m-chance-fixed"></a>Chance(Fixed)

```csharp
public bool Chance(Fixed probability);
```

True with the given probability; 0 is never and 1 is always.

**Parameters**

- `probability` ([`Fixed`](Fixed.md))

**Returns:** `bool`

### <a id="m-nextbool"></a>NextBool()

```csharp
public bool NextBool();
```

A random bool.

**Returns:** `bool`

### <a id="m-nextfixed"></a>NextFixed()

```csharp
public Fixed NextFixed();
```

A random [`Fixed`](Fixed.md) in \[0, 1).

**Returns:** [`Fixed`](Fixed.md)

### <a id="m-nextfixed-fixed-fixed"></a>NextFixed(Fixed, Fixed)

```csharp
public Fixed NextFixed(Fixed min, Fixed max);
```

A random [`Fixed`](Fixed.md) in \[`min`, `max`).

**Parameters**

- `min` ([`Fixed`](Fixed.md))
- `max` ([`Fixed`](Fixed.md))

**Returns:** [`Fixed`](Fixed.md)

### <a id="m-nextint-int"></a>NextInt(int)

```csharp
public int NextInt(int maxExclusive);
```

A random int in \[0, `maxExclusive`), without modulo bias.

**Parameters**

- `maxExclusive` (`int`)

**Returns:** `int`

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): `maxExclusive` is not positive.

### <a id="m-nextint-int-int"></a>NextInt(int, int)

```csharp
public int NextInt(int min, int maxExclusive);
```

A random int in \[`min`, `maxExclusive`).

**Parameters**

- `min` (`int`)
- `maxExclusive` (`int`)

**Returns:** `int`

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The range is empty.

### <a id="m-nextuint64"></a>NextUInt64()

```csharp
public ulong NextUInt64();
```

The next 64 random bits.

**Returns:** `ulong`
