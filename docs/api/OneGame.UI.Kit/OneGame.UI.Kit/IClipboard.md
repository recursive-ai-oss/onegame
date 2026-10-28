[API reference](../../index.md) › [OneGame.UI.Kit](../index.md) › OneGame.UI.Kit

# IClipboard interface

**Namespace:** `OneGame.UI.Kit`  
**Package:** [`RecursiveAI.OneGame.UI.Kit`](../index.md)

```csharp
public interface IClipboard
```

Where copied text goes. A windowed game's fields use the system clipboard; [`MemoryClipboard`](MemoryClipboard.md) keeps text inside the process (headless runs and tests).

## Methods

| Member | Description |
| --- | --- |
| [`GetText()`](#m-gettext) | The text on the clipboard, or null when there is none. |
| [`SetText(string)`](#m-settext-string) | Replaces the clipboard's content. |

### <a id="m-gettext"></a>GetText()

```csharp
string? GetText();
```

The text on the clipboard, or null when there is none.

**Returns:** `string?`

### <a id="m-settext-string"></a>SetText(string)

```csharp
void SetText(string text);
```

Replaces the clipboard's content.

**Parameters**

- `text` (`string`)
