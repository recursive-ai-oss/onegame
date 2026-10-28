[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam

# SteamLayout static class

**Namespace:** `OneGame.Steam`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)

```csharp
public static class SteamLayout
```

The packing in effect on this operating system.

## Properties

| Member | Description |
| --- | --- |
| [`Current`](#m-current) | The packing the Steam library for the running operating system uses. |

### <a id="m-current"></a>Current

```csharp
public static SteamPacking Current { get; }
```

The packing the Steam library for the running operating system uses.

**Type:** [`SteamPacking`](SteamPacking.md)

## Methods

| Member | Description |
| --- | --- |
| [`Compute(ReadOnlySpan<SteamField>, SteamPacking, Span<int>)`](#m-compute-readonlyspan-steamfield-steampacking-span-int) | Computes field offsets and the total struct size under C layout rules: each field aligns to the smaller of its own size and the packing, and the struct is padded to its largest field alignment. |
| [`SizeOf<T>(SteamPacking)`](#m-sizeof-t-steampacking) | The size of a callback struct under `packing`. |

### <a id="m-compute-readonlyspan-steamfield-steampacking-span-int"></a>Compute(ReadOnlySpan&lt;SteamField&gt;, SteamPacking, Span&lt;int&gt;)

```csharp
public static int Compute(ReadOnlySpan<SteamField> fields, SteamPacking packing, Span<int> offsets);
```

Computes field offsets and the total struct size under C layout rules: each field aligns to the smaller of its own size and the packing, and the struct is padded to its largest field alignment.

**Parameters**

- `fields` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)&lt;[`SteamField`](SteamField.md)&gt;): Fields in declaration order.
- `packing` ([`SteamPacking`](SteamPacking.md)): The packing to use.
- `offsets` ([`Span`](https://learn.microsoft.com/dotnet/api/system.span-1)`<int>`): Receives one offset per field; must be at least as long as `fields`.

**Returns:** `int` — The struct size in bytes.

### <a id="m-sizeof-t-steampacking"></a>SizeOf&lt;T&gt;(SteamPacking)

```csharp
public static int SizeOf<T>(SteamPacking packing) where T : struct, ISteamCallbackData<T>;
```

The size of a callback struct under `packing`.

**Parameters**

- `packing` ([`SteamPacking`](SteamPacking.md))

**Returns:** `int`
