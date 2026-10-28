[API reference](../../index.md) › [OneGame.Navigation](../index.md) › OneGame.Navigation

# NavOptions struct

**Namespace:** `OneGame.Navigation`  
**Package:** [`RecursiveAI.OneGame.Navigation`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`NavOptions`](NavOptions.md)&gt;

```csharp
public readonly struct NavOptions : IEquatable<NavOptions>
```

What kind of unit is searching. `default` is one cell wide, eight directions without corner cutting, A\*.

## Constructors

| Member | Description |
| --- | --- |
| [`NavOptions(NavMoves, int, NavAlgorithm)`](#m-ctor-navmoves-int-navalgorithm) | Creates options. |

### <a id="m-ctor-navmoves-int-navalgorithm"></a>NavOptions(NavMoves, int, NavAlgorithm)

```csharp
public NavOptions(NavMoves moves = 0, int agentSize = 1, NavAlgorithm algorithm = 0);
```

Creates options.

**Parameters**

- `moves` ([`NavMoves`](NavMoves.md)): The allowed steps.
- `agentSize` (`int`): Width of the unit in cells, 1 to 255; its position is the top-left cell of the block it fills.
- `algorithm` ([`NavAlgorithm`](NavAlgorithm.md)): The search used by one-shot queries.

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): An argument is out of range.

## Properties

| Member | Description |
| --- | --- |
| [`AgentSize`](#m-agentsize) | Width of the unit in cells. |
| [`Algorithm`](#m-algorithm) | The search used by one-shot queries. |
| [`Default`](#m-default) | One cell wide, eight directions, no corner cutting, A\*. |
| [`Moves`](#m-moves) | The allowed steps. |

### <a id="m-agentsize"></a>AgentSize

```csharp
public int AgentSize { get; }
```

Width of the unit in cells.

**Type:** `int`

### <a id="m-algorithm"></a>Algorithm

```csharp
public NavAlgorithm Algorithm { get; }
```

The search used by one-shot queries.

**Type:** [`NavAlgorithm`](NavAlgorithm.md)

### <a id="m-default"></a>Default

```csharp
public static NavOptions Default { get; }
```

One cell wide, eight directions, no corner cutting, A\*.

**Type:** [`NavOptions`](NavOptions.md)

### <a id="m-moves"></a>Moves

```csharp
public NavMoves Moves { get; }
```

The allowed steps.

**Type:** [`NavMoves`](NavMoves.md)

## Methods

| Member | Description |
| --- | --- |
| [`Equals(NavOptions)`](#m-equals-navoptions) | Equality of every part. |
| [`Equals(object?)`](#m-equals-object) |  |
| [`GetHashCode()`](#m-gethashcode) |  |

### <a id="m-equals-navoptions"></a>Equals(NavOptions)

```csharp
public bool Equals(NavOptions other);
```

Equality of every part.

**Parameters**

- `other` ([`NavOptions`](NavOptions.md))

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

## Operators

| Member | Description |
| --- | --- |
| [`operator !=(NavOptions, NavOptions)`](#m-operator-navoptions-navoptions) | Inequality. |
| [`operator ==(NavOptions, NavOptions)`](#m-operator-navoptions-navoptions-2) | Equality. |

### <a id="m-operator-navoptions-navoptions"></a>operator !=(NavOptions, NavOptions)

```csharp
public static bool operator !=(NavOptions a, NavOptions b);
```

Inequality.

**Parameters**

- `a` ([`NavOptions`](NavOptions.md))
- `b` ([`NavOptions`](NavOptions.md))

**Returns:** `bool`

### <a id="m-operator-navoptions-navoptions-2"></a>operator ==(NavOptions, NavOptions)

```csharp
public static bool operator ==(NavOptions a, NavOptions b);
```

Equality.

**Parameters**

- `a` ([`NavOptions`](NavOptions.md))
- `b` ([`NavOptions`](NavOptions.md))

**Returns:** `bool`
