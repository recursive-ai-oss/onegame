[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.UI

# StyleSheet class

**Namespace:** `OneGame.UI`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)

```csharp
public sealed class StyleSheet
```

An ordered list of style rules. Add sheets to a [`UiTree`](UiTree.md); later sheets win ties in specificity.

## Constructors

| Member | Description |
| --- | --- |
| [`StyleSheet()`](#m-ctor) |  |

### <a id="m-ctor"></a>StyleSheet()

```csharp
public StyleSheet();
```

## Methods

| Member | Description |
| --- | --- |
| [`Include(StyleSheet)`](#m-include-stylesheet) | Appends all rules of another sheet, so a game can extend a shared UI library's sheet. |
| [`Rule(string, Action<StyleBuilder>)`](#m-rule-string-action-stylebuilder) | Adds a rule. |

### <a id="m-include-stylesheet"></a>Include(StyleSheet)

```csharp
public StyleSheet Include(StyleSheet other);
```

Appends all rules of another sheet, so a game can extend a shared UI library's sheet.

**Parameters**

- `other` ([`StyleSheet`](StyleSheet.md))

**Returns:** [`StyleSheet`](StyleSheet.md)

### <a id="m-rule-string-action-stylebuilder"></a>Rule(string, Action&lt;StyleBuilder&gt;)

```csharp
public StyleSheet Rule(string selectors, Action<StyleBuilder> declare);
```

Adds a rule. `selectors` is a comma-separated list of selectors built from a type (the element kind), `#id`, `.class`, `:hover :focus :active :disabled :root :first-child :last-child :nth-child(odd|even|N)`, with descendant (space) and child (`>`) combinators.

**Parameters**

- `selectors` (`string`)
- `declare` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`StyleBuilder`](StyleBuilder.md)&gt;)

**Returns:** [`StyleSheet`](StyleSheet.md)

**Exceptions**

- [`UiStyleException`](UiStyleException.md): A selector does not parse.
