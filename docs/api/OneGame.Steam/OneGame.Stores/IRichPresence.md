[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Stores

# IRichPresence interface

**Namespace:** `OneGame.Stores`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)

```csharp
public interface IRichPresence
```

Rich presence: key/value text friends see next to the player's name.

## Methods

| Member | Description |
| --- | --- |
| [`Clear()`](#m-clear) | Removes every key this game set. |
| [`Update(string, string)`](#m-update-string-string) | Sets a key. |

### <a id="m-clear"></a>Clear()

```csharp
StoreResult Clear();
```

Removes every key this game set.

**Returns:** [`StoreResult`](StoreResult.md)

### <a id="m-update-string-string"></a>Update(string, string)

```csharp
StoreResult Update(string key, string value);
```

Sets a key. The key `status` is shown as the player's status line. An empty value removes the key.

**Parameters**

- `key` (`string`)
- `value` (`string`)

**Returns:** [`StoreResult`](StoreResult.md)
