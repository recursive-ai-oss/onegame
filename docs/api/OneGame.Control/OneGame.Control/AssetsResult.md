[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# AssetsResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`AssetsResult`](AssetsResult.md)&gt;

```csharp
public sealed record AssetsResult : IEquatable<AssetsResult>
```

Result of the `assets` operation.

## Constructors

| Member | Description |
| --- | --- |
| [`AssetsResult(AssetInfo[], AssetEventInfo[], long, string, string[], int)`](#m-ctor-assetinfo-asseteventinfo-long-string-string-int) | Result of the `assets` operation. |

### <a id="m-ctor-assetinfo-asseteventinfo-long-string-string-int"></a>AssetsResult(AssetInfo\[\], AssetEventInfo\[\], long, string, string\[\], int)

```csharp
public AssetsResult(AssetInfo[] Assets, AssetEventInfo[] Events, long LastSequence, string DataDigest, string[] Changed, int Applied);
```

Result of the `assets` operation.

**Parameters**

- `Assets` ([`AssetInfo`](AssetInfo.md)\[\]): Every loaded asset.
- `Events` ([`AssetEventInfo`](AssetEventInfo.md)\[\]): Events after `since`, oldest first (the last 256 are kept).
- `LastSequence` (`long`): Sequence of the newest event.
- `DataDigest` (`string`): Digest of all data files: equal when the game runs with the same data.
- `Changed` (`string[]`): Paths whose file changed and were read again by this call.
- `Applied` (`int`): How many pending data assets `apply_data` swapped in.

## Properties

| Member | Description |
| --- | --- |
| [`Applied`](#m-applied) | How many pending data assets `apply_data` swapped in. |
| [`Assets`](#m-assets) | Every loaded asset. |
| [`Changed`](#m-changed) | Paths whose file changed and were read again by this call. |
| [`DataDigest`](#m-datadigest) | Digest of all data files: equal when the game runs with the same data. |
| [`Events`](#m-events) | Events after `since`, oldest first (the last 256 are kept). |
| [`LastSequence`](#m-lastsequence) | Sequence of the newest event. |

### <a id="m-applied"></a>Applied

```csharp
public int Applied { get; init; }
```

How many pending data assets `apply_data` swapped in.

**Type:** `int`

### <a id="m-assets"></a>Assets

```csharp
public AssetInfo[] Assets { get; init; }
```

Every loaded asset.

**Type:** [`AssetInfo`](AssetInfo.md)\[\]

### <a id="m-changed"></a>Changed

```csharp
public string[] Changed { get; init; }
```

Paths whose file changed and were read again by this call.

**Type:** `string[]`

### <a id="m-datadigest"></a>DataDigest

```csharp
public string DataDigest { get; init; }
```

Digest of all data files: equal when the game runs with the same data.

**Type:** `string`

### <a id="m-events"></a>Events

```csharp
public AssetEventInfo[] Events { get; init; }
```

Events after `since`, oldest first (the last 256 are kept).

**Type:** [`AssetEventInfo`](AssetEventInfo.md)\[\]

### <a id="m-lastsequence"></a>LastSequence

```csharp
public long LastSequence { get; init; }
```

Sequence of the newest event.

**Type:** `long`
