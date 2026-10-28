[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Stores

# IAchievements interface

**Namespace:** `OneGame.Stores`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)

```csharp
public interface IAchievements
```

Achievements, named by the id configured in the store's partner site.

## Events

| Member | Description |
| --- | --- |
| [`Stored`](#m-stored) | Raised from [`IStorePlatform.Pump()`](IStorePlatform.md#m-pump) when the store confirms an achievement was saved. |

### <a id="m-stored"></a>Stored

```csharp
event Action<string>? Stored;
```

Raised from [`IStorePlatform.Pump()`](IStorePlatform.md#m-pump) when the store confirms an achievement was saved.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)`<string>?`

## Methods

| Member | Description |
| --- | --- |
| [`Clear(string)`](#m-clear-string) | Relocks an achievement. |
| [`ShowProgress(string, uint, uint)`](#m-showprogress-string-uint-uint) | Shows "n of max" progress toward an achievement. |
| [`TryGetUnlocked(string, out bool)`](#m-trygetunlocked-string-bool) | Reads whether an achievement is unlocked. |
| [`Unlock(string)`](#m-unlock-string) | Unlocks an achievement and asks the store to save it. |

### <a id="m-clear-string"></a>Clear(string)

```csharp
StoreResult Clear(string id);
```

Relocks an achievement. For development and tests.

**Parameters**

- `id` (`string`)

**Returns:** [`StoreResult`](StoreResult.md)

### <a id="m-showprogress-string-uint-uint"></a>ShowProgress(string, uint, uint)

```csharp
StoreResult ShowProgress(string id, uint current, uint max);
```

Shows "n of max" progress toward an achievement. Progress itself is kept in a stat the game sets.

**Parameters**

- `id` (`string`)
- `current` (`uint`)
- `max` (`uint`)

**Returns:** [`StoreResult`](StoreResult.md)

### <a id="m-trygetunlocked-string-bool"></a>TryGetUnlocked(string, out bool)

```csharp
StoreResult TryGetUnlocked(string id, out bool unlocked);
```

Reads whether an achievement is unlocked.

**Parameters**

- `id` (`string`)
- `unlocked` (`bool`)

**Returns:** [`StoreResult`](StoreResult.md)

### <a id="m-unlock-string"></a>Unlock(string)

```csharp
StoreResult Unlock(string id);
```

Unlocks an achievement and asks the store to save it. Idempotent.

**Parameters**

- `id` (`string`)

**Returns:** [`StoreResult`](StoreResult.md)
