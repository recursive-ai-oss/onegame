[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# KeyNames static class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public static class KeyNames
```

Key names: the platform's physical [`Key`](../../OneGame.Platform/OneGame.Platform/Key.md) to the DOM-style key strings the UI uses, and back.

## Methods

| Member | Description |
| --- | --- |
| [`ToUiKey(Key, bool)`](#m-touikey-key-bool) | The UI's key string for a physical key. |
| [`TryParse(string?, out Key)`](#m-tryparse-string-key) | Reads a key from a DOM-style name (`Enter`, `a`, `ArrowLeft`, `F5`, a space) or a [`Key`](../../OneGame.Platform/OneGame.Platform/Key.md) name (`Digit1`, `LeftShift`). |

### <a id="m-touikey-key-bool"></a>ToUiKey(Key, bool)

```csharp
public static string ToUiKey(Key key, bool shift = false);
```

The UI's key string for a physical key. Letters are lower case unless `shift` is set.

**Parameters**

- `key` ([`Key`](../../OneGame.Platform/OneGame.Platform/Key.md))
- `shift` (`bool`)

**Returns:** `string`

### <a id="m-tryparse-string-key"></a>TryParse(string?, out Key)

```csharp
public static bool TryParse(string? text, out Key key);
```

Reads a key from a DOM-style name (`Enter`, `a`, `ArrowLeft`, `F5`, a space) or a [`Key`](../../OneGame.Platform/OneGame.Platform/Key.md) name (`Digit1`, `LeftShift`).

**Parameters**

- `text` (`string?`)
- `key` ([`Key`](../../OneGame.Platform/OneGame.Platform/Key.md))

**Returns:** `bool`
