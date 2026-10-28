[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# SyncLimits static class

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public static class SyncLimits
```

Hard limits applied while decoding, so a hostile input cannot make the decoder allocate without bound.

## Fields

| Member | Description |
| --- | --- |
| [`MaxCommandBytes`](#m-maxcommandbytes) | Largest command payload, in bytes. |
| [`MaxEntities`](#m-maxentities) | Largest number of entities in a snapshot. |
| [`MaxListCount`](#m-maxlistcount) | Largest element count of a synced list. |
| [`MaxNamed`](#m-maxnamed) | Largest number of random streams or tables in a snapshot. |
| [`MaxStringBytes`](#m-maxstringbytes) | Longest string, in UTF-8 bytes. |
| [`MaxTableRows`](#m-maxtablerows) | Largest row count of a hot-data table. |

### <a id="m-maxcommandbytes"></a>MaxCommandBytes

```csharp
public const int MaxCommandBytes = 65536;
```

Largest command payload, in bytes.

**Type:** `int`

### <a id="m-maxentities"></a>MaxEntities

```csharp
public const int MaxEntities = 4194304;
```

Largest number of entities in a snapshot.

**Type:** `int`

### <a id="m-maxlistcount"></a>MaxListCount

```csharp
public const int MaxListCount = 65536;
```

Largest element count of a synced list.

**Type:** `int`

### <a id="m-maxnamed"></a>MaxNamed

```csharp
public const int MaxNamed = 4096;
```

Largest number of random streams or tables in a snapshot.

**Type:** `int`

### <a id="m-maxstringbytes"></a>MaxStringBytes

```csharp
public const int MaxStringBytes = 65536;
```

Longest string, in UTF-8 bytes.

**Type:** `int`

### <a id="m-maxtablerows"></a>MaxTableRows

```csharp
public const int MaxTableRows = 4194304;
```

Largest row count of a hot-data table.

**Type:** `int`
