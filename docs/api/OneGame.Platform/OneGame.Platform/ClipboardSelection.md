[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# ClipboardSelection enum

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)

```csharp
public enum ClipboardSelection : byte
```

Which system selection a clipboard call talks to.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-clipboard"></a>`Clipboard` | `0` | The ordinary clipboard: copy and paste. |
| <a id="m-primary"></a>`Primary` | `1` | The "primary" selection of X11: the text last highlighted, pasted with the middle button. Other systems do not have it. |
