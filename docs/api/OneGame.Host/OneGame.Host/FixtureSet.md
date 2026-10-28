[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# FixtureSet class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public sealed class FixtureSet
```

The named fixtures of a game. A fixture fills a fresh world (systems and tables are already registered).

## Constructors

| Member | Description |
| --- | --- |
| [`FixtureSet()`](#m-ctor) |  |

### <a id="m-ctor"></a>FixtureSet()

```csharp
public FixtureSet();
```

## Properties

| Member | Description |
| --- | --- |
| [`Names`](#m-names) | The fixture names in declaration order. |

### <a id="m-names"></a>Names

```csharp
public IReadOnlyList<string> Names { get; }
```

The fixture names in declaration order.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`

## Methods

| Member | Description |
| --- | --- |
| [`Add(string, Action<World>)`](#m-add-string-action-world) | Declares a fixture: a named starting state that tests, scenarios and live control can load without playing up to it. |

### <a id="m-add-string-action-world"></a>Add(string, Action&lt;World&gt;)

```csharp
public FixtureSet Add(string name, Action<World> populate);
```

Declares a fixture: a named starting state that tests, scenarios and live control can load without playing up to it.

**Parameters**

- `name` (`string`): The fixture's name.
- `populate` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`World`](../../OneGame.Core/OneGame.Core/World.md)&gt;): Adds entities and sets state on the fresh world (sim code, run on the authority).

**Returns:** [`FixtureSet`](FixtureSet.md) — This set, for chaining.

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The name is already taken.
