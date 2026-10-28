[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# ScreenSet class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public sealed class ScreenSet
```

The named screens of a game. Each is built fresh when opened, and may read the game's world through the instance it is given.

## Constructors

| Member | Description |
| --- | --- |
| [`ScreenSet()`](#m-ctor) |  |

### <a id="m-ctor"></a>ScreenSet()

```csharp
public ScreenSet();
```

## Properties

| Member | Description |
| --- | --- |
| [`Names`](#m-names) | The screen names in declaration order. |

### <a id="m-names"></a>Names

```csharp
public IReadOnlyList<string> Names { get; }
```

The screen names in declaration order.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`

## Methods

| Member | Description |
| --- | --- |
| [`Add(string, Func<GameInstance, Element>)`](#m-add-string-func-gameinstance-element) | Declares a screen. |

### <a id="m-add-string-func-gameinstance-element"></a>Add(string, Func&lt;GameInstance, Element&gt;)

```csharp
public ScreenSet Add(string name, Func<GameInstance, Element> build);
```

Declares a screen. The builder runs each time the screen opens and returns its root element; read the world through the [`GameInstance`](GameInstance.md) it is given, because the world object is replaced when a save or fixture is loaded.

**Parameters**

- `name` (`string`): The screen's name; `main` opens first.
- `build` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-2)&lt;[`GameInstance`](GameInstance.md), [`Element`](../../OneGame.UI/OneGame.UI/Element.md)&gt;): Builds the screen's element tree.

**Returns:** [`ScreenSet`](ScreenSet.md) — This set, for chaining.

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The name is already taken.
