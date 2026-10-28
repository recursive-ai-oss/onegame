[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# PlayerRegistry class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public sealed class PlayerRegistry
```

Gives each authenticated key a stable small player id, so a client that reconnects (a new connection, the same key) is the same player. The ids are what [`Trust.As()`](Trust.md#m-as-uint-uint) takes. A game owns one registry per authority and saves it with its own data ([`PlayerRegistry.Save()`](PlayerRegistry.md#m-save)), or the ids start over when the process does.

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
- `maxPlayers` (`int`): How many keys it will remember. Anyone can make keys at will, so a server should set this to what its game can hold; [`PlayerRegistry.TryPlayerFor()`](PlayerRegistry.md#m-tryplayerfor-peerkey-uint-bool) then refuses new keys instead of growing without bound.

## Properties

| Member | Description |
| --- | --- |
| [`Count`](#m-count) | Players known so far. |

### <a id="m-count"></a>Count

```csharp
public int Count { get; }
```

Players known so far.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`PlayerFor(PeerKey, out bool)`](#m-playerfor-peerkey-bool) | The player of `key`, adding it on first sight. |
| [`Save()`](#m-save) | Serialises the registry: next id, count, then (id, key) pairs in id order. |
| [`TryGetKey(uint, out PeerKey)`](#m-trygetkey-uint-peerkey) | The key a player id was issued to. |
| [`TryGetPlayer(PeerKey, out uint)`](#m-trygetplayer-peerkey-uint) | The player of `key` if it has been seen. |
| [`TryLoad(ReadOnlySpan<byte>, out PlayerRegistry, int)`](#m-tryload-readonlyspan-byte-playerregistry-int) | Reads what [`PlayerRegistry.Save()`](PlayerRegistry.md#m-save) wrote. |
| [`TryPlayerFor(PeerKey, out uint, out bool)`](#m-tryplayerfor-peerkey-uint-bool) | Like [`PlayerRegistry.PlayerFor()`](PlayerRegistry.md#m-playerfor-peerkey-bool), but returns false instead of adding a key when the registry already holds its maximum. |

### <a id="m-playerfor-peerkey-bool"></a>PlayerFor(PeerKey, out bool)

```csharp
public uint PlayerFor(PeerKey key, out bool isNew);
```

The player of `key`, adding it on first sight.

**Parameters**

- `key` ([`PeerKey`](../../OneGame.Core/OneGame.Core/PeerKey.md)): An authenticated key.
- `isNew` (`bool`): True if this call added the player.

**Returns:** `uint`

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): `key` is empty (an unauthenticated peer has no identity to register).

### <a id="m-save"></a>Save()

```csharp
public byte[] Save();
```

Serialises the registry: next id, count, then (id, key) pairs in id order.

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

Like [`PlayerRegistry.PlayerFor()`](PlayerRegistry.md#m-playerfor-peerkey-bool), but returns false instead of adding a key when the registry already holds its maximum.

**Parameters**

- `key` ([`PeerKey`](../../OneGame.Core/OneGame.Core/PeerKey.md)): An authenticated key.
- `player` (`uint`): The player id when this returns true.
- `isNew` (`bool`): True if this call added the player.

**Returns:** `bool`
