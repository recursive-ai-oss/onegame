[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# StateDifference record

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`StateDifference`](StateDifference.md)&gt;

```csharp
public sealed record StateDifference : IEquatable<StateDifference>
```

One entity, table or the random streams that differ between expected and actual state.

## Constructors

| Member | Description |
| --- | --- |
| [`StateDifference(DigestKind, ulong, string?, string?, DifferenceChange, ulong, ulong)`](#m-ctor-digestkind-ulong-string-string-differencechange-ulong-ulong) | One entity, table or the random streams that differ between expected and actual state. |

### <a id="m-ctor-digestkind-ulong-string-string-differencechange-ulong-ulong"></a>StateDifference(DigestKind, ulong, string?, string?, DifferenceChange, ulong, ulong)

```csharp
public StateDifference(DigestKind Kind, ulong EntityId, string? Name, string? TypeName, DifferenceChange Change, ulong ExpectedHash, ulong ActualHash);
```

One entity, table or the random streams that differ between expected and actual state.

**Parameters**

- `Kind` ([`DigestKind`](DigestKind.md))
- `EntityId` (`ulong`)
- `Name` (`string?`)
- `TypeName` (`string?`)
- `Change` ([`DifferenceChange`](DifferenceChange.md))
- `ExpectedHash` (`ulong`)
- `ActualHash` (`ulong`)

## Properties

| Member | Description |
| --- | --- |
| [`ActualHash`](#m-actualhash) |  |
| [`Change`](#m-change) |  |
| [`EntityId`](#m-entityid) |  |
| [`ExpectedHash`](#m-expectedhash) |  |
| [`Kind`](#m-kind) |  |
| [`Name`](#m-name) |  |
| [`TypeName`](#m-typename) |  |

### <a id="m-actualhash"></a>ActualHash

```csharp
public ulong ActualHash { get; init; }
```

**Type:** `ulong`

### <a id="m-change"></a>Change

```csharp
public DifferenceChange Change { get; init; }
```

**Type:** [`DifferenceChange`](DifferenceChange.md)

### <a id="m-entityid"></a>EntityId

```csharp
public ulong EntityId { get; init; }
```

**Type:** `ulong`

### <a id="m-expectedhash"></a>ExpectedHash

```csharp
public ulong ExpectedHash { get; init; }
```

**Type:** `ulong`

### <a id="m-kind"></a>Kind

```csharp
public DigestKind Kind { get; init; }
```

**Type:** [`DigestKind`](DigestKind.md)

### <a id="m-name"></a>Name

```csharp
public string? Name { get; init; }
```

**Type:** `string?`

### <a id="m-typename"></a>TypeName

```csharp
public string? TypeName { get; init; }
```

**Type:** `string?`
