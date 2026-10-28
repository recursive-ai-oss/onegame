[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Stores

# IStats interface

**Namespace:** `OneGame.Stores`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)

```csharp
public interface IStats
```

Numeric stats, named by the id configured in the store's partner site. Setting only changes the local copy until [`IStats.Commit()`](IStats.md#m-commit).

## Events

| Member | Description |
| --- | --- |
| [`Committed`](#m-committed) | Raised from [`IStorePlatform.Pump()`](IStorePlatform.md#m-pump) with whether the store stored the stats. |

### <a id="m-committed"></a>Committed

```csharp
event Action<bool>? Committed;
```

Raised from [`IStorePlatform.Pump()`](IStorePlatform.md#m-pump) with whether the store stored the stats.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)`<bool>?`

## Methods

| Member | Description |
| --- | --- |
| [`Commit()`](#m-commit) | Sends changed stats and achievements to the store. |
| [`SetFloat(string, float)`](#m-setfloat-string-float) | Sets a floating-point stat. |
| [`SetInt(string, int)`](#m-setint-string-int) | Sets an integer stat. |
| [`TryGetFloat(string, out float)`](#m-trygetfloat-string-float) | Reads a floating-point stat. |
| [`TryGetInt(string, out int)`](#m-trygetint-string-int) | Reads an integer stat. |

### <a id="m-commit"></a>Commit()

```csharp
StoreResult Commit();
```

Sends changed stats and achievements to the store. The store rate-limits this; call at natural breaks (end of a level, return to the menu), not every frame.

**Returns:** [`StoreResult`](StoreResult.md)

### <a id="m-setfloat-string-float"></a>SetFloat(string, float)

```csharp
StoreResult SetFloat(string id, float value);
```

Sets a floating-point stat.

**Parameters**

- `id` (`string`)
- `value` (`float`)

**Returns:** [`StoreResult`](StoreResult.md)

### <a id="m-setint-string-int"></a>SetInt(string, int)

```csharp
StoreResult SetInt(string id, int value);
```

Sets an integer stat.

**Parameters**

- `id` (`string`)
- `value` (`int`)

**Returns:** [`StoreResult`](StoreResult.md)

### <a id="m-trygetfloat-string-float"></a>TryGetFloat(string, out float)

```csharp
StoreResult TryGetFloat(string id, out float value);
```

Reads a floating-point stat.

**Parameters**

- `id` (`string`)
- `value` (`float`)

**Returns:** [`StoreResult`](StoreResult.md)

### <a id="m-trygetint-string-int"></a>TryGetInt(string, out int)

```csharp
StoreResult TryGetInt(string id, out int value);
```

Reads an integer stat.

**Parameters**

- `id` (`string`)
- `value` (`int`)

**Returns:** [`StoreResult`](StoreResult.md)
