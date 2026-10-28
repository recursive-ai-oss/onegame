[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# LoadResult record

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`LoadResult`](LoadResult.md)&gt;

```csharp
public sealed record LoadResult : IEquatable<LoadResult>
```

What [`SaveFile.Load()`](SaveFile.md#m-load-readonlyspan-byte-world) found and did.

## Constructors

| Member | Description |
| --- | --- |
| [`LoadResult(SaveStatus, SyncError, SaveHeader?, bool, bool)`](#m-ctor-savestatus-syncerror-saveheader-bool-bool) | What [`SaveFile.Load()`](SaveFile.md#m-load-readonlyspan-byte-world) found and did. |

### <a id="m-ctor-savestatus-syncerror-saveheader-bool-bool"></a>LoadResult(SaveStatus, SyncError, SaveHeader?, bool, bool)

```csharp
public LoadResult(SaveStatus Status, SyncError Detail, SaveHeader? Header, bool Migrated, bool HashVerified);
```

What [`SaveFile.Load()`](SaveFile.md#m-load-readonlyspan-byte-world) found and did.

**Parameters**

- `Status` ([`SaveStatus`](SaveStatus.md)): The outcome.
- `Detail` ([`SyncError`](../../OneGame.Core/OneGame.Core/SyncError.md)): The decoder's error, when there was one.
- `Header` ([`SaveHeader`](SaveHeader.md)?): The header, if it could be read.
- `Migrated` (`bool`): True when at least one entity type was written with an older schema and went through its migration.
- `HashVerified` (`bool`): True when the loaded world hashed to the recorded hash. Not checkable after a migration: the state layout changed.

## Properties

| Member | Description |
| --- | --- |
| [`Detail`](#m-detail) | The decoder's error, when there was one. |
| [`HashVerified`](#m-hashverified) | True when the loaded world hashed to the recorded hash. |
| [`Header`](#m-header) | The header, if it could be read. |
| [`Migrated`](#m-migrated) | True when at least one entity type was written with an older schema and went through its migration. |
| [`Status`](#m-status) | The outcome. |

### <a id="m-detail"></a>Detail

```csharp
public SyncError Detail { get; init; }
```

The decoder's error, when there was one.

**Type:** [`SyncError`](../../OneGame.Core/OneGame.Core/SyncError.md)

### <a id="m-hashverified"></a>HashVerified

```csharp
public bool HashVerified { get; init; }
```

True when the loaded world hashed to the recorded hash. Not checkable after a migration: the state layout changed.

**Type:** `bool`

### <a id="m-header"></a>Header

```csharp
public SaveHeader? Header { get; init; }
```

The header, if it could be read.

**Type:** [`SaveHeader`](SaveHeader.md)?

### <a id="m-migrated"></a>Migrated

```csharp
public bool Migrated { get; init; }
```

True when at least one entity type was written with an older schema and went through its migration.

**Type:** `bool`

### <a id="m-status"></a>Status

```csharp
public SaveStatus Status { get; init; }
```

The outcome.

**Type:** [`SaveStatus`](SaveStatus.md)
