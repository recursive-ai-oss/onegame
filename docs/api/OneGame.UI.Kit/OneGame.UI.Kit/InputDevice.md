[API reference](../../index.md) › [OneGame.UI.Kit](../index.md) › OneGame.UI.Kit

# InputDevice enum

**Namespace:** `OneGame.UI.Kit`  
**Package:** [`RecursiveAI.OneGame.UI.Kit`](../index.md)

```csharp
public enum InputDevice : int
```

The kind of device the player last used, which decides what an [`InputGlyphs`](InputGlyphs.md) glyph shows.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-keyboard"></a>`Keyboard` | `0` | Keyboard: the glyph is a key cap with the key's name. |
| <a id="m-mouse"></a>`Mouse` | `1` | Mouse: the glyph is a mouse with the button highlighted. |
| <a id="m-gamepad"></a>`Gamepad` | `2` | Gamepad: the glyph is a button, shoulder, stick or d-pad picture. |
