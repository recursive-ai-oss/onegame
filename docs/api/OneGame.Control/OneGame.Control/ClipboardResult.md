[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# ClipboardResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ClipboardResult`](ClipboardResult.md)&gt;

```csharp
public sealed record ClipboardResult : IEquatable<ClipboardResult>
```

Result of `clipboard_get` and `clipboard_set`. `Text` is null when the clipboard holds no text.

## Constructors

| Member | Description |
| --- | --- |
| [`ClipboardResult(string?, bool)`](#m-ctor-string-bool) | Result of `clipboard_get` and `clipboard_set`. |

### <a id="m-ctor-string-bool"></a>ClipboardResult(string?, bool)

```csharp
public ClipboardResult(string? Text, bool Ok);
```

Result of `clipboard_get` and `clipboard_set`. `Text` is null when the clipboard holds no text.

**Parameters**

- `Text` (`string?`): The clipboard text, or null.
- `Ok` (`bool`): True when the clipboard is supported and the call worked.

## Properties

| Member | Description |
| --- | --- |
| [`Ok`](#m-ok) | True when the clipboard is supported and the call worked. |
| [`Text`](#m-text) | The clipboard text, or null. |

### <a id="m-ok"></a>Ok

```csharp
public bool Ok { get; init; }
```

True when the clipboard is supported and the call worked.

**Type:** `bool`

### <a id="m-text"></a>Text

```csharp
public string? Text { get; init; }
```

The clipboard text, or null.

**Type:** `string?`
