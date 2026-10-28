[API reference](../../index.md) › [OneGame.Pages](../index.md) › OneGame.Pages

# PageScopes class

**Namespace:** `OneGame.Pages`  
**Package:** [`RecursiveAI.OneGame.Pages`](../index.md)

```csharp
public sealed class PageScopes
```

Gives the numbers a page sets with `--host-scope` (or `StyleDeclaration.SetHostScope`) their meaning. An unbound number draws as if the scope were absent, so a page can be written before the game decides what its scopes do.

## Constructors

| Member | Description |
| --- | --- |
| [`PageScopes()`](#m-ctor) |  |

### <a id="m-ctor"></a>PageScopes()

```csharp
public PageScopes();
```

## Fields

| Member | Description |
| --- | --- |
| [`Over`](#m-over) | The layer drawn over the game's 2D pass, where pages normally are. |
| [`Under`](#m-under) | The layer drawn beneath the game's 2D pass: sprites and the `OneGame.UI` tree draw over it. |

### <a id="m-over"></a>Over

```csharp
public const int Over = 0;
```

The layer drawn over the game's 2D pass, where pages normally are.

**Type:** `int`

### <a id="m-under"></a>Under

```csharp
public const int Under = -1;
```

The layer drawn beneath the game's 2D pass: sprites and the `OneGame.UI` tree draw over it.

**Type:** `int`

## Properties

| Member | Description |
| --- | --- |
| [`Count`](#m-count) | How many numbers are bound. |
| [`UsesUnder`](#m-usesunder) | True when some scope is bound to the [`PageScopes.Under`](PageScopes.md#m-under) layer. |

### <a id="m-count"></a>Count

```csharp
public int Count { get; }
```

How many numbers are bound.

**Type:** `int`

### <a id="m-usesunder"></a>UsesUnder

```csharp
public bool UsesUnder { get; }
```

True when some scope is bound to the [`PageScopes.Under`](PageScopes.md#m-under) layer.

**Type:** `bool`

## Methods

| Member | Description |
| --- | --- |
| [`Additive(int)`](#m-additive-int) | Draws the content of scope `scope` apart and adds it to what is below it, for glows and light. |
| [`Clear(int)`](#m-clear-int) | Removes a binding. |
| [`Layer(int, int)`](#m-layer-int-int) | Draws the content of scope `scope` in `layer` ([`PageScopes.Under`](PageScopes.md#m-under) or [`PageScopes.Over`](PageScopes.md#m-over)) instead of with the page. |

### <a id="m-additive-int"></a>Additive(int)

```csharp
public void Additive(int scope);
```

Draws the content of scope `scope` apart and adds it to what is below it, for glows and light.

**Parameters**

- `scope` (`int`): The host-scope number.

### <a id="m-clear-int"></a>Clear(int)

```csharp
public bool Clear(int scope);
```

Removes a binding.

**Parameters**

- `scope` (`int`): The host-scope number.

**Returns:** `bool` — True when it was bound.

### <a id="m-layer-int-int"></a>Layer(int, int)

```csharp
public void Layer(int scope, int layer);
```

Draws the content of scope `scope` in `layer` ([`PageScopes.Under`](PageScopes.md#m-under) or [`PageScopes.Over`](PageScopes.md#m-over)) instead of with the page.

**Parameters**

- `scope` (`int`): The host-scope number.
- `layer` (`int`): Where it is drawn.

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The layer is neither [`PageScopes.Under`](PageScopes.md#m-under) nor [`PageScopes.Over`](PageScopes.md#m-over).
