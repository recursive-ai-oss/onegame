[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# TransferLimits static class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public static class TransferLimits
```

Limits on what one authority transfer may carry. A peer's offer is checked against them before anything is allocated.

## Fields

| Member | Description |
| --- | --- |
| [`MaxBundleBytes`](#m-maxbundlebytes) | Most bytes in the serialized entities of one transfer. |
| [`MaxDone`](#m-maxdone) | Most finished incoming transfer ids a scope's log remembers (the oldest are forgotten first). |
| [`MaxEntities`](#m-maxentities) | Most entities in one transfer. |
| [`MaxOpen`](#m-maxopen) | Most unfinished outgoing transfers a scope's log keeps. |

### <a id="m-maxbundlebytes"></a>MaxBundleBytes

```csharp
public const int MaxBundleBytes = 49152;
```

Most bytes in the serialized entities of one transfer.

**Type:** `int`

### <a id="m-maxdone"></a>MaxDone

```csharp
public const int MaxDone = 1024;
```

Most finished incoming transfer ids a scope's log remembers (the oldest are forgotten first).

**Type:** `int`

### <a id="m-maxentities"></a>MaxEntities

```csharp
public const int MaxEntities = 256;
```

Most entities in one transfer.

**Type:** `int`

### <a id="m-maxopen"></a>MaxOpen

```csharp
public const int MaxOpen = 256;
```

Most unfinished outgoing transfers a scope's log keeps.

**Type:** `int`
