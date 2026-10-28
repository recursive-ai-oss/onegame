[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# TextParse static class

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public static class TextParse
```

Parsing helpers the generated state-patch code uses. They never throw.

## Methods

| Member | Description |
| --- | --- |
| [`TryBool(string, out bool)`](#m-trybool-string-bool) | Parses `true` or `false`. |
| [`TryInteger(string, long, long, out long)`](#m-tryinteger-string-long-long-long) | Parses a decimal integer within a range. |
| [`TryUnsigned(string, ulong, out ulong)`](#m-tryunsigned-string-ulong-ulong) | Parses a decimal unsigned integer up to `max`. |

### <a id="m-trybool-string-bool"></a>TryBool(string, out bool)

```csharp
public static bool TryBool(string text, out bool value);
```

Parses `true` or `false`.

**Parameters**

- `text` (`string`)
- `value` (`bool`)

**Returns:** `bool`

### <a id="m-tryinteger-string-long-long-long"></a>TryInteger(string, long, long, out long)

```csharp
public static bool TryInteger(string text, long min, long max, out long value);
```

Parses a decimal integer within a range.

**Parameters**

- `text` (`string`)
- `min` (`long`)
- `max` (`long`)
- `value` (`long`)

**Returns:** `bool`

### <a id="m-tryunsigned-string-ulong-ulong"></a>TryUnsigned(string, ulong, out ulong)

```csharp
public static bool TryUnsigned(string text, ulong max, out ulong value);
```

Parses a decimal unsigned integer up to `max`.

**Parameters**

- `text` (`string`)
- `max` (`ulong`)
- `value` (`ulong`)

**Returns:** `bool`
