[API reference](../../index.md) › [OneGame.UI.Kit](../index.md) › OneGame.UI.Kit

# MemoryClipboard class

**Namespace:** `OneGame.UI.Kit`  
**Package:** [`RecursiveAI.OneGame.UI.Kit`](../index.md)  
**Implements:** [`IClipboard`](IClipboard.md)

```csharp
public sealed class MemoryClipboard : IClipboard
```

A clipboard that lives in memory, shared by everything that was given the same instance.

## Constructors

| Member | Description |
| --- | --- |
| [`MemoryClipboard()`](#m-ctor) |  |

### <a id="m-ctor"></a>MemoryClipboard()

```csharp
public MemoryClipboard();
```

## Methods

| Member | Description |
| --- | --- |
| [`GetText()`](#m-gettext) | The text on the clipboard, or null when there is none. |
| [`SetText(string)`](#m-settext-string) | Replaces the clipboard's content. |

### <a id="m-gettext"></a>GetText()

```csharp
public string? GetText();
```

The text on the clipboard, or null when there is none.

**Returns:** `string?`

### <a id="m-settext-string"></a>SetText(string)

```csharp
public void SetText(string text);
```

Replaces the clipboard's content.

**Parameters**

- `text` (`string`)
