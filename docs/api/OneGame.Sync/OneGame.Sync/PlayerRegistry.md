[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# PlayerRegistry class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public sealed class PlayerRegistry
```

Gives each authenticated key a stable small player id, so a client that reconnects (a new connection, the same key) is the same player. The ids are what [`Trust.As()`](Trust.md#m-as-uint-uint) takes. A game owns one registry per authority and saves it with its own data ([`PlayerRegistry.Save()`](PlayerRegistry.md#m-save)), or the ids start over when the process does.

A key proves a key, not a person: anyone can make keys at will, and a handshake with a fresh key succeeds. So the registry is bounded, and it forgets the right keys. A key is *provisional* when first seen and becomes *active* when the game calls [`PlayerRegistry.Activate()`](PlayerRegistry.md#m-activate-uint) for something a spammer cannot mass-produce (the player played for a while, finished the tutorial, linked a platform account). When the registry is full, a new key replaces the oldest provisional one; active players are never replaced, so key spam cannot lock real players out, and cannot take away anyone who earned a place. A returning provisional key that was forgotten simply gets a new id.

## Constructors

| Member | Description |
| --- | --- |
| [`PlayerRegistry(uint, int)`](#m-ctor-uint-int) | Creates an empty registry that hands out ids from `firstId` upward. |

### <a id="m-ctor-uint-int"></a>PlayerRegistry(uint, int)

```csharp
public PlayerRegistry(uint firstId = 1, int maxPlayers = 4194304);
```

Creates an empty registry that hands out ids from `firstId` upward.

**Parameters**

- `firstId` (`uint`): The first player id; ids start at 1 by default because 0 means no player.
- `maxPlayers` (`int`): How many keys it will remember. Set it to what the game can hold. When it is reached, a new key replaces the oldest provisional one; if every key is active, [`PlayerRegistry.TryPlayerFor()`](PlayerRegistry.md#m-tryplayerfor-peerkey-uint-bool) refuses and [`PlayerRegistry.PlayerFor()`](PlayerRegistry.md#m-playerfor-peerkey-bool) throws [`PlayerRegistryFullException`](PlayerRegistryFullException.md).

## Properties

| Member | Description |
| --- | --- |
| [`ActiveCount`](#m-activecount) | Players that were activated. |
| [`Count`](#m-count) | Players known so far, provisional and active. |
| [`Version`](#m-version) | Counts every change that [`PlayerRegistry.Save()`](PlayerRegistry.md#m-save) would write, so a game can tell whether the registry needs saving. |

### <a id="m-activecount"></a>ActiveCount

```csharp
public int ActiveCount { get; }
```

Players that were activated.

**Type:** `int`

### <a id="m-count"></a>Count

```csharp
public int Count { get; }
```

Players known so far, provisional and active.

**Type:** `int`

### <a id="m-version"></a>Version

```csharp
public long Version { get; }
```

Counts every change that [`PlayerRegistry.Save()`](PlayerRegistry.md#m-save) would write, so a game can tell whether the registry needs saving.

**Type:** `long`

## Methods

| Member | Description |
| --- | --- |
| [`Activate(uint)`](#m-activate-uint) | Marks a player as one worth keeping: it is never replaced to make room for a new key. |
| [`IsActive(uint)`](#m-isactive-uint) | Whether the player was activated. |
| [`PlayerFor(PeerKey, out bool)`](#m-playerfor-peerkey-bool) | The player of `key`, adding it (provisionally) on first sight; the registry never holds more than its limit. |
| [`Save()`](#m-save) | Serialises the registry: next id, count, then (id, key, active) entries in id order. |
| [`TryGetKey(uint, out PeerKey)`](#m-trygetkey-uint-peerkey) | The key a player id was issued to. |
| [`TryGetPlayer(PeerKey, out uint)`](#m-trygetplayer-peerkey-uint) | The player of `key` if it has been seen. |
| [`TryLoad(ReadOnlySpan<byte>, out PlayerRegistry, int)`](#m-tryload-readonlyspan-byte-playerregistry-int) | Reads what [`PlayerRegistry.Save()`](PlayerRegistry.md#m-save) wrote. |
| [`TryPlayerFor(PeerKey, out uint, out bool)`](#m-tryplayerfor-peerkey-uint-bool) | The player of `key`, adding it (provisionally) on first sight. |

### <a id="m-activate-uint"></a>Activate(uint)

```csharp
public bool Activate(uint player);
```

Marks a player as one worth keeping: it is never replaced to make room for a new key. Call it when the player has done something a spammer cannot do at scale.

**Parameters**

- `player` (`uint`): A player id from this registry.

**Returns:** `bool` — True if the player exists (activating twice is fine).

### <a id="m-isactive-uint"></a>IsActive(uint)

```csharp
public bool IsActive(uint player);
```

Whether the player was activated.

**Parameters**

- `player` (`uint`): A player id.

**Returns:** `bool`

### <a id="m-playerfor-peerkey-bool"></a>PlayerFor(PeerKey, out bool)

```csharp
public uint PlayerFor(PeerKey key, out bool isNew);
```

The player of `key`, adding it (provisionally) on first sight; the registry never holds more than its limit.

**Parameters**

- `key` ([`PeerKey`](../../OneGame.Core/OneGame.Core/PeerKey.md)): An authenticated key.
- `isNew` (`bool`): True if this call added the player.

**Returns:** `uint`

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): `key` is empty (an unauthenticated peer has no identity to register).
- [`PlayerRegistryFullException`](PlayerRegistryFullException.md): The registry is full of active players; use [`PlayerRegistry.TryPlayerFor()`](PlayerRegistry.md#m-tryplayerfor-peerkey-uint-bool) to refuse the key instead.

### <a id="m-save"></a>Save()

```csharp
public byte[] Save();
```

Serialises the registry: next id, count, then (id, key, active) entries in id order.

**Returns:** `byte[]`

### <a id="m-trygetkey-uint-peerkey"></a>TryGetKey(uint, out PeerKey)

```csharp
public bool TryGetKey(uint player, out PeerKey key);
```

The key a player id was issued to.

**Parameters**

- `player` (`uint`): A player id.
- `key` ([`PeerKey`](../../OneGame.Core/OneGame.Core/PeerKey.md)): The key when this returns true.

**Returns:** `bool`

### <a id="m-trygetplayer-peerkey-uint"></a>TryGetPlayer(PeerKey, out uint)

```csharp
public bool TryGetPlayer(PeerKey key, out uint player);
```

The player of `key` if it has been seen.

**Parameters**

- `key` ([`PeerKey`](../../OneGame.Core/OneGame.Core/PeerKey.md)): An authenticated key.
- `player` (`uint`): The id when this returns true.

**Returns:** `bool`

### <a id="m-tryload-readonlyspan-byte-playerregistry-int"></a>TryLoad(ReadOnlySpan&lt;byte&gt;, out PlayerRegistry, int)

```csharp
public static bool TryLoad(ReadOnlySpan<byte> data, out PlayerRegistry registry, int maxPlayers = 4194304);
```

Reads what [`PlayerRegistry.Save()`](PlayerRegistry.md#m-save) wrote. A damaged or oversize file returns false and changes nothing.

**Parameters**

- `data` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`): The saved bytes.
- `registry` ([`PlayerRegistry`](PlayerRegistry.md)): The registry when this returns true.
- `maxPlayers` (`int`): The limit the loaded registry keeps (see the constructor).

**Returns:** `bool`

### <a id="m-tryplayerfor-peerkey-uint-bool"></a>TryPlayerFor(PeerKey, out uint, out bool)

```csharp
public bool TryPlayerFor(PeerKey key, out uint player, out bool isNew);
```

The player of `key`, adding it (provisionally) on first sight. Returns false instead of adding a key when the registry is full of active players.

**Parameters**

- `key` ([`PeerKey`](../../OneGame.Core/OneGame.Core/PeerKey.md)): An authenticated key.
- `player` (`uint`): The player id when this returns true.
- `isNew` (`bool`): True if this call added the player.

**Returns:** `bool`

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): `key` is empty (an unauthenticated peer has no identity to register).
