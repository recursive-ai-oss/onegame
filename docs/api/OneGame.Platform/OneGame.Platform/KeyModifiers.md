[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# KeyModifiers enum

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)

```csharp
[Flags]
public enum KeyModifiers : byte
```

Modifier keys held while an event happened.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-none"></a>`None` | `0` | No modifier. |
| <a id="m-shift"></a>`Shift` | `1` | Either Shift key. |
| <a id="m-control"></a>`Control` | `2` | Either Control key. |
| <a id="m-alt"></a>`Alt` | `4` | Either Alt key. |
| <a id="m-meta"></a>`Meta` | `8` | Either Meta (Windows, Command, Super) key. |
| <a id="m-capslock"></a>`CapsLock` | `16` | Caps Lock is on. |
| <a id="m-numlock"></a>`NumLock` | `32` | Num Lock is on. |
