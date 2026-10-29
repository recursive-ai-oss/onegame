[API reference](../index.md) › OneGame.Core

# OneGame.Core

Entity runtime: fixed-point numbers, random streams and the attributes that declare entities.

**Package:** `RecursiveAI.OneGame.Core`

```sh
dotnet add package RecursiveAI.OneGame.Core
```

## OneGame.Core

| Type | Kind | Description |
| --- | --- | --- |
| [`Audience`](OneGame.Core/Audience.md) | struct | Who a subscriber is, for visibility: the player it speaks for and the team it plays on. |
| [`CommandAttribute`](OneGame.Core/CommandAttribute.md) | attribute | Marks an entity method as a command: a request that a view or test sends and the authority validates and runs. |
| [`CommandObserver`](OneGame.Core/CommandObserver.md) | delegate | Receives every command a world executes, in order, for logging and replay. |
| [`CommandReply`](OneGame.Core/CommandReply.md) | struct | What a command handler answers: accepted, or rejected with a reason that travels back to whoever sent the command. |
| [`CommandStatus`](OneGame.Core/CommandStatus.md) | enum | What happened to a command when it was sent. |
| [`CoreModule`](OneGame.Core/CoreModule.md) | static class | Identifies the Core assembly; a placeholder until its first feature lands. |
| [`DuplicateTypeException`](OneGame.Core/DuplicateTypeException.md) | class | Thrown when an entity type is registered twice or two types share an id. |
| [`Entity`](OneGame.Core/Entity.md) | class | Base class of every entity. |
| [`EntityAttribute`](OneGame.Core/EntityAttribute.md) | attribute | Declares a partial class as an entity: synced properties, commands and sim logic in one class. |
| [`EntityId`](OneGame.Core/EntityId.md) | struct | Identifies an entity in a world. |
| [`EntityList`](OneGame.Core/EntityList.md) | class | The live entities of a world, in id order. |
| [`EntityList.Enumerator`](OneGame.Core/EntityList.Enumerator.md) | struct | Walks live entities; entities added while walking are visited too, removed ones are skipped. |
| [`EntityRegistry`](OneGame.Core/EntityRegistry.md) | class | The entity types a world can create while decoding a snapshot. |
| [`Entropy`](OneGame.Core/Entropy.md) | static class | Unpredictable bytes for connection ids, handshake secrets and keys. |
| [`FQuat`](OneGame.Core/FQuat.md) | struct | A rotation as a quaternion in [`Fixed`](OneGame.Core/Fixed.md) numbers. |
| [`FVec2`](OneGame.Core/FVec2.md) | struct | A two-dimensional point or vector in [`Fixed`](OneGame.Core/Fixed.md) numbers. |
| [`FVec3`](OneGame.Core/FVec3.md) | struct | A three-dimensional point or vector in [`Fixed`](OneGame.Core/Fixed.md) numbers. +X is right, +Y is up and -Z is forward, as in the 3D renderer. |
| [`Fixed`](OneGame.Core/Fixed.md) | struct | Deterministic fixed-point number: a signed 64-bit integer holding the value times 2^32 (31 integer bits and 32 fractional bits). |
| [`HotDataAttribute`](OneGame.Core/HotDataAttribute.md) | attribute | Marks a struct whose public fields become the columns of a generated struct-of-arrays table (`<Name>Table`) for hot sims. |
| [`IEntropySource`](OneGame.Core/IEntropySource.md) | interface | A source of unpredictable bytes, for platforms that supply their own (the browser's `crypto.getRandomValues`, say). |
| [`IHotTable`](OneGame.Core/IHotTable.md) | interface | A struct-of-arrays table the world snapshots and hashes. |
| [`ILerp<TSelf>`](OneGame.Core/ILerp-1.md) | interface | A value that can be blended between two of its kind; [`Timed<T>`](OneGame.Core/Timed-1.md) uses it to interpolate. |
| [`ISimSystem`](OneGame.Core/ISimSystem.md) | interface | Sim logic that is not tied to one entity. |
| [`ISyncValue<TSelf>`](OneGame.Core/ISyncValue-1.md) | interface | A value type with a binary codec. |
| [`JsonOut`](OneGame.Core/JsonOut.md) | class | A small JSON writer for state dumps. |
| [`KeyFiles`](OneGame.Core/KeyFiles.md) | record | The files of one named key: the private key file and the one-line public key file beside it. |
| [`KeyFormatException`](OneGame.Core/KeyFormatException.md) | class | A key or key file was malformed. |
| [`KeyStore`](OneGame.Core/KeyStore.md) | static class | Makes and finds key files. |
| [`MigrateAttribute`](OneGame.Core/MigrateAttribute.md) | attribute | Marks a static method on an entity that loads state saved at an older schema version into the current layout: `static void Name(ref SyncReader old, MyEntity entity)`. |
| [`MutatesStateAttribute`](OneGame.Core/MutatesStateAttribute.md) | attribute | Marks a method that changes simulation state. |
| [`NetKeyPair`](OneGame.Core/NetKeyPair.md) | class | An X25519 key pair: a server's identity or a client's. |
| [`NotInWorldException`](OneGame.Core/NotInWorldException.md) | class | Thrown when an entity that is not in a world is asked to do something that needs one. |
| [`OwnerKeyAttribute`](OneGame.Core/OwnerKeyAttribute.md) | attribute | Marks the synced [`UInt32`](https://learn.microsoft.com/dotnet/api/system.uint32) or [`Int32`](https://learn.microsoft.com/dotnet/api/system.int32) property that says which player owns the entity. |
| [`PeerKey`](OneGame.Core/PeerKey.md) | struct | A 32-byte X25519 public key. |
| [`ReplicaWriteException`](OneGame.Core/ReplicaWriteException.md) | class | Thrown when code changes state on a replica. |
| [`Rng`](OneGame.Core/Rng.md) | class | A named, seedable random stream (xoshiro256\*\*). |
| [`RngStreams`](OneGame.Core/RngStreams.md) | class | The world's random streams, found by name. |
| [`RootMotionTrack`](OneGame.Core/RootMotionTrack.md) | class | The movement of an animation's root bone, one fixed-point step per simulation tick. |
| [`SimAttribute`](OneGame.Core/SimAttribute.md) | attribute | Marks a class, struct or method outside an entity as sim code, so the sim analyzers check it. |
| [`SimSafeAttribute`](OneGame.Core/SimSafeAttribute.md) | attribute | Vouches that an API gives the same result on every machine and run, so sim code may call it. |
| [`SimTrustAttribute`](OneGame.Core/SimTrustAttribute.md) | attribute | Lets sim code call a library you trust to be deterministic. |
| [`StateHash`](OneGame.Core/StateHash.md) | static class | A deterministic 64-bit hash of bytes (FNV-1a with a final avalanche), used for drift detection. |
| [`SyncError`](OneGame.Core/SyncError.md) | enum | Why decoding untrusted bytes failed. |
| [`SyncLimitException`](OneGame.Core/SyncLimitException.md) | class | Thrown when state about to be written would be refused by the decoder's limits. |
| [`SyncLimits`](OneGame.Core/SyncLimits.md) | static class | Hard limits applied while decoding, so a hostile input cannot make the decoder allocate without bound. |
| [`SyncList<T>`](OneGame.Core/SyncList-1.md) | class | A list that is part of an entity's synced state. |
| [`SyncRead`](OneGame.Core/SyncRead.md) | static class | Reads synced values by type argument, which is how generated code reads types whose reader is an explicit interface member. |
| [`SyncReader`](OneGame.Core/SyncReader.md) | struct | Reads the generated binary format from untrusted bytes. |
| [`SyncWriter`](OneGame.Core/SyncWriter.md) | class | Growable byte buffer that the generated codecs write into. |
| [`SyncedAttribute`](OneGame.Core/SyncedAttribute.md) | attribute | Marks a partial property as synced state. |
| [`SyncedStructAttribute`](OneGame.Core/SyncedStructAttribute.md) | attribute | Marks a partial struct whose public fields and properties form a synced value. |
| [`TeamKeyAttribute`](OneGame.Core/TeamKeyAttribute.md) | attribute | Marks the synced [`UInt32`](https://learn.microsoft.com/dotnet/api/system.uint32) or [`Int32`](https://learn.microsoft.com/dotnet/api/system.int32) property that says which team the entity belongs to. |
| [`TextParse`](OneGame.Core/TextParse.md) | static class | Parsing helpers the generated state-patch code uses. |
| [`Timed<T>`](OneGame.Core/Timed-1.md) | struct | A value that moves from one value to another over a span of ticks. |
| [`Transform3`](OneGame.Core/Transform3.md) | struct | Where something is in a 3D world: a position, a rotation and a scale, all in [`Fixed`](OneGame.Core/Fixed.md) numbers so they can be synced and simulated deterministically. |
| [`ViewAttribute`](OneGame.Core/ViewAttribute.md) | attribute | Marks a member or a whole class as view code. |
| [`Visibility`](OneGame.Core/Visibility.md) | enum | Who may receive an entity or one of its synced properties. |
| [`World`](OneGame.Core/World.md) | class | The simulation: entities in a deterministic order, hot tables, systems, the named random streams and the tick counter. |
| [`WorldRole`](OneGame.Core/WorldRole.md) | enum | Whether a world owns its state or mirrors another node's. |

## OneGame.Storage

| Type | Kind | Description |
| --- | --- | --- |
| [`FolderByteStore`](OneGame.Storage/FolderByteStore.md) | class | Files kept in a folder on disk: the store for desktop, Android and iOS (where the folder is the app's own storage). |
| [`IByteStore`](OneGame.Storage/IByteStore.md) | interface | A flat folder of named byte blobs: the low level under save slots and settings. |
| [`ISettingsFile`](OneGame.Storage/ISettingsFile.md) | interface | The type-independent face of a [`SettingsFile<T>`](OneGame.Storage/SettingsFile-1.md), for tools that list a game's settings. |
| [`MemoryByteStore`](OneGame.Storage/MemoryByteStore.md) | class | Files held in memory only. |
| [`SettingsFile<T>`](OneGame.Storage/SettingsFile-1.md) | class | A typed settings object kept in a store: load it once at start, change its properties, call [`SettingsFile<T>.Save()`](OneGame.Storage/SettingsFile-1.md#m-save). |
| [`SettingsLoad`](OneGame.Storage/SettingsLoad.md) | record | What [`SettingsFile<T>.Load()`](OneGame.Storage/SettingsFile-1.md#m-load) found. |
| [`SettingsSchema<T>`](OneGame.Storage/SettingsSchema-1.md) | class | Describes a settings class: which properties are saved, under which keys, and the range each one may take. |
| [`SettingsStatus`](OneGame.Storage/SettingsStatus.md) | enum | How a settings load went. |
| [`StoreNames`](OneGame.Storage/StoreNames.md) | static class | The rules for file names in an [`IByteStore`](OneGame.Storage/IByteStore.md): ASCII letters, digits, `.`, `_` and `-`, up to 96 characters. |
