[API reference](../../index.md) › [OneGame.UI.Kit](../index.md) › OneGame.UI.Kit

# KitTheme static class

**Namespace:** `OneGame.UI.Kit`  
**Package:** [`RecursiveAI.OneGame.UI.Kit`](../index.md)

```csharp
public static class KitTheme
```

Builds the style sheet that gives every kit component its look. A game adds the sheet (or lets [`KitHost`](KitHost.md) do it) and then adds its own rules, which win ties.

## Methods

| Member | Description |
| --- | --- |
| [`Create(KitTokens?)`](#m-create-kittokens) | Creates the kit's style sheet from a set of tokens. |

### <a id="m-create-kittokens"></a>Create(KitTokens?)

```csharp
public static StyleSheet Create(KitTokens? tokens = null);
```

Creates the kit's style sheet from a set of tokens.

**Parameters**

- `tokens` ([`KitTokens`](KitTokens.md)?): The colors and sizes; [`KitTokens.Dark`](KitTokens.md#m-dark) by default.

**Returns:** [`StyleSheet`](../../OneGame.UI/OneGame.UI/StyleSheet.md)
