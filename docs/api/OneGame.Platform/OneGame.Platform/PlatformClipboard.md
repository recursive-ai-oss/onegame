[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# PlatformClipboard class

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)

```csharp
public sealed class PlatformClipboard
```

Text on the system clipboard. Reads and writes are synchronous and only for UTF-8 text; use it from the thread that runs the [`PlatformApp`](PlatformApp.md). Text another program owns is fetched from it on demand, so a read can wait (up to about a second) for a slow owner, and it is served to other programs while the application keeps pumping events.

## Fields

| Member | Description |
| --- | --- |
| [`MaxTextBytes`](#m-maxtextbytes) | The longest text read or written, in UTF-8 bytes. |

### <a id="m-maxtextbytes"></a>MaxTextBytes

```csharp
public const int MaxTextBytes = 4194304;
```

The longest text read or written, in UTF-8 bytes. Longer clipboard content is treated as absent.

**Type:** `int`

## Properties

| Member | Description |
| --- | --- |
| [`IsAvailable`](#m-isavailable) | True when the system has a clipboard the application can reach. |

### <a id="m-isavailable"></a>IsAvailable

```csharp
public bool IsAvailable { get; }
```

True when the system has a clipboard the application can reach. A headless run keeps a private in-process clipboard, so it is true there; it is false on a Wayland compositor with no data-device support.

**Type:** `bool`

## Methods

| Member | Description |
| --- | --- |
| [`GetText(ClipboardSelection)`](#m-gettext-clipboardselection) | The text on the clipboard, or null when there is none, it is not text, it is too long, or its owner did not answer in time. |
| [`SetText(string, ClipboardSelection)`](#m-settext-string-clipboardselection) | Puts text on the clipboard and keeps serving it until another program takes the clipboard. |
| [`Supports(ClipboardSelection)`](#m-supports-clipboardselection) | Whether `selection` exists on this system. |

### <a id="m-gettext-clipboardselection"></a>GetText(ClipboardSelection)

```csharp
public string? GetText(ClipboardSelection selection = 0);
```

The text on the clipboard, or null when there is none, it is not text, it is too long, or its owner did not answer in time.

**Parameters**

- `selection` ([`ClipboardSelection`](ClipboardSelection.md))

**Returns:** `string?`

### <a id="m-settext-string-clipboardselection"></a>SetText(string, ClipboardSelection)

```csharp
public bool SetText(string text, ClipboardSelection selection = 0);
```

Puts text on the clipboard and keeps serving it until another program takes the clipboard. Returns false when the system refused or the text is longer than [`PlatformClipboard.MaxTextBytes`](PlatformClipboard.md#m-maxtextbytes).

**Parameters**

- `text` (`string`)
- `selection` ([`ClipboardSelection`](ClipboardSelection.md))

**Returns:** `bool`

### <a id="m-supports-clipboardselection"></a>Supports(ClipboardSelection)

```csharp
public bool Supports(ClipboardSelection selection);
```

Whether `selection` exists on this system.

**Parameters**

- `selection` ([`ClipboardSelection`](ClipboardSelection.md))

**Returns:** `bool`
