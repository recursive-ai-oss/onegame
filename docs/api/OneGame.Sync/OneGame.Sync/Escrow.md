[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# Escrow static class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public static class Escrow
```

Installing escrow into a game's worlds, and the identity the coordinator acts as.

## Fields

| Member | Description |
| --- | --- |
| [`CoordinatorPlayer`](#m-coordinatorplayer) | The player id the coordinator acts as. |

### <a id="m-coordinatorplayer"></a>CoordinatorPlayer

```csharp
public const uint CoordinatorPlayer = 4294967294;
```

The player id the coordinator acts as. Connections of the coordinator carry `Trust.Owner(scope).As(CoordinatorPlayer)`; nobody else should.

**Type:** `uint`

## Methods

| Member | Description |
| --- | --- |
| [`CoordinatorTrust(ScopeId)`](#m-coordinatortrust-scopeid) | The trust a coordinator's node needs on a participant's or its own scope. |
| [`DeskOf(World)`](#m-deskof-world) | The desk entity of a world (authority or replica), or null. |
| [`Install(World, IEscrowHandler)`](#m-install-world-iescrowhandler) | Adds the escrow book and the desk entity to an authority world. |
| [`Register(EntityRegistry)`](#m-register-entityregistry) | Registers the escrow entity types with a registry (the one every world of the game shares). |

### <a id="m-coordinatortrust-scopeid"></a>CoordinatorTrust(ScopeId)

```csharp
public static Trust CoordinatorTrust(ScopeId scope);
```

The trust a coordinator's node needs on a participant's or its own scope.

**Parameters**

- `scope` ([`ScopeId`](ScopeId.md))

**Returns:** [`Trust`](Trust.md)

### <a id="m-deskof-world"></a>DeskOf(World)

```csharp
public static EscrowDesk? DeskOf(World world);
```

The desk entity of a world (authority or replica), or null.

**Parameters**

- `world` ([`World`](../../OneGame.Core/OneGame.Core/World.md))

**Returns:** [`EscrowDesk`](EscrowDesk.md)?

### <a id="m-install-world-iescrowhandler"></a>Install(World, IEscrowHandler)

```csharp
public static EscrowBook Install(World world, IEscrowHandler handler);
```

Adds the escrow book and the desk entity to an authority world. Call it right after building the world, in the same order on every node. Replica worlds need only the entity type registered (the book is hidden and never sent).

**Parameters**

- `world` ([`World`](../../OneGame.Core/OneGame.Core/World.md)): An authority world.
- `handler` ([`IEscrowHandler`](IEscrowHandler.md)): How the game debits and credits its own holdings.

**Returns:** [`EscrowBook`](EscrowBook.md)

### <a id="m-register-entityregistry"></a>Register(EntityRegistry)

```csharp
public static void Register(EntityRegistry registry);
```

Registers the escrow entity types with a registry (the one every world of the game shares).

**Parameters**

- `registry` ([`EntityRegistry`](../../OneGame.Core/OneGame.Core/EntityRegistry.md))
