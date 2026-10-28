[API reference](../../index.md) › [OneGame.UI.Kit](../index.md) › OneGame.UI.Kit

# ThemeLint static class

**Namespace:** `OneGame.UI.Kit`  
**Package:** [`RecursiveAI.OneGame.UI.Kit`](../index.md)

```csharp
public static class ThemeLint
```

Checks that a theme's colors stay legible. The pairs are the ones the kit's components actually draw: text on every surface it sits on, text on the filled variants in each state, disabled text (which WCAG exempts, but which must stay readable at 3:1), and the non-text indicators (the focus ring, the outline of checkboxes, radios, sliders and inputs, the accent fill of checked and filled parts, scroll thumbs) at 3:1. Use [`ThemeLint.Check()`](ThemeLint.md#m-check-kittokens) on tokens in a test, or [`ThemeLint.Check()`](ThemeLint.md#m-check-uitree) on a running UI to include a game's own token overrides.

## Methods

| Member | Description |
| --- | --- |
| [`Check(KitTokens)`](#m-check-kittokens) | Checks the colors the tokens produce through [`KitTheme.Create()`](KitTheme.md#m-create-kittokens), derived state colors included. |
| [`Check(UiTree)`](#m-check-uitree) | Checks the tokens in effect on a UI tree's root: the kit's theme as the cascade resolved it, with any token a game sheet overrides. |
| [`Check(Func<string, Color?>)`](#m-check-func-string-color) | Checks colors looked up by token name; null means the token is not defined. |

### <a id="m-check-kittokens"></a>Check(KitTokens)

```csharp
public static ThemeLintReport Check(KitTokens tokens);
```

Checks the colors the tokens produce through [`KitTheme.Create()`](KitTheme.md#m-create-kittokens), derived state colors included.

**Parameters**

- `tokens` ([`KitTokens`](KitTokens.md))

**Returns:** [`ThemeLintReport`](ThemeLintReport.md)

### <a id="m-check-uitree"></a>Check(UiTree)

```csharp
public static ThemeLintReport Check(UiTree tree);
```

Checks the tokens in effect on a UI tree's root: the kit's theme as the cascade resolved it, with any token a game sheet overrides. Resolve styles with one frame first.

**Parameters**

- `tree` ([`UiTree`](../../OneGame.UI/OneGame.UI/UiTree.md))

**Returns:** [`ThemeLintReport`](ThemeLintReport.md)

### <a id="m-check-func-string-color"></a>Check(Func&lt;string, Color?&gt;)

```csharp
public static ThemeLintReport Check(Func<string, Color?> token);
```

Checks colors looked up by token name; null means the token is not defined.

**Parameters**

- `token` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-2)`<string,` [`Color`](../../OneGame.UI/OneGame.UI/Color.md)?&gt;): Returns the color of a token such as `--kit-text`, or null.

**Returns:** [`ThemeLintReport`](ThemeLintReport.md)
