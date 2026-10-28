[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# ScopeSet struct

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public readonly struct ScopeSet
```

A set of scopes a connection is allowed to touch: all of them, none, or a listed few.

## Properties

| Member | Description |
| --- | --- |
| [`All`](#m-all) | Every scope. |
| [`IsAll`](#m-isall) | True for the set of every scope. |
| [`None`](#m-none) | No scope. |

### <a id="m-all"></a>All

```csharp
public static ScopeSet All { get; }
```

Every scope.

**Type:** [`ScopeSet`](ScopeSet.md)

### <a id="m-isall"></a>IsAll

```csharp
public bool IsAll { get; }
```

True for the set of every scope.

**Type:** `bool`

### <a id="m-none"></a>None

```csharp
public static ScopeSet None { get; }
```

No scope.

**Type:** [`ScopeSet`](ScopeSet.md)

## Methods

| Member | Description |
| --- | --- |
| [`Contains(ScopeId)`](#m-contains-scopeid) | Whether `scope` is in the set. |
| [`Of(ScopeId[])`](#m-of-scopeid) | Exactly the listed scopes. |

### <a id="m-contains-scopeid"></a>Contains(ScopeId)

```csharp
public bool Contains(ScopeId scope);
```

Whether `scope` is in the set.

**Parameters**

- `scope` ([`ScopeId`](ScopeId.md))

**Returns:** `bool`

### <a id="m-of-scopeid"></a>Of(ScopeId\[\])

```csharp
public static ScopeSet Of(params ScopeId[] ids);
```

Exactly the listed scopes.

**Parameters**

- `ids` ([`ScopeId`](ScopeId.md)\[\])

**Returns:** [`ScopeSet`](ScopeSet.md)
