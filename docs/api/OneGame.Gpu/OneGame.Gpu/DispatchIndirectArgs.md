[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# DispatchIndirectArgs struct

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`DispatchIndirectArgs`](DispatchIndirectArgs.md)&gt;

```csharp
public readonly struct DispatchIndirectArgs : IEquatable<DispatchIndirectArgs>
```

Arguments of one indirect dispatch, as stored in a buffer.

## Constructors

| Member | Description |
| --- | --- |
| [`DispatchIndirectArgs(uint, uint, uint)`](#m-ctor-uint-uint-uint) | Arguments of one indirect dispatch, as stored in a buffer. |

### <a id="m-ctor-uint-uint-uint"></a>DispatchIndirectArgs(uint, uint, uint)

```csharp
public DispatchIndirectArgs(uint X, uint Y, uint Z);
```

Arguments of one indirect dispatch, as stored in a buffer.

**Parameters**

- `X` (`uint`): Workgroups in X.
- `Y` (`uint`): Workgroups in Y.
- `Z` (`uint`): Workgroups in Z.

## Fields

| Member | Description |
| --- | --- |
| [`Size`](#m-size) | Size in bytes. |

### <a id="m-size"></a>Size

```csharp
public const int Size = 12;
```

Size in bytes.

**Type:** `int`

## Properties

| Member | Description |
| --- | --- |
| [`X`](#m-x) | Workgroups in X. |
| [`Y`](#m-y) | Workgroups in Y. |
| [`Z`](#m-z) | Workgroups in Z. |

### <a id="m-x"></a>X

```csharp
public uint X { get; init; }
```

Workgroups in X.

**Type:** `uint`

### <a id="m-y"></a>Y

```csharp
public uint Y { get; init; }
```

Workgroups in Y.

**Type:** `uint`

### <a id="m-z"></a>Z

```csharp
public uint Z { get; init; }
```

Workgroups in Z.

**Type:** `uint`
