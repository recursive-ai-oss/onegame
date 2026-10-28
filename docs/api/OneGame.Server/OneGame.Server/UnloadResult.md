[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# UnloadResult record

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`UnloadResult`](UnloadResult.md)&gt;

```csharp
public sealed record UnloadResult : IEquatable<UnloadResult>
```

The result of an unload.

## Constructors

| Member | Description |
| --- | --- |
| [`UnloadResult(string, int, bool)`](#m-ctor-string-int-bool) | The result of an unload. |

### <a id="m-ctor-string-int-bool"></a>UnloadResult(string, int, bool)

```csharp
public UnloadResult(string Name, int EntitiesRemoved, bool Collected);
```

The result of an unload.

**Parameters**

- `Name` (`string`): The plugin's name.
- `EntitiesRemoved` (`int`): How many of its entities were removed from the scopes.
- `Collected` (`bool`): True when its load context was garbage collected.

## Properties

| Member | Description |
| --- | --- |
| [`Collected`](#m-collected) | True when its load context was garbage collected. |
| [`EntitiesRemoved`](#m-entitiesremoved) | How many of its entities were removed from the scopes. |
| [`Name`](#m-name) | The plugin's name. |

### <a id="m-collected"></a>Collected

```csharp
public bool Collected { get; init; }
```

True when its load context was garbage collected.

**Type:** `bool`

### <a id="m-entitiesremoved"></a>EntitiesRemoved

```csharp
public int EntitiesRemoved { get; init; }
```

How many of its entities were removed from the scopes.

**Type:** `int`

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

The plugin's name.

**Type:** `string`
