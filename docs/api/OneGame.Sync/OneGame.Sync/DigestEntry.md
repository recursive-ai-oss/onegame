[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# DigestEntry struct

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`DigestEntry`](DigestEntry.md)&gt;

```csharp
public readonly struct DigestEntry : IEquatable<DigestEntry>
```

One line of a [`StateDigest`](StateDigest.md).

## Constructors

| Member | Description |
| --- | --- |
| [`DigestEntry(DigestKind, ulong, string?, ulong)`](#m-ctor-digestkind-ulong-string-ulong) | One line of a [`StateDigest`](StateDigest.md). |

### <a id="m-ctor-digestkind-ulong-string-ulong"></a>DigestEntry(DigestKind, ulong, string?, ulong)

```csharp
public DigestEntry(DigestKind Kind, ulong Id, string? Name, ulong Hash);
```

One line of a [`StateDigest`](StateDigest.md).

**Parameters**

- `Kind` ([`DigestKind`](DigestKind.md)): What it describes.
- `Id` (`ulong`): The entity id (entities only).
- `Name` (`string?`): The table name (tables only).
- `Hash` (`ulong`): Hash of its state.

## Properties

| Member | Description |
| --- | --- |
| [`Hash`](#m-hash) | Hash of its state. |
| [`Id`](#m-id) | The entity id (entities only). |
| [`Kind`](#m-kind) | What it describes. |
| [`Name`](#m-name) | The table name (tables only). |

### <a id="m-hash"></a>Hash

```csharp
public ulong Hash { get; init; }
```

Hash of its state.

**Type:** `ulong`

### <a id="m-id"></a>Id

```csharp
public ulong Id { get; init; }
```

The entity id (entities only).

**Type:** `ulong`

### <a id="m-kind"></a>Kind

```csharp
public DigestKind Kind { get; init; }
```

What it describes.

**Type:** [`DigestKind`](DigestKind.md)

### <a id="m-name"></a>Name

```csharp
public string? Name { get; init; }
```

The table name (tables only).

**Type:** `string?`
